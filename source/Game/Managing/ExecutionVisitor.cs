using System;

using AmaruCommon.Constants;
using AmaruCommon.Actions;
using AmaruCommon.GameAssets.Player;
using AmaruCommon.GameAssets.Cards;
using AmaruCommon.GameAssets.Cards.Properties.Abilities;
using AmaruCommon.GameAssets.Cards.Properties.Effects;

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

        public void Visit(AttackPlayerAction action)
        {
            Player target = GameManager.GetPlayer(action.Target.Character);
            Player caller = GameManager.GetPlayer(action.Caller);
            CreatureCard playedCard = (CreatureCard)(caller.GetCardFromId(action.PlayedCardId, Place.INNER) ?? caller.GetCardFromId(action.PlayedCardId, Place.OUTER));
            playedCard.Energy -= playedCard.Attack.Cost;
            AttacksVisitor attackVisitor = new AttacksVisitor(GameManager, caller, action.Target);
            playedCard.Attack.Visit(attackVisitor);
        }
    }
}
