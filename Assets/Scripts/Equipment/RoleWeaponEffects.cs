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
    /// Types of weapon relic effects (on-hit effects) per v5 specs.
    /// </summary>
    public enum WeaponRelicEffectType
    {
        None,

        // V5 Spec Unified Role Effects
        SurgeonHealAlly,         // Surgeon: Restore HP to lowest-HP ally
        CookReduceTactics,       // Cook: -Tactics to enemy next turn
        NavigatorAddMove,        // Navigator: +Move next turn
        CaptainAddMorale,        // Captain: +Morale to all allies
        QuartermasterStealMorale,// Quartermaster: Steal Morale
        SwashbucklerAddSpeed,    // Swashbuckler: +Speed next turn
        BoatswainAddThreat,      // Boatswain: +Grid Threat generation
        MasterAtArmsAddCombo,    // Master-at-Arms: +Combo multiplier next turn
        MasterGunnerReduceAim,   // Master Gunner: -Aim to enemy next turn
        HelmsmasterAddBuzz,      // Helmsmaster: +Buzz to self
        ShipwrightRestoreHull,   // Shipwright: Restore Hull to self
        DeckhandReduceMove       // Deckhand: -Move to enemy next turn
    }
}