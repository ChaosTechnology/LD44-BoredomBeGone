using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Collections;
using ChaosFramework.Collections.Immutable;
using ChaosFramework.Components;
using ChaosFramework.Physics;
using ChaosFramework.Physics.States;
using ChaosFramework.Shapes.Convex;
using ChaosUtil.Primitives;
using ChaosUtil.Reflection;
using System;
using SysCol = System.Collections.Generic;
using BindingFlags = System.Reflection.BindingFlags;

namespace LD44.Components.Weapons
{
    partial class Weapon
    {
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        public class WeaponAttribute : ShopItem
        {
            public struct WeaponPart
            {
                public enum DamageType
                {
                    Blunt,
                    Strike,
                    Pierce
                }

                public readonly DamageType damageType;
                public readonly float damage;
                public readonly bool infusable;

                public WeaponPart(string[] file)
                {
                    if (!Enum.TryParse(file[0].Trim(), out damageType) ||
                        !float.TryParse(file[1].Trim(), out damage) ||
                        !bool.TryParse(file[2].Trim(), out infusable))
                        throw new Exception("invalid weapon meta data");
                }
            }

            public enum SwipeDirection
            {
                Left,
                Right,
                Both,
                Any
            }

            public static readonly ImmutableArray<string> names;
            public static readonly ImmutableArray<string> gruntNames;
            static readonly SysCol.Dictionary<string, WeaponAttribute> attrs = new SysCol.Dictionary<string, WeaponAttribute>();

            static WeaponAttribute()
            {
                LinkedList<string> nameLst = new LinkedList<string>();
                LinkedList<string> gruntNameLst = new LinkedList<string>();
                foreach (WeaponAttribute attr in typeof(Weapon).GetCustomAttributes(typeof(WeaponAttribute), false))
                {
                    attrs[attr.displayName] = attr;
                    nameLst.Add(attr.displayName);
                    if (attr.forGrunts)
                        gruntNameLst.Add(attr.displayName);
                }
                nameLst.Sort(CompareByName);
                gruntNameLst.Sort(CompareByName);
                names = nameLst.ToArray();
                gruntNames = gruntNameLst.ToArray();
            }

            static int CompareByName(string a, string b)
                => Compare(attrs[a], attrs[b]);

            public static bool TryGetAttribute(string weaponName, out WeaponAttribute attr)
                => attrs.TryGetValue(weaponName, out attr);

            public static SysCol.IEnumerable<SysCol.KeyValuePair<string, WeaponAttribute>> Enumerate()
                => attrs;

            public readonly string mesh, material, physics;
            public readonly SwipeDirection swipeDir;
            public readonly float damage, weight;
            public readonly bool forGrunts;

            readonly SysCol.Dictionary<string, WeaponPart> parts = new SysCol.Dictionary<string, WeaponPart>();

            public WeaponAttribute(
                string displayName,
                string mesh,
                string material,
                string physics,
                SwipeDirection swipeDir,
                float damage,
                float weight,
                float price,
                string metadata,
                bool forGrunts = true
                )
                : base(
                    price,
                    displayName,
                    Properties.Resources.ResourceManager.GetString("Descr_" + displayName.Replace(" ", ""))
                    )
            {
                this.mesh = $"Models/Weapons/{mesh}.gmdl";
                this.material = $"Materials/Weapons/{material}.mat";
                this.physics = $"Models/Weapons/{physics}.obj";
                this.swipeDir = swipeDir;
                this.damage = damage;
                this.weight = weight;
                this.forGrunts = forGrunts;

                string text = (string)typeof(Properties.Resources).GetProperty("Weapon_" + metadata, BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                string[] lines = text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if ((lines.Length % 4) != 0)
                    throw new Exception("invalid weapon meta data");
                for (int i = 0; i < lines.Length; i += 4)
                    parts[lines[i].Trim()] = new WeaponPart(lines.SubArray(i + 1, 3));
            }

            public bool TryGetPart(string partName, out WeaponPart part)
                => parts.TryGetValue(partName, out part);

            protected override string GenerateShopText()
            {
                System.Text.StringBuilder txt = new System.Text.StringBuilder();
                txt.AppendLine(displayName);
                txt.AppendLine();
                txt.AppendLine("Damage: " + damage);
                txt.AppendLine("Weight: " + weight);
                txt.AppendLine();
                txt.Append(description);
                return txt.ToString();
            }
        }
    }
}
