using UnityEngine;
using TacticalGame.Enums;

namespace TacticalGame.Combat
{
    /// <summary>
    /// All status effect types for the relic system.
    /// Covers all buffs/debuffs from 192 relic effects.
    /// </summary>
    public enum StatusEffectType
    {
        None,
        
        // ==================== DAMAGE OVER TIME ====================
        Fire,               // Takes damage each turn
        Poison,             // Takes damage each turn (lower but longer)
        Bleed,              // Takes damage when moving
        
        // ==================== STAT MODIFIERS ====================
        GritBoost,          // +Grit stat
        GritReduction,      // -Grit stat
        AimBoost,           // +Aim stat
        AimReduction,       // -Aim stat
        PowerBoost,         // +Power stat
        PowerReduction,     // -Power stat
        SpeedBoost,         // +Speed stat
        SpeedReduction,     // -Speed stat
        ProficiencyBoost,   // +Proficiency stat
        HealthStatBoost,    // +Health stat (max HP modifier)
        HealthStatReduction,// -Health stat
        
        // ==================== DAMAGE MODIFIERS ====================
        DamageBoost,        // Deal more damage (percentage)
        DamageReduction,    // Take less damage (percentage)
        Vulnerable,         // Take more damage (percentage)
        RangedDamageReduction, // Reduce incoming ranged damage
        MoraleDamageReduction, // Reduce morale damage taken
        
        // ==================== COMBAT EFFECTS ====================
        Marked,             // Takes bonus damage, morale focus
        MoraleFocus,        // Target for morale damage
        Shielded,           // Damage reduction (flat or percentage)
        Regeneration,       // Heal over time
        HealBlock,          // Cannot restore HP or Morale
        MissChance,         // Chance to miss attacks
        Stun,               // Cannot act
        
        // ==================== MOVEMENT EFFECTS ====================
        Slowed,             // Reduced movement
        MovementTrap,       // Takes damage if moves
        FreeMove,           // Next move costs 0 energy
        PreventDisplacement,// Cannot be knocked back
        
        // ==================== TARGETING EFFECTS ====================
        ForceTargetClosest, // Must attack closest enemy
        OnlyLowerHPCanTarget, // Only lower HP enemies can target this unit
        RowCantBeTargeted,  // Allies behind can't be targeted
        Taunt,              // Forces enemies to target this unit
        IgnoredByEnemies,   // Cannot be targeted this turn
        
        // ==================== RESOURCE EFFECTS ====================
        EnergyDrain,        // Lose energy next turn
        ReduceCardDraw,     // Draw fewer cards
        IncreaseCost,       // Cards cost more energy
        ReduceNextRangedCost, // Next ranged attack costs less
        
        // ==================== BUZZ EFFECTS ====================
        PreventBuzzReduction, // Cannot reduce buzz
        BuzzFilled,         // Buzz is forced to maximum
        EnemyBuzzOnDamage,  // Enemy gains buzz when dealing damage
        
        // ==================== COUNTER/REACTIVE EFFECTS ====================
        ReturnDamage,       // Reflect damage back to attacker
        StunOnKnockback,    // If knocked back, stun attacker
        EnergyOnKnockback,  // Gain energy if knocked back
        DrawOnEnemyAttack,  // Draw cards when attacked
        DodgeFirstAttack,   // Dodge first attack by moving back
        KnockbackAttacker,  // Knockback attacker once per turn
        CounterAttack,      // Attack back when damaged
        
        // ==================== CAPTAIN/SPECIAL ====================
        CaptainDamageReflect, // Captain damage reflects to allies
        ReflectMoraleDamage,  // Morale damage reflected to enemies
        OnlyTargetThisTurn,   // Only this unit can be targeted
        
        // ==================== SURRENDER EFFECTS ====================
        PreventSurrender,   // Cannot surrender, restore morale instead
        EnemySurrenderEarly,// Enemies surrender at higher morale
        AllySurrenderLower, // Allies surrender at lower morale
        
        // ==================== PASSIVE AURAS ====================
        GritAura,           // Nearby allies gain grit
        PowerAura,          // Nearby allies gain power
        GlobalAllyRadius,   // "Nearby" means whole board
        IgnoreObstacles,    // Can move through soft obstacles
        
