using AmaruCommon.Actions.Targets;
using AmaruCommon.Communication.Messages;
using AmaruCommon.Constants;
using AmaruCommon.Exceptions;
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

        public OnCardPlayedVisitor(GameManager gameManager) : base (AmaruConstants.GAME_PREFIX + gameManager.Id)
        {
            this.GameManager = gameManager;
        }

        private void AddResponse(CharacterEnum c, Response r)
        {
            _successiveResponse.Add(new KeyValuePair<CharacterEnum, Response>(c, r));
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

        public override int Visit(DoubleHPAbility ability)
        {
            return 0;
        }

        public override int Visit(DuplicatorSpellAbility duplicatorSpellAbility)
        {
            List<CardMovement> movedCards = new List<CardMovement>();
            foreach(CardTarget ct in CardTargets)
            {
                Place origin = GameManager.UserDict[ct.Character].Player.GetCardFromId(ct.CardId, Place.INNER) == null ? Place.OUTER : Place.INNER;
                CreatureCard card = (CreatureCard)(GameManager.UserDict[ct.Character].Player.GetCardFromId(ct.CardId, origin));
                CreatureCard clone = (CreatureCard)card.Original;
                clone.IsCloned = true;
                // TODO: gestire se l'area è piena
                if (origin == Place.OUTER)
                {
                    if (GameManager.UserDict[ct.Character].Player.Outer.Count < AmaruConstants.OUTER_MAX_SIZE)
                        GameManager.UserDict[ct.Character].Player.Outer.Add(clone);
                    movedCards.Add(new CardMovement(origin, Place.OUTER, clone));
                }
                else if (origin == Place.INNER)
                {
                    if (GameManager.UserDict[ct.Character].Player.Inner.Count < AmaruConstants.INNER_MAX_SIZE)
                        GameManager.UserDict[ct.Character].Player.Inner.Add(clone);
                    movedCards.Add(new CardMovement(Place.DECK, Place.INNER, clone));
                }
            }
            foreach (CharacterEnum c in GameManager.UserDict.Keys)
                if (movedCards.Any())
                    AddResponse(c, new CardsDrawnResponse(Owner, movedCards));
            return 0;
        }

        public override int Visit(AddEPAndDrawSpellAbility spell)
        {
            // Draw card and prepare response
            AddResponse(Owner, new DrawCardResponse(Owner, GameManager.UserDict[Owner].Player.Draw()));
            foreach (CharacterEnum ch in CharacterManager.Instance.Others(Owner))
                AddResponse(Owner, new DrawCardResponse(Owner, null));

            // Handle Card targets
            List<CreatureCard> modCards = new List<CreatureCard>();
            foreach (CardTarget t in CardTargets)
            {
                CreatureCard card = (CreatureCard)(GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.INNER) ?? (GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.OUTER)));
                card.Energy -= spell.EpNumber;
                modCards.Add(card);
            }

            // Prepare responses
            foreach (CharacterEnum ch in GameManager.UserDict.Keys.ToList())
                AddResponse(ch, new CardsModifiedResponse(modCards));
            return 0;
        }

        public override int Visit(PDDamageToCreatureSpellAbility spell)
        {
            // Handle Card targets
            List<CreatureCard> modCards = new List<CreatureCard>();
            foreach (CardTarget t in CardTargets)
            {
                CreatureCard card = (CreatureCard)(GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.INNER) ?? (GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.OUTER)));
                card.Health -= spell.PDDamage;
                if (card.Health  > 0) 
                    card.PoisonDamage += spell.PDDamage;
                modCards.Add(card);
            }

            // Prepare responses
            foreach (CharacterEnum ch in GameManager.UserDict.Keys.ToList())
                if (modCards.Any())
                    AddResponse(ch, new CardsModifiedResponse(modCards));
            return 0;
        }

        public override int Visit(ResurrectSpecificCreatureSpellAbility spellAbility)
        {
            List<CardMovement> movedCards = new List<CardMovement>();
            LimitedList<CreatureCard> reborn = new LimitedList<CreatureCard>((AmaruConstants.OUTER_MAX_SIZE - GameManager.UserDict[Owner].Player.Outer.Count) > 3 ? (AmaruConstants.OUTER_MAX_SIZE - GameManager.UserDict[Owner].Player.Outer.Count) : 3);
            try
            {
                foreach (CreatureCard c in GameManager.Graveyard)
                    if (c.CardEnum.Equals(Amaru.BodyGuardian) || c.CardEnum.Equals(Amaru.SoulGuardian))
                        reborn.Add(c);
            }
            catch(LimitedListOutOfBoundException) { }
            finally
            {
                foreach (CreatureCard c in reborn)
                {
                    GameManager.Graveyard.Remove(c);
                    GameManager.UserDict[Owner].Player.Outer.Add((CreatureCard)c.Original);
                    movedCards.Add(new CardMovement(Place.GRAVEYARD, Place.OUTER, (CreatureCard)c.Original));
                }
                foreach (CharacterEnum c in GameManager.UserDict.Keys)
                    if (movedCards.Any())
                        AddResponse(c, new CardsDrawnResponse(Owner, movedCards));
            }
            return 0;
        }

        public override int Visit(ResurrectOrReturnToHandSpellAbility resurrectOrReturnToHandSpellAbility)
        {
            return 0;
        }

        public override int Visit(GiveHPSpellAbility spell)
        {
            // Handle player targets
            List<PlayerMod> modPlayers = new List<PlayerMod>();
            foreach (PlayerTarget t in PlayerTargets)
            {
                GameManager.UserDict[t.Character].Player.Health += spell.numHP;
                modPlayers.Add(new PlayerMod(GameManager.UserDict[t.Character].Player));
            }

            // Handle Card targets
            List<CreatureCard> modCards = new List<CreatureCard>();
            foreach (CardTarget t in CardTargets)
            {
                CreatureCard card = (CreatureCard)(GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.INNER) ?? (GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.OUTER)));
                card.Health += spell.numHP;
                modCards.Add(card);
            }

            // Prepare responses
            foreach (CharacterEnum ch in GameManager.UserDict.Keys.ToList())
            {
                if (modCards.Any())
                    AddResponse(ch, new CardsModifiedResponse(modCards));
                if (modPlayers.Any())
                    AddResponse(ch, new PlayerModifiedResponse(modPlayers));
            }
            return 0;
        }

        public override int Visit(GainCpSpellAbility spell)
        {
            Player owner = GameManager.UserDict[Owner].Player;
            owner.Mana += spell.numCP;
            foreach (CharacterEnum c in CharacterManager.Instance.Characters)
                AddResponse(c, new PlayerModifiedResponse(owner.Character, owner.Mana, owner.Health));
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
                AddResponse(c, new CardsModifiedResponse(mods));

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

            // Handle player targets
            List<PlayerMod> modPlayers = new List<PlayerMod>();
            foreach (PlayerTarget t in PlayerTargets)
            {
                GameManager.UserDict[t.Character].Player.Health -= PDcount;
                modPlayers.Add(new PlayerMod(GameManager.UserDict[t.Character].Player));
            }

            // Handle Card targets
            List<CreatureCard> modCards = new List<CreatureCard>();
            foreach (CardTarget t in CardTargets)
            {
                CreatureCard card = (CreatureCard)(GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.INNER) ?? (GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.OUTER)));
                card.Health -= PDcount;
                if (card.Health > 0) 
                    card.PoisonDamage += PDcount;
                modCards.Add(card);
            }

            // Prepare responses
            foreach (CharacterEnum ch in GameManager.UserDict.Keys.ToList())
            {
                if (modCards.Any())
                    AddResponse(ch, new CardsModifiedResponse(modCards));
                if (modPlayers.Any())
                    AddResponse(ch, new PlayerModifiedResponse(modPlayers));
            }
            return 0;
        }


        public override int Visit(DealDamageToEverythingSpellAbility spell)
        {
            List<CreatureCard> modCards = new List<CreatureCard>();
            foreach (User u in GameManager.UserDict.Values) {
                Player p = u.Player;
                foreach (CreatureCard c in p.Outer) {
                    c.Health -= spell.numDamage;
                    modCards.Add(c);
                }
            }

            foreach (CharacterEnum ch in GameManager.UserDict.Keys.ToList())
                if (modCards.Any())
                    AddResponse(ch, new CardsModifiedResponse(modCards));

            return 0;
        }

        public override int Visit(DealTotDamageToTotTargetsSpellAbility spell)
        {
            // Handle player targets
            List<PlayerMod> modPlayers = new List<PlayerMod>();
            foreach (PlayerTarget t in PlayerTargets) {
                GameManager.UserDict[t.Character].Player.Health -= spell.DamageToDeal;
                modPlayers.Add(new PlayerMod(GameManager.UserDict[t.Character].Player));
            }

            // Handle Card targets
            List<CreatureCard> modCards = new List<CreatureCard>();
            foreach (CardTarget t in CardTargets) {
                CreatureCard card = (CreatureCard)(GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.INNER) ?? (GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.OUTER)));
                card.Health -= spell.DamageToDeal;
                modCards.Add(card);
            }

            // Prepare responses
            foreach (CharacterEnum ch in GameManager.UserDict.Keys.ToList()) {
                if (modCards.Any())
                    AddResponse(ch, new CardsModifiedResponse(modCards));
                if (modPlayers.Any())
                    AddResponse(ch, new PlayerModifiedResponse(modPlayers));
            }
            return 0;
        }

        public override int Visit(DamagePDToAllCreaturesOfTargetPlayerSpellAbility spell)
        {
            // Handle Card targets
            List<CreatureCard> modCards = new List<CreatureCard>();
            foreach (PlayerTarget t in PlayerTargets) {
                foreach (CreatureCard card in GameManager.UserDict[t.Character].Player.Inner) {
                    card.Health -= spell.numPd;
                    if (card.Health > 0)
                        card.PoisonDamage += spell.numPd;
                    modCards.Add(card);
                }

                foreach (CreatureCard card in GameManager.UserDict[t.Character].Player.Outer) {
                    card.Health -= spell.numPd;
                    if (card.Health > 0)
                        card.PoisonDamage += spell.numPd;
                    modCards.Add(card);
                }
            }

            // Prepare responses
            foreach (CharacterEnum ch in GameManager.UserDict.Keys.ToList())
                if (modCards.Any())
                    AddResponse(ch, new CardsModifiedResponse(modCards));
            return 0;
        }

        public override int Visit(DealDamageDependingOnMAXHPSpellAbility spell)
        {
            int dmg = 0;
            List<CreatureCard> modCards = new List<CreatureCard>();
            
            foreach (CreatureCard card in GameManager.UserDict[GameManager.ActiveCharacter].Player.Inner) {
                if (card.Health > dmg)
                    dmg = card.Health;
            }

            foreach (CreatureCard card in GameManager.UserDict[GameManager.ActiveCharacter].Player.Outer) {
                if (card.Health > dmg)
                    dmg = card.Health;
            }

            foreach (CardTarget t in CardTargets) {
                CreatureCard card = (CreatureCard)(GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.INNER) ?? (GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.OUTER)));
                card.Health -= dmg;
                modCards.Add(card);
            }

            // Handle player targets
            List<PlayerMod> modPlayers = new List<PlayerMod>();
            foreach (PlayerTarget t in PlayerTargets) {
                GameManager.UserDict[t.Character].Player.Health -= dmg;
                modPlayers.Add(new PlayerMod(GameManager.UserDict[t.Character].Player));
            }

            // Prepare responses
            foreach (CharacterEnum ch in GameManager.UserDict.Keys.ToList()) {
                if (modCards.Any())
                    AddResponse(ch, new CardsModifiedResponse(modCards));
                if (modPlayers.Any())
                    AddResponse(ch, new PlayerModifiedResponse(modPlayers));
            }

            return 0;
        }

        public override int Visit(AttackEqualToHPSpellAbility attackEqualToHPSpellAbility)
        {
            List<CreatureCard> modCards = new List<CreatureCard>();

            // Handle card targets
            foreach (CardTarget t in CardTargets) {
                CreatureCard card = (CreatureCard)(GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.INNER) ?? (GameManager.UserDict[t.Character].Player.GetCardFromId(t.CardId, Place.OUTER)));
                card.Attack.BonusAttack += card.Health;
                modCards.Add(card);
            }

            // Prepare responses
            foreach (CharacterEnum ch in GameManager.UserDict.Keys.ToList())
                if (modCards.Any())
                    AddResponse(ch, new CardsModifiedResponse(modCards));
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
