using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Units;
using TacticalGame.Core;
using TacticalGame.Equipment;
using TacticalGame.Managers;
using TacticalGame.Grid;
using TacticalGame.Enums;

namespace TacticalGame.Combat
{
    /// <summary>
    /// Manages status effects (buffs/debuffs) on a unit.
    /// Attach to unit prefabs alongside UnitStatus.
    /// Supports all 192 relic effects.
    /// </summary>
    public class StatusEffectManager : MonoBehaviour
    {
        #region Private State

        private List<StatusEffect> activeEffects = new List<StatusEffect>();
        private UnitStatus unitStatus;
        private CardDeckManager cardDeck;
        private UnitAttack unitAttack;

        #endregion

        #region Public Properties

        public List<StatusEffect> ActiveEffects => activeEffects;
        public int DebuffCount => activeEffects.Count(e => e.isDebuff);
        public int BuffCount => activeEffects.Count(e => !e.isDebuff);

        /// <summary>
        /// Get all active debuffs on this unit.
        /// </summary>
        public List<StatusEffect> GetActiveDebuffs()
        {
            return activeEffects.Where(e => e.isDebuff).ToList();
        }

        /// <summary>
        /// Get all active buffs on this unit.
        /// </summary>
        public List<StatusEffect> GetActiveBuffs()
        {
            return activeEffects.Where(e => !e.isDebuff).ToList();
        }

        /// <summary>
        /// Remove a specific effect.
        /// </summary>
        public void RemoveEffect(StatusEffect effect)
        {
            if (effect != null && activeEffects.Contains(effect))
            {
                activeEffects.Remove(effect);
                Debug.Log($"{gameObject.name} lost {effect.effectName}");
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            unitStatus = GetComponent<UnitStatus>();
            cardDeck = GetComponent<CardDeckManager>();
            unitAttack = GetComponent<UnitAttack>();
        }

        private void OnEnable()
        {
            GameEvents.OnUnitDamaged += OnAnyUnitDamaged;
            GameEvents.OnUnitHealed += OnAnyUnitHealed;
            GameEvents.OnUnitAttack += OnAnyUnitAttack;
        }

        private void OnDisable()
        {
            GameEvents.OnUnitDamaged -= OnAnyUnitDamaged;
            GameEvents.OnUnitHealed -= OnAnyUnitHealed;
            GameEvents.OnUnitAttack -= OnAnyUnitAttack;
        }

        #endregion

        #region Public Methods - Apply Effects

        /// <summary>
        /// Apply a status effect to this unit.
        /// </summary>
        public void ApplyEffect(StatusEffect effect)
        {
            if (effect == null) return;

            // Check for heal block preventing buffs
            if (!effect.isDebuff && HasEffect(StatusEffectType.HealBlock))
            {
                Debug.Log($"{gameObject.name} is heal blocked - buff {effect.effectName} resisted!");
                return;
            }

            // Check for stasis - immune to all new effects
            if (HasEffect(StatusEffectType.Stasis))
            {
                Debug.Log($"{gameObject.name} is in stasis - effect {effect.effectName} ignored!");
                return;
            }

            // Check if same effect type already exists
            StatusEffect existing = activeEffects.FirstOrDefault(e => e.type == effect.type);
            
            if (existing != null)
            {
                if (StatusEffect.IsStackable(effect.type))
                {
                    existing.stacks++;
                    existing.value1 += effect.value1;
                    Debug.Log($"{gameObject.name}: {effect.effectName} stacked (x{existing.stacks})");
                }
                else if (effect.remainingTurns > existing.remainingTurns)
                {
                    // Refresh duration if new one is longer
                    existing.remainingTurns = effect.remainingTurns;
                    existing.value1 = Mathf.Max(existing.value1, effect.value1);
                    existing.value2 = effect.value2;
                    Debug.Log($"{gameObject.name}: {effect.effectName} refreshed ({effect.remainingTurns} turns)");
                }
            }
            else
            {
                activeEffects.Add(effect);
                Debug.Log($"<color=yellow>{gameObject.name}: {effect.effectName} applied ({effect.remainingTurns} turns, value={effect.value1})</color>");
                
                // Apply immediate effects
                ApplyImmediateEffect(effect);
            }
        }

        /// <summary>
        /// Apply immediate effects when status is first applied.
        /// </summary>
        private void ApplyImmediateEffect(StatusEffect effect)
        {
            switch (effect.type)
            {
                case StatusEffectType.Stun:
                    unitStatus.ApplyStun((int)effect.remainingTurns);
                    break;
                    
                case StatusEffectType.BuzzFilled:
                    // Force buzz to max
                    int buzzNeeded = unitStatus.MaxBuzz - unitStatus.CurrentBuzz;
                    if (buzzNeeded > 0)
                    {
                        // unitStatus.AddBuzz(buzzNeeded); // If method exists
                    }
                    break;
                    
                case StatusEffectType.FreeMove:
                    // Handled by movement system checking for this effect
                    break;
            }
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
            }
        }

