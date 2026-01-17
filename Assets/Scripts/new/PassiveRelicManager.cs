using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Enums;
using TacticalGame.Units;
using TacticalGame.Core;
using TacticalGame.Grid;
using TacticalGame.Managers;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Manages passive relic effects (Trinket + PassiveUnique).
    /// Subscribes to game events and applies effects when conditions are met.
    /// Attach to each unit that has UnitEquipmentUpdated.
    /// </summary>
    public class PassiveRelicManager : MonoBehaviour
    {
        private UnitStatus unitStatus;
        private UnitEquipmentUpdated equipment;
        private CardDeckManager deckManager;
        private GridManager gridManager;
        
        // Per-turn tracking
        private bool usedKnockbackRetaliation = false;
        private bool usedTaunt = false;
        private int damageTakenThisTurn = 0;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            unitStatus = GetComponent<UnitStatus>();
            equipment = GetComponent<UnitEquipmentUpdated>();
            deckManager = GetComponent<CardDeckManager>();
        }
        
        private void Start()
        {
            gridManager = ServiceLocator.Get<GridManager>();
        }
        
        private void OnEnable()
        {
            GameEvents.OnPlayerTurnStart += OnPlayerTurnStart;
            GameEvents.OnEnemyTurnStart += OnEnemyTurnStart;
            GameEvents.OnEnemyTurnEnd += OnEnemyTurnEnd;
            GameEvents.OnUnitDamaged += OnAnyUnitDamaged;
            GameEvents.OnUnitAttack += OnAnyUnitAttack;
            GameEvents.OnUnitDeath += OnAnyUnitDeath;
            GameEvents.OnUnitSurrender += OnAnyUnitSurrender;
            GameEvents.OnUnitMoved += OnAnyUnitMoved;
        }
        
        private void OnDisable()
        {
            GameEvents.OnPlayerTurnStart -= OnPlayerTurnStart;
            GameEvents.OnEnemyTurnStart -= OnEnemyTurnStart;
            GameEvents.OnEnemyTurnEnd -= OnEnemyTurnEnd;
            GameEvents.OnUnitDamaged -= OnAnyUnitDamaged;
            GameEvents.OnUnitAttack -= OnAnyUnitAttack;
            GameEvents.OnUnitDeath -= OnAnyUnitDeath;
            GameEvents.OnUnitSurrender -= OnAnyUnitSurrender;
            GameEvents.OnUnitMoved -= OnAnyUnitMoved;
        }
        
        #endregion
        
        #region Passive Checks
        
        private bool HasPassive(RelicEffectType type)
        {
            if (equipment == null) return false;
            
            var trinket = equipment.TrinketRelic;
            if (trinket?.effectData?.effectType == type) return true;
            
            var passive = equipment.PassiveUniqueRelic;
            if (passive?.effectData?.effectType == type) return true;
            
            return false;
        }
        
        private RelicEffectData GetPassiveData(RelicEffectType type)
        {
            if (equipment == null) return null;
            
            var trinket = equipment.TrinketRelic;
            if (trinket?.effectData?.effectType == type) return trinket.effectData;
            
            var passive = equipment.PassiveUniqueRelic;
            if (passive?.effectData?.effectType == type) return passive.effectData;
            
            return null;
        }
        
        #endregion
        
        #region Turn Start Events
        
        private void OnPlayerTurnStart()
        {
            if (unitStatus == null || unitStatus.Team != Team.Player) return;
            if (unitStatus.HasSurrendered) return;
            
            // Reset per-turn flags
            usedKnockbackRetaliation = false;
            usedTaunt = false;
            damageTakenThisTurn = 0;
            
            // === Captain PassiveUnique: +1 max energy each turn ===
            if (HasPassive(RelicEffectType.PassiveUnique_ExtraEnergy))
            {
                var energyManager = ServiceLocator.Get<EnergyManager>();
                if (energyManager != null)
                {
                    // Add bonus energy at turn start
                    energyManager.TrySpendEnergy(-1); // Negative spend = gain
                    Debug.Log($"<color=yellow>[Passive] {unitStatus.name}: Captain +1 Energy</color>");
                }
            }
            
            // === Quartermaster PassiveUnique: +2 cards each turn ===
            if (HasPassive(RelicEffectType.PassiveUnique_ExtraCards))
            {
                if (deckManager != null)
                {
                    deckManager.DrawCards(2);
                    Debug.Log($"<color=yellow>[Passive] {unitStatus.name}: Quartermaster +2 Cards</color>");
                }
            }
            
            // === Navigator Trinket: Draw extra if HP above 60% ===
            if (HasPassive(RelicEffectType.Trinket_DrawIfHighHP))
            {
                var data = GetPassiveData(RelicEffectType.Trinket_DrawIfHighHP);
                float threshold = data?.value2 ?? 0.6f;
                
                if (unitStatus.HPPercent > threshold && deckManager != null)
                {
                    deckManager.DrawCards(1);
                    Debug.Log($"<color=yellow>[Passive] {unitStatus.name}: Navigator Trinket +1 Card (HP > {threshold*100}%)</color>");
                }
            }
            
            // === Master Gunner PassiveUnique: Draw extra per grog ===
            if (HasPassive(RelicEffectType.PassiveUnique_DrawPerGrog))
            {
                var energyManager = ServiceLocator.Get<EnergyManager>();
                if (energyManager != null && deckManager != null)
                {
                    int grog = energyManager.GrogTokens;
                    if (grog > 0)
                    {
                        deckManager.DrawCards(grog);
                        Debug.Log($"<color=yellow>[Passive] {unitStatus.name}: Master Gunner +{grog} Cards (per grog)</color>");
                    }
                }
            }
        }
        
        private void OnEnemyTurnStart()
        {
            if (unitStatus == null || unitStatus.Team != Team.Player) return;
            
            // Reset taunt for enemy turn
            usedTaunt = false;
        }
        
        private void OnEnemyTurnEnd()
        {
            if (unitStatus == null || unitStatus.Team != Team.Player) return;
            
            // === Master-at-Arms PassiveUnique: Draw card if damage taken < 20% HP ===
            if (HasPassive(RelicEffectType.PassiveUnique_DrawOnLowDamage))
            {
                var data = GetPassiveData(RelicEffectType.PassiveUnique_DrawOnLowDamage);
                float threshold = data?.value2 ?? 0.2f;
                int thresholdDamage = Mathf.RoundToInt(unitStatus.MaxHP * threshold);
                
                if (damageTakenThisTurn > 0 && damageTakenThisTurn < thresholdDamage && deckManager != null)
                {
                    deckManager.DrawCards(1);
                    Debug.Log($"<color=yellow>[Passive] {unitStatus.name}: Master-at-Arms +1 Card (low damage taken)</color>");
                }
            }
            
            damageTakenThisTurn = 0;
        }
        
        #endregion
        
        #region Damage Events
        
        private void OnAnyUnitDamaged(GameObject damagedUnit, int damage)
        {
            if (unitStatus == null || unitStatus.HasSurrendered) return;
            
            var damagedStatus = damagedUnit?.GetComponent<UnitStatus>();
            if (damagedStatus == null) return;
            
            // Track damage to self
            if (damagedUnit == gameObject)
            {
                damageTakenThisTurn += damage;
                
                // === Cook Trinket: Knockback attacker once per turn ===
                if (HasPassive(RelicEffectType.Trinket_KnockbackAttacker) && !usedKnockbackRetaliation)
                {
                    // Need to find attacker - this would require tracking in the damage event
                    // For now, log placeholder
                    usedKnockbackRetaliation = true;
                    Debug.Log($"<color=yellow>[Passive] {unitStatus.name}: Cook Trinket - Knockback attacker (needs attacker tracking)</color>");
                }
            }
            
            // === Navigator PassiveUnique: Counter-attack when ally damaged ===
            if (HasPassive(RelicEffectType.PassiveUnique_CounterAttack))
            {
                if (damagedStatus.Team == unitStatus.Team && damagedUnit != gameObject)
                {
                    // Would need attacker reference to counter-attack
                    Debug.Log($"<color=yellow>[Passive] {unitStatus.name}: Navigator - Counter-attack triggered (needs attacker tracking)</color>");
                }
            }
        }
        
        private void OnAnyUnitAttack(GameObject attacker, GameObject target)
        {
            if (unitStatus == null || unitStatus.HasSurrendered) return;
            
            var attackerStatus = attacker?.GetComponent<UnitStatus>();
            var targetStatus = target?.GetComponent<UnitStatus>();
            
            if (attackerStatus == null || targetStatus == null) return;
            
            // === Surgeon Trinket: Taunt first attack per enemy turn ===
            if (HasPassive(RelicEffectType.Trinket_TauntFirstAttack) && !usedTaunt)
            {
                // If enemy is attacking an ally (not this unit), redirect to this unit
                if (attackerStatus.Team != unitStatus.Team && 
                    targetStatus.Team == unitStatus.Team && 
                    target != gameObject)
                {
                    usedTaunt = true;
                    Debug.Log($"<color=yellow>[Passive] {unitStatus.name}: Surgeon Trinket - Taunt! Redirecting attack to self</color>");
                    // Note: Actual redirection would need to happen in the attack system
                }
            }
        }
        
        #endregion
        
        #region Death/Surrender Events
        
        private void OnAnyUnitDeath(GameObject deadUnit)
        {
            HandleDeathOrSurrender(deadUnit, true);
        }
        
        private void OnAnyUnitSurrender(GameObject surrenderedUnit)
        {
            HandleDeathOrSurrender(surrenderedUnit, false);
        }
        
        private void HandleDeathOrSurrender(GameObject unit, bool isDeath)
        {
            if (unitStatus == null || unitStatus.HasSurrendered) return;
            
            var deadStatus = unit?.GetComponent<UnitStatus>();
            if (deadStatus == null) return;
            
            // === Boatswain Totem (Passive): Enemy death = morale swing ===
            if (HasPassive(RelicEffectType.Totem_EnemyDeathMoraleSwing))
            {
                if (deadStatus.Team != unitStatus.Team)
                {
                    var data = GetPassiveData(RelicEffectType.Totem_EnemyDeathMoraleSwing);
                    float moraleGain = data?.value2 ?? 0.05f;
                    
                    // Give morale to all player units
                    foreach (var ally in GetAllAllies())
                    {
                        int gain = Mathf.RoundToInt(ally.MaxMorale * moraleGain);
                        ally.RestoreMorale(gain);
                    }
                    Debug.Log($"<color=yellow>[Passive] {unitStatus.name}: Boatswain Totem - Enemy defeated, allies gain {moraleGain*100}% morale</color>");
                }
            }
            
            // === Helmsman PassiveUnique: Chance to attack on death based on morale ===
            if (unit == gameObject && HasPassive(RelicEffectType.PassiveUnique_DeathStrikeByMorale))
            {
                float moralePercent = unitStatus.MoralePercent;
                float chance = moralePercent; // Higher morale = higher chance
                
                if (Random.value < chance)
                {
                    Debug.Log($"<color=yellow>[Passive] {unitStatus.name}: Helmsman - Death strike! ({chance*100:F0}% chance)</color>");
                    // Would trigger an attack on nearest enemy
                }
            }
        }
        
        #endregion
        
        #region Movement Events
        
        private void OnAnyUnitMoved(GameObject unit, GridCell from, GridCell to)
        {
            if (unitStatus == null || unitStatus.HasSurrendered) return;
            
            var movedStatus = unit?.GetComponent<UnitStatus>();
            if (movedStatus == null) return;
            
            // === Master Gunner Trinket: Knockback increases enemy buzz ===
            if (HasPassive(RelicEffectType.Trinket_KnockbackIncreasesBuzz))
            {
                // Check if this was a knockback (enemy moved by force)
                if (movedStatus.Team != unitStatus.Team)
                {
                    // Would need to track if this was knockback vs voluntary move
                    // For now, skip - needs knockback tracking flag
                }
            }
        }
        
        #endregion
        
        #region Damage Modifier Interface
        
        /// <summary>
        /// Get damage modifier for an attack from this unit.
        /// Called by damage calculation system.
        /// </summary>
        public float GetOutgoingDamageModifier(UnitStatus target)
        {
            float modifier = 1f;
            if (equipment == null || unitStatus == null) return modifier;
            
            // === Captain Trinket: +20% per card in hand ===
            if (HasPassive(RelicEffectType.Trinket_BonusDamagePerCard))
            {
                var data = GetPassiveData(RelicEffectType.Trinket_BonusDamagePerCard);
                float perCard = data?.value2 ?? 0.2f;
                int cards = deckManager?.CardsInHand ?? 0;
                modifier += perCard * cards;
            }
            
            // === Quartermaster Trinket: +20% vs captain ===
            if (HasPassive(RelicEffectType.Trinket_BonusVsCaptain) && target != null)
            {
                var targetEquip = target.GetComponent<UnitEquipmentUpdated>();
                if (targetEquip?.UnitRole == UnitRole.Captain)
                {
                    var data = GetPassiveData(RelicEffectType.Trinket_BonusVsCaptain);
                    modifier += data?.value2 ?? 0.2f;
                }
            }
            
            // === Shipwright Trinket: +damage by buzz ===
            if (HasPassive(RelicEffectType.Trinket_DamageByBuzz))
            {
                float buzzPercent = unitStatus.MaxBuzz > 0 
                    ? (float)unitStatus.CurrentBuzz / unitStatus.MaxBuzz 
                    : 0f;
                modifier += buzzPercent * 0.5f; // Up to +50% at full buzz
            }
            
            // === Cook PassiveUnique: +20% vs low grit ===
            if (HasPassive(RelicEffectType.PassiveUnique_BonusVsLowGrit) && target != null)
            {
                if (target.Grit < unitStatus.Grit)
                {
                    var data = GetPassiveData(RelicEffectType.PassiveUnique_BonusVsLowGrit);
                    modifier += data?.value2 ?? 0.2f;
                }
            }
            
            // === Deckhand PassiveUnique: +damage vs <50% HP ===
            if (HasPassive(RelicEffectType.PassiveUnique_BonusVsLowHP) && target != null)
            {
                var data = GetPassiveData(RelicEffectType.PassiveUnique_BonusVsLowHP);
                float threshold = data?.value2 ?? 0.5f;
                if (target.HPPercent < threshold)
                {
                    modifier += 0.2f; // +20% bonus
                }
            }
            
            // === Deckhand Trinket: Enemies in row take +10% ===
            if (HasPassive(RelicEffectType.Trinket_RowEnemiesTakeMore) && target != null)
            {
                if (IsInSameRow(unitStatus, target))
                {
                    var data = GetPassiveData(RelicEffectType.Trinket_RowEnemiesTakeMore);
                    modifier += data?.value2 ?? 0.1f;
                }
            }
            
            return modifier;
        }
        
        /// <summary>
        /// Get damage modifier for incoming damage to this unit.
        /// Called by damage calculation system.
        /// </summary>
        public float GetIncomingDamageModifier(UnitStatus attacker)
        {
            float modifier = 1f;
            if (equipment == null || unitStatus == null) return modifier;
            
            // === Master-at-Arms Trinket: Closest enemy does -20% ===
            if (HasPassive(RelicEffectType.Trinket_ReduceDamageFromClosest) && attacker != null)
            {
                var closest = GetClosestEnemy();
                if (closest != null && closest == attacker)
                {
                    var data = GetPassiveData(RelicEffectType.Trinket_ReduceDamageFromClosest);
                    modifier -= data?.value2 ?? 0.2f;
                }
            }
            
            // === Swashbuckler Trinket: Enemies in row do -10% ===
            if (HasPassive(RelicEffectType.Trinket_RowEnemiesLessDamage) && attacker != null)
            {
                if (IsInSameRow(unitStatus, attacker))
                {
                    var data = GetPassiveData(RelicEffectType.Trinket_RowEnemiesLessDamage);
                    modifier -= data?.value2 ?? 0.1f;
                }
            }
            
            return Mathf.Max(0.1f, modifier); // Minimum 10% damage
        }
        
        /// <summary>
        /// Check if unit is immune to morale focus fire.
        /// </summary>
        public bool IsImmuneMoraleFocusFire()
        {
            return HasPassive(RelicEffectType.Trinket_ImmuneMoraleFocusFire);
        }
        
        /// <summary>
        /// Get modified surrender threshold.
        /// </summary>
        public float GetSurrenderThreshold()
        {
            // Default is 0.2 (20%)
            float threshold = 0.2f;
            
            // === Boatswain PassiveUnique: Allies surrender at 10% ===
            // This affects allies, so check all units with this passive
            foreach (var ally in GetAllAllies())
            {
                var allyPassive = ally.GetComponent<PassiveRelicManager>();
                if (allyPassive != null && allyPassive.HasPassive(RelicEffectType.PassiveUnique_LowerSurrenderThreshold))
                {
                    var data = allyPassive.GetPassiveData(RelicEffectType.PassiveUnique_LowerSurrenderThreshold);
                    threshold = Mathf.Min(threshold, data?.value2 ?? 0.1f);
                }
            }
            
            return threshold;
        }
        
        /// <summary>
        /// Check if buzz penalties are disabled.
        /// </summary>
        public bool HasNoBuzzDownside()
        {
            return HasPassive(RelicEffectType.PassiveUnique_NoBuzzDownside);
        }
        
        /// <summary>
        /// Get enemy surrender threshold (for Boatswain Trinket).
        /// </summary>
        public float GetEnemySurrenderThreshold()
        {
            if (HasPassive(RelicEffectType.Trinket_EnemySurrenderEarly))
            {
                var data = GetPassiveData(RelicEffectType.Trinket_EnemySurrenderEarly);
                return data?.value2 ?? 0.3f; // Enemies surrender at 30%
            }
            return 0.2f; // Default 20%
        }
        
        #endregion
        
        #region Helper Methods
        
        private List<UnitStatus> GetAllAllies()
        {
            return GameObject.FindGameObjectsWithTag("Unit")
                .Select(go => go.GetComponent<UnitStatus>())
                .Where(u => u != null && u.Team == unitStatus.Team && !u.HasSurrendered)
                .ToList();
        }
        
        private UnitStatus GetClosestEnemy()
        {
            Team enemyTeam = unitStatus.Team == Team.Player ? Team.Enemy : Team.Player;
            
            return GameObject.FindGameObjectsWithTag("Unit")
                .Select(go => go.GetComponent<UnitStatus>())
                .Where(u => u != null && u.Team == enemyTeam && !u.HasSurrendered)
                .OrderBy(u => Vector3.Distance(transform.position, u.transform.position))
                .FirstOrDefault();
        }
        
        private bool IsInSameRow(UnitStatus a, UnitStatus b)
        {
            if (gridManager == null)
                gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return false;
            
            Vector2Int posA = gridManager.WorldToGridPosition(a.transform.position);
            Vector2Int posB = gridManager.WorldToGridPosition(b.transform.position);
            
            return posA.y == posB.y;
        }
        
        #endregion
    }
}