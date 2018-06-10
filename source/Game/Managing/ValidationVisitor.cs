using System;

using AmaruCommon.Exceptions;
using AmaruCommon.Actions;

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

        public void Visit(PlayerAction action)
        {
            throw new NotImplementedException();
        }
    }
}
