using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Units;
using TacticalGame.Grid;
using TacticalGame.Core;
using TacticalGame.Enums;
using TacticalGame.Managers;
using TacticalGame.Hazards;
using TacticalGame.Combat;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Executes all relic effects based on RelicEffectType.
    /// Uses StatusEffectManager for buffs/debuffs.
    /// </summary>
    public static class RelicEffectExecutor
    {
        #region Main Execute Method
        
        public static void Execute(EquippedRelic relic, UnitStatus caster, UnitStatus target = null, GridCell targetCell = null, BattleCard card = null)
        {
            if (relic == null || caster == null)
            {
                Debug.LogWarning("RelicEffectExecutor: Missing relic or caster");
                return;
            }
            
            var effectData = relic.effectData;
            if (effectData == null)
            {
                Debug.LogWarning("RelicEffectExecutor: No effect data found");
                return;
            }
            
            ExecuteByEffectType(effectData.effectType, effectData, caster, target, targetCell, card);
        }
        
        public static void Execute(RelicEffectData effectData, UnitStatus caster, UnitStatus target = null, GridCell targetCell = null, BattleCard card = null)
        {
            if (effectData == null || caster == null)
            {
                Debug.LogWarning("RelicEffectExecutor: Missing effect data or caster");
                return;
            }
            
            ExecuteByEffectType(effectData.effectType, effectData, caster, target, targetCell, card);
        }
        
        private static void ExecuteByEffectType(RelicEffectType effectType, RelicEffectData effect, 
            UnitStatus caster, UnitStatus target, GridCell targetCell, BattleCard card = null)
        {
            Debug.Log($"<color=cyan>Executing {effectType} by {caster.UnitName}</color>");
            
            if (target == null)
            {
                target = GetClosestEnemy(caster);
            }
            
            switch (effectType)
            {
                // ==================== BOOTS ====================
                case RelicEffectType.Boots_SwapWithUnit:
                    ExecuteSwapWithUnit(caster, target, card);
                    break;
                    
                case RelicEffectType.Boots_MoveAlly:
                    ExecuteMoveAlly(caster, target, (int)effect.value1, card);
                    break;
                    
                case RelicEffectType.Boots_MoveRestoreMorale:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    caster.RestoreMorale(Mathf.RoundToInt(caster.MaxMorale * effect.value2));
                    break;
                    
                case RelicEffectType.Boots_AllyFreeMoveLowestMorale:
                {
                    var lowestAlly = GetLowestMoraleAlly(caster);
                    if (lowestAlly != null)
                    {
                        // Bypass the normal 1-step logic and trigger a board-wide Teleport selection!
                        RelicTargetSelector.Instance.SelectTile(
                            $"Teleport {lowestAlly.UnitName} anywhere on your side",
                            (destinationCell) =>
                            {
                                // Verify the tile is empty and on the correct side of the board
                                if (destinationCell != null && destinationCell.IsPlayerSide && destinationCell.CanPlaceUnit() && !destinationCell.IsMiddleColumn)
                                {
                                    if (card != null) BattleDeckManager.Instance.ConsumeCard(card);
                                    TeleportUnit(lowestAlly, destinationCell);
                                }
                                else
                                {
                                    Debug.Log("Invalid tile! You must select an empty tile on your side of the board.");
                                }
                            },
                            () => { Debug.Log("Move cancelled"); },
                            true, // requireEmpty
                            99,   // range (99 = entire board)
                            lowestAlly,
                            true, // playerSideOnly
                            true  // isFirstStep
                        );
                    }
                    break;
                }
                    
                case RelicEffectType.Boots_MoveClearBuzz:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    caster.ReduceBuzz(caster.CurrentBuzz);
                    break;
                    
                case RelicEffectType.Boots_FreeIfGrog:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    break;
                    
                case RelicEffectType.Boots_MoveReduceDamage:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    ApplyDamageReduction(caster, effect.value2, effect.duration);
                    break;
                    
                case RelicEffectType.Boots_MoveAnyIfHighestHP:
                    {
                        int moveRange = IsHighestHP(caster) ? 99 : (int)effect.value1;
                        ExecuteMove(caster, targetCell, moveRange);
                    }
                    break;
                    
                case RelicEffectType.Boots_MoveToNeutral:
                    ExecuteMoveToNeutralZone(caster, targetCell);
                    break;
                    
                case RelicEffectType.Boots_MoveGainGrit:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    ApplyGritBoost(caster, effect.value2, effect.duration);
                    break;
                    
                case RelicEffectType.Boots_MoveGainAim:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    ApplyAimBoost(caster, effect.value2, effect.duration);
                    break;
                    
                case RelicEffectType.Boots_MoveReduceRangedCost:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    ApplyReduceRangedCost(caster, (int)effect.value2);
                    break;

                // ==================== GLOVES ====================
                case RelicEffectType.Gloves_AttackReduceEnemyDraw:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        if (target != null && target.CurrentHP > 0 && !target.HasSurrendered)
                            ApplyReduceCardDraw(target, (int)effect.value2, effect.duration);
                    }
                    break;
                    
                case RelicEffectType.Gloves_AttackIncreaseEnemyCost:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        if (target != null && target.CurrentHP > 0 && !target.HasSurrendered)
                            ApplyIncreaseCost(target, (int)effect.value2, effect.duration);
                    }
                    break;
                    
                case RelicEffectType.Gloves_AttackBonusByMissingMorale:
                    if (target != null)
                    {
                        float missingMorale = 1f - target.MoralePercent;
                        ExecuteAttackWithPercentBonus(caster, target, missingMorale);
                    }
                    break;
                    
                case RelicEffectType.Gloves_AttackMarkMoraleFocus:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        ApplyMoraleFocus(target, effect.duration);
                    }
                    break;
                    
                case RelicEffectType.Gloves_AttackPreventBuzzReduce:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        ApplyPreventBuzzReduction(target, effect.duration);
                    }
                    break;
                    
                case RelicEffectType.Gloves_AttackBonusPerGrog:
                    if (target != null)
                    {
                        var energyManager = ServiceLocator.Get<EnergyManager>();
                        int grog = energyManager != null ? energyManager.GrogTokens : 0;
                        float bonusPercent = grog * effect.value2;
                        ExecuteAttackWithPercentBonus(caster, target, bonusPercent);
                    }
                    break;
                    
                case RelicEffectType.Gloves_AttackBonusIfMoreHP:
                    if (target != null)
                    {
                        float bonus = caster.CurrentHP > target.CurrentHP ? effect.value2 : 0f;
                        ExecuteAttackWithPercentBonus(caster, target, bonus);
                    }
                    break;
                    
                case RelicEffectType.Gloves_AttackLowerEnemyHealth:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        ApplyHealthStatReduction(target, effect.value2, effect.duration);
                    }
                    break;
                    
                case RelicEffectType.Gloves_AttackPushForward:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        PushUnit(target, caster, 1);
                    }
                    break;
                    
                case RelicEffectType.Gloves_AttackForceTargetClosest:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        ApplyForceTargetClosest(target, effect.duration);
                    }
                    break;
                    
                case RelicEffectType.Gloves_AttackBonusPerCardPlayed:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                    }
                    break;
                    
                case RelicEffectType.Gloves_AttackBonusPerGunnerRelic:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                    }
                    break;

                // ==================== HAT ====================
                case RelicEffectType.Hat_DrawCardsVulnerable:
                    DrawCards(caster, 2);
                    ApplyVulnerable(caster, 2.0f, effect.duration);
                    break;
                    
                case RelicEffectType.Hat_DrawUltimate:
                    DrawUltimateCard(caster);
                    break;
                    
                case RelicEffectType.Hat_RestoreMoraleLowest:
                    var lowestMoraleAlly = GetLowestMoraleAlly(caster);
                    if (lowestMoraleAlly != null)
                    {
                        lowestMoraleAlly.RestoreMorale(Mathf.RoundToInt(lowestMoraleAlly.MaxMorale * effect.value2));
                    }
                    break;
                    
                case RelicEffectType.Hat_RestoreMoraleNearby:
                    RestoreMoraleNearby(caster, effect.value2, effect.tileRange);
                    break;
                    
                case RelicEffectType.Hat_FreeRumUsage:
                    ApplyFreeRumUsage(caster, (int)effect.value1);
                    break;
                    
                case RelicEffectType.Hat_GenerateGrog:
                    GenerateGrog((int)effect.value1);
                    break;
                    
                case RelicEffectType.Hat_ReturnDamage:
                    ApplyReturnDamage(caster, (int)effect.value1, effect.duration);
                    break;
                    
                case RelicEffectType.Hat_IncreaseHealthStat:
                    ApplyHealthStatBoost(caster, effect.value2, effect.duration);
                    break;
                    
                case RelicEffectType.Hat_EnergyOnKnockback:
                    ApplyEnergyOnKnockback(caster, (int)effect.value1, effect.duration);
                    break;
                    
                case RelicEffectType.Hat_SwapEnemyByGrit:
                    SwapHighestLowestGritEnemies(caster);
                    break;
                    
                case RelicEffectType.Hat_WeaponUseTwice:
                    ApplyWeaponUseTwice(caster, effect.duration);
                    break;
                    
                case RelicEffectType.Hat_DrawWeaponRelic:
                    DrawWeaponRelicCard(caster);
                    break;

                // ==================== COAT ====================
                case RelicEffectType.Coat_BuffNearbyAimPower:
                    BuffNearbyAlliesAimPower(caster, effect.value2, effect.duration, effect.tileRange);
                    break;
                    
                case RelicEffectType.Coat_DrawOnEnemyAttack:
                    ApplyDrawOnEnemyAttack(caster, effect.duration);
                    break;
                    
                case RelicEffectType.Coat_ReduceMoraleDamage:
                    foreach (var ally in GetAllAllies(caster))
                    {
                        var effects = GetStatusEffects(ally);
                        effects?.ApplyEffect(StatusEffect.CreateMoraleDamageReduction(effect.duration, effect.value2, null));
                    }
                    Debug.Log($"All allies take {effect.value2*100}% less morale damage for {effect.duration} turns");
                    break;
                    
                case RelicEffectType.Coat_PreventSurrender:
                    ApplyPreventSurrender(target ?? caster, effect.value2, effect.duration);
                    break;
                    
                case RelicEffectType.Coat_ReduceRumEffect:
                    ApplyReducedRumEffectNearby(caster, effect.value2, effect.duration, effect.tileRange);
                    break;
                    
                case RelicEffectType.Coat_EnemyBuzzOnDamage:
                    ApplyEnemyBuzzOnDamage(caster, effect.duration);
                    break;
                    
                case RelicEffectType.Coat_PreventDisplacement:
                    ApplyPreventDisplacementNearby(caster, effect.duration, effect.tileRange);
                    break;
                    
                case RelicEffectType.Coat_ProtectLowHP:
                    ApplyOnlyLowerHPCanTargetLowest(caster, effect.duration);
                    break;
                    
                case RelicEffectType.Coat_RowCantBeTargeted:
                    // FIXED SHIPWRIGHT V1: Now applies Stealth to the whole row
                    foreach (var ally in GetAlliesInRow(caster))
                    {
                        var effects = GetStatusEffects(ally);
                        effects?.ApplyEffect(StatusEffect.CreateRowCantBeTargeted(effect.duration, null));
                    }
                    break;
                    
                case RelicEffectType.Coat_ColumnDamageBoost:
                    ApplyDamageBoostToColumn(caster, effect.value2, effect.duration);
                    break;
                    
                case RelicEffectType.Coat_FreeStow:
                    ApplyFreeStows(caster, (int)effect.value1);
                    break;
                    
                case RelicEffectType.Coat_RowRangedProtection:
                    ApplyRowRangedProtection(caster, effect.value2, effect.duration);
                    break;

                // ==================== TRINKET (Passive) ====================
                case RelicEffectType.Trinket_BonusDamagePerCard:
                case RelicEffectType.Trinket_BonusVsCaptain:
                case RelicEffectType.Trinket_ImmuneMoraleFocusFire:
                case RelicEffectType.Trinket_EnemySurrenderEarly:
                case RelicEffectType.Trinket_DamageByBuzz:
                case RelicEffectType.Trinket_KnockbackIncreasesBuzz:
                case RelicEffectType.Trinket_ReduceDamageFromClosest:
                case RelicEffectType.Trinket_DrawIfHighHP:
                case RelicEffectType.Trinket_TauntFirstAttack:
                case RelicEffectType.Trinket_KnockbackAttacker:
                case RelicEffectType.Trinket_RowEnemiesLessDamage:
                case RelicEffectType.Trinket_RowEnemiesTakeMore:
                    Debug.Log($"<color=gray>Passive trinket {effectType} - handled by PassiveRelicManager</color>");
                    break;

                // ==================== TOTEM ====================
                case RelicEffectType.Totem_SummonCannon:
                {
                    var hazardManager = ServiceLocator.Get<HazardManager>();
                    var gridManager = ServiceLocator.Get<GridManager>();
                    if (hazardManager != null && gridManager != null)
                    {
                        System.Collections.Generic.List<GridCell> validCells = new System.Collections.Generic.List<GridCell>();
                        for (int x = 0; x < gridManager.GetMiddleColumnIndex(); x++)
                        {
                            for (int y = 0; y < gridManager.GridHeight; y++)
                            {
                                var cell = gridManager.GetCell(x, y);
                                if (cell != null && !cell.IsOccupied && !cell.HasHazard)
                                {
                                    validCells.Add(cell);
                                }
                            }
                        }

                        if (validCells.Count > 0)
                        {
                            GridCell randomCell = validCells[UnityEngine.Random.Range(0, validCells.Count)];
                            
                            int weaponDamage = 0;
                            if (caster.WeaponType == TacticalGame.Enums.WeaponType.Melee)
                            {
                                weaponDamage = TacticalGame.Combat.DamageCalculator.GetMeleeBaseDamage(caster);
                            }
                            else
                            {
                                weaponDamage = TacticalGame.Combat.DamageCalculator.GetRangedBaseDamage(caster);
                            }

                            hazardManager.CreateCannonObstacle(randomCell, (int)effect.value1, weaponDamage);
                        }
                        else
                        {
                            Debug.Log("No empty tiles on player side to spawn cannon!");
                        }
                    }
                    break;
                }
                    
                case RelicEffectType.Totem_CurseCaptainReflect:
                    {
                        var enemyCaptain = GetEnemies(caster).FirstOrDefault(e => e.IsCaptain);
                        if (enemyCaptain != null)
                        {
                            ApplyCaptainDamageReflect(enemyCaptain, effect.duration);
                        }
                    }
                    break;
                    
                case RelicEffectType.Totem_RallyNoMoraleDamage:
                    ApplyNoMoraleDamageNearby(caster, effect.duration, effect.tileRange);
                    break;
                    
                case RelicEffectType.Totem_EnemyDeathMoraleSwing:
                    Debug.Log($"<color=gray>Passive totem {effectType} - handled by PassiveRelicManager</color>");
                    break;
                    
                case RelicEffectType.Totem_SummonHighQualityRum:
                    AddHighQualityRum(caster, (int)effect.value1);
                    break;
                    
                case RelicEffectType.Totem_ConvertGrogToEnergy:
                    ConvertGrogToEnergy((int)effect.value1);
                    break;
                    
                case RelicEffectType.Totem_StunOnKnockback:
                    ApplyStunOnKnockback(caster, effect.duration);
                    break;
                    
                case RelicEffectType.Totem_SummonAnchorHealthBuff:
                    SummonAnchor(caster, targetCell, effect.value2, effect.tileRange, effect.duration);
                    break;
                    
                case RelicEffectType.Totem_SummonTargetDummy:
                    SummonTargetDummy(caster, targetCell, (int)effect.value1);
                    break;
                    
                case RelicEffectType.Totem_SummonObstacleDisplace:
                    SummonObstacleAndDisplace(targetCell, target);
                    break;
                    
                case RelicEffectType.Totem_SummonExplodingBarrels:
                    SummonExplodingBarrels(caster, (int)effect.value1, effect.duration);
                    break;
                    
                case RelicEffectType.Totem_CurseRangedWeapons:
                    CurseEnemyRangedWeapons(caster, effect.value2, effect.duration);
                    break;

                // ==================== ULTIMATE ====================
                case RelicEffectType.Ultimate_ShipCannon:
                    ExecuteShipCannonUltimate(caster, (int)effect.value1, (int)effect.value2);
                    break;
                    
                case RelicEffectType.Ultimate_MarkCaptainOnly:
                    ExecuteMarkCaptainOnly(caster, target);
                    break;
                    
                case RelicEffectType.Ultimate_ReflectMoraleDamage:
                    ApplyReflectMoraleDamage(caster, effect.duration);
                    break;
                    
                case RelicEffectType.Ultimate_ReviveAlly:
                    ReviveAlly(caster, target, effect.value2);
                    break;
                    
                case RelicEffectType.Ultimate_FullBuzzAttack:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        ApplyFullBuzz(target, effect.duration);
                    }
                    break;
                    
                case RelicEffectType.Ultimate_RumBottleAoE:
                    ExecuteRumBottleAoE(caster, targetCell, (int)effect.value1, effect.duration);
                    break;
                    
                case RelicEffectType.Ultimate_SummonHardObstacles:
                    SummonHardObstacles(caster, (int)effect.value1, effect.duration);
                    break;
                    
                case RelicEffectType.Ultimate_IgnoreHighestHP:
                    ApplyIgnoreHighestHP(caster, effect.duration);
                    break;
                    
                case RelicEffectType.Ultimate_KnockbackToLastColumn:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        KnockbackToLastColumn(target);
                    }
                    break;
                    
                case RelicEffectType.Ultimate_AttackKnockbackNearby:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        KnockbackNearbyEnemies(caster, 1);
                    }
                    break;
                    
                case RelicEffectType.Ultimate_StunAoE:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        ApplyStun(target, effect.duration);
                        StunNearbyEnemies(target, effect.duration, 1);
                    }
                    break;
                    
                case RelicEffectType.Ultimate_MassiveSingleTarget:
                    if (target != null)
                    {
                        bool hasNearbyEnemies = HasNearbyEnemies(target, 1);
                        float bonus = hasNearbyEnemies ? 0f : effect.value2;
                        ExecuteAttackWithPercentBonus(caster, target, bonus);
                    }
                    break;

                // ==================== PASSIVE UNIQUE ====================
                case RelicEffectType.PassiveUnique_ExtraEnergy:
                case RelicEffectType.PassiveUnique_ExtraCards:
                case RelicEffectType.PassiveUnique_DeathStrikeByMorale:
                case RelicEffectType.PassiveUnique_LowerSurrenderThreshold:
                case RelicEffectType.PassiveUnique_NoBuzzDownside:
                case RelicEffectType.PassiveUnique_DrawPerGrog:
                case RelicEffectType.PassiveUnique_DrawOnLowDamage:
                case RelicEffectType.PassiveUnique_CounterAttack:
                case RelicEffectType.PassiveUnique_GritAura:
                case RelicEffectType.PassiveUnique_BonusVsLowGrit:
                case RelicEffectType.PassiveUnique_IgnoreRoles:
                case RelicEffectType.PassiveUnique_BonusVsLowHP:
                    Debug.Log($"<color=gray>Passive unique {effectType} - handled by PassiveRelicManager</color>");
                    break;

                // ==================== V2 BOOTS ====================
                case RelicEffectType.Boots_V2_SwapWithEnemy:
                    if (target != null && target.Team != caster.Team)
                        ExecuteSwapWithUnit(caster, target, card);
                    break;
                case RelicEffectType.Boots_V2_MoveAllyGainShield:
                    ExecuteMoveAlly(caster, target, (int)effect.value1, card);
                    break;
                case RelicEffectType.Boots_V2_MoveGainMoraleOnKill:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    ApplyMoraleOnKillBuff(caster, effect.value2, effect.duration);
                    break;
                case RelicEffectType.Boots_V2_AllAlliesMove1:
                    ApplyFreeMoveToAllAllies(caster);
                    break;
                case RelicEffectType.Boots_V2_MoveGainBuzzReduction:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    ApplyBuzzReduction(caster, effect.value2, effect.duration);
                    break;
                case RelicEffectType.Boots_V2_MoveGainGrog:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    GenerateGrog((int)effect.value2);
                    break;
                case RelicEffectType.Boots_V2_MoveGainArmor:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    caster.RestoreHull((int)effect.value2);
                    break;
                case RelicEffectType.Boots_V2_MoveExtraIfLowHP:
                    {
                        int moveRange2 = caster.HPPercent < 0.5f ? (int)effect.value1 + (int)effect.value2 : (int)effect.value1;
                        ExecuteMove(caster, targetCell, moveRange2);
                    }
                    break;
                case RelicEffectType.Boots_V2_MoveHealAdjacent:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    HealAdjacentAllies(caster, effect.value2);
                    break;
                case RelicEffectType.Boots_V2_MovePoisonTile:
                    {
                        var gridManager = ServiceLocator.Get<GridManager>();
                        GridCell previousCell = null;
                        if (gridManager != null)
                        {
                            var coords = gridManager.WorldToGridPosition(caster.transform.position);
                            previousCell = gridManager.GetCell(coords.x, coords.y);
                        }
                        
                        ExecuteMove(caster, targetCell, (int)effect.value1);
                        
                        if (previousCell != null)
                        {
                            CreatePoisonTile(previousCell, (int)effect.value2, effect.duration);
                        }
                    }
                    break;
                case RelicEffectType.Boots_V2_MoveGainDodge:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    ApplyDodgeChance(caster, effect.value2, effect.duration);
                    break;
                case RelicEffectType.Boots_V2_MoveDrawCard:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    DrawCards(caster, (int)effect.value2);
                    break;

                // ==================== V2 GLOVES ====================
                case RelicEffectType.Gloves_V2_AttackStealBuff:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        StealBuff(caster, target);
                    }
                    break;
                case RelicEffectType.Gloves_V2_AttackDiscard:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        ForceDiscard(target, (int)effect.value2);
                    }
                    break;
                case RelicEffectType.Gloves_V2_AttackMoraleDamage:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        int moraleDmg = Mathf.RoundToInt(target.MaxMorale * effect.value2);
                        target.ApplyMoraleDamage(moraleDmg);
                    }
                    break;
                case RelicEffectType.Gloves_V2_AttackHealAlly:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        var lowestAlly = GetLowestHPAlly(caster);
                        if (lowestAlly != null)
                            lowestAlly.Heal(Mathf.RoundToInt(lowestAlly.MaxHP * effect.value2));
                    }
                    break;
                case RelicEffectType.Gloves_V2_AttackReduceBuzz:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        caster.ReduceBuzz((int)effect.value2);
                    }
                    break;
                case RelicEffectType.Gloves_V2_AttackSpendGrogBonus:
                    if (target != null)
                    {
                        var em = ServiceLocator.Get<EnergyManager>();
                        if (em != null && em.TrySpendGrog((int)effect.value1))
                            ExecuteAttackWithPercentBonus(caster, target, effect.value2);
                        else
                            ExecuteAttack(caster, target);
                    }
                    break;
                case RelicEffectType.Gloves_V2_AttackGainHullOnKill:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                    }
                    break;
                case RelicEffectType.Gloves_V2_AttackSlowEnemy:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        ApplySlow(target, (int)effect.value2, effect.duration);
                    }
                    break;
                case RelicEffectType.Gloves_V2_AttackPullEnemy:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        PullUnit(target, caster, (int)effect.value2);
                    }
                    break;
                case RelicEffectType.Gloves_V2_AttackApplyPoison:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        ApplyPoison(target, (int)effect.value2, effect.duration);
                    }
                    break;
                case RelicEffectType.Gloves_V2_AttackBonusVsDebuffed:
                    if (target != null)
                    {
                        var targetEffects = target.GetComponent<StatusEffectManager>();
                        bool hasDebuffs = targetEffects != null && targetEffects.GetActiveDebuffs().Count > 0;
                        float bonus = hasDebuffs ? effect.value2 : 0f;
                        ExecuteAttackWithPercentBonus(caster, target, bonus);
                    }
                    break;
                case RelicEffectType.Gloves_V2_AttackChainToAdjacent:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        ChainDamageToAdjacent(caster, target, effect.value2);
                    }
                    break;

                // ==================== V2 HAT ====================
                case RelicEffectType.Hat_V2_DrawAndShield:
                    DrawCards(caster, (int)effect.value1);
                    ApplyShieldBuff(caster, (int)effect.value2);
                    break;
                case RelicEffectType.Hat_V2_DrawBootsRelic:
                    DrawBootsRelicCard(caster);
                    break;
                case RelicEffectType.Hat_V2_RestoreMoraleAll:
                    RestoreMoraleToAllAllies(caster, effect.value2);
                    break;
                case RelicEffectType.Hat_V2_PreventMoraleLoss:
                    ApplyPreventMoraleLoss(caster, effect.duration);
                    break;
                case RelicEffectType.Hat_V2_RumHealsMore:
                    ApplyRumHealBoost(caster, effect.value2, effect.duration);
                    break;
                case RelicEffectType.Hat_V2_GrogOnEnemyKill:
                    ApplyGrogOnKill(caster, (int)effect.value1, effect.duration);
                    break;
                case RelicEffectType.Hat_V2_DamageReductionBuff:
                    ApplyDamageReduction(caster, effect.value2, effect.duration);
                    break;
                case RelicEffectType.Hat_V2_SpeedBoost:
                    ApplySpeedBoost(caster, (int)effect.value1, effect.duration);
                    break;
                case RelicEffectType.Hat_V2_HealOnCardPlay:
                    ApplyHealOnCardPlay(caster, effect.value2, effect.duration);
                    break;
                case RelicEffectType.Hat_V2_BuffFoodEffects:
                    ApplyFoodEffectBoost(caster, effect.value2, effect.duration);
                    break;
                case RelicEffectType.Hat_V2_DrawPerEnemyInRange:
                    int enemiesInRange = GetEnemiesInRange(caster, effect.tileRange).Count;
                    DrawCards(caster, enemiesInRange);
                    break;
                case RelicEffectType.Hat_V2_ReduceAllCosts:
                    ApplyReduceAllCosts(caster, (int)effect.value2, effect.duration);
                    break;

                // ==================== V2 COAT ====================
                case RelicEffectType.Coat_V2_ShieldNearby:
                    ShieldNearbyAllies(caster, (int)effect.value1, effect.tileRange);
                    break;
                case RelicEffectType.Coat_V2_CounterOnAllyHit:
                    ApplyCounterOnAllyHit(caster, effect.duration);
                    break;
                case RelicEffectType.Coat_V2_MoraleShield:
                    ApplyMoraleShield(caster, (int)effect.value1, effect.duration);
                    break;
                case RelicEffectType.Coat_V2_RevivePrevent:
                    ApplyDeathPrevention(caster, effect.duration);
                    break;
                case RelicEffectType.Coat_V2_BuzzImmunity:
                    ApplyBuzzImmunityNearby(caster, effect.duration, effect.tileRange);
                    break;
                case RelicEffectType.Coat_V2_GrogShield:
                    {
                        var em = ServiceLocator.Get<EnergyManager>();
                        if (em != null && em.TrySpendGrog((int)effect.value1))
                            ApplyShieldBuff(caster, (int)effect.value2);
                    }
                    break;
                case RelicEffectType.Coat_V2_ThornsAura:
                    ApplyThorns(caster, (int)effect.value1, effect.duration);
                    break;
                case RelicEffectType.Coat_V2_DodgeAura:
                    ApplyDodgeAuraNearby(caster, effect.value2, effect.duration, effect.tileRange);
                    break;
                case RelicEffectType.Coat_V2_HealingAura:
                    ApplyHealingAuraNearby(caster, effect.value2, effect.duration, effect.tileRange);
                    break;
                case RelicEffectType.Coat_V2_WellFed:
                    ApplyMaxHPBoostNearby(caster, effect.value2, effect.duration, effect.tileRange);
                    break;
                case RelicEffectType.Coat_V2_Evasion:
                    ApplyDodgeChance(caster, effect.value2, effect.duration);
                    break;
                case RelicEffectType.Coat_V2_RangedBlock:
                    ApplyRangedBlock(caster, (int)effect.value1);
                    break;

                // ==================== V2 TRINKET (Passive) ====================
                case RelicEffectType.Trinket_V2_BonusDamagePerAlly:
                case RelicEffectType.Trinket_V2_DrawOnCaptainHit:
                case RelicEffectType.Trinket_V2_MoraleOnKill:
                case RelicEffectType.Trinket_V2_AllySurrenderLater:
                case RelicEffectType.Trinket_V2_NoBuzzPenalty:
                case RelicEffectType.Trinket_V2_GrogOnTurnStart:
                case RelicEffectType.Trinket_V2_ArmorOnLowHP:
                case RelicEffectType.Trinket_V2_SpeedOnHighHP:
                case RelicEffectType.Trinket_V2_HealOnTurnEnd:
                case RelicEffectType.Trinket_V2_FoodDoubleDuration:
                case RelicEffectType.Trinket_V2_CritChance:
                case RelicEffectType.Trinket_V2_BonusVsFullHP:
                    Debug.Log($"<color=gray>Passive trinket V2 {effectType} - handled by PassiveRelicManager</color>");
                    break;

                // ==================== V2 TOTEM ====================
                case RelicEffectType.Totem_V2_SummonHealingTotem:
                    SummonHealingTotem(caster, targetCell, (int)effect.value1, effect.duration);
                    break;
                case RelicEffectType.Totem_V2_CurseWeakness:
                    if (target != null)
                        ApplyWeaknessCurse(target, effect.value2, effect.duration);
                    break;
                case RelicEffectType.Totem_V2_RallyDamageBoost:
                    ApplyDamageBoostNearby(caster, effect.value2, effect.duration, effect.tileRange);
                    break;
                case RelicEffectType.Totem_V2_SummonMoraleBanner:
                    SummonMoraleBanner(caster, targetCell, effect.duration, effect.tileRange);
                    break;
                case RelicEffectType.Totem_V2_SummonGrogBarrel:
                    SummonGrogBarrel(caster, targetCell, (int)effect.value1);
                    break;
                case RelicEffectType.Totem_V2_TrapTile:
                    PlaceTrap(targetCell, effect.duration);
                    break;
                case RelicEffectType.Totem_V2_SummonShieldGenerator:
                    SummonShieldGenerator(caster, targetCell, (int)effect.value1, effect.duration);
                    break;
                case RelicEffectType.Totem_V2_SummonSpeedBooster:
                    SummonSpeedBooster(caster, targetCell, (int)effect.value1, effect.duration);
                    break;
                case RelicEffectType.Totem_V2_SummonHealingWell:
                    SummonHealingWell(caster, targetCell, effect.value2, effect.duration);
                    break;
                case RelicEffectType.Totem_V2_PoisonCloud:
                    CreatePoisonCloud(targetCell, (int)effect.value1, effect.duration, effect.tileRange);
                    break;
                case RelicEffectType.Totem_V2_SummonDecoy:
                    SummonDecoy(caster, targetCell, effect.duration);
                    break;
                case RelicEffectType.Totem_V2_CurseSlow:
                    if (target != null)
                        ApplySlow(target, (int)effect.value1, effect.duration);
                    break;

                // ==================== V2 ULTIMATE ====================
                case RelicEffectType.Ultimate_V2_TeamwideBuff:
                    ApplyTeamwideBuff(caster, effect.value2, effect.duration);
                    break;
                case RelicEffectType.Ultimate_V2_ExecuteBelow20:
                    ExecuteEnemyBelowThreshold(caster, target, effect.value2);
                    break;
                case RelicEffectType.Ultimate_V2_FullMoraleRestore:
                    FullMoraleRestoreAllAllies(caster);
                    break;
                case RelicEffectType.Ultimate_V2_MassRevive:
                    MassReviveAllies(caster, effect.value2);
                    break;
                case RelicEffectType.Ultimate_V2_BuzzExplosion:
                    BuzzExplosionAllEnemies(caster);
                    break;
                case RelicEffectType.Ultimate_V2_GrogRain:
                    GenerateGrog((int)effect.value1);
                    break;
                case RelicEffectType.Ultimate_V2_Fortress:
                    // FIXED SHIPWRIGHT V2: Percentage Shield!
                    ShieldAllAlliesPercent(caster, effect.value2);
                    break;
                case RelicEffectType.Ultimate_V2_Teleport:
                    RelicTargetSelector.Instance.SelectAllyThenTile(
                        "Select ally to teleport",
                        (ally, destinationCell) => {
                            if (card != null) BattleDeckManager.Instance.ConsumeCard(card);
                            TeleportUnit(ally, destinationCell);
                        },
                        () => {
                            Debug.Log("Teleport cancelled");
                        }
                    );
                    break;
                case RelicEffectType.Ultimate_V2_MassHeal:
                    MassHealAllAllies(caster, effect.value2);
                    break;
                case RelicEffectType.Ultimate_V2_Feast:
                    FeastAllAllies(caster, effect.value1, effect.value2);
                    break;
                case RelicEffectType.Ultimate_V2_BladeStorm:
                    BladeStormAllEnemies(caster, effect.value2, effect.tileRange);
                    break;
                case RelicEffectType.Ultimate_V2_PerfectShot:
                    if (target != null)
                        ExecutePerfectShot(caster, target, effect.value2);
                    break;

                // ==================== V2 PASSIVE UNIQUE ====================
                case RelicEffectType.PassiveUnique_V2_TeamLeader:
                case RelicEffectType.PassiveUnique_V2_CardMaster:
                case RelicEffectType.PassiveUnique_V2_Inspiring:
                case RelicEffectType.PassiveUnique_V2_LastStand:
                case RelicEffectType.PassiveUnique_V2_DrunkMaster:
                case RelicEffectType.PassiveUnique_V2_Efficient:
                case RelicEffectType.PassiveUnique_V2_Unstoppable:
                case RelicEffectType.PassiveUnique_V2_Scout:
                case RelicEffectType.PassiveUnique_V2_Medic:
                case RelicEffectType.PassiveUnique_V2_Nourishing:
                case RelicEffectType.PassiveUnique_V2_Riposte:
                case RelicEffectType.PassiveUnique_V2_Sniper:
                    Debug.Log($"<color=gray>Passive unique V2 {effectType} - handled by PassiveRelicManager</color>");
                    break;
                    
                // ==================== SURGEON SPECIFIC ====================
                case RelicEffectType.Boots_MoveRestoreHealth:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    caster.Heal(Mathf.RoundToInt(caster.MaxHP * effect.value2));
                    break;
                case RelicEffectType.Boots_V2_SwapLowestHealthAlly:
                    {
                        var lowestAlly = GetLowestHPAlly(caster);
                        if (lowestAlly != null)
                            ExecuteSwapWithUnit(caster, lowestAlly, card);
                    }
                    break;
                case RelicEffectType.Gloves_AttackHealLowestAlly:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        var lowestAlly2 = GetLowestHPAlly(caster);
                        if (lowestAlly2 != null)
                        {
                            lowestAlly2.Heal((int)effect.value2);
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V2_AttackHealedEnemy:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Hat_DrawTrinketReduceCost:
                    DrawCards(caster, 1);
                    ApplyReduceAllCosts(caster, 1, 1);
                    break;
                case RelicEffectType.Hat_V2_HealOnCaptainDamage:
                    {
                        var effects1 = GetStatusEffects(caster);
                        effects1?.ApplyEffect(StatusEffect.CreateHealOnCardPlay(effect.duration, effect.value1, null));
                    }
                    break;
                case RelicEffectType.Coat_DoubleAllyStats:
                    if (target != null && target.Team == caster.Team)
                    {
                        var effects2 = GetStatusEffects(target);
                        effects2?.ApplyEffect(StatusEffect.CreateDamageBoost(effect.duration, effect.value1, null));
                    }
                    break;
                case RelicEffectType.Coat_V2_KnockbackOnAllyDeath:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Trinket_BlockEnemyRowMovement:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Trinket_V2_GlobalRadius:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Totem_StunHealedEnemy:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Totem_V2_SummonHealingPotions:
                    {
                        var allies = GetAllAllies(caster);
                        int healed = 0;
                        while (healed < 3 && allies.Count > 0)
                        {
                            var ally = allies[Random.Range(0, allies.Count)];
                            ally.Heal((int)effect.value1);
                            healed++;
                        }
                    }
                    break;
                case RelicEffectType.Ultimate_PreventDeath:
                    if (target != null)
                    {
                        ApplyDeathPrevention(target, effect.duration);
                    }
                    break;
                case RelicEffectType.Ultimate_V2_FullHealthRestore:
                    if (target != null)
                    {
                        target.Heal(target.MaxHP);
                    }
                    break;
                case RelicEffectType.PassiveUnique_HealingAura:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.PassiveUnique_V2_TeamHealOnKill:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;

                // ==================== COOK SPECIFIC ====================
                case RelicEffectType.Boots_MoveDrawCard:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    DrawCards(caster, 1);
                    break;
                case RelicEffectType.Boots_V2_MoveBoostProficiency:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    {
                        var effects3 = GetStatusEffects(caster);
                        effects3?.ApplyEffect(StatusEffect.CreateDamageBoost(1, effect.value2, null));
                    }
                    break;
                case RelicEffectType.Gloves_AttackDetonateBuff:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        ApplyPoison(target, (int)effect.value2, effect.duration);
                    }
                    break;
                case RelicEffectType.Gloves_V2_StasisClosest:
                    {
                        var closest = GetClosestEnemy(caster);
                        if (closest != null)
                        {
                            ApplyStun(closest, effect.duration);
                        }
                    }
                    break;
                case RelicEffectType.Hat_ReduceLowestAllyCardCost:
                    {
                        var lowestAlly3 = GetLowestHPAlly(caster);
                        if (lowestAlly3 != null)
                        {
                            ApplyReduceAllCosts(lowestAlly3, (int)effect.value1, effect.duration);
                        }
                    }
                    break;
                case RelicEffectType.Hat_V2_MoveForwardHeal:
                    if (target != null && target.Team == caster.Team)
                    {
                        PushUnit(caster, target, -1); 
                        target.Heal(Mathf.RoundToInt(target.MaxHP * effect.value1));
                    }
                    break;
                case RelicEffectType.Coat_StunOnAllyAttacked:
                    {
                        var closestAlly = GetAlliesInRange(caster, 1).FirstOrDefault();
                        if (closestAlly != null)
                        {
                            ApplyCounterOnAllyHit(closestAlly, effect.duration);
                        }
                    }
                    break;
                case RelicEffectType.Coat_V2_ClearDebuffsNearby:
                    {
                        var nearbyAllies = GetAlliesInRange(caster, effect.tileRange);
                        foreach (var ally in nearbyAllies)
                        {
                            var effects4 = GetStatusEffects(ally);
                            effects4?.ClearDebuffs();
                        }
                    }
                    break;
                case RelicEffectType.Trinket_HazardSizeIncrease:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Trinket_V2_DrawExtraBelow50:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Totem_HealLowestOnDamage:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Totem_V2_SummonStatDebuffObstacle:
                    {
                        var nearbyEnemies = GetEnemiesInRange(caster, effect.tileRange);
                        foreach (var enemy in nearbyEnemies)
                        {
                            ApplyWeaknessCurse(enemy, effect.value1, effect.duration);
                        }
                    }
                    break;
                case RelicEffectType.Ultimate_SwapHealthClosest:
                    {
                        var closestEnemy = GetClosestEnemy(caster);
                        if (closestEnemy != null)
                        {
                            int casterHP = caster.CurrentHP;
                            int enemyHP = closestEnemy.CurrentHP;
                            
                            caster.SetHP(enemyHP);
                            closestEnemy.SetHP(casterHP);
                        }
                    }
                    break;
                case RelicEffectType.Ultimate_V2_FireColumn:
                    {
                        var closestEnemy2 = target ?? GetClosestEnemy(caster);
                        if (closestEnemy2 != null)
                        {
                            var gridManager = ServiceLocator.Get<GridManager>();
                            var hazardManager = ServiceLocator.Get<HazardManager>();
                            if (gridManager != null)
                            {
                                var pos = gridManager.WorldToGridPosition(closestEnemy2.transform.position);
                                for (int row = 0; row < 8; row++)
                                {
                                    var cell = gridManager.GetCell(pos.x, row);
                                    if (cell == null) continue;

                                    if (cell.IsOccupied && cell.OccupyingUnit != null)
                                    {
                                        var unit = cell.OccupyingUnit.GetComponent<UnitStatus>();
                                        if (unit != null && unit.Team != caster.Team)
                                            unit.TakeDamage((int)effect.value1, caster.gameObject, false);
                                    }

                                    if (hazardManager != null)
                                        hazardManager.CreateFireTile(cell, (int)effect.value2, effect.duration);
                                }
                            }
                        }
                    }
                    break;
                case RelicEffectType.PassiveUnique_DisplaceOnWeaponUse:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.PassiveUnique_V2_RelicsNotConsumed:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;

                // ==================== SWASHBUCKLER SPECIFIC ====================
                case RelicEffectType.Boots_MoveBySpeed:
                    {
                        bool isHighestSpeed = true;
                        foreach (var unit in GetAllUnits())
                        {
                            if (unit != caster && unit.Speed > caster.Speed)
                            {
                                isHighestSpeed = false;
                                break;
                            }
                        }
                        int moveRange = isHighestSpeed ? (int)effect.value2 : (int)effect.value1;
                        ExecuteMove(caster, targetCell, moveRange);
                    }
                    break;
                case RelicEffectType.Boots_V2_MoveRowOnly:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    break;
                case RelicEffectType.Gloves_AttackTwice:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        ExecuteAttack(caster, target);
                    }
                    break;
                case RelicEffectType.Gloves_V2_AttackStunOnMove:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        ApplyStun(target, effect.duration);
                    }
                    break;
                case RelicEffectType.Hat_DrawWeaponReduceCost:
                    DrawWeaponRelicCard(caster);
                    ApplyReduceAllCosts(caster, 1, 1);
                    break;
                case RelicEffectType.Hat_V2_StealEnemyCard:
                    DrawCards(caster, 1);
                    break;
                case RelicEffectType.Coat_NearbyAllyDamageReduction:
                    {
                        var nearbyAllies2 = GetAlliesInRange(caster, effect.tileRange);
                        foreach (var ally in nearbyAllies2)
                        {
                            ApplyDamageReduction(ally, effect.value1, effect.duration);
                        }
                    }
                    break;
                case RelicEffectType.Coat_V2_CurseEmptyTile:
                    if (targetCell != null)
                    {
                        var hazardManager2 = ServiceLocator.Get<HazardManager>();
                        if (hazardManager2 != null)
                            hazardManager2.CreateTrap(targetCell, effect.duration);
                    }
                    break;
                case RelicEffectType.Trinket_BonusDamageIfAlone:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Trinket_V2_EnemySpeedReduction:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Totem_SummonInvisibleTraps:
                    {
                        var gridManager2 = ServiceLocator.Get<GridManager>();
                        var hazardManager3 = ServiceLocator.Get<HazardManager>();
                        if (gridManager2 != null && hazardManager3 != null)
                        {
                            int placed = 0;
                            for (int attempt = 0; attempt < 20 && placed < (int)effect.value1; attempt++)
                            {
                                int x = Random.Range(0, 8);
                                int y = Random.Range(0, 8);
                                var cell = gridManager2.GetCell(x, y);
                                if (cell != null && !cell.IsOccupied)
                                {
                                    hazardManager3.CreateTrap(cell, effect.duration);
                                    placed++;
                                }
                            }
                        }
                    }
                    break;
                case RelicEffectType.Totem_V2_DisableEnemyPassives:
                    {
                        var enemies = GetEnemies(caster);
                        foreach (var enemy in enemies)
                        {
                            ApplyStun(enemy, 0); 
                        }
                    }
                    break;
                case RelicEffectType.Ultimate_ForceLowestAndCaptainFight:
                    {
                        var enemies2 = GetEnemies(caster);
                        var enemyCaptain = enemies2.FirstOrDefault(e => e.IsCaptain);
                        var lowestHP = enemies2.Where(e => !e.IsCaptain).OrderBy(e => e.CurrentHP).FirstOrDefault();
                        if (enemyCaptain != null && lowestHP != null)
                        {
                            ExecuteAttack(lowestHP, enemyCaptain);
                            ExecuteAttack(enemyCaptain, lowestHP);
                        }
                    }
                    break;
                case RelicEffectType.Ultimate_V2_SurrenderOn4Weapons:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.PassiveUnique_EnemyDiscardOnBoot:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.PassiveUnique_V2_EnemyBootsLimit:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;

                // ==================== DECKHAND SPECIFIC ====================
                case RelicEffectType.Boots_MoveColumnOnly:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    break;
                case RelicEffectType.Boots_V2_MoveRestoreHull:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    caster.RestoreHull((int)effect.value2);
                    break;
                case RelicEffectType.Gloves_AttackDrawOnHullDestroyed:
                    if (target != null)
                    {
                        int hullBefore = target.CurrentHullPool;
                        ExecuteAttack(caster, target);
                        if (target.CurrentHullPool <= 0 && hullBefore > 0)
                        {
                            DrawCards(caster, 1);
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V2_AttackEnergyOnHullDestroyed:
                    if (target != null)
                    {
                        int hullBefore2 = target.CurrentHullPool;
                        ExecuteAttack(caster, target);
                        if (target.CurrentHullPool <= 0 && hullBefore2 > 0)
                        {
                            var energyMgr = ServiceLocator.Get<EnergyManager>();
                            energyMgr?.TrySpendEnergy(-1);
                        }
                    }
                    break;
                case RelicEffectType.Hat_NearbyHullIncrease:
                    {
                        var nearbyAllies3 = GetAlliesInRange(caster, effect.tileRange);
                        foreach (var ally in nearbyAllies3)
                        {
                            ally.RestoreHull(Mathf.RoundToInt(ally.MaxHullPool * effect.value1));
                        }
                    }
                    break;
                case RelicEffectType.Hat_V2_DestroyObstaclesGainHull:
                    {
                        caster.RestoreHull(Mathf.RoundToInt(caster.MaxHullPool * effect.value1));
                    }
                    break;
                case RelicEffectType.Coat_HullBonusDamage:
                    {
                        float hullBonus = caster.CurrentHullPool * effect.value1;
                        var nearbyAllies4 = GetAlliesInRange(caster, effect.tileRange);
                        nearbyAllies4.Add(caster);
                        foreach (var ally in nearbyAllies4)
                        {
                            var effects5 = GetStatusEffects(ally);
                            effects5?.ApplyEffect(StatusEffect.CreateDamageBoost(effect.duration, hullBonus / 100f, null));
                        }
                    }
                    break;
                case RelicEffectType.Coat_V2_BuffTileDamageExchange:
                    if (targetCell != null)
                    {
                        if (targetCell.IsOccupied && targetCell.OccupyingUnit != null)
                        {
                            var unit = targetCell.OccupyingUnit.GetComponent<UnitStatus>();
                            if (unit != null)
                            {
                                ApplyVulnerable(unit, effect.value1, effect.duration);
                                var effects6 = GetStatusEffects(unit);
                                effects6?.ApplyEffect(StatusEffect.CreateDamageBoost(effect.duration, effect.value1, null));
                            }
                        }
                    }
                    break;
                case RelicEffectType.Trinket_HullFullRegen:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Trinket_V2_HullDiscardOnSurvive:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Totem_CreateSoftObstacles:
                    {
                        var gridManager3 = ServiceLocator.Get<GridManager>();
                        if (gridManager3 != null)
                        {
                            int placed2 = 0;
                            for (int attempt = 0; attempt < 20 && placed2 < (int)effect.value1; attempt++)
                            {
                                int x = Random.Range(0, 8);
                                int y = Random.Range(0, 8);
                                var cell = gridManager3.GetCell(x, y);
                                if (cell != null && !cell.IsOccupied)
                                {
                                    placed2++;
                                }
                            }
                        }
                    }
                    break;
                case RelicEffectType.Totem_V2_PullNearbyToRow:
                    {
                        var nearbyEnemies2 = GetEnemiesInRange(caster, effect.tileRange);
                        foreach (var enemy in nearbyEnemies2)
                        {
                            PullUnit(caster, enemy, 1);
                        }
                    }
                    break;
                case RelicEffectType.Ultimate_MassiveHullBuff:
                    if (target != null)
                    {
                        int hullAmount = Mathf.RoundToInt(target.MaxHullPool * effect.value1);
                        target.RestoreHull(hullAmount);
                    }
                    break;
                case RelicEffectType.Ultimate_V2_ClearHazardsPlayerSide:
                    {
                        var hazardManager4 = ServiceLocator.Get<HazardManager>();
                        var gridManager4 = ServiceLocator.Get<GridManager>();
                        if (hazardManager4 != null && gridManager4 != null)
                        {
                            for (int x = 0; x < 8; x++)
                            {
                                for (int y = 0; y < 8; y++)
                                {
                                    if (gridManager4.IsPlayerSide(x))
                                    {
                                        var cell = gridManager4.GetCell(x, y);
                                        if (cell != null)
                                            hazardManager4.ClearHazard(cell);
                                    }
                                }
                            }
                        }
                    }
                    break;
                case RelicEffectType.PassiveUnique_HullDestroyedRestoreHealth:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.PassiveUnique_V2_HullDestroyedDamageBonus:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;

                // ==================== NAVIGATOR SPECIFIC ====================
                case RelicEffectType.Boots_MoveFarDistance:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    break;
                case RelicEffectType.Boots_V2_MoveFree:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    break;
                case RelicEffectType.Gloves_DisableWeaponEffect:
                    if (target != null)
                    {
                        var effects7 = GetStatusEffects(target);
                        effects7?.ApplyEffect(StatusEffect.CreateWeakness(effect.duration, 0.5f, null));
                    }
                    break;
                case RelicEffectType.Gloves_V2_AttackBonusPerBootsCard:
                    if (target != null)
                    {
                        int bootsCount = 0;
                        if (BattleDeckManager.Instance != null)
                        {
                            bootsCount = BattleDeckManager.Instance.Hand
                                .Count(c => c.category == RelicCategory.Boots);
                        }
                        float bonus = bootsCount * effect.value1;
                        ExecuteAttackWithPercentBonus(caster, target, bonus);
                    }
                    break;
                case RelicEffectType.Hat_DisableEnemyUltimates:
                    {
                        var enemies3 = GetEnemies(caster);
                        foreach (var enemy in enemies3)
                        {
                            ApplyIncreaseCost(enemy, 99, effect.duration); 
                        }
                    }
                    break;
                case RelicEffectType.Hat_V2_DrawBootsCard:
                    DrawBootsCard(caster);
                    break;
                case RelicEffectType.Coat_HealthDamageImmunity:
                    ApplyDamageReduction(caster, 1f, effect.duration);
                    break;
                case RelicEffectType.Coat_V2_DodgeFirstAttack:
                    {
                        var allAllies = GetAllAllies(caster);
                        foreach (var ally in allAllies)
                        {
                            ApplyDodgeChance(ally, 1f, 1); 
                            break; 
                        }
                    }
                    break;
                case RelicEffectType.Trinket_NearbyTacticsBoost:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Trinket_V2_IgnoreSoftObstacles:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Totem_DisableEnemyMovement:
                    {
                        var enemies4 = GetEnemies(caster);
                        foreach (var enemy in enemies4)
                        {
                            ApplySlow(enemy, 99, effect.duration); 
                        }
                    }
                    break;
                case RelicEffectType.Totem_V2_DisableNonWeaponRelics:
                    {
                        var enemies5 = GetEnemies(caster);
                        foreach (var enemy in enemies5)
                        {
                            ApplyIncreaseCost(enemy, 99, effect.duration);
                        }
                    }
                    break;
                case RelicEffectType.Ultimate_MarkReflectToCaptain:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        var enemyCaptain2 = GetEnemies(caster).FirstOrDefault(e => e.IsCaptain);
                        if (enemyCaptain2 != null)
                        {
                            ApplyVulnerable(target, effect.value1, effect.duration);
                        }
                    }
                    break;
                case RelicEffectType.Ultimate_V2_SwapClosestFurthest:
                    {
                        var enemies6 = GetEnemies(caster);
                        if (enemies6.Count >= 2)
                        {
                            var sorted = enemies6.OrderBy(e =>
                                Vector3.Distance(caster.transform.position, e.transform.position)).ToList();
                            var closest2 = sorted.First();
                            var furthest = sorted.Last();
                            ExecuteSwapWithUnit(closest2, furthest, card);
                        }
                    }
                    break;
                case RelicEffectType.PassiveUnique_FreeMovement:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.PassiveUnique_V2_AllyMovementBoost:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;

                // ==================== MASTER-AT-ARMS SPECIFIC ====================
                case RelicEffectType.Boots_MoveBonusWeaponDamage:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    {
                        var effects8 = GetStatusEffects(caster);
                        effects8?.ApplyEffect(StatusEffect.CreateDamageBoost(1, effect.value2, null));
                    }
                    break;
                case RelicEffectType.Boots_V2_MoveDestroyObstacle:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    break;
                case RelicEffectType.Gloves_AttackBonusPerNearbyAlly:
                    if (target != null)
                    {
                        int nearbyCount = GetAlliesInRange(caster, effect.tileRange).Count;
                        float allyBonus = nearbyCount * effect.value1;
                        ExecuteAttackWithPercentBonus(caster, target, allyBonus);
                    }
                    break;
                case RelicEffectType.Gloves_V2_AttackBonusPerRelicInHand:
                    if (target != null)
                    {
                        int relicCount = 0;
                        if (BattleDeckManager.Instance != null)
                        {
                            relicCount = BattleDeckManager.Instance.Hand
                                .Count(c => c.ownerUnit == caster);
                        }
                        float relicBonus = relicCount * effect.value1;
                        ExecuteAttackWithPercentBonus(caster, target, relicBonus);
                    }
                    break;
                case RelicEffectType.Hat_ReduceUltimateCost:
                    ApplyReduceAllCosts(caster, (int)effect.value1, effect.duration);
                    break;
                case RelicEffectType.Hat_V2_IncreaseEnemyWeaponCost:
                    {
                        var closestEnemy3 = target ?? GetClosestEnemy(caster);
                        if (closestEnemy3 != null)
                        {
                            ApplyIncreaseCost(closestEnemy3, (int)effect.value1, effect.duration);
                        }
                    }
                    break;
                case RelicEffectType.Coat_BonusDamageNearbyAllies:
                    {
                        var nearbyAllies5 = GetAlliesInRange(caster, effect.tileRange);
                        foreach (var ally in nearbyAllies5)
                        {
                            var effects9 = GetStatusEffects(ally);
                            effects9?.ApplyEffect(StatusEffect.CreateDamageBoost(effect.duration, effect.value1, null));
                        }
                    }
                    break;
                case RelicEffectType.Coat_V2_ReduceEnemyPower:
                    {
                        var enemies7 = GetEnemies(caster);
                        foreach (var enemy in enemies7)
                        {
                            ApplyWeaknessCurse(enemy, effect.value1, effect.duration);
                        }
                    }
                    break;
                case RelicEffectType.Trinket_CounterAttackOnHit:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Trinket_V2_NearbyPowerBoost:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Totem_DisableEnemyWeapons:
                    {
                        var enemies8 = GetEnemies(caster);
                        foreach (var enemy in enemies8)
                        {
                            ApplyWeaknessCurse(enemy, 1f, effect.duration); 
                        }
                    }
                    break;
                case RelicEffectType.Totem_V2_EarthquakeHazard:
                    {
                        var gridManager5 = ServiceLocator.Get<GridManager>();
                        var hazardManager5 = ServiceLocator.Get<HazardManager>();
                        if (gridManager5 != null && hazardManager5 != null)
                        {
                            int placed3 = 0;
                            for (int attempt = 0; attempt < 20 && placed3 < (int)effect.value1; attempt++)
                            {
                                int x = Random.Range(0, 8);
                                int y = Random.Range(0, 8);
                                var cell = gridManager5.GetCell(x, y);
                                if (cell != null)
                                {
                                    if (cell.IsOccupied && cell.OccupyingUnit != null)
                                    {
                                        var unit = cell.OccupyingUnit.GetComponent<UnitStatus>();
                                        if (unit != null)
                                            unit.TakeDamage((int)effect.value2, caster.gameObject, false);
                                    }
                                    placed3++;
                                }
                            }
                        }
                    }
                    break;
                case RelicEffectType.Ultimate_AttackAllEnemies:
                    {
                        var allEnemies = GetEnemies(caster);
                        foreach (var enemy in allEnemies)
                        {
                            ExecuteAttack(caster, enemy);
                        }
                    }
                    break;
                case RelicEffectType.Ultimate_V2_AttackRowDamage:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        var gridManager6 = ServiceLocator.Get<GridManager>();
                        if (gridManager6 != null)
                        {
                            var targetPos = gridManager6.WorldToGridPosition(target.transform.position);
                            var rowEnemies = GetEnemies(caster).Where(e =>
                            {
                                var ePos = gridManager6.WorldToGridPosition(e.transform.position);
                                return ePos.y == targetPos.y && e != target;
                            }).ToList();
                            foreach (var enemy in rowEnemies)
                            {
                                enemy.TakeDamage((int)effect.value2, caster.gameObject, false);
                            }
                        }
                    }
                    break;
                case RelicEffectType.PassiveUnique_WeaponRelicOnKill:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.PassiveUnique_V2_HealOnKill:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;

                default:
                    Debug.LogWarning($"<color=orange>Unhandled effect type: {effectType}</color>");
                    break;
            }
        }
        
        #endregion
        
        #region Movement Helpers
        
        private static void ExecuteMove(UnitStatus unit, GridCell targetCell, int maxRange)
        {
            if (unit == null) return;
            
            var movement = unit.GetComponent<UnitMovement>();
            if (movement != null && targetCell != null)
            {
                movement.MoveToCell(targetCell);
                Debug.Log($"{unit.UnitName} moved to ({targetCell.XPosition}, {targetCell.YPosition})");
            }
        }
        
        private static void ExecuteSwapWithUnit(UnitStatus caster, UnitStatus target, BattleCard card = null)
        {
            if (caster == null) return;

            if (target != null)
            {
                SwapUnitsOnGrid(caster, target);
                return;
            }

            RelicTargetSelector.Instance.SelectAlly(
                "Select an ally to swap locations with",
                (ally) =>
                {
                    if (ally == null || ally == caster) 
                    {
                        Debug.Log("Invalid swap target. Cancelled.");
                        return;
                    }
                    if (card != null) BattleDeckManager.Instance.ConsumeCard(card);
                    SwapUnitsOnGrid(caster, ally);
                },
                () => {
                    Debug.Log("Swap cancelled");
                }
            );
        }

        private static void SwapUnitsOnGrid(UnitStatus a, UnitStatus b)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return;

            Vector2Int aPos = gridManager.WorldToGridPosition(a.transform.position);
            Vector2Int bPos = gridManager.WorldToGridPosition(b.transform.position);
            GridCell aCell = gridManager.GetCell(aPos.x, aPos.y);
            GridCell bCell = gridManager.GetCell(bPos.x, bPos.y);

            if (aCell == null || bCell == null) return;

            aCell.RemoveUnit();
            bCell.RemoveUnit();

            aCell.PlaceUnit(b.gameObject);
            b.transform.position = aCell.GetWorldPosition();

            bCell.PlaceUnit(a.gameObject);
            a.transform.position = bCell.GetWorldPosition();

            GameEvents.TriggerUnitMoved(a.gameObject, aCell, bCell);
            GameEvents.TriggerUnitMoved(b.gameObject, bCell, aCell);

            Debug.Log($"Swapped {a.UnitName} with {b.UnitName}");
        }

        private static void ExecuteMoveAlly(UnitStatus caster, UnitStatus target, int tiles, BattleCard card = null)
        {
            if (caster == null) return;
            UnitStatus allyToMove = target ?? caster;
            
            ExecuteMoveAllyStep(allyToMove, tiles, card, true);
        }

        private static void ExecuteMoveAllyStep(UnitStatus allyToMove, int remainingSteps, BattleCard card, bool isFirstStep)
        {
            if (remainingSteps <= 0 || allyToMove == null) return;

            bool isPlayerAlly = allyToMove.Team == Team.Player;

            if (!HasValidAdjacentTile(allyToMove, isPlayerAlly))
            {
                Debug.Log($"{allyToMove.UnitName} has no valid adjacent tiles left. Movement ends.");
                return;
            }

            RelicTargetSelector.Instance.SelectTile(
                $"Move {allyToMove.UnitName} ({remainingSteps} steps left) — click an adjacent tile",
                (destinationCell) =>
                {
                    if (destinationCell == null) return;

                    var gridManager = ServiceLocator.Get<GridManager>();
                    if (gridManager == null) return;

                    Vector2Int allyPos = gridManager.WorldToGridPosition(allyToMove.transform.position);
                    
                    int distance = Mathf.Max(Mathf.Abs(destinationCell.XPosition - allyPos.x), Mathf.Abs(destinationCell.YPosition - allyPos.y));

                    if (distance == 1 && (!isPlayerAlly || destinationCell.IsPlayerSide))
                    {
                        if (isFirstStep && card != null) BattleDeckManager.Instance.ConsumeCard(card);

                        GridCell oldCell = gridManager.GetCell(allyPos.x, allyPos.y);
                        if (oldCell != null) oldCell.RemoveUnit();
                        destinationCell.PlaceUnit(allyToMove.gameObject);

                        allyToMove.transform.position = destinationCell.GetWorldPosition();
                        GameEvents.TriggerUnitMoved(allyToMove.gameObject, oldCell, destinationCell);

                        ExecuteMoveAllyStep(allyToMove, remainingSteps - 1, card, false);
                    }
                    else
                    {
                        Debug.Log("Invalid tile! Must be exactly 1 step away.");
                        ExecuteMoveAllyStep(allyToMove, remainingSteps, card, isFirstStep); 
                    }
                },
                () => {
                    if (isFirstStep)
                    {
                        Debug.Log("Movement cancelled.");
                    }
                    else
                    {
                        Debug.Log("Cannot cancel mid-movement! You must finish taking your steps.");
                        ExecuteMoveAllyStep(allyToMove, remainingSteps, card, false);
                    }
                },
                true, 
                1,    
                allyToMove, 
                isPlayerAlly,
                isFirstStep
            );
        }

        private static bool HasValidAdjacentTile(UnitStatus unit, bool playerSideOnly)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return false;
            
            Vector2Int pos = gridManager.WorldToGridPosition(unit.transform.position);
            Vector2Int[] dirs = { 
                new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
            };
            
            foreach(var d in dirs) {
                var cell = gridManager.GetCell(pos.x + d.x, pos.y + d.y);
                if (cell != null && !cell.IsMiddleColumn && cell.CanPlaceUnit()) {
                    if (!playerSideOnly || cell.IsPlayerSide) return true;
                }
            }
            return false;
        }
        
        private static void ExecuteMoveToNeutralZone(UnitStatus caster, GridCell targetCell)
        {
            if (targetCell == null) return;
            
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager != null && targetCell.IsMiddleColumn)
            {
                ExecuteMove(caster, targetCell, 99);
            }
        }
        
        private static void TeleportUnit(UnitStatus unit, GridCell destination)
        {
            if (unit == null || destination == null) return;
            
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return;
            
            var coords = gridManager.WorldToGridPosition(unit.transform.position);
            GridCell currentCell = gridManager.GetCell(coords.x, coords.y);
            if (currentCell != null)
            {
                currentCell.RemoveUnit();
            }
            
            destination.PlaceUnit(unit.gameObject);
            unit.transform.position = destination.GetWorldPosition();
            
            GameEvents.TriggerUnitMoved(unit.gameObject, currentCell, destination);
        }
        
        private static void PushUnit(UnitStatus target, UnitStatus source, int tiles)
        {
            if (target == null || source == null) return;
            
            var effects = target.GetComponent<StatusEffectManager>();
            if (effects != null && !effects.CanBeKnockedBack()) return;
            
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return;
            
            Vector2Int sourcePos = gridManager.WorldToGridPosition(source.transform.position);
            Vector2Int targetPos = gridManager.WorldToGridPosition(target.transform.position);
            Vector2Int direction = targetPos - sourcePos;
            
            if (direction.x != 0) direction.x = direction.x > 0 ? 1 : -1;
            if (direction.y != 0) direction.y = direction.y > 0 ? 1 : -1;
            
            Vector2Int newPos = targetPos + (direction * tiles);
            var newCell = gridManager.GetCell(newPos.x, newPos.y);
            
            if (newCell != null && newCell.CanPlaceUnit())
            {
                target.transform.position = newCell.GetWorldPosition();
            }
        }
        
        private static void KnockbackToLastColumn(UnitStatus target)
        {
            if (target == null) return;
            
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return;
            
            Vector2Int targetPos = gridManager.WorldToGridPosition(target.transform.position);
            int lastColumn = target.Team == Team.Player ? 0 : gridManager.GridWidth - 1;
            
            var newCell = gridManager.GetCell(lastColumn, targetPos.y);
            if (newCell != null && newCell.CanPlaceUnit())
            {
                target.transform.position = newCell.GetWorldPosition();
            }
        }
        
        #endregion
        
        #region Attack Helpers
        
        private static void ExecuteAttack(UnitStatus attacker, UnitStatus target)
        {
            if (attacker == null || target == null) return;
            
            var attack = attacker.GetComponent<UnitAttack>();
            if (attack != null)
            {
                bool isMelee = attacker.WeaponType == WeaponType.Melee;
                int damage = isMelee 
                    ? DamageCalculator.GetMeleeBaseDamage(attacker)
                    : DamageCalculator.GetRangedBaseDamage(attacker);
                
                target.TakeDamage(damage, attacker.gameObject, isMelee);
            }
        }
        
        private static void ExecuteAttackWithBonus(UnitStatus attacker, UnitStatus target, int flatBonus)
        {
            if (attacker == null || target == null) return;
            
            bool isMelee = attacker.WeaponType == WeaponType.Melee;
            int baseDamage = isMelee 
                ? DamageCalculator.GetMeleeBaseDamage(attacker)
                : DamageCalculator.GetRangedBaseDamage(attacker);
            
            int totalDamage = baseDamage + flatBonus;
            target.TakeDamage(totalDamage, attacker.gameObject, isMelee);
        }
        
        private static void ExecuteAttackWithPercentBonus(UnitStatus attacker, UnitStatus target, float percentBonus)
        {
            if (attacker == null || target == null) return;
            
            bool isMelee = attacker.WeaponType == WeaponType.Melee;
            int baseDamage = isMelee 
                ? DamageCalculator.GetMeleeBaseDamage(attacker)
                : DamageCalculator.GetRangedBaseDamage(attacker);
            
            int totalDamage = Mathf.RoundToInt(baseDamage * (1f + percentBonus));
            target.TakeDamage(totalDamage, attacker.gameObject, isMelee);
        }
        
        #endregion
        
        #region Status Effect Helpers
        
        private static StatusEffectManager GetStatusEffects(UnitStatus unit)
        {
            return unit?.GetComponent<StatusEffectManager>();
        }
        
        private static void ApplyDamageReduction(UnitStatus unit, float percent, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateDamageReduction(duration, percent, null));
        }
        
        private static void ApplyGritBoost(UnitStatus unit, float percent, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateGritBoost(duration, percent, null));
        }
        
        private static void ApplyAimBoost(UnitStatus unit, float percent, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateAimBoost(duration, percent, null));
        }
        
        private static void ApplyVulnerable(UnitStatus unit, float percent, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateVulnerable(duration, percent, null));
        }
        
        private static void ApplyStun(UnitStatus unit, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateStun(duration, null));
        }
        
        private static void ApplyMoraleFocus(UnitStatus unit, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateMoraleFocus(duration, null));
        }
        
        private static void ApplyReduceCardDraw(UnitStatus unit, int reduction, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateReduceCardDraw(duration, reduction, null));
        }
        
        private static void ApplyIncreaseCost(UnitStatus unit, int increase, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateIncreaseCost(duration, increase, null));
        }
        
        private static void ApplyPreventBuzzReduction(UnitStatus unit, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreatePreventBuzzReduction(duration, null));
        }
        
        private static void ApplyHealthStatReduction(UnitStatus unit, float percent, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateHealthStatBoost(duration, -percent, null));
        }
        
        private static void ApplyHealthStatBoost(UnitStatus unit, float percent, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateHealthStatBoost(duration, percent, null));
        }
        
        private static void ApplyForceTargetClosest(UnitStatus unit, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateForceTargetClosest(duration, null));
        }
        
        private static void ApplyReduceRangedCost(UnitStatus unit, int reduction)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateReduceNextRangedCost(1, reduction, null));
        }
        
        private static void ApplyFreeMove(UnitStatus unit)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateFreeMove(1, null));
        }
        
        private static void ApplyReturnDamage(UnitStatus unit, int instances, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateReturnDamage(duration, instances, null));
        }
        
        private static void ApplyEnergyOnKnockback(UnitStatus unit, int energy, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateEnergyOnKnockback(duration, energy, null));
        }
        
        private static void ApplyWeaponUseTwice(UnitStatus unit, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateWeaponUseTwice(duration, null));
        }
        
        private static void ApplyDrawOnEnemyAttack(UnitStatus unit, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateDrawOnEnemyAttack(duration, 3, 1, null));
        }
        
        private static void ApplyPreventSurrender(UnitStatus unit, float moraleRestore, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreatePreventSurrender(duration, moraleRestore, null));
        }
        
        private static void ApplyRowCantBeTargeted(UnitStatus unit, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateRowCantBeTargeted(duration, null));
        }
        
        private static void ApplyFreeStows(UnitStatus unit, int count)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateFreeStows(99, count, null));
        }
        
        private static void ApplyFreeRumUsage(UnitStatus unit, int count)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateFreeRumUsage(99, count, null));
        }
        
        private static void ApplyStunOnKnockback(UnitStatus unit, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateStunOnKnockback(duration, null));
        }
        
        private static void ApplyFullBuzz(UnitStatus unit, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateBuzzFilled(duration, null));
        }
        
        private static void ApplyCaptainDamageReflect(UnitStatus unit, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateCaptainDamageReflect(duration, null));
        }
        
        private static void ApplyReflectMoraleDamage(UnitStatus caster, int duration)
        {
            foreach (var ally in GetAllAllies(caster))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateReflectMoraleDamage(duration, null));
            }
        }
        
        #endregion
        
        #region Area Effect Helpers
        
        private static void RestoreMoraleNearby(UnitStatus caster, float percent, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                ally.RestoreMorale(Mathf.RoundToInt(ally.MaxMorale * percent));
            }
        }
        
        private static void BuffNearbyAlliesAimPower(UnitStatus caster, float percent, int duration, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                ApplyAimBoost(ally, percent, duration);
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreatePowerBoost(duration, percent, null));
            }
        }
        
        private static void ApplyMoraleDamageReductionNearby(UnitStatus caster, float percent, int duration, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateMoraleDamageReduction(duration, percent, null));
            }
        }
        
        private static void ApplyReducedRumEffectNearby(UnitStatus caster, float reduction, int duration, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateReducedRumEffect(duration, reduction, null));
            }
        }
        
        private static void ApplyEnemyBuzzOnDamage(UnitStatus caster, int duration)
        {
            foreach (var enemy in GetEnemies(caster))
            {
                var effects = GetStatusEffects(enemy);
                effects?.ApplyEffect(StatusEffect.CreateEnemyBuzzOnDamage(duration, null));
            }
        }
        
        private static void ApplyPreventDisplacementNearby(UnitStatus caster, int duration, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreatePreventDisplacement(duration, null));
            }
        }
        
        private static void ApplyOnlyLowerHPCanTargetLowest(UnitStatus caster, int duration)
        {
            var lowestHP = GetLowestHPAlly(caster);
            if (lowestHP != null)
            {
                var effects = GetStatusEffects(lowestHP);
                effects?.ApplyEffect(StatusEffect.CreateOnlyLowerHPCanTarget(duration, null));
            }
        }
        
        private static void ApplyDamageBoostToColumn(UnitStatus caster, float percent, int duration)
        {
            float actualPercent = percent >= 1f ? percent / 100f : percent;
            
            foreach (var ally in GetAlliesInColumn(caster))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateDamageBoost(duration, actualPercent, null));
            }
        }
        
        private static void ApplyRowRangedProtection(UnitStatus caster, float reduction, int duration)
        {
            foreach (var ally in GetAlliesInRow(caster))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateRangedDamageReduction(duration, reduction, null));
            }
        }
        
        private static void ApplyNoMoraleDamageNearby(UnitStatus caster, int duration, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateMoraleDamageReduction(duration, 1f, null));
            }
        }
        
        private static void StunNearbyEnemies(UnitStatus center, int duration, int range)
        {
            foreach (var enemy in GetEnemiesInRange(center, range))
            {
                ApplyStun(enemy, duration);
            }
        }
        
        private static void KnockbackNearbyEnemies(UnitStatus caster, int tiles)
        {
            foreach (var enemy in GetEnemiesInRange(caster, 1))
            {
                PushUnit(enemy, caster, tiles);
            }
        }
        
        private static void CurseEnemyRangedWeapons(UnitStatus caster, float reduction, int duration)
        {
            foreach (var enemy in GetEnemies(caster))
            {
                if (enemy.WeaponType == WeaponType.Ranged)
                {
                    var effects = GetStatusEffects(enemy);
                    effects?.ApplyEffect(StatusEffect.CreateDamageReduction(duration, reduction, null));
                }
            }
        }
        
        #endregion
        
        #region Resource Helpers
        
        private static void GenerateGrog(int amount)
        {
            var energyManager = ServiceLocator.Get<EnergyManager>();
            energyManager?.AddGrog(amount);
        }
        
        private static void ConvertGrogToEnergy(int grogAmount)
        {
            var energyManager = ServiceLocator.Get<EnergyManager>();
            if (energyManager != null && energyManager.TrySpendGrog(grogAmount))
            {
                energyManager.AddEnergy(1);
                Debug.Log($"Converted {grogAmount} grog into 1 energy!");
            }
        }
        
        private static void DrawCards(UnitStatus unit, int count)
        {
            var deckManager = BattleDeckManager.Instance;
            if (deckManager != null)
            {
                for (int i = 0; i < count; i++)
                {
                    deckManager.DrawOneCard();
                }
            }
        }
        
        private static void DrawUltimateCard(UnitStatus unit)
        {
            var deckManager = BattleDeckManager.Instance;
            if (deckManager != null)
            {
                deckManager.DrawCardByCategory(unit, RelicCategory.Ultimate);
            }
        }
        
        private static void DrawWeaponRelicCard(UnitStatus unit)
        {
            var deckManager = BattleDeckManager.Instance;
            if (deckManager != null)
            {
                deckManager.DrawCardByCategory(unit, RelicCategory.Weapon);
            }
        }
        
        private static void DrawBootsCard(UnitStatus unit)
        {
            var deckManager = BattleDeckManager.Instance;
            if (deckManager != null)
            {
                deckManager.DrawCardByCategory(unit, RelicCategory.Boots);
            }
        }
        
        private static void AddHighQualityRum(UnitStatus unit, int count)
        {
            var energyManager = ServiceLocator.Get<EnergyManager>();
            if (energyManager != null)
            {
                energyManager.AddGrog(count); 
                Debug.Log($"{unit.UnitName} summoned {count} High Quality Rum!");
            }
        }
        
        #endregion
        
        #region Summon Helpers
        
        private static void SummonCannon(UnitStatus caster, GridCell cell, int hp)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            if (hazardManager == null) return;

            GridCell spawnCell = cell;
            if (spawnCell == null || spawnCell.IsOccupied || spawnCell.HasHazard)
            {
                var cells = hazardManager.FindEmptyCellsNear(caster.transform.position, 1);
                spawnCell = cells.Count > 0 ? cells[0] : null;
            }
            if (spawnCell != null)
            {
                int baseDamage = caster.WeaponType == WeaponType.Melee 
                    ? DamageCalculator.GetMeleeBaseDamage(caster) 
                    : DamageCalculator.GetRangedBaseDamage(caster);
                    
                hazardManager.CreateCannonObstacle(spawnCell, hp, baseDamage);
            }
        }

        private static void SummonAnchor(UnitStatus caster, GridCell cell, float healthBoost, int range, int duration)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            var gridManager = ServiceLocator.Get<GridManager>();
            if (hazardManager == null || gridManager == null) return;

            GridCell spawnCell = cell;
            if (spawnCell == null || spawnCell.IsOccupied || spawnCell.HasHazard)
            {
                var cells = hazardManager.FindEmptyCellsNear(caster.transform.position, 1);
                spawnCell = cells.Count > 0 ? cells[0] : null;
            }
            
            if (spawnCell != null)
            {
                hazardManager.CreateHardObstacle(spawnCell, duration);
                
                var allies = GetAllAllies(caster);
                allies.Add(caster); 
                
                foreach (var ally in allies)
                {
                    var pos = gridManager.WorldToGridPosition(ally.transform.position);
                    int dist = Mathf.Max(Mathf.Abs(pos.x - spawnCell.XPosition), Mathf.Abs(pos.y - spawnCell.YPosition));
                    
                    if (dist <= range)
                    {
                        var effects = ally.GetComponent<StatusEffectManager>();
                        effects?.ApplyEffect(StatusEffect.CreateMaxHPBoost(duration, healthBoost, null));
                        ally.Heal(Mathf.RoundToInt(ally.MaxHP * healthBoost));
                    }
                }
                
                Debug.Log($"<color=green>{caster.UnitName} summoned Anchor: +{healthBoost*100}% Max HP for {duration} turns in {range} tile range!</color>");
            }
        }

        private static void SummonTargetDummy(UnitStatus caster, GridCell cell, int hp)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            if (hazardManager == null) return;

            GridCell spawnCell = cell;
            if (spawnCell == null || spawnCell.IsOccupied || spawnCell.HasHazard)
            {
                var cells = hazardManager.FindEmptyCellsNear(caster.transform.position, 1);
                spawnCell = cells.Count > 0 ? cells[0] : null;
            }
            if (spawnCell != null)
            {
                hazardManager.CreateSoftObstacle(spawnCell, hp, -1); 
            }
        }

        private static void SummonObstacleAndDisplace(GridCell cell, UnitStatus target)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            var gridManager = ServiceLocator.Get<GridManager>();
            if (hazardManager == null || gridManager == null) return;

            if (target != null)
            {
                Vector2Int pos = gridManager.WorldToGridPosition(target.transform.position);
                var targetCell = gridManager.GetCell(pos.x, pos.y);

                var adjacent = gridManager.GetCell(pos.x + 1, pos.y) ?? gridManager.GetCell(pos.x - 1, pos.y);

                if (adjacent != null && adjacent.CanPlaceUnit())
                {
                    if (targetCell != null) targetCell.RemoveUnit();
                    adjacent.PlaceUnit(target.gameObject);
                    target.transform.position = adjacent.GetWorldPosition();

                    if (targetCell != null)
                    {
                        hazardManager.CreateHardObstacle(targetCell, 3);
                    }
                }
            }
            else if (cell != null)
            {
                hazardManager.CreateHardObstacle(cell, 3);
            }
        }

        private static void SummonExplodingBarrels(UnitStatus caster, int count, int delay)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            if (hazardManager == null) return;

            var emptyCells = hazardManager.FindEmptyCellsNear(caster.transform.position, count, 4);
            int placed = 0;
            foreach (var cell in emptyCells)
            {
                if (placed >= count) break;
                var barrel = hazardManager.CreateExplodingBarrel(cell, 150, delay);
                if (barrel != null) placed++;
            }
        }

        private static void SummonHardObstacles(UnitStatus caster, int count, int duration)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            var gridManager = ServiceLocator.Get<GridManager>();
            if (hazardManager == null || gridManager == null) return;

            int middleCol = gridManager.GetMiddleColumnIndex();
            int placed = 0;
            
            for (int col = middleCol; col >= 0; col--)
            {
                for (int row = 0; row < gridManager.GridHeight; row++)
                {
                    if (placed >= count) return;
                    
                    var cell = gridManager.GetCell(col, row);
                    if (cell != null && !cell.IsOccupied && !cell.IsBlocked && !cell.HasHazard && !cell.IsMiddleColumn)
                    {
                        hazardManager.CreateHardObstacle(cell, duration);
                        placed++;
                    }
                }
            }
        }
        
        #endregion
        
        #region Ultimate Helpers
        
        private const int SHIP_CANNON_FIRE_DPS = 25;
        private const int SHIP_CANNON_FIRE_DURATION = 2;

        private static void ExecuteShipCannonUltimate(UnitStatus caster, int damage, int shots, BattleCard card = null)
        {
            if (caster == null || shots <= 0) return;

            var hazardManager = ServiceLocator.Get<HazardManager>();
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return;

            int middleCol = gridManager.GetMiddleColumnIndex();
            int width = gridManager.GridWidth;
            int height = gridManager.GridHeight;
            
            List<GridCell> validCells = new List<GridCell>();
            for (int x = middleCol + 1; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var cell = gridManager.GetCell(x, y);
                    if (cell != null && !cell.HasHazard)
                    {
                        validCells.Add(cell);
                    }
                }
            }

            int hits = 0;
            int shotsFired = 0;

            for (int i = 0; i < shots; i++)
            {
                if (validCells.Count == 0) break; 

                int randomIndex = UnityEngine.Random.Range(0, validCells.Count);
                GridCell targetCell = validCells[randomIndex];
                
                validCells.RemoveAt(randomIndex);

                shotsFired++;

                if (targetCell.IsOccupied && targetCell.OccupyingUnit != null)
                {
                    var unit = targetCell.OccupyingUnit.GetComponent<UnitStatus>();
                    if (unit != null && unit.CurrentHP > 0 && !unit.HasSurrendered)
                    {
                        unit.TakeEnvironmentalDamage(damage, "ShipCannon");
                        hits++;
                    }
                }

                if (hazardManager != null)
                {
                    hazardManager.CreateFireTile(targetCell, SHIP_CANNON_FIRE_DPS, SHIP_CANNON_FIRE_DURATION);
                }
            }
        }
        
        private static void ExecuteMarkCaptainOnly(UnitStatus caster, UnitStatus target)
        {
            var enemies = GetEnemies(caster);
            var captain = enemies.FirstOrDefault(e => e.IsCaptain);
            
            if (captain != null)
            {
                ExecuteAttack(caster, captain);
                var effects = GetStatusEffects(captain);
                effects?.ApplyEffect(StatusEffect.CreateOnlyTargetThisTurn(1, null));
            }
        }
        
        private static void ReviveAlly(UnitStatus caster, UnitStatus target, float healthPercent)
        {
            if (target != null && target.Team == caster.Team && (target.HasSurrendered || target.CurrentHP <= 0))
            {
                target.Heal(Mathf.RoundToInt(target.MaxHP * healthPercent));
                target.RestoreMorale(Mathf.RoundToInt(target.MaxMorale * healthPercent));
                
                target.ClearSurrender();
                return;
            }

            var allUnits = UnityEngine.Object.FindObjectsByType<UnitStatus>(UnityEngine.FindObjectsSortMode.None);
            var deadAlly = allUnits.FirstOrDefault(u => u != null && u.Team == caster.Team && (u.HasSurrendered || u.CurrentHP <= 0));
            
            if (deadAlly != null)
            {
                deadAlly.Heal(Mathf.RoundToInt(deadAlly.MaxHP * healthPercent));
                deadAlly.RestoreMorale(Mathf.RoundToInt(deadAlly.MaxMorale * healthPercent));
                
                deadAlly.ClearSurrender();
            }
            else
            {
                Debug.Log("Revive failed: No dead or surrendered allies found.");
            }
        }
        
        private static void ExecuteRumBottleAoE(UnitStatus caster, GridCell cell, int damage, int duration)
        {
            if (cell == null) return;
            var gridManager = ServiceLocator.Get<GridManager>();
            var hazardManager = ServiceLocator.Get<HazardManager>();
            if (gridManager == null) return;

            var unitsInRadius = GetAllUnits().Where(u => {
                Vector2Int pos = gridManager.WorldToGridPosition(u.transform.position);
                int dist = Mathf.Max(Mathf.Abs(pos.x - cell.XPosition), Mathf.Abs(pos.y - cell.YPosition));
                return dist <= 1;
            }).ToList();

            foreach (var unit in unitsInRadius)
            {
                unit.TakeDamage(damage, caster.gameObject, false);
            }

            if (hazardManager != null)
            {
                hazardManager.CreateRumPuddleCloud(cell, 20, duration, 1); 
            }
        }
        
        private static void ApplyIgnoreHighestHP(UnitStatus caster, int duration)
        {
            var enemies = GetEnemies(caster).Where(e => !e.IsCaptain).ToList();
            if (enemies.Count > 0)
            {
                var highestHP = enemies.OrderByDescending(e => e.CurrentHP).First();
                var effects = GetStatusEffects(highestHP);
                effects?.ApplyEffect(StatusEffect.CreateIgnoredByEnemies(duration, null));
            }
        }

        private static void ShieldAllAllies(UnitStatus caster, int amount)
        {
            foreach (var ally in GetAllAllies(caster))
            {
                ally.RestoreHull(amount);
            }
        }
        
        private static void ShieldAllAlliesPercent(UnitStatus caster, float percent)
        {
            foreach (var ally in GetAllAllies(caster))
            {
                ally.RestoreHull(Mathf.RoundToInt(ally.MaxHP * percent));
            }
            Debug.Log($"<color=green>{caster.UnitName} shielded all allies for {percent * 100}% of their Max HP!</color>");
        }
        
        #endregion
        
        #region Query Helpers
        
        private static List<UnitStatus> GetAllUnits()
        {
            return GameObject.FindGameObjectsWithTag("Unit")
                .Select(go => go.GetComponent<UnitStatus>())
                .Where(u => u != null && !u.HasSurrendered)
                .ToList();
        }
        
        private static List<UnitStatus> GetAllAllies(UnitStatus caster)
        {
            return GetAllUnits().Where(u => u.Team == caster.Team).ToList();
        }
        
        private static List<UnitStatus> GetEnemies(UnitStatus caster)
        {
            return GetAllUnits().Where(u => u.Team != caster.Team).ToList();
        }
        
        private static UnitStatus GetClosestEnemy(UnitStatus caster)
        {
            return TacticalGame.Combat.TargetFinder.FindNearestEnemy(caster);
        }
        
        private static UnitStatus GetLowestMoraleAlly(UnitStatus caster)
        {
            var validAllies = GetAllAllies(caster);
            if (validAllies.Count == 0) return null;

            return validAllies
                .Where(a => a != null && !a.HasSurrendered && a.CurrentHP > 0)
                .OrderBy(a => a.MoralePercent)
                .ThenBy(a => a.GetInstanceID()) // Tie-breaker guarantees Execution matches UI
                .FirstOrDefault();
        }
        
        private static UnitStatus GetLowestHPAlly(UnitStatus caster)
        {
            return GetAllAllies(caster)
                .Where(a => a != caster)
                .OrderBy(a => a.HPPercent)
                .FirstOrDefault();
        }
        
        private static bool IsHighestHP(UnitStatus unit)
        {
            var allies = GetAllAllies(unit);
            return !allies.Any(a => a.CurrentHP > unit.CurrentHP);
        }
        
        private static void ApplyFreeMoveToLowestMoraleAlly(UnitStatus caster)
        {
            var ally = GetLowestMoraleAlly(caster);
            if (ally != null)
            {
                ApplyFreeMove(ally);
            }
        }
        
        private static void SwapHighestLowestGritEnemies(UnitStatus caster)
        {
            var enemies = GetEnemies(caster).Where(e => !e.HasSurrendered && e.CurrentHP > 0).ToList();
            if (enemies.Count < 2) return;
            
            // Sort ascending by Grit, then by Instance ID to guarantee tie-breaking!
            var sortedEnemies = enemies.OrderBy(e => e.Grit).ThenBy(e => e.GetInstanceID()).ToList();
            
            var lowest = sortedEnemies.First(); // Truly the lowest
            var highest = sortedEnemies.Last(); // Truly the highest
            
            if (highest != lowest)
            {
                ExecuteSwapWithUnit(highest, lowest);
            }
        }
        
        private static List<UnitStatus> GetAlliesInRange(UnitStatus caster, int range)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return new List<UnitStatus>();
            
            Vector2Int casterPos = gridManager.WorldToGridPosition(caster.transform.position);
            
            return GetAllAllies(caster).Where(ally => {
                Vector2Int allyPos = gridManager.WorldToGridPosition(ally.transform.position);
                int distance = Mathf.Max(Mathf.Abs(casterPos.x - allyPos.x), Mathf.Abs(casterPos.y - allyPos.y));
                return distance <= range && ally != caster;
            }).ToList();
        }
        
        private static List<UnitStatus> GetEnemiesInRange(UnitStatus center, int range)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return new List<UnitStatus>();
            
            Vector2Int centerPos = gridManager.WorldToGridPosition(center.transform.position);
            
            return GetEnemies(center).Where(enemy => {
                Vector2Int enemyPos = gridManager.WorldToGridPosition(enemy.transform.position);
                int distance = Mathf.Max(Mathf.Abs(centerPos.x - enemyPos.x), Mathf.Abs(centerPos.y - enemyPos.y));
                return distance <= range;
            }).ToList();
        }
        
        private static List<UnitStatus> GetAlliesInColumn(UnitStatus caster)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return new List<UnitStatus>();
            
            Vector2Int casterPos = gridManager.WorldToGridPosition(caster.transform.position);
            
            return GetAllAllies(caster).Where(ally => {
                Vector2Int allyPos = gridManager.WorldToGridPosition(ally.transform.position);
                return allyPos.x == casterPos.x;
            }).ToList();
        }
        
        private static List<UnitStatus> GetAlliesInRow(UnitStatus caster)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return new List<UnitStatus>();
            
            Vector2Int casterPos = gridManager.WorldToGridPosition(caster.transform.position);
            
            return GetAllAllies(caster).Where(ally => {
                Vector2Int allyPos = gridManager.WorldToGridPosition(ally.transform.position);
                return allyPos.y == casterPos.y;
            }).ToList();
        }
        
        private static bool HasNearbyEnemies(UnitStatus center, int range)
        {
            return GetEnemiesInRange(center, range).Count > 0;
        }
        
        #endregion
        
        #region V2 Helper Methods
        
        // === Movement V2 Helpers ===
        private static void ApplyMoraleOnKillBuff(UnitStatus unit, float percent, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateMoraleOnKill(duration, percent, null));
        }
        
        private static void ApplyFreeMoveToAllAllies(UnitStatus caster)
        {
            foreach (var ally in GetAllAllies(caster))
            {
                ApplyFreeMove(ally);
            }
        }
        
        private static void ApplyBuzzReduction(UnitStatus unit, float percent, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateBuzzGainReduction(duration, percent, null));
        }
        
        private static void HealAdjacentAllies(UnitStatus caster, float percent)
        {
            foreach (var ally in GetAlliesInRange(caster, 1))
            {
                ally.Heal(Mathf.RoundToInt(ally.MaxHP * percent));
            }
        }
        
        private static void ApplyDodgeChance(UnitStatus unit, float percent, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateDodge(duration, percent, null));
        }
        
        // === Attack V2 Helpers ===
        private static void StealBuff(UnitStatus caster, UnitStatus target)
        {
            var targetEffects = GetStatusEffects(target);
            var casterEffects = GetStatusEffects(caster);
            if (targetEffects != null && casterEffects != null)
            {
                var buffs = targetEffects.GetActiveBuffs();
                if (buffs != null && buffs.Count > 0)
                {
                    var stolenBuff = buffs[UnityEngine.Random.Range(0, buffs.Count)];
                    targetEffects.RemoveEffect(stolenBuff);
                    casterEffects.ApplyEffect(stolenBuff);
                }
            }
        }
        
        private static void ForceDiscard(UnitStatus target, int count)
        {
            var deckManager = BattleDeckManager.Instance;
            if (deckManager == null) return;
            
            int discarded = deckManager.ForceDiscardFromUnit(target, count);
        }
        
        private static void ApplySlow(UnitStatus unit, int reduction, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateSlow(duration, reduction, null));
        }
        
        private static void PullUnit(UnitStatus target, UnitStatus source, int tiles)
        {
            if (target == null || source == null) return;
            
            var effects = target.GetComponent<StatusEffectManager>();
            if (effects != null && !effects.CanBeKnockedBack()) return;
            
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return;
            
            Vector2Int sourcePos = gridManager.WorldToGridPosition(source.transform.position);
            Vector2Int targetPos = gridManager.WorldToGridPosition(target.transform.position);
            Vector2Int direction = sourcePos - targetPos;
            
            if (direction.x != 0) direction.x = direction.x > 0 ? 1 : -1;
            if (direction.y != 0) direction.y = direction.y > 0 ? 1 : -1;
            
            Vector2Int newPos = targetPos + (direction * tiles);
            var newCell = gridManager.GetCell(newPos.x, newPos.y);
            
            if (newCell != null && newCell.CanPlaceUnit())
            {
                target.transform.position = newCell.GetWorldPosition();
            }
        }
        
        private static void ApplyPoison(UnitStatus unit, int damagePerTurn, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreatePoison(duration, damagePerTurn, null));
        }
        
        private static void ChainDamageToAdjacent(UnitStatus caster, UnitStatus target, float percent)
        {
            var adjacent = GetEnemiesInRange(target, 1).Where(e => e != target).FirstOrDefault();
            if (adjacent != null)
            {
                bool isMelee = caster.WeaponType == WeaponType.Melee;
                int baseDamage = isMelee 
                    ? DamageCalculator.GetMeleeBaseDamage(caster)
                    : DamageCalculator.GetRangedBaseDamage(caster);
                int chainDamage = Mathf.RoundToInt(baseDamage * percent);
                adjacent.TakeDamage(chainDamage, caster.gameObject, isMelee);
            }
        }
        
        // === Hat V2 Helpers ===
        private static void ApplyShieldBuff(UnitStatus unit, int amount)
        {
            unit.RestoreHull(amount);
        }
        
        private static void DrawBootsRelicCard(UnitStatus unit)
        {
            var deckManager = BattleDeckManager.Instance;
            if (deckManager != null)
            {
                deckManager.DrawCardByCategory(unit, RelicCategory.Boots);
            }
        }
        
        private static void RestoreMoraleToAllAllies(UnitStatus caster, float percent)
        {
            foreach (var ally in GetAllAllies(caster))
            {
                ally.RestoreMorale(Mathf.RoundToInt(ally.MaxMorale * percent));
            }
        }
        
        private static void ApplyPreventMoraleLoss(UnitStatus caster, int duration)
        {
            foreach (var ally in GetAllAllies(caster))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateMoraleDamageReduction(duration, 1f, null));
            }
        }
        
        private static void ApplyRumHealBoost(UnitStatus unit, float percent, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateRumHealBoost(duration, percent, null));
        }
        
        private static void ApplyGrogOnKill(UnitStatus unit, int amount, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateGrogOnKill(duration, amount, null));
        }
        
        private static void ApplySpeedBoost(UnitStatus unit, int amount, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateSpeedBoost(duration, amount, null));
        }
        
        private static void ApplyHealOnCardPlay(UnitStatus unit, float percent, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateHealOnCardPlay(duration, percent, null));
        }
        
        private static void ApplyFoodEffectBoost(UnitStatus unit, float multiplier, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateFoodEffectBoost(duration, multiplier, null));
        }
        
        private static void ApplyReduceAllCosts(UnitStatus unit, int reduction, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateReduceAllCosts(duration, reduction, null));
        }
        
        // === Coat V2 Helpers ===
        private static void ShieldNearbyAllies(UnitStatus caster, int amount, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                ally.RestoreHull(amount);
            }
        }
        
        private static void ApplyCounterOnAllyHit(UnitStatus unit, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateCounterOnAllyHit(duration, null));
        }
        
        private static void ApplyMoraleShield(UnitStatus caster, int amount, int duration)
        {
            foreach (var ally in GetAllAllies(caster))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateMoraleShield(duration, amount, null));
            }
        }
        
        private static void ApplyDeathPrevention(UnitStatus caster, int duration)
        {
            foreach (var ally in GetAllAllies(caster))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateDeathPrevention(duration, null));
            }
        }
        
        private static void ApplyBuzzImmunityNearby(UnitStatus caster, int duration, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateBuzzImmunity(duration, null));
            }
        }
        
        private static void ApplyThorns(UnitStatus unit, int damage, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateThorns(duration, damage, null));
        }
        
        private static void ApplyDodgeAuraNearby(UnitStatus caster, float percent, int duration, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                ApplyDodgeChance(ally, percent, duration);
            }
        }
        
        private static void ApplyHealingAuraNearby(UnitStatus caster, float percent, int duration, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateHealOverTime(duration, percent, null));
            }
        }
        
        private static void ApplyMaxHPBoostNearby(UnitStatus caster, float percent, int duration, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateMaxHPBoost(duration, percent, null));
            }
        }
        
        private static void ApplyRangedBlock(UnitStatus unit, int charges)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateRangedBlock(99, charges, null));
        }
        
        // === Totem V2 Helpers ===
        private static void SummonHealingTotem(UnitStatus caster, GridCell cell, int healPerTurn, int duration)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            if (hazardManager != null)
            {
                hazardManager.CreateHealingZone(cell, healPerTurn, duration);
            }
        }
        
        private static void ApplyWeaknessCurse(UnitStatus target, float percent, int duration)
        {
            var effects = GetStatusEffects(target);
            effects?.ApplyEffect(StatusEffect.CreateWeakness(duration, percent, null));
        }
        
        private static void ApplyDamageBoostNearby(UnitStatus caster, float percent, int duration, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateDamageBoost(duration, percent, null));
            }
        }
        
        private static void SummonMoraleBanner(UnitStatus caster, GridCell cell, int duration, int range)
        {
        }
        
        private static void SummonGrogBarrel(UnitStatus caster, GridCell cell, int grogAmount)
        {
        }
        
        private static void PlaceTrap(GridCell cell, int stunDuration)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            if (hazardManager != null)
            {
                hazardManager.CreateTrap(cell, stunDuration);
            }
        }
        
        private static void SummonShieldGenerator(UnitStatus caster, GridCell cell, int shieldPerTurn, int duration)
        {
        }
        
        private static void SummonSpeedBooster(UnitStatus caster, GridCell cell, int speedBonus, int duration)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            if (hazardManager != null)
            {
                hazardManager.CreateSpeedZone(cell, speedBonus, duration);
            }
        }
        
        private static void SummonHealingWell(UnitStatus caster, GridCell cell, float healPercent, int duration)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            if (hazardManager != null)
            {
                int healAmount = Mathf.RoundToInt(caster.MaxHP * healPercent);
                hazardManager.CreateHealingZone(cell, healAmount, duration);
            }
        }
        
        private static void CreatePoisonCloud(GridCell cell, int damagePerTurn, int duration, int range)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            if (hazardManager != null)
            {
                hazardManager.CreatePoisonCloud(cell, damagePerTurn, duration, range);
            }
        }
        
        private static void CreatePoisonTile(GridCell cell, int damagePerTurn, int duration)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            if (hazardManager != null)
            {
                hazardManager.CreatePoisonTile(cell, damagePerTurn, duration);
            }
        }
        
        private static void SummonDecoy(UnitStatus caster, GridCell cell, int duration)
        {
        }
        
        // === Ultimate V2 Helpers ===
        private static void ApplyTeamwideBuff(UnitStatus caster, float percent, int duration)
        {
            foreach (var ally in GetAllAllies(caster))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateDamageBoost(duration, percent, null));
                effects?.ApplyEffect(StatusEffect.CreateDamageReduction(duration, percent, null));
            }
        }
        
        private static void ExecuteEnemyBelowThreshold(UnitStatus caster, UnitStatus target, float threshold)
        {
            if (target != null && target.HPPercent < threshold)
            {
                target.TakeDamage(target.CurrentHP + 1, caster.gameObject, false);
            }
        }
        
        private static void FullMoraleRestoreAllAllies(UnitStatus caster)
        {
            foreach (var ally in GetAllAllies(caster))
            {
                ally.RestoreMorale(ally.MaxMorale);
            }
        }
        
        private static void MassReviveAllies(UnitStatus caster, float healthPercent)
        {
            var surrendered = GameObject.FindGameObjectsWithTag("Untagged")
                .Select(go => go.GetComponent<UnitStatus>())
                .Where(u => u != null && u.HasSurrendered && u.Team == caster.Team)
                .ToList();
            
            foreach (var ally in surrendered)
            {
                ally.Heal(Mathf.RoundToInt(ally.MaxHP * healthPercent));
                ally.RestoreMorale(Mathf.RoundToInt(ally.MaxMorale * healthPercent));
            }
        }
        
        private static void BuzzExplosionAllEnemies(UnitStatus caster)
        {
            foreach (var enemy in GetEnemies(caster))
            {
                int buzzDamage = enemy.CurrentBuzz;
                enemy.TakeDamage(buzzDamage, caster.gameObject, false);
                var effects = GetStatusEffects(enemy);
                effects?.ApplyEffect(StatusEffect.CreateBuzzFilled(1, null));
            }
        }
        
        private static void MassHealAllAllies(UnitStatus caster, float percent)
        {
            foreach (var ally in GetAllAllies(caster))
            {
                ally.Heal(Mathf.RoundToInt(ally.MaxHP * percent));
            }
        }
        
        private static void FeastAllAllies(UnitStatus caster, float healthPercent, float moralePercent)
        {
            foreach (var ally in GetAllAllies(caster))
            {
                ally.Heal(Mathf.RoundToInt(ally.MaxHP * healthPercent));
                ally.RestoreMorale(Mathf.RoundToInt(ally.MaxMorale * moralePercent));
            }
        }
        
        private static void BladeStormAllEnemies(UnitStatus caster, float percent, int range)
        {
            bool isMelee = caster.WeaponType == WeaponType.Melee;
            int baseDamage = isMelee 
                ? DamageCalculator.GetMeleeBaseDamage(caster)
                : DamageCalculator.GetRangedBaseDamage(caster);
            int damage = Mathf.RoundToInt(baseDamage * percent);
            
            foreach (var enemy in GetEnemiesInRange(caster, range))
            {
                enemy.TakeDamage(damage, caster.gameObject, isMelee);
            }
        }
        
        private static void ExecutePerfectShot(UnitStatus caster, UnitStatus target, float critMultiplier)
        {
            bool isMelee = caster.WeaponType == WeaponType.Melee;
            int baseDamage = isMelee 
                ? DamageCalculator.GetMeleeBaseDamage(caster)
                : DamageCalculator.GetRangedBaseDamage(caster);
            int critDamage = Mathf.RoundToInt(baseDamage * critMultiplier);
            
            target.TakeDamage(critDamage, caster.gameObject, isMelee);
        }
        
        #endregion
    }
}