using FrostyPlatformer.Models.Objects;
using PixelEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostyPlatformer.Models.Items
{
    public class Item
    {
        public Item(string name, Sprite sprite, string desc)
        {
            Name = name;
            Sprite = sprite;
            Description = desc;
        }

        public virtual bool OnInteract(DynamicGameObject myobject) { return false; }
        public virtual bool OnUse(DynamicGameObject myobject) { return false; }


        public string Name { get; set; }
        public string Description { get; set; }
        public Sprite Sprite { get; set; }
        public bool KeyItem { get; set; } = false;
        public bool Equipable { get; set; } = false;

    };



    class ItemEnergi : Item
    {

        public ItemEnergi() : base("Energi", Core.Aggregate.Instance.GetSprite("items"), "Add Energi")
        {

        }
        public override bool OnInteract(DynamicGameObject myobject)
        {
            OnUse(myobject);
            return false; // Absorb
        }
        public override bool OnUse(DynamicGameObject myobject)
        {
            if (myobject != null)
            {
                Creature dyn = (Creature)myobject;
                dyn.Health = Math.Min(dyn.Health + 1, dyn.MaxHealth); // Not more then full
            }
            return true; // remove
        }
    };

  
}
