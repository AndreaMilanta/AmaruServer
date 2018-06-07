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
        List<User> waitingRoom = new List<User>();      //Dictionary mapping ranks with users
        private GameFactory gameFactory = null;

        private static ConnectionManager _instance = null;
        public static ConnectionManager Instance { get => _instance ?? new ConnectionManager(ServerConstants.ConnMngLogger); }

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

        public void NewLogin(User newUser)
        {
            try
            {
                lock (waitingRoom)
                {

                    waitingRoom.Add(newUser);
                    Log("user " + newUser.Username + " added to waiting room");
                    Reorder();
                    List<User> players = GetMatch(newUser);
                    if (players != null)
                    {
                        GameManager newGame = gameFactory.StartNew(players);
                        foreach (User u in players)
                        {
                            waitingRoom.Remove(u);
                        }
                        Log("Game " + newGame.Id + " has started");
                    }
                }
            }
            catch(InvalidUserCredentialsException e)
            {
                LogException(e);
            } 
        }

        /// <summary>
        /// Reorders users according to ranking
        /// </summary>
        private void Reorder()
        {
            waitingRoom = waitingRoom.OrderBy(u => u.Ranking).ToList();
        }

        private List<User> GetMatch(User newUser)
        {
            if (waitingRoom.Count < AmaruConstants.NUM_PLAYER)
                return null;
            int index = waitingRoom.FindIndex(u => u == newUser);
            for (int i = 0; i < AmaruConstants.NUM_PLAYER; i++)
                if (index >= i && index < waitingRoom.Count - AmaruConstants.NUM_PLAYER + i)
                    if (waitingRoom[index - i + AmaruConstants.NUM_PLAYER - 1].Ranking - waitingRoom[index - i].Ranking < UserConstants.maxRankDelta)
                        return waitingRoom.GetRange(index - i, AmaruConstants.NUM_PLAYER);
            return null;
        }
    }
}
