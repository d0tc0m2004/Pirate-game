using UnityEngine;
using TacticalGame.Equipment;
using TacticalGame.Enums;
using TacticalGame.Units;
using TacticalGame.Grid;
using TacticalGame.Managers;
using TacticalGame.Hazards;

namespace TacticalGame.Units
{
    /// <summary>
    /// Handles unit attack logic with weapon relic integration.
    /// </summary>
    public class UnitAttack : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private int attackEnergyCost = 1;

        private UnitStatus myStatus;
        private UnitMovement myMovement;
        private EnergyManager energyManager;
        private GridManager gridManager;

        [Header("Weapon Relic")]
        private WeaponRelic equippedWeaponRelic;
        private int attacksThisTurn = 0;
        private int comboCount = 0;

        private void Start()
        {
            myStatus = GetComponent<UnitStatus>();
            myMovement = GetComponent<UnitMovement>();
            energyManager = FindFirstObjectByType<EnergyManager>();
            gridManager = FindFirstObjectByType<GridManager>();
        }

        public void SetupManagers(GridManager grid, EnergyManager energy)
        {
            this.gridManager = grid;
            this.energyManager = energy;
        }

        /// <summary>
        /// Set the weapon relic for this unit (called during deployment).
        /// </summary>
        public void SetWeaponRelic(WeaponRelic relic)
        {
            equippedWeaponRelic = relic;
            if (relic != null)
            {
                Debug.Log($"<color=cyan>{gameObject.name} equipped: {relic.relicName}</color>");
            }
        }

        /// <summary>
        /// Get the equipped weapon relic.
        /// </summary>
        public WeaponRelic GetWeaponRelic()
        {
            return equippedWeaponRelic;
        }

        /// <summary>
        /// Reset combo counter at start of turn.
        /// </summary>
        public void ResetCombo()
        {
            comboCount = 0;
            attacksThisTurn = 0;
        }

        /// <summary>
        /// Reset attacks at start of turn.
        /// </summary>
        public void ResetForNewTurn()
        {
            attacksThisTurn = 0;
            comboCount = 0;
        }

        public void TryMeleeAttack()
        {
            if (!CanAct()) return;

            // Check weapon type using enum
            if (myStatus.WeaponType == WeaponType.Ranged)
            {
                Debug.Log($"<color=red>{name} cannot Melee! (Equipped: Ranged)</color>");
                return;
            }

            if (energyManager == null) energyManager = FindFirstObjectByType<EnergyManager>();
            if (!energyManager.TrySpendEnergy(attackEnergyCost)) return;

            UnitStatus target = FindNearestEnemy();
            if (target != null)
            {
                if (IsBlockedByRow(target))
                {
                    Debug.Log("Attack Blocked by Obstacle in Row!");
                    return;
                }

                ExecuteAttack(target, true);
            }
        }

        public void TryRangedAttack()
        {
            if (!CanAct()) return;
            
            // Check weapon type using enum
            if (myStatus.WeaponType == WeaponType.Melee)
            {
                Debug.Log($"<color=red>{name} cannot Shoot! (Equipped: Melee)</color>");
                return;
            }

            if (myStatus.CurrentArrows <= 0) return;

            if (energyManager == null) energyManager = FindFirstObjectByType<EnergyManager>();
            if (!energyManager.TrySpendEnergy(attackEnergyCost)) return;

            UnitStatus target = FindNearestEnemy();
            if (target != null)
            {
                if (IsBlockedByRow(target))
                {
                    Debug.Log("Shot Blocked by Obstacle in Row!");
                    myStatus.UseArrow();
                    return;
                }

                myStatus.UseArrow();
                ExecuteAttack(target, false);
            }
        }