        // ==================== WEAPON/CARD EFFECTS ====================
        WeaponUseTwice,     // Next weapon can be used twice
        FreeStows,          // Next N stows are free
        FreeRumUsage,       // Rum uses don't cost grog
        ReducedRumEffect,   // Rum effects reduced
        EnemyWeaponCostIncrease, // Enemy weapons cost more
        DisableNonWeaponRelics,  // Cannot use non-weapon relics
        DisablePassives,    // Passives don't trigger
        RelicsNotConsumed,  // Relics can be replayed
        
        // ==================== HEALING TRIGGERS ====================
        HealOnCaptainDamage, // Heal when damaging captain
        AttackOnEnemyHeal,  // Attack enemies that get healed
        
        // ==================== BONUS DAMAGE CONDITIONS ====================
        BonusVsCaptain,     // Extra damage vs captain
        BonusVsCaptainTarget, // Extra damage vs captain's targets
        BonusVsLowGrit,     // Extra damage vs lower grit
        BonusVsLowHP,       // Extra damage vs low HP targets
        BonusPerCardsInHand,// Extra damage per card in hand
        BonusPerBuzz,       // Extra damage based on buzz
        
        // ==================== MISC ====================
        Stasis,             // Cannot act or be damaged
        Trapped,            // Cannot move, takes more damage
        Cursed,             // Takes bonus damage from all sources
        EnemyBootsLimited,  // Enemies can only move 1 tile
        HullRegenOnSurvive, // If hull survives, discard enemy card
        BonusDmgPerHullDestroyed, // +damage per hull destroyed
        Invincible,         // Cannot take damage
        
        // ==================== V2 EFFECTS ====================
        MoraleOnKill,       // Gain morale when killing an enemy
        BuzzGainReduction,  // Reduce buzz gain
        Dodge,              // Chance to dodge attacks
        RumHealBoost,       // Rum heals more
        GrogOnKill,         // Gain grog when killing an enemy
        HealOnCardPlay,     // Heal when playing cards
        FoodEffectBoost,    // Food effects increased
        ReduceAllCosts,     // All card costs reduced
        CounterOnAllyHit,   // Counter-attack when ally is hit
        MoraleShield,       // Absorb morale damage
        DeathPrevention,    // Prevent death once
        BuzzImmunity,       // Immune to buzz effects
        Thorns,             // Reflect damage to attackers
        MaxHPBoost,         // Increase max HP
        RangedBlock,        // Block ranged attacks
        Weakness,           // Deal less damage
    }

    /// <summary>
    /// A status effect applied to a unit (buff or debuff).
    /// </summary>
    [System.Serializable]
    public class StatusEffect
    {
        public StatusEffectType type;
        public string effectName;
        public int remainingTurns;
        public float value1;        // Primary value (damage, percentage, etc.)
        public float value2;        // Secondary value (optional)
        public int stacks;          // For stackable effects
        public GameObject source;   // Who applied this effect
        public bool isDebuff;       // True = debuff, False = buff
        public bool triggeredThisTurn; // For once-per-turn effects

        /// <summary>
        /// Create a new status effect.
        /// </summary>
        public StatusEffect(StatusEffectType type, string name, int duration, float val1 = 0f, float val2 = 0f, GameObject source = null)
        {
            this.type = type;
            this.effectName = name;
            this.remainingTurns = duration;
            this.value1 = val1;
            this.value2 = val2;
            this.stacks = 1;
            this.source = source;
            this.isDebuff = IsDebuffType(type);
            this.triggeredThisTurn = false;
        }

        /// <summary>
        /// Tick down duration. Returns true if effect expired.
        /// </summary>
        public bool Tick()
        {
            triggeredThisTurn = false; // Reset for new turn
            remainingTurns--;
            return remainingTurns <= 0;
        }

