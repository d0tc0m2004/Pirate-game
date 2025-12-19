using System;
using TacticalGame.Enums;

namespace TacticalGame.Units
{
    /// <summary>
    /// Data container for unit stats. Used during character creation and initialization.
    /// </summary>
    [Serializable]
    public class UnitData
    {
        public string unitName;
        public UnitRole role;
        public Team team;
        public WeaponType weaponType;

        // Primary and Secondary stat tracking
        public StatType primaryStat;
        public StatType secondaryPrimaryStat; // Only used for Captain (second primary)
        public StatType secondaryStat;
        public bool hasTwoPrimaryStats; // True for Captain

        // Core Stats
        public int health;
        public int morale;
        public int buzz;
        public int power;
        public int aim;
        public int tactics;
        public int skill;
        public int proficiency; // Stored as percentage (e.g., 150 = 1.5x)
        public int grit;
        public int hull;
        public int speed;

        /// <summary>
        /// Get proficiency as a multiplier (e.g., 1.5 for 150%).
        /// </summary>
        public float ProficiencyMultiplier => proficiency / 100f;

        /// <summary>
        /// Create a copy of this unit data.
        /// </summary>
        public UnitData Clone()
        {
            return new UnitData
            {
                unitName = this.unitName,
                role = this.role,
                team = this.team,
                weaponType = this.weaponType,
                primaryStat = this.primaryStat,
                secondaryPrimaryStat = this.secondaryPrimaryStat,
                secondaryStat = this.secondaryStat,
                hasTwoPrimaryStats = this.hasTwoPrimaryStats,
                health = this.health,
                morale = this.morale,
                buzz = this.buzz,
                power = this.power,
                aim = this.aim,
                tactics = this.tactics,
                skill = this.skill,
                proficiency = this.proficiency,
                grit = this.grit,
                hull = this.hull,
                speed = this.speed
            };
        }

        /// <summary>
        /// Get a display name for the role.
        /// </summary>
        public string GetRoleDisplayName()
        {
            return role switch
            {
                UnitRole.MasterGunner => "Master Gunner",
                UnitRole.MasterAtArms => "Master-at-Arms",
                _ => role.ToString()
            };
        }

        /// <summary>
        /// Get the stat value by StatType.
        /// </summary>
        public int GetStat(StatType statType)
        {
            return statType switch
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

        /// <summary>
        /// Set the stat value by StatType.
        /// </summary>
        public void SetStat(StatType statType, int value)
        {
            switch (statType)
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
    }
}