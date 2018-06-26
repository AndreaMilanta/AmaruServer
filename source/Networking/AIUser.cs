using AmaruCommon.Actions;
using AmaruCommon.Actions.Targets;
using AmaruCommon.Communication.Messages;
using AmaruCommon.Constants;
using AmaruCommon.GameAssets.Cards;
using AmaruCommon.GameAssets.Cards.Properties;
using AmaruCommon.GameAssets.Characters;
using AmaruCommon.GameAssets.Players;
using AmaruCommon.Responses;
using AmaruServer.Game.Managing;
using ClientServer.Messages;
using Combinatorics.Collections;
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
        TargetValidationVisitor targetVisitor = new TargetValidationVisitor("FakeLogger");

        public struct GoalFunctionWeights
        {
            //Addictive values
            public const double AliveCreature = 1;
            public const double PlayerDead = 50.0;
            public const double LegendaryCardBonus= 5;
            public const double ShieldUpCreatureBonus = 4;
            public const double ShieldMaidenCreatureBonus = 2;

            //Multiplicative values
            public const double MultiplierOfPlayerHP = 3.5;
            public const double BalanceAttractor = 0.7;
            public const double ManaGreed = 1.5;
            public const double EPGreed = 0.9;
 

            //Random Power
            public const double Unpredictability = 0.1;

        }
        public struct GoalFunctionMyFieldWeights
        {
            //Addictive value
            public const double ShieldAndInnerZone = -4.1;
            public const double ShieldMaidenPresentLowHPInnerZone = 4.5;
            public const double CanAttackInnerZone = -6.0;
            public const double LowHPOuterZone =  -4.0;
            public const double EPValueOfLegendaryCreature = 2.0;

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
                    //   Log("TESTO la carta "+ cd.Name + "   in war zone? " + myWarZone.Contains(cd));
                    //   Log(me.IsShieldMaidenProtected.ToString());
                    GameManager toUseTemp = CreateGameManagerAndStuff(toUse);
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

            //Initializing all the actions doable
            List<KeyValuePair<Double, PlayerAction>> listPossibleActions = new List<KeyValuePair<double, PlayerAction>>();
            foreach (Card c in myCards)
            {
                try
                {
                    if (c is CreatureCard && myWarZone.Count < AmaruConstants.OUTER_MAX_SIZE)
                    {

                        PlayACreatureFromHandAction myIntention = new PlayACreatureFromHandAction(CharacterEnum.AMARU, c.Id, Place.OUTER, Player.Outer.Count);
                        Double valueOfGoal = SimulateAndEvaluate(toUse, myIntention);
                        listPossibleActions.Add(new KeyValuePair<Double, PlayerAction>(valueOfGoal, myIntention));

                    } else if (c is CreatureCard && myInnerZone.Count < AmaruConstants.INNER_MAX_SIZE) {

                        PlayACreatureFromHandAction myIntention = new PlayACreatureFromHandAction(CharacterEnum.AMARU, c.Id, Place.INNER, Player.Inner.Count);
                        Double valueOfGoal = SimulateAndEvaluate(toUse, myIntention);
                        listPossibleActions.Add(new KeyValuePair<Double, PlayerAction>(valueOfGoal, myIntention));
                    }
                    else if (c is SpellCard)
                    {
                        //Amaru hasn't spell card requiring targets
                        PlayASpellFromHandAction myIntention = new PlayASpellFromHandAction(CharacterEnum.AMARU, c.Id, null);
                        Double valueOfGoal = SimulateAndEvaluate(toUse, myIntention);
                        listPossibleActions.Add(new KeyValuePair<Double, PlayerAction>(valueOfGoal, myIntention));
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
            // Struttura dati per le abilità
            List<Target> abilityTarget = new List<Target>();

            foreach (KeyValuePair<CharacterEnum, User> pair in toUse.UserDict.ToArray())
            {
                Player player = pair.Value.Player;
                foreach (CreatureCard cd in player.Outer)
                {
                    allAcceptableTargets.Add(new CardTarget(pair.Key, cd));
                    abilityTarget.Add(new CardTarget(pair.Key, cd));
                }
                foreach (CreatureCard cd in player.Inner)
                {
                    abilityTarget.Add(new CardTarget(pair.Key, cd));

                    if (!pair.Value.Player.IsShieldMaidenProtected)
                        allAcceptableTargets.Add(new CardTarget(pair.Key, cd));
                }
                if (!player.IsImmune)
                {
                    abilityTarget.Add(new PlayerTarget(pair.Key));
                    if (!player.IsShieldUpProtected)
                    {
                        allAcceptablePlayerTarget.Add(new PlayerTarget(pair.Key));
                    }
                }
            }

            //All the possible attacks
            foreach (CreatureCard c in myWarZone)
            {
                if (c.Energy == 0 || c.Attack is null )
                {
                    continue;
                }
                //    Log("Nome " + c.Name + " Energy: " + c.Energy + " Attack null? " + (c.Attack is null));
                foreach (CardTarget cTarget in allAcceptableTargets)
                {
                    try
                    {
                        AttackCreatureAction myIntention = new AttackCreatureAction(CharacterEnum.AMARU, c.Id, Property.ATTACK, cTarget);
                        Double valueOfGoal = SimulateAndEvaluate(toUse, myIntention);
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
                        AttackPlayerAction myIntention = new AttackPlayerAction(CharacterEnum.AMARU, c.Id, Property.ATTACK, pTarget);
                        Double valueOfGoal = SimulateAndEvaluate(toUse, myIntention);
                        listPossibleActions.Add(new KeyValuePair<Double, PlayerAction>(valueOfGoal, myIntention));
                    }
                    catch (Exception e)
                    {
                        Log("Eccezione Player" + e.ToString());
                        Log(c.Name);
                    }
                }
            }

            //All the possible abilities
            foreach(CreatureCard cd in myWarZone.Concat(myInnerZone))
            {
                //to prune the search
                if (cd.Ability is null || cd.Ability.Cost > cd.Energy)
                {
                    continue;
                }

                int numTarget = cd.Ability.NumTarget;

                //Avoid searching for proper targets if numtarget == 0
                if (numTarget == 0)
                {
                    try
                    {
                        UseAbilityAction myIntention = new UseAbilityAction(CharacterEnum.AMARU, cd.Id, target: null);
                        Double valueOfGoal = SimulateAndEvaluate(toUse, myIntention);
                        listPossibleActions.Add(new KeyValuePair<Double, PlayerAction>(valueOfGoal, myIntention));
                    }
                    catch (Exception e)
                    {
                        Log("Eccezione Abilità" + e.ToString());
                        Log(cd.Name);
                    }
                    continue;
                }

                //Looking for all the proper targets for the ability
                List<Target> targetDiscerned = new List<Target>();
                foreach(Target t in abilityTarget)
                {
                    targetVisitor.Target = t;
                    if (cd.Ability.Visit(targetVisitor) >= 0)
                    {
                        targetDiscerned.Add(t);
                    }
                }

                //I have to generate all the possible combinations of available targets. Following the rules of the game i have to do it without repetition.
                List<Combinations<Target>> targetsCombined = new List<Combinations<Target>>();
                for(int i =1; i<= numTarget; i++)
                {
                    targetsCombined.Add( new Combinations<Target>(targetDiscerned, i, GenerateOption.WithoutRepetition));
                }

                //iterate all the possible combinations to evaluate the ability on that particular target
                foreach (List<Target> lt in targetsCombined.SelectMany(x => x))
                {
                    try
                    {
                        UseAbilityAction myIntention = new UseAbilityAction(CharacterEnum.AMARU, cd.Id, lt);
                        double valueOfGoal = SimulateAndEvaluate(toUse, myIntention);
                        listPossibleActions.Add(new KeyValuePair<Double, PlayerAction>(valueOfGoal, myIntention));
                    }
                    catch (Exception e)
                    {
                        Log("Eccezione Player" + e.ToString());
                        Log(cd.Name);
                    }
                }
            }
            //order the possible actions depending on their value
            listPossibleActions = listPossibleActions.OrderByDescending(x => x.Key).ToList();
            if (listPossibleActions.Count > 0)
            {
//                Log("Best Choice");
//                Log(listPossibleActions[0].ToString());
                return listPossibleActions[0];
            }
            else
            {
                return new KeyValuePair<double, PlayerAction>(Double.MinValue, new EndTurnAction(CharacterEnum.AMARU, -1, false));
            }
        }

        private double SimulateAndEvaluate(GameManager toUse, PlayerAction myIntention)
        {
            GameManager toUseTemp = CreateGameManagerAndStuff(toUse);
            myIntention.Visit(toUseTemp.ValidationVisitor);
            myIntention.Visit(toUseTemp.ExecutionVisitor);
            Double valueOfGoal = ValueGoalDiscontentment(toUseTemp);
            return valueOfGoal;
        }

        public override Message ReadSync(int timeout_s)
        {
            System.Threading.Thread.Sleep(1700+ (new Random()).Next(500));
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
            //This function evaluate the goal discontentment

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
                    //I get a prize for a dead player, this way i am sure i will kill a player if i have the chance.
                    value += GoalFunctionWeights.PlayerDead;
                }
            }

            value += me.Health;
            value += CalculateMyField(me);
            value += me.Mana * GoalFunctionWeights.ManaGreed;

            List<double> listHpField = new List<double>();
            List<double> listHpPlayers = new List<double>();
            foreach (Player p in lp)
            {
                double temp = CalculateHpOnField(p);
                value -= temp;
                listHpField.Add(temp);
                listHpPlayers.Add(p.Health);
            }

            double meanHp = Tools.calculateAverage(listHpPlayers);

            value -= meanHp * GoalFunctionWeights.MultiplierOfPlayerHP;
            value -= Tools.calculateStd(listHpPlayers)* GoalFunctionWeights.BalanceAttractor;
            value -= Tools.calculateStd(listHpField)* GoalFunctionWeights.BalanceAttractor;
            value += new Random().NextDouble()*GoalFunctionWeights.Unpredictability;
            return value;
        }

        private double ValueMyField(GameManager toUseTemp)
        {
            Player me = toUseTemp.UserDict[CharacterEnum.AMARU].Player;
            double value = 0;

            //when outer field is full
            if (me.Outer.Count == 6)
            {
                double outerValue = me.Outer.Average(x => (x.Health+x.myPowerAttack()+(x.Ability is null ? GoalFunctionMyFieldWeights.CanAttackInnerZone/2:0)));
                double outerMin = me.Outer.Min(x => (x.Health + x.myPowerAttack() + (x.Ability is null ? GoalFunctionMyFieldWeights.CanAttackInnerZone / 2 : 0)));
                double innerValue = me.Inner.Average(x => (x.Health + x.myPowerAttack() + (x.Ability is null ? -GoalFunctionMyFieldWeights.CanAttackInnerZone / 2 : 0)));
                double innerMax = me.Inner.Max(x => (x.Health + x.myPowerAttack() + (x.Ability is null ? -GoalFunctionMyFieldWeights.CanAttackInnerZone / 2 : 0)));
                double handValue = me.Hand.Average(x => x is CreatureCard? ((CreatureCard)x).Health + (((CreatureCard)x).myPowerAttack()) : 0);

                value += outerValue - innerValue;
                value += outerValue - handValue;
                value += outerMin - innerMax;

            }
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
            if (c.IsLegendary)
            {
                value += GoalFunctionMyFieldWeights.EPValueOfLegendaryCreature;
            }
            value += c.Energy*GoalFunctionWeights.EPGreed;
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
                if (c.IsLegendary)
                {
                    hp += GoalFunctionWeights.LegendaryCardBonus;
                }
                hp += c.Health;
                t += 1;
            }
            foreach (CreatureCard c in p.Inner)
            {
                hp += c.Health;
                if (c.IsLegendary)
                {
                    hp += GoalFunctionWeights.LegendaryCardBonus;
                }
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
