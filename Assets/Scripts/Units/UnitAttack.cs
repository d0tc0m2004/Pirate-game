using UnityEngine;
using TacticalGame.Equipment;
using TacticalGame.Enums;
using TacticalGame.Config;
using TacticalGame.Combat;
using TacticalGame.Units;
using TacticalGame.Core;
using TacticalGame.Grid;
using TacticalGame.Managers;
using TacticalGame.Hazards;

/// <summary>
/// Handles unit attacks with full weapon relic integration.
/// Supports both legacy keyboard attacks and new card-based attacks.
/// </summary>
public class UnitAttack : MonoBehaviour
{
    [Header("Stats")]
    public int attackEnergyCost = 1;

    private UnitStatus myStatus;
    private UnitMovement myMovement;
    private EnergyManager energyManager;
    private GridManager gridManager;

    [Header("Weapon Relic")]
    private WeaponRelic equippedWeaponRelic;
    private int attacksThisTurn = 0;
    private int comboCount = 0; // For skill-based combo system
    private AttackAnimator attackAnimator;

    private void Start()
    {
        myStatus = GetComponent<UnitStatus>();
        myMovement = GetComponent<UnitMovement>();
        attackAnimator = GetComponent<AttackAnimator>();
        if (attackAnimator == null) attackAnimator = gameObject.AddComponent<AttackAnimator>();
        energyManager = FindFirstObjectByType<EnergyManager>();
        gridManager = FindFirstObjectByType<GridManager>();
    }

    public void SetupManagers(GridManager grid, EnergyManager energy)
    {
        this.gridManager = grid;
        this.energyManager = energy;
    }

