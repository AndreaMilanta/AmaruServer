using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

using Logging;
using AmaruCommon.Constants;
using AmaruCommon.Communication.Messages;
using AmaruCommon.GameAssets.Characters;
using AmaruCommon.GameAssets.Players;
using AmaruCommon.Responses;
using AmaruServer.Networking;
using AmaruServer.Constants;
using AmaruCommon.Exceptions;
using ClientServer.Messages;
using AmaruCommon.GameAssets.Cards;

namespace AmaruServer.Game.Managing
{
    public class GameManager : Loggable
    {
        public readonly int Id;
        public Dictionary<CharacterEnum, User> UserDict = new Dictionary<CharacterEnum, User>();
        public ValidationVisitor ValidationVisitor { get; private set; }
        public ExecutionVisitor ExecutionVisitor { get; private set; }
        public bool GameHasFinished { get; set; }
        public CharacterEnum ActiveCharacter { get; private set; }                 // Player whose turn it is to play
        public int CurrentRound { get; private set; } = 1;
        public List<CreatureCard> Graveyard = new List<CreatureCard>();
        // private list for simplified turn management
        private int _currentIndex = 0;              // Index of current active player
        private List<CharacterEnum> _turnList;  // List of players in order of turn
        public bool IsMainTurn { get; set; }
        public bool Simulator { get; set; } = false;
        public List<CharacterEnum> PlayersAliveBeforeAction;
        /// <summary>
        /// Constructor for AI
        /// Attenzione a settare bene di chi è il turno (ActiveCharacter) -- ADESSO è CharacterEnum.AMARU
        /// </summary>
        /// <param name="players">!!!Lista già clonata ricorsivamente!!!!</param>
        /// <param name="logger">Nome del logger dell'AI (consiglio "AILogger"</param>
        public GameManager(GameManager gameManager, string logger) : base(logger)
        {

            foreach (CreatureCard c in gameManager.Graveyard)
            {
                this.Graveyard.Add(((CreatureCard)c).Clone());
            }
            this.UserDict = new Dictionary<CharacterEnum, User>();
            foreach (User user in gameManager.UserDict.Values)
            {
                this.UserDict.Add(user.Player.Character, new EmptyUser(new Player(user.Player), "FAKELOGGER"));
            }

            this.GameHasFinished = gameManager.GameHasFinished;
            this.ValidationVisitor = new ValidationVisitor(this);
            this.ExecutionVisitor = new ExecutionVisitor(this);
            this.ActiveCharacter = CharacterEnum.AMARU;
            this.Simulator = true;
        }

        public GameManager(int id, Dictionary<CharacterEnum, User> clientsDict) : base(AmaruConstants.GAME_PREFIX + id)
        {
            try {
                Id = id;
                this.UserDict = clientsDict;

                this.ValidationVisitor = new ValidationVisitor(this);
                this.ExecutionVisitor = new ExecutionVisitor(this);
                this.ActiveCharacter = this.UserDict.Keys.ToArray()[0];
                this._turnList = new List<CharacterEnum>();
                for (int i = 0; i < UserDict.Keys.ToArray().Length; i++) {
                    _turnList.Add(UserDict.Keys.ToArray()[i]);
                    if (i % 2 == 1)
                        _turnList.Add(CharacterEnum.AMARU);
                }
                if (_turnList.Last() != CharacterEnum.AMARU)
                    _turnList.Add(CharacterEnum.AMARU);

            }
            catch(Exception e) { LogException(e); throw e; }
        }

        public void StartGame()
        {
            try 
            { 
                Log("Game " + this.Id + " has started");

                // Get disadvantaged players
                List<CharacterEnum> disadvantaged = UserDict.Keys.ToList().Where(c => c != CharacterEnum.AMARU).ToList().GetRange(AmaruConstants.NUM_PLAYER - AmaruConstants.NUM_DISADVANTAGED, AmaruConstants.NUM_DISADVANTAGED);

                // Init Players
                foreach (CharacterEnum c in UserDict.Keys)                       // Default draw
                    UserDict[c].SetPlayer(new Player(c, AmaruConstants.GAME_PREFIX + Id), this);
                UserDict.Add(CharacterEnum.AMARU, new AIUserExperimental("AI_"+AmaruConstants.GAME_PREFIX + Id));
                    UserDict[CharacterEnum.AMARU].SetPlayer(new AmaruPlayer(AmaruConstants.GAME_PREFIX + Id), this);

                // Draw cards 
                foreach (CharacterEnum c in UserDict.Keys)                       // Default draw
                    UserDict[c].Player.Draw(AmaruConstants.INITIAL_HAND_SIZE);
                foreach (CharacterEnum c in disadvantaged)                      // Extra draw for disadvantaged
                    UserDict[c].Player.Draw(AmaruConstants.INITIAL_HAND_BONUS);

                // Send GameInitMessage to Users
                foreach (CharacterEnum target in UserDict.Keys)
                {
                    Dictionary<CharacterEnum, EnemyInfo> enemies = new Dictionary<CharacterEnum, EnemyInfo>();
                    OwnInfo own = UserDict[target].Player.AsOwn;
                    foreach (CharacterEnum c in CharacterManager.Instance.Others(target))
                        enemies.Add(c, UserDict[c].Player.AsEnemy);
                    UserDict[target].Write(new GameInitMessage(enemies, own, _turnList));
                }

                // Start turn
                StartTurn();
            } 
            catch(Exception e) { LogException(e); throw e; }

            // Start running
            this.Run();
        }

