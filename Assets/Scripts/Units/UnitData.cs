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

        // Core Stats
        public int health;
        public int morale;
        public int grit;
        public int buzz;
        public int power;
        public int aim;
        public int proficiency;
        public int skill;
        public int tactics;
        public int speed;
        public int hull;

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
                health = this.health,
                morale = this.morale,
                grit = this.grit,
                buzz = this.buzz,
                power = this.power,
                aim = this.aim,
                proficiency = this.proficiency,
                skill = this.skill,
                tactics = this.tactics,
                speed = this.speed,
                hull = this.hull
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
                UnitRole.MasterAtArms => "Master-at-arms",
                _ => role.ToString()
            };
        }
    }
}