    /// <summary>
    /// Set the weapon relic for this unit's next attack.
    /// </summary>
    public void SetWeaponRelic(WeaponRelic relic)
    {
        equippedWeaponRelic = relic;
        if (relic != null)
        {
            Debug.Log($"<color=cyan>{gameObject.name} using relic: {relic.relicName}</color>");
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
    /// Reset attacks at start of turn.
    /// </summary>
    public void ResetForNewTurn()
    {
        attacksThisTurn = 0;
    }

    /// <summary>
    /// Reset combo counter (called by TurnManager at turn start).
    /// </summary>
    public void ResetCombo()
    {
        comboCount = 0;
    }

    #region Card-Based Attacks (New System)

    /// <summary>
    /// Execute an attack using a specific weapon relic from a card.
    /// Called by RelicCardUI - energy is already spent by the card system.
    /// </summary>
    public void ExecuteCardAttack(WeaponRelic relic, bool energyAlreadySpent = true)
    {
        if (!CanAct()) 
        {
            Debug.Log($"<color=red>{name} cannot act!</color>");
            return;
        }

        if (relic == null)
        {
            Debug.LogError("ExecuteCardAttack called with null relic!");
            return;
        }

        // Set the relic for this attack
        equippedWeaponRelic = relic;

        // Determine if melee or ranged from the RELIC's weapon, not the unit's default
        bool isMelee = relic.baseWeaponData?.attackType == WeaponType.Melee;

        // For ranged attacks, check arrows
        if (!isMelee)
        {
            if (myStatus.CurrentArrows <= 0)
            {
                Debug.Log($"<color=red>{name} has no arrows!</color>");
                return;
            }
        }

        // Find target
        UnitStatus target = FindNearestEnemy();
        if (target == null)
        {
            Debug.Log($"<color=yellow>{name} found no target!</color>");
            return;
        }

        // Check for obstacles
        if (IsBlockedByRow(target))
        {
            Debug.Log($"<color=yellow>Attack blocked by obstacle!</color>");
            if (!isMelee) myStatus.UseArrow(); // Ranged attacks still use arrow
            return;
        }

        // Use arrow for ranged
        if (!isMelee)
        {
            myStatus.UseArrow();
        }

        // Execute the actual attack
        ExecuteAttackWithRelic(target, isMelee, relic);
    }

    /// <summary>
    /// Core attack execution with full relic support.
    /// </summary>
    private void ExecuteAttackWithRelic(UnitStatus target, bool isMelee, WeaponRelic relic)
    {
        attacksThisTurn++;
        comboCount++;
        bool isFirstAttack = (attacksThisTurn == 1);

        var config = GameConfig.Instance;
        var bonuses = GetStandingBonuses();

        // === START DAMAGE REPORT ===
        System.Text.StringBuilder damageLog = new System.Text.StringBuilder();
        damageLog.AppendLine($"<color=yellow>╔══════════════════════════════════════════════════════════════╗</color>");
        damageLog.AppendLine($"<color=yellow>║ ATTACK: {myStatus.UnitName} ({myStatus.Role}) → {target.UnitName}</color>");
        damageLog.AppendLine($"<color=yellow>║ Weapon: {relic?.baseWeaponData?.weaponName ?? "None"} | Relic: {relic?.relicName ?? "None"}</color>");
        damageLog.AppendLine($"<color=yellow>╠══════════════════════════════════════════════════════════════╣</color>");

        // === BASE DAMAGE CALCULATION ===
        int weaponBaseDmg = relic?.baseWeaponData != null ? relic.baseWeaponData.baseDamage : (isMelee ? config.meleeBaseDamage : config.rangedBaseDamage);
        damageLog.AppendLine($"<color=white>║ [1] Base Weapon Damage: {weaponBaseDmg}</color>");

        // === STAT SCALING ===
        int scaledDamage = isMelee ? DamageCalculator.GetMeleeBaseDamage(myStatus, weaponBaseDmg) : DamageCalculator.GetRangedBaseDamage(myStatus, weaponBaseDmg);
        damageLog.AppendLine($"<color=white>║ [2] Stat Scaling (Flat): +{(isMelee ? myStatus.Power : myStatus.Aim)} = {scaledDamage}</color>");

        // === RELIC EFFECT BONUS ===
        int effectFlatBonus = 0; // Temporarily 0 until RelicEffectHandler is updated to flat
        string effectName = relic != null ? relic.effectData.effectName : "None";
        damageLog.AppendLine($"<color=cyan>║ [3] Relic Effect ({effectName}): +{effectFlatBonus} (Flat)</color>");

        // === WEAPON BASE EFFECT BONUS ===
        int weaponEffectFlat = 0;
        string weaponEffectName = relic?.baseWeaponData != null && relic.baseWeaponData.effectType != WeaponEffectType.None ? relic.baseWeaponData.effectType.ToString() : "None";
        damageLog.AppendLine($"<color=red>║ [4] Weapon Effect ({weaponEffectName}): +{weaponEffectFlat} (Flat)</color>");

        // === PROFICIENCY BONUS (if role matches) ===
        int proficiencyBonus = 0;
        bool roleMatches = relic != null && relic.MatchesRole(myStatus.Role);
        string matchStr = roleMatches ? $"★ YES (Proficiency: {myStatus.Proficiency})" : "No";
        damageLog.AppendLine($"<color=green>║ [5] Role Match: {matchStr} -> +{proficiencyBonus}</color>");

        // === DRUNK PENALTY ===
        int drunkMod = myStatus.IsTooDrunk && !DamageCalculator.HasNoBuzzDownside(myStatus) ? -1 : 0;
        string drunkStr = myStatus.IsTooDrunk ? $"YES" : "No";
        damageLog.AppendLine($"<color=orange>║ [6] Too Drunk: {drunkStr} -> {drunkMod} (Flat penalty)</color>");

        // === FIRST ATTACK / COMBO ===
        damageLog.AppendLine($"<color=white>║ [7] First Attack This Turn: {(isFirstAttack ? "Yes" : "No")} | Combo Count: {comboCount}</color>");

        // === HAZARD BONUSES ===
        if (bonuses.hp > 0 || bonuses.morale > 0 || bonuses.applyCurse)
        {
            damageLog.AppendLine($"<color=red>║ [8] Hazard Bonuses: +{bonuses.hp} HP dmg, +{bonuses.morale} Morale dmg, Curse: {bonuses.applyCurse}</color>");
        }

        // === CALCULATE FINAL DAMAGE ===
        int finalDmg = Mathf.Max(0, scaledDamage + effectFlatBonus + weaponEffectFlat + proficiencyBonus + drunkMod);

        damageLog.AppendLine($"<color=yellow>╠══════════════════════════════════════════════════════════════╣</color>");
        damageLog.AppendLine($"<color=yellow>║ FINAL CALCULATION:</color>");
        damageLog.AppendLine($"<color=yellow>║ {scaledDamage} + {effectFlatBonus} + {weaponEffectFlat} + {proficiencyBonus} + {drunkMod}</color>");
        damageLog.AppendLine($"<color=yellow>║ = <b>{finalDmg} RAW DAMAGE</b></color>");
        damageLog.AppendLine($"<color=yellow>╚══════════════════════════════════════════════════════════════╝</color>");;

        Debug.Log(damageLog.ToString());

        // === CHECK COVER ===
        bool hasCover = CheckAdjacencyCover(target);

        // === DEAL DAMAGE (delayed until animation completes) ===
        System.Action applyDamage = () =>
        {
            target.TakeDamage(
                finalDmg, 
                this.gameObject, 
                isMelee, 
                bonuses.hp, 
                bonuses.morale, 
                bonuses.applyCurse,
                isFirstAttack,
                comboCount
            );

            // Check if target died
            bool targetDied = target.CurrentHP <= 0 || target.HasSurrendered;

            // === APPLY ON-HIT EFFECTS ===
            if (relic != null)
            {
                Debug.Log($"<color=cyan>║ Applying Relic On-Hit Effect: {relic.effectData.effectName} ({relic.effectData.effectType})</color>");
                WeaponRelicEffectHandler.ApplyOnHitEffect(
                    myStatus,
                    target,
                    relic,
                    finalDmg,
                    targetDied
                );
            }
            
            // === APPLY WEAPON BASE EFFECT ===
            if (relic?.baseWeaponData != null && relic.baseWeaponData.effectType != WeaponEffectType.None)
            {
                Debug.Log($"<color=red>║ Applying Weapon Effect: {relic.baseWeaponData.effectType}</color>");
                WeaponEffectHandler.ApplyPostAttackEffect(
                    myStatus,
                    target,
                    relic.baseWeaponData,
                    finalDmg,
                    targetDied
                );
            }

            // Final summary
            string resultStr = targetDied ? "<color=red>TARGET KILLED!</color>" : $"Target HP: {target.CurrentHP}/{target.MaxHP}";
            Debug.Log($"<color=green>══ ATTACK COMPLETE: {name} → {target.name} | {resultStr}</color>");
        };

        System.Action postAttack = () =>
        {
            // === POST-ATTACK CLEANUP ===
            myStatus.ReduceBuzz(GameConfig.Instance.buzzDecayOnAttack);
            
            // Mark as attacked
            if (myMovement != null)
            {
                myMovement.MarkAsAttacked();
            }

            // Fire event
            GameEvents.TriggerUnitAttack(this.gameObject, target.gameObject);
        };

        // Play animation then apply damage
        if (attackAnimator != null && target != null)
        {
            if (isMelee)
            {
                attackAnimator.PlayMeleeAttack(target.transform, applyDamage, postAttack);
            }
            else
            {
                attackAnimator.PlayRangedAttack(target.transform, applyDamage, postAttack);
            }
        }
        else
        {
            // No animator — instant fallback
            applyDamage();
            postAttack();
        }
    }

    #endregion

    #region Legacy Keyboard Attacks (Backward Compatibility)

    /// <summary>
    /// Legacy melee attack (keyboard 'C' key).
    /// Uses unit's default weapon type check.
    /// </summary>
    public void TryMeleeAttack()
    {
        if (!CanAct()) return;

        // Check unit's default weapon type for legacy attacks
        if (myStatus.WeaponType == WeaponType.Ranged)
        {
            Debug.Log($"<color=red>{name} cannot Melee! (Equipped: Ranged)</color>");
            return;
        }

        if (!SpendActionEnergy()) return;

        UnitStatus target = FindNearestEnemy();
        if (target != null)
        {
            if (IsBlockedByRow(target))
            {
                Debug.Log("Attack Blocked by Obstacle in Row!");
                return;
            }

            // Use legacy execution or relic if one is set
            if (equippedWeaponRelic != null)
            {
                ExecuteAttackWithRelic(target, true, equippedWeaponRelic);
            }
            else
            {
                ExecuteLegacyAttack(target, true);
            }
        }
    }

    /// <summary>
    /// Legacy ranged attack (keyboard 'X' key).
    /// Uses unit's default weapon type check.
    /// </summary>
    public void TryRangedAttack()
    {
        if (!CanAct()) return;
        
        if (myStatus.WeaponType == WeaponType.Melee)
        {
            Debug.Log($"<color=red>{name} cannot Shoot! (Equipped: Melee)</color>");
            return;
        }

        if (myStatus.CurrentArrows <= 0) return;

        if (!SpendActionEnergy()) return;

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
            
            // Use legacy execution or relic if one is set
            if (equippedWeaponRelic != null)
            {
                ExecuteAttackWithRelic(target, false, equippedWeaponRelic);
            }
            else
            {
                ExecuteLegacyAttack(target, false);
            }
        }
    }

    /// <summary>
    /// Legacy attack execution without relic (fallback).
    /// </summary>
    private void ExecuteLegacyAttack(UnitStatus target, bool isMelee)
    {
        attacksThisTurn++;
        comboCount++;
        bool isFirstAttack = (attacksThisTurn == 1);

        var config = GameConfig.Instance;
        var bonuses = GetStandingBonuses();

        // Calculate base damage using GameConfig formulas
        int baseDmg;
        if (isMelee)
        {
            baseDmg = DamageCalculator.GetMeleeBaseDamage(myStatus);
        }
        else
        {
            baseDmg = DamageCalculator.GetRangedBaseDamage(myStatus);
        }

        // Deal damage (delayed until animation completes)
        System.Action doDamage = () =>
        {
            target.TakeDamage(
                baseDmg, 
                this.gameObject, 
                isMelee, 
                bonuses.hp, 
                bonuses.morale, 
                bonuses.applyCurse,
                isFirstAttack,
                comboCount
            );
        };

        System.Action doCleanup = () =>
        {
            // Post-attack cleanup
            myStatus.ReduceBuzz(config.buzzDecayOnAttack);
            
            if (myMovement != null)
            {
                myMovement.MarkAsAttacked();
            }

            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.gray;
            }

            GameEvents.TriggerUnitAttack(this.gameObject, target.gameObject);
        };

        // Play animation then apply damage
        if (attackAnimator != null && target != null)
        {
            if (isMelee)
                attackAnimator.PlayMeleeAttack(target.transform, doDamage, doCleanup);
            else
                attackAnimator.PlayRangedAttack(target.transform, doDamage, doCleanup);
        }
        else
        {
            doDamage();
            doCleanup();
        }
    }

