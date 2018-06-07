using System;
using System.Collections.Generic;
using System.Linq;

using Logging;
using AmaruCommon.Constants;
using AmaruCommon.Communication.Messages;
using AmaruCommon.GameAssets.Characters;
using AmaruCommon.GameAssets.Player;
using AmaruServer.Game.Assets;
using AmaruServer.Networking;

namespace AmaruServer.Game.Managing
{
    public class GameManager : Loggable
    {
        public readonly int Id;
        Dictionary<CharacterEnum, User> _userDict = new Dictionary<CharacterEnum, User>();

        public GameManager(int id, Dictionary<CharacterEnum, User> clientsDict) : base(AmaruConstants.GAME_PREFIX + id)
        {
            Id = id;
            this._userDict = clientsDict;

            // Get disadvantaged players
            List<CharacterEnum> disadvantaged = _userDict.Keys.ToList().GetRange(AmaruConstants.NUM_PLAYER - AmaruConstants.NUM_DISADVANTAGED, AmaruConstants.NUM_DISADVANTAGED);

            // Init Players
            foreach (CharacterEnum c in _userDict.Keys)                       // Default draw
                _userDict[c].SetPlayer(new Player(c));

            // Draw cards 
            foreach (CharacterEnum c in _userDict.Keys)                       // Default draw
                _userDict[c].Player.Draw(AmaruConstants.INITIAL_HAND_SIZE);
            foreach (CharacterEnum c in disadvantaged)                      // Extra draw for disadvantaged
                _userDict[c].Player.Draw(AmaruConstants.INITIAL_HAND_BONUS);

            // Send GameInitMessage to Users
            foreach (CharacterEnum target in _userDict.Keys)
            {
                Dictionary<CharacterEnum,EnemyInfo> enemies = new Dictionary<CharacterEnum, EnemyInfo>();
                OwnInfo own = _userDict[target].Player.AsOwn;
                foreach (CharacterEnum c in CharacterManager.Instance.Others(target))
                    enemies.Add(c, _userDict[c].Player.AsEnemy);
               _userDict[target].Write(new GameInitMessage(enemies, own));
            }
        }

        public void Shutdown()
        {
            foreach(User u in _userDict.Values)
                u.Write(new ShutdownMessage());
        }
    }
}
