using System;
using Microsoft.Xna.Framework;

namespace Roma
{
    public class Upgrade
    {
        public string Name;
        public string Description;
        public Color Color;
        public Action<Player, Game1> ApplyEffect;

        public Upgrade(string name, string desc, Color color, Action<Player, Game1> effect)
        {
            Name = name;
            Description = desc;
            Color = color;
            ApplyEffect = effect;
        }
    }
}