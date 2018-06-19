using System;
using System.Net.Sockets;

using ClientServer.Messages;
using ClientServer.Communication;
using AmaruCommon.Constants;
using AmaruCommon.Communication.Messages;
using AmaruCommon.GameAssets.Player;
using AmaruCommon.Exceptions;
using AmaruServer.Constants;
using AmaruServer.Game.Managing;

namespace AmaruServer.Networking
{
    public class User : ClientWrapper
    {
        // User Stuff
        public int Ranking { get; private set; }
        public int Points { get; private set; }
        public string Username { get; private set; }

        // Player Stuff
        public Player Player { get; protected set; }
        protected GameManager GameManager { get; set; }

        public User(string logger) : base(logger)
        {

        }

        public User(Socket soc, string logger) : base(logger)
        {
            Client = new ClientTCP(soc, NetworkConstants.BufferSize, NetworkConstants.MaxFailures, NetworkConstants.MaxConsecutiveFailures, logger);
            Client.HandleASyncMessage += this.HandleUserMessage;

            // Begins Async reading
            try
            {
                this.ReadASync(false);
            }
            catch
            {
                this.Close();
            }
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
        private void LoadData()
        {
            Random rnd = new Random();
            this.Ranking = rnd.Next(UserConstants.maxRanking);
            this.Points = rnd.Next(UserConstants.maxPoints);
        }

        private void HandleUserMessage(Message mex)
        {
            // Logical switch on mex type
            if (mex is LoginMessage)
            {
                LoginMessage lgMex = (LoginMessage)mex;
                Validate(lgMex.Username, lgMex.Password);
                this.Username = lgMex.Username;
                LoadData();
                Write(new LoginReplyMessage(true, Ranking, Points));
                ConnectionManager.Instance.NewLogin(this);
            }
            else if (mex is ShutdownMessage)
            {
                Log("User at " + Client.Remote + " has shutdown");
                mex = (ShutdownMessage)mex;
                ConnectionManager.Instance.DropUser(this);
            }
            // Default
            else
            {
                Log("Unknown Message received (ignored)");
            }
        }

        public virtual void SetPlayer(Player player, GameManager gameManager)
        {
            this.Player = player;
            this.GameManager = gameManager;
            Client.HandleASyncMessage = GameManager.HandlePlayerMessage;
        }
    }
}
