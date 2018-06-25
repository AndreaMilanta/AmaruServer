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
            return attack.Power;
        }

        public override int Visit(KrumAttack attack)
        {
            return Caller.ManaSpentThisTurn + attack.BonusAttack;
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
            int PDcount = attack.BonusAttack;
            foreach(User u in GameManager.UserDict.Values) {
                foreach (CreatureCard c in u.Player.Inner)
                    PDcount += c.PoisonDamage;
                foreach (CreatureCard c in u.Player.Outer)
                    PDcount += c.PoisonDamage;
            }
            if (CardTarget != null) {
                CreatureCard targetCard = (CreatureCard)(GameManager.UserDict[CardTarget.Character].Player.GetCardFromId(CardTarget.CardId, Place.INNER) ?? GameManager.UserDict[CardTarget.Character].Player.GetCardFromId(CardTarget.CardId, Place.OUTER));
                if (targetCard.Health - attack.Power > 0) {
                    targetCard.PoisonDamage += attack.Power;
                    foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                        AddResponse(c, new CardsModifiedResponse(targetCard));
                }
            }

            return PDcount;
        }

        public override int Visit(SeribuAttack attack)
        {
            return Caller.Inner.Count + Caller.Outer.Count + attack.BonusAttack;
        }

        public override int Visit(GainHPAbility ability)
        {
            Log(OwnerCard.Name + " used GainHPAbility");
            ((CreatureCard)OwnerCard).Health += ability.Hp;
            foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                AddResponse(c, new CardsModifiedResponse((CreatureCard)OwnerCard));
            return 0;
        }

        public override int Visit(ReturnToHandAbility returnToHandAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(SalazarAbility salazarAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(SpendCPToDealDamageAbility spendCPToDealDamageAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(ResurrectOrTakeFromGraveyardAbility resurrectAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(SeribuAbility seribuAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(KillIfPDAbility ability)
        {
            Log(OwnerCard.Name + " used KillIfPDAbility");
            List<CreatureCard> DeadCards = new List<CreatureCard>();
            foreach (CardTarget t in CardTargets)
            {
                CreatureCard deadCard = (CreatureCard)(GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.INNER) ?? GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.OUTER));
                Log("Target is " + (deadCard.Name ?? "null") + " of " + t.Character.ToString());
                deadCard.Health = 0;
                DeadCards.Add(deadCard);
            }
            foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                AddResponse(c, new CardsModifiedResponse(DeadCards));
            return 0;
        }

        public override int Visit(SummonAbility ability)
        {
            if (GameManager.UserDict[Owner].Player.Outer.Count < AmaruConstants.OUTER_MAX_SIZE)
            {
                CreatureCard summoned = (CreatureCard)ability.toSummon.Original;
                GameManager.UserDict[Owner].Player.Outer.Add(summoned);

                foreach (CharacterEnum c in GameManager.UserDict.Keys)
                    AddResponse(c, new CardsDrawnResponse(Owner, Place.DECK, Place.OUTER, summoned));
            }
            return 0;
        }

        public override int Visit(AmaruIncarnationAbility amaruIncarnationAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(DamageDependingOnCreatureNumberAbility ability)
        {
            Log(OwnerCard.Name + " used DamageDependingOnCreatureNumberAbility");
            int attackPower = ability.myZone == Place.INNER ? GameManager.UserDict[Owner].Player.Inner.Count : GameManager.UserDict[Owner].Player.Outer.Count;
            // Case target is Creature
            if (Targets[0] is CardTarget)
            {
                CardTarget t = (CardTarget)Targets[0];
                CreatureCard targetCard = (CreatureCard)(GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.INNER) ?? GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.OUTER));
                targetCard.Health -= attackPower;
                foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                    AddResponse(c, new CardsModifiedResponse(targetCard));
            }
            // Case target is Player
            else
            {
                Player targetPlayer = GameManager.UserDict[Targets[0].Character].Player;
                targetPlayer.Health -= attackPower;
                foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                    AddResponse(c, new PlayerModifiedResponse(targetPlayer.Character, targetPlayer.Mana, targetPlayer.Health));
            }
            return 0;
        }

        public override int Visit(BonusAttackDependingOnHealthAbility ability)
        {
            Log(OwnerCard.Name + " used BonusAttackDependingOnHealthAbilit");
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
            Log(OwnerCard.Name + " used DamageWithPDAbility");
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
            Log(OwnerCard.Name + " used GiveEPAbility");
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
            Log(OwnerCard.Name + " used GainCPAbility");
            Player caller = GameManager.UserDict[Owner].Player;
            caller.Mana += ability.cp;
            Log(Owner.ToString() + " gained " + caller.Mana + " CP");
            foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                AddResponse(c, new PlayerModifiedResponse(caller.Character, caller.Mana, caller.Health));
            return 0;
        }

        public override int Visit(DoubleHPAbility ability)
        {
            Log(OwnerCard.Name + " used DoubleHPAbility");
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