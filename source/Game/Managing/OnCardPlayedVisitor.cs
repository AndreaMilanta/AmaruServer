using AmaruCommon.Actions.Targets;
using AmaruCommon.Constants;
using AmaruCommon.GameAssets.Cards;
using AmaruCommon.GameAssets.Cards.Properties;
using AmaruCommon.GameAssets.Cards.Properties.Abilities;
using AmaruCommon.GameAssets.Cards.Properties.Attacks;
using AmaruCommon.GameAssets.Cards.Properties.CreatureEffects;
using AmaruCommon.GameAssets.Cards.Properties.SpellAbilities;
using AmaruCommon.GameAssets.Characters;
using AmaruCommon.GameAssets.Players;
using AmaruCommon.Responses;
using AmaruServer.Networking;
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
        private Dictionary<CharacterEnum,Response> _successiveResponse = new Dictionary<CharacterEnum, Response>();
        public Dictionary<CharacterEnum,Response> SuccessiveResponse { get { Dictionary<CharacterEnum,Response> sr = _successiveResponse; _successiveResponse.Clear(); return sr; } }

        public OnCardPlayedVisitor(GameManager gameManager) : base (AmaruConstants.GAME_PREFIX + gameManager.Id)
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

        public override int Visit(GainCPAbility ability)
        {
            return 0;
        }

        public override int Visit(DuplicatorSpellAbility duplicatorSpellAbility)
        {
            return 0;
        }

        public override int Visit(AddEPAndDrawSpellAbility spell)
        {
            //ADD DRAW CARD!!!!!!!!!!!!!!

            List<CreatureCard> mods = new List<CreatureCard>();
            foreach (CardTarget ct in Targets) {
                CreatureCard targetCard = (CreatureCard)(GameManager.UserDict[ct.Character].Player.GetCardFromId(ct.CardId, Place.INNER) ?? GameManager.UserDict[ct.Character].Player.GetCardFromId(ct.CardId, Place.OUTER));
                targetCard.Energy += spell.EpNumber;
                mods.Add(targetCard);
            }
            foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                _successiveResponse.Add(c, new CardsModifiedResponse(mods));

            return 0;
        }

        public override int Visit(PDDamageToCreatureSpellAbility spell)
        {
            foreach (Target t in Targets) {
                CreatureCard c = ((CardTarget)t).Card;
                c.Health -= spell.PDDamage;
                if (c.Health - spell.PDDamage > 0) {
                    c.PoisonDamage += spell.PDDamage;
                    foreach (CharacterEnum ch in GameManager.UserDict.Keys.ToList())
                        _successiveResponse.Add(ch, new CardsModifiedResponse(c));
                }
            }

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

        public override int Visit(GiveHPSpellAbility spell)
        {
            Player owner = GameManager.UserDict[Owner].Player;
            owner.Health += spell.numHP;
            foreach (CharacterEnum c in CharacterManager.Instance.Characters)
                _successiveResponse.Add(c, new PlayerModifiedResponse(owner.Character, owner.Mana, owner.Health));
            return 0;
        }

        public override int Visit(GainCpSpellAbility spell)
        {
            Player owner = GameManager.UserDict[Owner].Player;
            owner.Mana += spell.numCP;
            foreach (CharacterEnum c in CharacterManager.Instance.Characters)
                _successiveResponse.Add(c, new PlayerModifiedResponse(owner.Character, owner.Mana, owner.Health));
            return 0;
        }

        public override int Visit(DoubleHPAbility ability)
        { 
            return 0;
        }

        public override int Visit(AttackFromInnerSpellAbility spell)
        {
            Player owner = GameManager.UserDict[Owner].Player;
            List<CreatureCard> mods = new List<CreatureCard>();
            foreach (CreatureCard c in owner.Inner) {
                c.Attack.BonusAttack++;
                mods.Add(c);
            }
            foreach (CreatureCard c in owner.Outer) { 
                c.Attack.BonusAttack++;
                mods.Add(c);
            }
            foreach (CharacterEnum c in CharacterManager.Instance.Characters)
                _successiveResponse.Add(c, new CardsModifiedResponse(mods));

            return 0;
        }

        public override int Visit(DealDamageDependingOnPDNumberSpellAbility spell)
        {
            int PDcount = 0;
            foreach (User u in GameManager.UserDict.Values) {
                foreach (CreatureCard c in u.Player.Inner)
                    PDcount += c.PoisonDamage;
                foreach (CreatureCard c in u.Player.Outer)
                    PDcount += c.PoisonDamage;
            }

            foreach (Target t in Targets) {
                if(t is PlayerTarget) {
                    GameManager.UserDict[t.Character].Player.Health -= PDcount;
                }
                else {
                    CreatureCard c = ((CardTarget)t).Card;
                    c.Health -= PDcount;
                    if (c.Health - PDcount > 0) {
                        c.PoisonDamage += PDcount;
                        foreach (CharacterEnum ch in GameManager.UserDict.Keys.ToList())
                            _successiveResponse.Add(ch, new CardsModifiedResponse(c));
                    }
                }
            }
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
    }
}