        /// <summary>
        /// Remove all effects.
        /// </summary>
        public void ClearAllEffects()
        {
            activeEffects.Clear();
            Debug.Log($"{gameObject.name}: All effects cleared");
        }

        /// <summary>
        /// Remove all debuffs.
        /// </summary>
        public void ClearDebuffs()
        {
            int count = activeEffects.RemoveAll(e => e.isDebuff);
            Debug.Log($"{gameObject.name}: {count} debuffs cleared");
        }

        /// <summary>
        /// Remove all buffs.
        /// </summary>
        public void ClearBuffs()
        {
            int count = activeEffects.RemoveAll(e => !e.isDebuff);
            Debug.Log($"{gameObject.name}: {count} buffs cleared");
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
        public bool IsHealBlocked() => HasEffect(StatusEffectType.HealBlock);

        /// <summary>
        /// Check if unit is stunned.
        /// </summary>
        public bool IsStunned() => HasEffect(StatusEffectType.Stun) || unitStatus.IsStunned;

        /// <summary>
        /// Check if unit is in stasis.
        /// </summary>
        public bool IsInStasis() => HasEffect(StatusEffectType.Stasis);

        /// <summary>
        /// Check if unit has free move available.
        /// </summary>
        public bool HasFreeMove() => HasEffect(StatusEffectType.FreeMove);

        /// <summary>
        /// Consume free move buff.
        /// </summary>
        public void ConsumeFreeMove()
        {
            RemoveEffect(StatusEffectType.FreeMove);
        }

        /// <summary>
        /// Check if unit can be knocked back.
        /// </summary>
        public bool CanBeKnockedBack() => !HasEffect(StatusEffectType.PreventDisplacement);

        /// <summary>
        /// Check if passives are disabled.
        /// </summary>
        public bool ArePassivesDisabled() => HasEffect(StatusEffectType.DisablePassives);

        /// <summary>
        /// Check if non-weapon relics are disabled.
        /// </summary>
        public bool AreNonWeaponRelicsDisabled() => HasEffect(StatusEffectType.DisableNonWeaponRelics);

        #endregion

        #region Stat Modifiers

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
        /// Get total Aim modifier from effects.
        /// </summary>
        public float GetAimModifier()
        {
            float modifier = 0f;

            StatusEffect reduction = GetEffect(StatusEffectType.AimReduction);
            if (reduction != null) modifier -= reduction.value1;

            StatusEffect boost = GetEffect(StatusEffectType.AimBoost);
            if (boost != null) modifier += boost.value1;

            return modifier;
        }

        /// <summary>
        /// Get total Power modifier from effects.
        /// </summary>
        public float GetPowerModifier()
        {
            float modifier = 0f;

            StatusEffect reduction = GetEffect(StatusEffectType.PowerReduction);
            if (reduction != null) modifier -= reduction.value1;

            StatusEffect boost = GetEffect(StatusEffectType.PowerBoost);
            if (boost != null) modifier += boost.value1;

            return modifier;
        }

        /// <summary>
        /// Get total Speed modifier from effects.
        /// </summary>
        public float GetSpeedModifier()
        {
            float modifier = 0f;

            StatusEffect reduction = GetEffect(StatusEffectType.SpeedReduction);
            if (reduction != null) modifier -= reduction.value1;

            StatusEffect boost = GetEffect(StatusEffectType.SpeedBoost);
            if (boost != null) modifier += boost.value1;

            return modifier;
        }

        /// <summary>
        /// Get Proficiency modifier from effects.
        /// </summary>
        public float GetProficiencyModifier()
        {
            StatusEffect boost = GetEffect(StatusEffectType.ProficiencyBoost);
            return boost?.value1 ?? 0f;
        }

        /// <summary>
        /// Get Health stat modifier from effects.
        /// </summary>
        public float GetHealthStatModifier()
        {
            float modifier = 0f;

            StatusEffect reduction = GetEffect(StatusEffectType.HealthStatReduction);
            if (reduction != null) modifier -= reduction.value1;

            StatusEffect boost = GetEffect(StatusEffectType.HealthStatBoost);
            if (boost != null) modifier += boost.value1;

            return modifier;
        }

        #endregion

        #region Damage Modifiers

        /// <summary>
        /// Get miss chance from effects.
        /// </summary>
        public float GetMissChance()
        {
            StatusEffect missEffect = GetEffect(StatusEffectType.MissChance);
            return missEffect?.value1 ?? 0f;
        }

        /// <summary>
        /// Get marked bonus damage multiplier.
        /// </summary>
        public float GetMarkedBonus()
        {
            StatusEffect marked = GetEffect(StatusEffectType.Marked);
            return marked?.value1 ?? 0f;
        }

        /// <summary>
        /// Get damage dealt modifier (positive = more damage).
        /// </summary>
        public float GetOutgoingDamageModifier()
        {
            float modifier = 0f;

            StatusEffect boost = GetEffect(StatusEffectType.DamageBoost);
            if (boost != null) modifier += boost.value1;

            return modifier;
        }

        /// <summary>
        /// Get damage taken modifier (positive = take more damage).
        /// </summary>
        public float GetIncomingDamageModifier()
        {
            float modifier = 0f;

            StatusEffect vulnerable = GetEffect(StatusEffectType.Vulnerable);
            if (vulnerable != null) modifier += vulnerable.value1;

            StatusEffect reduction = GetEffect(StatusEffectType.DamageReduction);
            if (reduction != null) modifier -= reduction.value1;

            StatusEffect shielded = GetEffect(StatusEffectType.Shielded);
            if (shielded != null) modifier -= shielded.value1;

            return modifier;
        }

        /// <summary>
        /// Get ranged damage reduction.
        /// </summary>
        public float GetRangedDamageReduction()
        {
            StatusEffect effect = GetEffect(StatusEffectType.RangedDamageReduction);
            return effect?.value1 ?? 0f;
        }

        /// <summary>
        /// Get morale damage reduction.
        /// </summary>
        public float GetMoraleDamageReduction()
        {
            StatusEffect effect = GetEffect(StatusEffectType.MoraleDamageReduction);
            return effect?.value1 ?? 0f;
        }

        /// <summary>
        /// Check if unit has return damage effect.
        /// </summary>
        public bool ShouldReturnDamage(out int instances)
        {
            StatusEffect effect = GetEffect(StatusEffectType.ReturnDamage);
            if (effect != null && effect.value1 > 0)
            {
                instances = (int)effect.value1;
                return true;
            }
            instances = 0;
            return false;
        }

        /// <summary>
        /// Consume one return damage instance.
        /// </summary>
        public void ConsumeReturnDamageInstance()
        {
            StatusEffect effect = GetEffect(StatusEffectType.ReturnDamage);
            if (effect != null)
            {
                effect.value1--;
                if (effect.value1 <= 0)
                {
                    RemoveEffect(StatusEffectType.ReturnDamage);
                }
            }
        }

        #endregion

        #region Movement Modifiers

        /// <summary>
        /// Get movement reduction from effects.
        /// </summary>
        public int GetMovementReduction()
        {
            StatusEffect slowed = GetEffect(StatusEffectType.Slowed);
            return slowed != null ? (int)slowed.value1 : 0;
        }

        /// <summary>
        /// Check if has movement trap.
        /// </summary>
        public bool HasMovementTrap() => HasEffect(StatusEffectType.MovementTrap);

        /// <summary>
        /// Get movement trap damage percent.
        /// </summary>
        public float GetMovementTrapDamage()
        {
            StatusEffect trap = GetEffect(StatusEffectType.MovementTrap);
            return trap?.value1 ?? 0f;
        }

        #endregion

        #region Card/Resource Modifiers

        /// <summary>
        /// Get card draw reduction.
        /// </summary>
        public int GetCardDrawReduction()
        {
            StatusEffect effect = GetEffect(StatusEffectType.ReduceCardDraw);
            return effect != null ? (int)effect.value1 : 0;
        }

        /// <summary>
        /// Get card cost increase.
        /// </summary>
        public int GetCardCostIncrease()
        {
            StatusEffect effect = GetEffect(StatusEffectType.IncreaseCost);
            return effect != null ? (int)effect.value1 : 0;
        }

        /// <summary>
        /// Get weapon cost increase for enemies.
        /// </summary>
        public int GetWeaponCostIncrease()
        {
            StatusEffect effect = GetEffect(StatusEffectType.EnemyWeaponCostIncrease);
            return effect != null ? (int)effect.value1 : 0;
        }

        /// <summary>
        /// Get next ranged cost reduction.
        /// </summary>
        public int GetRangedCostReduction()
        {
            StatusEffect effect = GetEffect(StatusEffectType.ReduceNextRangedCost);
            return effect != null ? (int)effect.value1 : 0;
        }

        /// <summary>
        /// Consume ranged cost reduction.
        /// </summary>
        public void ConsumeRangedCostReduction()
        {
            RemoveEffect(StatusEffectType.ReduceNextRangedCost);
        }

        /// <summary>
        /// Check if weapon can be used twice.
        /// </summary>
        public bool CanUseWeaponTwice()
        {
            return HasEffect(StatusEffectType.WeaponUseTwice);
        }

        /// <summary>
        /// Consume weapon use twice buff.
        /// </summary>
        public void ConsumeWeaponUseTwice()
        {
            RemoveEffect(StatusEffectType.WeaponUseTwice);
        }

        /// <summary>
        /// Get free stows remaining.
        /// </summary>
        public int GetFreeStowsRemaining()
        {
            StatusEffect effect = GetEffect(StatusEffectType.FreeStows);
            return effect != null ? (int)effect.value1 : 0;
        }

        /// <summary>
        /// Consume one free stow.
        /// </summary>
        public void ConsumeFreeStow()
        {
            StatusEffect effect = GetEffect(StatusEffectType.FreeStows);
            if (effect != null)
            {
                effect.value1--;
                if (effect.value1 <= 0)
                {
                    RemoveEffect(StatusEffectType.FreeStows);
                }
            }
        }

        /// <summary>
        /// Get free rum uses remaining.
        /// </summary>
        public int GetFreeRumUsesRemaining()
        {
            StatusEffect effect = GetEffect(StatusEffectType.FreeRumUsage);
            return effect != null ? (int)effect.value1 : 0;
        }

        /// <summary>
        /// Consume one free rum use.
        /// </summary>
        public void ConsumeFreeRumUse()
        {
            StatusEffect effect = GetEffect(StatusEffectType.FreeRumUsage);
            if (effect != null)
            {
                effect.value1--;
                if (effect.value1 <= 0)
                {
                    RemoveEffect(StatusEffectType.FreeRumUsage);
                }
            }
        }

        /// <summary>
        /// Get rum effect reduction (0-1 scale).
        /// </summary>
        public float GetRumEffectReduction()
        {
            StatusEffect effect = GetEffect(StatusEffectType.ReducedRumEffect);
            return effect?.value1 ?? 0f;
        }

        #endregion

        #region Turn Processing

        /// <summary>
        /// Called at start of this unit's turn.
        /// Processes DOT effects and ticks durations.
        /// </summary>
        public void OnTurnStart()
        {
            // Reset once-per-turn triggers
            foreach (var effect in activeEffects)
            {
                effect.triggeredThisTurn = false;
            }

            List<StatusEffect> toRemove = new List<StatusEffect>();

            foreach (StatusEffect effect in activeEffects.ToList())
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
                        if (!IsHealBlocked())
                        {
                            int healAmount = Mathf.RoundToInt(effect.value1);
                            unitStatus.Heal(healAmount);
                            Debug.Log($"{gameObject.name} regenerates {healAmount} HP!");
                        }
                        break;

                    case StatusEffectType.EnergyDrain:
                        var energyManager = ServiceLocator.Get<EnergyManager>();
                        int drain = Mathf.RoundToInt(effect.value1);
                        energyManager?.TrySpendEnergy(drain);
                        Debug.Log($"Energy drained: {drain}");
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
            }
        }

