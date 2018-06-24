using AmaruCommon.Actions;
using AmaruCommon.Actions.Targets;
using AmaruCommon.Communication.Messages;
using AmaruCommon.Constants;
using AmaruCommon.GameAssets.Cards;
using AmaruCommon.GameAssets.Characters;
using AmaruCommon.GameAssets.Players;
using AmaruCommon.Responses;
using AmaruServer.Game.Managing;
using ClientServer.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static ClientServer.Communication.ClientTCP;

namespace AmaruServer.Networking
{
    public class AIUser : User
    {
        private bool myTurn = false;
        MessageHandler messageHandler = null;
        Queue<PlayerAction> listOfActions;

        public struct GoalFunctionWeights
        {
            //Addictive values
            public const double AliveCreature = 2.0;
            public const double PlayerDead = 50.0;

            public const double LegendaryCardBonus= 5;
            public const double ShieldUpCreatureBonus = 4;
            public const double ShieldMaidenCreatureBonus = 2;

            //Multiplicative values
            public const double BonusOnHpPlayer = 3;
            public const double ManaGreed = 1.2;

            //Unimplementd cause it is not what i want
            public const double EPGreed = 0;

            //Random Power
            public const double Unpredictability = 0;

        }
        public struct GoalFunctionMyFieldWeights
        {
            //Addictive values
            public const double ShieldAndInnerZone = -2.0;
            public const double ShieldMaidenPresentLowHPInnerZone = 4.0;
            public const double CanAttackInnerZone = -2.5;
            public const double LowHPOuterZone =  -4.0;

            //Multiplicative Values
            public const double LowHpWhen = 1.0/3.0;
        }

        public AIUser(string logger) : base(logger)
        {
            listOfActions = new Queue<PlayerAction>();
        }

        public override void SetPlayer(Player player, GameManager gameManager)
        {
            this.Player = player;
            this.GameManager = gameManager;
            this.messageHandler = this.GameManager.HandlePlayerMessage;
        }

        public override void Write(Message mex)
        {
            if (mex is ResponseMessage)
            {
                Response responseReceived = ((ResponseMessage)mex).Response;
                if (responseReceived is NewTurnResponse)
                {
                    if (((NewTurnResponse)responseReceived).ActivePlayer == CharacterEnum.AMARU)
                    {
                        //Log("MOVIMENTO");
                        myTurn = true;
                        GameManager toIterate = CreateGameManagerAndStuff(this.GameManager);
                        double discontentment = ValueMyField(toIterate);
                        bool gain = true;

                        while (gain)
                        {
                            KeyValuePair<Double, PlayerAction> pair = ThinkToMove(toIterate);

                            if (pair.Key == Double.MinValue)
                            {
                                gain = false;
                                continue;
                            }

                            Log("discontentment: " + discontentment + " new Value: " + pair.Key);

                            if (discontentment < pair.Key)
                            {
                                try
                                {
                                    pair.Value.Visit(toIterate.ExecutionVisitor);
                                    discontentment = ValueMyField(toIterate);
                                    listOfActions.Enqueue(pair.Value);
                                    gain = true;
                                }
                                catch (Exception e)
                                {
                                    Log(e.ToString());
                                }
                            }
                            else
                            {
                                gain = false;
                            }
                        }
                        listOfActions.Enqueue(new EndTurnAction(CharacterEnum.AMARU, -1, GameManager.IsMainTurn));
                    }
                }
                else if (myTurn && (responseReceived is MainTurnResponse))
                {
                    myTurn = false;
                    GameManager toIterate = CreateGameManagerAndStuff(this.GameManager);
                    double discontentment = ValueGoalDiscontentment(toIterate);
                    Log("My Hand");
                    Log("\n");

                    foreach (Card c in Player.Hand)
                    {
                        Log(c.Name);
                    }

                    bool gain = true;

                    while (gain)
                    {
                        KeyValuePair<Double, PlayerAction> pair = Think(toIterate);

                        if (pair.Key == Double.MinValue)
                        {
                            gain = false;
                            continue;
                        }
                        Log("discontentment: " + discontentment + " new Value: " + pair.Key);
                        if (discontentment < pair.Key)
                        {
                            try
                            {
                                pair.Value.Visit(toIterate.ExecutionVisitor);
                                discontentment = ValueGoalDiscontentment(toIterate);
                                listOfActions.Enqueue(pair.Value);
                                gain = true;
                            }
                            catch (Exception e)
                            {
                                Log(e.ToString());
                            }
                        }
                        else
                        {
                            gain = false;
                        }
                    }
                    listOfActions.Enqueue(new EndTurnAction(CharacterEnum.AMARU, -1, GameManager.IsMainTurn));
                }
            }
        }