        /// <summary>
        /// Check if a status type is a debuff.
        /// </summary>
        public static bool IsDebuffType(StatusEffectType type)
        {
            return type switch
            {
                // DOT
                StatusEffectType.Fire => true,
                StatusEffectType.Poison => true,
                StatusEffectType.Bleed => true,
                
                // Stat reductions
                StatusEffectType.GritReduction => true,
                StatusEffectType.AimReduction => true,
                StatusEffectType.PowerReduction => true,
                StatusEffectType.SpeedReduction => true,
                StatusEffectType.HealthStatReduction => true,
                
                // Negative combat
                StatusEffectType.Vulnerable => true,
                StatusEffectType.Marked => true,
                StatusEffectType.MoraleFocus => true,
                StatusEffectType.HealBlock => true,
                StatusEffectType.MissChance => true,
                StatusEffectType.Stun => true,
                
                // Movement restrictions
                StatusEffectType.Slowed => true,
                StatusEffectType.MovementTrap => true,
                
                // Targeting restrictions
                StatusEffectType.ForceTargetClosest => true,
                
                // Resource drains
                StatusEffectType.EnergyDrain => true,
                StatusEffectType.ReduceCardDraw => true,
                StatusEffectType.IncreaseCost => true,
                
                // Buzz effects
                StatusEffectType.PreventBuzzReduction => true,
                StatusEffectType.BuzzFilled => true,
                StatusEffectType.EnemyBuzzOnDamage => true,
                
                // Misc debuffs
                StatusEffectType.Stasis => true,
                StatusEffectType.Trapped => true,
                StatusEffectType.Cursed => true,
                StatusEffectType.EnemyBootsLimited => true,
                StatusEffectType.DisableNonWeaponRelics => true,
                StatusEffectType.DisablePassives => true,
                StatusEffectType.EnemyWeaponCostIncrease => true,
                StatusEffectType.CaptainDamageReflect => true,
                
                _ => false // Everything else is a buff
            };
        }

        /// <summary>
        /// Check if effect stacks (multiple applications increase value).
        /// </summary>
        public static bool IsStackable(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Marked => true,
                StatusEffectType.MoraleFocus => true,
                StatusEffectType.BonusPerCardsInHand => true,
                StatusEffectType.BonusPerBuzz => true,
                _ => false
            };
        }

        // ==================== FACTORY METHODS ====================

        public static StatusEffect CreateFire(int duration, float damagePerTurn, GameObject source = null)
            => new StatusEffect(StatusEffectType.Fire, "Burning", duration, damagePerTurn, 0f, source);

        public static StatusEffect CreatePoison(int duration, float damagePerTurn, GameObject source = null)
            => new StatusEffect(StatusEffectType.Poison, "Poisoned", duration, damagePerTurn, 0f, source);

        public static StatusEffect CreateBleed(int duration, float damageOnMove, GameObject source = null)
            => new StatusEffect(StatusEffectType.Bleed, "Bleeding", duration, damageOnMove, 0f, source);

        public static StatusEffect CreateGritBoost(int duration, float amount, GameObject source = null)
            => new StatusEffect(StatusEffectType.GritBoost, "Fortified", duration, amount, 0f, source);

        public static StatusEffect CreateGritReduction(int duration, float reduction, GameObject source = null)
            => new StatusEffect(StatusEffectType.GritReduction, "Armor Broken", duration, reduction, 0f, source);

        public static StatusEffect CreateAimBoost(int duration, float amount, GameObject source = null)
            => new StatusEffect(StatusEffectType.AimBoost, "Focused", duration, amount, 0f, source);

        public static StatusEffect CreatePowerBoost(int duration, float amount, GameObject source = null)
            => new StatusEffect(StatusEffectType.PowerBoost, "Empowered", duration, amount, 0f, source);

        public static StatusEffect CreateProficiencyBoost(int duration, float amount, GameObject source = null)
            => new StatusEffect(StatusEffectType.ProficiencyBoost, "Skilled", duration, amount, 0f, source);

        public static StatusEffect CreateHealthStatBoost(int duration, float amount, GameObject source = null)
            => new StatusEffect(StatusEffectType.HealthStatBoost, "Vitality", duration, amount, 0f, source);

        public static StatusEffect CreateSpeedReduction(int duration, float amount, GameObject source = null)
            => new StatusEffect(StatusEffectType.SpeedReduction, "Slowed", duration, amount, 0f, source);

        public static StatusEffect CreateDamageBoost(int duration, float percent, GameObject source = null)
            => new StatusEffect(StatusEffectType.DamageBoost, "Damage Up", duration, percent, 0f, source);

        public static StatusEffect CreateDamageReduction(int duration, float percent, GameObject source = null)
            => new StatusEffect(StatusEffectType.DamageReduction, "Protected", duration, percent, 0f, source);

