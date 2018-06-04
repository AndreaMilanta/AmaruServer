using System;

using AmaruServer.Constants;
using AmaruServer.Networking.Communication;
using AmaruServer.Exceptions;
using AmaruServer.Logging;

namespace AmaruServer.Networking
{
    class User
    {
        private ClientTCP _client;
        private int _ranking;
        private int _points;
        private string _username;

        public int Ranking { get => _ranking; }
        public int Points { get => _points; }
        public string Name { get => _username; }

        public User(ClientTCP cl)
        {
            _client = cl;

        }

        /// <summary>
        /// Send Message to Client
        /// </summary>
        /// <param name="mex"></param>
        public void Write(Message mex)
        {
            _client.Write(mex);
        }

        /// <summary>
        /// checks if user exists
        /// </summary>
        /// <returns></returns>
        private bool Validate(string username, string password)
        {
            return true;
        }

        /// <summary>
        ///  Loads user data from DB
        /// AS OF NOW random Ranking and points
        /// <throws>UserNotFoundException</throws>
        /// </summary>
        public void LoadData()
        {
            if (!this.Validate(null, null))
                throw new UserNotFoundException();
            Random rnd = new Random();
            _ranking = rnd.Next(UserConstants.maxRanking);
            _points = rnd.Next(UserConstants.maxPoints);
            LoggerManager.NetworkLogger.Log("User " + _username + " logged in");
        }
    }
}
