using UnityEngine;
using TacticalGame.Enums;

namespace TacticalGame.Combat
{
    /// <summary>
    /// Types of status effects that can be applied to units.
    /// </summary>
    public enum StatusEffectType
    {
        None,
        
        // Damage Over Time
        Fire,               // Takes damage each turn
        Poison,             // Takes damage each turn (lower but longer)
        Bleed,              // Takes damage when moving
        
        // Debuffs
        HealBlock,          // Cannot restore HP or Morale
        MissChance,         // Chance to miss attacks
        Marked,             // Takes bonus damage from next hit
        Slowed,             // Reduced movement
        GritReduction,      // Reduced Grit stat
        EnergyDrain,        // Lose energy next turn
        MovementTrap,       // Takes damage if moves
        
        // Buffs
        GritBoost,          // Increased Grit
        DamageBoost,        // Increased damage
        Shielded,           // Damage reduction
        Regeneration,       // Heal over time
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
        public GameObject source;   // Who applied this effect
        public bool isDebuff;       // True = debuff, False = buff

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
            this.source = source;
            this.isDebuff = IsDebuffType(type);
        }

        /// <summary>
        /// Tick down duration. Returns true if effect expired.
        /// </summary>
        public bool Tick()
        {
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
                StatusEffectType.Fire => true,
                StatusEffectType.Poison => true,
                StatusEffectType.Bleed => true,
                StatusEffectType.HealBlock => true,
                StatusEffectType.MissChance => true,
                StatusEffectType.Marked => true,
                StatusEffectType.Slowed => true,
                StatusEffectType.GritReduction => true,
                StatusEffectType.EnergyDrain => true,
                StatusEffectType.MovementTrap => true,
                _ => false
            };
        }

        /// <summary>
        /// Create common status effects.
        /// </summary>
        public static StatusEffect CreateFire(int duration, float damagePerTurn, GameObject source = null)
        {
            return new StatusEffect(StatusEffectType.Fire, "Burning", duration, damagePerTurn, 0f, source);
        }

        public static StatusEffect CreateHealBlock(int duration, GameObject source = null)
        {
            return new StatusEffect(StatusEffectType.HealBlock, "Heal Block", duration, 0f, 0f, source);
        }

        public static StatusEffect CreateMissChance(int duration, float missPercent, GameObject source = null)
        {
            return new StatusEffect(StatusEffectType.MissChance, "Disoriented", duration, missPercent, 0f, source);
        }

        public static StatusEffect CreateMarked(int duration, float bonusDamage, GameObject source = null)
        {
            return new StatusEffect(StatusEffectType.Marked, "Marked", duration, bonusDamage, 0f, source);
        }

        public static StatusEffect CreateGritReduction(int duration, float reduction, GameObject source = null)
        {
            return new StatusEffect(StatusEffectType.GritReduction, "Armor Broken", duration, reduction, 0f, source);
        }

        public static StatusEffect CreateMovementTrap(int duration, float damagePercent, GameObject source = null)
        {
            return new StatusEffect(StatusEffectType.MovementTrap, "Trapped", duration, damagePercent, 0f, source);
        }

        public static StatusEffect CreateGritBoost(int duration, float amount, GameObject source = null)
        {
            return new StatusEffect(StatusEffectType.GritBoost, "Fortified", duration, amount, 0f, source);
        }

        public static StatusEffect CreateEnergyDrain(int duration, float amount, GameObject source = null)
        {
            return new StatusEffect(StatusEffectType.EnergyDrain, "Energy Drain", duration, amount, 0f, source);
        }
    }
}