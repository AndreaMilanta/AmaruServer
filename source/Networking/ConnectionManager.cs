using System.Collections.Generic;
using System.Linq;

using Logging;
using AmaruCommon.Communication.Messages;
using AmaruCommon.Exceptions;
using AmaruCommon.Constants;
using AmaruServer.Constants;
using AmaruServer.Game.Tools;
using AmaruServer.Game.Managing;

namespace AmaruServer.Networking
{
    public class ConnectionManager : Loggable
    {
        private List<User> _waitingRoom = new List<User>();     // List of users waiting to join a match
        List<GameManager> _matches = new List<GameManager>();   // List of active matches

        private GameFactory gameFactory = null;

        private static ConnectionManager _instance = null;
        internal static ConnectionManager Instance { get => _instance ?? new ConnectionManager(ServerConstants.ConnMngLogger); }

        private ConnectionManager(string log) : base(log)
        {
            ConnectionManager._instance = this;
            try
            {
                GameFactory.Init(log);
                gameFactory = GameFactory.Instance;
            }
            catch (ItemAlreadyInitializedException e)
            {
                LogError(e.ToString());
            }
        }

        internal void NewLogin(User newUser)
        {
            try
            {
                lock (_waitingRoom) lock(_matches)
                {

                    _waitingRoom.Add(newUser);
                    Log("user " + newUser.Username + " added to waiting room");
                    Reorder();
                    List<User> players = GetMatch(newUser);
                    if (players != null)
                    {
                        GameManager newGame = gameFactory.StartNew(players);
                        foreach (User u in players)
                        {
                            _waitingRoom.Remove(u);
                        }
                        Log("Game " + newGame.Id + " has started");
                    }
                    Log("Could not start new game");
                }
            }
            catch(InvalidUserCredentialsException e)
            {
                LogException(e);
            } 
        }

        internal void DropUser(User user)
        {
            lock(_waitingRoom)
            {
                if (_waitingRoom.Contains(user))
                {
                    _waitingRoom.Remove(user);
                    user.Close();
                }
            }
        }

        internal void GameFinished(GameManager match)
        {
            lock (_matches)
            {
                _matches.Remove(match);
                // TODO: Procedure to update users points and ranking
            }
        }

        internal void Shutdown()
        {
            lock (_waitingRoom) lock (_matches)
            {
                foreach (User u in _waitingRoom)
                    u.Close(new ShutdownMessage());
                foreach (GameManager m in _matches)
                    m.Shutdown();
            }
        }

        /// <summary>
        /// Reorders users according to ranking
        /// </summary>
        private void Reorder()
        {
            _waitingRoom = _waitingRoom.OrderBy(u => u.Ranking).ToList();
        }

        private List<User> GetMatch(User newUser)
        {
            if (AmaruConstants.NUM_PLAYER == 1)
                return new List<User>() { newUser };
            if (_waitingRoom.Count < AmaruConstants.NUM_PLAYER)
                return null;
            int index = _waitingRoom.FindIndex(u => u == newUser);
            Log("index: " + index);
            for (int i = 0; i < AmaruConstants.NUM_PLAYER; i++)
                if (index >= i && index <= _waitingRoom.Count - AmaruConstants.NUM_PLAYER + i) {
                    Log("i is" + i);
                    if (_waitingRoom[index - i + AmaruConstants.NUM_PLAYER - 1].Ranking - _waitingRoom[index - i].Ranking < UserConstants.maxRankDelta) {
                        Log("in if interno");
                        return _waitingRoom.GetRange(index - i, AmaruConstants.NUM_PLAYER);
                    }
                }
            return null;
        }
    }
}
