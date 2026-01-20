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
        public bool isVariant2 = false;  // True for V2 effects
        
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
        /// Get display name like "Captain Boots" or "Captain Boots V2"
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
            string suffix = isVariant2 ? " V2" : "";
            return $"{roleName} {category}{suffix}";
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
    /// All possible relic effects (192 total: 96 V1 + 96 V2).
    /// Organized by category for clarity.
    /// </summary>
    public enum RelicEffectType
    {
        None,
        
        // ==================== BOOTS V1 (Movement) ====================
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
        
        // ==================== BOOTS V2 (Movement) ====================
        Boots_V2_SwapWithEnemy,          // Captain: Swap location with enemy
        Boots_V2_MoveAllyGainShield,     // Quartermaster: Move ally, they gain shield
        Boots_V2_MoveGainMoraleOnKill,   // Helmsman: Move, gain morale on next kill
        Boots_V2_AllAlliesMove1,         // Boatswain: All allies can move 1 tile free
        Boots_V2_MoveGainBuzzReduction,  // Shipwright: Move, reduce buzz gain 50% for 2 turns
        Boots_V2_MoveGainGrog,           // Master Gunner: Move 2, gain 1 grog
        Boots_V2_MoveGainArmor,          // Master-at-Arms: Move 2, gain 50 hull
        Boots_V2_MoveExtraIfLowHP,       // Navigator: Move +2 if below 50% HP
        Boots_V2_MoveHealAdjacent,       // Surgeon: Move, heal adjacent allies 10%
        Boots_V2_MovePoisonTile,         // Cook: Move, leave poison on previous tile
        Boots_V2_MoveGainDodge,          // Swashbuckler: Move, 30% dodge next attack
        Boots_V2_MoveDrawCard,           // Deckhand: Move 1, draw a card
        
        // ==================== GLOVES V1 (Attack + Effect) ====================
        Gloves_AttackReduceEnemyDraw,    // Captain: Attack, enemy draws 1 less
        Gloves_AttackIncreaseEnemyCost,  // Quartermaster: Attack, enemy next card +1 cost
        Gloves_AttackBonusByMissingMorale, // Helmsman: Attack, +dmg by enemy missing morale
        Gloves_AttackMarkMoraleFocus,    // Boatswain: Attack, mark for morale focus 2 turns
        Gloves_AttackPreventBuzzReduce,  // Shipwright: Attack, prevent buzz reduction 2 turns
        Gloves_AttackBonusPerGrog,       // Master Gunner: Attack, +20% per grog token
        Gloves_AttackBonusIfMoreHP,      // Master-at-Arms: Attack, +20% if more HP than target
        Gloves_AttackLowerEnemyHealth,   // Navigator: Attack, lower enemy health stat 30%
        Gloves_AttackPushForward,        // Surgeon: Attack, push target forward 1 tile
        Gloves_AttackForceTargetClosest, // Cook: Attack, debuff forces attack closest
        Gloves_AttackBonusPerCardPlayed, // Swashbuckler: Attack, +10% per card played
        Gloves_AttackBonusPerGunnerRelic,// Deckhand: Attack, +10% per gunner relic used
        
        // ==================== GLOVES V2 (Attack + Effect) ====================
        Gloves_V2_AttackStealBuff,       // Captain: Attack, steal one buff from target
        Gloves_V2_AttackDiscard,         // Quartermaster: Attack, enemy discards 1 card
        Gloves_V2_AttackMoraleDamage,    // Helmsman: Attack deals bonus morale damage
        Gloves_V2_AttackHealAlly,        // Boatswain: Attack, heal lowest HP ally 15%
        Gloves_V2_AttackReduceBuzz,      // Shipwright: Attack, reduce own buzz by 20
        Gloves_V2_AttackSpendGrogBonus,  // Master Gunner: Spend 1 grog for +50% damage
        Gloves_V2_AttackGainHullOnKill,  // Master-at-Arms: Kill grants 30 hull
        Gloves_V2_AttackSlowEnemy,       // Navigator: Attack, slow enemy 1 tile for 2 turns
        Gloves_V2_AttackPullEnemy,       // Surgeon: Attack, pull enemy 1 tile toward you
        Gloves_V2_AttackApplyPoison,     // Cook: Attack applies poison (10 dmg/turn, 3 turns)
        Gloves_V2_AttackBonusVsDebuffed, // Swashbuckler: +30% vs debuffed enemies
        Gloves_V2_AttackChainToAdjacent, // Deckhand: Attack chains 50% damage to adjacent
        
        // ==================== HAT V1 (Resource/Card Manipulation) ====================
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
        
        // ==================== HAT V2 (Resource/Card Manipulation) ====================
        Hat_V2_DrawAndShield,            // Captain: Draw 1, gain 30 shield
        Hat_V2_DrawBootsRelic,           // Quartermaster: Draw a boots relic
        Hat_V2_RestoreMoraleAll,         // Helmsman: All allies restore 5% morale
        Hat_V2_PreventMoraleLoss,        // Boatswain: Allies can't lose morale this turn
        Hat_V2_RumHealsMore,             // Shipwright: Rum heals 50% more this turn
        Hat_V2_GrogOnEnemyKill,          // Master Gunner: Gain 1 grog per enemy killed
        Hat_V2_DamageReductionBuff,      // Master-at-Arms: Gain 30% damage reduction 2 turns
        Hat_V2_SpeedBoost,               // Navigator: +2 movement this turn
        Hat_V2_HealOnCardPlay,           // Surgeon: Heal 5% HP per card played this turn
        Hat_V2_BuffFoodEffects,          // Cook: Food effects doubled this turn
        Hat_V2_DrawPerEnemyInRange,      // Swashbuckler: Draw 1 card per enemy in 2 tiles
        Hat_V2_ReduceAllCosts,           // Deckhand: All card costs -1 this turn
        
        // ==================== COAT V1 (Defensive) ====================
        Coat_BuffNearbyAimPower,         // Captain: +20% Aim/Power to allies in 1 tile
        Coat_DrawOnEnemyAttack,          // Quartermaster: Draw card when attacked
        Coat_ReduceMoraleDamage,         // Helmsman: 30% less morale damage 2 turns
        Coat_PreventSurrender,           // Boatswain: Prevent surrender, restore 20% morale
        Coat_ReduceRumEffect,            // Shipwright: Nearby allies reduced rum effect
        Coat_EnemyBuzzOnDamage,          // Master Gunner: Enemy buzz fills when damaging
        Coat_PreventDisplacement,        // Master-at-Arms: Allies can't be knocked back
        Coat_ProtectLowHP,               // Navigator: Lowest HP only targetable by lower HP
        Coat_RowCantBeTargeted,          // Surgeon: Allies behind can't be targeted
        Coat_ColumnDamageBoost,          // Cook: +40% damage to allies in column
        Coat_FreeStow,                   // Swashbuckler: Next 2 stows free
        Coat_RowRangedProtection,        // Deckhand: Row takes 50% less ranged damage
        
        // ==================== COAT V2 (Defensive) ====================
        Coat_V2_ShieldNearby,            // Captain: Nearby allies gain 25 shield
        Coat_V2_CounterOnAllyHit,        // Quartermaster: Counter-attack when ally is hit
        Coat_V2_MoraleShield,            // Helmsman: Absorb next 50 morale damage
        Coat_V2_RevivePrevent,           // Boatswain: Prevent one ally death (1 HP)
        Coat_V2_BuzzImmunity,            // Shipwright: Nearby allies immune to buzz effects
        Coat_V2_GrogShield,              // Master Gunner: Spend 1 grog, gain 50 shield
        Coat_V2_ThornsAura,              // Master-at-Arms: Attackers take 20 damage
        Coat_V2_DodgeAura,               // Navigator: Nearby allies 15% dodge chance
        Coat_V2_HealingAura,             // Surgeon: Nearby allies heal 5% at turn end
        Coat_V2_WellFed,                 // Cook: Nearby allies +10% max HP for 2 turns
        Coat_V2_Evasion,                 // Swashbuckler: Gain 25% evasion for 2 turns
        Coat_V2_RangedBlock,             // Deckhand: Block next ranged attack completely
        
        // ==================== TRINKET V1 (Passive) ====================
        Trinket_BonusDamagePerCard,      // Captain: +20% weapon damage per card in hand
        Trinket_BonusVsCaptain,          // Quartermaster: +20% damage vs enemy captain
        Trinket_ImmuneMoraleFocusFire,   // Helmsman: Immune to morale focus fire
        Trinket_EnemySurrenderEarly,     // Boatswain: Enemies surrender at 30% morale
        Trinket_DamageByBuzz,            // Shipwright: +damage based on own buzz
        Trinket_KnockbackIncreasesBuzz,  // Master Gunner: Knockback increases enemy buzz
        Trinket_ReduceDamageFromClosest, // Master-at-Arms: Closest enemy -20% damage
        Trinket_DrawIfHighHP,            // Navigator: Draw extra if HP above 60%
        Trinket_TauntFirstAttack,        // Surgeon: Taunt first attack per enemy turn
        Trinket_KnockbackAttacker,       // Cook: Knockback attacker once per turn
        Trinket_RowEnemiesLessDamage,    // Swashbuckler: Enemies in row -10% damage
        Trinket_RowEnemiesTakeMore,      // Deckhand: Enemies in row take +10% damage
        
        // ==================== TRINKET V2 (Passive) ====================
        Trinket_V2_BonusDamagePerAlly,   // Captain: +5% damage per ally alive
        Trinket_V2_DrawOnCaptainHit,     // Quartermaster: Draw when captain takes damage
        Trinket_V2_MoraleOnKill,         // Helmsman: Gain 10% morale on enemy kill
        Trinket_V2_AllySurrenderLater,   // Boatswain: Allies surrender at 10%
        Trinket_V2_NoBuzzPenalty,        // Shipwright: No penalty when buzz is full
        Trinket_V2_GrogOnTurnStart,      // Master Gunner: 25% chance for free grog
        Trinket_V2_ArmorOnLowHP,         // Master-at-Arms: +50% armor below 30% HP
        Trinket_V2_SpeedOnHighHP,        // Navigator: +1 movement above 70% HP
        Trinket_V2_HealOnTurnEnd,        // Surgeon: Heal 3% at turn end
        Trinket_V2_FoodDoubleDuration,   // Cook: Food buffs last twice as long
        Trinket_V2_CritChance,           // Swashbuckler: 15% chance for +50% damage
        Trinket_V2_BonusVsFullHP,        // Deckhand: +25% damage vs full HP targets
        
        // ==================== TOTEM V1 (Summons/Curses) ====================
        Totem_SummonCannon,              // Captain: Summon cannon, 250 HP, attacks random
        Totem_CurseCaptainReflect,       // Quartermaster: Captain damage reflects
        Totem_RallyNoMoraleDamage,       // Helmsman: No morale damage next turn
        Totem_EnemyDeathMoraleSwing,     // Boatswain: Enemy death = morale swing
        Totem_SummonHighQualityRum,      // Shipwright: Add 2 high quality rum
        Totem_ConvertGrogToEnergy,       // Master Gunner: Convert 2 grog to 1 energy
        Totem_StunOnKnockback,           // Master-at-Arms: Stun attacker if knocked back
        Totem_SummonAnchorHealthBuff,    // Navigator: Summon anchor, +25% health nearby
        Totem_SummonTargetDummy,         // Surgeon: Summon dummy in front row, 250 HP
        Totem_SummonObstacleDisplace,    // Cook: Summon obstacle, displace target
        Totem_SummonExplodingBarrels,    // Swashbuckler: 3 barrels explode after 2 turns
        Totem_CurseRangedWeapons,        // Deckhand: Enemy ranged -50% damage next turn
        
        // ==================== TOTEM V2 (Summons/Curses) ====================
        Totem_V2_SummonHealingTotem,     // Captain: Totem heals nearby allies 10/turn
        Totem_V2_CurseWeakness,          // Quartermaster: Curse enemy, -20% damage 3 turns
        Totem_V2_RallyDamageBoost,       // Helmsman: Nearby allies +15% damage 2 turns
        Totem_V2_SummonMoraleBanner,     // Boatswain: Banner prevents morale loss
        Totem_V2_SummonGrogBarrel,       // Shipwright: Barrel gives grog when destroyed
        Totem_V2_TrapTile,               // Master Gunner: Place trap that stuns 1 turn
        Totem_V2_SummonShieldGenerator,  // Master-at-Arms: Generator gives 10 shield/turn
        Totem_V2_SummonSpeedBooster,     // Navigator: Tile gives +1 movement to allies
        Totem_V2_SummonHealingWell,      // Surgeon: Well heals 15%/turn
        Totem_V2_PoisonCloud,            // Cook: Poison cloud, 15 dmg/turn for 3 turns
        Totem_V2_SummonDecoy,            // Swashbuckler: Decoy taunts enemies 2 turns
        Totem_V2_CurseSlow,              // Deckhand: Curse enemy, -2 movement 2 turns
        
        // ==================== ULTIMATE V1 (Role-specific powerful) ====================
        Ultimate_ShipCannon,             // Captain: 3 shots, 200 dmg, fire hazard
        Ultimate_MarkCaptainOnly,        // Quartermaster: Attack captain, only target
        Ultimate_ReflectMoraleDamage,    // Helmsman: Reflect morale damage to enemies
        Ultimate_ReviveAlly,             // Boatswain: Revive dead/surrendered at 30%
        Ultimate_FullBuzzAttack,         // Shipwright: Attack, target buzz full 2 turns
        Ultimate_RumBottleAoE,           // Master Gunner: 200 dmg AoE, rum spill zone
        Ultimate_SummonHardObstacles,    // Master-at-Arms: 3 hard obstacles front row
        Ultimate_IgnoreHighestHP,        // Navigator: Highest HP enemy ignored
        Ultimate_KnockbackToLastColumn,  // Surgeon: Knockback to last column
        Ultimate_AttackKnockbackNearby,  // Cook: Attack, knockback nearby enemies
        Ultimate_StunAoE,                // Swashbuckler: Attack, stun target + nearby
        Ultimate_MassiveSingleTarget,    // Deckhand: +300% if no nearby enemies
        
        // ==================== ULTIMATE V2 (Role-specific powerful) ====================
        Ultimate_V2_TeamwideBuff,        // Captain: All allies +25% damage/armor 2 turns
        Ultimate_V2_ExecuteBelow20,      // Quartermaster: Instantly kill enemy below 20%
        Ultimate_V2_FullMoraleRestore,   // Helmsman: Fully restore all allies' morale
        Ultimate_V2_MassRevive,          // Boatswain: Revive all dead allies at 20% HP
        Ultimate_V2_BuzzExplosion,       // Shipwright: All enemies buzz fills, dmg = buzz
        Ultimate_V2_GrogRain,            // Master Gunner: Gain 5 grog instantly
        Ultimate_V2_Fortress,            // Master-at-Arms: All allies gain 100 shield
        Ultimate_V2_Teleport,            // Navigator: Teleport any ally to any tile
        Ultimate_V2_MassHeal,            // Surgeon: Heal all allies 40% HP
        Ultimate_V2_Feast,               // Cook: All allies heal 30% HP + 20% morale
        Ultimate_V2_BladeStorm,          // Swashbuckler: Attack all enemies 75% damage
        Ultimate_V2_PerfectShot,         // Deckhand: Guaranteed crit ignoring armor
        
        // ==================== PASSIVE UNIQUE V1 ====================
        PassiveUnique_ExtraEnergy,       // Captain: +1 max energy each turn
        PassiveUnique_ExtraCards,        // Quartermaster: +2 cards each turn
        PassiveUnique_DeathStrikeByMorale,// Helmsman: Higher morale = attack on death
        PassiveUnique_LowerSurrenderThreshold, // Boatswain: Allies surrender at 10%
        PassiveUnique_NoBuzzDownside,    // Shipwright: No penalty when buzz full
        PassiveUnique_DrawPerGrog,       // Master Gunner: Draw extra per grog
        PassiveUnique_DrawOnLowDamage,   // Master-at-Arms: Draw if <20% HP damage
        PassiveUnique_CounterAttack,     // Navigator: Attack when ally damaged
        PassiveUnique_GritAura,          // Surgeon: Nearby allies +5% grit
        PassiveUnique_BonusVsLowGrit,    // Cook: +20% vs lower grit targets
        PassiveUnique_IgnoreRoles,       // Swashbuckler: Ignore Shipwright/Boatswain
        PassiveUnique_BonusVsLowHP,      // Deckhand: Bonus vs <50% HP targets
        
        // ==================== PASSIVE UNIQUE V2 ====================
        PassiveUnique_V2_TeamLeader,     // Captain: Allies in 2 tiles +10% all stats
        PassiveUnique_V2_CardMaster,     // Quartermaster: Cards cost 1 less (min 0)
        PassiveUnique_V2_Inspiring,      // Helmsman: Allies gain 5% morale on attack
        PassiveUnique_V2_LastStand,      // Boatswain: 50% ally survives at 1 HP
        PassiveUnique_V2_DrunkMaster,    // Shipwright: Buzz gives bonuses instead
        PassiveUnique_V2_Efficient,      // Master Gunner: 50% grog not consumed
        PassiveUnique_V2_Unstoppable,    // Master-at-Arms: Can't be stunned/slowed
        PassiveUnique_V2_Scout,          // Navigator: See enemy cards/cooldowns
        PassiveUnique_V2_Medic,          // Surgeon: Heals are 25% more effective
        PassiveUnique_V2_Nourishing,     // Cook: Food heals 10% HP additionally
        PassiveUnique_V2_Riposte,        // Swashbuckler: 30% chance counter-attack
        PassiveUnique_V2_Sniper,         // Deckhand: +20% damage at max range
    }
}