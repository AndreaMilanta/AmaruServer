using System;

using AmaruCommon.Constants;
using AmaruCommon.GameAssets.Characters;

namespace AmaruServer.Game.Assets
{
    public class AmaruPlayer : Player
    {
        public AmaruPlayer() : base(CharacterEnum.AMARU)
        {
            this.Health = AmaruConstants.INITIAL_AMARU_HEALTH;
        }
    }
}
