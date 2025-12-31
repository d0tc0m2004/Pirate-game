using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Units;
using TacticalGame.Core;

namespace TacticalGame.Combat
{
    /// <summary>
    /// Manages status effects (buffs/debuffs) on a unit.
    /// Attach to unit prefabs alongside UnitStatus.
    /// </summary>
    public class StatusEffectManager : MonoBehaviour
    {
        #region Private State

        private List<StatusEffect> activeEffects = new List<StatusEffect>();
        private UnitStatus unitStatus;

        #endregion

        #region Public Properties

        public List<StatusEffect> ActiveEffects => activeEffects;
        public int DebuffCount => activeEffects.Count(e => e.isDebuff);
        public int BuffCount => activeEffects.Count(e => !e.isDebuff);

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            unitStatus = GetComponent<UnitStatus>();
        }

        #endregion

        #region Public Methods - Apply Effects

        /// <summary>
        /// Apply a status effect to this unit.
        /// </summary>
        public void ApplyEffect(StatusEffect effect)
        {
            // Check for heal block preventing buffs
            if (!effect.isDebuff && HasEffect(StatusEffectType.HealBlock))
            {
                Debug.Log($"{gameObject.name} is heal blocked - buff {effect.effectName} resisted!");
                return;
            }

            // Check if same effect type already exists
            StatusEffect existing = activeEffects.FirstOrDefault(e => e.type == effect.type);
            
            if (existing != null)
            {
                // Refresh duration if new one is longer
                if (effect.remainingTurns > existing.remainingTurns)
                {
                    existing.remainingTurns = effect.remainingTurns;
                    existing.value1 = effect.value1;
                    existing.value2 = effect.value2;
                }
                Debug.Log($"{gameObject.name}: {effect.effectName} refreshed ({effect.remainingTurns} turns)");
            }
            else
            {
                activeEffects.Add(effect);
                Debug.Log($"<color=yellow>{gameObject.name}: {effect.effectName} applied ({effect.remainingTurns} turns)</color>");
            }

            // Fire event (if your GameEvents has this method, uncomment)
            // GameEvents.TriggerStatusEffectApplied(gameObject, effect.effectName);
        }

        /// <summary>
        /// Remove a specific effect type.
        /// </summary>
        public void RemoveEffect(StatusEffectType type)
        {
            StatusEffect effect = activeEffects.FirstOrDefault(e => e.type == type);
            if (effect != null)
            {
                activeEffects.Remove(effect);
                Debug.Log($"{gameObject.name}: {effect.effectName} removed");
                // GameEvents.TriggerStatusEffectRemoved(gameObject, effect.effectName);
            }
        }

        /// <summary>
        /// Remove all effects.
        /// </summary>
        public void ClearAllEffects()
        {
            activeEffects.Clear();
        }

        /// <summary>
        /// Remove all debuffs.
        /// </summary>
        public void ClearDebuffs()
        {
            activeEffects.RemoveAll(e => e.isDebuff);
        }

        #endregion

        #region Public Methods - Query Effects

        /// <summary>
        /// Check if unit has a specific effect.
        /// </summary>
        public bool HasEffect(StatusEffectType type)
        {
            return activeEffects.Any(e => e.type == type);
        }

        /// <summary>
        /// Get a specific effect.
        /// </summary>
        public StatusEffect GetEffect(StatusEffectType type)
        {
            return activeEffects.FirstOrDefault(e => e.type == type);
        }

        /// <summary>
        /// Check if unit is heal blocked.
        /// </summary>
        public bool IsHealBlocked()
        {
            return HasEffect(StatusEffectType.HealBlock);
        }

        /// <summary>
        /// Get miss chance from effects.
        /// </summary>
        public float GetMissChance()
        {
            StatusEffect missEffect = GetEffect(StatusEffectType.MissChance);
            return missEffect?.value1 ?? 0f;
        }

        /// <summary>
        /// Get marked bonus damage.
        /// </summary>
        public float GetMarkedBonus()
        {
            StatusEffect marked = GetEffect(StatusEffectType.Marked);
            return marked?.value1 ?? 0f;
        }

        /// <summary>
        /// Get total Grit modifier from effects.
        /// </summary>
        public float GetGritModifier()
        {
            float modifier = 0f;

            StatusEffect reduction = GetEffect(StatusEffectType.GritReduction);
            if (reduction != null) modifier -= reduction.value1;

            StatusEffect boost = GetEffect(StatusEffectType.GritBoost);
            if (boost != null) modifier += boost.value1;

            return modifier;
        }

