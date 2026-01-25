using UnityEngine;
using TacticalGame.Enums;
using TacticalGame.Config;
using TacticalGame.Core;
using TacticalGame.Combat;
using TacticalGame.Grid;

namespace TacticalGame.Units
{
    /// <summary>
    /// Core unit component handling stats, state, and status effects.
    /// </summary>
    [RequireComponent(typeof(TacticalGame.Equipment.UnitEquipmentUpdated))]
    [RequireComponent(typeof(TacticalGame.Equipment.PassiveRelicManager))]
    [RequireComponent(typeof(StatusEffectManager))]
    public class UnitStatus : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Identity")]
        [SerializeField] private string unitName;
        [SerializeField] private UnitRole role;
        [SerializeField] private Team team;
        [SerializeField] private WeaponType weaponType;

        [Header("Primary/Secondary Stats")]
        [SerializeField] private StatType primaryStat;
        [SerializeField] private StatType secondaryPrimaryStat; // Captain only
        [SerializeField] private StatType secondaryStat;
        [SerializeField] private bool hasTwoPrimaryStats;

        [Header("Health & Morale")]
        [SerializeField] private int maxHP = 600;
        [SerializeField] private int currentHP;
        [SerializeField] private int maxMorale = 900;
        [SerializeField] private int currentMorale;

        [Header("Core Stats")]
        [SerializeField] private int power;
        [SerializeField] private int aim;
        [SerializeField] private int tactics;
        [SerializeField] private int skill;
        [SerializeField] private int proficiency; // Stored as percentage (150 = 1.5x)
        [SerializeField] private int grit;
        [SerializeField] private int hull;
        [SerializeField] private int speed;

        [Header("Buzz System")]
        [SerializeField] private int maxBuzz = 100; // Buzz capacity from stats
        [SerializeField] private int currentBuzz = 0;

        [Header("Ammo")]
        [SerializeField] private int currentArrows;

        [Header("Hull Pool")]
        [SerializeField] private int maxHullPool;
        [SerializeField] private int currentHullPool;

        [Header("Visuals")]
        [SerializeField] private GameObject whiteFlagVisual;

        #endregion

        #region Private State

        private int curseCharges = 0;
        private float curseMultiplier = 1.0f;
        private int stunDuration = 0;
        private int swapCooldown = 0;
        private bool isExposed = false;
        private bool isStunned = false;
        private bool isTrapped = false;
        private bool hasSurrendered = false;

        // Focus fire tracking
        private GameObject lastAttacker;
        private int focusFireStacks = 0;

        // Cached references
        private GridManager gridManager;
        private MeshRenderer meshRenderer;
        private Color originalColor;

        #endregion

        #region Public Properties

        // Identity
        public string UnitName => unitName;
        public UnitRole Role => role;
        public Team Team => team;
        public WeaponType WeaponType => weaponType;
        public bool IsCaptain => role == UnitRole.Captain;

        // Primary/Secondary tracking
        public StatType PrimaryStat => primaryStat;
        public StatType SecondaryPrimaryStat => secondaryPrimaryStat;
        public StatType SecondaryStat => secondaryStat;
        public bool HasTwoPrimaryStats => hasTwoPrimaryStats;

        // Health & Morale
        public int MaxHP => maxHP;
        public int CurrentHP => currentHP;
        public int MaxMorale => maxMorale;
        public int CurrentMorale => currentMorale;
        public float HPPercent => maxHP > 0 ? (float)currentHP / maxHP : 0f;
        public float MoralePercent => maxMorale > 0 ? (float)currentMorale / maxMorale : 0f;

        // Stats
        public int Power => power;
        public int Aim => aim;
        public int Tactics => tactics;
        public int Skill => skill;
        public int Proficiency => proficiency;
        public float ProficiencyMultiplier => proficiency / 100f;
        public int Grit => grit;
        public int Hull => hull;
        public int Speed => speed;