        private KeyValuePair<Double,PlayerAction> ThinkToMove(GameManager gm)
        {
            GameManager toUse = CreateGameManagerAndStuff(gm);
            Player me = toUse.UserDict[CharacterEnum.AMARU].Player;
            LimitedList<Card> myCards = me.Hand;
            LimitedList<CreatureCard> myWarZone = me.Outer;
            LimitedList<CreatureCard> myInnerZone = me.Inner;

            List<KeyValuePair<Double, PlayerAction>> listPossibleActions = new List<KeyValuePair<double, PlayerAction>>();
            foreach (CreatureCard cd in myWarZone.Concat(myInnerZone))
            {
                try
                {
                    GameManager toUseTemp = CreateGameManagerAndStuff(toUse);
                 //   Log("TESTO la carta "+ cd.Name + "   in war zone? " + myWarZone.Contains(cd));
                 //   Log(me.IsShieldMaidenProtected.ToString());
                    MoveCreatureAction myIntention = new MoveCreatureAction(CharacterEnum.AMARU, cd.Id, (myInnerZone.Contains(cd)? Place.OUTER: Place.INNER), 0);
                    myIntention.Visit(toUseTemp.ValidationVisitor);
                    myIntention.Visit(toUseTemp.ExecutionVisitor);
                    Double valueOfGoal = ValueMyField(toUseTemp);
                    listPossibleActions.Add(new KeyValuePair<Double, PlayerAction>(valueOfGoal, myIntention));
                }
                catch (Exception e){
                    Log("Eccezione " + e.ToString());
                    Log(cd.Name);
                }            
            }
            listPossibleActions = listPossibleActions.OrderByDescending(x => x.Key).ToList();
            if (listPossibleActions.Count > 0)
            {
                Log(listPossibleActions[0].ToString());
                return listPossibleActions[0];
            }
            else
            {
                return new KeyValuePair<double, PlayerAction>(Double.MinValue, new EndTurnAction(CharacterEnum.AMARU, -1, false));
            }
        }