        /// <summary>
        /// Get damage boost from effects.
        /// </summary>
        public float GetDamageBoostPercent()
        {
            StatusEffect boost = GetEffect(StatusEffectType.DamageBoost);
            return boost?.value1 ?? 0f;
        }

        /// <summary>
        /// Check if has movement trap.
        /// </summary>
        public bool HasMovementTrap()
        {
            return HasEffect(StatusEffectType.MovementTrap);
        }

        /// <summary>
        /// Get movement trap damage percent.
        /// </summary>
        public float GetMovementTrapDamage()
        {
            StatusEffect trap = GetEffect(StatusEffectType.MovementTrap);
            return trap?.value1 ?? 0f;
        }

        #endregion

        #region Turn Processing

        /// <summary>
        /// Called at start of this unit's turn.
        /// Processes DOT effects and ticks durations.
        /// </summary>
        public void OnTurnStart()
        {
            List<StatusEffect> toRemove = new List<StatusEffect>();

            foreach (StatusEffect effect in activeEffects)
            {
                // Process turn-start effects
                switch (effect.type)
                {
                    case StatusEffectType.Fire:
                        int fireDamage = Mathf.RoundToInt(effect.value1);
                        unitStatus.TakeDamage(fireDamage, effect.source, false);
                        Debug.Log($"{gameObject.name} takes {fireDamage} fire damage!");
                        break;

                    case StatusEffectType.Poison:
                        int poisonDamage = Mathf.RoundToInt(effect.value1);
                        unitStatus.TakeDamage(poisonDamage, effect.source, false);
                        Debug.Log($"{gameObject.name} takes {poisonDamage} poison damage!");
                        break;

                    case StatusEffectType.Regeneration:
                        int healAmount = Mathf.RoundToInt(effect.value1);
                        if (!IsHealBlocked())
                        {
                            unitStatus.Heal(healAmount);
                            Debug.Log($"{gameObject.name} regenerates {healAmount} HP!");
                        }
                        break;

                    case StatusEffectType.EnergyDrain:
                        // This is handled by EnergyManager
                        break;
                }

                // Tick duration
                if (effect.Tick())
                {
                    toRemove.Add(effect);
                }
            }

            // Remove expired effects
            foreach (StatusEffect expired in toRemove)
            {
                activeEffects.Remove(expired);
                Debug.Log($"{gameObject.name}: {expired.effectName} expired");
                // GameEvents.TriggerStatusEffectRemoved(gameObject, expired.effectName);
            }
        }

        /// <summary>
        /// Called when unit moves. Triggers movement-based effects.
        /// </summary>
        public void OnUnitMoved()
        {
            // Bleed damage on movement
            if (HasEffect(StatusEffectType.Bleed))
            {
                StatusEffect bleed = GetEffect(StatusEffectType.Bleed);
                int bleedDamage = Mathf.RoundToInt(bleed.value1);
                unitStatus.TakeDamage(bleedDamage, bleed.source, false);
                Debug.Log($"{gameObject.name} takes {bleedDamage} bleed damage from moving!");
            }

            // Movement trap damage
            if (HasEffect(StatusEffectType.MovementTrap))
            {
                StatusEffect trap = GetEffect(StatusEffectType.MovementTrap);
                int trapDamage = Mathf.RoundToInt(unitStatus.CurrentHP * trap.value1);
                unitStatus.TakeDamage(trapDamage, trap.source, false);
                Debug.Log($"{gameObject.name} takes {trapDamage} trap damage from moving!");
                RemoveEffect(StatusEffectType.MovementTrap);
            }
        }

        /// <summary>
        /// Called when unit is hit. Processes on-hit effects.
        /// </summary>
        public void OnHit(GameObject attacker)
        {
            // Marked - consume and return bonus damage
            if (HasEffect(StatusEffectType.Marked))
            {
                // Bonus is applied in damage calculation
                // Remove after being hit
                RemoveEffect(StatusEffectType.Marked);
            }
        }

        #endregion

        #region Debug

        /// <summary>
        /// Get debug string of all active effects.
        /// </summary>
        public string GetEffectsDebugString()
        {
            if (activeEffects.Count == 0) return "No effects";

            return string.Join(", ", activeEffects.Select(e => $"{e.effectName}({e.remainingTurns})"));
        }

        #endregion
    }
}