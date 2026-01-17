using UnityEngine;
using TacticalGame.Enums;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Defines a single relic effect (category + role combination).
    /// Example: "Captain Boots" = Boots category + Captain role tag
    /// </summary>
    [System.Serializable]
    public class RelicEffectData
    {
        [Header("Identity")]
        public RelicCategory category;
        public UnitRole roleTag;
        
        [Header("Card Info")]
        public int copies = 2;          // How many cards in deck
        public int energyCost = 1;      // Cost to play
        public bool isPassive = false;  // If true, always active (no card)
        
        [Header("Effect")]
        public RelicEffectType effectType;
        [TextArea(2, 4)]
        public string description;
        
        [Header("Values")]
        public float value1;            // Primary value (damage %, tiles, etc.)
        public float value2;            // Secondary value
        public int duration;            // Effect duration in turns
        public int tileRange = 1;       // Tile radius for AoE effects
        
        /// <summary>
        /// Get display name like "Captain Boots"
        /// </summary>
        public string GetDisplayName()
        {
            string roleName = roleTag switch
            {
                UnitRole.MasterGunner => "Master Gunner",
                UnitRole.MasterAtArms => "Master-at-Arms",
                _ => roleTag.ToString()
            };
            return $"{roleName} {category}";
        }
        
        /// <summary>
        /// Check if this relic matches a unit's role (for Proficiency bonus).
        /// </summary>
        public bool MatchesRole(UnitRole unitRole)
        {
            return roleTag == unitRole;
        }
    }
    
    /// <summary>
    /// All possible relic effects.
    /// Organized by category for clarity.
    /// </summary>
    public enum RelicEffectType
    {
        None,
        
        // ========== BOOTS (Movement) ==========
        Boots_SwapWithUnit,              // Captain: Swap location with another unit
        Boots_MoveAlly,                  // Quartermaster: Move any allied unit 2 tiles
        Boots_MoveRestoreMorale,         // Helmsman: Move 2 tiles, restore 10% morale
        Boots_AllyFreeMoveLowestMorale,  // Boatswain: Lowest morale ally moves free
        Boots_MoveClearBuzz,             // Shipwright: Move 2 tiles, clear buzz
        Boots_FreeIfGrog,                // Master Gunner: Move 2, free if grog available
        Boots_MoveReduceDamage,          // Master-at-Arms: Move 2, 20% damage reduction
        Boots_MoveAnyIfHighestHP,        // Navigator: Move any distance if highest HP, else 2
        Boots_MoveToNeutral,             // Surgeon: Move to any tile in neutral zone
        Boots_MoveGainGrit,              // Cook: Move 2, gain 20% Grit for 2 turns
        Boots_MoveGainAim,               // Swashbuckler: Move 2, gain 50% Aim this turn
        Boots_MoveReduceRangedCost,      // Deckhand: Move 1, reduce next ranged cost by 1
        
        // ========== GLOVES (Attack + Effect) ==========
        Gloves_AttackReduceEnemyDraw,    // Captain: Attack, enemy draws 1 less
        Gloves_AttackIncreaseEnemyCost,  // Quartermaster: Attack, enemy next card +1 cost
        Gloves_AttackBonusByMissingMorale, // Helmsman: Attack, +dmg by enemy missing morale
        Gloves_AttackMarkMoraleFocus,    // Boatswain: Attack, mark for morale focus 2 turns
        Gloves_AttackPreventBuzzReduce,  // Shipwright: Attack, prevent buzz reduction 2 turns
        Gloves_AttackBonusPerGrog,       // Master Gunner: Attack, +20% per grog token
        Gloves_AttackBonusIfMoreHP,      // Master-at-Arms: Attack, +20% if more HP than target
        Gloves_AttackLowerEnemyHealth,   // Navigator: Attack, lower enemy health stat 30% for 2 turns
        Gloves_AttackPushForward,        // Surgeon: Attack, push target forward 1 tile
        Gloves_AttackForceTargetClosest, // Cook: Attack, debuff forces attack closest next turn
        Gloves_AttackBonusPerCardPlayed, // Swashbuckler: Attack, +10% per card played this round
        Gloves_AttackBonusPerGunnerRelic,// Deckhand: Attack, +10% per gunner relic used this game
        
        // ========== HAT (Resource/Card Manipulation) ==========
        Hat_DrawCardsVulnerable,         // Captain: Draw 2, take 200% damage for 2 turns
        Hat_DrawUltimate,                // Quartermaster: Draw an ultimate ability
        Hat_RestoreMoraleLowest,         // Helmsman: Restore 30% morale to lowest ally
        Hat_RestoreMoraleNearby,         // Boatswain: 10% morale to allies in 1 tile
        Hat_FreeRumUsage,                // Shipwright: 3 rum usage free this round
        Hat_GenerateGrog,                // Master Gunner: Generate 2 grog tokens
        Hat_ReturnDamage,                // Master-at-Arms: Return 1 damage instance for 2 turns
        Hat_IncreaseHealthStat,          // Navigator: +25% health stat for 2 turns
        Hat_EnergyOnKnockback,           // Surgeon: +2 energy if knocked back next turn
        Hat_SwapEnemyByGrit,             // Cook: Swap highest/lowest grit enemies
        Hat_WeaponUseTwice,              // Swashbuckler: Next weapon can be used twice
        Hat_DrawWeaponRelic,             // Deckhand: Draw a weapon relic
        
        // ========== COAT (Defensive) ==========
        Coat_BuffNearbyAimPower,         // Captain: +20% Aim/Power to allies in 1 tile
        Coat_DrawOnEnemyAttack,          // Quartermaster: Draw card per enemy attack, enemy discards
        Coat_ReduceMoraleDamage,         // Helmsman: Allies take 30% less morale damage 2 turns
        Coat_PreventSurrender,           // Boatswain: If ally would surrender, restore 20% morale
        Coat_ReduceRumEffect,            // Shipwright: Nearby allies reduced rum effect
        Coat_EnemyBuzzOnDamage,          // Master Gunner: Enemy buzz fills when dealing damage
        Coat_PreventDisplacement,        // Master-at-Arms: Allies can't be knocked back
        Coat_ProtectLowHP,               // Navigator: Lowest HP can only be targeted by lower HP
        Coat_RowCantBeTargeted,          // Surgeon: Allies behind in row can't be targeted
        Coat_ColumnDamageBoost,          // Cook: +40% damage to allies in same column
        Coat_FreeStow,                   // Swashbuckler: Next 2 stows free
        Coat_RowRangedProtection,        // Deckhand: Row takes 50% less ranged damage
        
        // ========== TRINKET (Passive) ==========
        Trinket_BonusDamagePerCard,      // Captain: +20% weapon damage per card in hand
        Trinket_BonusVsCaptain,          // Quartermaster: +20% damage vs enemy captain
        Trinket_ImmuneMoraleFocusFire,   // Helmsman: Immune to morale focus fire
        Trinket_EnemySurrenderEarly,     // Boatswain: Enemies surrender at 30% morale
        Trinket_DamageByBuzz,            // Shipwright: +damage based on own buzz
        Trinket_KnockbackIncreasesBuzz,  // Master Gunner: Knockback increases enemy buzz
        Trinket_ReduceDamageFromClosest, // Master-at-Arms: Closest enemy does -20% damage
        Trinket_DrawIfHighHP,            // Navigator: Draw extra if HP above 60%
        Trinket_TauntFirstAttack,        // Surgeon: Taunt first attack per enemy turn
        Trinket_KnockbackAttacker,       // Cook: Knockback attacker once per turn
        Trinket_RowEnemiesLessDamage,    // Swashbuckler: Enemies in row do -10% damage
        Trinket_RowEnemiesTakeMore,      // Deckhand: Enemies in row take +10% damage
        
        // ========== TOTEM (Summons/Curses) ==========
        Totem_SummonCannon,              // Captain: Summon cannon, 250 HP, attacks random
        Totem_CurseCaptainReflect,       // Quartermaster: Captain damage reflects to allies
        Totem_RallyNoMoraleDamage,       // Helmsman: Nearby allies no morale damage next turn
        Totem_EnemyDeathMoraleSwing,     // Boatswain: Enemy death = ally morale loss + player gain
        Totem_SummonHighQualityRum,      // Shipwright: Add 2 high quality rum
        Totem_ConvertGrogToEnergy,       // Master Gunner: Convert 2 grog to 1 energy
        Totem_StunOnKnockback,           // Master-at-Arms: If knocked back, stun attacker
        Totem_SummonAnchorHealthBuff,    // Navigator: Summon anchor, +25% health nearby
        Totem_SummonTargetDummy,         // Surgeon: Summon dummy in front row, 250 HP
        Totem_SummonObstacleDisplace,    // Cook: Summon obstacle at target, displace them
        Totem_SummonExplodingBarrels,    // Swashbuckler: 3 barrels explode after 2 turns
        Totem_CurseRangedWeapons,        // Deckhand: Enemy ranged -50% damage next turn
        
        // ========== ULTIMATE (Role-specific powerful) ==========
        Ultimate_ShipCannon,             // Captain: 3 shots, 200 dmg, fire hazard
        Ultimate_MarkCaptainOnly,        // Quartermaster: Attack captain, only target this turn
        Ultimate_ReflectMoraleDamage,    // Helmsman: Reflect morale damage to enemies
        Ultimate_ReviveAlly,             // Boatswain: Revive dead/surrendered at 30%
        Ultimate_FullBuzzAttack,         // Shipwright: Attack, target buzz full 2 turns
        Ultimate_RumBottleAoE,           // Master Gunner: 200 dmg AoE, rum spill zone
        Ultimate_SummonHardObstacles,    // Master-at-Arms: 3 hard obstacles front row
        Ultimate_IgnoreHighestHP,        // Navigator: Highest HP enemy ignored this turn
        Ultimate_KnockbackToLastColumn,  // Surgeon: Attack, knockback to last column
        Ultimate_AttackKnockbackNearby,  // Cook: Attack, knockback nearby enemies
        Ultimate_StunAoE,                // Swashbuckler: Attack, stun target + nearby
        Ultimate_MassiveSingleTarget,    // Deckhand: +300% if no nearby enemies
        
        // ========== PASSIVE UNIQUE (Role-specific passive) ==========
        PassiveUnique_ExtraEnergy,       // Captain: +1 max energy each turn
        PassiveUnique_ExtraCards,        // Quartermaster: +2 cards each turn
        PassiveUnique_DeathStrikeByMorale,// Helmsman: Higher morale = chance to attack on death
        PassiveUnique_LowerSurrenderThreshold, // Boatswain: Allies surrender at 10%
        PassiveUnique_NoBuzzDownside,    // Shipwright: No penalty when buzz full
        PassiveUnique_DrawPerGrog,       // Master Gunner: Draw extra per grog
        PassiveUnique_DrawOnLowDamage,   // Master-at-Arms: Draw if <20% HP damage taken
        PassiveUnique_CounterAttack,     // Navigator: Attack back when ally damaged
        PassiveUnique_GritAura,          // Surgeon: Nearby allies +5% of this unit's grit
        PassiveUnique_BonusVsLowGrit,    // Cook: +20% vs lower grit targets
        PassiveUnique_IgnoreRoles,       // Swashbuckler: Ignore Shipwright/Boatswain
        PassiveUnique_BonusVsLowHP,      // Deckhand: Bonus damage vs <50% HP targets
    }
}