        private KeyValuePair<Double, PlayerAction> Think(GameManager gm)
        {
            GameManager toUse = CreateGameManagerAndStuff(gm);
            Player me = toUse.UserDict[CharacterEnum.AMARU].Player;
            LimitedList<Card> myCards = me.Hand;
            LimitedList<CreatureCard> myWarZone = me.Outer;
            LimitedList<CreatureCard> myInnerZone = me.Inner;

            //Inizializzo tutte le azioni posisbili legate alle carte in mano
            List<KeyValuePair<Double, PlayerAction>> listPossibleActions = new List<KeyValuePair<double, PlayerAction>>();
            foreach (Card c in myCards)
            {
                try
                {
                    if (c is CreatureCard && myWarZone.Count < AmaruConstants.OUTER_MAX_SIZE)
                    {
                        GameManager toUseTemp = CreateGameManagerAndStuff(toUse);
                        PlayACreatureFromHandAction myIntention = new PlayACreatureFromHandAction(CharacterEnum.AMARU, c.Id, Place.OUTER, Player.Outer.Count);
                        myIntention.Visit(toUseTemp.ValidationVisitor);
                        myIntention.Visit(toUseTemp.ExecutionVisitor);
                        Double valueOfGoal = ValueGoalDiscontentment(toUseTemp);
                        listPossibleActions.Add(new KeyValuePair<Double, PlayerAction>(valueOfGoal, myIntention));

                    } else if (c is CreatureCard && myInnerZone.Count < AmaruConstants.INNER_MAX_SIZE) {
                        GameManager toUseTemp = CreateGameManagerAndStuff(toUse);
                        PlayACreatureFromHandAction myIntention = new PlayACreatureFromHandAction(CharacterEnum.AMARU, c.Id, Place.INNER, Player.Inner.Count);
                        myIntention.Visit(toUseTemp.ValidationVisitor);
                        myIntention.Visit(toUseTemp.ExecutionVisitor);
                        Double valueOfGoal = ValueGoalDiscontentment(toUseTemp);
                        listPossibleActions.Add(new KeyValuePair<Double, PlayerAction>(valueOfGoal, myIntention));
                    }
                    else if (c is SpellCard)
                    {
                        /*
                        GameManager toUseTemp = createGameManagerAndStuff(toUse);
                        PlayASpellFromHandAction myIntention = new PlayASpellFromHandAction(CharacterEnum.AMARU, c.Id, null);
                        myIntention.Visit(toUseTemp.ValidationVisitor);
                        myIntention.Visit(toUseTemp.ExecutionVisitor);
                        Double valueOfGoal = ValueGoalDiscontentment(toUseTemp);
                        listPossibleActions.Add(new KeyValuePair<Double, PlayerAction>(valueOfGoal, myIntention));
                        */
                    }
                }
                catch (Exception e)
                {
                    Log("Eccezione " + e.ToString());
                    Log(c.Name);
                }
            }

            // inizializzo struttura dati di possibili target per un attacco, potando la ricerca delle azioni evidentemente impossibili
            List<CardTarget> allAcceptableTargets = new List<CardTarget>();
            List<PlayerTarget> allAcceptablePlayerTarget = new List<PlayerTarget>();
            foreach (KeyValuePair<CharacterEnum, User> pair in toUse.UserDict.ToArray())
            {
                Player player = pair.Value.Player;
                foreach (CreatureCard cd in player.Outer)
                {
                    allAcceptableTargets.Add(new CardTarget(pair.Key, cd));
                }
                if (!pair.Value.Player.IsShieldMaidenProtected)
                {
                    foreach (CreatureCard cd in player.Inner)
                    {
                        allAcceptableTargets.Add(new CardTarget(pair.Key, cd));
                    }
                }
                if (!player.IsShieldUpProtected && !player.IsImmune)
                {
                    allAcceptablePlayerTarget.Add(new PlayerTarget(pair.Key));
                }
            }
            //inizializzo tutti i target possibili per un attacco
            foreach (CreatureCard c in myWarZone)
            {
                if (c.Energy == 0 || c.Attack is null)
                {
                    continue;
                }
            //    Log("Nome " + c.Name + " Energy: " + c.Energy + " Attack null? " + (c.Attack is null));
                foreach (CardTarget cTarget in allAcceptableTargets)
                {
                    try
                    {
                        GameManager toUseTemp = CreateGameManagerAndStuff(toUse);
                        AttackCreatureAction myIntention = new AttackCreatureAction(CharacterEnum.AMARU, c.Id, Property.ATTACK, cTarget);
                        myIntention.Visit(toUseTemp.ValidationVisitor);
                        myIntention.Visit(toUseTemp.ExecutionVisitor);
                        Double valueOfGoal = ValueGoalDiscontentment(toUseTemp);
                        listPossibleActions.Add(new KeyValuePair<Double, PlayerAction>(valueOfGoal, myIntention));
                    }
                    catch (Exception e)
                    {
                        Log("Eccezione " + e.ToString());
                        Log(c.Name);
                        Log(cTarget.Card.Name);
                    }
                }
                foreach (PlayerTarget pTarget in allAcceptablePlayerTarget)
                {
                    try
                    {
                        GameManager toUseTemp = CreateGameManagerAndStuff(toUse);
                        AttackPlayerAction myIntention = new AttackPlayerAction(CharacterEnum.AMARU, c.Id, Property.ATTACK, pTarget);
                        myIntention.Visit(toUseTemp.ValidationVisitor);
                        myIntention.Visit(toUseTemp.ExecutionVisitor);
                        Double valueOfGoal = ValueGoalDiscontentment(toUseTemp);
                        listPossibleActions.Add(new KeyValuePair<Double, PlayerAction>(valueOfGoal, myIntention));
                    }
                    catch (Exception e)
                    {
                        Log("Eccezione Player" + e.ToString());
                        Log(c.Name);
                    }
                }
            }
            listPossibleActions = listPossibleActions.OrderByDescending(x => x.Key).ToList();
            if (listPossibleActions.Count > 0)
            {
                Log(listPossibleActions[0].ToString());
                return listPossibleActions[0];
            }
            else
            {
                return new KeyValuePair<double, PlayerAction>(Double.MinValue, new EndTurnAction(CharacterEnum.AMARU, -1, false));
            }

        }

        public override Message ReadSync(int timeout_s)
        {
            System.Threading.Thread.Sleep(1200+ (new Random()).Next(500));
            return new ActionMessage(listOfActions.Dequeue());
        }

        public override void ReadASync(bool continuous)
        {
            // Asyncronously calls messageHabndler(Message)
        }

        public override void Close(Message notification = null)
        {

        }

