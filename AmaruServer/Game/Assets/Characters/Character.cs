using System;

namespace AmaruServer.Game.Assets.Characters
{
    class Character
    {
        private String _info;
        private CharacterEnum _name;

        public String Info { get => _info; }
        public CharacterEnum Name { get => _name;}

        public Character(CharacterEnum name)
        {
            _name = name;
        }
    }
}