        /// <summary>
        /// Execute the attack with weapon relic effects.
        /// </summary>
        private void ExecuteAttack(UnitStatus target, bool isMelee)
        {
            attacksThisTurn++;
            comboCount++;
            bool isFirstAttack = (attacksThisTurn == 1);

            var bonuses = GetStandingBonuses();

            // Calculate base damage using properties
            float drunkMod = myStatus.IsTooDrunk ? 0.8f : 1.0f;
            int baseDmg;
            
            if (isMelee)
            {
                baseDmg = 10 + Mathf.RoundToInt(myStatus.Power * 0.4f);
            }
            else
            {
                baseDmg = 8 + Mathf.RoundToInt(myStatus.Aim * 0.4f);
            }

            // Apply weapon relic damage bonus
            float relicMultiplier = 1.0f;
            if (equippedWeaponRelic != null)
            {
                // Add weapon base damage from relic
                baseDmg += equippedWeaponRelic.GetTotalBaseDamage();

                // Calculate bonus damage from relic effects
                relicMultiplier = WeaponRelicEffectHandler.CalculateBonusDamageMultiplier(
                    myStatus,
                    target,
                    equippedWeaponRelic,
                    isFirstAttack,
                    false, // TODO: track if attacker moved last turn
                    false  // TODO: track if target moved last turn
                );
            }

            int finalDmg = Mathf.RoundToInt(baseDmg * drunkMod * relicMultiplier);

            // Store target HP before damage
            int targetHPBefore = target.CurrentHP;

            // Deal damage with combo count
            target.TakeDamage(finalDmg, this.gameObject, isMelee, bonuses.hp, bonuses.morale, bonuses.applyCurse, isFirstAttack, comboCount);

            // Check if target died
            bool targetDied = target.CurrentHP <= 0 || target.HasSurrendered;

            // Apply weapon relic on-hit effects
            if (equippedWeaponRelic != null)
            {
                WeaponRelicEffectHandler.ApplyOnHitEffect(
                    myStatus,
                    target,
                    equippedWeaponRelic,
                    finalDmg,
                    targetDied
                );
            }

            // Post-attack cleanup
            myStatus.ReduceBuzz(TacticalGame.Config.GameConfig.Instance.buzzDecayOnAttack);
            myMovement.MarkAsAttacked();
            
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null) meshRenderer.material.color = Color.gray;
        }

        (int hp, int morale, bool applyCurse) GetStandingBonuses()
        {
            int totalHP = 0;
            int totalMorale = 0;
            bool applyCurse = false;

            if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
            if (gridManager != null)
            {
                Vector2Int pos = gridManager.WorldToGridPosition(transform.position);
                GridCell cell = gridManager.GetCell(pos.x, pos.y);

                if (cell != null && cell.HasHazard && cell.HazardVisualObject != null)
                {
                    HazardInstance hazardInst = cell.HazardVisualObject.GetComponent<HazardInstance>();
                    if (hazardInst != null && hazardInst.Data != null)
                    {
                        totalHP += hazardInst.Data.standingBonusHP;
                        totalMorale += hazardInst.Data.standingBonusMorale;
                        if (hazardInst.Data.standingAppliesCurse) applyCurse = true;
                    }
                }
            }
            return (totalHP, totalMorale, applyCurse);
        }

        bool IsBlockedByRow(UnitStatus target)
        {
            if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
            if (gridManager == null) return false;

            Vector2Int myPos = gridManager.WorldToGridPosition(transform.position);
            Vector2Int targetPos = gridManager.WorldToGridPosition(target.transform.position);

            if (myPos.y != targetPos.y) return false;

            int startX = Mathf.Min(myPos.x, targetPos.x) + 1;
            int endX = Mathf.Max(myPos.x, targetPos.x);

            for (int x = startX; x < endX; x++)
            {
                GridCell cell = gridManager.GetCell(x, myPos.y);
                if (cell != null && cell.HasHazard && cell.HazardVisualObject != null)
                {
                    HazardInstance hazard = cell.HazardVisualObject.GetComponent<HazardInstance>();
                    if (hazard != null && (hazard.IsHardObstacle || hazard.IsSoftObstacle))
                    {
                        hazard.TakeObstacleDamage(100);
                        return true;
                    }
                }
            }
            return false;
        }

        UnitStatus FindNearestEnemy()
        {
            GameObject[] allUnits = GameObject.FindGameObjectsWithTag("Unit");
            UnitStatus nearest = null;
            float minDistance = float.MaxValue;

            foreach (GameObject unitObj in allUnits)
            {
                if (unitObj == this.gameObject) continue;
                UnitStatus status = unitObj.GetComponent<UnitStatus>();
                if (status == null) continue;
                if (status.HasSurrendered) continue;

                // Check if enemy using Team enum
                bool isMyEnemy = (myStatus.Team == Team.Player && status.Team == Team.Enemy) ||
                                 (myStatus.Team == Team.Enemy && status.Team == Team.Player);

                if (isMyEnemy)
                {
                    float distX = Mathf.Abs(transform.position.x - unitObj.transform.position.x);
                    float distZ = Mathf.Abs(transform.position.z - unitObj.transform.position.z);
                    float dist = distX + distZ;
                    if (dist < minDistance) { minDistance = dist; nearest = status; }
                }
            }
            return nearest;
        }

        bool CanAct()
        {
            if (myStatus.HasSurrendered) return false;
            if (myStatus.IsStunned) return false;
            if (myMovement.HasAttacked) return false;
            return true;
        }
    }
}