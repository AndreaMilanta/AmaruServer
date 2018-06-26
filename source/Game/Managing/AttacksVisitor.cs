using System;

using AmaruCommon.Actions.Targets;
using AmaruCommon.GameAssets.Cards.Properties.Attacks;
using AmaruCommon.GameAssets.Cards.Properties.Abilities;
using AmaruCommon.GameAssets.Cards.Properties.SpellAbilities;
using AmaruCommon.GameAssets.Cards.Properties.CreatureEffects;
using AmaruCommon.GameAssets.Cards.Properties;
using AmaruCommon.GameAssets.Players;
using AmaruCommon.Constants;
using AmaruCommon.GameAssets.Cards;
using System.Collections.Generic;
using AmaruCommon.GameAssets.Characters;
using AmaruCommon.Responses;
using System.Linq;
using AmaruServer.Networking;

namespace AmaruServer.Game.Managing
{
    public class AttacksVisitor : PropertyVisitor
    {
        private GameManager GameManager { get; set; }
        private Player Caller { get; set; }
        private PlayerTarget PlayerTarget { get; set; } = null;
        private CardTarget CardTarget { get; set; } = null;
        public Target Target {
            set {
                if (value is CardTarget)
                    CardTarget = (CardTarget)value;
                if (value is PlayerTarget)
                    PlayerTarget = (PlayerTarget)value;
            }}
        public List<Target> Targets { private get; set; }
        private List<CardTarget> CardTargets { get { return Targets.Where(t => t is CardTarget).Select(t => (CardTarget)t).ToList(); } }
        private List<PlayerTarget> PlayerTargets { get { return Targets.Where(t => t is PlayerTarget).Select(t => (PlayerTarget)t).ToList(); } }

        private CreatureCard Attacker;

        private List<KeyValuePair<CharacterEnum, Response>> _successiveResponse = new List<KeyValuePair<CharacterEnum, Response>>();
        public List<KeyValuePair<CharacterEnum, Response>> SuccessiveResponse { get { List<KeyValuePair<CharacterEnum, Response>> sr = _successiveResponse;  return sr; }  set { _successiveResponse.Clear(); } }
        /// <summary>
        /// Handles attack procedures.
        /// Does NOT take care of reducing card EP
        /// </summary>
        /// <param name="gameManager"></param>
        /// <param name="caller"></param>
        /// <param name="target"></param>
        public AttacksVisitor(GameManager gameManager, Player caller, Target target, CreatureCard attacker) : base(AmaruConstants.GAME_PREFIX + gameManager.Id)
        {
            this.Owner = caller.Character;
            this.OwnerCard = attacker;
            this.Caller = caller;
            this.Target = target;
            this.GameManager = gameManager;
            this.Attacker = attacker;
        }

        private void AddResponse(CharacterEnum c, Response r)
        {
            _successiveResponse.Add(new KeyValuePair<CharacterEnum, Response>(c, r));
        }

        public override int Visit(SimpleAttack attack)
        {
            return attack.Power;
        }
        public override int Visit(ImperiaAttack attack)
        {
            return Attacker.Health + attack.BonusAttack;
        }

        public override int Visit(GainCPAttack attack)
        {
            Caller.Mana += attack.Cp;
            foreach(CharacterEnum c in GameManager.UserDict.Keys.ToList()) {
                AddResponse(c, new PlayerModifiedResponse(Caller.Character, Caller.Mana, Caller.Health));
            }
            return attack.Power;
        }

