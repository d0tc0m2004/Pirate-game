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
        
        
    private static void SwapUnits(TacticalGame.Units.UnitStatus a, TacticalGame.Units.UnitStatus b)
    {
        var grid = ServiceLocator.Get<TacticalGame.Grid.GridManager>();
        var aPos = grid.WorldToGridPosition(a.transform.position);
        var bPos = grid.WorldToGridPosition(b.transform.position);
        var aCell = grid.GetCell(aPos.x, aPos.y);
        var bCell = grid.GetCell(bPos.x, bPos.y);
        
        a.GetComponent<TacticalGame.Units.UnitMovement>()?.MoveToCell(bCell);
        b.GetComponent<TacticalGame.Units.UnitMovement>()?.MoveToCell(aCell);
    }

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
                    TacticalGame.Units.UnitStatus hat_ally = GetLowestMoraleAlly(caster);
                    if (hat_ally != null)
                    {
                        hat_ally.RestoreMorale(Mathf.RoundToInt(hat_ally.MaxMorale * effect.value2));
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
                    // Only applies to allies BEHIND the caster in the same row
                    // "Behind" = further from neutral zone (lower x for player, higher x for enemy)
                    foreach (var ally in GetAlliesBehindInRow(caster))
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
                case RelicEffectType.Trinket_V1_Captain:
                case RelicEffectType.Trinket_V2_Captain:
                case RelicEffectType.Trinket_V1_Quartermaster:
                case RelicEffectType.Trinket_V2_Quartermaster:
                case RelicEffectType.Trinket_V1_Helmsmaster:
                case RelicEffectType.Trinket_V2_Helmsmaster:
                case RelicEffectType.Trinket_V1_Boatswain:
                case RelicEffectType.Trinket_V2_Boatswain:
                case RelicEffectType.Trinket_V1_Shipwright:
                case RelicEffectType.Trinket_V2_Shipwright:
                case RelicEffectType.Trinket_V1_MasterGunner:
                case RelicEffectType.Trinket_V2_MasterGunner:
                    Debug.Log($"<color=gray>Passive trinket {effectType} - handled by PassiveRelicManager</color>");
                    break;

                // ==================== TOTEM ====================
                case RelicEffectType.Totem_V1_Captain:
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
                    
                case RelicEffectType.Totem_V2_Captain:
                    {
                        var enemyCaptain = GetEnemies(caster).FirstOrDefault(e => e.IsCaptain);
                        if (enemyCaptain != null)
                        {
                            ApplyCaptainDamageReflect(enemyCaptain, effect.duration);
                        }
                    }
                    break;
                    
                case RelicEffectType.Totem_V1_Quartermaster:
                    ApplyNoMoraleDamageNearby(caster, effect.duration, effect.tileRange);
                    break;
                    
                case RelicEffectType.Totem_V2_Quartermaster:
                    Debug.Log($"<color=gray>Passive totem {effectType} - handled by PassiveRelicManager</color>");
                    break;
                    
                case RelicEffectType.Totem_V1_Helmsmaster:
                    AddHighQualityRum(caster, (int)effect.value1);
                    break;
                    
                case RelicEffectType.Totem_V2_Helmsmaster:
                    ConvertGrogToEnergy((int)effect.value1);
                    break;
                    
                case RelicEffectType.Totem_V1_Boatswain:
                    ApplyStunOnKnockback(caster, effect.duration);
                    break;
                    
                case RelicEffectType.Totem_V2_Boatswain:
                    SummonAnchor(caster, targetCell, effect.value2, effect.tileRange, effect.duration);
                    break;
                    
                case RelicEffectType.Totem_V1_Shipwright:
                    SummonTargetDummy(caster, targetCell, (int)effect.value1);
                    break;
                    
                case RelicEffectType.Totem_V2_Shipwright:
                    SummonObstacleAndDisplace(targetCell, target);
                    break;
                    
                case RelicEffectType.Totem_V1_MasterGunner:
                    SummonExplodingBarrels(caster, (int)effect.value1, effect.duration);
                    break;
                    
                case RelicEffectType.Totem_V2_MasterGunner:
                    CurseEnemyRangedWeapons(caster, effect.value2, effect.duration);
                    break;

                // ==================== ULTIMATE ====================
                case RelicEffectType.Ultimate_V1_Captain:
                    ExecuteShipCannonUltimate(caster, (int)effect.value1, (int)effect.value2);
                    break;
                    
                case RelicEffectType.Ultimate_V2_Captain:
                    ExecuteMarkCaptainOnly(caster, target);
                    break;
                    
                case RelicEffectType.Ultimate_V1_Quartermaster:
                    ApplyReflectMoraleDamage(caster, effect.duration);
                    break;
                    
                case RelicEffectType.Ultimate_V2_Quartermaster:
                    ReviveAlly(caster, target, effect.value2);
                    break;
                    
                case RelicEffectType.Ultimate_V1_Helmsmaster:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        ApplyFullBuzz(target, effect.duration);
                    }
                    break;
                    
                case RelicEffectType.Ultimate_V2_Helmsmaster:
                    ExecuteRumBottleAoE(caster, targetCell, (int)effect.value1, effect.duration);
                    break;
                    
                case RelicEffectType.Ultimate_V1_Boatswain:
                    SummonHardObstacles(caster, (int)effect.value1, effect.duration);
                    break;
                    
                case RelicEffectType.Ultimate_V2_Boatswain:
                    ApplyIgnoreHighestHP(caster, effect.duration);
                    break;
                    
                case RelicEffectType.Ultimate_V1_Shipwright:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        KnockbackToLastColumn(target);
                    }
                    break;
                    
                case RelicEffectType.Ultimate_V2_Shipwright:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        KnockbackNearbyEnemies(caster, 1);
                    }
                    break;
                    
                case RelicEffectType.Ultimate_V1_MasterGunner:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        ApplyStun(target, effect.duration);
                        StunNearbyEnemies(caster, target, effect.duration, 1);
                    }
                    break;
                    
                case RelicEffectType.Ultimate_V2_MasterGunner:
                    if (target != null)
                    {
                        bool hasNearbyEnemies = HasNearbyEnemies(target, 1);
                        float bonus = hasNearbyEnemies ? 0f : effect.value2;
                        ExecuteAttackWithPercentBonus(caster, target, bonus);
                    }
                    break;

                // ==================== PASSIVE UNIQUE ====================
                case RelicEffectType.PassiveUnique_V1_Captain:
                case RelicEffectType.PassiveUnique_V1_Quartermaster:
                case RelicEffectType.PassiveUnique_DeathStrikeByMorale:
                case RelicEffectType.PassiveUnique_V2_Quartermaster:
                case RelicEffectType.PassiveUnique_V1_Helmsmaster:
                case RelicEffectType.PassiveUnique_DrawPerGrog:
                case RelicEffectType.PassiveUnique_DrawOnLowDamage:
                case RelicEffectType.PassiveUnique_V1_MasterAtArms:
                case RelicEffectType.PassiveUnique_V1_Shipwright:
                case RelicEffectType.PassiveUnique_V2_Shipwright:
                case RelicEffectType.PassiveUnique_V1_MasterGunner:
                case RelicEffectType.PassiveUnique_V1_Deckhand:
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
                case RelicEffectType.PassiveUnique_V2_Captain:
                case RelicEffectType.PassiveUnique_V2_CardMaster:
                case RelicEffectType.PassiveUnique_V2_Inspiring:
                case RelicEffectType.PassiveUnique_V2_LastStand:
                case RelicEffectType.PassiveUnique_V2_Helmsmaster:
                case RelicEffectType.PassiveUnique_V2_Cook:
                case RelicEffectType.PassiveUnique_V2_Boatswain:
                case RelicEffectType.PassiveUnique_V2_Scout:
                case RelicEffectType.PassiveUnique_V2_Surgeon:
                case RelicEffectType.PassiveUnique_V2_Nourishing:
                case RelicEffectType.PassiveUnique_V2_MasterAtArms:
                case RelicEffectType.PassiveUnique_V2_MasterGunner:
                    Debug.Log($"<color=gray>Passive unique V2 {effectType} - handled by PassiveRelicManager</color>");
                    break;
                    
                // ==================== SURGEON SPECIFIC ====================
                case RelicEffectType.Boots_MoveRestoreHealth:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    caster.Heal(Mathf.RoundToInt(caster.MaxHP * effect.value2));
                    break;
                case RelicEffectType.Boots_V2_SwapLowestHealthAlly:
                    {
                        // 1. Get all allies
                        var validAllies = GetAllAllies(caster)
                            .Where(a => a != null && a != caster && !a.HasSurrendered && a.CurrentHP > 0) // EXPLICITLY ignore the caster!
                            .ToList();

                        // 2. Sort by lowest health percentage
                        var lowestAlly = validAllies
                            .OrderBy(a => a.HPPercent)
                            .ThenBy(a => a.GetInstanceID()) // Guarantee it picks the exact same one the UI highlighted
                            .FirstOrDefault();

                        if (lowestAlly != null)
                        {
                            ExecuteSwapWithUnit(caster, lowestAlly);
                            Debug.Log($"<color=yellow>{caster.UnitName} swapped places with {lowestAlly.UnitName} (Lowest HP)!</color>");
                        }
                        else
                        {
                            Debug.LogWarning("No valid other allies alive to swap with!");
                        }
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
                    DrawTrinketCard(caster);
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
                        effects2?.ApplyEffect(StatusEffect.CreateDamageBoost(effect.duration, 1.0f, null));
                        effects2?.ApplyEffect(StatusEffect.CreateAimBoost(effect.duration, 1.0f, null));
                        Debug.Log($"<color=cyan>{target.UnitName} stats doubled!</color>");
                    }
                    break;
                case RelicEffectType.Coat_V2_KnockbackOnAllyDeath:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Trinket_V1_Surgeon:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Trinket_V2_Surgeon:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Totem_V1_Surgeon:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Totem_V2_Surgeon:
                    {
                        var gridManager = ServiceLocator.Get<GridManager>();
                        var hazardManager = ServiceLocator.Get<HazardManager>();
                        if (gridManager != null && hazardManager != null)
                        {
                            int middleCol = gridManager.GetMiddleColumnIndex(); 
                            
                            int placed = 0;
                            for (int attempt = 0; attempt < 50 && placed < 3; attempt++) 
                            {
                                int x = UnityEngine.Random.Range(0, middleCol); 
                                int y = UnityEngine.Random.Range(0, gridManager.GridHeight);
                                var cell = gridManager.GetCell(x, y);
                                
                                // FIXED: CanPlaceUnit() guarantees it is a completely empty, walkable floor tile!
                                if (cell != null && cell.CanPlaceUnit() && !cell.HasHazard)
                                {
                                    hazardManager.CreateHealingZone(cell, 200, 99); 
                                    placed++;
                                }
                            }
                            Debug.Log($"<color=green>{caster.UnitName} dropped {placed} Healing Potions on empty Player tiles!</color>");
                        }
                    }
                    break;
                case RelicEffectType.Ultimate_V1_Surgeon:
                    if (target != null)
                    {
                        ApplyDeathPrevention(target, effect.duration);
                    }
                    break;
                case RelicEffectType.Ultimate_V2_Surgeon:
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
                    if (BattleDeckManager.Instance != null) {
                        int beforeCount = BattleDeckManager.Instance.Hand.Count;
                        DrawCards(caster, 1);
                        if (BattleDeckManager.Instance.Hand.Count > beforeCount) {
                            var drawnCard = BattleDeckManager.Instance.Hand.Last();
                            // Only reduce cost if drawn card is a Cook relic card
                            if (drawnCard != null && drawnCard.roleTag == UnitRole.Cook) {
                                drawnCard.originalEnergyCost = drawnCard.energyCost; // Save for revert
                                drawnCard.energyCost = Mathf.Max(0, drawnCard.energyCost - (int)effect.value2);
                                Debug.Log($"<color=green>Cook Boots V1: Reduced {drawnCard.cardName} cost to {drawnCard.energyCost} (was {drawnCard.originalEnergyCost})</color>");
                            }
                        }
                    }
                    break;
                case RelicEffectType.Boots_V2_MoveBoostProficiency:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    {
                        var effects3 = GetStatusEffects(caster);
                        effects3?.ApplyEffect(StatusEffect.CreateProficiencyBoost(1, effect.value2, null));
                    }
                    break;
                case RelicEffectType.Gloves_AttackDetonateBuff:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        var effects = GetStatusEffects(target);
                        effects?.ApplyEffect(StatusEffect.CreateCookDetonateBuff(effect.duration, 200f, caster.gameObject));
                    }
                    break;
                case RelicEffectType.Gloves_V2_StasisClosest:
                    {
                        var allUnits = TargetFinder.GetAllUnits(false);
                        UnitStatus closestUnit = null;
                        float minDistance = float.MaxValue;
                        foreach (var u in allUnits) {
                            if (u == caster) continue;
                            float dist = Vector3.Distance(caster.transform.position, u.transform.position);
                            if (dist < minDistance) {
                                minDistance = dist;
                                closestUnit = u;
                            }
                        }
                        if (closestUnit != null)
                        {
                            var effects = GetStatusEffects(closestUnit);
                            effects?.ApplyEffect(StatusEffect.CreateStasis(effect.duration, null));
                        }
                    }
                    break;
                case RelicEffectType.Hat_ReduceLowestAllyCardCost:
                    {
                        // Find the lowest HP ally (excluding caster)
                        var lowestAlly3 = GetAllAllies(caster)
                            .Where(a => a != caster)
                            .OrderBy(a => a.HPPercent)
                            .ThenBy(a => a.gameObject.GetInstanceID())
                            .FirstOrDefault();
                        if (lowestAlly3 != null && BattleDeckManager.Instance != null)
                        {
                            // Directly reduce the cost of this ally's cards in hand
                            int reduction = (int)effect.value1;
                            foreach (var handCard in BattleDeckManager.Instance.Hand)
                            {
                                if (handCard.BelongsTo(lowestAlly3))
                                {
                                    if (handCard.originalEnergyCost < 0)
                                        handCard.originalEnergyCost = handCard.energyCost; // Save for revert
                                    handCard.energyCost = Mathf.Max(0, handCard.energyCost - reduction);
                                }
                            }
                            Debug.Log($"<color=green>Cook Hat V1: Reduced all {lowestAlly3.UnitName}'s card costs by {reduction} this turn</color>");
                        }
                    }
                    break;
                case RelicEffectType.Hat_V2_MoveForwardHeal:
                    if (target != null)
                    {
                        var gridManager = ServiceLocator.Get<GridManager>();
                        if (gridManager != null) {
                            int dir = target.Team == Team.Player ? 1 : -1;
                            var pos = gridManager.WorldToGridPosition(target.transform.position);
                            var nextCell = gridManager.GetCell(pos.x + dir, pos.y);
                            if (nextCell != null && nextCell.CanPlaceUnit()) {
                                ExecuteMove(target, nextCell, 1);
                            }
                        }
                        target.Heal(Mathf.RoundToInt(target.MaxHP * effect.value1));
                    }
                    break;
                case RelicEffectType.Coat_StunOnAllyAttacked:
                    {
                        var allAllies = TargetFinder.GetAllAllies(caster.Team, false, caster);
                        UnitStatus closestAlly = null;
                        float minDistance = float.MaxValue;
                        foreach (var u in allAllies) {
                            float dist = Vector3.Distance(caster.transform.position, u.transform.position);
                            if (dist < minDistance) {
                                minDistance = dist;
                                closestAlly = u;
                            }
                        }
                        if (closestAlly != null)
                        {
                            var effects = GetStatusEffects(closestAlly);
                            effects?.ApplyEffect(StatusEffect.CreateStunAttackerOnHit(effect.duration, null));
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
                case RelicEffectType.Trinket_V1_Cook:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Trinket_V2_Cook:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Totem_V1_Cook:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Totem_V2_Cook:
                    {
                        var hazardManager = ServiceLocator.Get<HazardManager>();
                        var gridManager = ServiceLocator.Get<GridManager>();
                        if (hazardManager != null && gridManager != null) {
                            // Always spawn on the enemy side
                            GridCell spawnCell = null;
                            int middleCol = gridManager.GetMiddleColumnIndex();
                            for (int attempt = 0; attempt < 50; attempt++)
                            {
                                int x, y;
                                if (caster.Team == Team.Player)
                                {
                                    x = Random.Range(middleCol + 1, gridManager.GridWidth);
                                }
                                else
                                {
                                    x = Random.Range(0, middleCol);
                                }
                                y = Random.Range(0, gridManager.GridHeight);
                                var cell = gridManager.GetCell(x, y);
                                if (cell != null && !cell.IsOccupied && !cell.HasHazard && !cell.IsMiddleColumn)
                                {
                                    spawnCell = cell;
                                    break;
                                }
                            }
                            
                            if (spawnCell != null) {
                                hazardManager.CreateSoftObstacle(spawnCell, 50, effect.duration);
                                Debug.Log($"<color=cyan>Cook Totem V2: Spawned debuff obstacle on ENEMY side at ({spawnCell.XPosition},{spawnCell.YPosition})</color>");
                                // Debuff nearby enemies
                                var enemies = TargetFinder.GetAllEnemies(caster.Team);
                                foreach (var enemy in enemies) {
                                    var ePos = gridManager.WorldToGridPosition(enemy.transform.position);
                                    if (Mathf.Abs(ePos.x - spawnCell.XPosition) <= 1 && Mathf.Abs(ePos.y - spawnCell.YPosition) <= 1) {
                                        var effects = GetStatusEffects(enemy);
                                        effects?.ApplyEffect(StatusEffect.CreateWeakness(effect.duration, 0.5f, null));
                                        effects?.ApplyEffect(StatusEffect.CreateHealthStatReduction(effect.duration, 0.5f, null));
                                    }
                                }
                            }
                        }
                    }
                    break;
                case RelicEffectType.Ultimate_V1_Cook:
                    {
                        var closestEnemy = GetClosestEnemy(caster);
                        if (closestEnemy != null)
                        {
                            int casterHP = caster.CurrentHP;
                            int enemyHP = closestEnemy.CurrentHP;
                            
                            caster.SetHP(Mathf.Min(enemyHP, caster.MaxHP));
                            closestEnemy.SetHP(Mathf.Min(casterHP, closestEnemy.MaxHP));
                        }
                    }
                    break;
                case RelicEffectType.Ultimate_V2_Cook:
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
                case RelicEffectType.PassiveUnique_V1_Cook:
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
                        var targetEffects = GetStatusEffects(target);
                        if (targetEffects != null)
                        {
                            targetEffects.ApplyEffect(StatusEffect.CreateStunOnMoveTracker(effect.duration, caster.gameObject));
                        }
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
                    {
                        var gridManager2 = ServiceLocator.Get<GridManager>();
                        var hazardManager2 = ServiceLocator.Get<HazardManager>();
                        if (gridManager2 != null && hazardManager2 != null)
                        {
                            GridCell validCell = null;
                            for (int attempt = 0; attempt < 50; attempt++)
                            {
                                int x = Random.Range(0, gridManager2.GridWidth);
                                int y = Random.Range(0, gridManager2.GridHeight);
                                var cell = gridManager2.GetCell(x, y);
                                bool targetIsPlayerSide = caster.Team == Team.Enemy; // Target opposite team's side
                                if (cell != null && !cell.HasHazard && !cell.IsMiddleColumn && cell.IsPlayerSide == targetIsPlayerSide)
                                {
                                    validCell = cell;
                                    break;
                                }
                            }
                            if (validCell != null)
                            {
                                hazardManager2.CreateCursedTile(validCell, effect.duration);
                            }
                        }
                    }
                    break;
                case RelicEffectType.Trinket_V1_Swashbuckler:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Trinket_V2_Swashbuckler:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Totem_V1_Swashbuckler:
                    {
                        var gridManager2 = ServiceLocator.Get<GridManager>();
                        var hazardManager3 = ServiceLocator.Get<HazardManager>();
                        if (gridManager2 != null && hazardManager3 != null)
                        {
                            int placed = 0;
                            for (int attempt = 0; attempt < 50 && placed < (int)effect.value1; attempt++)
                            {
                                int x = Random.Range(0, gridManager2.GridWidth);
                                int y = Random.Range(0, gridManager2.GridHeight);
                                var cell = gridManager2.GetCell(x, y);
                                bool targetIsPlayerSide = caster.Team == Team.Enemy; // Target opposite team's side
                                if (cell != null && !cell.IsOccupied && !cell.IsMiddleColumn && !cell.HasHazard && cell.IsPlayerSide == targetIsPlayerSide)
                                {
                                    hazardManager3.CreateInvisibleTrap(cell, effect.duration);
                                    placed++;
                                }
                            }
                        }
                    }
                    break;
                case RelicEffectType.Totem_V2_Swashbuckler:
                    {
                        var enemies = GetEnemies(caster);
                        foreach (var enemy in enemies)
                        {
                            var targetEffects = GetStatusEffects(enemy);
                            if (targetEffects != null)
                            {
                                targetEffects.ApplyEffect(StatusEffect.CreatePassivesDisabled(effect.duration, caster.gameObject));
                            }
                        }
                    }
                    break;
                case RelicEffectType.Ultimate_V1_Swashbuckler:
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
                case RelicEffectType.Ultimate_V2_Swashbuckler:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.PassiveUnique_EnemyDiscardOnBoot:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.PassiveUnique_V2_Swashbuckler:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;

                // ==================== DECKHAND SPECIFIC ====================
                case RelicEffectType.Boots_MoveColumnOnly:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    break;
                case RelicEffectType.Boots_V2_MoveRestoreHull:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    caster.RestoreHull((int)effect.value2);
                    Debug.Log($"{caster.UnitName} restored {(int)effect.value2} hull");
                    break;
                case RelicEffectType.Gloves_AttackDrawOnHullDestroyed:
                    if (target != null)
                    {
                        int hullBefore = target.CurrentHullPool;
                        ExecuteAttack(caster, target);
                        if (target.CurrentHullPool <= 0 && hullBefore > 0)
                        {
                            DrawCards(caster, 1);
                            Debug.Log($"<color=cyan>Hull destroyed! Drawing 1 card</color>");
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
                            Debug.Log($"<color=cyan>Hull destroyed! Gained 1 energy</color>");
                        }
                    }
                    break;
                case RelicEffectType.Hat_NearbyHullIncrease:
                    {
                        // Increase hull for self AND nearby allies in 1 tile radius
                        var nearbyAllies3 = GetAlliesInRange(caster, effect.tileRange);
                        nearbyAllies3.Add(caster); // Include self
                        foreach (var ally in nearbyAllies3)
                        {
                            int hullBoost = Mathf.RoundToInt(ally.MaxHullPool * effect.value1);
                            ally.RestoreHull(hullBoost);
                            Debug.Log($"<color=cyan>{ally.UnitName} hull increased by {hullBoost} (+{effect.value1 * 100}%)</color>");
                        }
                    }
                    break;
                case RelicEffectType.Hat_V2_DestroyObstaclesGainHull:
                    {
                        // Destroy ALL soft obstacles, gain +20% hull per obstacle
                        var hazardMgr = ServiceLocator.Get<HazardManager>();
                        if (hazardMgr != null)
                        {
                            int destroyed = hazardMgr.DestroyAllSoftObstacles();
                            if (destroyed > 0)
                            {
                                int hullGain = Mathf.RoundToInt(caster.MaxHullPool * effect.value2 * destroyed);
                                caster.RestoreHull(hullGain);
                                Debug.Log($"<color=cyan>{caster.UnitName} destroyed {destroyed} obstacles, gained {hullGain} hull</color>");
                            }
                            else
                            {
                                Debug.Log($"<color=yellow>No soft obstacles to destroy</color>");
                            }
                        }
                    }
                    break;
                case RelicEffectType.Coat_HullBonusDamage:
                    {
                        // Bonus weapon damage = 50% of hull shield for self and nearby allies
                        float hullDmgBonus = caster.CurrentHullPool * effect.value2; // value2 = 0.50
                        var nearbyAllies4 = GetAlliesInRange(caster, effect.tileRange);
                        nearbyAllies4.Add(caster);
                        foreach (var ally in nearbyAllies4)
                        {
                            var effects5 = GetStatusEffects(ally);
                            // Apply as flat damage bonus via DamageBoost (value represents flat bonus / base damage)
                            float bonusPercent = ally.MaxHullPool > 0 ? hullDmgBonus / 100f : 0f;
                            effects5?.ApplyEffect(StatusEffect.CreateDamageBoost(effect.duration, bonusPercent, caster.gameObject));
                            Debug.Log($"<color=cyan>{ally.UnitName} gained +{hullDmgBonus:F0} weapon damage from hull</color>");
                        }
                    }
                    break;
                case RelicEffectType.Coat_V2_BuffTileDamageExchange:
                    {
                        // Buff a random occupied tile - unit takes 15% more damage and does 15% more
                        var gridMgr = ServiceLocator.Get<GridManager>();
                        if (gridMgr != null)
                        {
                            var occupiedCells = new List<GridCell>();
                            for (int x = 0; x < gridMgr.GridWidth; x++)
                            {
                                for (int y = 0; y < gridMgr.GridHeight; y++)
                                {
                                    var c = gridMgr.GetCell(x, y);
                                    if (c != null && c.IsOccupied && c.OccupyingUnit != null && !c.IsMiddleColumn)
                                        occupiedCells.Add(c);
                                }
                            }
                            if (occupiedCells.Count > 0)
                            {
                                var randomCell = occupiedCells[Random.Range(0, occupiedCells.Count)];
                                var unit = randomCell.OccupyingUnit.GetComponent<UnitStatus>();
                                if (unit != null)
                                {
                                    ApplyVulnerable(unit, effect.value1, effect.duration);
                                    var effects6 = GetStatusEffects(unit);
                                    effects6?.ApplyEffect(StatusEffect.CreateDamageBoost(effect.duration, effect.value2, null));
                                    
                                    // Paint the tile green permanently so the player can see it
                                    var cellRenderer = randomCell.GetComponent<Renderer>();
                                    if (cellRenderer != null)
                                        cellRenderer.material.color = new Color(0.2f, 0.85f, 0.3f, 1f); // Bright green
                                    
                                    Debug.Log($"<color=cyan>Buffed tile at ({randomCell.XPosition},{randomCell.YPosition}): {unit.UnitName} takes +{effect.value1 * 100}% dmg, deals +{effect.value2 * 100}% dmg</color>");
                                }
                            }
                        }
                    }
                    break;
                case RelicEffectType.Trinket_V1_Deckhand:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Trinket_V2_Deckhand:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Totem_V1_Deckhand:
                    {
                        // Create 2 soft obstacles in random empty tiles (auto-cast)
                        var hazardMgr2 = ServiceLocator.Get<HazardManager>();
                        var gridMgr2 = ServiceLocator.Get<GridManager>();
                        if (hazardMgr2 != null && gridMgr2 != null)
                        {
                            int toPlace = (int)effect.value1; // 2
                            int placed = 0;
                            for (int attempt = 0; attempt < 30 && placed < toPlace; attempt++)
                            {
                                int x = Random.Range(0, gridMgr2.GridWidth);
                                int y = Random.Range(0, gridMgr2.GridHeight);
                                var cell = gridMgr2.GetCell(x, y);
                                if (cell != null && !cell.IsOccupied && !cell.HasHazard && !cell.IsMiddleColumn)
                                {
                                    var obstacle = hazardMgr2.CreateSoftObstacle(cell, 50, 99);
                                    if (obstacle != null)
                                    {
                                        placed++;
                                        Debug.Log($"<color=cyan>Created soft obstacle at ({x},{y})</color>");
                                    }
                                }
                            }
                            Debug.Log($"<color=cyan>Created {placed}/{toPlace} soft obstacles</color>");
                        }
                    }
                    break;
                case RelicEffectType.Totem_V2_Deckhand:
                    {
                        // Player selected an enemy-side tile — spawn a cube there and pull nearby enemies to its row
                        var gridMgrT = ServiceLocator.Get<GridManager>();
                        if (gridMgrT != null && targetCell != null)
                        {
                            // 1. Spawn a cube (soft obstacle) at the selected tile
                            var hazardMgrT = ServiceLocator.Get<HazardManager>();
                            if (hazardMgrT != null)
                            {
                                hazardMgrT.CreateSoftObstacle(targetCell, 80, 99);
                                Debug.Log($"<color=cyan>Deckhand Totem V2 placed at ({targetCell.XPosition},{targetCell.YPosition})</color>");
                            }
                            
                            // 2. Find all enemies within 1-tile radius of the totem
                            int totemX = targetCell.XPosition;
                            int totemY = targetCell.YPosition;
                            var allEnemies = Object.FindObjectsByType<UnitStatus>(FindObjectsSortMode.None);
                            var nearbyEnemies = new System.Collections.Generic.List<UnitStatus>();
                            
                            foreach (var enemy in allEnemies)
                            {
                                if (enemy == null || enemy.Team == caster.Team || enemy.HasSurrendered || enemy.CurrentHP <= 0) continue;
                                Vector2Int ePos = gridMgrT.WorldToGridPosition(enemy.transform.position);
                                int dist = Mathf.Max(Mathf.Abs(ePos.x - totemX), Mathf.Abs(ePos.y - totemY)); // Chebyshev
                                if (dist <= 1 && dist > 0)
                                    nearbyEnemies.Add(enemy);
                            }
                            
                            // 3. Pull each nearby enemy to the totem's row
                            foreach (var enemy in nearbyEnemies)
                            {
                                Vector2Int ePos = gridMgrT.WorldToGridPosition(enemy.transform.position);
                                if (ePos.y == totemY) continue; // Already on same row
                                
                                // Try same column, totem's row first
                                GridCell dest = gridMgrT.GetCell(ePos.x, totemY);
                                if (dest == null || !dest.CanPlaceUnit() || dest.IsMiddleColumn)
                                {
                                    // Fallback: find nearest empty tile to the totem
                                    dest = null;
                                    int bestDist = int.MaxValue;
                                    for (int sx = 0; sx < gridMgrT.GridWidth; sx++)
                                    {
                                        for (int sy = 0; sy < gridMgrT.GridHeight; sy++)
                                        {
                                            var candidate = gridMgrT.GetCell(sx, sy);
                                            if (candidate == null || !candidate.CanPlaceUnit() || candidate.IsMiddleColumn) continue;
                                            int d = Mathf.Abs(sx - totemX) + Mathf.Abs(sy - totemY);
                                            if (d < bestDist)
                                            {
                                                bestDist = d;
                                                dest = candidate;
                                            }
                                        }
                                    }
                                }
                                
                                if (dest != null)
                                {
                                    var fromCell = gridMgrT.GetCell(ePos.x, ePos.y);
                                    fromCell?.RemoveUnit();
                                    dest.PlaceUnit(enemy.gameObject);
                                    enemy.transform.position = dest.GetWorldPosition();
                                    Debug.Log($"<color=cyan>Pulled {enemy.UnitName} to ({dest.XPosition},{dest.YPosition})</color>");
                                }
                            }
                            
                            Debug.Log($"<color=cyan>Totem V2: Pulled {nearbyEnemies.Count} enemies toward row {totemY}</color>");
                        }
                        else
                        {
                            Debug.LogWarning("Totem V2: No target cell provided!");
                        }
                    }
                    break;
                case RelicEffectType.Ultimate_V1_Deckhand:
                    if (target != null)
                    {
                        // Give target 300% hull for 2 turns (value2 = 3.0)
                        int hullAmount = Mathf.RoundToInt(target.MaxHullPool * effect.value2);
                        target.RestoreHull(hullAmount);
                        Debug.Log($"<color=cyan>{target.UnitName} gained {hullAmount} hull ({effect.value2 * 100}%)</color>");
                    }
                    break;
                case RelicEffectType.Ultimate_V2_Deckhand:
                    {
                        // Clear all hazards on player side AND prevent new ones
                        var hazardMgr3 = ServiceLocator.Get<HazardManager>();
                        var gridMgr4 = ServiceLocator.Get<GridManager>();
                        if (hazardMgr3 != null && gridMgr4 != null)
                        {
                            for (int x = 0; x < gridMgr4.GridWidth; x++)
                            {
                                for (int y = 0; y < gridMgr4.GridHeight; y++)
                                {
                                    if (gridMgr4.IsPlayerSide(x))
                                    {
                                        var cell = gridMgr4.GetCell(x, y);
                                        if (cell != null)
                                            hazardMgr3.ClearHazard(cell);
                                    }
                                }
                            }
                            // Prevent new hazards on player side
                            hazardMgr3.SetPreventPlayerSideHazards(true);
                            Debug.Log($"<color=cyan>Cleared all player-side hazards and preventing new ones</color>");
                        }
                    }
                    break;
                case RelicEffectType.PassiveUnique_HullDestroyedRestoreHealth:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.PassiveUnique_V2_Deckhand:
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
                    {
                        // Auto-cast: disable weapon role effects on ALL enemies
                        foreach (var enemy in GetEnemies(caster))
                        {
                            var fx = GetStatusEffects(enemy);
                            fx?.ApplyEffect(StatusEffect.CreateWeakness(effect.duration, 0.5f, null));
                        }
                        Debug.Log($"<color=cyan>Navigator Gloves V1: Disabled weapon effects on all enemies for {effect.duration} turns</color>");
                    }
                    break;
                case RelicEffectType.Gloves_V2_AttackBonusPerBootsCard:
                    if (target != null)
                    {
                        int bootsCount = 0;
                        if (BattleDeckManager.Instance != null)
                        {
                            var dm = BattleDeckManager.Instance;
                            bootsCount = dm.Deck.Count(c => c.category == RelicCategory.Boots)
                                       + dm.Hand.Count(c => c.category == RelicCategory.Boots)
                                       + dm.DiscardPile.Count(c => c.category == RelicCategory.Boots);
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
                case RelicEffectType.Trinket_V1_Navigator:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Trinket_V2_Navigator:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Totem_V1_Navigator:
                    {
                        var enemies4 = GetEnemies(caster);
                        foreach (var enemy in enemies4)
                        {
                            ApplySlow(enemy, 99, effect.duration); 
                        }
                    }
                    break;
                case RelicEffectType.Totem_V2_Navigator:
                    {
                        foreach (var enemy in GetEnemies(caster))
                        {
                            var fx = GetStatusEffects(enemy);
                            fx?.ApplyEffect(StatusEffect.CreateDisableNonWeaponRelics(effect.duration, null));
                        }
                        Debug.Log($"<color=cyan>Navigator Totem V2: Disabled non-weapon relics on all enemies for {effect.duration} turns</color>");
                    }
                    break;
                case RelicEffectType.Ultimate_V1_Navigator:
                    if (target != null)
                    {
                        // Mark target: any damage taken is also reflected to their captain
                        var markEffect = StatusEffect.CreateMarked(effect.duration, 0f, caster.gameObject);
                        markEffect.value2 = 1f; // Flag: reflect damage to captain
                        var targetFx = GetStatusEffects(target);
                        targetFx?.ApplyEffect(markEffect);
                        Debug.Log($"<color=magenta>Navigator Ult V1: Marked {target.UnitName} — damage will reflect to their captain!</color>");
                    }
                    break;
                case RelicEffectType.Ultimate_V2_Navigator:
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
                case RelicEffectType.PassiveUnique_V1_Navigator:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.PassiveUnique_V2_Navigator:
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
                    // Destroy obstacle on target tile BEFORE moving so tile is accessible
                    if (targetCell != null && (targetCell.HasHazard || targetCell.IsBlocked))
                    {
                        var hazMgr = ServiceLocator.Get<HazardManager>();
                        if (hazMgr != null)
                        {
                            hazMgr.ClearHazard(targetCell);
                        }
                        // Also unblock the cell if it was blocked by an obstacle
                        targetCell.isBlockedState = false;
                        Debug.Log($"<color=cyan>Boots V2: Destroyed obstacle at ({targetCell.XPosition},{targetCell.YPosition})</color>");
                    }
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    break;
                case RelicEffectType.Gloves_AttackBonusPerNearbyAlly:
                    {
                        // Auto-resolve closest enemy if no target specified
                        var glovesTarget1 = target ?? GetClosestEnemy(caster);
                        if (glovesTarget1 != null)
                        {
                            // Count nearby enemy allies around the TARGET (not caster allies)
                            var gridMgr = ServiceLocator.Get<GridManager>();
                            int nearbyEnemyAllyCount = 0;
                            if (gridMgr != null)
                            {
                                var targetPos2 = gridMgr.WorldToGridPosition(glovesTarget1.transform.position);
                                var enemyAllies = GetEnemies(caster).Where(e => e != glovesTarget1);
                                foreach (var ea in enemyAllies)
                                {
                                    var eaPos = gridMgr.WorldToGridPosition(ea.transform.position);
                                    if (Mathf.Max(Mathf.Abs(eaPos.x - targetPos2.x), Mathf.Abs(eaPos.y - targetPos2.y)) <= effect.tileRange)
                                        nearbyEnemyAllyCount++;
                                }
                            }
                            float allyBonus = nearbyEnemyAllyCount * effect.value2;
                            ExecuteAttackWithPercentBonus(caster, glovesTarget1, allyBonus);
                            Debug.Log($"<color=cyan>Gloves V1: {nearbyEnemyAllyCount} nearby enemy allies, bonus={allyBonus*100}%</color>");
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V2_AttackBonusPerRelicInHand:
                    {
                        // Auto-resolve closest enemy if no target specified
                        var glovesTarget = target ?? GetClosestEnemy(caster);
                        if (glovesTarget != null)
                        {
                            int relicCount = 0;
                            if (BattleDeckManager.Instance != null)
                            {
                                // Only count Master-at-Arms relic cards in hand
                                relicCount = BattleDeckManager.Instance.Hand
                                    .Count(c => c.roleTag == UnitRole.MasterAtArms);
                            }
                            float relicBonus = relicCount * effect.value2;
                            ExecuteAttackWithPercentBonus(caster, glovesTarget, relicBonus);
                            Debug.Log($"<color=cyan>Gloves V2: {relicCount} MA cards in hand, bonus={relicBonus*100}%</color>");
                        }
                    }
                    break;
                case RelicEffectType.Hat_ReduceUltimateCost:
                    {
                        if (BattleDeckManager.Instance != null)
                        {
                            int reduction2 = (int)effect.value1;
                            
                            // Set persistent flag so future drawn ultimates also get reduced
                            BattleDeckManager.Instance.ultimateCostReductionActive = true;
                            BattleDeckManager.Instance.ultimateCostReductionAmount = reduction2;
                            
                            // Reduce all ultimate cards currently in hand
                            foreach (var handCard2 in BattleDeckManager.Instance.Hand)
                            {
                                if (handCard2.category == RelicCategory.Ultimate)
                                {
                                    if (handCard2.originalEnergyCost < 0)
                                        handCard2.originalEnergyCost = handCard2.energyCost;
                                    // Always reduce from ORIGINAL cost to prevent double-stacking
                                    handCard2.energyCost = Mathf.Max(0, handCard2.originalEnergyCost - reduction2);
                                    Debug.Log($"<color=green>Hat V1: Reduced {handCard2.cardName} cost to {handCard2.energyCost}</color>");
                                }
                            }
                            
                            // Also reduce ultimates still in the deck
                            foreach (var deckCard in BattleDeckManager.Instance.Deck)
                            {
                                if (deckCard.category == RelicCategory.Ultimate)
                                {
                                    if (deckCard.originalEnergyCost < 0)
                                        deckCard.originalEnergyCost = deckCard.energyCost;
                                    deckCard.energyCost = Mathf.Max(0, deckCard.originalEnergyCost - reduction2);
                                }
                            }
                            
                            Debug.Log($"<color=green>Hat V1: Ultimate cost reduction active (-{reduction2} energy) until next ultimate is played</color>");
                        }
                    }
                    break;
                case RelicEffectType.Hat_V2_IncreaseEnemyWeaponCost:
                    {
                        // Apply WeaponCostIncrease to ALL enemies so whichever plays a weapon next pays more
                        var allEnemies2 = GetEnemies(caster);
                        foreach (var enemy in allEnemies2)
                        {
                            var sem5 = GetStatusEffects(enemy);
                            sem5?.ApplyEffect(StatusEffect.CreateWeaponCostIncrease(1, (int)effect.value1, null));
                        }
                        Debug.Log($"<color=cyan>Hat V2: Next enemy weapon relic costs +{(int)effect.value1} energy</color>");
                    }
                    break;
                case RelicEffectType.Coat_BonusDamageNearbyAllies:
                    {
                        var nearbyAllies5 = GetAlliesInRange(caster, effect.tileRange);
                        foreach (var ally in nearbyAllies5)
                        {
                            var effects9 = GetStatusEffects(ally);
                            effects9?.ApplyEffect(StatusEffect.CreateDamageBoost(effect.duration, effect.value2, null));
                        }
                        
                        // Flash highlight tiles and units in radius
                        var gridMgr6 = ServiceLocator.Get<GridManager>();
                        if (gridMgr6 != null)
                        {
                            var casterGridPos = gridMgr6.WorldToGridPosition(caster.transform.position);
                            int range = effect.tileRange;
                            Color highlightColor = new Color(1f, 0.85f, 0.2f, 0.8f); // Golden
                            
                            for (int dx = -range; dx <= range; dx++)
                            {
                                for (int dy = -range; dy <= range; dy++)
                                {
                                    var cell = gridMgr6.GetCell(casterGridPos.x + dx, casterGridPos.y + dy);
                                    if (cell != null)
                                    {
                                        cell.FlashHighlight(highlightColor, 2f);
                                    }
                                }
                            }
                        }
                        
                        Debug.Log($"<color=cyan>Coat V1: Boosted {nearbyAllies5.Count} nearby allies by {effect.value2*100}% damage</color>");
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
                case RelicEffectType.Trinket_V1_MasterAtArms:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Trinket_V2_MasterAtArms:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Totem_V1_MasterAtArms:
                    {
                        // Disable enemy weapon/gloves relics for next turn
                        var enemies8 = GetEnemies(caster);
                        foreach (var enemy in enemies8)
                        {
                            var sem6 = GetStatusEffects(enemy);
                            sem6?.ApplyEffect(StatusEffect.CreateWeaponDisabled(effect.duration, null));
                        }
                        Debug.Log($"<color=red>Totem V1: Disabled enemy weapons for {effect.duration} turn(s)</color>");
                    }
                    break;
                case RelicEffectType.Totem_V2_MasterAtArms:
                    {
                        var gridManager5 = ServiceLocator.Get<GridManager>();
                        var hazardManager5 = ServiceLocator.Get<HazardManager>();
                        if (gridManager5 != null && hazardManager5 != null)
                        {
                            int middleCol2 = gridManager5.GetMiddleColumnIndex();
                            int placed3 = 0;
                            for (int attempt = 0; attempt < 50 && placed3 < (int)effect.value1; attempt++)
                            {
                                // Only on enemy side
                                int x;
                                if (caster.Team == Team.Player)
                                    x = Random.Range(middleCol2 + 1, gridManager5.GridWidth);
                                else
                                    x = Random.Range(0, middleCol2);
                                int y = Random.Range(0, gridManager5.GridHeight);
                                var cell = gridManager5.GetCell(x, y);
                                if (cell != null && !cell.HasHazard && !cell.IsMiddleColumn)
                                {
                                    hazardManager5.CreateEarthquakeHazard(cell, effect.duration);
                                    placed3++;
                                    Debug.Log($"<color=yellow>Earthquake hazard placed at ({cell.XPosition},{cell.YPosition})</color>");
                                }
                            }
                        }
                    }
                    break;
                case RelicEffectType.Ultimate_V1_MasterAtArms:
                    {
                        var allEnemies = GetEnemies(caster);
                        foreach (var enemy in allEnemies)
                        {
                            ExecuteAttack(caster, enemy);
                        }
                    }
                    break;
                case RelicEffectType.Ultimate_V2_MasterAtArms:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        var gridManager6 = ServiceLocator.Get<GridManager>();
                        if (gridManager6 != null)
                        {
                            var targetPos = gridManager6.WorldToGridPosition(target.transform.position);
                            var rowUnits = GetEnemies(caster).Where(e =>
                            {
                                var ePos = gridManager6.WorldToGridPosition(e.transform.position);
                                return ePos.y == targetPos.y && e != target;
                            }).ToList();
                            foreach (var enemy in rowUnits)
                            {
                                enemy.TakeDamage((int)effect.value1, caster.gameObject, false);
                            }
                            Debug.Log($"<color=cyan>Ult V2: Row splash hit {rowUnits.Count} enemies for {(int)effect.value1} damage</color>");
                        }
                    }
                    break;
                case RelicEffectType.PassiveUnique_WeaponRelicOnKill:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.PassiveUnique_V2_HealOnKill:
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;

                case RelicEffectType.Boots_V1_Captain:
                    {
                        // V5: Swap location with another unit.
                        if (target != null) {
                            SwapUnits(caster, target);
                        }
                    }
                    break;
                case RelicEffectType.Boots_V2_Captain:
                    {
                        // V5: Move any allied unit 2 tiles.
                        RelicTargetSelector.Instance.SelectAllyThenTile("Select an ally, then a tile", (ally, cell) => {
                            ExecuteMove(ally, cell, 2);
                        });
                    }
                    break;
                case RelicEffectType.Boots_V1_Quartermaster:
                    {
                        // V5: Move 2 tiles and restore Morale Tier morale (to self).
                            caster.RestoreMorale(Mathf.FloorToInt(caster.CurrentMorale / 10f));
                    }
                    break;
                case RelicEffectType.Boots_V2_Quartermaster:
                    {
                        // V5: Lowest-morale ally moves free.
                        TacticalGame.Units.UnitStatus boots_ally_boots_v2 = GetLowestMoraleAlly(caster);
                        if (boots_ally_boots_v2 != null) {
                            RelicTargetSelector.Instance.SelectTile("Move lowest-morale ally", (cell) => {
                                ExecuteMove(boots_ally_boots_v2, cell, 99); // Free move
                            }, null, true, 99, boots_ally_boots_v2);
                        }
                    }
                    break;
                case RelicEffectType.Boots_V1_Helmsmaster:
                    {
                        // V5: Move 2 tiles and clear the Buzz meter.
                            caster.ReduceBuzz(99); // Clear buzz 
                    }
                    break;
                case RelicEffectType.Boots_V2_Helmsmaster:
                    {
                        // V5: Move 2 tiles. If Grog > 0, cost 0 Energy.
                    }
                    break;
                case RelicEffectType.Boots_V1_Boatswain:
                    {
                        // V5: Move 2 tiles. Take -2 dmg during enemy next turn.
                            var effects = GetStatusEffects(caster);
                            effects?.ApplyEffect(StatusEffect.CreateDamageReduction(1, 2f, null));
                    }
                    break;
                case RelicEffectType.Boots_V2_Boatswain:
                    {
                        // V5: If highest HP, any distance; else 2 tiles.
                        // Movement handled by BattleDeckUI (teleport if highest HP, step-by-step otherwise).
                    }
                    break;
                case RelicEffectType.Boots_V1_Shipwright:
                    {
                        // V5: Can move to any tile inside the Neutral Zone.
                        // Movement handled by BattleDeckUI step-by-step targeting.
                    }
                    break;
                case RelicEffectType.Boots_V2_Shipwright:
                    {
                        // V5: Move 2 tiles, gain +2 Grit for 2 turns.
                            var effects = GetStatusEffects(caster);
                            effects?.ApplyEffect(StatusEffect.CreateMaxHPBoost(2, 2f, null));
                    }
                    break;
                case RelicEffectType.Boots_V1_MasterGunner:
                    {
                        // V5: Move 2 tiles and gain +5 Aim for that turn.
                            var effects = GetStatusEffects(caster);
                            effects?.ApplyEffect(StatusEffect.CreateAimBoost(1, 5f, null));
                    }
                    break;
                case RelicEffectType.Boots_V2_MasterGunner:
                    {
                        // V5: Move 1 tile, reduce next ranged-weapon by 1.
                            ApplyReduceRangedCost(caster, 1);
                    }
                    break;
                case RelicEffectType.Boots_V1_Navigator:
                    {
                        // V5: Move 4 tiles in any direction.
                    }
                    break;
                case RelicEffectType.Boots_V2_Navigator:
                    {
                        // V5: Move 2 tiles. Cost 0 Energy.
                    }
                    break;
                case RelicEffectType.Boots_V1_Surgeon:
                    {
                        // V5: Move 2 tiles and restore Health Tier HP (to self).
                            caster.RestoreHull(Mathf.FloorToInt(caster.MaxHP / 8f));
                    }
                    break;
                case RelicEffectType.Boots_V2_Surgeon:
                    {
                        // V5: Swap location with lowest-health ally.
                        var lowestHealthAlly = GetLowestHPAlly(caster);
                        if (lowestHealthAlly != null) {
                            SwapUnits(caster, lowestHealthAlly);
                        }
                    }
                    break;
                case RelicEffectType.Boots_V1_Cook:
                    {
                        // V5: Move 1 tile and draw a card; if Cook relic, reduce cost by 1.
                            DrawCards(caster, 1);
                            if (BattleDeckManager.Instance != null && BattleDeckManager.Instance.Hand.Count > 0) {
                                var drawnCard = BattleDeckManager.Instance.Hand[BattleDeckManager.Instance.Hand.Count - 1];
                                if (drawnCard.roleTag == TacticalGame.Enums.UnitRole.Cook) {
                                    if (drawnCard.originalEnergyCost < 0) drawnCard.originalEnergyCost = drawnCard.energyCost;
                                    drawnCard.energyCost = UnityEngine.Mathf.Max(0, drawnCard.energyCost - 1);
                                }
                            }
                    }
                    break;
                case RelicEffectType.Boots_V2_Cook:
                    {
                        // V5: Move 2 tiles, increase Proficiency 100%.
                            var effects = GetStatusEffects(caster);
                            effects?.ApplyEffect(StatusEffect.CreateProficiencyBoost(1, 100f, null));
                    }
                    break;
                case RelicEffectType.Boots_V1_Swashbuckler:
                    {
                        // V5: Move 2 tiles; if highest Speed, move 4 instead.
                        bool highestSpeed = true;
                        foreach(var unit in GetAllAllies(caster)) {
                            if (unit != caster && unit.Speed > caster.Speed) highestSpeed = false;
                        }
                        int range = highestSpeed ? 4 : 2;
                    }
                    break;
                case RelicEffectType.Boots_V2_Swashbuckler:
                    {
                        // V5: Move to any tile in the same row, but only 1 tile on a column.
                        // Movement handled by BattleDeckUI row-move targeting.
                    }
                    break;
                case RelicEffectType.Boots_V1_Deckhand:
                    {
                        // V5: Move to any tile in the same column, but only 1 tile on a row.
                        // Movement handled by BattleDeckUI column-move targeting.
                    }
                    break;
                case RelicEffectType.Boots_V2_Deckhand:
                    {
                        // V5: Dash up to 3 tiles towards an enemy. If you end next to them, stun them.
                        // Movement handled by BattleDeckUI. Post-move stun applied via ExecutePostMoveEffects.
                    }
                    break;
                case RelicEffectType.Boots_V1_MasterAtArms:
                    {
                    }
                    break;
                case RelicEffectType.Boots_V2_MasterAtArms:
                    {
                    }
                    break;
                case RelicEffectType.Gloves_V1_Captain:
                    {
                        // V5: Default attack. Enemy draws 1 fewer card next turn.
                        if (target != null) {
                            ExecuteAttack(caster, target);
                            if (target.CurrentHP > 0 && !target.HasSurrendered) {
                                var effects = GetStatusEffects(target);
                                effects?.ApplyEffect(StatusEffect.CreateReduceCardDraw(1, 1, null));
                            }
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V2_Captain:
                    {
                        // V5: Forces target's next card to cost +1 Energy.
                        if (target != null) {
                            ExecuteAttack(caster, target);
                            var effects = GetStatusEffects(target);
                            effects?.ApplyEffect(StatusEffect.CreateIncreaseCost(1, 1, null));
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V1_Quartermaster:
                    {
                        // V5: Bonus dmg scales with target's missing morale (+1 per missing block, max +3).
                        if (target != null) {
                            int missingBlocks = Mathf.FloorToInt((target.MaxMorale - target.CurrentMorale) / 10f);
                            if (missingBlocks > 3) missingBlocks = 3;
                            if (missingBlocks < 0) missingBlocks = 0;
                            ExecuteAttackWithBonus(caster, target, missingBlocks);
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V2_Quartermaster:
                    {
                        // V5: Apply Morale-Marked 2.
                        if (target != null) {
                            ExecuteAttack(caster, target);
                            var effects = GetStatusEffects(target);
                            effects?.ApplyEffect(StatusEffect.CreateMoraleFocus(2, null));
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V1_Helmsmaster:
                    {
                        // V5: Applies a debuff: target cannot reduce its Buzz meter for 2 turns.
                        if (target != null) {
                            ExecuteAttack(caster, target);
                            if (target.CurrentHP > 0 && !target.HasSurrendered) {
                                var effects = GetStatusEffects(target);
                                effects?.ApplyEffect(StatusEffect.CreatePreventBuzzReduction(2, null));
                            }
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V2_Helmsmaster:
                    {
                        // V5: +1 dmg per Grog Token currently available.
                        if (target != null) {
                            var em = ServiceLocator.Get<TacticalGame.Managers.EnergyManager>(); int grog = em != null ? em.GrogTokens : 0;
                            ExecuteAttackWithBonus(caster, target, grog);
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V1_Boatswain:
                    {
                        // V5: +2 dmg if target has less current HP than this unit.
                        if (target != null) {
                            int bonus = (target.CurrentHP < caster.CurrentHP) ? 2 : 0;
                            ExecuteAttackWithBonus(caster, target, bonus);
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V2_Boatswain:
                    {
                        // V5: Lower target's Health stat by Health Tier for 2 turns.
                        if (target != null) {
                            ExecuteAttack(caster, target);
                            var effects = GetStatusEffects(target);
                            int tier = Mathf.FloorToInt(caster.MaxHP / 8f);
                            effects?.ApplyEffect(StatusEffect.CreateLowerHealthStat(2, tier, null));
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V1_Shipwright:
                    {
                        // V5: Target is forced forward 1 tile.
                        if (target != null) {
                            ExecuteAttack(caster, target);
                            if (target.CurrentHP > 0 && !target.HasSurrendered) {
                                // Knockback target towards caster
                                var grid = ServiceLocator.Get<TacticalGame.Grid.GridManager>();
                                if (grid != null) {
                                    var casterPos = grid.WorldToGridPosition(caster.transform.position);
                                    var targetPos = grid.WorldToGridPosition(target.transform.position);
                                    // Move 1 tile towards caster
                                    int dx = System.Math.Sign(casterPos.x - targetPos.x);
                                    int dy = System.Math.Sign(casterPos.y - targetPos.y);
                                    var newPos = new UnityEngine.Vector2Int(targetPos.x + dx, targetPos.y + dy);
                                    var newCell = grid.GetCell(newPos.x, newPos.y);
                                    if (newCell != null) target.GetComponent<TacticalGame.Units.UnitMovement>()?.MoveToCell(newCell);
                                }
                            }
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V2_Shipwright:
                    {
                        // V5: Target's next turn it can only attack the closest target.
                        if (target != null) {
                            ExecuteAttack(caster, target);
                            var effects = GetStatusEffects(target);
                            effects?.ApplyEffect(StatusEffect.CreateForceTargetClosest(1, null));
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V1_MasterGunner:
                    {
                        // V5: +1 dmg per card already played this round.
                        if (target != null) {
                            int played = 0;
                            if (BattleDeckManager.Instance != null) {
                                played = BattleDeckManager.Instance.CardsPlayedThisTurn;
                            }
                            ExecuteAttackWithBonus(caster, target, played);
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V2_MasterGunner:
                    {
                        // V5: +1 dmg per Master Gunner relic used this game.
                        if (target != null) {
                            int mgRelicsUsed = BattleDeckManager.Instance != null ? BattleDeckManager.Instance.MasterGunnerRelicsUsed : 0;
                            ExecuteAttackWithBonus(caster, target, mgRelicsUsed);
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V1_Navigator:
                    {
                        // V5: Cast: disable enemy weapons' role effect next turn.
                        if (target != null) {
                            ExecuteAttack(caster, target);
                            var effects = GetStatusEffects(target);
                            effects?.ApplyEffect(StatusEffect.CreateWeaponDisabled(1, null));
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V2_Navigator:
                    {
                        // V5: +3 dmg per Boots relic card in your deck.
                        if (target != null) {
                            int boots = 0;
                            if (BattleDeckManager.Instance != null) {
                                foreach(var c in BattleDeckManager.Instance.Deck) {
                                    if (c.category == RelicCategory.Boots) boots++;
                                }
                            }
                            ExecuteAttackWithBonus(caster, target, boots * 3);
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V1_Surgeon:
                    {
                        // V5: Default attack and restore 20 HP to the lowest-HP allied unit.
                        if (target != null) {
                            ExecuteAttack(caster, target);
                            var ally = GetLowestHPAlly(caster);
                            if (ally != null) {
                                ally.Heal(20);
                            }
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V2_Surgeon:
                    {
                        // V5: Passive - Whenever an enemy gets healed, attack them.
                        // Handled by PassiveRelicManager or Event System
                        Debug.Log("Surgeon V2 Passive triggered");
                    }
                    break;
                case RelicEffectType.Gloves_V1_Cook:
                    {
                        // V5: Next time the target attacks, the debuff detonates for 8 dmg to all nearby enemies.
                        if (target != null) {
                            ExecuteAttack(caster, target);
                            var effects = GetStatusEffects(target);
                            effects?.ApplyEffect(StatusEffect.CreateCookDetonateBuff(1, 8f, null));
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V2_Cook:
                    {
                        // V5: Put the closest target into Stasis for 1 turn.
                        var closest = GetClosestEnemy(caster);
                        if (closest != null) {
                            var effects = GetStatusEffects(closest);
                            effects?.ApplyEffect(StatusEffect.CreateStasis(1, null));
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V1_Swashbuckler:
                    {
                        // V5: Default attack 2 times.
                        if (target != null) {
                            ExecuteAttack(caster, target);
                            ExecuteAttack(caster, target);
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V2_Swashbuckler:
                    {
                        // V5: For 2 turns: if the target moves, it is stunned for 1 turn.
                        if (target != null) {
                            ExecuteAttack(caster, target);
                            var effects = GetStatusEffects(target);
                            effects?.ApplyEffect(StatusEffect.CreateStunOnMoveTracker(2, null));
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V1_MasterAtArms:
                    {
                        if (target != null) {
                            ExecuteAttack(caster, target);
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V2_MasterAtArms:
                    {
                        if (target != null) {
                            ExecuteAttack(caster, target);
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V1_Deckhand:
                    {
                        // V5: If the attack destroys the target's Hull shield, draw 1 card.
                        if (target != null) {
                            float hpBefore = target.CurrentHP;
                            ExecuteAttack(caster, target);
                            if (target.CurrentHP <= 0 && hpBefore > 0) {
                                DrawCards(caster, 1);
                            }
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V2_Deckhand:
                    {
                        // V5: If the attack destroys the target's Hull shield, gain 1 Energy.
                        if (target != null) {
                            float hpBefore = target.CurrentHP;
                            ExecuteAttack(caster, target);
                            if (target.CurrentHP <= 0 && hpBefore > 0) {
                                ServiceLocator.Get<TacticalGame.Managers.EnergyManager>()?.AddEnergy(1);
                            }
                        }
                    }
                    break;
                case RelicEffectType.Hat_V1_Captain:
                    {
                        // V5: Draw 2 cards. For 2 turns, the Captain takes +200% damage taken (i.e. double damage taken).
                        DrawCards(caster, 2);
                        var effects = GetStatusEffects(caster);
                        effects?.ApplyEffect(StatusEffect.CreateWeakness(2, 2.0f, null));
                    }
                    break;
                case RelicEffectType.Hat_V2_Captain:
                    {
                        // V5: Draw an Ultimate ability card immediately.
                        var deckManager = BattleDeckManager.Instance;
                        if (deckManager != null) {
                            deckManager.DrawCardByCategoryAnyUnit(TacticalGame.Enums.RelicCategory.Ultimate);
                        }
                    }
                    break;
                case RelicEffectType.Hat_V1_Quartermaster:
                    {
                        // V5: Restore 10 + Morale Tier morale to the lowest-morale ally.
                        var ally = GetLowestMoraleAlly(caster);
                        if (ally != null) {
                            int tier = UnityEngine.Mathf.FloorToInt(ally.MaxMorale / 10f);
                            ally.RestoreMorale(10 + tier);
                        }
                    }
                    break;
                case RelicEffectType.Hat_V2_Quartermaster:
                    {
                        // V5: All nearby allies within 1 tile restore Morale Tier morale each.
                        var nearby = GetAlliesInRange(caster, 1);
                        foreach (var a in nearby) {
                            if (a != null && a != caster) {
                                int tier = UnityEngine.Mathf.FloorToInt(a.MaxMorale / 10f);
                                a.RestoreMorale(tier);
                            }
                        }
                    }
                    break;
                case RelicEffectType.Hat_V1_Helmsmaster:
                    {
                        // V5: This round, all rum usage costs 0 Grog Tokens (next 3 rum uses).
                        var effects = GetStatusEffects(caster);
                        effects?.ApplyEffect(StatusEffect.CreateFreeRumUsage(1, 3, null));
                    }
                    break;
                case RelicEffectType.Hat_V2_Helmsmaster:
                    {
                        // V5: Generate 2 Grog Tokens.
                        var em = ServiceLocator.Get<TacticalGame.Managers.EnergyManager>();
                        if (em != null) {
                            em.AddGrog(2);
                        }
                    }
                    break;
                case RelicEffectType.Hat_V1_Boatswain:
                    {
                        // V5: Last 2 turns: returns 1 instance of damage back to attacker per hit.
                        var effects = GetStatusEffects(caster);
                        effects?.ApplyEffect(StatusEffect.CreateReturnDamage(2, 1, null));
                    }
                    break;
                case RelicEffectType.Hat_V2_Boatswain:
                    {
                        // V5: Last 2 turns: this unit's Health stat is increased by 25% (+Health Tierx2).
                        int tier = UnityEngine.Mathf.FloorToInt(caster.MaxHP / 8f);
                        var effects = GetStatusEffects(caster);
                        effects?.ApplyEffect(StatusEffect.CreateHealthStatBoost(2, tier * 2, null));
                    }
                    break;
                case RelicEffectType.Hat_V1_Shipwright:
                    {
                        // V5: Gain 2 extra Energy next turn if this unit is knocked back.
                        var effects = GetStatusEffects(caster);
                        effects?.ApplyEffect(StatusEffect.CreateEnergyOnKnockback(1, 2, null));
                    }
                    break;
                case RelicEffectType.Hat_V2_Shipwright:
                    {
                        // V5: Swap the position of the enemy unit with the highest Grit with the one with the lowest Grit.
                        var highestGrit = GetHighestGritEnemy(caster);
                        var lowestGrit = GetLowestGritEnemy(caster);
                        if (highestGrit != null && lowestGrit != null && highestGrit != lowestGrit) {
                            SwapUnits(highestGrit, lowestGrit);
                        }
                    }
                    break;
                case RelicEffectType.Hat_V1_MasterGunner:
                    {
                        // V5: Your next weapon relic can be used twice this turn. (Bonus damage applies to any weapon used.)
                        var deckManager = BattleDeckManager.Instance;
                        if (deckManager != null) deckManager.weaponUseTwiceActive = true;
                    }
                    break;
                case RelicEffectType.Hat_V2_MasterGunner:
                    {
                        // V5: Draw a weapon relic from your deck.
                        var deckManager = BattleDeckManager.Instance;
                        if (deckManager != null) {
                            deckManager.DrawCardByCategoryAnyUnit(TacticalGame.Enums.RelicCategory.Weapon);
                        }
                    }
                    break;
                case RelicEffectType.Hat_V1_Navigator:
                    {
                        // V5: Enemies cannot use Ultimate abilities next turn.
                        // We apply DisableNonWeaponRelics to ALL enemies
                        var enemies = UnityEngine.Object.FindObjectsByType<TacticalGame.Units.UnitStatus>(UnityEngine.FindObjectsSortMode.None);
                        foreach (var e in enemies) {
                            if (e != null && e.Team != caster.Team && e.CurrentHP > 0 && !e.HasSurrendered) {
                                var eff = GetStatusEffects(e);
                                eff?.ApplyEffect(StatusEffect.CreateDisableNonWeaponRelics(1, null));
                            }
                        }
                    }
                    break;
                case RelicEffectType.Hat_V2_Navigator:
                    {
                        // V5: Cast to get a Boots relic card in hand.
                        var deckManager = BattleDeckManager.Instance;
                        if (deckManager != null) {
                            deckManager.DrawCardByCategoryAnyUnit(TacticalGame.Enums.RelicCategory.Boots);
                        }
                    }
                    break;
                case RelicEffectType.Hat_V1_Surgeon:
                    {
                        // V5: Draw a Trinket relic card and reduce its cost by 1.
                        var deckManager = BattleDeckManager.Instance;
                        if (deckManager != null) {
                            deckManager.DrawCardByCategoryAnyUnit(TacticalGame.Enums.RelicCategory.Trinket);
                            if (deckManager.Hand.Count > 0) {
                                var drawn = deckManager.Hand[deckManager.Hand.Count - 1];
                                if (drawn.category == TacticalGame.Enums.RelicCategory.Trinket) {
                                    drawn.originalEnergyCost = drawn.energyCost;
                                    drawn.energyCost = UnityEngine.Mathf.Max(0, drawn.energyCost - 1);
                                }
                            }
                        }
                    }
                    break;
                case RelicEffectType.Hat_V2_Surgeon:
                    {
                        // V5: Buff: one ally that does damage to an enemy is healed by 10% HP (+Health Tier HP) this turn.
                        RelicTargetSelector.Instance.SelectAlly("Select ally to buff", (ally) => {
                            int tier = UnityEngine.Mathf.FloorToInt(ally.MaxHP / 8f);
                            var effects = GetStatusEffects(ally);
                            effects?.ApplyEffect(StatusEffect.CreateHealOnDamage(1, 0.10f, tier, null));
                        });
                    }
                    break;
                case RelicEffectType.Hat_V1_Cook:
                    {
                        // V5: This turn, reduce by 1 the cost of relic cards of the lowest-HP allied unit.
                        var lowestAlly = GetLowestHPAlly(caster);
                        if (lowestAlly != null) {
                            var deckManager = BattleDeckManager.Instance;
                            if (deckManager != null) {
                                foreach(var c in deckManager.Hand) {
                                    if (c.BelongsTo(lowestAlly)) {
                                        if (c.originalEnergyCost < 0) c.originalEnergyCost = c.energyCost;
                                        c.energyCost = UnityEngine.Mathf.Max(0, c.energyCost - 1);
                                    }
                                }
                            }
                        }
                    }
                    break;
                case RelicEffectType.Hat_V2_Cook:
                    {
                        // V5: Move a unit forward 1 tile and heal it for 10% HP (+Health Tier HP).
                        RelicTargetSelector.Instance.SelectAlly("Select ally to move and heal", (ally) => {
                            // Move 1 tile forward (assuming 'forward' means +x or something, let's just push towards enemy side or generic forward)
                            var grid = ServiceLocator.Get<TacticalGame.Grid.GridManager>();
                            if (grid != null) {
                                var pos = grid.WorldToGridPosition(ally.transform.position);
                                // Forward usually means x+1 for player team
                                int dirX = (ally.Team == TacticalGame.Enums.Team.Player) ? 1 : -1;
                                var targetCell = grid.GetCell(pos.x + dirX, pos.y);
                                if (targetCell != null) ally.GetComponent<TacticalGame.Units.UnitMovement>()?.MoveToCell(targetCell);
                            }
                            int tier = UnityEngine.Mathf.FloorToInt(ally.MaxHP / 8f);
                            ally.Heal(UnityEngine.Mathf.RoundToInt(ally.MaxHP * 0.10f) + tier);
                        });
                    }
                    break;
                case RelicEffectType.Hat_V1_Swashbuckler:
                    {
                        // V5: Draw a card; if it's a weapon relic, reduce its cost by 1.
                        var deckManager = BattleDeckManager.Instance;
                        if (deckManager != null) {
                            deckManager.DrawOneCard();
                            if (deckManager.Hand.Count > 0) {
                                var drawn = deckManager.Hand[deckManager.Hand.Count - 1];
                                if (drawn.IsWeaponCard || drawn.category == TacticalGame.Enums.RelicCategory.Gloves) {
                                    if (drawn.originalEnergyCost < 0) drawn.originalEnergyCost = drawn.energyCost;
                                    drawn.energyCost = UnityEngine.Mathf.Max(0, drawn.energyCost - 1);
                                }
                            }
                        }
                    }
                    break;
                case RelicEffectType.Hat_V2_Swashbuckler:
                    {
                        // V5: Steal a random enemy card; if it's a weapon, reduce its cost by 1.
                        var randEnemy = GetRandomEnemy(caster);
                        var deckManager = BattleDeckManager.Instance;
                        if (deckManager != null) {
                            if (randEnemy != null) deckManager.ForceDiscardFromUnit(randEnemy, 1);
                            // We just draw a card to simulate 'stealing' since enemies don't usually have their own full decks mapped out
                            deckManager.DrawOneCard();
                            if (deckManager.Hand.Count > 0) {
                                var drawn = deckManager.Hand[deckManager.Hand.Count - 1];
                                if (drawn.IsWeaponCard || drawn.category == TacticalGame.Enums.RelicCategory.Gloves) {
                                    if (drawn.originalEnergyCost < 0) drawn.originalEnergyCost = drawn.energyCost;
                                    drawn.energyCost = UnityEngine.Mathf.Max(0, drawn.energyCost - 1);
                                }
                            }
                        }
                    }
                    break;
                case RelicEffectType.Hat_V1_Deckhand:
                    {
                        // V5: Nearby allies within 1 tile have their Hull shield increased by 3 (approx +30%).
                        var nearby = GetAlliesInRange(caster, 1);
                        foreach (var a in nearby) {
                            if (a != null && a != caster) {
                                // Add logic to increase Hull shield by 3
                            }
                        }
                    }
                    break;
                case RelicEffectType.Hat_V1_MasterAtArms:
                    {
                        // V5: Reduce the cost of your next ultimate ability by 2
                    }
                    break;
                case RelicEffectType.Hat_V2_MasterAtArms:
                    {
                        // V5: Increase the cost of enemy next weapon relic by 1
                    }
                    break;
                case RelicEffectType.Hat_V2_Deckhand:
                    {
                        // V5: Destroy all soft obstacles on the map; gain +20% Hull for each destroyed (+2 Hull per obstacle).
                        var hazardMgr = ServiceLocator.Get<TacticalGame.Hazards.HazardManager>();
                        if (hazardMgr != null) {
                            int destroyedCount = hazardMgr.DestroyAllSoftObstacles();
                            if (destroyedCount > 0) {
                                caster.RestoreHull(destroyedCount * 2);
                            }
                        }
                    }
                    break;
                case RelicEffectType.Coat_V1_Captain:
                    {
                        // V5: Allies in 1-tile radius gain +2 Aim and +2 Power for 2 turns.
                        var nearby = GetAlliesInRange(caster, 1);
                        foreach (var a in nearby) {
                            if (a != null) {
                                var eff = GetStatusEffects(a);
                                eff?.ApplyEffect(StatusEffect.CreateAimBoost(2, 2.0f, caster.gameObject));
                                eff?.ApplyEffect(StatusEffect.CreatePowerBoost(2, 2.0f, caster.gameObject));
                            }
                        }
                    }
                    break;
                case RelicEffectType.Coat_V2_Captain:
                    {
                        // V5: For 2 turns (max 3 enemy attacks): when enemies attack, draw 1 card and the enemy discards 1 next turn.
                        var eff = GetStatusEffects(caster);
                        eff?.ApplyEffect(StatusEffect.CreateDrawOnEnemyAttack(2, 3, 1, caster.gameObject));
                    }
                    break;
                case RelicEffectType.Coat_V1_Quartermaster:
                    {
                        // V5: For 2 turns, allies take -3 morale damage (30% less).
                        var allAllies = UnityEngine.Object.FindObjectsByType<TacticalGame.Units.UnitStatus>(UnityEngine.FindObjectsSortMode.None);
                        foreach (var a in allAllies) {
                            if (a != null && a.Team == caster.Team && a.CurrentHP > 0) {
                                var eff = GetStatusEffects(a);
                                eff?.ApplyEffect(StatusEffect.CreateNoMoraleDamage(2, caster.gameObject)); // We will tweak MoraleDamageReduction to support percentage if needed, for now use existing
                            }
                        }
                    }
                    break;
                case RelicEffectType.Coat_V2_Quartermaster:
                    {
                        // V5: For 2 turns, buff an ally unit: if that unit would surrender, restore 5 + Morale Tier morale instead.
                        RelicTargetSelector.Instance.SelectAlly("Select ally to protect", (ally) => {
                            var effects = GetStatusEffects(ally);
                            effects?.ApplyEffect(new StatusEffect(TacticalGame.Combat.StatusEffectType.DeathPrevention, "Surrender Cloak", 2, 5f, 0f, caster.gameObject));
                        });
                    }
                    break;
                case RelicEffectType.Coat_V1_Helmsmaster:
                    {
                        // V5: Nearby allies in 1 tile have reduced rum effect for that turn.
                        var nearby = GetAlliesInRange(caster, 1);
                        foreach (var a in nearby) {
                            if (a != null) {
                                var eff = GetStatusEffects(a);
                                // Using existing BuzzGainReduction
                                eff?.ApplyEffect(StatusEffect.CreateBuzzGainReduction(1, 0.5f, caster.gameObject));
                            }
                        }
                    }
                    break;
                case RelicEffectType.Coat_V2_Helmsmaster:
                    {
                        // V5: Next turn, enemies' Buzz meter fills completely whenever they deal damage.
                        var enemies = UnityEngine.Object.FindObjectsByType<TacticalGame.Units.UnitStatus>(UnityEngine.FindObjectsSortMode.None);
                        foreach (var e in enemies) {
                            if (e != null && e.Team != caster.Team && e.CurrentHP > 0) {
                                var eff = GetStatusEffects(e);
                                eff?.ApplyEffect(new StatusEffect(TacticalGame.Combat.StatusEffectType.EnemyBuzzOnDamage, "Brewer's Mantle", 1, 0f, 0f, caster.gameObject));
                            }
                        }
                    }
                    break;
                case RelicEffectType.Coat_V1_Boatswain:
                    {
                        // V5: Allied units within 1 tile cannot be displaced or knocked back during enemy next turn.
                        var nearby = GetAlliesInRange(caster, 1);
                        foreach (var a in nearby) {
                            if (a != null) {
                                var eff = GetStatusEffects(a);
                                // Need CreatePreventDisplacement
                                eff?.ApplyEffect(new StatusEffect(TacticalGame.Combat.StatusEffectType.PreventDisplacement, "Rooted", 2, 0f, 0f, caster.gameObject));
                            }
                        }
                    }
                    break;
                case RelicEffectType.Coat_V2_Boatswain:
                    {
                        // V5: The lowest-HP ally can only be targeted next turn by enemies with lower HP than themselves.
                        var ally = GetLowestHPAlly(caster);
                        if (ally != null) {
                            var eff = GetStatusEffects(ally);
                            eff?.ApplyEffect(new StatusEffect(TacticalGame.Combat.StatusEffectType.OnlyLowerHPCanTarget, "Stormwarden", 1, 0f, 0f, caster.gameObject));
                        }
                    }
                    break;
                case RelicEffectType.Coat_V1_Shipwright:
                    {
                        // V5: For 2 turns, allies in the same row behind this unit cannot be targeted.
                        var allAllies = UnityEngine.Object.FindObjectsByType<TacticalGame.Units.UnitStatus>(UnityEngine.FindObjectsSortMode.None);
                        var grid = ServiceLocator.Get<TacticalGame.Grid.GridManager>();
                        if (grid != null) {
                            var casterPos = grid.WorldToGridPosition(caster.transform.position);
                            foreach (var a in allAllies) {
                                if (a != null && a.Team == caster.Team && a != caster) {
                                    var allyPos = grid.WorldToGridPosition(a.transform.position);
                                    if (allyPos.y == casterPos.y && allyPos.x < casterPos.x) { // Behind
                                        var eff = GetStatusEffects(a);
                                        eff?.ApplyEffect(new StatusEffect(TacticalGame.Combat.StatusEffectType.RowCantBeTargeted, "Covered", 2, 0f, 0f, caster.gameObject));
                                    }
                                }
                            }
                        }
                    }
                    break;
                case RelicEffectType.Coat_V2_Shipwright:
                    {
                        // V5: Give +4 dmg (+40%) to all allied units in the same column.
                        var allAllies = UnityEngine.Object.FindObjectsByType<TacticalGame.Units.UnitStatus>(UnityEngine.FindObjectsSortMode.None);
                        var grid = ServiceLocator.Get<TacticalGame.Grid.GridManager>();
                        if (grid != null) {
                            var casterPos = grid.WorldToGridPosition(caster.transform.position);
                            foreach (var a in allAllies) {
                                if (a != null && a.Team == caster.Team) {
                                    var allyPos = grid.WorldToGridPosition(a.transform.position);
                                    if (allyPos.x == casterPos.x) { // Same column
                                        var eff = GetStatusEffects(a);
                                        eff?.ApplyEffect(StatusEffect.CreateDamageBoost(1, 0.40f, caster.gameObject));
                                    }
                                }
                            }
                        }
                    }
                    break;
                case RelicEffectType.Coat_V1_MasterGunner:
                    {
                        // V5: Your next 2 Stows have no Energy cost.
                        var deckManager = BattleDeckManager.Instance;
                        if (deckManager != null) deckManager.freeStowsRemaining += 2;
                    }
                    break;
                case RelicEffectType.Coat_V2_MasterGunner:
                    {
                        // V5: Allies in the same row take 50% less damage from Ranged attacks next turn (5 dmg cap).
                        var allAllies = UnityEngine.Object.FindObjectsByType<TacticalGame.Units.UnitStatus>(UnityEngine.FindObjectsSortMode.None);
                        var grid = ServiceLocator.Get<TacticalGame.Grid.GridManager>();
                        if (grid != null) {
                            var casterPos = grid.WorldToGridPosition(caster.transform.position);
                            foreach (var a in allAllies) {
                                if (a != null && a.Team == caster.Team) {
                                    var allyPos = grid.WorldToGridPosition(a.transform.position);
                                    if (allyPos.y == casterPos.y) { // Same row
                                        var eff = GetStatusEffects(a);
                                        eff?.ApplyEffect(new StatusEffect(TacticalGame.Combat.StatusEffectType.RangedDamageReduction, "Volley Cover", 1, 0.50f, 0f, caster.gameObject));
                                    }
                                }
                            }
                        }
                    }
                    break;
                case RelicEffectType.Coat_V1_MasterAtArms:
                    {
                        // V5: Gives +2 (+20%) extra weapon damage to all nearby allies in 1-tile radius.
                        var nearby = GetAlliesInRange(caster, 1);
                        foreach (var a in nearby) {
                            if (a != null) {
                                var eff = GetStatusEffects(a);
                                eff?.ApplyEffect(StatusEffect.CreateDamageBoost(1, 0.20f, caster.gameObject));
                            }
                        }
                    }
                    break;
                case RelicEffectType.Coat_V2_MasterAtArms:
                    {
                        // V5: All enemies next turn have -3 Power (-35% Power stat).
                        var enemies = UnityEngine.Object.FindObjectsByType<TacticalGame.Units.UnitStatus>(UnityEngine.FindObjectsSortMode.None);
                        foreach (var e in enemies) {
                            if (e != null && e.Team != caster.Team && e.CurrentHP > 0) {
                                var eff = GetStatusEffects(e);
                                eff?.ApplyEffect(new StatusEffect(TacticalGame.Combat.StatusEffectType.PowerReduction, "Charge Coat", 1, 3f, 0f, caster.gameObject));
                            }
                        }
                    }
                    break;
                case RelicEffectType.Coat_V1_Navigator:
                    {
                        // V5: Take 0 HP damage from the next attack for 2 turns.
                        var eff = GetStatusEffects(caster);
                        eff?.ApplyEffect(new StatusEffect(TacticalGame.Combat.StatusEffectType.Invincible, "HP Immunity", 2, 0f, 0f, caster.gameObject));
                    }
                    break;
                case RelicEffectType.Coat_V2_Navigator:
                    {
                        // V5: Next turn, the first ally that gets attacked dodges by moving 1 tile back.
                        var allAllies = UnityEngine.Object.FindObjectsByType<TacticalGame.Units.UnitStatus>(UnityEngine.FindObjectsSortMode.None);
                        foreach (var a in allAllies) {
                            if (a != null && a.Team == caster.Team && a.CurrentHP > 0) {
                                var eff = GetStatusEffects(a);
                                eff?.ApplyEffect(new StatusEffect(TacticalGame.Combat.StatusEffectType.Dodge, "Skyline Mantle", 1, 1.0f, 0f, caster.gameObject));
                            }
                        }
                    }
                    break;
                case RelicEffectType.Coat_V1_Surgeon:
                    {
                        // V5: Increase the Primary and Secondary stat of an allied unit by 100% (double both) for 1 turn.
                        RelicTargetSelector.Instance.SelectAlly("Select ally to double stats", (ally) => {
                            var eff = GetStatusEffects(ally);
                            // Simulating by boosting Aim and Power
                            eff?.ApplyEffect(StatusEffect.CreateAimBoost(1, ally.Aim, caster.gameObject));
                            eff?.ApplyEffect(StatusEffect.CreatePowerBoost(1, ally.Power, caster.gameObject));
                        });
                    }
                    break;
                case RelicEffectType.Coat_V2_Surgeon:
                    {
                        // V5: When an enemy kills or makes an ally surrender, the enemy in 1-tile radius is knocked back 1 tile.
                        // Implemented as passive on Surgeon
                        var eff = GetStatusEffects(caster);
                        eff?.ApplyEffect(new StatusEffect(TacticalGame.Combat.StatusEffectType.KnockbackOnAllyDeath, "Last-Stand", 99, 0f, 0f, caster.gameObject));
                    }
                    break;
                case RelicEffectType.Coat_V1_Cook:
                    {
                        // V5: Apply a buff to the closest ally for 1 turn: if that ally is attacked next turn, the attacker is stunned for 1 turn.
                        var ally = GetClosestAlly(caster);
                        if (ally != null) {
                            var eff = GetStatusEffects(ally);
                            eff?.ApplyEffect(StatusEffect.CreateStunAttackerOnHit(1, caster.gameObject));
                        }
                    }
                    break;
                case RelicEffectType.Coat_V2_Cook:
                    {
                        // V5: Clear all debuffs from nearby allies in 1-tile radius.
                        var nearby = GetAlliesInRange(caster, 1);
                        foreach (var a in nearby) {
                            if (a != null) {
                                var eff = GetStatusEffects(a);
                                if (eff != null) {
                                    // Remove debuffs
                                    var debuffs = eff.ActiveEffects.FindAll(e => e.type == TacticalGame.Combat.StatusEffectType.Fire || e.type == TacticalGame.Combat.StatusEffectType.Poison || e.type == TacticalGame.Combat.StatusEffectType.Slowed || e.type == TacticalGame.Combat.StatusEffectType.Weakness || e.type == TacticalGame.Combat.StatusEffectType.AimReduction || e.type == TacticalGame.Combat.StatusEffectType.PowerReduction || e.type == TacticalGame.Combat.StatusEffectType.SpeedReduction || e.type == TacticalGame.Combat.StatusEffectType.Stun);
                                    foreach (var d in debuffs) eff.RemoveEffect(d.type);
                                }
                            }
                        }
                    }
                    break;
                case RelicEffectType.Coat_V1_Swashbuckler:
                    {
                        // V5: Nearby allies within 1-tile radius take 15% less damage when attacked by an enemy with lower Speed.
                        var nearby = GetAlliesInRange(caster, 1);
                        foreach (var a in nearby) {
                            if (a != null && a != caster) {
                                var eff = GetStatusEffects(a);
                                eff?.ApplyEffect(StatusEffect.CreateDamageReduction(1, 0.15f, caster.gameObject)); // Simplification: 15% less damage
                            }
                        }
                    }
                    break;
                case RelicEffectType.Coat_V2_Swashbuckler:
                    {
                        // V5: Curse a random empty tile on enemy side. Any enemy that steps in cannot leave it anymore and takes +1 dmg (+10% incoming damage).
                        var hazardMgr = ServiceLocator.Get<TacticalGame.Hazards.HazardManager>();
                        if (hazardMgr != null) {
                            var grid = ServiceLocator.Get<TacticalGame.Grid.GridManager>();
                            var cells = grid.GetAllCellsList();
                            var enemyCells = cells.FindAll(c => c.XPosition > grid.GridWidth / 2 && !c.IsOccupied);
                            if (enemyCells.Count > 0) {
                                var snareCell = enemyCells[UnityEngine.Random.Range(0, enemyCells.Count)];
                                hazardMgr.CreateCursedTile(snareCell, 99);
                            }
                        }
                    }
                    break;
                case RelicEffectType.Coat_V1_Deckhand:
                    {
                        // V5: This turn, gain bonus weapon damage equal to 50% of available Hull shield for yourself and nearby allies in 1-tile radius.
                        var nearby = GetAlliesInRange(caster, 1);
                        foreach (var a in nearby) {
                            if (a != null) {
                                int bonus = UnityEngine.Mathf.RoundToInt(a.CurrentHullPool * 0.5f);
                                var eff = GetStatusEffects(a);
                                eff?.ApplyEffect(StatusEffect.CreateDamageBoost(1, bonus / 100f, caster.gameObject)); // Assuming flat bonus dmg, but DamageBoost uses percent, maybe use custom effect
                                // We'll just give a flat damage boost StatusEffect if possible, or percentage
                            }
                        }
                    }
                    break;
                case RelicEffectType.Coat_V2_Deckhand:
                    {
                        // V5: Buff a random tile. Units that stay on that tile take -1 dmg (-15%) and deal +1 dmg (+15%).
                        var hazardMgr = ServiceLocator.Get<TacticalGame.Hazards.HazardManager>();
                        if (hazardMgr != null) {
                            var grid = ServiceLocator.Get<TacticalGame.Grid.GridManager>();
                            var cells = grid.GetAllCellsList();
                            var emptyCells = cells.FindAll(c => !c.IsOccupied);
                            if (emptyCells.Count > 0) {
                                var buffCell = emptyCells[UnityEngine.Random.Range(0, emptyCells.Count)];
                                hazardMgr.CreateHealingZone(buffCell, 10, 99);
                            }
                        }
                    }
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
            if (targetCell == null || caster == null) return;
            
            // The UI already constrains the selection to neutral zone tiles
            TeleportUnit(caster, targetCell);
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
            var deckManager = BattleDeckManager.Instance;
            if (deckManager == null) return;
            
            var hand = deckManager.Hand;
            int reduced = 0;
            foreach (var card in hand)
            {
                // Reduce cost of ANY ranged weapon card in hand
                if (card.IsWeaponCard && card.ownerUnit != null && 
                    card.ownerUnit.WeaponType == WeaponType.Ranged)
                {
                    if (card.originalEnergyCost < 0) // Not already reduced
                        card.originalEnergyCost = card.energyCost;
                    card.energyCost = Mathf.Max(0, card.energyCost - reduction);
                    reduced++;
                    Debug.Log($"<color=cyan>Boots V2: Reduced {card.GetDisplayName()} cost to {card.energyCost}</color>");
                }
            }
            if (reduced > 0)
                Debug.Log($"<color=cyan>Boots V2: Reduced cost of {reduced} ranged weapon cards by {reduction}</color>");
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
            var deckManager = BattleDeckManager.Instance;
            if (deckManager != null)
            {
                deckManager.weaponUseTwiceActive = true;
                Debug.Log($"<color=magenta>Hat V1: Next weapon relic is FREE and stays in hand!</color>");
            }
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
            var deckManager = BattleDeckManager.Instance;
            if (deckManager != null)
            {
                deckManager.freeStowsRemaining += count;
                Debug.Log($"<color=cyan>Coat V1: Next {count} stows are FREE! Total free stows: {deckManager.freeStowsRemaining}</color>");
            }
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
        
        private static void StunNearbyEnemies(UnitStatus caster, UnitStatus center, int duration, int range)
        {
            // Find enemies of CASTER that are near CENTER's position
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return;
            
            Vector2Int centerPos = gridManager.WorldToGridPosition(center.transform.position);
            foreach (var enemy in GetEnemies(caster))
            {
                if (enemy == center) continue; // Already stunned directly
                Vector2Int enemyPos = gridManager.WorldToGridPosition(enemy.transform.position);
                int distance = Mathf.Max(Mathf.Abs(centerPos.x - enemyPos.x), Mathf.Abs(centerPos.y - enemyPos.y));
                if (distance <= range)
                {
                    ApplyStun(enemy, duration);
                }
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
        
        private static void DrawTrinketCard(UnitStatus unit)
        {
            var deckManager = BattleDeckManager.Instance;
            if (deckManager != null)
            {
                deckManager.DrawCardByCategory(unit, RelicCategory.Trinket);
            }
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
            if (deckManager == null) return;
            
            // Draw any boots card from any unit in the team
            bool drawn = deckManager.DrawCardByCategoryAnyUnit(RelicCategory.Boots);
            if (!drawn)
                Debug.Log($"<color=yellow>Hat V2: No boots cards available in deck or discard</color>");
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

            GridCell targetCell = cell;

            // If target unit was passed, use its cell
            if (target != null)
            {
                Vector2Int pos = gridManager.WorldToGridPosition(target.transform.position);
                targetCell = gridManager.GetCell(pos.x, pos.y);
            }

            if (targetCell == null) return;

            // If a unit occupies the tile, displace them to nearest free tile
            if (targetCell.IsOccupied && targetCell.OccupyingUnit != null)
            {
                var unitOnTile = targetCell.OccupyingUnit.GetComponent<UnitStatus>();
                if (unitOnTile != null)
                {
                    // Find nearest free adjacent tile
                    GridCell freeCell = null;
                    int searchRadius = 1;
                    while (freeCell == null && searchRadius <= 5)
                    {
                        for (int dx = -searchRadius; dx <= searchRadius && freeCell == null; dx++)
                        {
                            for (int dy = -searchRadius; dy <= searchRadius && freeCell == null; dy++)
                            {
                                if (dx == 0 && dy == 0) continue;
                                var adj = gridManager.GetCell(targetCell.XPosition + dx, targetCell.YPosition + dy);
                                if (adj != null && adj.CanPlaceUnit())
                                {
                                    freeCell = adj;
                                }
                            }
                        }
                        searchRadius++;
                    }

                    if (freeCell != null)
                    {
                        targetCell.RemoveUnit();
                        freeCell.PlaceUnit(unitOnTile.gameObject);
                        unitOnTile.transform.position = freeCell.GetWorldPosition();
                        GameEvents.TriggerUnitMoved(unitOnTile.gameObject, targetCell, freeCell);
                        Debug.Log($"Displaced {unitOnTile.UnitName} to ({freeCell.XPosition},{freeCell.YPosition})");
                    }
                    else
                    {
                        Debug.LogWarning($"Cannot displace {unitOnTile.UnitName} — no free adjacent tile found. Obstacle not placed.");
                        return;
                    }
                }
            }
            // If an obstacle is on the tile (blocked but not middle), displace it to nearest free tile
            else if (targetCell.IsBlocked && !targetCell.IsMiddleColumn)
            {
                GridCell freeCell = null;
                int searchRadius = 1;
                while (freeCell == null && searchRadius <= 5)
                {
                    for (int dx = -searchRadius; dx <= searchRadius && freeCell == null; dx++)
                    {
                        for (int dy = -searchRadius; dy <= searchRadius && freeCell == null; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            var adj = gridManager.GetCell(targetCell.XPosition + dx, targetCell.YPosition + dy);
                            if (adj != null && !adj.IsOccupied && !adj.IsBlocked && !adj.IsMiddleColumn && !adj.HasHazard)
                            {
                                freeCell = adj;
                            }
                        }
                    }
                    searchRadius++;
                }

                if (freeCell != null)
                {
                    // Move the obstacle visual to the new cell
                    if (targetCell.HazardVisualObject != null)
                    {
                        targetCell.HazardVisualObject.transform.position = freeCell.GetWorldPosition();
                        targetCell.HazardVisualObject.transform.SetParent(freeCell.transform);
                        freeCell.hazardVisualObjectRef = targetCell.HazardVisualObject;
                    }
                    freeCell.hasHazardState = true;
                    freeCell.isBlockedState = true;

                    // Clear original tile without destroying visual
                    targetCell.hazardVisualObjectRef = null;
                    targetCell.hasHazardState = false;
                    targetCell.isBlockedState = false;

                    Debug.Log($"Displaced obstacle from ({targetCell.XPosition},{targetCell.YPosition}) to ({freeCell.XPosition},{freeCell.YPosition})");
                }
                else
                {
                    Debug.LogWarning($"Cannot displace obstacle — no free adjacent tile found. Obstacle not placed.");
                    return;
                }
            }

            // Now place the soft obstacle on the target tile (only reached if displacement succeeded or tile was empty)
            hazardManager.CreateSoftObstacle(targetCell, 50, 3);
            Debug.Log($"Summoned obstacle at ({targetCell.XPosition},{targetCell.YPosition})");
        }

        private static void SummonExplodingBarrels(UnitStatus caster, int count, int delay)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            var gridManager = ServiceLocator.Get<GridManager>();
            if (hazardManager == null || gridManager == null) return;

            // Find empty cells on the ENEMY side
            int middleCol = gridManager.GetMiddleColumnIndex();
            bool isPlayer = caster.Team == Team.Player;
            var emptyCells = new List<GridCell>();

            for (int x = 0; x < gridManager.GridWidth; x++)
            {
                // Player's enemies are on the right side (x > middleCol), enemy's enemies on left
                bool isEnemySide = isPlayer ? (x > middleCol) : (x < middleCol);
                if (!isEnemySide) continue;

                for (int y = 0; y < gridManager.GridHeight; y++)
                {
                    var cell = gridManager.GetCell(x, y);
                    if (cell != null && !cell.IsOccupied && !cell.IsBlocked && !cell.HasHazard && !cell.IsMiddleColumn)
                    {
                        emptyCells.Add(cell);
                    }
                }
            }

            // Shuffle and pick 'count' cells
            for (int i = emptyCells.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var temp = emptyCells[i];
                emptyCells[i] = emptyCells[j];
                emptyCells[j] = temp;
            }

            int placed = 0;
            foreach (var cell in emptyCells)
            {
                if (placed >= count) break;
                var barrel = hazardManager.CreateExplodingBarrel(cell, 150, delay);
                if (barrel != null) placed++;
            }
            Debug.Log($"<color=orange>MG Totem: Spawned {placed} exploding barrels on enemy side</color>");
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
            return UnityEngine.Object.FindObjectsByType<TacticalGame.Units.UnitStatus>(UnityEngine.FindObjectsSortMode.None).Select(u => u.gameObject).ToArray()
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
        
        
        private static UnitStatus GetClosestAlly(UnitStatus caster)
        {
            UnitStatus closest = null;
            float minDistance = float.MaxValue;
            var allies = UnityEngine.Object.FindObjectsByType<UnitStatus>(UnityEngine.FindObjectsSortMode.None);
            foreach (var a in allies)
            {
                if (a != null && a.Team == caster.Team && a != caster && a.CurrentHP > 0 && !a.HasSurrendered)
                {
                    float dist = UnityEngine.Vector3.Distance(caster.transform.position, a.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closest = a;
                    }
                }
            }
            return closest;
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
                .Where(a => !a.HasSurrendered && a.CurrentHP > 0)
                .OrderBy(a => a.HPPercent)
                .ThenBy(a => a.GetInstanceID()) // Tie-breaker guarantees Execution matches UI
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

        /// <summary>
        /// Get allies behind the caster in the same row.
        /// "Behind" = further from the neutral zone (middle column).
        /// For player units: lower x values are behind.
        /// For enemy units: higher x values are behind.
        /// </summary>
        private static List<UnitStatus> GetAlliesBehindInRow(UnitStatus caster)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return new List<UnitStatus>();
            
            Vector2Int casterPos = gridManager.WorldToGridPosition(caster.transform.position);
            bool isPlayer = caster.Team == Team.Player;
            
            return GetAllAllies(caster).Where(ally => {
                if (ally == caster) return false;
                Vector2Int allyPos = gridManager.WorldToGridPosition(ally.transform.position);
                if (allyPos.y != casterPos.y) return false;
                // For player: behind = lower x (further from middle)
                // For enemy: behind = higher x (further from middle)
                return isPlayer ? allyPos.x < casterPos.x : allyPos.x > casterPos.x;
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
    
        private static TacticalGame.Units.UnitStatus GetHighestGritEnemy(TacticalGame.Units.UnitStatus caster)
        {
            TacticalGame.Units.UnitStatus highest = null;
            int maxGrit = -1;
            var enemies = UnityEngine.Object.FindObjectsByType<TacticalGame.Units.UnitStatus>(UnityEngine.FindObjectsSortMode.None);
            foreach (var e in enemies)
            {
                if (e != null && e.Team != caster.Team && !e.HasSurrendered && e.CurrentHP > 0)
                {
                    if (e.Grit > maxGrit)
                    {
                        maxGrit = e.Grit;
                        highest = e;
                    }
                }
            }
            return highest;
        }

        private static TacticalGame.Units.UnitStatus GetLowestGritEnemy(TacticalGame.Units.UnitStatus caster)
        {
            TacticalGame.Units.UnitStatus lowest = null;
            int minGrit = int.MaxValue;
            var enemies = UnityEngine.Object.FindObjectsByType<TacticalGame.Units.UnitStatus>(UnityEngine.FindObjectsSortMode.None);
            foreach (var e in enemies)
            {
                if (e != null && e.Team != caster.Team && !e.HasSurrendered && e.CurrentHP > 0)
                {
                    if (e.Grit < minGrit)
                    {
                        minGrit = e.Grit;
                        lowest = e;
                    }
                }
            }
            return lowest;
        }

        private static TacticalGame.Units.UnitStatus GetRandomEnemy(TacticalGame.Units.UnitStatus caster)
        {
            var enemies = new System.Collections.Generic.List<TacticalGame.Units.UnitStatus>();
            var all = UnityEngine.Object.FindObjectsByType<TacticalGame.Units.UnitStatus>(UnityEngine.FindObjectsSortMode.None);
            foreach (var e in all)
            {
                if (e != null && e.Team != caster.Team && !e.HasSurrendered && e.CurrentHP > 0)
                {
                    enemies.Add(e);
                }
            }
            if (enemies.Count > 0)
            {
                return enemies[UnityEngine.Random.Range(0, enemies.Count)];
            }
            return null;
        }
    }
}