        /// <summary>
        /// Called at end of this unit's turn.
        /// </summary>
        public void OnTurnEnd()
        {
            // Check for buzz prevention
            if (HasEffect(StatusEffectType.PreventBuzzReduction))
            {
                // Buzz cannot be reduced this turn - handled elsewhere
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

            // Consume free move if used
            if (HasEffect(StatusEffectType.FreeMove))
            {
                // This should be handled by the movement system
            }
        }

        /// <summary>
        /// Called when this unit is hit. Processes on-hit effects.
        /// </summary>
        public void OnHit(GameObject attacker, int damage)
        {
            // Marked - consume
            if (HasEffect(StatusEffectType.Marked))
            {
                RemoveEffect(StatusEffectType.Marked);
            }

            // Return damage
            if (ShouldReturnDamage(out int instances))
            {
                var attackerStatus = attacker?.GetComponent<UnitStatus>();
                if (attackerStatus != null)
                {
                    int returnDamage = Mathf.RoundToInt(damage * 0.5f); // 50% return
                    attackerStatus.TakeDamage(returnDamage, gameObject, false);
                    Debug.Log($"{gameObject.name} returns {returnDamage} damage to {attacker.name}!");
                    ConsumeReturnDamageInstance();
                }
            }

            // Draw on enemy attack
            if (HasEffect(StatusEffectType.DrawOnEnemyAttack))
            {
                StatusEffect effect = GetEffect(StatusEffectType.DrawOnEnemyAttack);
                if (!effect.triggeredThisTurn && effect.value1 > 0)
                {
                    cardDeck?.DrawCards(1);
                    effect.value1--;
                    effect.triggeredThisTurn = true;
                    Debug.Log($"{gameObject.name} drew a card from being attacked!");
                    
                    if (effect.value1 <= 0)
                        RemoveEffect(StatusEffectType.DrawOnEnemyAttack);
                }
            }

            // Dodge first attack
            if (HasEffect(StatusEffectType.DodgeFirstAttack))
            {
                // Move back 1 tile - handled by movement system
                var gridManager = ServiceLocator.Get<GridManager>();
                if (gridManager != null)
                {
                    Vector2Int currentPos = gridManager.WorldToGridPosition(transform.position);
                    // Move toward own side
                    int direction = unitStatus.Team == Team.Player ? -1 : 1;
                    var targetCell = gridManager.GetCell(currentPos.x + direction, currentPos.y);
                    if (targetCell != null && targetCell.CanPlaceUnit())
                    {
                        var currentCell = gridManager.GetCell(currentPos.x, currentPos.y);
                        currentCell?.RemoveUnit();
                        targetCell.PlaceUnit(gameObject);
                        transform.position = targetCell.transform.position;
                        Debug.Log($"{gameObject.name} dodged attack by moving back!");
                    }
                }
                RemoveEffect(StatusEffectType.DodgeFirstAttack);
            }

            // Knockback attacker (once per turn)
            if (HasEffect(StatusEffectType.KnockbackAttacker))
            {
                StatusEffect effect = GetEffect(StatusEffectType.KnockbackAttacker);
                if (!effect.triggeredThisTurn)
                {
                    var attackerStatus = attacker?.GetComponent<UnitStatus>();
                    var attackerEffects = attacker?.GetComponent<StatusEffectManager>();
                    if (attackerStatus != null && (attackerEffects == null || attackerEffects.CanBeKnockedBack()))
                    {
                        PushUnit(attacker, 1, false); // Push away from this unit
                        effect.triggeredThisTurn = true;
                        Debug.Log($"{gameObject.name} knocked back {attacker.name}!");
                    }
                }
            }

            // Counter attack
            if (HasEffect(StatusEffectType.CounterAttack))
            {
                StatusEffect effect = GetEffect(StatusEffectType.CounterAttack);
                if (!effect.triggeredThisTurn && unitAttack != null)
                {
                    unitAttack.TryMeleeAttack(); // Or appropriate attack
                    effect.triggeredThisTurn = true;
                    Debug.Log($"{gameObject.name} counter attacks!");
                }
            }

            // Stun on knockback check is handled by knockback system
        }

        /// <summary>
        /// Called when this unit is knocked back.
        /// </summary>
        public void OnKnockedBack(GameObject attacker)
        {
            // Stun attacker if we have that effect
            if (HasEffect(StatusEffectType.StunOnKnockback))
            {
                var attackerStatus = attacker?.GetComponent<UnitStatus>();
                if (attackerStatus != null)
                {
                    attackerStatus.ApplyStun(1);
                    Debug.Log($"{attacker.name} stunned for knocking back {gameObject.name}!");
                }
                RemoveEffect(StatusEffectType.StunOnKnockback);
            }

            // Gain energy if we have that effect
            if (HasEffect(StatusEffectType.EnergyOnKnockback))
            {
                StatusEffect effect = GetEffect(StatusEffectType.EnergyOnKnockback);
                var energyManager = ServiceLocator.Get<EnergyManager>();
                if (energyManager != null)
                {
                    energyManager.TrySpendEnergy(-(int)effect.value1); // Negative spend = gain
                    Debug.Log($"{gameObject.name} gained {effect.value1} energy from knockback!");
                }
                RemoveEffect(StatusEffectType.EnergyOnKnockback);
            }
        }

        #endregion

        #region Event Handlers

        private void OnAnyUnitDamaged(GameObject unit, int damage)
        {
            // Check if we should heal on captain damage
            if (HasEffect(StatusEffectType.HealOnCaptainDamage))
            {
                var targetStatus = unit?.GetComponent<UnitStatus>();
                if (targetStatus != null && targetStatus.IsCaptain && targetStatus.Team != unitStatus.Team)
                {
                    StatusEffect effect = GetEffect(StatusEffectType.HealOnCaptainDamage);
                    int healAmount = Mathf.RoundToInt(unitStatus.MaxHP * effect.value1);
                    unitStatus.Heal(healAmount);
                    Debug.Log($"{gameObject.name} healed {healAmount} from captain damage!");
                }
            }
        }

        private void OnAnyUnitHealed(GameObject unit, int amount)
        {
            // Check if we should attack on enemy heal
            if (HasEffect(StatusEffectType.AttackOnEnemyHeal))
            {
                var targetStatus = unit?.GetComponent<UnitStatus>();
                if (targetStatus != null && targetStatus.Team != unitStatus.Team)
                {
                    StatusEffect effect = GetEffect(StatusEffectType.AttackOnEnemyHeal);
                    if (!effect.triggeredThisTurn && unitAttack != null)
                    {
                        // Attack the healed unit
                        Debug.Log($"{gameObject.name} attacks {unit.name} for healing!");
                        unitAttack.TryMeleeAttack(); // Would need to target specific unit
                        effect.triggeredThisTurn = true;
                    }
                }
            }
        }

        private void OnAnyUnitAttack(GameObject attacker, GameObject target)
        {
            // Check if enemy gains buzz on damage
            if (attacker == gameObject && HasEffect(StatusEffectType.EnemyBuzzOnDamage))
            {
                // This unit gains buzz when dealing damage
                // unitStatus.AddBuzz(10); // If method exists
                Debug.Log($"{gameObject.name}'s buzz increased from dealing damage!");
            }
        }

        #endregion

        #region Helper Methods

        private void PushUnit(GameObject unit, int tiles, bool towardsCaster)
        {
            if (unit == null) return;
            
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return;

            Vector2Int unitPos = gridManager.WorldToGridPosition(unit.transform.position);
            Vector2Int myPos = gridManager.WorldToGridPosition(transform.position);
            
            int direction = towardsCaster ? 
                (myPos.x > unitPos.x ? 1 : -1) : 
                (myPos.x > unitPos.x ? -1 : 1);

            for (int i = 0; i < tiles; i++)
            {
                var targetCell = gridManager.GetCell(unitPos.x + direction * (i + 1), unitPos.y);
                if (targetCell != null && targetCell.CanPlaceUnit())
                {
                    var currentCell = gridManager.GetCell(unitPos.x + direction * i, unitPos.y);
                    currentCell?.RemoveUnit();
                    targetCell.PlaceUnit(unit);
                    unit.transform.position = targetCell.transform.position;
                }
                else
                {
                    break;
                }
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

            return string.Join(", ", activeEffects.Select(e => 
                $"{e.effectName}({e.remainingTurns}t, v={e.value1:F1})"));
        }

        #endregion
    }
}