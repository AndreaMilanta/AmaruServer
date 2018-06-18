using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

using Logging;
using AmaruCommon.Constants;
using AmaruCommon.Communication.Messages;
using AmaruCommon.GameAssets.Characters;
using AmaruCommon.GameAssets.Player;
using AmaruCommon.Responses;
using AmaruServer.Networking;
using AmaruServer.Constants;
using AmaruCommon.Exceptions;
using ClientServer.Messages;

namespace AmaruServer.Game.Managing
{
    public class GameManager : Loggable
    {
        public readonly int Id;
        Dictionary<CharacterEnum, User> _userDict = new Dictionary<CharacterEnum, User>();
        public ValidationVisitor ValidationVisitor { get; private set; }
        public ExecutionVisitor ExecutionVisitor { get; private set; }
        public bool GameHasFinished { get; set; }
        public CharacterEnum ActiveCharacter { get; private set; }                 // Player whose turn it is to play
        public int CurrentRound { get; private set; }

        // private list for simplified turn management
        private int _currentIndex;              // Index of current active player
        private List<CharacterEnum> _turnList;  // List of players in order of turn



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
                List<CharacterEnum> disadvantaged = _userDict.Keys.ToList().GetRange(AmaruConstants.NUM_PLAYER - AmaruConstants.NUM_DISADVANTAGED, AmaruConstants.NUM_DISADVANTAGED);

                // Init Players
                foreach (CharacterEnum c in _userDict.Keys)                       // Default draw
                    _userDict[c].SetPlayer(new Player(c), this);

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
                    foreach (CharacterEnum c in CharacterManager.Instance.PlayOthers(target))
                        enemies.Add(c, _userDict[c].Player.AsEnemy);
                    _userDict[target].Write(new GameInitMessage(enemies, own, _turnList));
                }
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
                Thread.Sleep(ServerConstants.SleepTime_ms);
            }
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
            // Logical switch on mex type  
            if (mex is ActionMessage)
            {
                ActionMessage aMex = (ActionMessage)mex;
                try
                {
                    aMex.Action.Visit(this.ValidationVisitor);
                    aMex.Action.Visit(this.ExecutionVisitor);
                }
                catch (InvalidActionException)
                {
                    LogError("Invalid action attempted");
                    // TODO send response to INVALID ACTION
                }
            }
            //*/
            // Default
            else
            {
                Log("Unknown Message received (ignored)");
            }
        }

        public CharacterEnum NextTurn()
        {
            this._currentIndex = (_currentIndex == _turnList.Count - 1) ? 0 : _currentIndex++;
            if (_currentIndex == 0)
                CurrentRound++;
            this.ActiveCharacter = _turnList[_currentIndex];
            this._userDict[this.ActiveCharacter].Player.ResetManaCount();
            return ActiveCharacter;
        }

        public void KillPlayer(CharacterEnum deadChar)
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
                if (_turnList.Exists(c => c == CharacterEnum.AMARU) && _turnList.Count <= 4)     
                {
                    int tempIndex = _currentIndex;
                    while (_turnList[tempIndex] == CharacterEnum.AMARU)
                        tempIndex = tempIndex == 0 ? _turnList.Count - 1 : tempIndex - 1;
                    _turnList.RemoveAt(tempIndex);
                    if (tempIndex < _currentIndex)
                        _currentIndex--;
                }
            }
        }
    }
}