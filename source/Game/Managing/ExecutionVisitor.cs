using System;

using AmaruCommon.Constants;
using AmaruCommon.Actions;
using AmaruCommon.GameAssets.Player;
using AmaruCommon.GameAssets.Cards;
using AmaruCommon.GameAssets.Cards.Properties.Abilities;
using AmaruCommon.GameAssets.Characters;
using System.Linq;
using AmaruCommon.Communication.Messages;
using AmaruCommon.Responses;

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
            target.Health -= playedCard.Attack.Visit(attackVisitor);
        }

        public void Visit(MoveCreatureAction action)
        {
            Player p = GameManager.GetPlayer(action.Caller);
            CreatureCard creature = p.MoveACreatureFromPlace(action.PlayedCardId, action.Place);
            foreach (CharacterEnum target in GameManager._userDict.Keys.ToList())
                GameManager._userDict[target].Write(new ResponseMessage(new MoveCreatureResponse(action.Caller,creature,action.Place,action.TablePos)));
        }

        public void Visit(PlayACreatureFromHandAction action)
        {
            Player p = GameManager.GetPlayer(action.Caller);
            CreatureCard creature = p.PlayACreatureFromHand(action.PlayedCardId, action.Place);
            foreach (CharacterEnum target in GameManager._userDict.Keys.ToList())
                GameManager._userDict[target].Write(new ResponseMessage(new PlayACreatureResponse(action.Caller, creature, action.Place, action.TablePos)));
        }

        public void Visit(PlayASpellFromHandAction action)
        {
            Player p = GameManager.GetPlayer(action.Caller);
            SpellCard spell = p.PlayASpellFromHand(action.PlayedCardId, action.Targets);
            foreach (CharacterEnum target in GameManager._userDict.Keys.ToList())
                GameManager._userDict[target].Write(new ResponseMessage(new PlayASpellResponse(action.Caller,spell,action.Targets)));
        }

        public void Visit(EndTurnAction action)
        {
            if (action.IsMainTurn) {
                GameManager.NextTurn();
                GameManager.StartTurn();
            }
            else
                GameManager.StartMainTurn();
        }
    }
}
