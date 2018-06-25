using System;
using System.Collections.Generic;
using System.Threading;

using Logging;
using AmaruCommon.Exceptions;
using AmaruCommon.Constants;
using AmaruCommon.GameAssets.Characters;
using AmaruServer.Networking;
using AmaruServer.Game.Managing;

namespace AmaruServer.Game.Tools
{
    public class GameFactory : Loggable
    {
        // Singleton Stuff 
        private static GameFactory _instance = null;
        public static GameFactory Instance { get => _instance ?? throw new ItemNotYetInitializedException("GameFactory"); }
        private GameFactory(string logger) : base(logger)
        {

        }
        public static void Init(string logger)
        {
            if (_instance != null)
                throw new ItemAlreadyInitializedException("GameFactory");
            _instance = new GameFactory(logger);
        }

        public int NextId { get; private set; } = 0;

        public GameManager StartNew(List<User> users)
        {
            // Assign user to character
            Dictionary<CharacterEnum, User> playerClientDict = new Dictionary<CharacterEnum, User>();
            List<CharacterEnum> chars = CharacterManager.Instance.RandomPlayCharList;                  //TODO: Reset ordine casuale
            //List<CharacterEnum> chars = CharacterManager.Instance.PlayCharacters;
            for (int i = 0; i < AmaruConstants.NUM_PLAYER; i++)
                playerClientDict.Add(chars[i], users[i]);
            GameManager newGame = new GameManager(NextId, playerClientDict);

            // Create and run game thread
            Thread newGameThread = new Thread(new ThreadStart(newGame.StartGame));
            newGameThread.Start();

            NextId++;
            return newGame;
        }
    }
}