        public static StatusEffect CreateVulnerable(int duration, float percent, GameObject source = null)
            => new StatusEffect(StatusEffectType.Vulnerable, "Vulnerable", duration, percent, 0f, source);

        public static StatusEffect CreateRangedDamageReduction(int duration, float percent, GameObject source = null)
            => new StatusEffect(StatusEffectType.RangedDamageReduction, "Ranged Shield", duration, percent, 0f, source);

        public static StatusEffect CreateMoraleDamageReduction(int duration, float percent, GameObject source = null)
            => new StatusEffect(StatusEffectType.MoraleDamageReduction, "Morale Shield", duration, percent, 0f, source);

        public static StatusEffect CreateMarked(int duration, float bonusDamage, GameObject source = null)
            => new StatusEffect(StatusEffectType.Marked, "Marked", duration, bonusDamage, 0f, source);

        public static StatusEffect CreateMoraleFocus(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.MoraleFocus, "Morale Focus", duration, 0f, 0f, source);

        public static StatusEffect CreateHealBlock(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.HealBlock, "Heal Block", duration, 0f, 0f, source);

        public static StatusEffect CreateMissChance(int duration, float missPercent, GameObject source = null)
            => new StatusEffect(StatusEffectType.MissChance, "Disoriented", duration, missPercent, 0f, source);

        public static StatusEffect CreateStun(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.Stun, "Stunned", duration, 0f, 0f, source);

        public static StatusEffect CreateSlowed(int duration, int movementReduction, GameObject source = null)
            => new StatusEffect(StatusEffectType.Slowed, "Slowed", duration, movementReduction, 0f, source);

        public static StatusEffect CreateMovementTrap(int duration, float damagePercent, GameObject source = null)
            => new StatusEffect(StatusEffectType.MovementTrap, "Trapped", duration, damagePercent, 0f, source);

        public static StatusEffect CreateFreeMove(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.FreeMove, "Free Move", duration, 0f, 0f, source);

        public static StatusEffect CreatePreventDisplacement(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.PreventDisplacement, "Anchored", duration, 0f, 0f, source);

        public static StatusEffect CreateForceTargetClosest(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.ForceTargetClosest, "Taunted", duration, 0f, 0f, source);

        public static StatusEffect CreateOnlyLowerHPCanTarget(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.OnlyLowerHPCanTarget, "Protected", duration, 0f, 0f, source);

        public static StatusEffect CreateRowCantBeTargeted(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.RowCantBeTargeted, "Covered", duration, 0f, 0f, source);

        public static StatusEffect CreateIgnoredByEnemies(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.IgnoredByEnemies, "Invisible", duration, 0f, 0f, source);

        public static StatusEffect CreateEnergyDrain(int duration, float amount, GameObject source = null)
            => new StatusEffect(StatusEffectType.EnergyDrain, "Energy Drain", duration, amount, 0f, source);

        public static StatusEffect CreateReduceCardDraw(int duration, int reduction, GameObject source = null)
            => new StatusEffect(StatusEffectType.ReduceCardDraw, "Draw Reduced", duration, reduction, 0f, source);

        public static StatusEffect CreateIncreaseCost(int duration, int increase, GameObject source = null)
            => new StatusEffect(StatusEffectType.IncreaseCost, "Cost Increased", duration, increase, 0f, source);

        public static StatusEffect CreateReduceNextRangedCost(int duration, int reduction, GameObject source = null)
            => new StatusEffect(StatusEffectType.ReduceNextRangedCost, "Ranged Discount", duration, reduction, 0f, source);

        public static StatusEffect CreatePreventBuzzReduction(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.PreventBuzzReduction, "Buzz Locked", duration, 0f, 0f, source);

        public static StatusEffect CreateBuzzFilled(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.BuzzFilled, "Drunk", duration, 0f, 0f, source);

        public static StatusEffect CreateEnemyBuzzOnDamage(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.EnemyBuzzOnDamage, "Sloppy", duration, 0f, 0f, source);

        public static StatusEffect CreateReturnDamage(int duration, int instances, GameObject source = null)
            => new StatusEffect(StatusEffectType.ReturnDamage, "Thorns", duration, instances, 0f, source);

