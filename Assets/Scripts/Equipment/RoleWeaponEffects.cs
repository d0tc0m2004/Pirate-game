using UnityEngine;
using TacticalGame.Enums;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Defines the 3 weapon relic effects for each role.
    /// Each role has: Common (Effect 1), Uncommon (Effect 2), Rare (Effect 3)
    /// </summary>
    [CreateAssetMenu(fileName = "New Role Effects", menuName = "Tactical/Equipment/Role Weapon Effects")]
    public class RoleWeaponEffects : ScriptableObject
    {
        [Header("Role")]
        public UnitRole role;

        [Header("Effect 1 - Common (On-Hit)")]
        public string effect1Name;
        [TextArea(2, 4)]
        public string effect1Description;
        public WeaponRelicEffectType effect1Type;
        public float effect1Value1;
        public float effect1Value2;
        public int effect1Duration;

        [Header("Effect 2 - Uncommon (On-Hit, +20% Base Damage)")]
        public string effect2Name;
        [TextArea(2, 4)]
        public string effect2Description;
        public WeaponRelicEffectType effect2Type;
        public float effect2Value1;
        public float effect2Value2;
        public int effect2Duration;
        public float effect2BonusDamage = 0.20f; // +20% base damage

        [Header("Effect 3 - Rare (On-Hit, +40% Base Damage)")]
        public string effect3Name;
        [TextArea(2, 4)]
        public string effect3Description;
        public WeaponRelicEffectType effect3Type;
        public float effect3Value1;
        public float effect3Value2;
        public int effect3Duration;
        public float effect3BonusDamage = 0.40f; // +40% base damage

        /// <summary>
        /// Get effect data by rarity tier (1, 2, or 3).
        /// </summary>
        public WeaponRelicEffectData GetEffect(int tier)
        {
            return tier switch
            {
                1 => new WeaponRelicEffectData
                {
                    effectName = effect1Name,
                    description = effect1Description,
                    effectType = effect1Type,
                    value1 = effect1Value1,
                    value2 = effect1Value2,
                    duration = effect1Duration,
                    bonusDamagePercent = 0f,
                    rarity = RelicRarity.Common
                },
                2 => new WeaponRelicEffectData
                {
                    effectName = effect2Name,
                    description = effect2Description,
                    effectType = effect2Type,
                    value1 = effect2Value1,
                    value2 = effect2Value2,
                    duration = effect2Duration,
                    bonusDamagePercent = effect2BonusDamage,
                    rarity = RelicRarity.Uncommon
                },
                3 => new WeaponRelicEffectData
                {
                    effectName = effect3Name,
                    description = effect3Description,
                    effectType = effect3Type,
                    value1 = effect3Value1,
                    value2 = effect3Value2,
                    duration = effect3Duration,
                    bonusDamagePercent = effect3BonusDamage,
                    rarity = RelicRarity.Rare
                },
                _ => GetEffect(1)
            };
        }
    }

    /// <summary>
    /// Data container for a single weapon relic effect.
    /// </summary>
    [System.Serializable]
    public struct WeaponRelicEffectData
    {
        public string effectName;
        public string description;
        public WeaponRelicEffectType effectType;
        public float value1;
        public float value2;
        public int duration;
        public float bonusDamagePercent;
        public RelicRarity rarity;

        public string GetRarityName()
        {
            return rarity switch
            {
                RelicRarity.Common => "Common",
                RelicRarity.Uncommon => "Uncommon",
                RelicRarity.Rare => "Rare",
                _ => "Common"
            };
        }
    }

    /// <summary>
    /// Types of weapon relic effects (on-hit effects).
    /// </summary>
    public enum WeaponRelicEffectType
    {
        None,

        // Captain Effects
        RestoreEnergyOnKill,        // Effect 1: Restore 1 energy on kill/surrender
        BonusDamagePerUnspentEnergy, // Effect 2: +20% damage per unspent energy
        MarkTargetRestoreEnergy,    // Effect 3: Mark target, if hit again restore 1 energy

        // Quartermaster Effects
        StealMorale,                // Effect 1: Steal 50 morale
        BonusDamageIfLowerMorale,   // Effect 2: +20% if target has lower morale
        RestoreMoraleToAllies,      // Effect 3: Restore morale to all allies (20% of current)

        // Helmsmaster Effects
        IncreaseBuzzMeter,          // Effect 1: Increase enemy Buzz by 25%
        BonusDamageByBuzzState,     // Effect 2: Bonus damage based on enemy buzz
        ApplyMissDebuff,            // Effect 3: 50% miss chance for 2 turns

        // Boatswain Effects
        StealHealth,                // Effect 1: Steal 10% health
        BonusDamageByProximity,     // Effect 2: Bonus damage based on distance
        DamageBasedOnHealthPercent, // Effect 3: +20% damage from total health

        // Shipwright Effects
        ReduceEnemyGrit,            // Effect 1: Reduce enemy Grit on hit
        BonusDamageByMissingHealth, // Effect 2: Bonus based on own missing health
        GainGritDealBonusDamage,    // Effect 3: Gain 50% of unit grit, deal bonus damage

        // Master Gunner Effects
        GainPrimaryStatOnKill,      // Effect 1: Gain 10% primary stat on kill
        ReuseAbilityOnKill,         // Effect 2: Can re-use ability if unit dies
        BonusDamagePerStowedWeapon, // Effect 3: +20% per time weapon was stowed

        // Master-at-Arms Effects
        ReduceWeaponRelicCost,      // Effect 1: Reduce other weapon relic cost by 1
        BonusDamageIfNotMoved,      // Effect 2: +20% if unit didn't move last turn
        ExecuteLowHealth,           // Effect 3: Execute targets below 15% health

        // Navigator Effects
        BonusDamageSameRow,         // Effect 1: +20% to targets on same row
        BonusDamageIfTargetMoved,   // Effect 2: +40% if target moved last turn
        CreateHazardAtTarget,       // Effect 3: Create random hazard at target

        // Surgeon Effects
        HealClosestAlly,            // Effect 1: Restore 150 health to closest ally
        ApplyHealBlock,             // Effect 2: Target can't heal for 2 turns
        BonusDamagePerAllyInRadius, // Effect 3: +20% per allied unit in 1 tile

        // Cook Effects
        ApplyFireDebuff,            // Effect 1: Set target on fire for 4 turns
        BonusDamagePerDebuff,       // Effect 2: +10% per unique debuff on target
        ApplyDebuffWithHealthDamage, // Effect 3: Debuff + 30% current health damage if moved

        // Swashbuckler Effects
        FreeCostIfFirst,            // Effect 1: Cost is 0 if first attack in turn
        BonusDamageIfLowerSpeed,    // Effect 2: +20% if target has lower speed
        BonusDamageIfFirstAttack,   // Effect 3: +50% if first attack in turn

        // Deckhand Effects
        RestoreHull,                // Effect 1: Restore 30 Hull
        ReduceEnemyEnergyOnHullBreak, // Effect 2: Enemy loses 1 energy if hull breaks
        BonusDamageNoHull           // Effect 3: +40% to targets with no hull
    }
}