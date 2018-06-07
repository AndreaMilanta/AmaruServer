using System;
using System.Collections.Generic;

using AmaruCommon.Constants;
using AmaruCommon.GameAssets.Cards;
using AmaruCommon.GameAssets.Characters;
using AmaruCommon.GameAssets.Player;

namespace AmaruServer.Game.Assets
{
    public class Player
    {
        // Values
        public int Mana { get;  set; } = 0;
        public int Health { get;  set; } = AmaruConstants.INITIAL_PLAYER_HEALTH;

        // Cards
        private Stack<Card> _deck = null;
        private LimitedList<Card> _hand = new LimitedList<Card>(AmaruConstants.HAND_MAX_SIZE);
        private LimitedList<Card> _inner = new LimitedList<Card>(AmaruConstants.INNER_MAX_SIZE);
        private LimitedList<Card> _outer = new LimitedList<Card>(AmaruConstants.OUTER_MAX_SIZE);

        // Communication
        public EnemyInfo AsEnemy { get => new EnemyInfo(Character, Health, Mana, _deck.Count, _hand.Count, _inner, _outer); }
        public OwnInfo AsOwn { get => new OwnInfo(Character,  Health, Mana, _deck.Count, _hand, _inner, _outer); }

        public CharacterEnum Character { get; private set; } = CharacterEnum.INVALID;
         
        public Player(CharacterEnum character)
        {
            Character = character;
            _deck = new Stack<Card>(DeckFactory.GetDeck(Character));
        }

        public void Draw()
        {
            _hand.Add(_deck.Pop());
        }

        public void Draw(int amount)
        {
            for (int i = 0; i < amount; i++)
                this.Draw();
        }
    }
}