    #endregion

    #region Helper Methods

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

    bool CheckAdjacencyCover(UnitStatus target)
    {
        if (gridManager == null) return false;

        Vector2Int targetPos = gridManager.WorldToGridPosition(target.transform.position);
        Vector2Int[] neighbors = 
        {
            new Vector2Int(targetPos.x + 1, targetPos.y),
            new Vector2Int(targetPos.x - 1, targetPos.y),
            new Vector2Int(targetPos.x, targetPos.y + 1),
            new Vector2Int(targetPos.x, targetPos.y - 1)
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

    UnitStatus FindNearestEnemy()
    {
        // Use the centralized TargetFinder
        return TargetFinder.FindNearestEnemy(myStatus);
    }

    private bool SpendActionEnergy()
    {
        if (myStatus.Team == Team.Enemy)
        {
            var enemyEnergy = ServiceLocator.Get<TacticalGame.Managers.EnemyEnergyManager>();
            if (enemyEnergy != null) return enemyEnergy.TrySpendEnergy(attackEnergyCost);
            return true; // Fallback if no manager
        }
        else
        {
            if (energyManager == null) energyManager = ServiceLocator.Get<EnergyManager>();
            if (energyManager != null) return energyManager.TrySpendEnergy(attackEnergyCost);
            return true;
        }
    }

    bool CanAct()
    {
        if (myStatus == null) return false;
        if (myStatus.HasSurrendered) return false;
        if (myStatus.IsStunned) return false;
        
        // Check if already attacked via movement component
        if (myMovement != null && myMovement.HasAttacked) return false;
        
        return true;
    }

    #endregion
}