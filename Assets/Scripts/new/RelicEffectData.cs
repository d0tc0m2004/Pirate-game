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
        
        [Header("Display Info")]
        public string effectName;           // Display name like "Captain Boots"
        public RelicRarity rarity = RelicRarity.Common;
        
        [Header("Card Info")]
        public int copies = 2;              // How many cards in deck
        public int energyCost = 1;          // Cost to play
        public bool isPassive = false;      // If true, always active (no card)
        
        [Header("Effect")]
        public RelicEffectType effectType;
        [TextArea(2, 4)]
        public string description;
        
        [Header("Values")]
        public float value1;                // Primary value (damage %, tiles, etc.)
        public float value2;                // Secondary value
        public int duration;                // Effect duration in turns
        public int tileRange = 1;           // Tile radius for AoE effects
        
        /// <summary>
        /// Get display name like "Captain Boots"
        /// </summary>
        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(effectName))
                return effectName;
                
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
    /// V1 = Original effects, V2 = Variant 2 effects (192 total = 8 categories × 12 roles × 2 variants)
    /// </summary>
    public enum RelicEffectType
    {
        None,
        
        // =====================================================================
        // BOOTS V1 (Movement) - Original Effects
        // =====================================================================
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
        
        // =====================================================================
        // BOOTS V2 (Movement) - Variant 2 Effects
        // =====================================================================
        Boots_MoveAnyAlly,               // Captain V2: Move 2 tiles any allied unit
        Boots_LowestMoraleAllyFree,      // Quartermaster V2: Lowest morale ally moves free this turn
        Boots_FreeIfGrogAvailable,       // Helmsman V2: Move 2, free if grog tokens available
        Boots_MoveAnyDistanceHighHP,     // Boatswain V2: Move any distance if highest HP, else 2
        Boots_MoveGainGritStat,          // Shipwright V2: Move 2, +20% Grit for 2 turns
        Boots_MoveReduceRangedWeapon,    // MasterGunner V2: Move 1, reduce next ranged weapon cost by 1
        Boots_MoveDestroyObstacle,       // MasterAtArms V2: Move to obstacle tile in 2 range, destroy it
        Boots_MoveFreeZeroCost,          // Navigator V2: Move 2 tiles, costs 0 energy
        Boots_SwapLowestHealthAlly,      // Surgeon V2: Swap with lowest health ally
        Boots_MoveGainProficiency,       // Cook V2: Move 2, +100% Proficiency this turn
        Boots_MoveRowUnlimited,          // Swashbuckler V2: Move unlimited on row, 1 on column
        Boots_MoveRestoreHull,           // Deckhand V2: Move 2, restore 50 hull shield
        
        // =====================================================================
        // GLOVES V1 (Attack + Effect) - Original Effects
        // =====================================================================
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
        
        // =====================================================================
        // GLOVES V2 (Attack + Effect) - Variant 2 Effects
        // =====================================================================
        Gloves_AttackEnemyCostIncrease,  // Captain V2: Attack, enemy next card costs +1 energy
        Gloves_AttackMarkMoraleFocus2,   // Quartermaster V2: Attack, mark morale focus for 2 turns
        Gloves_AttackBonusPerGrogToken,  // Helmsman V2: Attack, +20% per grog token
        Gloves_AttackLowerHealthStat,    // Boatswain V2: Attack, lower enemy health stat 30% for 2 turns
        Gloves_AttackForceTargetNearest, // Shipwright V2: Attack, debuff forces attack closest next turn
        Gloves_AttackBonusPerGunnerUsed, // MasterGunner V2: Attack, +10% per gunner relic used this game
        Gloves_AttackBonusPerMArmCard,   // MasterAtArms V2: Attack, +10% per Master-at-Arms card in hand
        Gloves_AttackBonusPerBootsCard,  // Navigator V2: Attack, +30% per boots card in deck
        Gloves_AttackOnEnemyHeal,        // Surgeon V2: Passive - attack any enemy that gets healed
        Gloves_AttackStasisTarget,       // Cook V2: Put closest target in stasis for 1 turn
        Gloves_AttackTwice,              // Swashbuckler V2: Attack with default weapon 2 times
        Gloves_AttackHullDestroyEnergy,  // Deckhand V2: Attack, if hull destroyed gain 1 energy
        
        // =====================================================================
        // HAT V1 (Resource/Card Manipulation) - Original Effects
        // =====================================================================
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
        
        // =====================================================================
        // HAT V2 (Resource/Card Manipulation) - Variant 2 Effects
        // =====================================================================
        Hat_DrawUltimateAbility,         // Captain V2: Draw an ultimate ability
        Hat_RestoreMoraleNearbyAllies,   // Quartermaster V2: 10% morale to allies in 1 tile range
        Hat_GenerateGrogTokens,          // Helmsman V2: Generate 2 grog tokens
        Hat_IncreaseHealthStatBuff,      // Boatswain V2: +25% health stat for 2 turns
        Hat_SwapEnemyGritPositions,      // Shipwright V2: Swap highest/lowest grit enemy positions
        Hat_DrawWeaponRelicCard,         // MasterGunner V2: Draw a weapon relic card
        Hat_IncreaseEnemyWeaponCost,     // MasterAtArms V2: Enemy next weapon relic costs +1
        Hat_DrawBootsRelicCard,          // Navigator V2: Draw a boots relic card
        Hat_HealOnCaptainDamage,         // Surgeon V2: Allies heal 10% when damaging enemy captain
        Hat_MoveForwardAndHeal,          // Cook V2: Move ally forward 1 tile, heal 10%
        Hat_StealEnemyCard,              // Swashbuckler V2: Steal random enemy card, reduce weapon cost
        Hat_DestroyObstaclesGainHull,    // Deckhand V2: Destroy soft obstacles, +20% hull each
        
        // =====================================================================
        // COAT V1 (Defensive) - Original Effects
        // =====================================================================
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
        
        // =====================================================================
        // COAT V2 (Defensive) - Variant 2 Effects
        // =====================================================================
        Coat_DrawOnEnemyAttackDiscard,   // Captain V2: Per enemy attack (3 max), draw card, enemy discards
        Coat_PreventSurrenderRestore,    // Quartermaster V2: If ally would surrender, restore 20% morale
        Coat_EnemyBuzzOnDealDamage,      // Helmsman V2: Enemy buzz fills every time they deal damage
        Coat_ProtectLowestHP,            // Boatswain V2: Lowest HP only targeted by lower HP enemies
        Coat_ColumnDamageBoostAllies,    // Shipwright V2: +40% damage to allies in same column
        Coat_RowRangedProtect,           // MasterGunner V2: Row takes 50% less ranged damage next turn
        Coat_NearbyDamageBoost,          // MasterAtArms V2: +20% damage to nearby allies in 1 tile
        Coat_DodgeFirstAttack,           // Navigator V2: First attacked ally dodges by moving 1 tile back
        Coat_KnockbackOnAllyDeath,       // Surgeon V2: Knockback enemy 1 tile when ally dies nearby
        Coat_ClearDebuffsNearby,         // Cook V2: Clear all debuffs from nearby allies
        Coat_CurseTileTrapped,           // Swashbuckler V2: Curse random enemy tile, trap + 10% more damage
        Coat_BuffRandomTile,             // Deckhand V2: Buff tile, units take 15% less, deal 15% more
        
        // =====================================================================
        // TRINKET V1 (Passive) - Original Effects
        // =====================================================================
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
        
        // =====================================================================
        // TRINKET V2 (Passive) - Variant 2 Effects
        // =====================================================================
        Trinket_BonusVsCaptainTarget,    // Captain V2: +20% damage vs Captain targets
        Trinket_EnemySurrenderAt30,      // Quartermaster V2: Enemies surrender at 30% morale
        Trinket_KnockbackFillsBuzz,      // Helmsman V2: When enemies knocked back, their buzz increases
        Trinket_DrawIfHighHealth,        // Boatswain V2: Draw extra card if HP above 60%
        Trinket_KnockbackAttackerOnce,   // Shipwright V2: Knockback attacker 1 tile once per turn
        Trinket_RowEnemiesTakeMoreDmg,   // MasterGunner V2: Enemies in row take +10% damage
        Trinket_NearbyAlliesPowerBuff,   // MasterAtArms V2: Nearby allies +30% Power stat
        Trinket_NearbyIgnoreObstacles,   // Navigator V2: Nearby allies ignore soft obstacles when attacking
        Trinket_GlobalAllyRadius,        // Surgeon V2: Nearby allies radius = whole board
        Trinket_DrawIfLowHP,             // Cook V2: Draw extra if below 50% HP
        Trinket_EnemiesLoseSpeed,        // Swashbuckler V2: All enemies -10% Speed stat
        Trinket_HullRegenOnSurvive,      // Deckhand V2: If hull survives enemy attack, discard enemy card
        
        // =====================================================================
        // TOTEM V1 (Summons/Curses) - Original Effects
        // =====================================================================
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
        
        // =====================================================================
        // TOTEM V2 (Summons/Curses) - Variant 2 Effects
        // =====================================================================
        Totem_CurseCaptainDamageReflect, // Captain V2: Captain damage reflects to all enemy allies
        Totem_EnemyDeathMoraleSwap,      // Quartermaster V2: Enemy death = enemies lose morale, allies gain
        Totem_ConvertGrogToEnergyFree,   // Helmsman V2: Convert 2 grog to 1 energy (0 cost)
        Totem_SummonAnchorHealthAura,    // Boatswain V2: Summon anchor, +25% health to nearby for 2 turns
        Totem_SummonObstacleDisplaceTarget, // Shipwright V2: Summon obstacle at target, displace target
        Totem_CurseRangedWeaponsDamage,  // MasterGunner V2: Enemy ranged weapons -50% damage
        Totem_EarthquakeHazards,         // MasterAtArms V2: 3 random earthquake hazards, displace at end
        Totem_DisableEnemyRelics,        // Navigator V2: Disable enemy non-weapon relics for 1 turn
        Totem_SummonHealingPotions,      // Surgeon V2: Summon 3 healing potions (200 HP) in random tiles
        Totem_SummonDebuffObstacle,      // Cook V2: Summon obstacle that reduces nearby enemy stats 50%
        Totem_DisableEnemyPassives,      // Swashbuckler V2: Enemies can't use passives next turn
        Totem_PullEnemiestoRow,          // Deckhand V2: Pull nearby enemies to same row
        
        // =====================================================================
        // ULTIMATE V1 (Role-specific powerful) - Original Effects
        // =====================================================================
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
        
        // =====================================================================
        // ULTIMATE V2 (Role-specific powerful) - Variant 2 Effects
        // =====================================================================
        Ultimate_AttackCaptainMark,      // Captain V2: Attack captain, mark as only target this turn
        Ultimate_ReviveAllyFull,         // Quartermaster V2: Revive dead/surrendered at 30% HP/morale
        Ultimate_RumBottleAoEBuzz,       // Helmsman V2: 200 dmg AoE, rum spill increases buzz
        Ultimate_IgnoreHighestHPEnemy,   // Boatswain V2: Highest HP enemy (not captain) ignored
        Ultimate_AttackKnockbackNearbyAll, // Shipwright V2: Attack, knockback all nearby enemies 1 tile
        Ultimate_MassiveSingleTargetBonus, // MasterGunner V2: +300% damage if no nearby enemies
        Ultimate_AttackAllEnemiesRow,    // MasterAtArms V2: Attack closest + 350 damage to whole row
        Ultimate_SwapClosestFurthest,    // Navigator V2: Swap closest and furthest enemy positions
        Ultimate_FullHealthRestore,      // Surgeon V2: Fully restore any unit's health
        Ultimate_SetColumnOnFire,        // Cook V2: Set closest target's whole column on fire
        Ultimate_FourWeaponsSurrender,   // Swashbuckler V2: Passive - 4 weapons on same target = surrender
        Ultimate_ClearHazardsPrevent,    // Deckhand V2: Clear all hazards, prevent new ones
        
        // =====================================================================
        // PASSIVE UNIQUE V1 (Role-specific passive) - Original Effects
        // =====================================================================
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
        
        // =====================================================================
        // PASSIVE UNIQUE V2 (Role-specific passive) - Variant 2 Effects
        // =====================================================================
        PassiveUnique_ExtraCardsEachTurn,// Captain V2: +2 cards each turn
        PassiveUnique_LowerAllySurrender,// Quartermaster V2: Allies surrender at 10% morale
        PassiveUnique_DrawPerGrogToken,  // Helmsman V2: Draw extra cards per grog token
        PassiveUnique_CounterAttackAlly, // Boatswain V2: Attack back when any ally takes damage
        PassiveUnique_BonusVsLowGritTarget, // Shipwright V2: +20% vs targets with lower grit
        PassiveUnique_BonusVsLowHPTarget,// MasterGunner V2: Bonus damage vs <50% HP targets
        PassiveUnique_KillRestoreHealth, // MasterAtArms V2: Kill/surrender restores 20% health
        PassiveUnique_AllAlliesExtraMove,// Navigator V2: All allies can move +1 tile
        PassiveUnique_KillRestoreAllyHP, // Surgeon V2: Kill/surrender restores 5% HP to all allies
        PassiveUnique_RelicsNotConsumed, // Cook V2: Relics not consumed, can replay if energy allows
        PassiveUnique_EnemyBootsLimited, // Swashbuckler V2: Enemies can only move 1 tile with boots
        PassiveUnique_BonusDmgPerHullDestroyed, // Deckhand V2: +30% weapon damage per hull destroyed (any)
    }
}