        public override int Visit(GainHPAttack attack)
        {
            if (attack.ToCreature) {
                Attacker.Health += attack.Hp;
                foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList()) {
                    AddResponse(c, new CardsModifiedResponse(Attacker));
                }
            }
            else {
                Caller.Health += attack.Hp;

                foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList()) {
                    AddResponse(c, new PlayerModifiedResponse(Caller.Character, Caller.Mana, Caller.Health));
                }
            }
            return attack.Power;
        }

        //AGGIUNGERE DRAW CARD
        public override int Visit(DrawCardAndAttack attack)
        {
            //Log("In DrawCardAndAttack called by " + Owner.ToString());
            // Draw card and prepare response
            AddResponse(Owner, new DrawCardResponse(Owner, GameManager.UserDict[Owner].Player.Draw()));
            foreach (CharacterEnum ch in CharacterManager.Instance.Others(Owner))
                AddResponse(ch, new DrawCardResponse(Owner, null));
            return attack.Power;
        }

        public override int Visit(KrumAttack attack)
        {
            return (int)Math.Ceiling((Double)(Caller.ManaSpentThisTurn/2)) + attack.BonusAttack;
        }

        public override int Visit(PoisonAttack attack)
        {
            if (CardTarget != null) { 
            CreatureCard targetCard = (CreatureCard)(GameManager.UserDict[CardTarget.Character].Player.GetCardFromId(CardTarget.CardId, Place.INNER) ?? GameManager.UserDict[CardTarget.Character].Player.GetCardFromId(CardTarget.CardId, Place.OUTER));
                if (targetCard.Health - attack.Power > 0) {
                    targetCard.PoisonDamage += attack.Power;
                    foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                        AddResponse(c, new CardsModifiedResponse(targetCard));
                }
            }
        
            return attack.Power;
        }

        public override int Visit(SalazarAttack attack)
        {
            int attackPower = 2;
            foreach(User u in GameManager.UserDict.Values) {
                foreach (CreatureCard c in u.Player.Inner)
                    attackPower += c.PoisonDamage;
                foreach (CreatureCard c in u.Player.Outer)
                    attackPower += c.PoisonDamage;
            }
            
            if (CardTarget != null) {
                CreatureCard targetCard = (CreatureCard)(GameManager.UserDict[CardTarget.Character].Player.GetCardFromId(CardTarget.CardId, Place.INNER) ?? GameManager.UserDict[CardTarget.Character].Player.GetCardFromId(CardTarget.CardId, Place.OUTER));
                if (targetCard.Health - attackPower > 0) {
                    targetCard.PoisonDamage += attackPower;
                    foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                        AddResponse(c, new CardsModifiedResponse(targetCard));
                }
            }

            return attackPower;
        }

        public override int Visit(SeribuAttack attack)
        {
            return Caller.Inner.Count + Caller.Outer.Count + attack.BonusAttack;
        }

        public override int Visit(IfKillGainHPAttack attack)
        {
            if (CardTarget != null)
            {
                CreatureCard card = (CreatureCard)(Caller.GetCardFromId(CardTarget.CardId, Place.INNER) ?? Caller.GetCardFromId(CardTarget.CardId, Place.OUTER));
                if (card.Health - attack.Power < 0)
                    ((CreatureCard)OwnerCard).Health += attack.BonusHP;
                foreach (CharacterEnum c in GameManager.UserDict.Keys)
                    AddResponse(c, new CardsModifiedResponse((CreatureCard)OwnerCard));
            }
            return attack.Power;
        }

        public override int Visit(GainHPAbility ability)
        {
            //Log(OwnerCard.Name + " used GainHPAbility");
            ((CreatureCard)OwnerCard).Health += ability.Hp;
            foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                AddResponse(c, new CardsModifiedResponse((CreatureCard)OwnerCard));
            return 0;
        }

        public override int Visit(ReturnToHandAbility ability)
        {
            foreach (CardTarget target in CardTargets)
            {
                if (Caller.Hand.Count >= AmaruConstants.HAND_MAX_SIZE)
                    return 0;
                Place origin = GameManager.UserDict[target.Character].Player.GetCardFromId(target.CardId, Place.INNER) == null ? Place.OUTER : Place.INNER;
                CreatureCard oldCard = ((CreatureCard)GameManager.GetPlayer(target.Character).GetCardFromId(target.CardId, origin));
                CreatureCard moved = (CreatureCard)oldCard.Original;

                // TODO: gestire se l'area è piena
                if (origin == Place.OUTER)
                    Caller.Outer.Remove(oldCard);
                else if (origin == Place.INNER)
                    Caller.Inner.Remove(oldCard);
                else//*/
                    return 0;
                Caller.Hand.Add(moved);
                foreach (CharacterEnum c in GameManager.UserDict.Keys)
                    AddResponse(c, new EvocationResponse(Owner, oldCard, moved, Place.HAND, deleteOriginal: true));
            }
            return 0;
        }

        public override int Visit(SalazarAbility ability)
        {
            // Handle Card targets
            List<CreatureCard> modCards = new List<CreatureCard>();
            foreach (PlayerTarget t in PlayerTargets) {
                foreach (CreatureCard card in GameManager.UserDict[t.Character].Player.Inner) {
                    card.Health -= ability.NumPD;
                    if (card.Health > 0)
                        card.PoisonDamage += ability.NumPD;
                    modCards.Add(card);
                }

                foreach (CreatureCard card in GameManager.UserDict[t.Character].Player.Outer) {
                    card.Health -= ability.NumPD;
                    if (card.Health > 0)
                        card.PoisonDamage += ability.NumPD;
                    modCards.Add(card);
                }
            }

            // Prepare responses
            foreach (CharacterEnum ch in GameManager.UserDict.Keys.ToList())
                if (modCards.Any())
                    AddResponse(ch, new CardsModifiedResponse(modCards));
            return 0;
        }

        public override int Visit(DrawCardAbility ability)
        {
            // Draw card and prepare response
            AddResponse(Owner, new DrawCardResponse(Owner, GameManager.UserDict[Owner].Player.Draw()));
            foreach (CharacterEnum ch in CharacterManager.Instance.Others(Owner))
                AddResponse(ch, new DrawCardResponse(Owner, null));
            return 0;
        }

        public override int Visit(DamageDependingOnCPAbility ability)
        {
            //Log(OwnerCard.Name + " used KillIfPDAbility");
            List<CreatureCard> modCards = new List<CreatureCard>();
            foreach (CardTarget t in CardTargets)
            {
                CreatureCard targetCard = (CreatureCard)(GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.INNER) ?? GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.OUTER));
                //Log("Target is " + (deadCard.Name ?? "null") + " of " + t.Character.ToString());
                targetCard.Health -= (int)Math.Ceiling((double)Caller.Mana / 2);
                modCards.Add(targetCard);
            }
            foreach (CharacterEnum c in GameManager.UserDict.Keys)
                if(modCards.Any())
                    AddResponse(c, new CardsModifiedResponse(modCards));
            // Case target is Player
            foreach (PlayerTarget t in PlayerTargets)
            {
                Player targetPlayer = GameManager.UserDict[t.Character].Player;
                targetPlayer.Health -= Caller.Mana;
                foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                    AddResponse(c, new PlayerModifiedResponse(targetPlayer.Character, targetPlayer.Mana, targetPlayer.Health));
            }
            return 0;
        }

        public override int Visit(ResurrectOrTakeFromGraveyardAbility ability)
        {
            Random rnd = new Random();
            //Log(OwnerCard.Name + " used ResurrectOrTakeFromGraveyardAbility");
            CreatureCard resurrect = GameManager.Graveyard[rnd.Next(GameManager.Graveyard.Count)];
            GameManager.Graveyard.Remove(resurrect);
            CreatureCard evoked = (CreatureCard)resurrect.Original;
            //Log(OwnerCard.Name + " used ResurrectOrTakeFromGraveyardAbility, resurrected " + evoked.Name);
            Place place;
            if (GameManager.GetPlayer(Owner).Outer.Count < AmaruConstants.OUTER_MAX_SIZE) {
                place = Place.OUTER;
                GameManager.GetPlayer(Owner).Outer.Add(evoked);
            }
            else if (GameManager.GetPlayer(Owner).Inner.Count < AmaruConstants.INNER_MAX_SIZE) {
                place = Place.INNER;
                GameManager.GetPlayer(Owner).Inner.Add(evoked);
            }
            else
            {
                return 0;
            }
            foreach (CharacterEnum c in GameManager.UserDict.Keys)
                AddResponse(c, new ResurrectResponse(Owner, evoked, place));
            return 0;
        }

        public override int Visit(SeribuAbility seribuAbility)
        {
            Place place;
            List<CreatureCard> tbdOut = new List<CreatureCard>();
            List<CreatureCard> tbdIn = new List<CreatureCard>();
            foreach (CreatureCard card in Caller.Outer.Where(c => !c.IsCloned && !c.IsLegendary))
            {
                CreatureCard clone = (CreatureCard)card.Original;
                clone.IsCloned = true;
                // TODO: gestire se l'area è piena
                if (Caller.Outer.Count + tbdOut.Count < AmaruConstants.OUTER_MAX_SIZE)
                {
                    place = Place.OUTER;
                    tbdOut.Add(clone);
                }
                else if (Caller.Inner.Count + tbdIn.Count < AmaruConstants.INNER_MAX_SIZE)
                {
                    place = Place.INNER;
                    tbdIn.Add(clone);
                }
                else
                    continue;
                foreach (CharacterEnum c in GameManager.UserDict.Keys)
                    AddResponse(c, new EvocationResponse(Owner, card, clone, place));
            }
            foreach(CreatureCard card in Caller.Inner.Where(c => !c.IsCloned && !c.IsLegendary))
            {
                CreatureCard clone = (CreatureCard)card.Original;
                clone.IsCloned = true;
                // TODO: gestire se l'area è piena
                if (Caller.Inner.Count + tbdIn.Count < AmaruConstants.INNER_MAX_SIZE)
                {
                    place = Place.INNER;
                    tbdIn.Add(clone);
                }
                else if (Caller.Outer.Count + tbdOut.Count < AmaruConstants.OUTER_MAX_SIZE)
                {
                    place = Place.OUTER;
                    tbdOut.Add(clone);
                }
                else
                    continue;
                foreach (CharacterEnum c in GameManager.UserDict.Keys)
                    AddResponse(c, new EvocationResponse(Owner, card, clone, place));
            }
            if (tbdIn.Any())
                Caller.Inner.AddRange(tbdIn);
            if (tbdOut.Any())
                Caller.Outer.AddRange(tbdOut);
            return 0;
        }

        public override int Visit(KillIfPDAbility ability)
        {
            //Log(OwnerCard.Name + " used KillIfPDAbility");
            List<CreatureCard> DeadCards = new List<CreatureCard>();
            foreach (CardTarget t in CardTargets)
            {
                CreatureCard deadCard = (CreatureCard)(GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.INNER) ?? GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.OUTER));
                //Log("Target is " + (deadCard.Name ?? "null") + " of " + t.Character.ToString());
                deadCard.Health = 0;
                DeadCards.Add(deadCard);
            }
            foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                AddResponse(c, new CardsModifiedResponse(DeadCards));
            return 0;
        }

        public override int Visit(SummonAbility ability)
        {
            //Log("In summonAbility");
            if (GameManager.UserDict[Owner].Player.Outer.Count < AmaruConstants.OUTER_MAX_SIZE)
            {
                CreatureCard summoned = (CreatureCard)ability.toSummon.Original;
                summoned.Energy = 1;
                GameManager.UserDict[Owner].Player.Outer.Add(summoned);

                foreach (CharacterEnum c in GameManager.UserDict.Keys)
                    AddResponse(c, new EvocationResponse(Owner, (CreatureCard)OwnerCard, summoned, Place.OUTER));
            }
            return 0;
        }

        public override int Visit(AmaruIncarnationAbility ability)
        {
            foreach (CardTarget target in CardTargets)
            {
                if (Caller.Outer.Count >= AmaruConstants.OUTER_MAX_SIZE && Caller.Inner.Count >= AmaruConstants.INNER_MAX_SIZE)
                    return 0;
                Place origin = GameManager.UserDict[target.Character].Player.GetCardFromId(target.CardId, Place.INNER) == null ? Place.OUTER : Place.INNER;
                Place dest;
                CreatureCard oldCard = ((CreatureCard)GameManager.GetPlayer(target.Character).GetCardFromId(target.CardId, origin));
                CreatureCard moved = oldCard.Clone(false);

                // TODO: gestire se l'area è piena
                if (Caller.Outer.Count < AmaruConstants.OUTER_MAX_SIZE)
                {
                    dest = Place.OUTER;
                    GameManager.GetPlayer(target.Character).Outer.Remove(oldCard);
                    Caller.Outer.Add(moved);
                }
                else if (Caller.Inner.Count < AmaruConstants.INNER_MAX_SIZE)
                {
                    dest = Place.INNER;
                    GameManager.GetPlayer(target.Character).Inner.Remove(oldCard);
                    Caller.Inner.Add(moved);
                }
                else//*/
                    return 0;
                foreach (CharacterEnum c in GameManager.UserDict.Keys)
                    AddResponse(c, new EvocationResponse(Owner, oldCard, moved, dest, deleteOriginal: true));
            }
            return 0;
        }

        public override int Visit(DamageDependingOnCreatureNumberAbility ability)
        {
            //Log(OwnerCard.Name + " used DamageDependingOnCreatureNumberAbility");
            int attackPower = ability.myZone == Place.INNER ? GameManager.UserDict[Owner].Player.Inner.Count : GameManager.UserDict[Owner].Player.Outer.Count;
            // Case target is Creature
            if (Targets[0] is CardTarget)
            {
                CardTarget t = (CardTarget)Targets[0];
                CreatureCard targetCard = (CreatureCard)(GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.INNER) ?? GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.OUTER));
                targetCard.Health -= attackPower+ability.bonusDmg;
                foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                    AddResponse(c, new CardsModifiedResponse(targetCard));
            }
            // Case target is Player
            else
            {
                Player targetPlayer = GameManager.UserDict[Targets[0].Character].Player;
                targetPlayer.Health -= attackPower+ability.bonusDmg;
                foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                    AddResponse(c, new PlayerModifiedResponse(targetPlayer.Character, targetPlayer.Mana, targetPlayer.Health));
            }
            return 0;
        }

        public override int Visit(BonusAttackDependingOnHealthAbility ability)
        {
            //Log(OwnerCard.Name + " used BonusAttackDependingOnHealthAbilit");
            List<CreatureCard> targets = new List<CreatureCard>();
            foreach (CardTarget ct in CardTargets)
            {
                CreatureCard card = (CreatureCard)(GameManager.UserDict[ct.Character].Player.GetCardFromId(ct.CardId, Place.INNER) ?? GameManager.UserDict[ct.Character].Player.GetCardFromId(ct.CardId, Place.OUTER));
                card.Attack.BonusAttack += (int)Math.Ceiling((float)card.Health / (float)ability.myDivisor);
                targets.Add(card);
            }
            foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                AddResponse(c, new CardsModifiedResponse(targets));
            return 0;
        }

        public override int Visit(DamageWithPDAbility ability)
        {
            //Log(OwnerCard.Name + " used DamageWithPDAbility");
            List<CreatureCard> mods = new List<CreatureCard>();
            foreach (CardTarget ct in CardTargets)
            {
                CreatureCard targetCard = (CreatureCard)(GameManager.UserDict[ct.Character].Player.GetCardFromId(ct.CardId, Place.INNER) ?? GameManager.UserDict[ct.Character].Player.GetCardFromId(ct.CardId, Place.OUTER));
                targetCard.Health -= ability.NumPD;
                targetCard.PoisonDamage += ability.NumPD;
                mods.Add(targetCard);
            }
            foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                AddResponse(c, new CardsModifiedResponse(mods));
            return 0;
        }

        public override int Visit(GiveEPAbility ability)
        {
            //Log(OwnerCard.Name + " used GiveEPAbility");
            List<CreatureCard> mods = new List<CreatureCard>();
            foreach (CardTarget ct in CardTargets)
            {
                CreatureCard targetCard = (CreatureCard)(GameManager.UserDict[ct.Character].Player.GetCardFromId(ct.CardId, Place.INNER) ?? GameManager.UserDict[ct.Character].Player.GetCardFromId(ct.CardId, Place.OUTER));
                targetCard.Energy += ability.Ep;
                mods.Add(targetCard);
            }
            foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                AddResponse(c, new CardsModifiedResponse(mods));
            return 0;
        }

        public override int Visit(GainCPAbility ability)
        {
            //Log(OwnerCard.Name + " used GainCPAbility");
            Player caller = GameManager.UserDict[Owner].Player;
            caller.Mana += ability.cp;
            //Log(Owner.ToString() + " gained " + caller.Mana + " CP");
            foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                AddResponse(c, new PlayerModifiedResponse(caller.Character, caller.Mana, caller.Health));
            return 0;
        }

        public override int Visit(DoubleHPAbility ability)
        {
            //Log(OwnerCard.Name + " used DoubleHPAbility");
            ((CreatureCard)OwnerCard).Health *= 2;
            foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                AddResponse(c, new CardsModifiedResponse((CreatureCard)OwnerCard));
            return 0;
        }

        public override int Visit(DuplicatorSpellAbility ability)
        {
            throw new NotImplementedException();
        }

        public override int Visit(AddEPAndDrawSpellAbility ability)
        {
            throw new NotImplementedException();
        }

        public override int Visit(PDDamageToCreatureSpellAbility ability)
        {
            throw new NotImplementedException();
        }

        public override int Visit(ResurrectSpecificCreatureSpellAbility ability)
        {
            throw new NotImplementedException();
        }

        public override int Visit(ResurrectOrReturnToHandSpellAbility ability)
        {
            throw new NotImplementedException();
        }

        public override int Visit(GiveHPSpellAbility ability)
        {
            throw new NotImplementedException();
        }

        public override int Visit(GainCpSpellAbility ability)
        {
            throw new NotImplementedException();
        }

        public override int Visit(AttackFromInnerSpellAbility ability)
        {
            throw new NotImplementedException();
        }

        public override int Visit(DealDamageDependingOnPDNumberSpellAbility ability)
        {
            throw new NotImplementedException();
        }

        public override int Visit(DealDamageToEverythingSpellAbility ability)
        {
            throw new NotImplementedException();
        }

        public override int Visit(DealTotDamageToTotTargetsSpellAbility ability)
        {
            throw new NotImplementedException();
        }

        public override int Visit(DamagePDToAllCreaturesOfTargetPlayerSpellAbility ability)
        {
            throw new NotImplementedException();
        }

        public override int Visit(AttackEqualToHPSpellAbility ability)
        {
            throw new NotImplementedException();
        }

        public override int Visit(HalveDamageIfPDEffect effect)
        {
            throw new NotImplementedException();
        }

        public override int Visit(CostLessForPDEffect effect)
        {
            throw new NotImplementedException();
        }

        public override int Visit(GainHPForDamageEffect effect)
        {
            throw new NotImplementedException();
        }

        public override int Visit(IfKillGainHPEffect effect)
        {
            throw new NotImplementedException();
        }

        public override int Visit(GainAdditionalEPEffect effect)
        {
            throw new NotImplementedException();
        }

        public override int Visit(GainCPForCardPlayedEffect effect)
        {
            throw new NotImplementedException();
        }

        public override int Visit(AttackBuffInSpecificZoneEffect effect)
        {
            throw new NotImplementedException();
        }

        public override int Visit(ImmunityCreatureEffect effect)
        {
            throw new NotImplementedException();
        }

        public override int Visit(DealDamageDependingOnMAXHPSpellAbility dealDamageDependingOnMAXHPSpeelAbility)
        {
            throw new NotImplementedException();
        }
    }
}