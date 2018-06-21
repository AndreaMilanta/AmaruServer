using System;

using AmaruCommon.Exceptions;
using AmaruCommon.Constants;
using AmaruCommon.Actions;
using AmaruCommon.GameAssets.Players;
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

            // Check caller player is alive and it is its main turn
            Player caller = this.GameManager.GetPlayer(action.Caller);
            if (!caller.IsAlive || GameManager.ActiveCharacter != action.Caller || !GameManager.IsMainTurn)
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

        public void Visit(MoveCreatureAction action)
        {
            // Check caller player is alive and it is not its main turn
            Player caller = this.GameManager.GetPlayer(action.Caller);
            if (!caller.IsAlive || GameManager.ActiveCharacter != action.Caller || GameManager.IsMainTurn)
                throw new CallerCannotPlayException();

            // Check card exists and is in proper position
            Place from;
            if (action.Place == Place.INNER)
                from = Place.OUTER;
            else if (action.Place == Place.OUTER)
                from = Place.INNER;
            else
                throw new InvalidCardLocationException();
            if(caller.GetCardFromId(action.PlayedCardId, from) == null)
                throw new CardNotAvailableException();

            // Check that target place has enough room
            if ((action.Place == Place.INNER && caller.Inner.Count >= AmaruConstants.INNER_MAX_SIZE) ||
                (action.Place == Place.OUTER && caller.Outer.Count >= AmaruConstants.OUTER_MAX_SIZE))
                throw new TargetPlaceFullException(action.Place);

        }

        public void Visit(PlayACreatureFromHandAction action)
        {


        }

        public void Visit(PlayASpellFromHandAction action)
        {
            /// Check validity
        }

        public void Visit(EndTurnAction action)
        {
            
        }

        public void Visit(AttackCreatureAction attackCreatureAction)
        {
            throw new NotImplementedException();
        }
    }
}
