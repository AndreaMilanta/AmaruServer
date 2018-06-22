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

namespace AmaruServer.Game.Managing
{
    public class AttacksVisitor : PropertyVisitor
    {
        private GameManager GameManager { get; set; }
        private Player Caller { get; set; }
        private PlayerTarget PlayerTarget { get; set; } = null;
        private CardTarget CardTarget { get; set; } = null;
        private CreatureCard Attacker;

        private Dictionary<CharacterEnum, Response> _successiveResponse = new Dictionary<CharacterEnum, Response>();
        public Dictionary<CharacterEnum, Response> SuccessiveResponse { get { Dictionary<CharacterEnum, Response> sr = _successiveResponse;  return sr; }  set { _successiveResponse.Clear(); } }
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
            if (target is CardTarget)
                CardTarget = (CardTarget)target;
            if (target is PlayerTarget)
                PlayerTarget = (PlayerTarget)target;

            this.GameManager = gameManager;
            this.Attacker = attacker;
        }
        public override int Visit(SimpleAttack attack)
        {
            return attack.Power;
        }
        public override int Visit(ImperiaAttack attack)
        {
            return Attacker.Health;
        }

        public override int Visit(GainCPAttack attack)
        {
            Caller.Mana += attack.Cp;
            foreach(CharacterEnum c in GameManager._userDict.Keys.ToList()) {
                _successiveResponse.Add(c, new PlayerModifiedResponse(Caller.Character, Caller.Mana, Caller.Health));
            }
            return attack.Power;
        }

        public override int Visit(GainHPAttack attack)
        {
            Caller.Health += attack.Hp;
            foreach (CharacterEnum c in GameManager._userDict.Keys.ToList()) {
                _successiveResponse.Add(c, new PlayerModifiedResponse(Caller.Character, Caller.Mana, Caller.Health));
            }
            return attack.Power;
        }

        public override int Visit(KrumAttack attack)
        {
            throw new NotImplementedException();
        }

        public override int Visit(PoisonAttack attack)
        {
            return attack.Power;
        }

        public override int Visit(SalazarAttack attack)
        {
            throw new NotImplementedException();
        }

        public override int Visit(SeribuAttack attack)
        {
            throw new NotImplementedException();
        }

        public override int Visit(GainHPAbility ability)
        {
            throw new NotImplementedException();
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

        public override int Visit(KillIfPDAbility killIfPDAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(SummonAbility summonAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(AmaruIncarnationAbility amaruIncarnationAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(DamageDependingOnCreatureNumberAbility damageDependingOnCreatureNumberAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(BonusAttackDependingOnHealthAbility bonusAttackDependingOnHealthAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(DamageWithPDAbility bility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(GiveEPAbility ability)
        {
            throw new NotImplementedException();
        }

        public override int Visit(GainCPAbility ability)
        {
            throw new NotImplementedException();
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

        public override int Visit(DealDamageDependingOnMAXHPSpeelAbility ability)
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

        public override int Visit(DrawCardAndAttack attack)
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

        public override int Visit(DoubleHPAbility doubleHPAbility)
        {
            throw new NotImplementedException();
        }
    }
}