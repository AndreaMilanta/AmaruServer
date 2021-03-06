﻿using System;
using System.Collections.Generic;
using AmaruCommon.Constants;
using AmaruCommon.GameAssets.Cards;
using AmaruCommon.GameAssets.Cards.Properties;
using AmaruCommon.GameAssets.Cards.Properties.Abilities;
using AmaruCommon.GameAssets.Cards.Properties.Attacks;
using AmaruCommon.GameAssets.Cards.Properties.CreatureEffects;
using AmaruCommon.GameAssets.Cards.Properties.SpellAbilities;
using AmaruCommon.GameAssets.Characters;
using AmaruCommon.GameAssets.Players;

namespace AmaruServer.Game.Managing
{
    public class OnTurnStartVisitor : PropertyVisitor
    {
        public List<Card> ModifiedCard { get; private set; }

        public OnTurnStartVisitor(CharacterEnum player, string logger, Card card = null) : base (logger)
        {
            this.Owner = player;
            this.OwnerCard = card;
            this.ModifiedCard = new List<Card>();
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

        public override int Visit(IfKillGainHPAttack attack)
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

        public override int Visit(DamageDependingOnCPAbility spendCPToDealDamageAbility)
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

        public override int Visit(DealDamageDependingOnMAXHPSpellAbility dealDamageDependingOnMAXHPSpeelAbility)
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

        public override int Visit(ImmunityCreatureEffect immunityCreatureEffect)
        {
            return 0;
        }

        public override int Visit(DoubleHPAbility doubleHPAbility)
        {
            return 0;
        }

        public override int Visit(DrawCardAbility drawCardAbility)
        {
            return 0;
        }
    }
}