        public static StatusEffect CreateStunOnKnockback(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.StunOnKnockback, "Braced", duration, 0f, 0f, source);

        public static StatusEffect CreateEnergyOnKnockback(int duration, int energyGain, GameObject source = null)
            => new StatusEffect(StatusEffectType.EnergyOnKnockback, "Spring Loaded", duration, energyGain, 0f, source);

        public static StatusEffect CreateDrawOnEnemyAttack(int duration, int maxDraws, GameObject source = null)
            => new StatusEffect(StatusEffectType.DrawOnEnemyAttack, "Counter Draw", duration, maxDraws, 0f, source);

        public static StatusEffect CreateDodgeFirstAttack(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.DodgeFirstAttack, "Evasive", duration, 0f, 0f, source);

        public static StatusEffect CreateKnockbackAttacker(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.KnockbackAttacker, "Repel", duration, 0f, 0f, source);

        public static StatusEffect CreateCounterAttack(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.CounterAttack, "Counter", duration, 0f, 0f, source);

        public static StatusEffect CreateCaptainDamageReflect(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.CaptainDamageReflect, "Damage Reflect", duration, 0f, 0f, source);

        public static StatusEffect CreateReflectMoraleDamage(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.ReflectMoraleDamage, "Morale Reflect", duration, 0f, 0f, source);

        public static StatusEffect CreateOnlyTargetThisTurn(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.OnlyTargetThisTurn, "Priority Target", duration, 0f, 0f, source);

        public static StatusEffect CreatePreventSurrender(int duration, float moraleRestore, GameObject source = null)
            => new StatusEffect(StatusEffectType.PreventSurrender, "Rally", duration, moraleRestore, 0f, source);

        public static StatusEffect CreateWeaponUseTwice(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.WeaponUseTwice, "Double Strike", duration, 0f, 0f, source);

        public static StatusEffect CreateFreeStows(int duration, int count, GameObject source = null)
            => new StatusEffect(StatusEffectType.FreeStows, "Quick Stow", duration, count, 0f, source);

        public static StatusEffect CreateFreeRumUsage(int duration, int count, GameObject source = null)
            => new StatusEffect(StatusEffectType.FreeRumUsage, "Open Bar", duration, count, 0f, source);

        public static StatusEffect CreateReducedRumEffect(int duration, float reduction, GameObject source = null)
            => new StatusEffect(StatusEffectType.ReducedRumEffect, "Tolerance", duration, reduction, 0f, source);

        public static StatusEffect CreateEnemyWeaponCostIncrease(int duration, int increase, GameObject source = null)
            => new StatusEffect(StatusEffectType.EnemyWeaponCostIncrease, "Disarm", duration, increase, 0f, source);

        public static StatusEffect CreateDisableNonWeaponRelics(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.DisableNonWeaponRelics, "Relic Lock", duration, 0f, 0f, source);

        public static StatusEffect CreateDisablePassives(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.DisablePassives, "Silence", duration, 0f, 0f, source);

        public static StatusEffect CreateHealOnCaptainDamage(int duration, float healPercent, GameObject source = null)
            => new StatusEffect(StatusEffectType.HealOnCaptainDamage, "Captain's Bane", duration, healPercent, 0f, source);

        public static StatusEffect CreateAttackOnEnemyHeal(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.AttackOnEnemyHeal, "Anti-Heal", duration, 0f, 0f, source);

        public static StatusEffect CreateStasis(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.Stasis, "Stasis", duration, 0f, 0f, source);

        public static StatusEffect CreateRegeneration(int duration, int healPerTurn, GameObject source = null)
            => new StatusEffect(StatusEffectType.Regeneration, "Regenerating", duration, healPerTurn, 0f, source);

        public static StatusEffect CreateShielded(int duration, float damageReduction, GameObject source = null)
            => new StatusEffect(StatusEffectType.Shielded, "Shielded", duration, damageReduction, 0f, source);

        public static StatusEffect CreateNoMoraleDamage(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.MoraleDamageReduction, "Morale Shield", duration, 1.0f, 0f, source);

        // Additional factory methods for full coverage
        public static StatusEffect CreateInvincible(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.Invincible, "Invincible", duration, 0f, 0f, source);

