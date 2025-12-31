using System;
using TacticalGame.Equipment;
using TacticalGame.Enums;

namespace TacticalGame.Units
{
    /// <summary>
    /// Data container for a unit's stats and equipment.
    /// </summary>
    [Serializable]
    public class UnitData
    {
        // Identity
        public string unitName;
        public UnitRole role;
        public Team team;

        // Weapon
        public WeaponType weaponType;
        public WeaponFamily weaponFamily;

        // Primary/Secondary tracking
        public StatType primaryStat;
        public StatType secondaryPrimaryStat;
        public StatType secondaryStat;
        public bool hasTwoPrimaryStats;

        // Stats
        public int health;
        public int morale;
        public int buzz;
        public int power;
        public int aim;
        public int tactics;
        public int skill;
        public int proficiency;
        public int grit;
        public int hull;
        public int speed;

        // Equipment - the default weapon relic from character creation
        [NonSerialized] public WeaponRelic defaultWeaponRelic;
        
        // Full equipment data (6 slots with jewels)
        [NonSerialized] public UnitEquipmentData equipment;

        // Display Helpers
        public string GetRoleDisplayName()
        {
            return role switch
            {
                UnitRole.MasterGunner => "Master Gunner",
                UnitRole.MasterAtArms => "Master-at-Arms",
                _ => role.ToString()
            };
        }

        public string GetWeaponFamilyDisplayName()
        {
            return weaponFamily switch
            {
                WeaponFamily.BoardingPike => "Boarding Pike",
                WeaponFamily.CursedBird => "Cursed Bird",
                WeaponFamily.CursedMonkey => "Cursed Monkey",
                _ => weaponFamily.ToString()
            };
        }

        public float GetProficiencyMultiplier() => proficiency / 100f;

        public void SetStat(StatType stat, int value)
        {
            switch (stat)
            {
                case StatType.Health: health = value; break;
                case StatType.Morale: morale = value; break;
                case StatType.Buzz: buzz = value; break;
                case StatType.Power: power = value; break;
                case StatType.Aim: aim = value; break;
                case StatType.Tactics: tactics = value; break;
                case StatType.Skill: skill = value; break;
                case StatType.Proficiency: proficiency = value; break;
                case StatType.Grit: grit = value; break;
                case StatType.Hull: hull = value; break;
                case StatType.Speed: speed = value; break;
            }
        }

        public int GetStat(StatType stat)
        {
            return stat switch
            {
                StatType.Health => health,
                StatType.Morale => morale,
                StatType.Buzz => buzz,
                StatType.Power => power,
                StatType.Aim => aim,
                StatType.Tactics => tactics,
                StatType.Skill => skill,
                StatType.Proficiency => proficiency,
                StatType.Grit => grit,
                StatType.Hull => hull,
                StatType.Speed => speed,
                _ => 0
            };
        }

        public bool HasRoleMatchingRelic()
        {
            return defaultWeaponRelic != null && defaultWeaponRelic.MatchesRole(role);
        }
    }
}