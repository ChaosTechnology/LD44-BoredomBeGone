using ChaosUtil.Reflection;
using System;

namespace LD44.Components.Weapons
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public abstract class ShopItem : Attribute
    {
        public static int Compare<Ware>(Ware a, Ware b)
            where Ware : ShopItem
            => a.price.CompareTo(b.price);

        public static int Compare<Ware>(TypeWithAttribute<Ware> a, TypeWithAttribute<Ware> b)
            where Ware : ShopItem
            => Compare(a.attribute, b.attribute);

        public readonly float price;
        public readonly string displayName, description;

        string _shopText;
        public string shopText => _shopText ?? (_shopText = GenerateShopText());

        public ShopItem(float price, string displayName, string description)
        {
            this.price = price;
            this.displayName = displayName;
            this.description = description;
        }

        protected abstract string GenerateShopText();
    }
}
