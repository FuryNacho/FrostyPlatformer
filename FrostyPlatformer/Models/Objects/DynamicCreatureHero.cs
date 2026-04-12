#nullable enable
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.Models.Objects
{
    public class DynamicCreatureHero : Creature
    {
        public bool LookUp { get; set; }
        public bool LookDown { get; set; }
        public DynamicCreatureHero() : base("hero", SpriteId.Hero)
        {
            Friendly = true;
            Health = 9;
            MaxHealth = 100;
            StateTick = 2.0f;
            IsHero = true;
        }

        /// <summary>
        /// Hjältens beteende styrs av Program.cs via IInputProvider — inget autonomt AI-beteende.
        /// </summary>
        public override void Behaviour(float fElapsedTime, DynamicGameObject? player = null) { }
    }
}
