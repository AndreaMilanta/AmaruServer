using System;

using AmaruCommon.Constants;
using AmaruCommon.Actions;
using AmaruCommon.GameAssets.Players;
using AmaruCommon.GameAssets.Cards;
using AmaruCommon.GameAssets.Cards.Properties.Abilities;
using AmaruCommon.GameAssets.Characters;
using System.Linq;
using AmaruCommon.Communication.Messages;
using AmaruCommon.Responses;
using System.Collections.Generic;

namespace AmaruServer.Game.Managing
{
    /// <summary>
    /// Visitor for execution of actions
    /// Also takes care of sending responses
    /// </summary>
    public class ExecutionVisitor : ActionVisitor
    {
        private GameManager GameManager { get; set; }

        public ExecutionVisitor(GameManager gameManager) : base(AmaruConstants.GAME_PREFIX + gameManager.Id)
        {
            this.GameManager = gameManager;
        }

        public override void Visit(AttackPlayerAction action)
        {
            Player target = GameManager.GetPlayer(action.Target.Character);
            Player caller = GameManager.GetPlayer(action.Caller);
            CreatureCard playedCard = (CreatureCard)(caller.GetCardFromId(action.PlayedCardId, Place.INNER) ?? caller.GetCardFromId(action.PlayedCardId, Place.OUTER));
            playedCard.Energy -= playedCard.Attack.Cost;
            AttacksVisitor attackVisitor = new AttacksVisitor(GameManager, caller, action.Target, playedCard);
            int attackPower = playedCard.Attack.Visit(attackVisitor);
            target.Health -= attackPower;

            foreach (CharacterEnum dest in GameManager._userDict.Keys.ToList())
                GameManager._userDict[dest].Write(new ResponseMessage(new AttackPlayerResponse(action.Caller, action.Target.Character, playedCard, target.Health)));

            foreach (KeyValuePair<CharacterEnum, Response> kvp in attackVisitor.SuccessiveResponse)
                GameManager._userDict[kvp.Key].Write(new ResponseMessage(kvp.Value));
        }

        public override void Visit(MoveCreatureAction action)
        {
            Log("card moved to " + action.Place.ToString());
            Player p = GameManager.GetPlayer(action.Caller);
            CreatureCard creature = p.MoveACreatureFromPlace(action.PlayedCardId, action.Place);
            foreach (CharacterEnum target in GameManager._userDict.Keys.ToList())
                GameManager._userDict[target].Write(new ResponseMessage(new MoveCreatureResponse(action.Caller, creature, action.Place, action.TablePos)));
        }

        public override void Visit(PlayACreatureFromHandAction action)
        {
            OnCardPlayedVisitor visitor = new OnCardPlayedVisitor(GameManager);
            Player p = GameManager.GetPlayer(action.Caller);
            CreatureCard creature = p.PlayACreatureFromHand(action.PlayedCardId, action.Place);
            creature.Visit(visitor, p);
            foreach (CharacterEnum target in GameManager._userDict.Keys.ToList())
                GameManager._userDict[target].Write(new ResponseMessage(new PlayACreatureResponse(action.Caller, creature, action.Place, action.TablePos)));
        }

        public override void Visit(PlayASpellFromHandAction action)
        {
            OnCardPlayedVisitor visitor = new OnCardPlayedVisitor(GameManager);
            Player p = GameManager.GetPlayer(action.Caller);
            SpellCard spell = p.PlayASpellFromHand(action.PlayedCardId);
            visitor.Targets = action.Targets;
            spell.Visit(visitor, p);
            foreach (CharacterEnum target in GameManager._userDict.Keys.ToList())
                GameManager._userDict[target].Write(new ResponseMessage(new PlayASpellResponse(action.Caller,spell,action.Targets)));
            foreach (KeyValuePair<CharacterEnum,Response> kvp in visitor.SuccessiveResponse)
                GameManager._userDict[kvp.Key].Write(new ResponseMessage(kvp.Value));
        }

        public override void Visit(EndTurnAction action)
        {
            if (action.IsMainTurn) {
                GameManager.NextTurn();
                GameManager.StartTurn();
            }
            else
                GameManager.StartMainTurn();
        }

        public override void Visit(AttackCreatureAction action)
        {
            throw new NotImplementedException();
        }
    }
}
