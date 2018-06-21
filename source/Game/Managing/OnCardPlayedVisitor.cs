using AmaruCommon.Actions.Targets;
using AmaruCommon.GameAssets.Cards.Properties;
using AmaruCommon.GameAssets.Cards.Properties.Abilities;
using AmaruCommon.GameAssets.Cards.Properties.Attacks;
using AmaruCommon.GameAssets.Cards.Properties.CreatureEffects;
using AmaruCommon.GameAssets.Cards.Properties.SpellAbilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmaruServer.Game.Managing
{
    public class OnCardPlayedVisitor : PropertyVisitor
    {
        private GameManager GameManager { get; set; }
        public List<Target> Targets { get; set; }

        public OnCardPlayedVisitor(GameManager gameManager)
        {
            this.GameManager = gameManager;
        }

        public override int Visit(GainCPAttack attack)
        {
            return 0;
        }

        public override int Visit(GainHPAttack attack)
        {
            return 0;
        }

        public override int Visit(ImperiaAttack attack)
        {
            return 0;
        }

        public override int Visit(KrumAttack attack)
        {
            return 0;
        }

        public override int Visit(PoisonAttack attack)
        {
            return 0;
        }

        public override int Visit(SalazarAttack attack)
        {
            return 0;
        }

        public override int Visit(SeribuAttack attack)
        {
            return 0;
        }

        public override int Visit(SimpleAttack attack)
        {
            return 0;
        }

        public override int Visit(GainHPAbility ability)
        {
            return 0;
        }

        public override int Visit(ReturnToHandAbility returnToHandAbility)
        {
            return 0;
        }

        public override int Visit(SalazarAbility salazarAbility)
        {
            return 0;
        }

        public override int Visit(SpendCPToDealDamageAbility spendCPToDealDamageAbility)
        {
            return 0;
        }

        public override int Visit(ResurrectOrTakeFromGraveyardAbility resurrectAbility)
        {
            return 0;
        }

        public override int Visit(SeribuAbility seribuAbility)
        {
            return 0;
        }

        public override int Visit(KillIfPDAbility killIfPDAbility)
        {
            return 0;
        }

        public override int Visit(SummonAbility summonAbility)
        {
            return 0;
        }

        public override int Visit(AmaruIncarnationAbility amaruIncarnationAbility)
        {
            return 0;
        }

        public override int Visit(DamageDependingOnCreatureNumberAbility damageDependingOnCreatureNumberAbility)
        {
            return 0;
        }

        public override int Visit(BonusAttackDependingOnHealthAbility bonusAttackDependingOnHealthAbility)
        {
            return 0;
        }

        public override int Visit(DamageWithPDAbility damageWithPDAbility)
        {
            return 0;
        }

        public override int Visit(GiveEPAbility ability)
        {
            return 0;
        }

        public override int Visit(GainCPAbility gainCPAbility)
        {
            return 0;
        }

        public override int Visit(DuplicatorSpellAbility duplicatorSpellAbility)
        {
            return 0;
        }

        public override int Visit(AddEPAndDrawSpellAbility addEPAndDrawSpellAbility)
        {
            return 0;
        }

        public override int Visit(PDDamageToCreatureSpellAbility pDDamageToCreatureSpellAbility)
        {
            return 0;
        }

        public override int Visit(ResurrectSpecificCreatureSpellAbility resurrectSpecificCreatureSpellAbility)
        {
            return 0;
        }

        public override int Visit(ResurrectOrReturnToHandSpellAbility resurrectOrReturnToHandSpellAbility)
        {
            return 0;
        }

        public override int Visit(GiveHPSpellAbility giveHPSpellAbility)
        {
            return 0;
        }

        public override int Visit(GainCpSpellAbility gainCpSpellAbility)
        {
            return 0;
        }

        public override int Visit(AttackFromInnerSpellAbility attackFromInnerSpellAbility)
        {
            return 0;
        }

        public override int Visit(DealDamageDependingOnPDNumberSpellAbility dealDamageDependingOnPDNumberSpellAbility)
        {
            return 0;
        }

        public override int Visit(DealDamageToEverythingSpellAbility dealDamageToEverythingSpellAbility)
        {
            return 0;
        }

        public override int Visit(DealTotDamageToTotTargetsSpellAbility dealTotDamageToTotTargetsSpellAbility)
        {
            return 0;
        }

        public override int Visit(DamagePDToAllCreaturesOfTargetPlayerSpellAbility damagePDToAllCreaturesOfTargetPlayerSpellAbility)
        {
            return 0;
        }

        public override int Visit(DealDamageDependingOnMAXHPSpeelAbility dealDamageDependingOnMAXHPSpeelAbility)
        {
            return 0;
        }

        public override int Visit(AttackEqualToHPSpellAbility attackEqualToHPSpellAbility)
        {
            return 0;
        }

        public override int Visit(HalveDamageIfPDEffect halveDamageIfPDEffect)
        {
            return 0;
        }

        public override int Visit(CostLessForPDEffect costLessForPDEffect)
        {
            return 0;
        }

        public override int Visit(GainHPForDamageEffect gainHPForDamageEffect)
        {
            return 0;
        }

        public override int Visit(IfKillGainHPEffect ifKillGainHPEffect)
        {
            return 0;
        }

        public override int Visit(GainAdditionalEPEffect gainAdditionalEPEffect)
        {
            return 0;
        }

        public override int Visit(GainCPForCardPlayedEffect gainCPForCardPlayed)
        {
            return 0;
        }

        public override int Visit(DrawCardAndAttack drawCardAndAttack)
        {
            return 0;
        }

        public override int Visit(AttackBuffInSpecificZoneEffect attackBuffInSpecificZoneEffect)
        {
            return 0;
        }
    }
}
