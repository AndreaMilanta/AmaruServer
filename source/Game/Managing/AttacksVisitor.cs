using System;

using AmaruCommon.Actions.Targets;
using AmaruCommon.GameAssets.Cards.Properties.Attacks;
using AmaruCommon.GameAssets.Cards.Properties;
using AmaruCommon.GameAssets.Player;
using AmaruCommon.GameAssets.Characters;
using AmaruCommon.GameAssets.Cards.Properties.Effects;

namespace AmaruServer.Game.Managing
{
    public class AttacksVisitor : IPropertyVisitor
    {
        private GameManager GameManager { get; set; }
        private Player Caller { get; set; }
        private PlayerTarget PlayerTarget { get; set; } = null;
        private CardTarget CardTarget { get; set; } = null;

        /// <summary>
        /// Handles attack procedures.
        /// Does NOT take care of reducing card EP
        /// </summary>
        /// <param name="gameManager"></param>
        /// <param name="caller"></param>
        /// <param name="target"></param>
        public AttacksVisitor(GameManager gameManager, Player caller, Target target)
        {
            this.Caller = caller;
            if (target is CardTarget)
                CardTarget = (CardTarget)target;
            if (target is PlayerTarget)
                PlayerTarget = (PlayerTarget)target;
        }
        public int Visit(SimpleAttack attack)
        {
            return attack.Power;
        }

        public int Visit(AttackFromInnerEffect  effect)
        {
            throw new NotImplementedException();
        }

        public int Visit(ImperiaAttack attack)
        {
            throw new NotImplementedException();
        }

        int IPropertyVisitor.Visit(SimpleAttack attack)
        {
            throw new NotImplementedException();
        }

        int IPropertyVisitor.Visit(AttackFromInnerEffect effect)
        {
            throw new NotImplementedException();
        }

        int IPropertyVisitor.Visit(ImperiaAttack attack)
        {
            throw new NotImplementedException();
        }
    }
}
