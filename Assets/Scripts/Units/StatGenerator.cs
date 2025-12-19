using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Enums;
using TacticalGame.Config;

namespace TacticalGame.Units
{
    /// <summary>
    /// Handles stat generation for units based on role, primary/secondary stats.
    /// </summary>
    public static class StatGenerator
    {
        // All possible stats that can be primary/secondary
        private static readonly StatType[] AllStats = 
        {
            StatType.Health,
            StatType.Morale,
            StatType.Buzz,
            StatType.Power,
            StatType.Aim,
            StatType.Tactics,
            StatType.Skill,
            StatType.Proficiency,
            StatType.Grit,
            StatType.Hull,
            StatType.Speed
        };

        /// <summary>
        /// Get the primary stat for a role. Captain returns a random stat.
        /// </summary>
        public static StatType GetPrimaryStatForRole(UnitRole role)
        {
            return role switch
            {
                UnitRole.Captain => GetRandomStat(), // Captain gets random primary
                UnitRole.Quartermaster => StatType.Morale,
                UnitRole.Helmsmaster => StatType.Buzz,
                UnitRole.Boatswain => StatType.Health,
                UnitRole.Shipwright => StatType.Grit,
                UnitRole.MasterGunner => StatType.Aim,
                UnitRole.MasterAtArms => StatType.Power,
                UnitRole.Navigator => StatType.Tactics,
                UnitRole.Surgeon => StatType.Skill,
                UnitRole.Cook => StatType.Proficiency,
                UnitRole.Swashbuckler => StatType.Speed,
                UnitRole.Deckhand => StatType.Hull,
                _ => StatType.Power
            };
        }

        /// <summary>
        /// Get a random stat from all available stats.
        /// </summary>
        public static StatType GetRandomStat()
        {
            return AllStats[Random.Range(0, AllStats.Length)];
        }

        /// <summary>
        /// Get a random stat excluding the specified stats.
        /// </summary>
        public static StatType GetRandomStatExcluding(params StatType[] excludeStats)
        {
            var available = AllStats.Where(s => !excludeStats.Contains(s)).ToArray();
            if (available.Length == 0) return AllStats[0];
            return available[Random.Range(0, available.Length)];
        }

        /// <summary>
        /// Generate all stats for a unit based on role.
        /// </summary>
        public static UnitData GenerateStats(UnitRole role, Team team)
        {
            var config = GameConfig.Instance;
            var data = new UnitData
            {
                role = role,
                team = team,
                unitName = $"{team}_{role}"
            };

            // Determine primary stat(s)
            if (role == UnitRole.Captain)
            {
                // Captain gets 2 random primary stats (no duplicates)
                data.hasTwoPrimaryStats = true;
                data.primaryStat = GetRandomStat();
                data.secondaryPrimaryStat = GetRandomStatExcluding(data.primaryStat);
                data.secondaryStat = GetRandomStatExcluding(data.primaryStat, data.secondaryPrimaryStat);
            }
            else
            {
                // Normal role: fixed primary, random secondary
                data.hasTwoPrimaryStats = false;
                data.primaryStat = GetPrimaryStatForRole(role);
                data.secondaryStat = GetRandomStatExcluding(data.primaryStat);
            }

            // Generate each stat based on whether it's primary, secondary, or low
            foreach (StatType stat in AllStats)
            {
                StatRangeType rangeType = GetRangeTypeForStat(data, stat);
                int value = RollStat(stat, rangeType, config);
                data.SetStat(stat, value);
            }

            // Determine weapon type based on role
            data.weaponType = DetermineWeaponType(role);

            return data;
        }

        /// <summary>
        /// Determine which range type to use for a given stat.
        /// </summary>
        private static StatRangeType GetRangeTypeForStat(UnitData data, StatType stat)
        {
            // Check if it's a primary stat
            if (stat == data.primaryStat)
                return StatRangeType.High;
            
            // Check if it's the second primary (Captain only)
            if (data.hasTwoPrimaryStats && stat == data.secondaryPrimaryStat)
                return StatRangeType.High;
            
            // Check if it's the secondary stat
            if (stat == data.secondaryStat)
                return StatRangeType.Mid;
            
            // Otherwise it's low range
            return StatRangeType.Low;
        }

        /// <summary>
        /// Roll a stat value based on stat type and range.
        /// </summary>
        private static int RollStat(StatType stat, StatRangeType rangeType, GameConfig config)
        {
            var range = GetRangeForStat(stat, rangeType, config);
            return Random.Range(range.min, range.max + 1);
        }

        /// <summary>
        /// Get the min/max range for a stat and range type.
        /// </summary>
        private static (int min, int max) GetRangeForStat(StatType stat, StatRangeType rangeType, GameConfig config)
        {
            return stat switch
            {
                StatType.Health => config.GetHealthRange(rangeType),
                StatType.Morale => config.GetMoraleRange(rangeType),
                StatType.Buzz => config.GetBuzzRange(rangeType),
                StatType.Power => config.GetPowerRange(rangeType),
                StatType.Aim => config.GetAimRange(rangeType),
                StatType.Tactics => config.GetTacticsRange(rangeType),
                StatType.Skill => config.GetSkillRange(rangeType),
                StatType.Proficiency => config.GetProficiencyRange(rangeType),
                StatType.Grit => config.GetGritRange(rangeType),
                StatType.Hull => config.GetHullRange(rangeType),
                StatType.Speed => config.GetSpeedRange(rangeType),
                _ => (0, 0)
            };
        }

        /// <summary>
        /// Determine weapon type based on role.
        /// </summary>
        private static WeaponType DetermineWeaponType(UnitRole role)
        {
            return role switch
            {
                UnitRole.MasterAtArms => WeaponType.Melee,      // Melee only
                UnitRole.MasterGunner => WeaponType.Ranged,     // Ranged only
                _ => Random.value > 0.5f ? WeaponType.Melee : WeaponType.Ranged  // Random for others
            };
        }

        /// <summary>
        /// Get a display string for the stat's range type.
        /// </summary>
        public static string GetRangeTypeDisplay(UnitData data, StatType stat)
        {
            var rangeType = GetRangeTypeForStat(data, stat);
            return rangeType switch
            {
                StatRangeType.High => "(Primary)",
                StatRangeType.Mid => "(Secondary)",
                StatRangeType.Low => "",
                _ => ""
            };
        }
    }
}