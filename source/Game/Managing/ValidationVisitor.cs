using System;

using AmaruCommon.Exceptions;
using AmaruCommon.Constants;
using AmaruCommon.Actions;
using AmaruCommon.GameAssets.Players;
using AmaruCommon.GameAssets.Cards;
using AmaruCommon.Actions.Targets;
using System.Collections.Generic;
using AmaruCommon.GameAssets.Cards.Properties.SpellAbilities;
using AmaruCommon.GameAssets.Cards.Properties.CreatureEffects;
using AmaruCommon.GameAssets.Characters;
using AmaruServer.Networking;

namespace AmaruServer.Game.Managing
{
    /// <summary>
    /// Visitor to check validity of action
    /// </summary>
    /// <exception cref="InvalidActionException">thrown when action is not valid</exception>
    public class ValidationVisitor : ActionVisitor
    {
        private GameManager GameManager { get; set; }

        public ValidationVisitor(GameManager gameManager) : base(AmaruConstants.GAME_PREFIX + gameManager.Id)
        {
            this.GameManager = gameManager;
        }

        // Attack from card to player
        public override void Visit(AttackPlayerAction action)
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

        public override void Visit(MoveCreatureAction action)
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

        public override void Visit(PlayACreatureFromHandAction action)
        {
            // Check caller player is alive and it is its main turn
            Player caller = this.GameManager.GetPlayer(action.Caller);
            if (!caller.IsAlive || GameManager.ActiveCharacter != action.Caller || !GameManager.IsMainTurn)
                throw new CallerCannotPlayException();

            //Check if caller player has enough CPs to play the card
            Card cardPlaying = caller.GetCardFromId(action.PlayedCardId, Place.HAND);
            if (cardPlaying.Cost > caller.Mana)
            {
                throw new NotEnoughManaAvailableException();
            }

            //check if the Card is a Creature
            if (!(cardPlaying is CreatureCard))
            {
                throw new InvalidCardTypeException();
            }

            //Check that target place has enough room
            if ((action.Place == Place.INNER && caller.Inner.Count >= AmaruConstants.INNER_MAX_SIZE) ||
                (action.Place == Place.OUTER && caller.Outer.Count >= AmaruConstants.OUTER_MAX_SIZE))
                throw new TargetPlaceFullException(action.Place);
        }

        public override void Visit(PlayASpellFromHandAction action)
        {
            // Ignore check if caller not AMARU
            // TODO: Remove
            if (action.Caller != CharacterEnum.AMARU)
                return;
            
            // Check caller player is alive and it is its main turn
            Player caller = this.GameManager.GetPlayer(action.Caller);
            if (!caller.IsAlive || GameManager.ActiveCharacter != action.Caller || !GameManager.IsMainTurn)
                throw new CallerCannotPlayException();

            //Check if caller player has enough CPs to play the card
            Card cardPlaying = caller.GetCardFromId(action.PlayedCardId, Place.HAND);

            if (cardPlaying.Cost > caller.Mana)
            {
                throw new NotEnoughManaAvailableException();
            }

            //check if the Card is a Creature
            if (!(cardPlaying is SpellCard))
            {
                throw new InvalidCardTypeException();
            }

            //Check if target is alive, if the spell has a target or more than one target
            List<Target> target = action.Targets;
            SpellAbility effect = ((SpellCard)cardPlaying).Effect;
            int numTarget = effect.NumTarget;
            KindOfTarget acceptableTypeOfTarget = effect.kindOfTarget;

            if (target is null)
            {
                return;
            }

            if (numTarget !=0 ||target.Count > numTarget) {
                //Check targets are not immune, and that the right number of target has been chosen. BUT it depends on the card!ù
                throw new InvalidTargetException();
            }

            foreach (Target t in target)
            {
                if (t is PlayerTarget && acceptableTypeOfTarget != KindOfTarget.PLAYER && acceptableTypeOfTarget != KindOfTarget.MIXED)
                {
                    throw new InvalidTargetException();
                }
                if( t is CardTarget && acceptableTypeOfTarget != KindOfTarget.MIXED && acceptableTypeOfTarget != KindOfTarget.CREATURE)
                {
                    throw new InvalidTargetException();
                }
                if (t is PlayerTarget  && GameManager.UserDict[((PlayerTarget)t).Character].Player.IsImmune)
                {
                    throw new InvalidTargetException();
                }
                if (t is CardTarget) 
                {
                    CardTarget cardTarget = (CardTarget)t;
                    Card cardOuter= GameManager.UserDict[((CardTarget)t).Character].Player.GetCardFromId(cardTarget.CardId, Place.OUTER);
                    Card cardInner = GameManager.UserDict[((CardTarget)t).Character].Player.GetCardFromId(cardTarget.CardId, Place.INNER);
                    if (cardOuter != null && cardOuter is CreatureCard)
                    {
                        if (((CreatureCard) cardOuter).creatureEffect is ImmunityCreatureEffect)
                        {
                            throw new InvalidTargetException();
                        }
                    }
                    if (cardInner != null && cardInner is CreatureCard)
                    {
                        if (((CreatureCard)cardInner).creatureEffect is ImmunityCreatureEffect)
                        {
                            throw new InvalidTargetException();
                        }
                    }
                }
            }//*/
        }

        public override void Visit(EndTurnAction action)
        {
            
        }

        public override void Visit(AttackCreatureAction action)
        {

            Player caller = this.GameManager.GetPlayer(action.Caller);

            // Check caller player is alive and it is its main turn
            if (!caller.IsAlive || GameManager.ActiveCharacter != action.Caller || !GameManager.IsMainTurn)
                throw new CallerCannotPlayException();

            // Check playedCard can attack and has enough EP
            if (caller.GetCardFromId(action.PlayedCardId, Place.OUTER) == null &&
               (caller.GetCardFromId(action.PlayedCardId, Place.INNER) == null && caller.InnerAttackAllowed))
                throw new InvalidCardLocationException();
            CreatureCard card = (CreatureCard)(caller.GetCardFromId(action.PlayedCardId, Place.OUTER) ?? caller.GetCardFromId(action.PlayedCardId, Place.INNER));
            if (card.Energy < card.Attack.Cost)
                throw new NotEnoughEPAvailableException();

            // Looking that the creature is not protected
            foreach (User p in GameManager.UserDict.Values)
            {
                if (p.Player.GetCardFromId(action.Target.CardId, Place.INNER) != null)
                {
                    if (p.Player.IsShieldMaidenProtected)
                    {
                        throw new InvalidCardLocationException();
                    }
                }
            }


        }

        public override void Visit(UseAbilityAction action)
        {
            Player caller = this.GameManager.GetPlayer(action.Caller);

            if (!caller.IsAlive || GameManager.ActiveCharacter != action.Caller || !GameManager.IsMainTurn)
                throw new CallerCannotPlayException();

            CreatureCard card = (CreatureCard)(caller.GetCardFromId(action.PlayedCardId, Place.OUTER) ?? caller.GetCardFromId(action.PlayedCardId, Place.INNER));

            if (card.Energy < card.Ability.Cost)
                throw new NotEnoughEPAvailableException();
        }
    }
}
