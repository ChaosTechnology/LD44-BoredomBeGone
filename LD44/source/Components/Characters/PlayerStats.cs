using ChaosFramework.Collections;
using ChaosFramework.Collections.Immutable;
using System.Reflection;
using SysCol = System.Collections.Generic;

namespace LD44.Components.Characters
{
    public class PlayerStats
    {
        // TODO: seriously question why this is all FieldInfo based!

        public const float LEVEL_BASE_PRICE = 20;
        public const float max_agility = 2.5f;

        public static readonly ImmutableArray<string> skills;
        static readonly SysCol.Dictionary<string, string> descriptions;

        static PlayerStats()
        {
            LinkedList<string> lst = new LinkedList<string>();
            foreach (FieldInfo info in typeof(PlayerStats).GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (info.FieldType == typeof(float))
                    lst.Add(info.Name);
            skills = lst.ToArray();

            descriptions = new SysCol.Dictionary<string, string>();
            descriptions[nameof(agility)] = Properties.Resources.Skill_Agility;
            descriptions[nameof(constitution)] = Properties.Resources.Skill_Constitution;
            descriptions[nameof(strength)] = Properties.Resources.Skill_Strength;
            descriptions[nameof(magic)] = Properties.Resources.Skill_Magic;
        }

        public static string GetDescription(string skill) => descriptions[skill];

        public float agility = 1f;
        public float constitution = 1f;
        public float strength = 1f;
        public float magic = 1f;

        public LinkedList<System.Type> knownSpells = new LinkedList<System.Type>();
        public LinkedList<string> accessibleWeapons = new LinkedList<string>();

        readonly StickMan stickMan;

        public float speedFactor => 2.5f;
        public float walkingSpeed => (agility + .4f) * 6f;
        public float jumpLaunch => agility * 3;

        public float swingStrength => 0.5f * (float)System.Math.Sqrt(strength);
        public float physicalDamage => strength * strength;
        public float magicDamage => magic * magic;

        public float physicalResistance => constitution * strength;
        public float magicResistance => constitution * magic;

        public float lifeDrainStamina => 5 / constitution;
        public float lifeDrainMagic => stickMan.brain is Brains.Enemy ? 0 : 1 / magic;

        public PlayerStats(
            StickMan stickMan,
            float agility = 1,
            float constitution = 1,
            float strength = 1,
            float magic = 1
            )
        {
            this.stickMan = stickMan;
            this.agility = agility;
            this.constitution = constitution;
            this.strength = strength;
            this.magic = magic;
        }

        FieldInfo GetSkillField(string skill)
        {
            FieldInfo info = typeof(PlayerStats).GetField(skill, BindingFlags.Public | BindingFlags.Instance);
            if (info == null)
                throw new System.Exception("No skill named \"" + skill + "\" found.");
            if (info.FieldType != typeof(float))
                throw new System.Exception("You cannot level \"" + skill + "\".");

            return info;
        }

        public void Level(string skill)
        {
            FieldInfo info = GetSkillField(skill);
            info.SetValue(this, (float)info.GetValue(this) + 0.1f);
        }

        public int GetPrice(string skill)
            => GetPrice(GetSkill(skill));

        public int GetPrice(float level)
            => (int)(LEVEL_BASE_PRICE * System.Math.Pow(13, level - 1));

        public float GetSkill(string skill)
            => (float)GetSkillField(skill).GetValue(this);

        public float GetSkillMaximum(string skill)
        {
            FieldInfo info = typeof(PlayerStats).GetField("max_" + skill, BindingFlags.Public | BindingFlags.Static);
            if (info == null) return float.MaxValue;
            if (info.FieldType != typeof(float)) return float.MaxValue;
            return (float)info.GetValue(this);
        }
    }
}
