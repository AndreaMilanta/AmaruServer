using System;

using AmaruCommon.Actions;

namespace AmaruServer.Game.Managing
{
    /// <summary>
    /// Visitor for execution of actions
    /// Also takes care of sending responses
    /// </summary>
    public class ExecutionVisitor : IActionVisitor
    {
        private GameManager GameManager { get; set; }

        public ExecutionVisitor(GameManager gameManager)
        {
            this.GameManager = gameManager;
        }

        public void Visit(PlayerAction action)
        {
            throw new NotImplementedException();
        }
    }
}
