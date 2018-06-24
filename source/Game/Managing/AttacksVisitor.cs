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
            this.Target = target;
            this.GameManager = gameManager;
            this.Attacker = attacker;
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
                _successiveResponse.Add(c, new PlayerModifiedResponse(Caller.Character, Caller.Mana, Caller.Health));
            }
            return attack.Power;
        }

        public override int Visit(GainHPAttack attack)
        {
            if (attack.ToCreature) {
                Attacker.Health += attack.Hp;
                foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList()) {
                    _successiveResponse.Add(c, new CardsModifiedResponse(Attacker));
                }
            }
            else {
                Caller.Health += attack.Hp;

                foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList()) {
                    _successiveResponse.Add(c, new PlayerModifiedResponse(Caller.Character, Caller.Mana, Caller.Health));
                }
            }
            return attack.Power;
        }

        public override int Visit(KrumAttack attack)
        {
            // Remember to include BonusAttack
            throw new NotImplementedException();
        }

        public override int Visit(PoisonAttack attack)
        {
            if (CardTarget != null) { 
            CreatureCard targetCard = (CreatureCard)(GameManager.UserDict[CardTarget.Character].Player.GetCardFromId(CardTarget.CardId, Place.INNER) ?? GameManager.UserDict[CardTarget.Character].Player.GetCardFromId(CardTarget.CardId, Place.OUTER));
                if (targetCard.Health - attack.Power > 0) {
                    targetCard.PoisonDamage += attack.Power;
                    foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                        _successiveResponse.Add(c, new CardsModifiedResponse(targetCard));
                }
            }
        
            return attack.Power;
        }

        public override int Visit(SalazarAttack attack)
        {
            // Remember to include BonusAttack
            throw new NotImplementedException();
        }

        public override int Visit(SeribuAttack attack)
        {
            // Remember to include BonusAttack
            throw new NotImplementedException();
        }

        public override int Visit(GainHPAbility ability)
        {
            Log(OwnerCard.Name + " used GainHPAbility");
            ((CreatureCard)OwnerCard).Health += ability.Hp;
            foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                _successiveResponse.Add(c, new CardsModifiedResponse((CreatureCard)OwnerCard));
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
                deadCard.Health = 0;
                DeadCards.Add(deadCard);
            }
            foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
            {
                GameManager.UserDict[c].Player.Refresh();
                _successiveResponse.Add(c, new CardsModifiedResponse(DeadCards));
            }
            return 0;
        }

        public override int Visit(SummonAbility summonAbility)
        {
            throw new NotImplementedException();
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
                    _successiveResponse.Add(c, new CardsModifiedResponse(targetCard));
                GameManager.UserDict[t.Character].Player.Refresh();
            }
            // Case target is Player
            else
            {
                Player targetPlayer = GameManager.UserDict[Targets[0].Character].Player;
                targetPlayer.Health -= attackPower;
                foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                    _successiveResponse.Add(c, new PlayerModifiedResponse(targetPlayer.Character, targetPlayer.Mana, targetPlayer.Health));
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
                card.Attack.BonusAttack = (int)((float)card.Health / ability.myDivisor);
            }
            foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                _successiveResponse.Add(c, new CardsModifiedResponse(targets));
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
                GameManager.GetPlayer(ct.Character).Refresh();
            }
            foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                _successiveResponse.Add(c, new CardsModifiedResponse(mods));
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
            }
            foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                _successiveResponse.Add(c, new CardsModifiedResponse(mods));
            return 0;
        }

        public override int Visit(GainCPAbility ability)
        {
            Log(OwnerCard.Name + " used GainCPAbility");
            List<PlayerMod> mods = new List<PlayerMod>();
            foreach (PlayerTarget pt in PlayerTargets) {
                Player player = GameManager.UserDict[pt.Character].Player;
                player.Mana += ability.cp;
                mods.Add(new PlayerMod(player.Character, player.Mana, player.Health));
            }
            foreach (CharacterEnum c in GameManager.UserDict.Keys.ToList())
                _successiveResponse.Add(c, new PlayerModifiedResponse(mods));
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

        public override int Visit(DealDamageDependingOnMAXHPSpellAbility dealDamageDependingOnMAXHPSpeelAbility)
        {
            throw new NotImplementedException();
        }
    }
}