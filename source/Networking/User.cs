using System;
using System.Threading;

using AmaruCommon.Messages;
using AmaruCommon.Exceptions;
using AmaruServer.Constants;

namespace AmaruServer.Networking
{
    class User
    {
        private ServerClient _client = null;

        public int Ranking { get; private set; }
        public int Points { get; private set; }
        public string Username { get; private set; }

        public User(ServerClient client, LoginMessage mex)
        {
            _client = client;
            Validate(mex.Username, mex.Password);
            this.Username = mex.Username;
            LoadData();
            Write(new LoginReplyMessage(true, Ranking, Points));
        }

        /// <summary>
        /// Send Message to Client
        /// </summary>
        /// <param name="mex"></param>
        public void Write(AmaruMessage mex)
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
            //_client.Write(new LoginReplyMessage(false));
            //throw new InvalidUserCredentialsException(username);
        }

        /// <summary>
        ///  Loads user data from DB
        /// AS OF NOW random Ranking and points
        /// <throws>UserNotFoundException</throws>
        /// </summary>
        public void LoadData()
        {
            Random rnd = new Random();
            this.Ranking = rnd.Next(UserConstants.maxRanking);
            this.Points = rnd.Next(UserConstants.maxPoints);
        }
    }
}