        // Buzz System
        public int CurrentBuzz => currentBuzz;
        public int MaxBuzz => maxBuzz;
        public bool IsTooDrunk => currentBuzz >= maxBuzz;

        // Ammo
        public int MaxArrows => GameConfig.Instance.defaultMaxArrows;
        public int CurrentArrows => currentArrows;

        // Hull Pool (armor)
        public int MaxHullPool => maxHullPool;
        public int CurrentHullPool => currentHullPool;
        public float HullPercent => maxHullPool > 0 ? (float)currentHullPool / maxHullPool : 0f;
        public bool HasHull => currentHullPool > 0;

        // Status Flags
        public bool IsStunned => isStunned;
        public bool IsTrapped => isTrapped;
        public bool HasSurrendered => hasSurrendered;
        public bool IsExposed => isExposed;
        public bool IsCursed => curseCharges > 0;
        public float CurseMultiplier => curseMultiplier;
        public int SwapCooldown => swapCooldown;

        // Focus Fire
        public int FocusFireStacks => focusFireStacks;
        public GameObject LastAttacker => lastAttacker;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                originalColor = meshRenderer.material.color;
            }
        }

        private void Start()
        {
            gridManager = ServiceLocator.Get<GridManager>();
            
            if (currentHP == 0) currentHP = maxHP;
            if (currentMorale == 0) currentMorale = maxMorale;
            currentArrows = MaxArrows;
            
            if (whiteFlagVisual != null)
            {
                whiteFlagVisual.SetActive(false);
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize unit from UnitData.
        /// </summary>
        public void Initialize(UnitData data)
        {
            unitName = data.unitName;
            role = data.role;
            team = data.team;
            weaponType = data.weaponType;

            // Primary/Secondary stat tracking
            primaryStat = data.primaryStat;
            secondaryPrimaryStat = data.secondaryPrimaryStat;
            secondaryStat = data.secondaryStat;
            hasTwoPrimaryStats = data.hasTwoPrimaryStats;

            // Set stats from data
            maxHP = data.health;
            currentHP = maxHP;
            maxMorale = data.morale;
            currentMorale = maxMorale;
            maxBuzz = data.buzz; // Buzz is now capacity
            currentBuzz = 0;
            
            power = data.power;
            aim = data.aim;
            tactics = data.tactics;
            skill = data.skill;
            proficiency = data.proficiency;
            grit = data.grit;
            hull = data.hull;
            speed = data.speed;

            // Calculate Hull pool: MaxHull = BaseHull + Hull×10
            var config = GameConfig.Instance;
            maxHullPool = config.baseHull + (hull * config.hullPerPoint);
            currentHullPool = maxHullPool;

            currentArrows = MaxArrows;
        }

        #endregion

        #region Damage & Healing

        /// <summary>
        /// Take damage from an attack.
        /// </summary>
        public void TakeDamage(int rawDamage, GameObject source, bool isMelee, 
                               int flatBonusHP = 0, int flatBonusMorale = 0, bool applyCurse = false,
                               bool isFirstAction = false, int comboCount = 1)
        {
            if (hasSurrendered) return;

            // Update focus fire tracking
            UpdateFocusFireStacks(source);
            
            // Apply curse if triggered
            if (applyCurse)
            {
                ApplyCurse(GameConfig.Instance.defaultCurseMultiplier);
            }

            // Check for adjacency cover
            bool hasCover = CheckAdjacencyCover();

            // Get attacker status for first-action bonus and combo calculation
            UnitStatus attackerStatus = source != null ? source.GetComponent<UnitStatus>() : null;

            // Calculate damage using DamageCalculator (includes combo scaling)
            var result = DamageCalculator.Calculate(
                rawDamage, 
                isMelee, 
                attackerStatus,
                this, 
                hasCover,
                isFirstAction,
                comboCount,
                flatBonusHP, 
                flatBonusMorale
            );

            // Apply Grit damage reduction
            float gritDR = CalculateGritDamageReduction();
            int damageAfterGrit = Mathf.RoundToInt(result.FinalHPDamage * (1f - gritDR));

            // Apply Hull absorption
            int hullAbsorbed = 0;
            int finalHPDamage = damageAfterGrit;
            
            if (currentHullPool > 0)
            {
                var config = GameConfig.Instance;
                // Hull absorbs up to X% of incoming damage
                int maxAbsorb = Mathf.RoundToInt(damageAfterGrit * config.hullAbsorbPercent);
                hullAbsorbed = Mathf.Min(currentHullPool, maxAbsorb);
                currentHullPool -= hullAbsorbed;
                finalHPDamage = damageAfterGrit - hullAbsorbed;
            }

            // Apply HP damage
            currentHP -= finalHPDamage;

            // Apply morale damage
            ApplyMoraleDamage(result.FinalMoraleDamage);

            // Log damage report
            string attackerName = source != null ? source.name : "Unknown Source";
            string hullInfo = hullAbsorbed > 0 ? $" (Hull absorbed: {hullAbsorbed}, Hull remaining: {currentHullPool})" : "";
            string comboInfo = comboCount > 1 ? $" [Combo x{comboCount}]" : "";
            Debug.Log($"<color=red><b>DAMAGE REPORT: {gameObject.name}</b></color>\n" +
                      $"<b>Attacker:</b> {attackerName}{comboInfo}\n" +
                      $"<b>HP Lost: {finalHPDamage}</b> (Grit DR: {gritDR:P0}){hullInfo} [{result.HPBreakdown}]\n" +
                      $"<b>Morale Lost: {result.FinalMoraleDamage}</b>  [{result.MoraleBreakdown}]");

            // Reduce curse charges
            if (curseCharges > 0) curseCharges--;

            // Fire event
            GameEvents.TriggerUnitDamaged(gameObject, finalHPDamage);

            // Check death
            if (currentHP <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Calculate Grit-based damage reduction.
        /// Formula: GritFactor = ((1 - HP%) × 0.50 + Morale% × 0.40)
        /// DR = min(DRCap, GritFactor × (Grit / 100))
        /// </summary>
        private float CalculateGritDamageReduction()
        {
            // Use the centralized calculation from DamageCalculator
            return DamageCalculator.GetGritDamageReduction(this);
        }

        /// <summary>
        /// Apply morale damage (used by hazards and other sources).
        /// </summary>
        public void ApplyMoraleDamage(int amount)
        {
            currentMorale -= amount;
            if (currentMorale < 0) currentMorale = 0;

            GameEvents.TriggerMoraleDamaged(gameObject, amount);

            if (currentMorale < GameConfig.Instance.surrenderThreshold && !hasSurrendered)
            {
                Surrender();
            }
        }

        /// <summary>
        /// Heal HP.
        /// </summary>
        public void Heal(int amount)
        {
            int oldHP = currentHP;
            currentHP = Mathf.Min(maxHP, currentHP + amount);
            int actualHeal = currentHP - oldHP;
            
            if (actualHeal > 0)
            {
                GameEvents.TriggerUnitHealed(gameObject, actualHeal);
            }
        }

        /// <summary>
        /// Restore morale.
        /// </summary>
        public void RestoreMorale(int amount)
        {
            currentMorale = Mathf.Min(maxMorale, currentMorale + amount);
        }

        #endregion

        #region Rum System

        /// <summary>
        /// Drink rum to restore HP or Morale.
        /// </summary>
        public void DrinkRum(string type)
        {
            var config = GameConfig.Instance;
            
            currentBuzz += config.buzzPerDrink;
            if (currentBuzz > maxBuzz) currentBuzz = maxBuzz;

            if (type == "Health")
            {
                Heal(config.healthRumRestore);
            }
            else if (type == "Morale")
            {
                RestoreMorale(config.moraleRumRestore);
            }
        }

        /// <summary>
        /// Reduce buzz level.
        /// </summary>
        public void ReduceBuzz(int amount)
        {
            currentBuzz = Mathf.Max(0, currentBuzz - amount);
        }

        #endregion

        #region Arrows

        /// <summary>
        /// Use an arrow. Returns true if arrow was available.
        /// </summary>
        public bool UseArrow()
        {
            if (currentArrows <= 0) return false;
            currentArrows--;
            return true;
        }

        /// <summary>
        /// Add arrows.
        /// </summary>
        public void AddArrows(int amount)
        {
            currentArrows = Mathf.Min(MaxArrows, currentArrows + amount);
        }

        #endregion

        #region Hull System

        /// <summary>
        /// Restore hull points.
        /// </summary>
        public void RestoreHull(int amount)
        {
            currentHullPool = Mathf.Min(maxHullPool, currentHullPool + amount);
        }

        /// <summary>
        /// Damage hull directly (bypassing HP).
        /// Some abilities can "break hull" directly.
        /// </summary>
        public void DamageHullDirect(int amount)
        {
            currentHullPool = Mathf.Max(0, currentHullPool - amount);
        }

        /// <summary>
        /// Apply "Crack Hull" debuff - temporarily reduces hull effectiveness.
        /// Returns the amount of hull removed (to restore later if needed).
        /// </summary>
        public int CrackHull(float percentRemoved = 0.5f)
        {
            int amountToRemove = Mathf.RoundToInt(currentHullPool * percentRemoved);
            currentHullPool -= amountToRemove;
            return amountToRemove;
        }

        #endregion

        #region Status Effects

        /// <summary>
        /// Apply stun effect.
        /// </summary>
        public void ApplyStun(int duration)
        {
            isStunned = true;
            stunDuration = duration;
            GameEvents.TriggerUnitStunned(gameObject);
        }

        /// <summary>
        /// Apply trap effect.
        /// </summary>
        public void ApplyTrap()
        {
            isTrapped = true;
            GameEvents.TriggerUnitTrapped(gameObject);
        }

        /// <summary>
        /// Apply curse effect.
        /// </summary>
        public void ApplyCurse(float multiplier)
        {
            curseCharges = GameConfig.Instance.defaultCurseCharges;
            curseMultiplier = multiplier;
            GameEvents.TriggerUnitCursed(gameObject);
        }

        /// <summary>
        /// Clear curse effect.
        /// </summary>
        public void ClearCurse()
        {
            curseCharges = 0;
        }

        /// <summary>
        /// Apply swap penalty (morale loss + exposed state).
        /// </summary>
        public void ApplySwapPenalty()
        {
            if (!IsCaptain)
            {
                int penalty = Mathf.RoundToInt(currentMorale * GameConfig.Instance.swapMoralePenalty);
                currentMorale -= penalty;
            }
            
            isExposed = true;
            swapCooldown = GameConfig.Instance.swapCooldownTurns;
            GameEvents.TriggerUnitExposed(gameObject);
        }

        /// <summary>
        /// Set swap cooldown directly.
        /// </summary>
        public void SetSwapCooldown(int turns)
        {
            swapCooldown = turns;
        }

        #endregion

        #region Turn Lifecycle

        /// <summary>
        /// Called at the start of this unit's turn.
        /// </summary>
        public void OnTurnStart()
        {
            // Clear trap
            if (isTrapped) isTrapped = false;
            
            // Decay buzz
            ReduceBuzz(GameConfig.Instance.buzzDecayPerTurn);
            
            // Reset focus fire
            focusFireStacks = 0;
            lastAttacker = null;
            
            // Clear exposed
            if (isExposed) isExposed = false;
            
            // Reduce swap cooldown
            if (swapCooldown > 0) swapCooldown--;
        }

        /// <summary>
        /// Called at the end of this unit's turn.
        /// </summary>
        public void OnTurnEnd()
        {
            // Process stun duration
            if (isStunned)
            {
                stunDuration--;
                if (stunDuration <= 0)
                {
                    isStunned = false;
                }
            }
        }

        #endregion

        #region State Checks

        /// <summary>
        /// Check if unit can act (not surrendered, not stunned).
        /// </summary>
        public bool CanAct()
        {
            return !hasSurrendered && !isStunned;
        }

        /// <summary>
        /// Check if unit can be swapped.
        /// </summary>
        public bool CanSwap()
        {
            if (hasSurrendered) return false;
            if (swapCooldown > 0) return false;
            if (HPPercent < GameConfig.Instance.minHPPercentToSwap) return false;
            return true;
        }

        #endregion

        #region Private Helpers

        private void UpdateFocusFireStacks(GameObject source)
        {
            if (lastAttacker != source)
            {
                focusFireStacks = 1;
                lastAttacker = source;
            }
            else
            {
                focusFireStacks++;
                int maxStacks = GameConfig.Instance.focusFireMultipliers.Length - 1;
                if (focusFireStacks > maxStacks)
                {
                    focusFireStacks = maxStacks;
                }
            }
        }

        private bool CheckAdjacencyCover()
        {
            if (gridManager == null)
            {
                gridManager = ServiceLocator.Get<GridManager>();
            }
            
            if (gridManager == null) return false;

            Vector2Int myPos = gridManager.WorldToGridPosition(transform.position);
            Vector2Int[] neighbors = 
            {
                new Vector2Int(myPos.x + 1, myPos.y),
                new Vector2Int(myPos.x - 1, myPos.y),
                new Vector2Int(myPos.x, myPos.y + 1),
                new Vector2Int(myPos.x, myPos.y - 1)
            };

            foreach (Vector2Int n in neighbors)
            {
                GridCell cell = gridManager.GetCell(n.x, n.y);
                if (cell != null && cell.HasHazard)
                {
                    return true;
                }
            }
            
            return false;
        }

        private void Surrender()
        {
            hasSurrendered = true;
            
            if (meshRenderer != null)
            {
                meshRenderer.material.color = Color.grey;
            }
            
            if (whiteFlagVisual != null)
            {
                whiteFlagVisual.SetActive(true);
            }
            
            gameObject.tag = "Untagged";
            
            GameEvents.TriggerUnitSurrender(gameObject);
        }

        private void Die()
        {
            GameEvents.TriggerUnitDeath(gameObject);
            Destroy(gameObject);
        }

        #endregion

        #region Visual Helpers

        /// <summary>
        /// Set unit visual to indicate it has acted.
        /// </summary>
        public void SetActedVisual()
        {
            if (meshRenderer != null)
            {
                meshRenderer.material.color = Color.gray;
            }
        }

        /// <summary>
        /// Reset unit visual to original color.
        /// </summary>
        public void ResetVisual()
        {
            if (meshRenderer != null)
            {
                meshRenderer.material.color = originalColor;
            }
        }

        /// <summary>
        /// Capture current color as original (call after material setup).
        /// </summary>
        public void CaptureOriginalColor()
        {
            if (meshRenderer != null)
            {
                originalColor = meshRenderer.material.color;
            }
        }

        #endregion

        #region Stat Helpers

        /// <summary>
        /// Check if a stat is this unit's primary stat.
        /// </summary>
        public bool IsPrimaryStat(StatType stat)
        {
            if (stat == primaryStat) return true;
            if (hasTwoPrimaryStats && stat == secondaryPrimaryStat) return true;
            return false;
        }

        /// <summary>
        /// Check if a stat is this unit's secondary stat.
        /// </summary>
        public bool IsSecondaryStat(StatType stat)
        {
            return stat == secondaryStat;
        }

        #endregion

        #region Legacy/Event Handlers

        /// <summary>
        /// Handle team-wide events (placeholder for future use).
        /// </summary>
        public void OnTeamEvent(string eventType) { }

        #endregion
    }
}