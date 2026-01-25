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
        // private int weaponsUsedOnCurrentTarget = 0;
        // private GameObject currentTarget;
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
            
            // PassiveUnique_ExtraCards - Quartermaster V1
            if (HasPassive(RelicEffectType.PassiveUnique_ExtraCards))
            {
                cardDeck?.DrawCards(2);
                Debug.Log($"<color=cyan>{gameObject.name}: +2 cards from passive</color>");
            }
            
            // PassiveUnique_DrawPerGrog - MasterGunner V1
            if (HasPassive(RelicEffectType.PassiveUnique_DrawPerGrog))
            {
                var energyManager = ServiceLocator.Get<EnergyManager>();
                int grog = energyManager?.GrogTokens ?? 0;
                if (grog > 0)
                {
                    cardDeck?.DrawCards(grog);
                    Debug.Log($"<color=cyan>{gameObject.name}: +{grog} cards from grog</color>");
                }
            }
            
            // Trinket_DrawIfHighHP - Navigator V1
            if (HasPassive(RelicEffectType.Trinket_DrawIfHighHP))
            {
                if (unitStatus.HPPercent > 0.6f)
                {
                    cardDeck?.DrawCards(1);
                    Debug.Log($"<color=cyan>{gameObject.name}: +1 card (high HP)</color>");
                }
            }
            
            // ==================== V2 TURN START PASSIVES ====================
            
            // Trinket_V2_GrogOnTurnStart - Master Gunner V2 (25% chance for free grog)
            if (HasPassive(RelicEffectType.Trinket_V2_GrogOnTurnStart))
            {
                if (UnityEngine.Random.value < 0.25f)
                {
                    var energyManager = ServiceLocator.Get<EnergyManager>();
                    energyManager?.AddGrog(1);
                    Debug.Log($"<color=cyan>{gameObject.name}: +1 grog from passive (25% chance)</color>");
                }
            }
            
            // PassiveUnique_V2_CardMaster - Quartermaster V2 (cards cost 1 less is handled in GetCardCostReduction)
        }

        private void OnPlayerTurnEnd()
        {
            // PassiveUnique_DrawOnLowDamage - MasterAtArms V1
            // This would need damage tracking per turn
            
            // ==================== V2 TURN END PASSIVES ====================
            
            // Trinket_V2_HealOnTurnEnd - Surgeon V2 (heal 3% at turn end)
            if (HasPassive(RelicEffectType.Trinket_V2_HealOnTurnEnd))
            {
                int heal = Mathf.RoundToInt(unitStatus.MaxHP * 0.03f);
                unitStatus.Heal(heal);
                Debug.Log($"<color=cyan>{gameObject.name}: Healed {heal} at turn end</color>");
            }
        }

        private void OnRoundStart(int round)
        {
            // weaponsUsedOnCurrentTarget = 0;
            // currentTarget = null;
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
            // Trinket_KnockbackAttacker - Cook V1
            if (HasPassive(RelicEffectType.Trinket_KnockbackAttacker) && !knockbackAttackerUsedThisTurn)
            {
                // Would need attacker reference - handled by StatusEffectManager
                knockbackAttackerUsedThisTurn = true;
            }

            // ==================== V2 DAMAGE TAKEN PASSIVES ====================
            
            // PassiveUnique_V2_LastStand - Boatswain V2 (50% survive at 1 HP)
            if (HasPassive(RelicEffectType.PassiveUnique_V2_LastStand))
            {
                if (unitStatus.CurrentHP <= 0 && UnityEngine.Random.value < 0.5f)
                {
                    unitStatus.Heal(1); // Survive at 1 HP
                    Debug.Log($"<color=yellow>{gameObject.name}: Last Stand triggered! Survived at 1 HP</color>");
                }
            }
        }

        private void HandleAllyDamaged(GameObject ally, int damage)
        {
            // PassiveUnique_CounterAttack - Navigator V1
            if (HasPassive(RelicEffectType.PassiveUnique_CounterAttack))
            {
                // Attack the enemy that damaged ally
                if (unitAttack != null)
                {
                    Debug.Log($"<color=cyan>{gameObject.name}: Counter-attacking for ally!</color>");
                    unitAttack.TryMeleeAttack(); // Would need to target specific enemy
                }
            }
            
            // PassiveUnique_V2_Riposte - Swashbuckler V2 (30% counter on self damage handled separately)
        }

        private void HandleCaptainDamaged(GameObject captain, int damage)
        {
            // Trinket_V2_DrawOnCaptainHit - Quartermaster V2
            if (HasPassive(RelicEffectType.Trinket_V2_DrawOnCaptainHit))
            {
                cardDeck?.DrawCards(1);
                Debug.Log($"<color=cyan>{gameObject.name}: Drew card because captain was hit</color>");
            }
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
            // ==================== V2 KILL PASSIVES ====================
            
            // Trinket_V2_MoraleOnKill - Helmsman V2 (gain 10% morale on kill)
            if (HasPassive(RelicEffectType.Trinket_V2_MoraleOnKill))
            {
                int moraleGain = Mathf.RoundToInt(unitStatus.MaxMorale * 0.10f);
                unitStatus.RestoreMorale(moraleGain);
                Debug.Log($"<color=cyan>{gameObject.name}: +{moraleGain} morale from kill</color>");
            }

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

            // ==================== V2 DAMAGE BONUSES ====================
            
            // Trinket_V2_BonusDamagePerAlly - Captain V2 (+5% per ally)
            if (HasPassive(RelicEffectType.Trinket_V2_BonusDamagePerAlly))
            {
                int allyCount = GetAllies().Count;
                bonus += allyCount * 0.05f;
            }
            
            // Trinket_V2_CritChance - Swashbuckler V2 (15% chance for +50%)
            if (HasPassive(RelicEffectType.Trinket_V2_CritChance))
            {
                if (UnityEngine.Random.value < 0.15f)
                {
                    bonus += 0.5f;
                    Debug.Log($"<color=yellow>{gameObject.name}: Critical hit! +50% damage</color>");
                }
            }
            
            // Trinket_V2_BonusVsFullHP - Deckhand V2 (+25% vs full HP)
            if (HasPassive(RelicEffectType.Trinket_V2_BonusVsFullHP) && 
                target != null && target.HPPercent >= 0.99f)
            {
                bonus += 0.25f;
            }
            
            // PassiveUnique_V2_Sniper - Deckhand V2 (+20% at max range)
            if (HasPassive(RelicEffectType.PassiveUnique_V2_Sniper) && target != null)
            {
                float dist = Vector3.Distance(transform.position, target.transform.position);
                if (dist >= 5f) // Max range assumed 5 tiles
                {
                    bonus += 0.2f;
                }
            }
            
            // PassiveUnique_V2_TeamLeader - Captain V2 (handled via aura)

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

            // ==================== V2 DAMAGE REDUCTION ====================
            
            // Trinket_V2_ArmorOnLowHP - MasterAtArms V2 (+50% armor below 30%)
            if (HasPassive(RelicEffectType.Trinket_V2_ArmorOnLowHP) && 
                unitStatus.HPPercent < 0.3f)
            {
                reduction += 0.5f;
            }
            
            // PassiveUnique_V2_Unstoppable - MasterAtArms V2 (handled in StatusEffectManager)

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
        
        // ==================== V2 PASSIVE METHODS ====================
        
        /// <summary>
        /// Get card cost reduction from PassiveUnique_V2_CardMaster.
        /// </summary>
        public int GetCardCostReduction()
        {
            if (HasPassive(RelicEffectType.PassiveUnique_V2_CardMaster))
            {
                return 1; // All cards cost 1 less
            }
            return 0;
        }
        
        /// <summary>
        /// Get heal effectiveness multiplier from PassiveUnique_V2_Medic.
        /// </summary>
        public float GetHealEffectivenessMultiplier()
        {
            if (HasPassive(RelicEffectType.PassiveUnique_V2_Medic))
            {
                return 1.25f; // Heals are 25% more effective
            }
            return 1f;
        }
        
        /// <summary>
        /// Check if grog consumption should be halved (PassiveUnique_V2_Efficient).
        /// </summary>
        public bool ShouldHalveGrogConsumption()
        {
            if (HasPassive(RelicEffectType.PassiveUnique_V2_Efficient))
            {
                return UnityEngine.Random.value < 0.5f; // 50% chance not consumed
            }
            return false;
        }
        
        /// <summary>
        /// Check if buzz penalties become bonuses (PassiveUnique_V2_DrunkMaster).
        /// </summary>
        public bool BuzzGivesBonuses()
        {
            return HasPassive(RelicEffectType.PassiveUnique_V2_DrunkMaster);
        }
        
        /// <summary>
        /// Check if unit can't be stunned/slowed (PassiveUnique_V2_Unstoppable).
        /// </summary>
        public bool IsUnstoppable()
        {
            return HasPassive(RelicEffectType.PassiveUnique_V2_Unstoppable);
        }
        
        /// <summary>
        /// Get movement bonus from Trinket_V2_SpeedOnHighHP.
        /// </summary>
        public int GetSpeedBonus()
        {
            if (HasPassive(RelicEffectType.Trinket_V2_SpeedOnHighHP) && unitStatus.HPPercent > 0.7f)
            {
                return 1; // +1 movement above 70% HP
            }
            return 0;
        }
        
        /// <summary>
        /// Get food buff duration multiplier from Trinket_V2_FoodDoubleDuration.
        /// </summary>
        public int GetFoodDurationMultiplier()
        {
            if (HasPassive(RelicEffectType.Trinket_V2_FoodDoubleDuration))
            {
                return 2; // Food buffs last twice as long
            }
            return 1;
        }
        
        /// <summary>
        /// Check if ally should surrender at lower threshold (Trinket_V2_AllySurrenderLater).
        /// </summary>
        public bool AlliesSurrenderLater()
        {
            return HasPassive(RelicEffectType.Trinket_V2_AllySurrenderLater);
        }
        
        /// <summary>
        /// Check if buzz should not cause penalty (Trinket_V2_NoBuzzPenalty).
        /// </summary>
        public bool NoBuzzPenalty()
        {
            return HasPassive(RelicEffectType.Trinket_V2_NoBuzzPenalty) || 
                   HasPassive(RelicEffectType.PassiveUnique_NoBuzzDownside);
        }
        
        /// <summary>
        /// Get additional heal from food (PassiveUnique_V2_Nourishing).
        /// </summary>
        public float GetFoodHealBonus()
        {
            if (HasPassive(RelicEffectType.PassiveUnique_V2_Nourishing))
            {
                return 0.1f; // Food heals 10% HP additionally
            }
            return 0f;
        }
        
        /// <summary>
        /// Check for counter-attack chance (PassiveUnique_V2_Riposte).
        /// </summary>
        public bool ShouldRiposte()
        {
            if (HasPassive(RelicEffectType.PassiveUnique_V2_Riposte))
            {
                return UnityEngine.Random.value < 0.3f; // 30% chance
            }
            return false;
        }
        
        /// <summary>
        /// Get ally morale bonus on attack (PassiveUnique_V2_Inspiring).
        /// </summary>
        public void TriggerInspiringOnAttack()
        {
            if (HasPassive(RelicEffectType.PassiveUnique_V2_Inspiring))
            {
                var allies = GetAllies();
                foreach (var ally in allies)
                {
                    if (ally != unitStatus)
                    {
                        ally.RestoreMorale(Mathf.RoundToInt(ally.MaxMorale * 0.05f));
                    }
                }
                Debug.Log($"<color=cyan>{gameObject.name}: Inspired allies +5% morale</color>");
            }
        }
        
        /// <summary>
        /// Apply team leader aura (PassiveUnique_V2_TeamLeader).
        /// </summary>
        public void ApplyTeamLeaderAura()
        {
            if (HasPassive(RelicEffectType.PassiveUnique_V2_TeamLeader))
            {
                var allies = GetNearbyAllies(2); // 2 tile radius
                foreach (var ally in allies)
                {
                    if (ally != unitStatus)
                    {
                        var effects = ally.GetComponent<StatusEffectManager>();
                        effects?.ApplyEffect(StatusEffect.CreateDamageBoost(1, 0.1f, gameObject));
                        effects?.ApplyEffect(StatusEffect.CreateDamageReduction(1, 0.1f, gameObject));
                    }
                }
            }
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