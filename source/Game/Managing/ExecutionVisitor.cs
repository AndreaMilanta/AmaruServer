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
using AmaruCommon.Actions.Targets;

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

            foreach (CharacterEnum dest in GameManager.UserDict.Keys.ToList())
                GameManager.UserDict[dest].Write(new ResponseMessage(new AttackPlayerResponse(action.Caller, action.Target.Character, playedCard, target.Health)));

            foreach (KeyValuePair<CharacterEnum, Response> kvp in attackVisitor.SuccessiveResponse) {
                GameManager.UserDict[kvp.Key].Write(new ResponseMessage(kvp.Value));
            }

            if (!target.IsAlive)
                GameManager.KillPlayer(caller.Character, target.Character);
        }

        public override void Visit(MoveCreatureAction action)
        {
            Log("card moved to " + action.Place.ToString());
            Player p = GameManager.GetPlayer(action.Caller);
            CreatureCard creature = p.MoveACreatureFromPlace(action.PlayedCardId, action.Place);
            foreach (CharacterEnum target in GameManager.UserDict.Keys.ToList())
                GameManager.UserDict[target].Write(new ResponseMessage(new MoveCreatureResponse(action.Caller, creature, action.Place, action.TablePos)));
        }

        public override void Visit(PlayACreatureFromHandAction action)
        {
            OnCardPlayedVisitor visitor = new OnCardPlayedVisitor(GameManager);
            Player p = GameManager.GetPlayer(action.Caller);
            CreatureCard creature = p.PlayACreatureFromHand(action.PlayedCardId, action.Place);
            creature.Visit(visitor, p.Character);
            foreach (CharacterEnum target in GameManager.UserDict.Keys.ToList())
                GameManager.UserDict[target].Write(new ResponseMessage(new PlayACreatureResponse(action.Caller, creature, action.Place, action.TablePos)));
        }

        public override void Visit(PlayASpellFromHandAction action)
        {
            OnCardPlayedVisitor visitor = new OnCardPlayedVisitor(GameManager);
            Player p = GameManager.GetPlayer(action.Caller);
            SpellCard spell = p.PlayASpellFromHand(action.PlayedCardId);
            visitor.Targets = action.Targets;
            p.PlayedSpell.Add(spell);
            spell.Visit(visitor, p.Character);
            foreach (CharacterEnum target in GameManager.UserDict.Keys.ToList())
                GameManager.UserDict[target].Write(new ResponseMessage(new PlayASpellResponse(action.Caller,spell,action.Targets)));
            foreach (KeyValuePair<CharacterEnum,Response> kvp in visitor.SuccessiveResponse)
                GameManager.UserDict[kvp.Key].Write(new ResponseMessage(kvp.Value));
            // visitor must take care of players which he kills
        }

        public override void Visit(EndTurnAction action)
        {
            if (action.IsMainTurn) {
                //GameManager.UserDict[GameManager.ActiveCharacter].Player.PlayedSpell = new List<SpellCard>();

                GameManager.NextTurn();
                GameManager.StartTurn();
            }
            else
                GameManager.StartMainTurn();
        }

        public override void Visit(AttackCreatureAction action)
        {
            Player target = GameManager.GetPlayer(action.Target.Character);
            Player caller = GameManager.GetPlayer(action.Caller);
            CreatureCard attackedCard = (CreatureCard)(target.GetCardFromId(action.Target.CardId, Place.INNER) ?? target.GetCardFromId(action.Target.CardId, Place.OUTER));
            CreatureCard playedCard = (CreatureCard)(caller.GetCardFromId(action.PlayedCardId, Place.INNER) ?? caller.GetCardFromId(action.PlayedCardId, Place.OUTER));
            playedCard.Energy -= playedCard.Attack.Cost;
            AttacksVisitor attackVisitor = new AttacksVisitor(GameManager, caller, action.Target, playedCard);
            int attackPower = playedCard.Attack.Visit(attackVisitor);
            attackedCard.Health -= attackPower;

            //handle death
            if (attackedCard.Health <= 0) {
                (target.GetCardFromId(attackedCard.Id, Place.INNER) == null ? target.Outer : target.Inner).Remove(attackedCard);

                GameManager.Graveyard.Add(attackedCard);
            }

            foreach (CharacterEnum dest in GameManager.UserDict.Keys.ToList())
                GameManager.UserDict[dest].Write(new ResponseMessage(new AttackCreatureResponse(action.Caller, action.Target.Character, playedCard, attackedCard)));

            foreach (KeyValuePair<CharacterEnum, Response> kvp in attackVisitor.SuccessiveResponse) {
                GameManager.UserDict[kvp.Key].Write(new ResponseMessage(kvp.Value));
            }

            if (!target.IsAlive)
                GameManager.KillPlayer(caller.Character, target.Character);
        }

        public override void Visit(UseAbilityAction action)
        {
            Player caller = GameManager.GetPlayer(action.Caller);
            CreatureCard playedCard = (CreatureCard)(caller.GetCardFromId(action.PlayedCardId, Place.INNER) ?? caller.GetCardFromId(action.PlayedCardId, Place.OUTER));
            playedCard.Energy -= playedCard.Ability.Cost;
            AttacksVisitor attackVisitor = new AttacksVisitor(GameManager, caller, null, playedCard);
            if (action.Targets == null)
                playedCard.Visit(attackVisitor, caller.Character, playedCard.Ability);
            else
            {
                attackVisitor.Targets = action.Targets;
                playedCard.Visit(attackVisitor, caller.Character, playedCard.Ability);
            }
            foreach (KeyValuePair<CharacterEnum, Response> kvp in attackVisitor.SuccessiveResponse) {
                Log("Player " + kvp.Key.ToString() + " recieved a successive response");
                GameManager.UserDict[kvp.Key].Write(new ResponseMessage(kvp.Value));
            }
            foreach (CharacterEnum c in GameManager.UserDict.Keys)
                GameManager.UserDict[c].Write(new ResponseMessage(new CardsModifiedResponse(playedCard)));
            // visitor must take care of players which he kills
        }
    }
}
