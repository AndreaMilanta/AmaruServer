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
        ValidationVisitor myValidation;

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
                        myTurn = true;

                        //thinkToMove();
                        listOfActions.Enqueue(new EndTurnAction(CharacterEnum.AMARU, -1, GameManager.IsMainTurn));
                    }
                }
                else if (myTurn && (responseReceived is MainTurnResponse))
                {
                    myTurn = false;
                    GameManager toIterate = createGameManagerAndStuff(this.GameManager);
                    double discontentment = ValueGoalDiscontentment(toIterate);
                    Log("Start to think");
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
                        //                       Log(toIterate._userDict[CharacterEnum.AMARU].Player.GetCardFromId(pair.Value.PlayedCardId, Place.HAND).Name.ToString());
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
                            Log("No vantaggio");
                            gain = false;
                        }
                    }
                    Log("SCS");
                    listOfActions.Enqueue(new EndTurnAction(CharacterEnum.AMARU, -1, GameManager.IsMainTurn));
                }
            }
        }

        private void thinkToMove()
        {
            LimitedList<CreatureCard> inner = Player.Inner;
            LimitedList<CreatureCard> outer = Player.Outer;
            List<int> moved = new List<int>();
            int countOuter = outer.Count;
            //per ogni giocatore in generale voglio sapere:

            LimitedList<Card> myCards = Player.Hand;
            LimitedList<CreatureCard> myWarZone = Player.Outer;
            LimitedList<CreatureCard> myInnerZone = Player.Inner;

            //Prima implementazione di Ai, se può mettere creature fuori, ce le mette.
            foreach (CreatureCard cd in inner)
            {
                try
                {
                    MoveCreatureAction moveCreatureAction = new MoveCreatureAction(CharacterEnum.AMARU, cd.Id, Place.OUTER, 0);
                    moveCreatureAction.Visit(myValidation);
                    listOfActions.Enqueue(moveCreatureAction);
                }
                catch { }

            }
            /*
            foreach (CreatureCard cd in outer)
            {
                try
                {
                    MoveCreatureAction moveCreatureAction = new MoveCreatureAction(CharacterEnum.AMARU, cd.Id, Place.INNER, 0);
                    moveCreatureAction.Visit(myValidation);
                    listOfPossibleActions.Add(moveCreatureAction);
                } catch { }

            }
            */
        }

        private KeyValuePair<Double, PlayerAction> Think(GameManager gm)
        {
            GameManager toUse = createGameManagerAndStuff(gm);
            Player me = toUse._userDict[CharacterEnum.AMARU].Player;
            LimitedList<Card> myCards = me.Hand;
            LimitedList<CreatureCard> myWarZone = me.Outer;
            LimitedList<CreatureCard> myInnerZone = me.Inner;

            //Inizializzo tutte le azioni posisbili legate alle carte in mano
            List<KeyValuePair<Double, PlayerAction>> listPossibleActions = new List<KeyValuePair<double, PlayerAction>>();
            foreach (Card c in myCards)
            {
                try
                {
                    if (c is CreatureCard)
                    {
                        GameManager toUseTemp = createGameManagerAndStuff(toUse);
                        PlayACreatureFromHandAction myIntention = new PlayACreatureFromHandAction(CharacterEnum.AMARU, c.Id, Place.OUTER, Player.Outer.Count);
                        myIntention.Visit(toUseTemp.ValidationVisitor);
                        myIntention.Visit(toUseTemp.ExecutionVisitor);
                        Double valueOfGoal = ValueGoalDiscontentment(toUseTemp);
                        listPossibleActions.Add(new KeyValuePair<Double, PlayerAction>(valueOfGoal, myIntention));
                    }
                    else if (c is SpellCard)
                    {
                        //Amaru ha solo spell senza target CREDO
                        GameManager toUseTemp = createGameManagerAndStuff(toUse);
                        PlayASpellFromHandAction myIntention = new PlayASpellFromHandAction(CharacterEnum.AMARU, c.Id, null);
                        myIntention.Visit(toUseTemp.ValidationVisitor);
                        myIntention.Visit(toUseTemp.ExecutionVisitor);
                        Double valueOfGoal = ValueGoalDiscontentment(toUseTemp);
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
            foreach (KeyValuePair<CharacterEnum, User> pair in toUse._userDict.ToArray())
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
                Log("Nome " + c.Name + " Energy: " + c.Energy + " Attack " + (c.Attack is null));
                foreach (CardTarget cTarget in allAcceptableTargets)
                {
                    try
                    {
                        GameManager toUseTemp = createGameManagerAndStuff(toUse);
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
                        GameManager toUseTemp = createGameManagerAndStuff(toUse);
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
            System.Threading.Thread.Sleep(1500);
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
            Player me = gm._userDict[CharacterEnum.AMARU].Player;
            foreach (KeyValuePair<CharacterEnum, User> p in gm._userDict.ToList())
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
                    //Se sto per uccidere un giocatore lo faccio, valutando bene la situazione in cui un giocatore è morto
                    value += 20;
                }
            }

            //Somma vita mia e delle mie creature, il mio mana (moltiplicato per 2 per dargli più valore)
            value += me.Health;
            value += calculateHpOnField(me) * (me.Outer.Count + me.Inner.Count);
            value += me.Outer.Count;
            value += me.Inner.Count;
            value += me.Mana;

            //sommo la vita degli altri e la deviazione standard, la vita media nei loro campi e la deviazione standard
            List<double> listHpField = new List<double>();
            List<double> listHpPlayers = new List<double>();
            foreach (Player p in lp)
            {
                listHpField.Add(calculateHpOnField(p));
                listHpPlayers.Add(p.Health);
            }
            double meanHp = Tools.calculateAverage(listHpPlayers);
            double meanHpField = Tools.calculateAverage(listHpField);

            value -= meanHp * 2;
            value -= meanHpField;
            value -= Tools.calculateStd(listHpPlayers);
            value -= Tools.calculateStd(listHpField);
            return value;
        }

        private double calculateHpOnField(Player p)
        {
            double hp = 0;
            int t = 0;
            foreach (CreatureCard c in p.Outer)
            {
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
            hp /= t;
            return hp;
        }


        private GameManager createGameManagerAndStuff(GameManager m)
        {
            //per ogni giocatore in generale voglio sapere:
            GameManager FakeGM = new GameManager(m, "AILogger");
            FakeGM.IsMainTurn = !myTurn;
            return FakeGM;
        }
    }
}
