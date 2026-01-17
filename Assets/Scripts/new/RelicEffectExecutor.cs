using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Enums;
using TacticalGame.Units;
using TacticalGame.Grid;
using TacticalGame.Core;
using TacticalGame.Config;
using TacticalGame.Managers;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Executes relic effects when cards are played.
    /// Handles all 96 relic effects (8 categories x 12 roles).
    /// </summary>
    public static class RelicEffectExecutor
    {
        #region Main Execute Method
        
        /// <summary>
        /// Execute a relic effect.
        /// </summary>
        public static void Execute(EquippedRelic relic, UnitStatus caster, UnitStatus target = null, GridCell targetCell = null)
        {
            if (relic == null || relic.effectData == null)
            {
                Debug.LogWarning("Cannot execute null relic");
                return;
            }
            
            var effect = relic.effectData;
            Debug.Log($"<color=cyan>Executing {relic.relicName}: {effect.effectType}</color>");
            
            // Get managers
            var gridManager = ServiceLocator.Get<GridManager>();
            var energyManager = ServiceLocator.Get<EnergyManager>();
            
            switch (effect.effectType)
            {
                // ========== BOOTS ==========
                case RelicEffectType.Boots_SwapWithUnit:
                    ExecuteSwapWithUnit(caster, target);
                    break;
                case RelicEffectType.Boots_MoveAlly:
                    ExecuteMoveAlly(caster, target, targetCell, (int)effect.value1);
                    break;
                case RelicEffectType.Boots_MoveRestoreMorale:
                    ExecuteMoveAndEffect(caster, targetCell, (int)effect.value1, () => {
                        caster.RestoreMorale(Mathf.RoundToInt(caster.MaxMorale * effect.value2));
                    });
                    break;
                case RelicEffectType.Boots_AllyFreeMoveLowestMorale:
                    ExecuteGrantFreeMove(GetLowestMoraleAlly(caster));
                    break;
                case RelicEffectType.Boots_MoveClearBuzz:
                    ExecuteMoveAndEffect(caster, targetCell, (int)effect.value1, () => {
                        caster.ReduceBuzz(caster.CurrentBuzz); // Clear all buzz
                    });
                    break;
                case RelicEffectType.Boots_FreeIfGrog:
                    // Energy cost handled by card system, just move
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    break;
                case RelicEffectType.Boots_MoveReduceDamage:
                    ExecuteMoveAndEffect(caster, targetCell, (int)effect.value1, () => {
                        ApplyDamageReduction(caster, effect.value2, effect.duration);
                    });
                    break;
                case RelicEffectType.Boots_MoveAnyIfHighestHP:
                    int distance = IsHighestHP(caster) ? 99 : (int)effect.value1;
                    ExecuteMove(caster, targetCell, distance);
                    break;
                case RelicEffectType.Boots_MoveToNeutral:
                    ExecuteMoveToNeutralZone(caster, targetCell);
                    break;
                case RelicEffectType.Boots_MoveGainGrit:
                    ExecuteMoveAndEffect(caster, targetCell, (int)effect.value1, () => {
                        ApplyGritBoost(caster, effect.value2, effect.duration);
                    });
                    break;
                case RelicEffectType.Boots_MoveGainAim:
                    ExecuteMoveAndEffect(caster, targetCell, (int)effect.value1, () => {
                        ApplyAimBoost(caster, effect.value2, effect.duration);
                    });
                    break;
                case RelicEffectType.Boots_MoveReduceRangedCost:
                    ExecuteMoveAndEffect(caster, targetCell, (int)effect.value1, () => {
                        ApplyReduceNextRangedCost(caster, (int)effect.value2);
                    });
                    break;
                    
                // ========== GLOVES ==========
                case RelicEffectType.Gloves_AttackReduceEnemyDraw:
                    ExecuteAttackWithEffect(caster, target, () => {
                        ApplyReduceCardDraw(target, (int)effect.value2, effect.duration);
                    });
                    break;
                case RelicEffectType.Gloves_AttackIncreaseEnemyCost:
                    ExecuteAttackWithEffect(caster, target, () => {
                        ApplyIncreaseCost(target, (int)effect.value2, effect.duration);
                    });
                    break;
                case RelicEffectType.Gloves_AttackBonusByMissingMorale:
                    float missingMoralePercent = 1f - target.MoralePercent;
                    int bonusDamage = Mathf.RoundToInt(50 * missingMoralePercent); // Base 50 * missing%
                    ExecuteAttackWithBonusDamage(caster, target, bonusDamage);
                    break;
                case RelicEffectType.Gloves_AttackMarkMoraleFocus:
                    ExecuteAttackWithEffect(caster, target, () => {
                        ApplyMoraleFocusMark(target, effect.duration);
                    });
                    break;
                case RelicEffectType.Gloves_AttackPreventBuzzReduce:
                    ExecuteAttackWithEffect(caster, target, () => {
                        ApplyPreventBuzzReduction(target, effect.duration);
                    });
                    break;
                case RelicEffectType.Gloves_AttackBonusPerGrog:
                    int grogBonus = Mathf.RoundToInt(energyManager.GrogTokens * effect.value2 * 100);
                    ExecuteAttackWithBonusDamage(caster, target, grogBonus);
                    break;
                case RelicEffectType.Gloves_AttackBonusIfMoreHP:
                    if (caster.CurrentHP > target.CurrentHP)
                        ExecuteAttackWithBonusDamage(caster, target, Mathf.RoundToInt(50 * effect.value2));
                    else
                        ExecuteAttack(caster, target);
                    break;
                case RelicEffectType.Gloves_AttackLowerEnemyHealth:
                    ExecuteAttackWithEffect(caster, target, () => {
                        ApplyHealthStatReduction(target, effect.value2, effect.duration);
                    });
                    break;
                case RelicEffectType.Gloves_AttackPushForward:
                    ExecuteAttackWithEffect(caster, target, () => {
                        PushUnit(target, (int)effect.value2, true); // Forward = toward enemy spawn
                    });
                    break;
                case RelicEffectType.Gloves_AttackForceTargetClosest:
                    ExecuteAttackWithEffect(caster, target, () => {
                        ApplyForceTargetClosest(target, effect.duration);
                    });
                    break;
                case RelicEffectType.Gloves_AttackBonusPerCardPlayed:
                    // Need card system tracking
                    ExecuteAttack(caster, target);
                    break;
                case RelicEffectType.Gloves_AttackBonusPerGunnerRelic:
                    // Need game-wide tracking
                    ExecuteAttack(caster, target);
                    break;
                    
                // ========== HAT ==========
                case RelicEffectType.Hat_DrawCardsVulnerable:
                    DrawCards((int)effect.value1, caster);
                    ApplyVulnerable(caster, effect.value2, effect.duration);
                    break;
                case RelicEffectType.Hat_DrawUltimate:
                    DrawUltimateCard(caster);
                    break;
                case RelicEffectType.Hat_RestoreMoraleLowest:
                    var lowestMorale = GetLowestMoraleAlly(caster);
                    if (lowestMorale != null)
                        lowestMorale.RestoreMorale(Mathf.RoundToInt(lowestMorale.MaxMorale * effect.value2));
                    break;
                case RelicEffectType.Hat_RestoreMoraleNearby:
                    foreach (var ally in GetAlliesInRange(caster, effect.tileRange))
                        ally.RestoreMorale(Mathf.RoundToInt(ally.MaxMorale * effect.value2));
                    break;
                case RelicEffectType.Hat_FreeRumUsage:
                    ApplyFreeRumUsage((int)effect.value1);
                    break;
                case RelicEffectType.Hat_GenerateGrog:
                    energyManager.AddGrog((int)effect.value1);
                    break;
                case RelicEffectType.Hat_ReturnDamage:
                    ApplyDamageReturn(caster, (int)effect.value1, effect.duration);
                    break;
                case RelicEffectType.Hat_IncreaseHealthStat:
                    ApplyHealthStatBoost(caster, effect.value2, effect.duration);
                    break;
                case RelicEffectType.Hat_EnergyOnKnockback:
                    ApplyEnergyOnKnockback(caster, (int)effect.value1, effect.duration);
                    break;
                case RelicEffectType.Hat_SwapEnemyByGrit:
                    ExecuteSwapEnemiesByGrit(caster);
                    break;
                case RelicEffectType.Hat_WeaponUseTwice:
                    ApplyWeaponUseTwice(caster);
                    break;
                case RelicEffectType.Hat_DrawWeaponRelic:
                    DrawWeaponRelicCard(caster);
                    break;
                    
                // ========== COAT ==========
                case RelicEffectType.Coat_BuffNearbyAimPower:
                    foreach (var ally in GetAlliesInRange(caster, effect.tileRange))
                    {
                        ApplyAimBoost(ally, effect.value2, effect.duration);
                        ApplyPowerBoost(ally, effect.value2, effect.duration);
                    }
                    break;
                case RelicEffectType.Coat_DrawOnEnemyAttack:
                    ApplyDrawOnEnemyAttack(caster, (int)effect.value1, effect.duration);
                    break;
                case RelicEffectType.Coat_ReduceMoraleDamage:
                    foreach (var ally in GetAllAllies(caster))
                        ApplyMoraleDamageReduction(ally, effect.value2, effect.duration);
                    break;
                case RelicEffectType.Coat_PreventSurrender:
                    ApplyPreventSurrender(target ?? caster, effect.value2, effect.duration);
                    break;
                case RelicEffectType.Coat_ReduceRumEffect:
                    foreach (var ally in GetAlliesInRange(caster, effect.tileRange))
                        ApplyReducedRumEffect(ally, effect.value2, effect.duration);
                    break;
                case RelicEffectType.Coat_EnemyBuzzOnDamage:
                    ApplyEnemyBuzzOnDamage(effect.duration);
                    break;
                case RelicEffectType.Coat_PreventDisplacement:
                    foreach (var ally in GetAlliesInRange(caster, effect.tileRange))
                        ApplyPreventDisplacement(ally, effect.duration);
                    break;
                case RelicEffectType.Coat_ProtectLowHP:
                    var lowestHP = GetLowestHPAlly(caster);
                    if (lowestHP != null)
                        ApplyOnlyLowerHPCanTarget(lowestHP, effect.duration);
                    break;
                case RelicEffectType.Coat_RowCantBeTargeted:
                    ApplyRowCantBeTargeted(caster, effect.duration);
                    break;
                case RelicEffectType.Coat_ColumnDamageBoost:
                    foreach (var ally in GetAlliesInColumn(caster))
                        ApplyDamageBoost(ally, effect.value2, effect.duration);
                    break;
                case RelicEffectType.Coat_FreeStow:
                    ApplyFreeStows((int)effect.value1);
                    break;
                case RelicEffectType.Coat_RowRangedProtection:
                    foreach (var ally in GetAlliesInRow(caster))
                        ApplyRangedDamageReduction(ally, effect.value2, effect.duration);
                    break;
                    
                // ========== TOTEM ==========
                case RelicEffectType.Totem_SummonCannon:
                    SummonCannon(caster, (int)effect.value1);
                    break;
                case RelicEffectType.Totem_CurseCaptainReflect:
                    ApplyCaptainDamageReflect(caster, effect.duration);
                    break;
                case RelicEffectType.Totem_RallyNoMoraleDamage:
                    foreach (var ally in GetAlliesInRange(caster, effect.tileRange))
                        ApplyNoMoraleDamage(ally, effect.duration);
                    break;
                case RelicEffectType.Totem_EnemyDeathMoraleSwing:
                    // Passive - handled by event system
                    break;
                case RelicEffectType.Totem_SummonHighQualityRum:
                    AddHighQualityRum((int)effect.value1);
                    break;
                case RelicEffectType.Totem_ConvertGrogToEnergy:
                    if (energyManager.TrySpendGrog((int)effect.value1))
                        energyManager.TrySpendEnergy(-(int)effect.value2); // Negative spend = gain
                    break;
                case RelicEffectType.Totem_StunOnKnockback:
                    ApplyStunOnKnockback(caster, effect.duration);
                    break;
                case RelicEffectType.Totem_SummonAnchorHealthBuff:
                    SummonAnchor(caster, effect.value2, effect.duration, effect.tileRange);
                    break;
                case RelicEffectType.Totem_SummonTargetDummy:
                    SummonTargetDummy(caster, (int)effect.value1);
                    break;
                case RelicEffectType.Totem_SummonObstacleDisplace:
                    SummonObstacleAndDisplace(target, targetCell);
                    break;
                case RelicEffectType.Totem_SummonExplodingBarrels:
                    SummonExplodingBarrels((int)effect.value1, effect.duration, effect.tileRange);
                    break;
                case RelicEffectType.Totem_CurseRangedWeapons:
                    ApplyCurseRangedWeapons(effect.value2, effect.duration);
                    break;
                    
                // ========== ULTIMATE ==========
                case RelicEffectType.Ultimate_ShipCannon:
                    ExecuteShipCannonUltimate((int)effect.value1, (int)effect.value2);
                    break;
                case RelicEffectType.Ultimate_MarkCaptainOnly:
                    ExecuteMarkCaptainUltimate(caster);
                    break;
                case RelicEffectType.Ultimate_ReflectMoraleDamage:
                    ApplyReflectMoraleDamage(effect.duration);
                    break;
                case RelicEffectType.Ultimate_ReviveAlly:
                    ReviveAlly(caster, effect.value2);
                    break;
                case RelicEffectType.Ultimate_FullBuzzAttack:
                    ExecuteAttackWithEffect(caster, target, () => {
                        target.ReduceBuzz(-target.MaxBuzz); // Fill buzz
                        ApplyPreventBuzzReduction(target, effect.duration);
                    });
                    break;
                case RelicEffectType.Ultimate_RumBottleAoE:
                    ExecuteRumBottleAoE(caster, targetCell, (int)effect.value1, effect.duration, effect.tileRange);
                    break;
                case RelicEffectType.Ultimate_SummonHardObstacles:
                    SummonHardObstacles((int)effect.value1, effect.duration);
                    break;
                case RelicEffectType.Ultimate_IgnoreHighestHP:
                    ApplyIgnoreHighestHP(effect.duration);
                    break;
                case RelicEffectType.Ultimate_KnockbackToLastColumn:
                    ExecuteAttackWithEffect(caster, target, () => {
                        PushToLastColumn(target);
                    });
                    break;
                case RelicEffectType.Ultimate_AttackKnockbackNearby:
                    ExecuteAttackWithEffect(caster, target, () => {
                        foreach (var nearby in GetEnemiesInRange(target, effect.tileRange))
                            PushUnit(nearby, 1, false);
                    });
                    break;
                case RelicEffectType.Ultimate_StunAoE:
                    ExecuteAttackWithEffect(caster, target, () => {
                        target.ApplyStun(effect.duration);
                        foreach (var nearby in GetEnemiesInRange(target, effect.tileRange))
                            nearby.ApplyStun(effect.duration);
                    });
                    break;
                case RelicEffectType.Ultimate_MassiveSingleTarget:
                    var nearbyEnemies = GetEnemiesInRange(target, effect.tileRange);
                    float multiplier = nearbyEnemies.Count == 0 ? (1f + effect.value2) : 1f;
                    ExecuteAttackWithBonusDamage(caster, target, Mathf.RoundToInt(50 * multiplier));
                    break;
                    
                // ========== PASSIVE UNIQUE (handled elsewhere) ==========
                default:
                    Debug.LogWarning($"Unhandled effect type: {effect.effectType}");
                    break;
            }
        }
        
        #endregion
        
        #region Movement Helpers
        
        private static void ExecuteMove(UnitStatus caster, GridCell targetCell, int maxDistance)
        {
            if (targetCell == null) return;
            
            var gridManager = ServiceLocator.Get<GridManager>();
            Vector2Int currentPos = gridManager.WorldToGridPosition(caster.transform.position);
            Vector2Int targetPos = new Vector2Int(targetCell.XPosition, targetCell.YPosition);
            
            int distance = Mathf.Abs(currentPos.x - targetPos.x) + Mathf.Abs(currentPos.y - targetPos.y);
            if (distance > maxDistance)
            {
                Debug.Log($"Target too far: {distance} > {maxDistance}");
                return;
            }
            
            // Update grid state
            GridCell currentCell = gridManager.GetCell(currentPos.x, currentPos.y);
            if (currentCell != null) currentCell.RemoveUnit();
            
            // Move unit
            caster.transform.position = targetCell.GetWorldPosition();
            targetCell.PlaceUnit(caster.gameObject);
            
            Debug.Log($"<color=green>{caster.name} moved to ({targetCell.XPosition}, {targetCell.YPosition})</color>");
        }
        
        private static void ExecuteMoveAndEffect(UnitStatus caster, GridCell targetCell, int maxDistance, System.Action effect)
        {
            ExecuteMove(caster, targetCell, maxDistance);
            effect?.Invoke();
        }
        
        private static void ExecuteMoveToNeutralZone(UnitStatus caster, GridCell targetCell)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (targetCell != null && targetCell.XPosition == gridManager.GetMiddleColumnIndex())
            {
                Debug.Log("Cannot move to middle column");
                return;
            }
            ExecuteMove(caster, targetCell, 99);
        }
        
        private static void ExecuteSwapWithUnit(UnitStatus caster, UnitStatus target)
        {
            if (target == null) return;
            
            Vector3 casterPos = caster.transform.position;
            Vector3 targetPos = target.transform.position;
            
            var gridManager = ServiceLocator.Get<GridManager>();
            Vector2Int casterGrid = gridManager.WorldToGridPosition(casterPos);
            Vector2Int targetGrid = gridManager.WorldToGridPosition(targetPos);
            
            GridCell casterCell = gridManager.GetCell(casterGrid.x, casterGrid.y);
            GridCell targetCell = gridManager.GetCell(targetGrid.x, targetGrid.y);
            
            // Swap positions
            caster.transform.position = targetPos;
            target.transform.position = casterPos;
            
            // Update grid
            if (casterCell != null) casterCell.PlaceUnit(target.gameObject);
            if (targetCell != null) targetCell.PlaceUnit(caster.gameObject);
            
            Debug.Log($"<color=green>Swapped {caster.name} with {target.name}</color>");
        }
        
        private static void ExecuteMoveAlly(UnitStatus caster, UnitStatus ally, GridCell targetCell, int maxDistance)
        {
            if (ally == null || targetCell == null) return;
            ExecuteMove(ally, targetCell, maxDistance);
        }
        
        private static void ExecuteGrantFreeMove(UnitStatus unit)
        {
            if (unit == null) return;
            // Apply free move buff
            Debug.Log($"<color=green>{unit.name} granted free move</color>");
            // TODO: Implement free move buff
        }
        
        private static void PushUnit(UnitStatus unit, int tiles, bool forward)
        {
            if (unit == null) return;
            
            var gridManager = ServiceLocator.Get<GridManager>();
            Vector2Int pos = gridManager.WorldToGridPosition(unit.transform.position);
            
            // Determine direction (forward = toward enemy side for player, toward player side for enemy)
            int direction = unit.Team == Team.Player ? 1 : -1;
            if (!forward) direction = -direction;
            
            int newX = pos.x + (tiles * direction);
            newX = Mathf.Clamp(newX, 0, gridManager.GridWidth - 1);
            
            GridCell newCell = gridManager.GetCell(newX, pos.y);
            if (newCell != null && newCell.CanPlaceUnit())
            {
                GridCell currentCell = gridManager.GetCell(pos.x, pos.y);
                if (currentCell != null) currentCell.RemoveUnit();
                
                unit.transform.position = newCell.GetWorldPosition();
                newCell.PlaceUnit(unit.gameObject);
                
                Debug.Log($"<color=yellow>Pushed {unit.name} to ({newX}, {pos.y})</color>");
            }
        }
        
        private static void PushToLastColumn(UnitStatus unit)
        {
            if (unit == null) return;
            
            var gridManager = ServiceLocator.Get<GridManager>();
            Vector2Int pos = gridManager.WorldToGridPosition(unit.transform.position);
            
            int lastColumn = unit.Team == Team.Player ? 0 : gridManager.GridWidth - 1;
            
            GridCell newCell = gridManager.GetCell(lastColumn, pos.y);
            if (newCell != null && newCell.CanPlaceUnit())
            {
                GridCell currentCell = gridManager.GetCell(pos.x, pos.y);
                if (currentCell != null) currentCell.RemoveUnit();
                
                unit.transform.position = newCell.GetWorldPosition();
                newCell.PlaceUnit(unit.gameObject);
                
                Debug.Log($"<color=yellow>Knocked {unit.name} to last column</color>");
            }
        }
        
        #endregion
        
        #region Attack Helpers
        
        private static void ExecuteAttack(UnitStatus attacker, UnitStatus target)
        {
            if (attacker == null || target == null) return;
            
            // Use default weapon attack
            UnitAttack attackComponent = attacker.GetComponent<UnitAttack>();
            if (attackComponent != null)
            {
                // Get weapon type to determine melee or ranged
                bool isMelee = attacker.WeaponType == WeaponType.Melee;
                
                int baseDamage = isMelee 
                    ? 10 + Mathf.RoundToInt(attacker.Power * 0.4f)
                    : 8 + Mathf.RoundToInt(attacker.Aim * 0.4f);
                
                target.TakeDamage(baseDamage, attacker.gameObject, isMelee);
            }
        }
        
        private static void ExecuteAttackWithEffect(UnitStatus attacker, UnitStatus target, System.Action effect)
        {
            ExecuteAttack(attacker, target);
            effect?.Invoke();
        }
        
        private static void ExecuteAttackWithBonusDamage(UnitStatus attacker, UnitStatus target, int bonusDamage)
        {
            if (attacker == null || target == null) return;
            
            bool isMelee = attacker.WeaponType == WeaponType.Melee;
            int baseDamage = isMelee 
                ? 10 + Mathf.RoundToInt(attacker.Power * 0.4f)
                : 8 + Mathf.RoundToInt(attacker.Aim * 0.4f);
            
            target.TakeDamage(baseDamage + bonusDamage, attacker.gameObject, isMelee);
        }
        
        #endregion
        
        #region Status Effect Helpers
        
        private static void ApplyDamageReduction(UnitStatus unit, float percent, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} gains {percent*100}% damage reduction for {duration} turns</color>");
            // TODO: Implement via StatusEffectManager
        }
        
        private static void ApplyGritBoost(UnitStatus unit, float percent, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} gains {percent*100}% Grit boost for {duration} turns</color>");
        }
        
        private static void ApplyAimBoost(UnitStatus unit, float percent, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} gains {percent*100}% Aim boost for {duration} turns</color>");
        }
        
        private static void ApplyPowerBoost(UnitStatus unit, float percent, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} gains {percent*100}% Power boost for {duration} turns</color>");
        }
        
        private static void ApplyReduceNextRangedCost(UnitStatus unit, int amount)
        {
            Debug.Log($"<color=blue>{unit.name} next ranged attack costs {amount} less</color>");
        }
        
        private static void ApplyReduceCardDraw(UnitStatus unit, int amount, int duration)
        {
            Debug.Log($"<color=red>{unit.name} draws {amount} fewer cards for {duration} turns</color>");
        }
        
        private static void ApplyIncreaseCost(UnitStatus unit, int amount, int duration)
        {
            Debug.Log($"<color=red>{unit.name} next card costs +{amount} for {duration} turns</color>");
        }
        
        private static void ApplyMoraleFocusMark(UnitStatus unit, int duration)
        {
            Debug.Log($"<color=red>{unit.name} marked for morale focus fire for {duration} turns</color>");
        }
        
        private static void ApplyPreventBuzzReduction(UnitStatus unit, int duration)
        {
            Debug.Log($"<color=red>{unit.name} cannot reduce buzz for {duration} turns</color>");
        }
        
        private static void ApplyHealthStatReduction(UnitStatus unit, float percent, int duration)
        {
            Debug.Log($"<color=red>{unit.name} health stat reduced {percent*100}% for {duration} turns</color>");
        }
        
        private static void ApplyForceTargetClosest(UnitStatus unit, int duration)
        {
            Debug.Log($"<color=red>{unit.name} forced to attack closest for {duration} turns</color>");
        }
        
        private static void ApplyVulnerable(UnitStatus unit, float multiplier, int duration)
        {
            Debug.Log($"<color=red>{unit.name} takes {multiplier*100}% damage for {duration} turns</color>");
        }
        
        private static void ApplyDamageBoost(UnitStatus unit, float percent, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} gains {percent*100}% damage boost for {duration} turns</color>");
        }
        
        private static void ApplyMoraleDamageReduction(UnitStatus unit, float percent, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} takes {percent*100}% less morale damage for {duration} turns</color>");
        }
        
        private static void ApplyPreventSurrender(UnitStatus unit, float moraleRestore, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} will restore {moraleRestore*100}% morale instead of surrendering for {duration} turns</color>");
        }
        
        private static void ApplyReducedRumEffect(UnitStatus unit, float reduction, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} has reduced rum effect for {duration} turns</color>");
        }
        
        private static void ApplyPreventDisplacement(UnitStatus unit, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} cannot be displaced for {duration} turns</color>");
        }
        
        private static void ApplyRangedDamageReduction(UnitStatus unit, float percent, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} takes {percent*100}% less ranged damage for {duration} turns</color>");
        }
        
        private static void ApplyNoMoraleDamage(UnitStatus unit, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} immune to morale damage for {duration} turns</color>");
        }
        
        private static void ApplyStunOnKnockback(UnitStatus unit, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} will stun attacker if knocked back for {duration} turns</color>");
        }
        
        private static void ApplyHealthStatBoost(UnitStatus unit, float percent, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} health stat increased {percent*100}% for {duration} turns</color>");
        }
        
        private static void ApplyDamageReturn(UnitStatus unit, int instances, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} will return {instances} damage instances for {duration} turns</color>");
        }
        
        private static void ApplyEnergyOnKnockback(UnitStatus unit, int energy, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} will gain {energy} energy if knocked back for {duration} turns</color>");
        }
        
        private static void ApplyWeaponUseTwice(UnitStatus unit)
        {
            Debug.Log($"<color=blue>{unit.name} can use next weapon twice</color>");
        }
        
        private static void ApplyDrawOnEnemyAttack(UnitStatus unit, int maxDraws, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} draws card on enemy attack (max {maxDraws}) for {duration} turns</color>");
        }
        
        private static void ApplyOnlyLowerHPCanTarget(UnitStatus unit, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} can only be targeted by enemies with lower HP for {duration} turns</color>");
        }
        
        private static void ApplyRowCantBeTargeted(UnitStatus unit, int duration)
        {
            Debug.Log($"<color=blue>Allies behind {unit.name} in row cannot be targeted for {duration} turns</color>");
        }
        
        private static void ApplyFreeStows(int count)
        {
            Debug.Log($"<color=blue>Next {count} stows are free</color>");
        }
        
        private static void ApplyEnemyBuzzOnDamage(int duration)
        {
            Debug.Log($"<color=yellow>Enemies' buzz fills when dealing damage for {duration} turns</color>");
        }
        
        private static void ApplyCaptainDamageReflect(UnitStatus caster, int duration)
        {
            Debug.Log($"<color=purple>Enemy captain damage reflects to allies for {duration} turns</color>");
        }
        
        private static void ApplyCurseRangedWeapons(float reduction, int duration)
        {
            Debug.Log($"<color=purple>Enemy ranged weapons deal {reduction*100}% less damage for {duration} turns</color>");
        }
        
        private static void ApplyReflectMoraleDamage(int duration)
        {
            Debug.Log($"<color=purple>Morale damage to allies reflects to enemies for {duration} turns</color>");
        }
        
        private static void ApplyIgnoreHighestHP(int duration)
        {
            Debug.Log($"<color=purple>Highest HP enemy (except captain) ignored for {duration} turns</color>");
        }
        
        #endregion
        
        #region Card Helpers
        
        #region Card Helpers
        
        private static void DrawCards(int count, UnitStatus caster)
        {
            var deckManager = caster?.GetComponent<CardDeckManager>();
            if (deckManager != null)
            {
                deckManager.DrawCards(count);
                Debug.Log($"<color=cyan>{caster.name} drew {count} cards</color>");
            }
            else
            {
                Debug.LogWarning($"No CardDeckManager on {caster?.name}");
            }
        }
        
        private static void DrawUltimateCard(UnitStatus caster)
        {
            var deckManager = caster?.GetComponent<CardDeckManager>();
            if (deckManager != null)
            {
                var ultimateCard = deckManager.FindCardByCategory(RelicCategory.Ultimate);
                if (ultimateCard != null)
                {
                    deckManager.AddCardToHand(ultimateCard);
                    Debug.Log($"<color=purple>{caster.name} drew Ultimate: {ultimateCard.GetDisplayName()}</color>");
                }
                else
                {
                    Debug.Log($"<color=yellow>{caster.name} has no Ultimate card to draw</color>");
                }
            }
        }
        
        private static void DrawWeaponRelicCard(UnitStatus caster)
        {
            var deckManager = caster?.GetComponent<CardDeckManager>();
            if (deckManager != null)
            {
                var weaponCard = deckManager.FindCardByCategory(RelicCategory.Weapon);
                if (weaponCard != null)
                {
                    deckManager.AddCardToHand(weaponCard);
                    Debug.Log($"<color=orange>{caster.name} drew Weapon: {weaponCard.GetDisplayName()}</color>");
                }
                else
                {
                    Debug.Log($"<color=yellow>{caster.name} has no Weapon card to draw</color>");
                }
            }
        }
        
        private static void ApplyFreeRumUsage(int count)
        {
            Debug.Log($"<color=cyan>Next {count} rum uses are free</color>");
            // TODO: Implement rum system tracking
        }
        
        private static void AddHighQualityRum(int count)
        {
            Debug.Log($"<color=cyan>Added {count} high quality rum</color>");
            // TODO: Implement rum inventory system
        }
        
        #endregion
        #endregion
        
        #region Summon Helpers
        
        private static void SummonCannon(UnitStatus caster, int hp)
        {
            Debug.Log($"<color=yellow>Summoned cannon with {hp} HP</color>");
            // TODO: Spawn cannon prefab
        }
        
        private static void SummonAnchor(UnitStatus caster, float healthBoost, int duration, int range)
        {
            Debug.Log($"<color=yellow>Summoned anchor: +{healthBoost*100}% health in {range} tile radius for {duration} turns</color>");
        }
        
        private static void SummonTargetDummy(UnitStatus caster, int hp)
        {
            Debug.Log($"<color=yellow>Summoned target dummy with {hp} HP</color>");
        }
        
        private static void SummonObstacleAndDisplace(UnitStatus target, GridCell cell)
        {
            Debug.Log($"<color=yellow>Summoned obstacle and displaced target</color>");
        }
        
        private static void SummonExplodingBarrels(int count, int delay, int range)
        {
            Debug.Log($"<color=yellow>Summoned {count} exploding barrels (explode in {delay} turns, {range} tile radius)</color>");
        }
        
        private static void SummonHardObstacles(int count, int duration)
        {
            Debug.Log($"<color=yellow>Summoned {count} hard obstacles for {duration} turns</color>");
        }
        
        #endregion
        
        #region Ultimate Helpers
        
        private static void ExecuteShipCannonUltimate(int damage, int shots)
        {
            Debug.Log($"<color=purple>SHIP CANNON: {shots} shots of {damage} damage + fire hazard</color>");
            // TODO: Implement
        }
        
        private static void ExecuteMarkCaptainUltimate(UnitStatus caster)
        {
            Debug.Log($"<color=purple>MARK CAPTAIN: Enemy captain is only target this turn</color>");
        }
        
        private static void ReviveAlly(UnitStatus caster, float percentHP)
        {
            Debug.Log($"<color=purple>REVIVE: Revive ally at {percentHP*100}% HP/Morale</color>");
            // TODO: Implement dead unit tracking
        }
        
        private static void ExecuteRumBottleAoE(UnitStatus caster, GridCell cell, int damage, int duration, int range)
        {
            Debug.Log($"<color=purple>RUM BOTTLE: {damage} damage in {range} tiles, rum spill for {duration} turns</color>");
        }
        
        private static void ExecuteSwapEnemiesByGrit(UnitStatus caster)
        {
            var enemies = GetAllEnemies(caster);
            if (enemies.Count < 2) return;
            
            var highest = enemies.OrderByDescending(e => e.Grit).First();
            var lowest = enemies.OrderBy(e => e.Grit).First();
            
            ExecuteSwapWithUnit(highest, lowest);
        }
        
        #endregion
        
        #region Query Helpers
        
        private static List<UnitStatus> GetAllAllies(UnitStatus caster)
        {
            return GameObject.FindGameObjectsWithTag("Unit")
                .Select(go => go.GetComponent<UnitStatus>())
                .Where(u => u != null && u.Team == caster.Team && !u.HasSurrendered)
                .ToList();
        }
        
        private static List<UnitStatus> GetAllEnemies(UnitStatus caster)
        {
            Team enemyTeam = caster.Team == Team.Player ? Team.Enemy : Team.Player;
            return GameObject.FindGameObjectsWithTag("Unit")
                .Select(go => go.GetComponent<UnitStatus>())
                .Where(u => u != null && u.Team == enemyTeam && !u.HasSurrendered)
                .ToList();
        }
        
        private static List<UnitStatus> GetAlliesInRange(UnitStatus caster, int range)
        {
            return GetAllAllies(caster)
                .Where(u => GetManhattanDistance(caster, u) <= range)
                .ToList();
        }
        
        private static List<UnitStatus> GetEnemiesInRange(UnitStatus target, int range)
        {
            return GetAllEnemies(target)
                .Where(u => GetManhattanDistance(target, u) <= range)
                .ToList();
        }
        
        private static List<UnitStatus> GetAlliesInRow(UnitStatus caster)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            Vector2Int casterPos = gridManager.WorldToGridPosition(caster.transform.position);
            
            return GetAllAllies(caster)
                .Where(u => {
                    Vector2Int pos = gridManager.WorldToGridPosition(u.transform.position);
                    return pos.y == casterPos.y;
                })
                .ToList();
        }
        
        private static List<UnitStatus> GetAlliesInColumn(UnitStatus caster)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            Vector2Int casterPos = gridManager.WorldToGridPosition(caster.transform.position);
            
            return GetAllAllies(caster)
                .Where(u => {
                    Vector2Int pos = gridManager.WorldToGridPosition(u.transform.position);
                    return pos.x == casterPos.x;
                })
                .ToList();
        }
        
        private static UnitStatus GetLowestMoraleAlly(UnitStatus caster)
        {
            return GetAllAllies(caster)
                .Where(u => u != caster)
                .OrderBy(u => u.CurrentMorale)
                .FirstOrDefault();
        }
        
        private static UnitStatus GetLowestHPAlly(UnitStatus caster)
        {
            return GetAllAllies(caster)
                .Where(u => u != caster)
                .OrderBy(u => u.CurrentHP)
                .FirstOrDefault();
        }
        
        private static bool IsHighestHP(UnitStatus caster)
        {
            return GetAllAllies(caster).All(u => u.CurrentHP <= caster.CurrentHP);
        }
        
        private static int GetManhattanDistance(UnitStatus a, UnitStatus b)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            Vector2Int posA = gridManager.WorldToGridPosition(a.transform.position);
            Vector2Int posB = gridManager.WorldToGridPosition(b.transform.position);
            return Mathf.Abs(posA.x - posB.x) + Mathf.Abs(posA.y - posB.y);
        }
        
        #endregion
    }
}