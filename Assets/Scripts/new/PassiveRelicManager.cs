using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Units;
using TacticalGame.Core;
using TacticalGame.Grid;
using TacticalGame.Enums;
using TacticalGame.Combat;
using TacticalGame.Managers;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Manages passive relic effects for a unit.
    /// Handles all Trinket and PassiveUnique effects via event hooks.
    /// Attach to unit prefabs alongside UnitStatus.
    /// </summary>
    public class PassiveRelicManager : MonoBehaviour
    {
        #region Private State

        private UnitStatus unitStatus;
        private UnitAttack unitAttack;
        private CardDeckManager cardDeck;
        private StatusEffectManager statusEffects;
        private UnitEquipmentUpdated equipment;
        
        private List<RelicEffectType> activePassives = new List<RelicEffectType>();
        
        // Tracking for conditional passives
        private bool knockbackAttackerUsedThisTurn = false;
        private int weaponsUsedOnCurrentTarget = 0;
        private GameObject currentTarget;
        private int hullsDestroyedThisGame = 0;

        #endregion

        #region Properties

        public IReadOnlyList<RelicEffectType> ActivePassives => activePassives;
        public int HullsDestroyedThisGame => hullsDestroyedThisGame;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            unitStatus = GetComponent<UnitStatus>();
            unitAttack = GetComponent<UnitAttack>();
            cardDeck = GetComponent<CardDeckManager>();
            statusEffects = GetComponent<StatusEffectManager>();
            equipment = GetComponent<UnitEquipmentUpdated>();
        }

        private void Start()
        {
            RegisterPassiveEffects();
        }

        private void OnEnable()
        {
            // Subscribe to game events
            GameEvents.OnUnitDamaged += OnUnitDamaged;
            GameEvents.OnUnitHealed += OnUnitHealed;
            GameEvents.OnUnitDeath += OnUnitDeath;
            GameEvents.OnUnitSurrender += OnUnitSurrender;
            GameEvents.OnUnitAttack += OnUnitAttack;
            GameEvents.OnUnitMoved += OnUnitMoved;
            GameEvents.OnPlayerTurnStart += OnPlayerTurnStart;
            GameEvents.OnPlayerTurnEnd += OnPlayerTurnEnd;
            GameEvents.OnRoundStart += OnRoundStart;
        }

        private void OnDisable()
        {
            GameEvents.OnUnitDamaged -= OnUnitDamaged;
            GameEvents.OnUnitHealed -= OnUnitHealed;
            GameEvents.OnUnitDeath -= OnUnitDeath;
            GameEvents.OnUnitSurrender -= OnUnitSurrender;
            GameEvents.OnUnitAttack -= OnUnitAttack;
            GameEvents.OnUnitMoved -= OnUnitMoved;
            GameEvents.OnPlayerTurnStart -= OnPlayerTurnStart;
            GameEvents.OnPlayerTurnEnd -= OnPlayerTurnEnd;
            GameEvents.OnRoundStart -= OnRoundStart;
        }

        #endregion

        #region Passive Registration

        /// <summary>
        /// Register all passive effects from equipped relics.
        /// </summary>
        public void RegisterPassiveEffects()
        {
            activePassives.Clear();

            if (equipment == null) return;

            // Get all passive relics
            var passiveRelics = equipment.GetPassiveRelics();
            foreach (var relic in passiveRelics)
            {
                activePassives.Add(relic.GetEffectType());
                Debug.Log($"<color=magenta>{gameObject.name}: Registered passive {relic.relicName}</color>");
            }
        }

        /// <summary>
        /// Check if a passive effect is active.
        /// </summary>
        public bool HasPassive(RelicEffectType effectType)
        {
            // Check if passives are disabled by status effect
            if (statusEffects != null && statusEffects.ArePassivesDisabled())
                return false;
                
            return activePassives.Contains(effectType);
        }

        #endregion

        #region Event Handlers

        private void OnPlayerTurnStart()
        {
            if (unitStatus.Team != Team.Player) return;
            
            knockbackAttackerUsedThisTurn = false;
            
            // PassiveUnique_ExtraEnergy - Captain V1
            if (HasPassive(RelicEffectType.PassiveUnique_ExtraEnergy))
            {
                var energyManager = ServiceLocator.Get<EnergyManager>();
                energyManager?.TrySpendEnergy(-1); // Gain 1 energy
                Debug.Log($"<color=cyan>{gameObject.name}: +1 energy from passive</color>");
            }
            
            // PassiveUnique_ExtraCards / ExtraCardsEachTurn - Quartermaster V1/Captain V2
            if (HasPassive(RelicEffectType.PassiveUnique_ExtraCards) || 
                HasPassive(RelicEffectType.PassiveUnique_ExtraCards))
            {
                cardDeck?.DrawCards(2);
                Debug.Log($"<color=cyan>{gameObject.name}: +2 cards from passive</color>");
            }
            
            // PassiveUnique_DrawPerGrog / DrawPerGrogToken - MasterGunner V1/Helmsman V2
            if (HasPassive(RelicEffectType.PassiveUnique_DrawPerGrog) || 
                HasPassive(RelicEffectType.PassiveUnique_DrawPerGrog))
            {
                var energyManager = ServiceLocator.Get<EnergyManager>();
                int grog = energyManager?.GrogTokens ?? 0;
                if (grog > 0)
                {
                    cardDeck?.DrawCards(grog);
                    Debug.Log($"<color=cyan>{gameObject.name}: +{grog} cards from grog</color>");
                }
            }
            
            // Trinket_DrawIfHighHP / DrawIfHighHealth - Navigator V1/Boatswain V2
            if (HasPassive(RelicEffectType.Trinket_DrawIfHighHP) || 
                HasPassive(RelicEffectType.Trinket_DrawIfHighHP))
            {
                if (unitStatus.HPPercent > 0.6f)
                {
                    cardDeck?.DrawCards(1);
                    Debug.Log($"<color=cyan>{gameObject.name}: +1 card (high HP)</color>");
                }
            }
            
            // Trinket_DrawIfLowHP - Cook V2 (not in current enum, commented out)
            // if (HasPassive(RelicEffectType.Trinket_DrawIfLowHP))
            // {
            //     if (unitStatus.HPPercent < 0.5f)
            //     {
            //         cardDeck?.DrawCards(1);
            //         Debug.Log($"<color=cyan>{gameObject.name}: +1 card (low HP)</color>");
            //     }
            // }
        }

        private void OnPlayerTurnEnd()
        {
            // PassiveUnique_DrawOnLowDamage - MasterAtArms V1
            // This would need damage tracking per turn
        }

        private void OnRoundStart(int round)
        {
            weaponsUsedOnCurrentTarget = 0;
            currentTarget = null;
        }

        private void OnUnitDamaged(GameObject unit, int damage)
        {
            var targetStatus = unit?.GetComponent<UnitStatus>();
            if (targetStatus == null) return;

            // Check if this unit caused the damage (would need attacker info)
            // For now, check if an ally took damage

            // If THIS unit took damage
            if (unit == gameObject)
            {
                HandleDamageTaken(damage);
            }
            
            // If an ALLY took damage
            if (targetStatus.Team == unitStatus.Team && unit != gameObject)
            {
                HandleAllyDamaged(unit, damage);
            }

            // If ENEMY captain took damage
            if (targetStatus.Team != unitStatus.Team && targetStatus.IsCaptain)
            {
                HandleCaptainDamaged(unit, damage);
            }
        }

        private void HandleDamageTaken(int damage)
        {
            // Trinket_KnockbackAttacker / KnockbackAttackerOnce - Cook V1/Shipwright V2
            if ((HasPassive(RelicEffectType.Trinket_KnockbackAttacker) || 
                 HasPassive(RelicEffectType.Trinket_KnockbackAttacker)) && 
                !knockbackAttackerUsedThisTurn)
            {
                // Would need attacker reference - handled by StatusEffectManager
                knockbackAttackerUsedThisTurn = true;
            }

            // Check hull destruction for BonusDmgPerHullDestroyed tracking
            if (unitStatus.CurrentHullPool <= 0 && unitStatus.HasHull)
            {
                // Hull was destroyed this hit
                // Track for enemy passive if they have it
            }
        }

        private void HandleAllyDamaged(GameObject ally, int damage)
        {
            // PassiveUnique_CounterAttack / CounterAttackAlly - Navigator V1/Boatswain V2
            if (HasPassive(RelicEffectType.PassiveUnique_CounterAttack) || 
                HasPassive(RelicEffectType.PassiveUnique_CounterAttack))
            {
                // Attack the enemy that damaged ally
                if (unitAttack != null)
                {
                    Debug.Log($"<color=cyan>{gameObject.name}: Counter-attacking for ally!</color>");
                    unitAttack.TryMeleeAttack(); // Would need to target specific enemy
                }
            }
        }

        private void HandleCaptainDamaged(GameObject captain, int damage)
        {
            // Trinket_HealOnCaptainDamage handled by StatusEffectManager
        }

        private void OnUnitHealed(GameObject unit, int amount)
        {
            // Trinket_AttackOnEnemyHeal handled by StatusEffectManager
        }

        private void OnUnitDeath(GameObject unit)
        {
            var deadStatus = unit?.GetComponent<UnitStatus>();
            if (deadStatus == null) return;

            // If enemy died
            if (deadStatus.Team != unitStatus.Team)
            {
                HandleEnemyDeath(unit);
            }

            // If ally died
            if (deadStatus.Team == unitStatus.Team && unit != gameObject)
            {
                HandleAllyDeath(unit);
            }
        }

        private void HandleEnemyDeath(GameObject enemy)
        {
            // PassiveUnique_KillRestoreHealth - MasterAtArms V2 (not in current enum)
            // if (HasPassive(RelicEffectType.PassiveUnique_KillRestoreHealth))
            // {
            //     int heal = Mathf.RoundToInt(unitStatus.MaxHP * 0.2f);
            //     unitStatus.Heal(heal);
            //     Debug.Log($"<color=cyan>{gameObject.name}: Healed {heal} from kill</color>");
            // }

            // PassiveUnique_KillRestoreAllyHP - Surgeon V2 (not in current enum)
            // if (HasPassive(RelicEffectType.PassiveUnique_KillRestoreAllyHP))
            // {
            //     var allies = GetAllies();
            //     foreach (var ally in allies)
            //     {
            //         int heal = Mathf.RoundToInt(ally.MaxHP * 0.05f);
            //         ally.Heal(heal);
            //     }
            //     Debug.Log($"<color=cyan>{gameObject.name}: Healed all allies 5% from kill</color>");
            // }

            // Totem_EnemyDeathMoraleSwing - Boatswain V1
            if (HasPassive(RelicEffectType.Totem_EnemyDeathMoraleSwing))
            {
                // Enemies lose morale, allies gain
                var enemies = GetEnemies();
                var allies = GetAllies();
                
                foreach (var enemyUnit in enemies)
                {
                    enemyUnit.ApplyMoraleDamage(Mathf.RoundToInt(enemyUnit.MaxMorale * 0.05f));
                }
                foreach (var ally in allies)
                {
                    ally.RestoreMorale(Mathf.RoundToInt(ally.MaxMorale * 0.05f));
                }
                Debug.Log($"<color=cyan>Morale swing from enemy death!</color>");
            }
        }

        private void HandleAllyDeath(GameObject ally)
        {
            // Coat_KnockbackOnAllyDeath - Surgeon V2 (handled as active, not passive here)
        }

        private void OnUnitSurrender(GameObject unit)
        {
            // Same handling as death for kill-based passives
            var surrenderedStatus = unit?.GetComponent<UnitStatus>();
            if (surrenderedStatus != null && surrenderedStatus.Team != unitStatus.Team)
            {
                HandleEnemyDeath(unit);
            }
        }

        private void OnUnitAttack(GameObject attacker, GameObject target)
        {
            // Track weapons used on target for Ultimate_FourWeaponsSurrender (not in current enum)
            // if (attacker == gameObject && HasPassive(RelicEffectType.Ultimate_FourWeaponsSurrender))
            // {
            //     if (currentTarget != target)
            //     {
            //         currentTarget = target;
            //         weaponsUsedOnCurrentTarget = 0;
            //     }
            //     weaponsUsedOnCurrentTarget++;
            //     
            //     if (weaponsUsedOnCurrentTarget >= 4)
            //     {
            //         var targetStatus = target?.GetComponent<UnitStatus>();
            //         if (targetStatus != null && !targetStatus.IsCaptain)
            //         {
            //             // Force surrender
            //             Debug.Log($"<color=yellow>{target.name} forced to surrender after 4 weapon hits!</color>");
            //             GameEvents.TriggerUnitSurrender(target);
            //         }
            //     }
            // }
        }

        private void OnUnitMoved(GameObject unit, GridCell from, GridCell to)
        {
            // No movement-triggered passives currently
        }

        #endregion

        #region Damage Modifiers (Called by DamageCalculator)

        /// <summary>
        /// Get passive damage bonus percentage.
        /// </summary>
        public float GetPassiveDamageBonus(UnitStatus target)
        {
            float bonus = 0f;

            // Trinket_BonusDamagePerCard - Captain V1
            if (HasPassive(RelicEffectType.Trinket_BonusDamagePerCard))
            {
                int cardsInHand = cardDeck?.CardsInHand ?? 0;
                bonus += cardsInHand * 0.2f; // +20% per card
            }

            // Trinket_BonusVsCaptain / BonusVsCaptainTarget - Quartermaster V1/Captain V2
            if ((HasPassive(RelicEffectType.Trinket_BonusVsCaptain) || 
                 HasPassive(RelicEffectType.Trinket_BonusVsCaptain)) && 
                target != null && target.IsCaptain)
            {
                bonus += 0.2f; // +20% vs captain
            }

            // Trinket_DamageByBuzz - Shipwright V1
            if (HasPassive(RelicEffectType.Trinket_DamageByBuzz))
            {
                float buzzPercent = unitStatus.MaxBuzz > 0 ? 
                    (float)unitStatus.CurrentBuzz / unitStatus.MaxBuzz : 0f;
                bonus += buzzPercent * 0.5f; // Up to +50% at full buzz
            }

            // PassiveUnique_BonusVsLowGrit - Cook V1
            if (HasPassive(RelicEffectType.PassiveUnique_BonusVsLowGrit) && 
                target != null && target.Grit < unitStatus.Grit)
            {
                bonus += 0.2f; // +20% vs lower grit
            }

            // PassiveUnique_BonusVsLowHP - Deckhand V1
            if (HasPassive(RelicEffectType.PassiveUnique_BonusVsLowHP) && 
                target != null && target.HPPercent < 0.5f)
            {
                bonus += 0.2f; // +20% vs low HP
            }

            // PassiveUnique_BonusDmgPerHullDestroyed - not in current enum
            // if (HasPassive(RelicEffectType.PassiveUnique_BonusDmgPerHullDestroyed))
            // {
            //     bonus += hullsDestroyedThisGame * 0.3f; // +30% per hull destroyed
            // }

            // Trinket_RowEnemiesTakeMore - Deckhand V1
            if (HasPassive(RelicEffectType.Trinket_RowEnemiesTakeMore) && 
                target != null && IsSameRow(target))
            {
                bonus += 0.1f; // +10% vs same row enemies
            }

            return bonus;
        }

        /// <summary>
        /// Get outgoing damage modifier as multiplier (for DamageCalculator compatibility).
        /// Returns 1.0 for no change, >1.0 for bonus, <1.0 for penalty.
        /// </summary>
        public float GetOutgoingDamageModifier(UnitStatus target)
        {
            return 1f + GetPassiveDamageBonus(target);
        }

        /// <summary>
        /// Get passive damage reduction percentage.
        /// </summary>
        public float GetPassiveDamageReduction(UnitStatus attacker)
        {
            float reduction = 0f;

            // Trinket_ReduceDamageFromClosest - MasterAtArms V1
            if (HasPassive(RelicEffectType.Trinket_ReduceDamageFromClosest) && 
                attacker != null && IsClosestEnemy(attacker))
            {
                reduction += 0.2f; // -20% from closest enemy
            }

            // Trinket_RowEnemiesLessDamage - Swashbuckler V1
            if (HasPassive(RelicEffectType.Trinket_RowEnemiesLessDamage) && 
                attacker != null && IsSameRow(attacker))
            {
                reduction += 0.1f; // -10% from same row enemies
            }

            return reduction;
        }

        /// <summary>
        /// Get incoming damage modifier as multiplier (for DamageCalculator compatibility).
        /// Returns 1.0 for no change, <1.0 for reduction.
        /// </summary>
        public float GetIncomingDamageModifier(UnitStatus attacker)
        {
            return 1f - GetPassiveDamageReduction(attacker);
        }

        /// <summary>
        /// Check if unit is immune to morale focus fire.
        /// </summary>
        public bool IsImmuneTOMoraleFocus()
        {
            return HasPassive(RelicEffectType.Trinket_ImmuneMoraleFocusFire);
        }

        /// <summary>
        /// Alias for DamageCalculator compatibility.
        /// </summary>
        public bool IsImmuneMoraleFocusFire()
        {
            return IsImmuneTOMoraleFocus();
        }

        /// <summary>
        /// Get enemy surrender threshold modifier.
        /// </summary>
        public float GetEnemySurrenderThreshold()
        {
            // Trinket_EnemySurrenderEarly - Boatswain V1
            if (HasPassive(RelicEffectType.Trinket_EnemySurrenderEarly))
            {
                return 0.3f; // Enemies surrender at 30%
            }
            return 0.2f; // Default 20%
        }

        /// <summary>
        /// Get ally surrender threshold modifier.
        /// </summary>
        public float GetAllySurrenderThreshold()
        {
            // PassiveUnique_LowerSurrenderThreshold - Boatswain V1
            if (HasPassive(RelicEffectType.PassiveUnique_LowerSurrenderThreshold))
            {
                return 0.1f; // Allies surrender at 10%
            }
            return 0.2f; // Default 20%
        }

        /// <summary>
        /// Get surrender threshold (for DamageCalculator compatibility).
        /// Returns ally threshold for own team.
        /// </summary>
        public float GetSurrenderThreshold()
        {
            return GetAllySurrenderThreshold();
        }

        /// <summary>
        /// Check if buzz penalty is disabled.
        /// </summary>
        public bool IsBuzzPenaltyDisabled()
        {
            return HasPassive(RelicEffectType.PassiveUnique_NoBuzzDownside);
        }

        /// <summary>
        /// Alias for DamageCalculator compatibility.
        /// </summary>
        public bool HasNoBuzzDownside()
        {
            return IsBuzzPenaltyDisabled();
        }

        /// <summary>
        /// Check if relics are not consumed.
        /// </summary>
        public bool AreRelicsNotConsumed()
        {
            // PassiveUnique_RelicsNotConsumed not in current enum
            // return HasPassive(RelicEffectType.PassiveUnique_RelicsNotConsumed);
            return false;
        }

        /// <summary>
        /// Get enemy movement limit.
        /// </summary>
        public int GetEnemyMovementLimit()
        {
            // PassiveUnique_EnemyBootsLimited not in current enum
            // if (HasPassive(RelicEffectType.PassiveUnique_EnemyBootsLimited))
            // {
            //     return 1; // Enemies limited to 1 tile
            // }
            return -1; // No limit
        }

        /// <summary>
        /// Get ally extra movement.
        /// </summary>
        public int GetAllyExtraMovement()
        {
            // PassiveUnique_AllAlliesExtraMove not in current enum
            // if (HasPassive(RelicEffectType.PassiveUnique_AllAlliesExtraMove))
            // {
            //     return 1; // +1 movement for all allies
            // }
            return 0;
        }

        /// <summary>
        /// Check if knockback increases enemy buzz.
        /// </summary>
        public bool KnockbackIncreasesBuzz()
        {
            return HasPassive(RelicEffectType.Trinket_KnockbackIncreasesBuzz);
        }

        /// <summary>
        /// Check if nearby allies ignore obstacles.
        /// </summary>
        public bool NearbyAlliesIgnoreObstacles()
        {
            // Trinket_NearbyIgnoreObstacles not in current enum
            // return HasPassive(RelicEffectType.Trinket_NearbyIgnoreObstacles);
            return false;
        }

        /// <summary>
        /// Check if nearby radius is global.
        /// </summary>
        public bool IsNearbyRadiusGlobal()
        {
            // Trinket_GlobalAllyRadius not in current enum
            // return HasPassive(RelicEffectType.Trinket_GlobalAllyRadius);
            return false;
        }

        /// <summary>
        /// Get power bonus for nearby allies.
        /// </summary>
        public float GetNearbyAllyPowerBonus()
        {
            // Trinket_NearbyAlliesPowerBuff not in current enum
            // if (HasPassive(RelicEffectType.Trinket_NearbyAlliesPowerBuff))
            // {
            //     return 0.3f; // +30% power to nearby
            // }
            return 0f;
        }

        /// <summary>
        /// Get speed reduction for all enemies.
        /// </summary>
        public float GetEnemySpeedReduction()
        {
            // Trinket_EnemiesLoseSpeed not in current enum
            // if (HasPassive(RelicEffectType.Trinket_EnemiesLoseSpeed))
            // {
            //     return 0.1f; // -10% speed to all enemies
            // }
            return 0f;
        }

        /// <summary>
        /// Track hull destroyed for bonus damage.
        /// </summary>
        public void TrackHullDestroyed()
        {
            hullsDestroyedThisGame++;
            Debug.Log($"Hulls destroyed this game: {hullsDestroyedThisGame}");
        }

        /// <summary>
        /// Check if should discard enemy card on hull survive.
        /// </summary>
        public bool ShouldDiscardOnHullSurvive()
        {
            // Trinket_HullRegenOnSurvive not in current enum
            // return HasPassive(RelicEffectType.Trinket_HullRegenOnSurvive);
            return false;
        }

        #endregion

        #region Aura Effects

        /// <summary>
        /// Get grit aura bonus for nearby allies.
        /// </summary>
        public float GetGritAuraBonus()
        {
            if (HasPassive(RelicEffectType.PassiveUnique_GritAura))
            {
                return unitStatus.Grit * 0.05f; // 5% of this unit's grit
            }
            return 0f;
        }

        /// <summary>
        /// Apply aura effects to nearby allies.
        /// </summary>
        public void ApplyAuraEffects()
        {
            var allies = GetNearbyAllies(IsNearbyRadiusGlobal() ? 99 : 1);
            
            float gritBonus = GetGritAuraBonus();
            float powerBonus = GetNearbyAllyPowerBonus();
            
            if (gritBonus > 0 || powerBonus > 0)
            {
                foreach (var ally in allies)
                {
                    var allyEffects = ally.GetComponent<StatusEffectManager>();
                    if (allyEffects != null)
                    {
                        if (gritBonus > 0)
                        {
                            allyEffects.ApplyEffect(StatusEffect.CreateGritBoost(1, gritBonus, gameObject));
                        }
                        if (powerBonus > 0)
                        {
                            allyEffects.ApplyEffect(StatusEffect.CreatePowerBoost(1, powerBonus, gameObject));
                        }
                    }
                }
            }
        }

        #endregion

        #region Helper Methods

        private List<UnitStatus> GetAllies()
        {
            return GameObject.FindGameObjectsWithTag("Unit")
                .Select(go => go.GetComponent<UnitStatus>())
                .Where(u => u != null && u.Team == unitStatus.Team && u != unitStatus && !u.HasSurrendered)
                .ToList();
        }

        private List<UnitStatus> GetEnemies()
        {
            return GameObject.FindGameObjectsWithTag("Unit")
                .Select(go => go.GetComponent<UnitStatus>())
                .Where(u => u != null && u.Team != unitStatus.Team && !u.HasSurrendered)
                .ToList();
        }

        private List<UnitStatus> GetNearbyAllies(int range)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return new List<UnitStatus>();

            Vector2Int myPos = gridManager.WorldToGridPosition(transform.position);
            
            return GetAllies().Where(ally =>
            {
                Vector2Int allyPos = gridManager.WorldToGridPosition(ally.transform.position);
                int dist = Mathf.Abs(myPos.x - allyPos.x) + Mathf.Abs(myPos.y - allyPos.y);
                return dist <= range;
            }).ToList();
        }

        private bool IsSameRow(UnitStatus other)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return false;

            Vector2Int myPos = gridManager.WorldToGridPosition(transform.position);
            Vector2Int otherPos = gridManager.WorldToGridPosition(other.transform.position);
            
            return myPos.y == otherPos.y;
        }

        private bool IsClosestEnemy(UnitStatus attacker)
        {
            var enemies = GetEnemies();
            if (enemies.Count == 0) return false;

            var closest = enemies.OrderBy(e => 
                Vector3.Distance(transform.position, e.transform.position)).First();
            
            return closest == attacker;
        }

        /// <summary>
        /// Check if Shipwright and Boatswain roles should be ignored.
        /// </summary>
        public bool ShouldIgnoreRoles()
        {
            return HasPassive(RelicEffectType.PassiveUnique_IgnoreRoles);
        }

        #endregion

        #region Debug

        public string GetPassivesDebugString()
        {
            if (activePassives.Count == 0) return "No passives";
            return string.Join(", ", activePassives.Select(p => p.ToString()));
        }

        #endregion
    }
}