        /// <summary>
        /// Running method for the GameManager
        /// </summary>
        private void Run()
        {
            while (!GameHasFinished)
            {
                try
                {
                    HandlePlayerMessage(UserDict[ActiveCharacter].ReadSync(ServerConstants.ReadTimeout_ms));
                }
                catch (Exception e)
                {
                    LogException(e);
                    KillPlayer4Turn(ActiveCharacter);
                    UserDict.Remove(ActiveCharacter);
                    foreach (CharacterEnum target in UserDict.Keys.ToList())
                        UserDict[target].Write(new PlayerKilledMessage(ActiveCharacter, true));
                    NextTurn();
                }
            }
        }

        public void StartTurn()
        {
            Log("Start turn for " + ActiveCharacter.ToString());
            IsMainTurn = false;

            // Draw card
            int damage = 0;
            Card drawnCard = UserDict[ActiveCharacter].Player.Draw();
            if (drawnCard == null)                                         // Handle if deck is finished
                if (UserDict[ActiveCharacter].Player.Deck.Count == 0)
                    damage = 1;

            // Give mana
            UserDict[ActiveCharacter].Player.Mana += CurrentRound * AmaruConstants.MANA_TURN_FACTOR;

            // Add EP and execute onturnstart for each card on table
            OnTurnStartVisitor OTSVisitor = new OnTurnStartVisitor(ActiveCharacter, AmaruConstants.GAME_PREFIX + Id);
            List<Card> Modified = new List<Card>();
            foreach (CreatureCard card in UserDict[ActiveCharacter].Player.Inner)
            {
                card.Energy++;
                card.Visit(OTSVisitor, ActiveCharacter, null);
                Modified.Add(card);
            }
            foreach (CreatureCard card in UserDict[ActiveCharacter].Player.Outer)
            {
                card.Energy++;
                card.Visit(OTSVisitor, ActiveCharacter, null);
                Modified.Add(card);
            }

            // Disable immunity
            if (UserDict[ActiveCharacter].Player.IsImmune)
                UserDict[ActiveCharacter].Player.IsImmune = false;

            UserDict[ActiveCharacter].Write(new ResponseMessage(new NewTurnResponse(CurrentRound, ActiveCharacter, UserDict[ActiveCharacter].Player.Mana, drawnCard, Modified, damage)));
            foreach (CharacterEnum target in CharacterManager.Instance.Others(ActiveCharacter))
                UserDict[target].Write(new ResponseMessage(new NewTurnResponse(CurrentRound, ActiveCharacter, UserDict[ActiveCharacter].Player.Mana, drawnCard != null, Modified, damage)));
        }

        public void StartMainTurn()
        {
            Log("Start main turn for " + ActiveCharacter.ToString());
            IsMainTurn = true;
            foreach (CharacterEnum target in UserDict.Keys.ToList())
                UserDict[target].Write(new ResponseMessage(new MainTurnResponse()));
        }

        public void Shutdown()
        {
            foreach (User u in UserDict.Values)
                u.Write(new ShutdownMessage());
        }