        public static StatusEffect CreateLowerHealthStat(int duration, int reduction, GameObject source = null)
            => new StatusEffect(StatusEffectType.HealthStatReduction, "Weakened", duration, reduction, 0f, source);

        public static StatusEffect CreateWeaponCostIncrease(int duration, int increase, GameObject source = null)
            => new StatusEffect(StatusEffectType.EnemyWeaponCostIncrease, "Disarm", duration, increase, 0f, source);

        // ==================== V2 FACTORY METHODS ====================
        
        public static StatusEffect CreateMoraleOnKill(int duration, float percent, GameObject source = null)
            => new StatusEffect(StatusEffectType.MoraleOnKill, "Bloodthirst", duration, percent, 0f, source);

        public static StatusEffect CreateBuzzGainReduction(int duration, float percent, GameObject source = null)
            => new StatusEffect(StatusEffectType.BuzzGainReduction, "Clear Head", duration, percent, 0f, source);

        public static StatusEffect CreateDodge(int duration, float percent, GameObject source = null)
            => new StatusEffect(StatusEffectType.Dodge, "Evasive", duration, percent, 0f, source);

        public static StatusEffect CreateSlow(int duration, int movementReduction, GameObject source = null)
            => new StatusEffect(StatusEffectType.Slowed, "Slowed", duration, movementReduction, 0f, source);

        public static StatusEffect CreateRumHealBoost(int duration, float percent, GameObject source = null)
            => new StatusEffect(StatusEffectType.RumHealBoost, "Rum Lover", duration, percent, 0f, source);

        public static StatusEffect CreateGrogOnKill(int duration, int amount, GameObject source = null)
            => new StatusEffect(StatusEffectType.GrogOnKill, "Plunderer", duration, amount, 0f, source);

        public static StatusEffect CreateSpeedBoost(int duration, int amount, GameObject source = null)
            => new StatusEffect(StatusEffectType.SpeedBoost, "Haste", duration, amount, 0f, source);

        public static StatusEffect CreateHealOnCardPlay(int duration, float percent, GameObject source = null)
            => new StatusEffect(StatusEffectType.HealOnCardPlay, "Meditative", duration, percent, 0f, source);

        public static StatusEffect CreateFoodEffectBoost(int duration, float multiplier, GameObject source = null)
            => new StatusEffect(StatusEffectType.FoodEffectBoost, "Well Fed", duration, multiplier, 0f, source);

        public static StatusEffect CreateReduceAllCosts(int duration, int reduction, GameObject source = null)
            => new StatusEffect(StatusEffectType.ReduceAllCosts, "Efficient", duration, reduction, 0f, source);

        public static StatusEffect CreateCounterOnAllyHit(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.CounterOnAllyHit, "Guardian", duration, 0f, 0f, source);

        public static StatusEffect CreateMoraleShield(int duration, int amount, GameObject source = null)
            => new StatusEffect(StatusEffectType.MoraleShield, "Stalwart", duration, amount, 0f, source);

        public static StatusEffect CreateDeathPrevention(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.DeathPrevention, "Last Stand", duration, 0f, 0f, source);

        public static StatusEffect CreateBuzzImmunity(int duration, GameObject source = null)
            => new StatusEffect(StatusEffectType.BuzzImmunity, "Clear Minded", duration, 0f, 0f, source);

        public static StatusEffect CreateThorns(int duration, int damage, GameObject source = null)
            => new StatusEffect(StatusEffectType.Thorns, "Thorns", duration, damage, 0f, source);

        public static StatusEffect CreateHealOverTime(int duration, float percentPerTurn, GameObject source = null)
            => new StatusEffect(StatusEffectType.Regeneration, "Regenerating", duration, percentPerTurn, 0f, source);

        public static StatusEffect CreateMaxHPBoost(int duration, float percent, GameObject source = null)
            => new StatusEffect(StatusEffectType.MaxHPBoost, "Fortified", duration, percent, 0f, source);

        public static StatusEffect CreateRangedBlock(int duration, int charges, GameObject source = null)
            => new StatusEffect(StatusEffectType.RangedBlock, "Arrow Ward", duration, charges, 0f, source);

        public static StatusEffect CreateWeakness(int duration, float percent, GameObject source = null)
            => new StatusEffect(StatusEffectType.Weakness, "Weakened", duration, percent, 0f, source);
    }
}