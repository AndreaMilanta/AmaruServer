using System;

using AmaruCommon.Exceptions;
using AmaruCommon.Constants;
using AmaruCommon.Actions;
using AmaruCommon.GameAssets.Player;
using AmaruCommon.GameAssets.Cards;

namespace AmaruServer.Game.Managing
{
    /// <summary>
    /// Visitor to check validity of action
    /// </summary>
    /// <exception cref="InvalidActionException">thrown when action is not valid</exception>
    public class ValidationVisitor : IActionVisitor
    {
        private GameManager GameManager { get; set; }

        public ValidationVisitor(GameManager gameManager)
        {
            this.GameManager = gameManager;
        }

        // Attack from card to player
        public void Visit(AttackPlayerAction action)
        {
            // Check target player is alive
            Player target = this.GameManager.GetPlayer(action.Target.Character);
            if (!target.IsAlive)
                throw new TargetPlayerIsDeadException();

            // Check caller player is alive and it is its turn
            Player caller = this.GameManager.GetPlayer(action.Caller);
            if (!caller.IsAlive || GameManager.ActiveCharacter != action.Caller)
                throw new CallerCannotPlayException();

            // Check playedCard can attack and has enough EP
            if (caller.GetCardFromId(action.PlayedCardId, Place.OUTER) == null &&
               (caller.GetCardFromId(action.PlayedCardId, Place.INNER) == null && caller.InnerAttackAllowed))
                throw new InvalidCardLocationException();
            CreatureCard card = (CreatureCard)(caller.GetCardFromId(action.PlayedCardId, Place.OUTER) ?? caller.GetCardFromId(action.PlayedCardId, Place.INNER));
            if (card.Energy < card.Attack.Cost)
                throw new NotEnoughEPAvailableException();

            // Check that target player is not immune and has no Shield Up
            if (target.IsImmune || target.IsShieldUpProtected)
                throw new PlayerCannotBeTargetedException();
        }
    }
}
