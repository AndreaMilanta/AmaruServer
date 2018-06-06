using System.Collections.Generic;
using System.Linq;

using Logging;
using AmaruCommon.Messages;
using AmaruCommon.Exceptions;
using AmaruCommon.Constants;
using AmaruServer.Constants;

namespace AmaruServer.Networking
{
    public class ConnectionManager : Loggable
    {
        List<User> users = new List<User>();      //Dictionary mapping ranks with users
        private static ConnectionManager _instance = null;
        public static ConnectionManager Instance { get => _instance ?? new ConnectionManager(ServerConstants.ConnMngLogger); }

        private ConnectionManager(string log) : base(log)
        {
            ConnectionManager._instance = this;
        }

        public void NewLogin(ServerClient caller, LoginMessage mex)
        {
            try
            {
                User newUser = new User(caller, mex);
                users.Add(newUser);
                Log("user " + newUser.Username + " added to waiting room");
                Reorder();
                List<User> players = GetMatch(newUser);
                if(players != null)
                {
                    foreach (User u in players)
                        users.Remove(u);
                    // TODO 
                    // start new game
                }
            }
            catch(InvalidUserCredentialsException e)
            {
                LogException(e.ToString());
            } 
        }

        /// <summary>
        /// Reorders users according to ranking
        /// </summary>
        private void Reorder()
        {
            users = users.OrderBy(u => u.Ranking).ToList();
        }

        private List<User> GetMatch(User newUser)
        {
            if (users.Count < AmaruConstants.NUM_PLAYER)
                return null;
            int index = users.FindIndex(u => u == newUser);
            for (int i = 0; i < AmaruConstants.NUM_PLAYER; i++)
                if (index >= i && index < users.Count - AmaruConstants.NUM_PLAYER + i)
                    if (users[index - i + AmaruConstants.NUM_PLAYER - 1].Ranking - users[index - i].Ranking < UserConstants.maxRankDelta)
                        return users.GetRange(index - i, AmaruConstants.NUM_PLAYER);
            return null;
        }
    }
}
