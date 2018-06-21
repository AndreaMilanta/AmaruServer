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
            throw new NotImplementedException();
        }

        public override int Visit(GainHPAttack attack)
        {
            throw new NotImplementedException();
        }

        public override int Visit(ImperiaAttack attack)
        {
            throw new NotImplementedException();
        }

        public override int Visit(KrumAttack attack)
        {
            throw new NotImplementedException();
        }

        public override int Visit(PoisonAttack attack)
        {
            throw new NotImplementedException();
        }

        public override int Visit(SalazarAttack attack)
        {
            throw new NotImplementedException();
        }

        public override int Visit(SeribuAttack attack)
        {
            throw new NotImplementedException();
        }

        public override int Visit(SimpleAttack attack)
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

        public override int Visit(DamageWithPDAbility damageWithPDAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(GiveEPAbility ability)
        {
            throw new NotImplementedException();
        }

        public override int Visit(GainCPAbility gainCPAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(DuplicatorSpellAbility duplicatorSpellAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(AddEPAndDrawSpellAbility addEPAndDrawSpellAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(PDDamageToCreatureSpellAbility pDDamageToCreatureSpellAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(ResurrectSpecificCreatureSpellAbility resurrectSpecificCreatureSpellAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(ResurrectOrReturnToHandSpellAbility resurrectOrReturnToHandSpellAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(GiveHPSpellAbility giveHPSpellAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(GainCpSpellAbility gainCpSpellAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(AttackFromInnerSpellAbility attackFromInnerSpellAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(DealDamageDependingOnPDNumberSpellAbility dealDamageDependingOnPDNumberSpellAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(DealDamageToEverythingSpellAbility dealDamageToEverythingSpellAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(DealTotDamageToTotTargetsSpellAbility dealTotDamageToTotTargetsSpellAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(DamagePDToAllCreaturesOfTargetPlayerSpellAbility damagePDToAllCreaturesOfTargetPlayerSpellAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(DealDamageDependingOnMAXHPSpeelAbility dealDamageDependingOnMAXHPSpeelAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(AttackEqualToHPSpellAbility attackEqualToHPSpellAbility)
        {
            throw new NotImplementedException();
        }

        public override int Visit(HalveDamageIfPDEffect halveDamageIfPDEffect)
        {
            throw new NotImplementedException();
        }

        public override int Visit(CostLessForPDEffect costLessForPDEffect)
        {
            throw new NotImplementedException();
        }

        public override int Visit(GainHPForDamageEffect gainHPForDamageEffect)
        {
            throw new NotImplementedException();
        }

        public override int Visit(IfKillGainHPEffect ifKillGainHPEffect)
        {
            throw new NotImplementedException();
        }

        public override int Visit(GainAdditionalEPEffect gainAdditionalEPEffect)
        {
            throw new NotImplementedException();
        }

        public override int Visit(GainCPForCardPlayedEffect gainCPForCardPlayed)
        {
            throw new NotImplementedException();
        }

        public override int Visit(DrawCardAndAttack drawCardAndAttack)
        {
            throw new NotImplementedException();
        }

        public override int Visit(AttackBuffInSpecificZoneEffect attackBuffInSpecificZoneEffect)
        {
            throw new NotImplementedException();
        }
    }
}