        private void RefreshTable()
        {
            bool graveyardChanged = false;
            foreach (User u in UserDict.Values)
            {
                if (u.Player.Inner.Where(c => c.Health <= 0).Any())
                {
                    foreach(CreatureCard card in u.Player.Inner.Where(c => c.Health <= 0)) {
                        if (card.Name != "Calf" && card.Name != "Bear" && card.Name != "Imperial Toucan")
                            Graveyard.Add(card);
                    }
                    //Graveyard.AddRange(u.Player.Inner.Where(c => c.Health <= 0));
                    u.Player.Inner.RemoveAll(c => c.Health <= 0);
                    graveyardChanged = true;
                }
                if (u.Player.Outer.Where(c => c.Health <= 0).Any())
                {
                    foreach (CreatureCard card in u.Player.Outer.Where(c => c.Health <= 0)) {
                        if (card.Name != "Calf" && card.Name != "Bear" && card.Name != "Imperial Toucan")
                            Graveyard.Add(card);
                    }
                    //Graveyard.AddRange(u.Player.Outer.Where(c => c.Health <= 0));
                    u.Player.Outer.RemoveAll(c => c.Health <= 0);
                    graveyardChanged = true;
                }
            }

            Graveyard.RemoveAll(c => c.IsCloned);

            if (graveyardChanged)
                foreach (CharacterEnum c in UserDict.Keys)
                    UserDict[c].Write(new ResponseMessage(new GraveyardChangedResponse(Graveyard.Count)));
        }

        public void SendResponse(CharacterEnum Dest, Response response)
        {
            if (Dest == CharacterEnum.AMARU)
                return;
            UserDict[Dest].Write(new ResponseMessage(response));
        }

        public Player GetPlayer(CharacterEnum character)
        {
            return UserDict[character].Player;
        }

        public void HandlePlayerMessage(Message mex)
        {
            if (mex is null)
                return;
            ActionMessage aMex = (ActionMessage)mex;
            try
            {
                aMex.Action.Visit(this.ValidationVisitor);
                aMex.Action.Visit(this.ExecutionVisitor);
                RefreshTable();
            }
            catch (Exception e)
            {
                //LogError("Invalid action attempted");
                if (ActiveCharacter != CharacterEnum.AMARU)
                    LogException(e);

            }
        }

        public CharacterEnum NextTurn()
        {

            this._currentIndex = (_currentIndex == (_turnList.Count - 1)) ? 0 : _currentIndex+1;
            if (_currentIndex == 0)
                CurrentRound++;
            this.ActiveCharacter = _turnList[_currentIndex];
            this.UserDict[this.ActiveCharacter].Player.ResetManaCount();
            //Log("New Player: " + ActiveCharacter.ToString());
            //Log("Current index: " + _currentIndex);
            
            return ActiveCharacter;
        }

        /// <summary>
        /// Handles a player death
        /// Takes care of notifying everyone
        /// </summary>
        /// <param name="killer"></param>
        /// <param name="deadChar"></param>
        public void KillPlayer(CharacterEnum killer, CharacterEnum deadChar)
        {     
            List<Card> drawnCards = new List<Card>();
            if (killer != CharacterEnum.AMARU)
            {
                if (deadChar == CharacterEnum.AMARU)
                {
                    UserDict[killer].Player.IsImmune = true;
                    drawnCards.Add(UserDict[killer].Player.Draw());
                }
                drawnCards.Add(UserDict[killer].Player.Draw());
            }
            KillPlayer4Turn(deadChar);
            UserDict[ActiveCharacter].Write(new ResponseMessage(new PlayerKilledResponse(killer, deadChar, UserDict[killer].Player.IsImmune, drawnCards)));
            foreach (CharacterEnum target in CharacterManager.Instance.Others(ActiveCharacter))
                UserDict[target].Write(new ResponseMessage(new PlayerKilledResponse(killer, deadChar, UserDict[killer].Player.IsImmune, drawnCards.Count)));

            if (Simulator)
                return;

            if (_turnList.Count == 1)
                foreach (CharacterEnum target in UserDict.Keys)
                    UserDict[target].Write(new ResponseMessage(new GameFinishedResponse(_turnList[0])));
        }

        private void KillPlayer4Turn(CharacterEnum deadChar)
        {
            if (Simulator)
                return;

            // Adapt current player index
            if (_turnList.FindIndex(c => c == deadChar) < _currentIndex)
                _currentIndex--;

            // Remove player from turn
            if (deadChar == CharacterEnum.AMARU)
                _turnList.RemoveAll(c => c == CharacterEnum.AMARU); 
            else
            {
                _turnList.Remove(deadChar);
                // Handle two players left (must remove first AMARU before currentIndex)
                if (_turnList.Exists(c => c == CharacterEnum.AMARU) && _turnList.Count == 4)     
                {
                    int tempIndex = _currentIndex;
                    while (_turnList[tempIndex] != CharacterEnum.AMARU)
                        tempIndex = tempIndex == 0 ? _turnList.Count - 1 : tempIndex - 1;
                    _turnList.RemoveAt(tempIndex);
                    if (tempIndex < _currentIndex)
                        _currentIndex--;
                }
            }
        }
    }
}