        public double ValueGoalDiscontentment(GameManager gm)
        {
            double value = 0;
            List<Player> lp = new List<Player>();
            Player me = gm.UserDict[CharacterEnum.AMARU].Player;
            foreach (KeyValuePair<CharacterEnum, User> p in gm.UserDict.ToList())
            {
                if (p.Key == CharacterEnum.AMARU)
                {
                    continue;
                }
                Player player = p.Value.Player;
                if (player.IsAlive)
                {
                    lp.Add(player);
                }
                else
                {
                    //Se sto per uccidere un giocatore lo faccio, valutando in maniera positiva la situazione in cui un giocatore è morto
                    value += GoalFunctionWeights.PlayerDead;
                }
            }

            //Somma vita mia e delle mie creature, il mio mana 
            value += me.Health;
            value += CalculateMyField(me);
            value += me.Mana * GoalFunctionWeights.ManaGreed;

            //sommo la vita degli altri e la deviazione standard, la vita media nei loro campi e la deviazione standard
            List<double> listHpField = new List<double>();
            List<double> listHpPlayers = new List<double>();
            foreach (Player p in lp)
            {
                listHpField.Add(CalculateHpOnField(p));
                listHpPlayers.Add(p.Health);
            }
            double meanHp = Tools.calculateAverage(listHpPlayers);
            double meanHpField = Tools.calculateAverage(listHpField);

            value -= meanHp * GoalFunctionWeights.BonusOnHpPlayer;
            value -= meanHpField;
            value -= Tools.calculateStd(listHpPlayers);
            value -= Tools.calculateStd(listHpField);
            value += new Random().NextDouble()*GoalFunctionWeights.Unpredictability;
            return value;
        }

        private double ValueMyField(GameManager toUseTemp)
        {
            return CalculateMyField(toUseTemp.UserDict[CharacterEnum.AMARU].Player);
        }

        private double CalculateMyField(Player me)
        {
            double value = 0;
            foreach (CreatureCard cd in me.Inner)
            {
                value += cd.Health;
                value += GoalFunctionWeights.AliveCreature;
                value += ValueGoalForSingleCreature(me.IsShieldMaidenProtected,Place.INNER,cd);
            }
            foreach(CreatureCard cd in me.Outer)
            {
                value += cd.Health;
                value += GoalFunctionWeights.AliveCreature;
                value += ValueGoalForSingleCreature(me.IsShieldMaidenProtected,Place.OUTER,cd);
            }
            return value;
        }

        private double ValueGoalForSingleCreature(bool ShieldMaidenProtected,Place p, CreatureCard c)
        {
            double value = 0;
            if (p ==  Place.INNER)
            {
                if (!(c.Attack is null) && (c.Ability is null))
                    value += GoalFunctionMyFieldWeights.CanAttackInnerZone;

                if(c.Shield != Shield.NONE)
                    value += GoalFunctionMyFieldWeights.ShieldAndInnerZone;

                if (LowHP(c) && ShieldMaidenProtected)
                    value += GoalFunctionMyFieldWeights.ShieldMaidenPresentLowHPInnerZone;
            }
            else if (p== Place.OUTER)
            {
                if (LowHP(c))
                {
                    value += GoalFunctionMyFieldWeights.LowHPOuterZone;
                }
            }
            return value;
        }


        private bool LowHP(CreatureCard c)
        {
            CreatureCard creatureOriginal = (CreatureCard)c.Original;
            return (c.Health <= creatureOriginal.Health * GoalFunctionMyFieldWeights.LowHpWhen);
        }
        
        
        private double CalculateHpOnField(Player p)
        {
            double hp = 0;
            int t = 0;
            foreach (CreatureCard c in p.Outer)
            {
                if(c.Shield == Shield.SHIELDUP || c.Shield == Shield.BOTH)
                {
                    hp += GoalFunctionWeights.ShieldUpCreatureBonus;
                }
                if(c.Shield == Shield.SHIELDMAIDEN)
                {
                    hp += GoalFunctionWeights.ShieldMaidenCreatureBonus;
                }
                hp += c.Health;
                t += 1;
            }
            foreach (CreatureCard c in p.Inner)
            {
                hp += c.Health;
                t += 1;
            }
            if (t == 0)
            {
                return 0;
            }
            hp = hp + (t * GoalFunctionWeights.AliveCreature);
            return hp;
        }


        private GameManager CreateGameManagerAndStuff(GameManager m)
        {
            //per ogni giocatore in generale voglio sapere:
            GameManager FakeGM = new GameManager(m, "AILogger");
            FakeGM.IsMainTurn = !myTurn;
            return FakeGM;
        }
    }
}
