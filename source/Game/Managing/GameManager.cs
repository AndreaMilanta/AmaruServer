﻿using System;
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
        public Dictionary<CharacterEnum, User> _userDict = new Dictionary<CharacterEnum, User>();
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

        /// <summary>
        /// Constructor for AI
        /// Attenzione a settare bene di chi è il turno (ActiveCharacter) -- ADESSO è CharacterEnum.AMARU
        /// </summary>
        /// <param name="players">!!!Lista già clonata ricorsivamente!!!!</param>
        /// <param name="logger">Nome del logger dell'AI (consiglio "AILogger"</param>
        public GameManager(GameManager gameManager, string logger) : base(logger)
        {

            //per ogni giocatore in generale voglio sapere:
            /*
            List<Player> playerToClone = new List<Player>();
            foreach (User user in playerToClone)
            {
                playerToClone.Add(new Player(user.Player));
            }
            foreach (Player p in playerToClone)
                _userDict.Add(p.Character, new EmptyUser(p, logger));
            */
            foreach (CreatureCard c in gameManager.Graveyard)
            {
                this.Graveyard.Add(((CreatureCard)c).clone());
            }
            this._userDict = new Dictionary<CharacterEnum, User>();
            foreach (User user in gameManager._userDict.Values)
            {
                this._userDict.Add(user.Player.Character, new EmptyUser(new Player(user.Player), "FAKELOGGER"));
            }

            this.GameHasFinished = gameManager.GameHasFinished;
            this.ValidationVisitor = new ValidationVisitor(this);
            this.ExecutionVisitor = new ExecutionVisitor(this);
            this.ActiveCharacter = CharacterEnum.AMARU;
        }

        public GameManager(int id, Dictionary<CharacterEnum, User> clientsDict) : base(AmaruConstants.GAME_PREFIX + id)
        {
            try {
                Id = id;
                this._userDict = clientsDict;

                this.ValidationVisitor = new ValidationVisitor(this);
                this.ExecutionVisitor = new ExecutionVisitor(this);
                this.ActiveCharacter = this._userDict.Keys.ToArray()[0];
                this._turnList = new List<CharacterEnum>();
                for (int i = 0; i < _userDict.Keys.ToArray().Length; i++) {
                    _turnList.Add(_userDict.Keys.ToArray()[i]);
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
                List<CharacterEnum> disadvantaged = _userDict.Keys.ToList().Where(c => c != CharacterEnum.AMARU).ToList().GetRange(AmaruConstants.NUM_PLAYER - AmaruConstants.NUM_DISADVANTAGED, AmaruConstants.NUM_DISADVANTAGED);

                // Init Players
                foreach (CharacterEnum c in _userDict.Keys)                       // Default draw
                    _userDict[c].SetPlayer(new Player(c, AmaruConstants.GAME_PREFIX + Id), this);
                _userDict.Add(CharacterEnum.AMARU, new AIUser("AI_"+AmaruConstants.GAME_PREFIX + Id));
                    _userDict[CharacterEnum.AMARU].SetPlayer(new AmaruPlayer(AmaruConstants.GAME_PREFIX + Id), this);

                // Draw cards 
                foreach (CharacterEnum c in _userDict.Keys)                       // Default draw
                    _userDict[c].Player.Draw(AmaruConstants.INITIAL_HAND_SIZE);
                foreach (CharacterEnum c in disadvantaged)                      // Extra draw for disadvantaged
                    _userDict[c].Player.Draw(AmaruConstants.INITIAL_HAND_BONUS);

                // Send GameInitMessage to Users
                foreach (CharacterEnum target in _userDict.Keys)
                {
                    Dictionary<CharacterEnum, EnemyInfo> enemies = new Dictionary<CharacterEnum, EnemyInfo>();
                    OwnInfo own = _userDict[target].Player.AsOwn;
                    foreach (CharacterEnum c in CharacterManager.Instance.Others(target))
                        enemies.Add(c, _userDict[c].Player.AsEnemy);
                    _userDict[target].Write(new GameInitMessage(enemies, own, _turnList));
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
                    HandlePlayerMessage(_userDict[ActiveCharacter].ReadSync(ServerConstants.ReadTimeout_ms));
                }
                catch (Exception e)
                {
                    LogException(e);
                    KillPlayer4Turn(ActiveCharacter);
                    _userDict.Remove(ActiveCharacter);
                    foreach (CharacterEnum target in _userDict.Keys.ToList())
                        _userDict[target].Write(new PlayerKilledMessage(ActiveCharacter, true));
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
            Card drawnCard = _userDict[ActiveCharacter].Player.Draw();
            if (drawnCard == null)                                         // Handle if deck is finished
                if (_userDict[ActiveCharacter].Player.Deck.Count == 0)
                    damage = 1;

            // Give mana
            _userDict[ActiveCharacter].Player.Mana += CurrentRound * AmaruConstants.MANA_TURN_FACTOR;

            // Add EP and execute onturnstart for each card on table
            OnTurnStartVisitor OTSVisitor = new OnTurnStartVisitor(ActiveCharacter, AmaruConstants.GAME_PREFIX + Id);
            List<Card> Modified = new List<Card>();
            foreach (CreatureCard card in _userDict[ActiveCharacter].Player.Inner)
            {
                card.Energy++;
                card.Visit(OTSVisitor, ActiveCharacter, null);
                Modified.Add(card);
            }
            foreach (CreatureCard card in _userDict[ActiveCharacter].Player.Outer)
            {
                card.Energy++;
                card.Visit(OTSVisitor, ActiveCharacter, null);
                Modified.Add(card);
            }

            // Disable immunity
            if (_userDict[ActiveCharacter].Player.IsImmune)
                _userDict[ActiveCharacter].Player.IsImmune = false;

            _userDict[ActiveCharacter].Write(new ResponseMessage(new NewTurnResponse(CurrentRound, ActiveCharacter, _userDict[ActiveCharacter].Player.Mana, drawnCard, Modified, damage)));
            foreach (CharacterEnum target in CharacterManager.Instance.Others(ActiveCharacter))
                _userDict[target].Write(new ResponseMessage(new NewTurnResponse(CurrentRound, ActiveCharacter, _userDict[ActiveCharacter].Player.Mana, drawnCard != null, Modified, damage)));
        }

        public void StartMainTurn()
        {
            Log("Start main turn for " + ActiveCharacter.ToString());
            IsMainTurn = true;
            foreach (CharacterEnum target in _userDict.Keys.ToList())
                _userDict[target].Write(new ResponseMessage(new MainTurnResponse()));
        }

        public void Shutdown()
        {
            foreach (User u in _userDict.Values)
                u.Write(new ShutdownMessage());
        }

        public void SendResponse(CharacterEnum Dest, Response response)
        {
            if (Dest == CharacterEnum.AMARU)
                return;
            _userDict[Dest].Write(new ResponseMessage(response));
        }

        public Player GetPlayer(CharacterEnum character)
        {
            return _userDict[character].Player;
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
            }
            catch (Exception e)
            {
                //LogError("Invalid action attempted");
                //LogException(e);

            }
        }

        public CharacterEnum NextTurn()
        {
            this._currentIndex = (_currentIndex == (_turnList.Count - 1)) ? 0 : _currentIndex+1;
            if (_currentIndex == 0)
                CurrentRound++;
            this.ActiveCharacter = _turnList[_currentIndex];
            this._userDict[this.ActiveCharacter].Player.ResetManaCount();
            Log("New Player: " + ActiveCharacter.ToString());
            Log("Current index: " + _currentIndex);
            
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
                    _userDict[killer].Player.IsImmune = true;
                    drawnCards.Add(_userDict[killer].Player.Draw());
                }
                drawnCards.Add(_userDict[killer].Player.Draw());
            }
            KillPlayer4Turn(deadChar);
            _userDict[ActiveCharacter].Write(new ResponseMessage(new PlayerKilledResponse(killer, deadChar, _userDict[killer].Player.IsImmune, drawnCards)));
            foreach (CharacterEnum target in CharacterManager.Instance.Others(ActiveCharacter))
                _userDict[target].Write(new ResponseMessage(new PlayerKilledResponse(killer, deadChar, _userDict[killer].Player.IsImmune, drawnCards.Count)));
            if (_turnList.Count == 1)
                foreach (CharacterEnum target in _userDict.Keys)
                    _userDict[target].Write(new ResponseMessage(new GameFinishedResponse(_turnList[0])));
        }

        private void KillPlayer4Turn(CharacterEnum deadChar)
        {
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