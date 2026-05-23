using ChaosFramework.Collections.Immutable;
using ChaosUtil.Reflection;

namespace LD44.Components.Weapons
{
    using SpellType = TypeWithAttribute<Components.Weapons.SpellAttribute>;

    public class SpellAttribute : ShopItem
    {
        public static readonly ImmutableArray<SpellType> spells;

        static SpellAttribute()
        {
            SpellType[] spells = SpellType.GetTypes(typeof(Components.Weapons.OffHand));
            System.Array.Sort(spells, Compare);
            SpellAttribute.spells = spells;
        }

        public readonly float damage, drain, castingTime, coolDown;

        public SpellAttribute(
            string displayName,
            string description,
            float damage,
            float price,
            float drain,
            float castingTime,
            float coolDown
            ) : base(price, displayName, description)
        {
            this.damage = damage;
            this.drain = drain;
            this.castingTime = castingTime;
            this.coolDown = coolDown;
        }

        protected override string GenerateShopText()
        {
            System.Text.StringBuilder txt = new System.Text.StringBuilder();
            txt.AppendLine(displayName);
            txt.AppendLine();
            txt.AppendLine("Damage: " + damage);
            txt.AppendLine("Casting Time: " + castingTime);
            txt.AppendLine("Cooldown: " + coolDown);
            txt.AppendLine("Life cost: " + drain);
            txt.AppendLine();
            txt.Append(description);
            return txt.ToString();
        }
    }
}
