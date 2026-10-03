// =====================================================================
//  SkillData.cs - skill-urile eroilor. Fiecare din cei 12 eroi are 3 skill-uri
//  proprii (combinatie diferita la fiecare erou), pe tastele Z / X / C
//  sau din bara de skill-uri de pe HUD. Damage-ul creste cu damage-ul tau.
// =====================================================================
using UnityEngine;

namespace ColorRPG
{
    public enum SkillKind
    {
        Whirlwind, ShieldWall, GroundSlam, LeapSlam, ArrowRain, DashStrike, HealingLight,
        Fireball, Meteor, FlameRing, IceShard, FrostNova, Blizzard, ChainLightning,
        ThunderClap, Blink, PoisonCloud, BattleCry, ShadowFury, LifeDrain, HolyNova
    }

    public class SkillDef
    {
        public SkillKind kind;
        public string name, desc;
        public float cooldown;
        public Color col;
        public int icon;   // iconita din Resources/UI/IconArt
    }

    public static class SkillDB
    {
        static SkillDef[] defs;

        public static SkillDef Get(SkillKind k)
        {
            if (defs == null) Build();
            return defs[(int)k];
        }

        static void Add(SkillKind k, string n, float cd, Color c, string desc)
        {
            defs[(int)k] = new SkillDef { kind = k, name = n, cooldown = cd, col = c, desc = desc, icon = 100 + (int)k };
        }

        static void Build()
        {
            defs = new SkillDef[System.Enum.GetValues(typeof(SkillKind)).Length];
            Color red = new Color(1f, 0.4f, 0.3f), fire = new Color(1f, 0.55f, 0.15f), ice = new Color(0.55f, 0.85f, 1f), holy = new Color(1f, 0.92f, 0.5f),
                  green = new Color(0.45f, 1f, 0.45f), purple = new Color(0.75f, 0.45f, 1f), bolt = new Color(1f, 0.95f, 0.4f), steel = new Color(0.8f, 0.85f, 0.95f);
            Add(SkillKind.Whirlwind, "Whirlwind", 7f, steel, "Spin with your weapon and hit everything around you twice.");
            Add(SkillKind.ShieldWall, "Shield Wall", 16f, steel, "Take 65% less damage for 6 seconds.");
            Add(SkillKind.GroundSlam, "Ground Slam", 9f, new Color(0.85f, 0.7f, 0.5f), "Jump and smash the ground - big damage around you, enemies are stunned.");
            Add(SkillKind.LeapSlam, "Leap Slam", 10f, red, "Leap forward and crash down on your enemies.");
            Add(SkillKind.ArrowRain, "Arrow Rain", 11f, new Color(0.85f, 0.75f, 0.5f), "Arrows rain down on your target's area for 2 seconds.");
            Add(SkillKind.DashStrike, "Dash Strike", 6f, steel, "Dash forward and slash every enemy in your path.");
            Add(SkillKind.HealingLight, "Healing Light", 18f, green, "Heal 35% of your HP.");
            Add(SkillKind.Fireball, "Fireball", 5f, fire, "Throw a fireball that explodes on impact.");
            Add(SkillKind.Meteor, "Meteor", 16f, fire, "Call a meteor from the sky - huge damage in a big area.");
            Add(SkillKind.FlameRing, "Flame Ring", 10f, fire, "A ring of fire bursts out around you.");
            Add(SkillKind.IceShard, "Ice Shards", 6f, ice, "Throw 3 ice shards that slow the enemies they hit.");
            Add(SkillKind.FrostNova, "Frost Nova", 11f, ice, "Freeze everything around you - damage and slow.");
            Add(SkillKind.Blizzard, "Blizzard", 14f, ice, "An ice storm hits the area for 3 seconds and slows enemies.");
            Add(SkillKind.ChainLightning, "Chain Lightning", 7f, bolt, "Lightning jumps between up to 5 enemies.");
            Add(SkillKind.ThunderClap, "Thunder Clap", 10f, bolt, "A thunder blast around you that stuns enemies.");
            Add(SkillKind.Blink, "Blink", 8f, purple, "Teleport forward and damage the enemies where you land.");
            Add(SkillKind.PoisonCloud, "Poison Cloud", 12f, green, "A poison cloud that damages enemies for 4 seconds.");
            Add(SkillKind.BattleCry, "Battle Cry", 18f, red, "+50% damage for 8 seconds.");
            Add(SkillKind.ShadowFury, "Shadow Fury", 16f, purple, "Every hit is a critical hit for 5 seconds.");
            Add(SkillKind.LifeDrain, "Life Drain", 9f, new Color(0.9f, 0.2f, 0.3f), "Drain the nearest enemy and heal for part of the damage.");
            Add(SkillKind.HolyNova, "Holy Nova", 12f, holy, "Holy light damages enemies around you and heals you.");
        }

        /// <summary>Cele 3 skill-uri ale fiecarui erou (index in GameData.Heroes). Fiecare erou are combinatia lui.</summary>
        public static readonly SkillKind[][] HeroSkills =
        {
            new[] { SkillKind.Whirlwind, SkillKind.ShieldWall, SkillKind.GroundSlam },       // Knight
            new[] { SkillKind.ArrowRain, SkillKind.DashStrike, SkillKind.HealingLight },     // Ranger
            new[] { SkillKind.Fireball, SkillKind.FlameRing, SkillKind.Meteor },             // Pyromancer
            new[] { SkillKind.IceShard, SkillKind.FrostNova, SkillKind.Blizzard },           // Frost Mage
            new[] { SkillKind.ChainLightning, SkillKind.ThunderClap, SkillKind.Blink },      // Storm Caller
            new[] { SkillKind.DashStrike, SkillKind.PoisonCloud, SkillKind.ShadowFury },     // Assassin
            new[] { SkillKind.BattleCry, SkillKind.LeapSlam, SkillKind.Whirlwind },          // Berserker
            new[] { SkillKind.HealingLight, SkillKind.HolyNova, SkillKind.ShieldWall },      // Priestess
            new[] { SkillKind.LifeDrain, SkillKind.PoisonCloud, SkillKind.Meteor },          // Warlock
            new[] { SkillKind.DashStrike, SkillKind.ThunderClap, SkillKind.GroundSlam },     // Monk
            new[] { SkillKind.HolyNova, SkillKind.LeapSlam, SkillKind.BattleCry },           // Paladin
            new[] { SkillKind.ChainLightning, SkillKind.IceShard, SkillKind.Blink },         // Sorceress
        };

        public static readonly string[] HeroTitles =
        {
            "Knight", "Ranger", "Pyromancer", "Frost Mage", "Storm Caller", "Assassin",
            "Berserker", "Priestess", "Warlock", "Monk", "Paladin", "Sorceress"
        };

        public static SkillKind[] ForHero(int hero) { return HeroSkills[Mathf.Clamp(hero, 0, HeroSkills.Length - 1)]; }
        /// <summary>Textul din chenarul de hover al unui skill.</summary>
        public static string Tooltip(SkillKind k, string key)
        {
            var d = Get(k);
            return "<size=28><color=#" + ColorUtility.ToHtmlStringRGB(Color.Lerp(d.col, Color.white, 0.3f)) + ">" + d.name + "</color></size>\n" +
                   "<color=#9aa6c0>" + (string.IsNullOrEmpty(key) ? "Tap to use" : "Key " + key) + "   -   Cooldown " + d.cooldown.ToString("0") + "s</color>\n\n" + d.desc;
        }

        public static string HeroTitle(int hero) { return HeroTitles[Mathf.Clamp(hero, 0, HeroTitles.Length - 1)]; }
    }
}
