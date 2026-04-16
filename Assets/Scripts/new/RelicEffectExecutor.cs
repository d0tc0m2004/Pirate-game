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
        
        /// <summary>
        /// Execute a relic effect from an EquippedRelic.
        /// </summary>
        public static void Execute(EquippedRelic relic, UnitStatus caster, UnitStatus target = null, GridCell targetCell = null)
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
            
            ExecuteByEffectType(effectData.effectType, effectData, caster, target, targetCell);
        }
        
        /// <summary>
        /// Execute a relic effect directly from RelicEffectData.
        /// </summary>
        public static void Execute(RelicEffectData effectData, UnitStatus caster, UnitStatus target = null, GridCell targetCell = null)
        {
            if (effectData == null || caster == null)
            {
                Debug.LogWarning("RelicEffectExecutor: Missing effect data or caster");
                return;
            }
            
            ExecuteByEffectType(effectData.effectType, effectData, caster, target, targetCell);
        }
        
        /// <summary>
        /// Main execution switch based on effect type.
        /// </summary>
        private static void ExecuteByEffectType(RelicEffectType effectType, RelicEffectData effect, 
            UnitStatus caster, UnitStatus target, GridCell targetCell)
        {
            Debug.Log($"<color=cyan>Executing {effectType} by {caster.UnitName}</color>");
            
            // Auto-select target if needed
            if (target == null)
            {
                target = GetClosestEnemy(caster);
            }
            
            switch (effectType)
            {
                // ==================== BOOTS ====================
                case RelicEffectType.Boots_SwapWithUnit:
                    ExecuteSwapWithUnit(caster, target);
                    break;
                    
                case RelicEffectType.Boots_MoveAlly:
                    ExecuteMoveAlly(caster, target, (int)effect.value1);
                    break;
                    
                case RelicEffectType.Boots_MoveRestoreMorale:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    caster.RestoreMorale(Mathf.RoundToInt(caster.MaxMorale * effect.value2));
                    break;
                    
                case RelicEffectType.Boots_AllyFreeMoveLowestMorale:
                    ApplyFreeMoveToLowestMoraleAlly(caster);
                    break;
                    
                case RelicEffectType.Boots_MoveClearBuzz:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    caster.ReduceBuzz(caster.CurrentBuzz);
                    break;
                    
                case RelicEffectType.Boots_FreeIfGrog:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    // Free if grog - handled by card cost system
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
                        ApplyReduceCardDraw(target, (int)effect.value2, effect.duration);
                    }
                    break;
                    
                case RelicEffectType.Gloves_AttackIncreaseEnemyCost:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        ApplyIncreaseCost(target, (int)effect.value2, effect.duration);
                    }
                    break;
                    
                case RelicEffectType.Gloves_AttackBonusByMissingMorale:
                    if (target != null)
                    {
                        float missingMorale = 1f - target.MoralePercent;
                        int bonusDamage = Mathf.RoundToInt(missingMorale * 100);
                        ExecuteAttackWithBonus(caster, target, bonusDamage);
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
                        // Bonus per card played this round - would need CardDeckManager tracking
                        ExecuteAttack(caster, target);
                    }
                    break;
                    
                case RelicEffectType.Gloves_AttackBonusPerGunnerRelic:
                    if (target != null)
                    {
                        // Bonus per gunner relic used - would need tracking
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
                    ApplyDrawOnEnemyAttack(caster, 1, effect.duration);
                    break;
                    
                case RelicEffectType.Coat_ReduceMoraleDamage:
                    ApplyMoraleDamageReductionNearby(caster, effect.value2, effect.duration, effect.tileRange);
                    break;
                    
                case RelicEffectType.Coat_PreventSurrender:
                    ApplyPreventSurrender(target ?? caster, effect.value2, effect.duration);
                    break;
                    
                case RelicEffectType.Coat_ReduceRumEffect:
                    ApplyReducedRumEffectNearby(caster, effect.value2, effect.tileRange);
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
                    ApplyRowCantBeTargeted(caster, effect.duration);
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
                    SummonCannon(caster, targetCell, (int)effect.value1);
                    break;
                    
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
                    // Passive - handled elsewhere
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
                    SummonAnchor(caster, targetCell, effect.value2, effect.tileRange);
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
                        ExecuteSwapWithUnit(caster, target);
                    break;
                case RelicEffectType.Boots_V2_MoveAllyGainShield:
                    ExecuteMoveAlly(caster, target, (int)effect.value1);
                    // Shield would be applied via StatusEffectManager
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
                        // Get current position before moving
                        var gridManager = ServiceLocator.Get<GridManager>();
                        GridCell previousCell = null;
                        if (gridManager != null)
                        {
                            var coords = gridManager.WorldToGridPosition(caster.transform.position);
                            previousCell = gridManager.GetCell(coords.x, coords.y);
                        }
                        
                        // Move
                        ExecuteMove(caster, targetCell, (int)effect.value1);
                        
                        // Create poison hazard on previous position
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
                        // Hull gain on kill would be tracked by event system
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
                    ShieldAllAllies(caster, (int)effect.value1);
                    break;
                case RelicEffectType.Ultimate_V2_Teleport:
                    // Use RelicTargetSelector to select ally then destination
                    RelicTargetSelector.Instance.SelectAllyThenTile(
                        "Select ally to teleport",
                        (ally, destinationCell) => {
                            TeleportUnit(ally, destinationCell);
                        },
                        () => Debug.Log("Teleport cancelled")
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
                    Debug.Log($"{caster.UnitName} moved and restored {effect.value2 * 100}% health");
                    break;
                case RelicEffectType.Boots_V2_SwapLowestHealthAlly:
                    {
                        var lowestAlly = GetLowestHPAlly(caster);
                        if (lowestAlly != null)
                            ExecuteSwapWithUnit(caster, lowestAlly);
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
                            Debug.Log($"Healed {lowestAlly2.UnitName} for {(int)effect.value2} HP");
                        }
                    }
                    break;
                case RelicEffectType.Gloves_V2_AttackHealedEnemy:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Hat_DrawTrinketReduceCost:
                    DrawCards(caster, 1);
                    ApplyReduceAllCosts(caster, 1, 1);
                    Debug.Log($"{caster.UnitName} drew trinket card with reduced cost");
                    break;
                case RelicEffectType.Hat_V2_HealOnCaptainDamage:
                    {
                        // Apply buff: allies that damage captain get healed
                        var effects1 = GetStatusEffects(caster);
                        effects1?.ApplyEffect(StatusEffect.CreateHealOnCardPlay(effect.duration, effect.value1, null));
                        Debug.Log($"Allies healing on captain damage for {effect.duration} turns");
                    }
                    break;
                case RelicEffectType.Coat_DoubleAllyStats:
                    if (target != null && target.Team == caster.Team)
                    {
                        var effects2 = GetStatusEffects(target);
                        effects2?.ApplyEffect(StatusEffect.CreateDamageBoost(effect.duration, effect.value1, null));
                        Debug.Log($"{target.UnitName} stats boosted by {effect.value1 * 100}% for {effect.duration} turn(s)");
                    }
                    break;
                case RelicEffectType.Coat_V2_KnockbackOnAllyDeath:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Trinket_BlockEnemyRowMovement:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Trinket_V2_GlobalRadius:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Totem_StunHealedEnemy:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Totem_V2_SummonHealingPotions:
                    {
                        // Heal 3 random allies
                        var allies = GetAllAllies(caster);
                        int healed = 0;
                        while (healed < 3 && allies.Count > 0)
                        {
                            var ally = allies[Random.Range(0, allies.Count)];
                            ally.Heal((int)effect.value1);
                            healed++;
                        }
                        Debug.Log($"Summoned healing potions, healed {healed} allies for {(int)effect.value1} HP each");
                    }
                    break;
                case RelicEffectType.Ultimate_PreventDeath:
                    if (target != null)
                    {
                        ApplyDeathPrevention(target, effect.duration);
                        Debug.Log($"{target.UnitName} cannot die or surrender for {effect.duration} turns");
                    }
                    break;
                case RelicEffectType.Ultimate_V2_FullHealthRestore:
                    if (target != null)
                    {
                        target.Heal(target.MaxHP);
                        Debug.Log($"{target.UnitName} fully restored to {target.MaxHP} HP");
                    }
                    break;
                case RelicEffectType.PassiveUnique_HealingAura:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.PassiveUnique_V2_TeamHealOnKill:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;

                // ==================== COOK SPECIFIC ====================
                case RelicEffectType.Boots_MoveDrawCard:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    DrawCards(caster, 1);
                    Debug.Log($"{caster.UnitName} moved and drew a card");
                    break;
                case RelicEffectType.Boots_V2_MoveBoostProficiency:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    {
                        var effects3 = GetStatusEffects(caster);
                        effects3?.ApplyEffect(StatusEffect.CreateDamageBoost(1, effect.value2, null));
                    }
                    Debug.Log($"{caster.UnitName} moved with +{effect.value2 * 100}% proficiency this turn");
                    break;
                case RelicEffectType.Gloves_AttackDetonateBuff:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        ApplyPoison(target, (int)effect.value2, effect.duration);
                        Debug.Log($"{target.UnitName} marked for detonation, {(int)effect.value2} dmg/turn for {effect.duration} turns");
                    }
                    break;
                case RelicEffectType.Gloves_V2_StasisClosest:
                    {
                        var closest = GetClosestEnemy(caster);
                        if (closest != null)
                        {
                            ApplyStun(closest, effect.duration);
                            Debug.Log($"{closest.UnitName} put in stasis for {effect.duration} turn(s)");
                        }
                    }
                    break;
                case RelicEffectType.Hat_ReduceLowestAllyCardCost:
                    {
                        var lowestAlly3 = GetLowestHPAlly(caster);
                        if (lowestAlly3 != null)
                        {
                            ApplyReduceAllCosts(lowestAlly3, (int)effect.value1, effect.duration);
                            Debug.Log($"Reduced {lowestAlly3.UnitName}'s card costs by {(int)effect.value1}");
                        }
                    }
                    break;
                case RelicEffectType.Hat_V2_MoveForwardHeal:
                    if (target != null && target.Team == caster.Team)
                    {
                        PushUnit(caster, target, -1); // negative = forward
                        target.Heal(Mathf.RoundToInt(target.MaxHP * effect.value1));
                        Debug.Log($"Moved {target.UnitName} forward and healed {effect.value1 * 100}%");
                    }
                    break;
                case RelicEffectType.Coat_StunOnAllyAttacked:
                    {
                        // Buff closest ally with counter-stun
                        var closestAlly = GetAlliesInRange(caster, 1).FirstOrDefault();
                        if (closestAlly != null)
                        {
                            ApplyCounterOnAllyHit(closestAlly, effect.duration);
                            Debug.Log($"{closestAlly.UnitName} will stun attacker if hit");
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
                        Debug.Log($"Cleared debuffs from {nearbyAllies.Count} nearby allies");
                    }
                    break;
                case RelicEffectType.Trinket_HazardSizeIncrease:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Trinket_V2_DrawExtraBelow50:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Totem_HealLowestOnDamage:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Totem_V2_SummonStatDebuffObstacle:
                    {
                        // Debuff nearby enemies' stats
                        var nearbyEnemies = GetEnemiesInRange(caster, effect.tileRange);
                        foreach (var enemy in nearbyEnemies)
                        {
                            ApplyWeaknessCurse(enemy, effect.value1, effect.duration);
                        }
                        Debug.Log($"Debuffed {nearbyEnemies.Count} nearby enemies by -{effect.value1 * 100}% stats");
                    }
                    break;
                case RelicEffectType.Ultimate_SwapHealthClosest:
                    {
                        var closestEnemy = GetClosestEnemy(caster);
                        if (closestEnemy != null)
                        {
                            int casterHP = caster.CurrentHP;
                            int enemyHP = closestEnemy.CurrentHP;
                            
                            // Use SetHP to directly swap, bypassing damage modifications (hull, armor, cover)
                            caster.SetHP(enemyHP);
                            closestEnemy.SetHP(casterHP);
                            
                            Debug.Log($"Swapped HP: {caster.UnitName}({casterHP}->{caster.CurrentHP}) <-> {closestEnemy.UnitName}({enemyHP}->{closestEnemy.CurrentHP})");
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
                                // Fire the whole column
                                for (int row = 0; row < 8; row++) // iterate all rows
                                {
                                    var cell = gridManager.GetCell(pos.x, row);
                                    if (cell == null) continue;

                                    // Deal fire damage to occupants
                                    if (cell.IsOccupied && cell.OccupyingUnit != null)
                                    {
                                        var unit = cell.OccupyingUnit.GetComponent<UnitStatus>();
                                        if (unit != null && unit.Team != caster.Team)
                                            unit.TakeDamage((int)effect.value1, caster.gameObject, false);
                                    }

                                    // Create fire hazard
                                    if (hazardManager != null)
                                        hazardManager.CreateFireTile(cell, (int)effect.value2, effect.duration);
                                }
                                Debug.Log($"Set fire to {closestEnemy2.UnitName}'s column! {effect.value1} dmg + fire for {effect.duration} turns");
                            }
                        }
                    }
                    break;
                case RelicEffectType.PassiveUnique_DisplaceOnWeaponUse:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.PassiveUnique_V2_RelicsNotConsumed:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;

                // ==================== SWASHBUCKLER SPECIFIC ====================
                case RelicEffectType.Boots_MoveBySpeed:
                    {
                        // If highest speed, move 4; else move 2
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
                        Debug.Log($"{caster.UnitName} moved {moveRange} tiles (highest speed: {isHighestSpeed})");
                    }
                    break;
                case RelicEffectType.Boots_V2_MoveRowOnly:
                    // Move any tile in same row, 1 tile on column
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    break;
                case RelicEffectType.Gloves_AttackTwice:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        ExecuteAttack(caster, target);
                        Debug.Log($"{caster.UnitName} attacked {target.UnitName} twice!");
                    }
                    break;
                case RelicEffectType.Gloves_V2_AttackStunOnMove:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        // Apply debuff: if target moves in 2 turns, stun 1 turn
                        ApplyStun(target, effect.duration);
                        Debug.Log($"{target.UnitName} will be stunned if they move");
                    }
                    break;
                case RelicEffectType.Hat_DrawWeaponReduceCost:
                    DrawWeaponRelicCard(caster);
                    ApplyReduceAllCosts(caster, 1, 1);
                    Debug.Log($"{caster.UnitName} drew weapon card with reduced cost");
                    break;
                case RelicEffectType.Hat_V2_StealEnemyCard:
                    DrawCards(caster, 1);
                    Debug.Log($"{caster.UnitName} stole an enemy card");
                    break;
                case RelicEffectType.Coat_NearbyAllyDamageReduction:
                    {
                        // Passive - nearby allies -15% damage if attacker has lower speed
                        var nearbyAllies2 = GetAlliesInRange(caster, effect.tileRange);
                        foreach (var ally in nearbyAllies2)
                        {
                            ApplyDamageReduction(ally, effect.value1, effect.duration);
                        }
                        Debug.Log($"Nearby allies gain {effect.value1 * 100}% damage reduction");
                    }
                    break;
                case RelicEffectType.Coat_V2_CurseEmptyTile:
                    if (targetCell != null)
                    {
                        var hazardManager2 = ServiceLocator.Get<HazardManager>();
                        if (hazardManager2 != null)
                            hazardManager2.CreateTrap(targetCell, effect.duration);
                        Debug.Log($"Cursed tile - enemy trapped and takes {effect.value1 * 100}% more damage");
                    }
                    break;
                case RelicEffectType.Trinket_BonusDamageIfAlone:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Trinket_V2_EnemySpeedReduction:
                    // Passive - handled by PassiveRelicManager
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
                            Debug.Log($"Placed {placed} invisible traps");
                        }
                    }
                    break;
                case RelicEffectType.Totem_V2_DisableEnemyPassives:
                    {
                        var enemies = GetEnemies(caster);
                        foreach (var enemy in enemies)
                        {
                            ApplyStun(enemy, 0); // Light disable - prevents passive triggers
                        }
                        Debug.Log($"Enemy passives disabled next turn");
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
                            Debug.Log($"Forced {lowestHP.UnitName} and {enemyCaptain.UnitName} to fight!");
                        }
                    }
                    break;
                case RelicEffectType.Ultimate_V2_SurrenderOn4Weapons:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.PassiveUnique_EnemyDiscardOnBoot:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.PassiveUnique_V2_EnemyBootsLimit:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;

                // ==================== DECKHAND SPECIFIC ====================
                case RelicEffectType.Boots_MoveColumnOnly:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    break;
                case RelicEffectType.Boots_V2_MoveRestoreHull:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    caster.RestoreHull((int)effect.value2);
                    Debug.Log($"{caster.UnitName} moved and restored {(int)effect.value2} hull");
                    break;
                case RelicEffectType.Gloves_AttackDrawOnHullDestroyed:
                    if (target != null)
                    {
                        int hullBefore = target.CurrentHullPool;
                        ExecuteAttack(caster, target);
                        if (target.CurrentHullPool <= 0 && hullBefore > 0)
                        {
                            DrawCards(caster, 1);
                            Debug.Log($"Hull destroyed! {caster.UnitName} drew a card");
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
                            // Refund 1 energy by spending -1
                            var energyMgr = ServiceLocator.Get<EnergyManager>();
                            energyMgr?.TrySpendEnergy(-1);
                            Debug.Log($"Hull destroyed! {caster.UnitName} gained 1 energy");
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
                        Debug.Log($"Nearby allies gained {effect.value1 * 100}% hull shield");
                    }
                    break;
                case RelicEffectType.Hat_V2_DestroyObstaclesGainHull:
                    {
                        // Gain hull bonus (obstacles destruction is placeholder)
                        caster.RestoreHull(Mathf.RoundToInt(caster.MaxHullPool * effect.value1));
                        Debug.Log($"{caster.UnitName} destroyed obstacles and gained hull");
                    }
                    break;
                case RelicEffectType.Coat_HullBonusDamage:
                    {
                        // Buff self and nearby allies with hull-based damage bonus
                        float hullBonus = caster.CurrentHullPool * effect.value1;
                        var nearbyAllies4 = GetAlliesInRange(caster, effect.tileRange);
                        nearbyAllies4.Add(caster);
                        foreach (var ally in nearbyAllies4)
                        {
                            var effects5 = GetStatusEffects(ally);
                            effects5?.ApplyEffect(StatusEffect.CreateDamageBoost(effect.duration, hullBonus / 100f, null));
                        }
                        Debug.Log($"Hull bonus damage: +{hullBonus} to nearby allies");
                    }
                    break;
                case RelicEffectType.Coat_V2_BuffTileDamageExchange:
                    if (targetCell != null)
                    {
                        // Units on tile take and deal more damage
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
                        Debug.Log($"Tile buffed: units take {effect.value1 * 100}% more damage and deal {effect.value1 * 100}% more");
                    }
                    break;
                case RelicEffectType.Trinket_HullFullRegen:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Trinket_V2_HullDiscardOnSurvive:
                    // Passive - handled by PassiveRelicManager
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
                                    // Soft obstacle = placeholder
                                    placed2++;
                                }
                            }
                            Debug.Log($"Created {placed2} soft obstacles (placeholder)");
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
                        Debug.Log($"Pulled {nearbyEnemies2.Count} nearby enemies to same row");
                    }
                    break;
                case RelicEffectType.Ultimate_MassiveHullBuff:
                    if (target != null)
                    {
                        int hullAmount = Mathf.RoundToInt(target.MaxHullPool * effect.value1);
                        target.RestoreHull(hullAmount);
                        Debug.Log($"{target.UnitName} gained {effect.value1 * 100}% hull for {effect.duration} turns");
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
                        Debug.Log($"Cleared all hazards on player side");
                    }
                    break;
                case RelicEffectType.PassiveUnique_HullDestroyedRestoreHealth:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.PassiveUnique_V2_HullDestroyedDamageBonus:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;

                // ==================== NAVIGATOR SPECIFIC ====================
                case RelicEffectType.Boots_MoveFarDistance:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    Debug.Log($"{caster.UnitName} moved up to {(int)effect.value1} tiles");
                    break;
                case RelicEffectType.Boots_V2_MoveFree:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    // 0 energy cost is handled by the card's energyCost field
                    Debug.Log($"{caster.UnitName} moved free");
                    break;
                case RelicEffectType.Gloves_DisableWeaponEffect:
                    if (target != null)
                    {
                        // Disable enemy weapon role effect
                        var effects7 = GetStatusEffects(target);
                        effects7?.ApplyEffect(StatusEffect.CreateWeakness(effect.duration, 0.5f, null));
                        Debug.Log($"Disabled {target.UnitName}'s weapon effect for {effect.duration} turn(s)");
                    }
                    break;
                case RelicEffectType.Gloves_V2_AttackBonusPerBootsCard:
                    if (target != null)
                    {
                        // Count boots cards in deck
                        int bootsCount = 0;
                        if (BattleDeckManager.Instance != null)
                        {
                            bootsCount = BattleDeckManager.Instance.Hand
                                .Count(c => c.category == RelicCategory.Boots);
                        }
                        float bonus = bootsCount * effect.value1;
                        ExecuteAttackWithPercentBonus(caster, target, bonus);
                        Debug.Log($"{caster.UnitName} attacked with +{bonus * 100}% bonus ({bootsCount} boots cards)");
                    }
                    break;
                case RelicEffectType.Hat_DisableEnemyUltimates:
                    {
                        var enemies3 = GetEnemies(caster);
                        foreach (var enemy in enemies3)
                        {
                            ApplyIncreaseCost(enemy, 99, effect.duration); // Make ultimates unplayable
                        }
                        Debug.Log($"Enemy ultimates disabled for {effect.duration} turn(s)");
                    }
                    break;
                case RelicEffectType.Hat_V2_DrawBootsCard:
                    DrawBootsCard(caster);
                    Debug.Log($"{caster.UnitName} drew a boots card");
                    break;
                case RelicEffectType.Coat_HealthDamageImmunity:
                    ApplyDamageReduction(caster, 1f, effect.duration);
                    Debug.Log($"{caster.UnitName} immune to health damage for {effect.duration} turns");
                    break;
                case RelicEffectType.Coat_V2_DodgeFirstAttack:
                    {
                        // First ally attacked dodges
                        var allAllies = GetAllAllies(caster);
                        foreach (var ally in allAllies)
                        {
                            ApplyDodgeChance(ally, 1f, 1); // 100% dodge for 1 attack
                            break; // Only first ally
                        }
                        Debug.Log($"First ally attacked will dodge");
                    }
                    break;
                case RelicEffectType.Trinket_NearbyTacticsBoost:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Trinket_V2_IgnoreSoftObstacles:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Totem_DisableEnemyMovement:
                    {
                        var enemies4 = GetEnemies(caster);
                        foreach (var enemy in enemies4)
                        {
                            ApplySlow(enemy, 99, effect.duration); // Effectively disable movement
                        }
                        Debug.Log($"All enemy movement disabled for {effect.duration} turn(s)");
                    }
                    break;
                case RelicEffectType.Totem_V2_DisableNonWeaponRelics:
                    {
                        var enemies5 = GetEnemies(caster);
                        foreach (var enemy in enemies5)
                        {
                            ApplyIncreaseCost(enemy, 99, effect.duration);
                        }
                        Debug.Log($"Enemy non-weapon relics disabled for {effect.duration} turn(s)");
                    }
                    break;
                case RelicEffectType.Ultimate_MarkReflectToCaptain:
                    if (target != null)
                    {
                        // Mark target - damage reflects to their captain
                        ExecuteAttack(caster, target);
                        var enemyCaptain2 = GetEnemies(caster).FirstOrDefault(e => e.IsCaptain);
                        if (enemyCaptain2 != null)
                        {
                            ApplyVulnerable(target, effect.value1, effect.duration);
                            Debug.Log($"Marked {target.UnitName} - damage reflects to captain");
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
                            ExecuteSwapWithUnit(closest2, furthest);
                            Debug.Log($"Swapped {closest2.UnitName} and {furthest.UnitName} positions");
                        }
                    }
                    break;
                case RelicEffectType.PassiveUnique_FreeMovement:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.PassiveUnique_V2_AllyMovementBoost:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;

                // ==================== MASTER-AT-ARMS SPECIFIC ====================
                case RelicEffectType.Boots_MoveBonusWeaponDamage:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    {
                        var effects8 = GetStatusEffects(caster);
                        effects8?.ApplyEffect(StatusEffect.CreateDamageBoost(1, effect.value2, null));
                    }
                    Debug.Log($"{caster.UnitName} moved, next weapon +{effect.value2 * 100}% damage");
                    break;
                case RelicEffectType.Boots_V2_MoveDestroyObstacle:
                    ExecuteMove(caster, targetCell, (int)effect.value1);
                    Debug.Log($"{caster.UnitName} moved and destroyed obstacle (placeholder)");
                    break;
                case RelicEffectType.Gloves_AttackBonusPerNearbyAlly:
                    if (target != null)
                    {
                        int nearbyCount = GetAlliesInRange(caster, effect.tileRange).Count;
                        float allyBonus = nearbyCount * effect.value1;
                        ExecuteAttackWithPercentBonus(caster, target, allyBonus);
                        Debug.Log($"{caster.UnitName} attacked with +{allyBonus * 100}% ({nearbyCount} nearby allies)");
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
                        Debug.Log($"{caster.UnitName} attacked with +{relicBonus * 100}% ({relicCount} MaA cards in hand)");
                    }
                    break;
                case RelicEffectType.Hat_ReduceUltimateCost:
                    ApplyReduceAllCosts(caster, (int)effect.value1, effect.duration);
                    Debug.Log($"{caster.UnitName}'s next ultimate cost reduced by {(int)effect.value1}");
                    break;
                case RelicEffectType.Hat_V2_IncreaseEnemyWeaponCost:
                    {
                        var closestEnemy3 = target ?? GetClosestEnemy(caster);
                        if (closestEnemy3 != null)
                        {
                            ApplyIncreaseCost(closestEnemy3, (int)effect.value1, effect.duration);
                            Debug.Log($"{closestEnemy3.UnitName}'s weapon cost +{(int)effect.value1}");
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
                        Debug.Log($"Nearby allies gain +{effect.value1 * 100}% damage for {effect.duration} turns");
                    }
                    break;
                case RelicEffectType.Coat_V2_ReduceEnemyPower:
                    {
                        var enemies7 = GetEnemies(caster);
                        foreach (var enemy in enemies7)
                        {
                            ApplyWeaknessCurse(enemy, effect.value1, effect.duration);
                        }
                        Debug.Log($"All enemies -{effect.value1 * 100}% Power for {effect.duration} turns");
                    }
                    break;
                case RelicEffectType.Trinket_CounterAttackOnHit:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Trinket_V2_NearbyPowerBoost:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.Totem_DisableEnemyWeapons:
                    {
                        var enemies8 = GetEnemies(caster);
                        foreach (var enemy in enemies8)
                        {
                            ApplyWeaknessCurse(enemy, 1f, effect.duration); // -100% weapon damage
                        }
                        Debug.Log($"Enemy weapons disabled for {effect.duration} turn(s)");
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
                                    // Earthquake = damage hazard on tile
                                    if (cell.IsOccupied && cell.OccupyingUnit != null)
                                    {
                                        var unit = cell.OccupyingUnit.GetComponent<UnitStatus>();
                                        if (unit != null)
                                            unit.TakeDamage((int)effect.value2, caster.gameObject, false);
                                    }
                                    placed3++;
                                }
                            }
                            Debug.Log($"Earthquake on {placed3} tiles for {(int)effect.value2} damage each");
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
                        Debug.Log($"{caster.UnitName} attacked all {allEnemies.Count} enemies!");
                    }
                    break;
                case RelicEffectType.Ultimate_V2_AttackRowDamage:
                    if (target != null)
                    {
                        ExecuteAttack(caster, target);
                        // Damage all enemies in same row
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
                            Debug.Log($"Row damage: {(int)effect.value2} to {rowEnemies.Count} additional enemies");
                        }
                    }
                    break;
                case RelicEffectType.PassiveUnique_WeaponRelicOnKill:
                    // Passive - handled by PassiveRelicManager
                    Debug.Log($"<color=gray>Passive effect {effectType} - handled by PassiveRelicManager</color>");
                    break;
                case RelicEffectType.PassiveUnique_V2_HealOnKill:
                    // Passive - handled by PassiveRelicManager
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
        
        private static void ExecuteSwapWithUnit(UnitStatus caster, UnitStatus _)
        {
            // Captain V1 Boots: player picks an ally to swap positions with.
            // Uses RelicTargetSelector to prompt the player rather than auto-targeting.
            if (caster == null) return;

            RelicTargetSelector.Instance.SelectAlly(
                "Select an ally to swap locations with",
                (ally) =>
                {
                    if (ally == null || ally == caster) return;
                    SwapUnitsOnGrid(caster, ally);
                },
                () => Debug.Log("Swap cancelled")
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

            // Clear both cells, then re-place at the swapped positions so occupancy tracks.
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

        private static void ExecuteMoveAlly(UnitStatus caster, UnitStatus _, int tiles)
        {
            // Captain V2 Boots: player picks an ally, then a destination tile within range.
            if (caster == null) return;

            RelicTargetSelector.Instance.SelectAllyThenTile(
                $"Select an ally, then a destination (up to {tiles} tiles away)",
                (ally, destinationCell) =>
                {
                    if (ally == null || destinationCell == null) return;
                    if (ally.Team != caster.Team) return;

                    var movement = ally.GetComponent<UnitMovement>();
                    if (movement == null) return;

                    var gridManager = ServiceLocator.Get<GridManager>();
                    if (gridManager == null) return;

                    Vector2Int allyPos = gridManager.WorldToGridPosition(ally.transform.position);
                    int distance = Mathf.Abs(destinationCell.XPosition - allyPos.x) +
                                   Mathf.Abs(destinationCell.YPosition - allyPos.y);

                    if (distance <= tiles)
                    {
                        movement.MoveToCell(destinationCell);
                        Debug.Log($"{ally.UnitName} moved to ({destinationCell.XPosition}, {destinationCell.YPosition})");
                    }
                    else
                    {
                        Debug.Log($"Destination {distance} tiles away, max {tiles}");
                    }
                },
                () => Debug.Log("Move ally cancelled")
            );
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
        
        /// <summary>
        /// Instantly teleport a unit to a destination cell (no range limit).
        /// </summary>
        private static void TeleportUnit(UnitStatus unit, GridCell destination)
        {
            if (unit == null || destination == null) return;
            
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return;
            
            // Get current cell and clear it
            var coords = gridManager.WorldToGridPosition(unit.transform.position);
            GridCell currentCell = gridManager.GetCell(coords.x, coords.y);
            if (currentCell != null)
            {
                currentCell.RemoveUnit();
            }
            
            // Place at destination
            destination.PlaceUnit(unit.gameObject);
            unit.transform.position = destination.GetWorldPosition();
            
            // Trigger move event
            GameEvents.TriggerUnitMoved(unit.gameObject, currentCell, destination);
            
            Debug.Log($"{unit.UnitName} teleported to ({destination.XPosition}, {destination.YPosition})");
        }
        
        private static void PushUnit(UnitStatus target, UnitStatus source, int tiles)
        {
            if (target == null || source == null) return;
            
            var effects = target.GetComponent<StatusEffectManager>();
            if (effects != null && !effects.CanBeKnockedBack())
            {
                Debug.Log($"{target.UnitName} cannot be knocked back!");
                return;
            }
            
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
                Debug.Log($"Pushed {target.UnitName} by {tiles} tiles");
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
                Debug.Log($"Knocked {target.UnitName} to last column");
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
                Debug.Log($"{attacker.UnitName} dealt {damage} damage to {target.UnitName}");
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
            Debug.Log($"{attacker.UnitName} dealt {totalDamage} damage to {target.UnitName} (+{flatBonus} bonus)");
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
            Debug.Log($"{attacker.UnitName} dealt {totalDamage} damage to {target.UnitName} (+{percentBonus*100}%)");
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
            Debug.Log($"{unit.UnitName} gains {percent*100}% damage reduction for {duration} turns");
        }
        
        private static void ApplyGritBoost(UnitStatus unit, float percent, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateGritBoost(duration, percent, null));
            Debug.Log($"{unit.UnitName} gains {percent*100}% Grit boost for {duration} turns");
        }
        
        private static void ApplyAimBoost(UnitStatus unit, float percent, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateAimBoost(duration, percent, null));
            Debug.Log($"{unit.UnitName} gains {percent*100}% Aim boost for {duration} turns");
        }
        
        private static void ApplyVulnerable(UnitStatus unit, float percent, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateVulnerable(duration, percent, null));
            Debug.Log($"{unit.UnitName} is vulnerable for {duration} turns");
        }
        
        private static void ApplyStun(UnitStatus unit, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateStun(duration, null));
            Debug.Log($"{unit.UnitName} is stunned for {duration} turns");
        }
        
        private static void ApplyMoraleFocus(UnitStatus unit, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateMoraleFocus(duration, null));
            Debug.Log($"{unit.UnitName} is marked for morale focus for {duration} turns");
        }
        
        private static void ApplyReduceCardDraw(UnitStatus unit, int reduction, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateReduceCardDraw(duration, reduction, null));
            Debug.Log($"{unit.UnitName} draws {reduction} fewer cards for {duration} turns");
        }
        
        private static void ApplyIncreaseCost(UnitStatus unit, int increase, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateIncreaseCost(duration, increase, null));
            Debug.Log($"{unit.UnitName} card costs +{increase} for {duration} turns");
        }
        
        private static void ApplyPreventBuzzReduction(UnitStatus unit, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreatePreventBuzzReduction(duration, null));
            Debug.Log($"{unit.UnitName} buzz cannot be reduced for {duration} turns");
        }
        
        private static void ApplyHealthStatReduction(UnitStatus unit, float percent, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateHealthStatBoost(duration, -percent, null));
            Debug.Log($"{unit.UnitName} health stat reduced by {percent*100}% for {duration} turns");
        }
        
        private static void ApplyHealthStatBoost(UnitStatus unit, float percent, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateHealthStatBoost(duration, percent, null));
            Debug.Log($"{unit.UnitName} health stat boosted by {percent*100}% for {duration} turns");
        }
        
        private static void ApplyForceTargetClosest(UnitStatus unit, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateForceTargetClosest(duration, null));
            Debug.Log($"{unit.UnitName} forced to target closest for {duration} turns");
        }
        
        private static void ApplyReduceRangedCost(UnitStatus unit, int reduction)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateReduceNextRangedCost(1, reduction, null));
            Debug.Log($"{unit.UnitName} next ranged cost reduced by {reduction}");
        }
        
        private static void ApplyFreeMove(UnitStatus unit)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateFreeMove(1, null));
            Debug.Log($"{unit.UnitName} has a free move");
        }
        
        private static void ApplyReturnDamage(UnitStatus unit, int instances, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateReturnDamage(duration, instances, null));
            Debug.Log($"{unit.UnitName} returns {instances} damage instances for {duration} turns");
        }
        
        private static void ApplyEnergyOnKnockback(UnitStatus unit, int energy, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateEnergyOnKnockback(duration, energy, null));
            Debug.Log($"{unit.UnitName} gains {energy} energy if knocked back for {duration} turns");
        }
        
        private static void ApplyWeaponUseTwice(UnitStatus unit, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateWeaponUseTwice(duration, null));
            Debug.Log($"{unit.UnitName} can use weapon twice for {duration} turns");
        }
        
        private static void ApplyDrawOnEnemyAttack(UnitStatus unit, int cards, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateDrawOnEnemyAttack(duration, cards, null));
            Debug.Log($"{unit.UnitName} draws {cards} cards when attacked for {duration} turns");
        }
        
        private static void ApplyPreventSurrender(UnitStatus unit, float moraleRestore, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreatePreventSurrender(duration, moraleRestore, null));
            Debug.Log($"{unit.UnitName} cannot surrender for {duration} turns");
        }
        
        private static void ApplyRowCantBeTargeted(UnitStatus unit, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateRowCantBeTargeted(duration, null));
            Debug.Log($"{unit.UnitName}'s row protected for {duration} turns");
        }
        
        private static void ApplyFreeStows(UnitStatus unit, int count)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateFreeStows(99, count, null));
            Debug.Log($"{unit.UnitName} has {count} free stows");
        }
        
        private static void ApplyFreeRumUsage(UnitStatus unit, int count)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateFreeRumUsage(99, count, null));
            Debug.Log($"{unit.UnitName} has {count} free rum uses");
        }
        
        private static void ApplyStunOnKnockback(UnitStatus unit, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateStunOnKnockback(duration, null));
            Debug.Log($"{unit.UnitName} will stun attacker if knocked back for {duration} turns");
        }
        
        private static void ApplyFullBuzz(UnitStatus unit, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateBuzzFilled(duration, null));
            Debug.Log($"{unit.UnitName} buzz forced to full for {duration} turns");
        }
        
        private static void ApplyCaptainDamageReflect(UnitStatus unit, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateCaptainDamageReflect(duration, null));
            Debug.Log($"Captain damage reflects for {duration} turns");
        }
        
        private static void ApplyReflectMoraleDamage(UnitStatus unit, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateReflectMoraleDamage(duration, null));
            Debug.Log($"Morale damage reflects to enemies for {duration} turns");
        }
        
        #endregion
        
        #region Area Effect Helpers
        
        private static void RestoreMoraleNearby(UnitStatus caster, float percent, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                ally.RestoreMorale(Mathf.RoundToInt(ally.MaxMorale * percent));
            }
            Debug.Log($"Restored {percent*100}% morale to nearby allies");
        }
        
        private static void BuffNearbyAlliesAimPower(UnitStatus caster, float percent, int duration, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                ApplyAimBoost(ally, percent, duration);
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreatePowerBoost(duration, percent, null));
            }
            Debug.Log($"Buffed nearby allies +{percent*100}% Aim/Power for {duration} turns");
        }
        
        private static void ApplyMoraleDamageReductionNearby(UnitStatus caster, float percent, int duration, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateMoraleDamageReduction(duration, percent, null));
            }
            Debug.Log($"Nearby allies take {percent*100}% less morale damage for {duration} turns");
        }
        
        private static void ApplyReducedRumEffectNearby(UnitStatus caster, float reduction, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateReducedRumEffect(99, reduction, null));
            }
            Debug.Log($"Nearby allies have reduced rum effects");
        }
        
        private static void ApplyEnemyBuzzOnDamage(UnitStatus caster, int duration)
        {
            var effects = GetStatusEffects(caster);
            effects?.ApplyEffect(StatusEffect.CreateEnemyBuzzOnDamage(duration, null));
            Debug.Log($"Enemies gain buzz when dealing damage for {duration} turns");
        }
        
        private static void ApplyPreventDisplacementNearby(UnitStatus caster, int duration, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreatePreventDisplacement(duration, null));
            }
            Debug.Log($"Nearby allies can't be displaced for {duration} turns");
        }
        
        private static void ApplyOnlyLowerHPCanTargetLowest(UnitStatus caster, int duration)
        {
            var lowestHP = GetLowestHPAlly(caster);
            if (lowestHP != null)
            {
                var effects = GetStatusEffects(lowestHP);
                effects?.ApplyEffect(StatusEffect.CreateOnlyLowerHPCanTarget(duration, null));
                Debug.Log($"{lowestHP.UnitName} can only be targeted by lower HP for {duration} turns");
            }
        }
        
        private static void ApplyDamageBoostToColumn(UnitStatus caster, float percent, int duration)
        {
            foreach (var ally in GetAlliesInColumn(caster))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateDamageBoost(duration, percent, null));
            }
            Debug.Log($"Column allies deal +{percent*100}% damage for {duration} turns");
        }
        
        private static void ApplyRowRangedProtection(UnitStatus caster, float reduction, int duration)
        {
            foreach (var ally in GetAlliesInRow(caster))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateRangedDamageReduction(duration, reduction, null));
            }
            Debug.Log($"Row takes {reduction*100}% less ranged damage for {duration} turns");
        }
        
        private static void ApplyNoMoraleDamageNearby(UnitStatus caster, int duration, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateMoraleDamageReduction(duration, 1f, null));
            }
            Debug.Log($"Nearby allies take no morale damage for {duration} turns");
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
            Debug.Log($"Knocked back nearby enemies {tiles} tiles");
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
            Debug.Log($"Enemy ranged weapons deal {reduction*100}% less damage for {duration} turns");
        }
        
        #endregion
        
        #region Resource Helpers
        
        private static void GenerateGrog(int amount)
        {
            var energyManager = ServiceLocator.Get<EnergyManager>();
            energyManager?.AddGrog(amount);
            Debug.Log($"Generated {amount} grog");
        }
        
        private static void ConvertGrogToEnergy(int grogAmount)
        {
            var energyManager = ServiceLocator.Get<EnergyManager>();
            if (energyManager != null && energyManager.TrySpendGrog(grogAmount))
            {
                // Energy is granted at turn start, so this would need special handling
                Debug.Log($"Converted {grogAmount} grog to energy");
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
                Debug.Log($"{unit.UnitName} drew {count} cards");
            }
            else
            {
                Debug.Log($"{unit.UnitName} tried to draw {count} cards but no deck manager");
            }
        }
        
        private static void DrawUltimateCard(UnitStatus unit)
        {
            var deckManager = BattleDeckManager.Instance;
            if (deckManager != null)
            {
                deckManager.DrawCardByCategory(unit, RelicCategory.Ultimate);
            }
            else
            {
                Debug.Log($"{unit.UnitName} tried to draw ultimate but no deck manager");
            }
        }
        
        private static void DrawWeaponRelicCard(UnitStatus unit)
        {
            var deckManager = BattleDeckManager.Instance;
            if (deckManager != null)
            {
                deckManager.DrawCardByCategory(unit, RelicCategory.Weapon);
            }
            else
            {
                Debug.Log($"{unit.UnitName} tried to draw weapon but no deck manager");
            }
        }
        
        private static void DrawBootsCard(UnitStatus unit)
        {
            var deckManager = BattleDeckManager.Instance;
            if (deckManager != null)
            {
                deckManager.DrawCardByCategory(unit, RelicCategory.Boots);
            }
            else
            {
                Debug.Log($"{unit.UnitName} tried to draw boots but no deck manager");
            }
        }
        
        private static void AddHighQualityRum(UnitStatus unit, int count)
        {
            Debug.Log($"Added {count} high quality rum (placeholder)");
        }
        
        #endregion
        
        #region Summon Helpers
        
        private static void SummonCannon(UnitStatus caster, GridCell cell, int hp)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            if (hazardManager == null) { Debug.LogWarning("No HazardManager found!"); return; }

            // Place cannon as a soft obstacle (destructible) at target cell or near caster
            GridCell spawnCell = cell;
            if (spawnCell == null || spawnCell.IsOccupied || spawnCell.HasHazard)
            {
                var cells = hazardManager.FindEmptyCellsNear(caster.transform.position, 1);
                spawnCell = cells.Count > 0 ? cells[0] : null;
            }
            if (spawnCell != null)
            {
                hazardManager.CreateSoftObstacle(spawnCell, hp, -1); // Permanent until destroyed
                Debug.Log($"<color=green>{caster.UnitName} summoned a cannon with {hp} HP!</color>");
            }
        }

        private static void SummonAnchor(UnitStatus caster, GridCell cell, float healthBoost, int range)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            if (hazardManager == null) { Debug.LogWarning("No HazardManager found!"); return; }

            GridCell spawnCell = cell;
            if (spawnCell == null || spawnCell.IsOccupied || spawnCell.HasHazard)
            {
                var cells = hazardManager.FindEmptyCellsNear(caster.transform.position, 1);
                spawnCell = cells.Count > 0 ? cells[0] : null;
            }
            if (spawnCell != null)
            {
                // Create healing zone around the anchor
                int healPerTurn = Mathf.RoundToInt(healthBoost * 100);
                hazardManager.CreateHealingZone(spawnCell, healPerTurn, 3);
                Debug.Log($"<color=green>{caster.UnitName} summoned an anchor with +{healthBoost*100}% health buff in {range} range!</color>");
            }
        }

        private static void SummonTargetDummy(UnitStatus caster, GridCell cell, int hp)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            if (hazardManager == null) { Debug.LogWarning("No HazardManager found!"); return; }

            GridCell spawnCell = cell;
            if (spawnCell == null || spawnCell.IsOccupied || spawnCell.HasHazard)
            {
                var cells = hazardManager.FindEmptyCellsNear(caster.transform.position, 1);
                spawnCell = cells.Count > 0 ? cells[0] : null;
            }
            if (spawnCell != null)
            {
                hazardManager.CreateSoftObstacle(spawnCell, hp, -1); // Permanent until destroyed
                Debug.Log($"<color=green>{caster.UnitName} summoned a target dummy with {hp} HP!</color>");
            }
        }

        private static void SummonObstacleAndDisplace(GridCell cell, UnitStatus target)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            var gridManager = ServiceLocator.Get<GridManager>();
            if (hazardManager == null || gridManager == null) return;

            if (target != null)
            {
                // Get target's current cell
                Vector2Int pos = gridManager.WorldToGridPosition(target.transform.position);
                var targetCell = gridManager.GetCell(pos.x, pos.y);

                // Find adjacent cell for displacement
                var adjacent = gridManager.GetCell(pos.x + 1, pos.y) ?? gridManager.GetCell(pos.x - 1, pos.y);

                if (adjacent != null && adjacent.CanPlaceUnit())
                {
                    // Displace the unit
                    if (targetCell != null) targetCell.RemoveUnit();
                    adjacent.PlaceUnit(target.gameObject);
                    target.transform.position = adjacent.GetWorldPosition();

                    // Spawn obstacle where the unit was
                    if (targetCell != null)
                    {
                        hazardManager.CreateHardObstacle(targetCell, 3);
                    }
                    Debug.Log($"<color=green>Summoned obstacle at ({pos.x},{pos.y}) and displaced {target.UnitName}!</color>");
                }
            }
            else if (cell != null)
            {
                hazardManager.CreateHardObstacle(cell, 3);
                Debug.Log($"<color=green>Summoned obstacle at ({cell.XPosition},{cell.YPosition})!</color>");
            }
        }

        private static void SummonExplodingBarrels(UnitStatus caster, int count, int delay)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            if (hazardManager == null) { Debug.LogWarning("No HazardManager found!"); return; }

            var emptyCells = hazardManager.FindEmptyCellsNear(caster.transform.position, count, 4);
            int placed = 0;
            foreach (var cell in emptyCells)
            {
                if (placed >= count) break;
                var barrel = hazardManager.CreateExplodingBarrel(cell, 150, delay);
                if (barrel != null) placed++;
            }
            Debug.Log($"<color=green>{caster.UnitName} summoned {placed} exploding barrels (fuse: {delay} turns)!</color>");
        }

        private static void SummonHardObstacles(UnitStatus caster, int count, int duration)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            var gridManager = ServiceLocator.Get<GridManager>();
            if (hazardManager == null || gridManager == null)
            {
                Debug.LogWarning("SummonHardObstacles: Missing HazardManager or GridManager!");
                return;
            }

            Vector2Int casterPos = gridManager.WorldToGridPosition(caster.transform.position);
            int middleCol = gridManager.GetMiddleColumnIndex();
            int gridHeight = gridManager.GridHeight;

            Debug.Log($"<color=cyan>SummonHardObstacles: Caster at ({casterPos.x}, {casterPos.y}), " +
                      $"scanning columns {casterPos.x + 1} to {middleCol} for {count} adjacent empty cells</color>");

            // Scan each column from caster's column + 1 toward the neutral zone (middleCol)
            // Never go past middleCol (that would be enemy zone)
            for (int col = casterPos.x + 1; col <= middleCol; col++)
            {
                // Find all empty cells in this column
                var emptyCellsInColumn = new System.Collections.Generic.List<GridCell>();
                for (int row = 0; row < gridHeight; row++)
                {
                    var cell = gridManager.GetCell(col, row);
                    if (cell != null && !cell.IsOccupied && !cell.IsBlocked && !cell.HasHazard && !cell.IsMiddleColumn)
                    {
                        emptyCellsInColumn.Add(cell);
                    }
                }

                if (emptyCellsInColumn.Count < count)
                    continue; // Not enough empty cells in this column

                // Find the best group of 'count' adjacent cells, centered on caster's Y
                GridCell bestStartCell = null;
                int bestDistance = int.MaxValue;

                for (int startIdx = 0; startIdx <= emptyCellsInColumn.Count - count; startIdx++)
                {
                    // Check if 'count' cells starting from startIdx are adjacent (consecutive Y values)
                    bool adjacent = true;
                    for (int i = 1; i < count; i++)
                    {
                        if (emptyCellsInColumn[startIdx + i].YPosition != emptyCellsInColumn[startIdx + i - 1].YPosition + 1)
                        {
                            adjacent = false;
                            break;
                        }
                    }

                    if (adjacent)
                    {
                        // Calculate center of this group and distance from caster's Y
                        int groupCenterY = emptyCellsInColumn[startIdx].YPosition + (count - 1) / 2;
                        int dist = Mathf.Abs(groupCenterY - casterPos.y);
                        if (dist < bestDistance)
                        {
                            bestDistance = dist;
                            bestStartCell = emptyCellsInColumn[startIdx];
                        }
                    }
                }

                if (bestStartCell != null)
                {
                    // Place obstacles at the best adjacent group
                    int placed = 0;
                    int startY = bestStartCell.YPosition;
                    for (int i = 0; i < count; i++)
                    {
                        var cell = gridManager.GetCell(col, startY + i);
                        if (cell != null)
                        {
                            var obstacle = hazardManager.CreateHardObstacle(cell, duration);
                            if (obstacle != null) placed++;
                        }
                    }

                    Debug.Log($"<color=green>{caster.UnitName} summoned {placed} hard obstacles at column {col}, " +
                              $"rows {startY}-{startY + count - 1} for {duration} turns!</color>");
                    return;
                }
            }

            // Fallback: no column found with enough adjacent empty cells
            Debug.LogWarning($"{caster.UnitName} couldn't find {count} adjacent empty cells in front! " +
                             $"(Checked columns {casterPos.x + 1} to {middleCol})");
        }
        
        #endregion
        
        #region Ultimate Helpers
        
        // Captain Ultimate V1 — Ship Cannon balance values.
        // (Spec only stores damage + shots; these are the fire-hazard knobs.)
        private const int SHIP_CANNON_FIRE_DPS = 25;
        private const int SHIP_CANNON_FIRE_DURATION = 2;

        private static void ExecuteShipCannonUltimate(UnitStatus caster, int damage, int shots)
        {
            if (caster == null || shots <= 0) return;

            var hazardManager = ServiceLocator.Get<HazardManager>();
            var gridManager = ServiceLocator.Get<GridManager>();

            var alreadyHit = new HashSet<UnitStatus>(); // Each enemy can only be hit once
            int hits = 0;

            for (int i = 0; i < shots; i++)
            {
                // Get living enemies that haven't been hit yet
                var eligible = GetEnemies(caster)
                    .Where(e => e != null && e.CurrentHP > 0 && !alreadyHit.Contains(e))
                    .ToList();
                if (eligible.Count == 0) break;

                var target = eligible[Random.Range(0, eligible.Count)];
                alreadyHit.Add(target);

                // Capture the target's tile BEFORE damage
                GridCell hitCell = null;
                if (gridManager != null)
                {
                    Vector2Int pos = gridManager.WorldToGridPosition(target.transform.position);
                    hitCell = gridManager.GetCell(pos.x, pos.y);
                }

                // HP-only damage — cannon shots don't affect morale
                target.TakeEnvironmentalDamage(damage, "ShipCannon");
                hits++;

                // Spawn a fire hazard on the hit tile
                if (hazardManager != null && hitCell != null)
                {
                    hazardManager.CreateFireTile(hitCell, SHIP_CANNON_FIRE_DPS, SHIP_CANNON_FIRE_DURATION);
                }
            }

            Debug.Log($"<color=orange>Ship cannon fired {hits}/{shots} shots for {damage} HP damage each (0 morale), hitting {hits} different enemies</color>");
        }
        
        private static void ExecuteMarkCaptainOnly(UnitStatus caster, UnitStatus target)
        {
            // Find enemy captain
            var enemies = GetEnemies(caster);
            var captain = enemies.FirstOrDefault(e => e.IsCaptain);
            
            if (captain != null)
            {
                ExecuteAttack(caster, captain);
                var effects = GetStatusEffects(captain);
                effects?.ApplyEffect(StatusEffect.CreateOnlyTargetThisTurn(1, null));
                Debug.Log($"Marked {captain.UnitName} as only target this turn");
            }
        }
        
        private static void ReviveAlly(UnitStatus caster, UnitStatus target, float healthPercent)
        {
            if (target != null && target.HasSurrendered && target.Team == caster.Team)
            {
                target.Heal(Mathf.RoundToInt(target.MaxHP * healthPercent));
                target.RestoreMorale(Mathf.RoundToInt(target.MaxMorale * healthPercent));
                
                // Remove surrendered state
                target.ClearSurrender();
                
                Debug.Log($"Revived exactly {target.UnitName} at {healthPercent*100}%");
                return;
            }

            // Fallback: Find first surrendered or dead ally if target isn't specified
            var allUnits = GameObject.FindGameObjectsWithTag("Untagged")
                .Select(go => go.GetComponent<UnitStatus>())
                .Where(u => u != null && u.HasSurrendered && u.Team == caster.Team)
                .ToList();
            
            if (allUnits.Count > 0)
            {
                var ally = allUnits[0];
                ally.Heal(Mathf.RoundToInt(ally.MaxHP * healthPercent));
                ally.RestoreMorale(Mathf.RoundToInt(ally.MaxMorale * healthPercent));
                
                // Remove surrendered state
                ally.ClearSurrender();
                
                Debug.Log($"Revived fallback {ally.UnitName} at {healthPercent*100}%");
            }
        }
        
        private static void ExecuteRumBottleAoE(UnitStatus caster, GridCell cell, int damage, int duration)
        {
            if (cell == null) return;
            
            // Damage units at target
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return;
            
            var unitsAtCell = GetAllUnits().Where(u => {
                Vector2Int pos = gridManager.WorldToGridPosition(u.transform.position);
                return pos.x == cell.XPosition && pos.y == cell.YPosition;
            });
            
            foreach (var unit in unitsAtCell)
            {
                unit.TakeDamage(damage, caster.gameObject, false);
            }
            
            Debug.Log($"Rum bottle AoE dealt {damage} damage, spill lasts {duration} turns");
        }
        
        private static void ApplyIgnoreHighestHP(UnitStatus caster, int duration)
        {
            var enemies = GetEnemies(caster).Where(e => !e.IsCaptain).ToList();
            if (enemies.Count > 0)
            {
                var highestHP = enemies.OrderByDescending(e => e.CurrentHP).First();
                var effects = GetStatusEffects(highestHP);
                effects?.ApplyEffect(StatusEffect.CreateIgnoredByEnemies(duration, null));
                Debug.Log($"{highestHP.UnitName} is ignored for {duration} turns");
            }
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
            return GetAllAllies(caster)
                .Where(a => a != caster)
                .OrderBy(a => a.MoralePercent)
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
            var enemies = GetEnemies(caster);
            if (enemies.Count < 2) return;
            
            var highest = enemies.OrderByDescending(e => e.Grit).First();
            var lowest = enemies.OrderBy(e => e.Grit).First();
            
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
            Debug.Log($"{unit.UnitName} gains morale on kill for {duration} turns");
        }
        
        private static void ApplyFreeMoveToAllAllies(UnitStatus caster)
        {
            foreach (var ally in GetAllAllies(caster))
            {
                ApplyFreeMove(ally);
            }
            Debug.Log("All allies can move free this turn");
        }
        
        private static void ApplyBuzzReduction(UnitStatus unit, float percent, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateBuzzGainReduction(duration, percent, null));
            Debug.Log($"{unit.UnitName} buzz gain reduced by {percent*100}% for {duration} turns");
        }
        
        private static void HealAdjacentAllies(UnitStatus caster, float percent)
        {
            foreach (var ally in GetAlliesInRange(caster, 1))
            {
                ally.Heal(Mathf.RoundToInt(ally.MaxHP * percent));
            }
            Debug.Log($"Healed adjacent allies {percent*100}%");
        }
        
        private static void ApplyDodgeChance(UnitStatus unit, float percent, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateDodge(duration, percent, null));
            Debug.Log($"{unit.UnitName} gains {percent*100}% dodge for {duration} turns");
        }
        
        // === Attack V2 Helpers ===
        private static void StealBuff(UnitStatus caster, UnitStatus target)
        {
            var targetEffects = GetStatusEffects(target);
            var casterEffects = GetStatusEffects(caster);
            if (targetEffects != null && casterEffects != null)
            {
                // Get a random buff from target and transfer to caster
                var buffs = targetEffects.GetActiveBuffs();
                if (buffs != null && buffs.Count > 0)
                {
                    var stolenBuff = buffs[UnityEngine.Random.Range(0, buffs.Count)];
                    targetEffects.RemoveEffect(stolenBuff);
                    casterEffects.ApplyEffect(stolenBuff);
                    Debug.Log($"{caster.UnitName} stole {stolenBuff.effectName} from {target.UnitName}");
                }
            }
        }
        
        private static void ForceDiscard(UnitStatus target, int count)
        {
            var deckManager = BattleDeckManager.Instance;
            if (deckManager == null)
            {
                Debug.Log($"{target.UnitName} forced to discard {count} cards (no deck manager)");
                return;
            }
            
            int discarded = deckManager.ForceDiscardFromUnit(target, count);
            Debug.Log($"{target.UnitName} forced to discard {discarded} cards");
        }
        
        private static void ApplySlow(UnitStatus unit, int reduction, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateSlow(duration, reduction, null));
            Debug.Log($"{unit.UnitName} slowed by {reduction} for {duration} turns");
        }
        
        private static void PullUnit(UnitStatus target, UnitStatus source, int tiles)
        {
            if (target == null || source == null) return;
            
            var effects = target.GetComponent<StatusEffectManager>();
            if (effects != null && !effects.CanBeKnockedBack())
            {
                Debug.Log($"{target.UnitName} cannot be pulled!");
                return;
            }
            
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
                Debug.Log($"Pulled {target.UnitName} by {tiles} tiles");
            }
        }
        
        private static void ApplyPoison(UnitStatus unit, int damagePerTurn, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreatePoison(duration, damagePerTurn, null));
            Debug.Log($"{unit.UnitName} poisoned for {damagePerTurn} dmg/turn for {duration} turns");
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
                Debug.Log($"Chain damage: {chainDamage} to {adjacent.UnitName}");
            }
        }
        
        // === Hat V2 Helpers ===
        private static void ApplyShieldBuff(UnitStatus unit, int amount)
        {
            unit.RestoreHull(amount);
            Debug.Log($"{unit.UnitName} gained {amount} shield");
        }
        
        private static void DrawBootsRelicCard(UnitStatus unit)
        {
            var deckManager = BattleDeckManager.Instance;
            if (deckManager != null)
            {
                deckManager.DrawCardByCategory(unit, RelicCategory.Boots);
            }
            else
            {
                Debug.Log($"{unit.UnitName} tried to draw boots but no deck manager");
            }
        }
        
        private static void RestoreMoraleToAllAllies(UnitStatus caster, float percent)
        {
            foreach (var ally in GetAllAllies(caster))
            {
                ally.RestoreMorale(Mathf.RoundToInt(ally.MaxMorale * percent));
            }
            Debug.Log($"Restored {percent*100}% morale to all allies");
        }
        
        private static void ApplyPreventMoraleLoss(UnitStatus caster, int duration)
        {
            foreach (var ally in GetAllAllies(caster))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateMoraleDamageReduction(duration, 1f, null));
            }
            Debug.Log($"Allies can't lose morale for {duration} turns");
        }
        
        private static void ApplyRumHealBoost(UnitStatus unit, float percent, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateRumHealBoost(duration, percent, null));
            Debug.Log($"{unit.UnitName} rum heals {percent*100}% more for {duration} turns");
        }
        
        private static void ApplyGrogOnKill(UnitStatus unit, int amount, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateGrogOnKill(duration, amount, null));
            Debug.Log($"{unit.UnitName} gains {amount} grog on kill for {duration} turns");
        }
        
        private static void ApplySpeedBoost(UnitStatus unit, int amount, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateSpeedBoost(duration, amount, null));
            Debug.Log($"{unit.UnitName} gains +{amount} movement for {duration} turns");
        }
        
        private static void ApplyHealOnCardPlay(UnitStatus unit, float percent, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateHealOnCardPlay(duration, percent, null));
            Debug.Log($"{unit.UnitName} heals {percent*100}% HP per card for {duration} turns");
        }
        
        private static void ApplyFoodEffectBoost(UnitStatus unit, float multiplier, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateFoodEffectBoost(duration, multiplier, null));
            Debug.Log($"{unit.UnitName} food effects x{multiplier} for {duration} turns");
        }
        
        private static void ApplyReduceAllCosts(UnitStatus unit, int reduction, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateReduceAllCosts(duration, reduction, null));
            Debug.Log($"{unit.UnitName} all card costs -{reduction} for {duration} turns");
        }
        
        // === Coat V2 Helpers ===
        private static void ShieldNearbyAllies(UnitStatus caster, int amount, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                ally.RestoreHull(amount);
            }
            Debug.Log($"Nearby allies gained {amount} shield");
        }
        
        private static void ApplyCounterOnAllyHit(UnitStatus unit, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateCounterOnAllyHit(duration, null));
            Debug.Log($"{unit.UnitName} will counter-attack when ally is hit for {duration} turns");
        }
        
        private static void ApplyMoraleShield(UnitStatus caster, int amount, int duration)
        {
            foreach (var ally in GetAllAllies(caster))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateMoraleShield(duration, amount, null));
            }
            Debug.Log($"Allies have {amount} morale shield for {duration} turns");
        }
        
        private static void ApplyDeathPrevention(UnitStatus caster, int duration)
        {
            foreach (var ally in GetAllAllies(caster))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateDeathPrevention(duration, null));
            }
            Debug.Log($"One ally death prevented for {duration} turns");
        }
        
        private static void ApplyBuzzImmunityNearby(UnitStatus caster, int duration, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateBuzzImmunity(duration, null));
            }
            Debug.Log($"Nearby allies immune to buzz effects for {duration} turns");
        }
        
        private static void ApplyThorns(UnitStatus unit, int damage, int duration)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateThorns(duration, damage, null));
            Debug.Log($"{unit.UnitName} reflects {damage} damage to attackers for {duration} turns");
        }
        
        private static void ApplyDodgeAuraNearby(UnitStatus caster, float percent, int duration, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                ApplyDodgeChance(ally, percent, duration);
            }
            Debug.Log($"Nearby allies gain {percent*100}% dodge for {duration} turns");
        }
        
        private static void ApplyHealingAuraNearby(UnitStatus caster, float percent, int duration, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateHealOverTime(duration, percent, null));
            }
            Debug.Log($"Nearby allies heal {percent*100}% at turn end for {duration} turns");
        }
        
        private static void ApplyMaxHPBoostNearby(UnitStatus caster, float percent, int duration, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateMaxHPBoost(duration, percent, null));
            }
            Debug.Log($"Nearby allies +{percent*100}% max HP for {duration} turns");
        }
        
        private static void ApplyRangedBlock(UnitStatus unit, int charges)
        {
            var effects = GetStatusEffects(unit);
            effects?.ApplyEffect(StatusEffect.CreateRangedBlock(99, charges, null));
            Debug.Log($"{unit.UnitName} blocks next {charges} ranged attacks");
        }
        
        // === Totem V2 Helpers ===
        private static void SummonHealingTotem(UnitStatus caster, GridCell cell, int healPerTurn, int duration)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            if (hazardManager != null)
            {
                hazardManager.CreateHealingZone(cell, healPerTurn, duration);
            }
            else
            {
                Debug.Log($"Summoned healing totem: heals {healPerTurn}/turn for {duration} turns (no HazardManager)");
            }
        }
        
        private static void ApplyWeaknessCurse(UnitStatus target, float percent, int duration)
        {
            var effects = GetStatusEffects(target);
            effects?.ApplyEffect(StatusEffect.CreateWeakness(duration, percent, null));
            Debug.Log($"{target.UnitName} cursed with -{percent*100}% damage for {duration} turns");
        }
        
        private static void ApplyDamageBoostNearby(UnitStatus caster, float percent, int duration, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreateDamageBoost(duration, percent, null));
            }
            Debug.Log($"Nearby allies +{percent*100}% damage for {duration} turns");
        }
        
        private static void SummonMoraleBanner(UnitStatus caster, GridCell cell, int duration, int range)
        {
            Debug.Log($"Summoned morale banner: prevents morale loss in {range} tiles for {duration} turns (placeholder)");
        }
        
        private static void SummonGrogBarrel(UnitStatus caster, GridCell cell, int grogAmount)
        {
            Debug.Log($"Summoned grog barrel: gives {grogAmount} grog when destroyed (placeholder)");
        }
        
        private static void PlaceTrap(GridCell cell, int stunDuration)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            if (hazardManager != null)
            {
                hazardManager.CreateTrap(cell, stunDuration);
            }
            else
            {
                Debug.Log($"Placed trap: stuns for {stunDuration} turns (no HazardManager)");
            }
        }
        
        private static void SummonShieldGenerator(UnitStatus caster, GridCell cell, int shieldPerTurn, int duration)
        {
            Debug.Log($"Summoned shield generator: gives {shieldPerTurn} shield/turn for {duration} turns (placeholder)");
        }
        
        private static void SummonSpeedBooster(UnitStatus caster, GridCell cell, int speedBonus, int duration)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            if (hazardManager != null)
            {
                hazardManager.CreateSpeedZone(cell, speedBonus, duration);
            }
            else
            {
                Debug.Log($"Summoned speed booster: +{speedBonus} movement for {duration} turns (no HazardManager)");
            }
        }
        
        private static void SummonHealingWell(UnitStatus caster, GridCell cell, float healPercent, int duration)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            if (hazardManager != null)
            {
                // Convert percent to flat heal amount based on caster's max HP
                int healAmount = Mathf.RoundToInt(caster.MaxHP * healPercent);
                hazardManager.CreateHealingZone(cell, healAmount, duration);
            }
            else
            {
                Debug.Log($"Summoned healing well: heals {healPercent*100}%/turn for {duration} turns (no HazardManager)");
            }
        }
        
        private static void CreatePoisonCloud(GridCell cell, int damagePerTurn, int duration, int range)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            if (hazardManager != null)
            {
                hazardManager.CreatePoisonCloud(cell, damagePerTurn, duration, range);
            }
            else
            {
                Debug.Log($"Created poison cloud (no HazardManager): {damagePerTurn} dmg/turn for {duration} turns");
            }
        }
        
        private static void CreatePoisonTile(GridCell cell, int damagePerTurn, int duration)
        {
            var hazardManager = ServiceLocator.Get<HazardManager>();
            if (hazardManager != null)
            {
                hazardManager.CreatePoisonTile(cell, damagePerTurn, duration);
            }
            else
            {
                Debug.Log($"Created poison tile (no HazardManager)");
            }
        }
        
        private static void SummonDecoy(UnitStatus caster, GridCell cell, int duration)
        {
            Debug.Log($"Summoned decoy: taunts enemies for {duration} turns (placeholder)");
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
            Debug.Log($"All allies +{percent*100}% damage and armor for {duration} turns");
        }
        
        private static void ExecuteEnemyBelowThreshold(UnitStatus caster, UnitStatus target, float threshold)
        {
            if (target != null && target.HPPercent < threshold)
            {
                target.TakeDamage(target.CurrentHP + 1, caster.gameObject, false);
                Debug.Log($"Executed {target.UnitName} below {threshold*100}% HP");
            }
        }
        
        private static void FullMoraleRestoreAllAllies(UnitStatus caster)
        {
            foreach (var ally in GetAllAllies(caster))
            {
                ally.RestoreMorale(ally.MaxMorale);
            }
            Debug.Log("Fully restored morale to all allies");
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
            Debug.Log($"Revived all dead allies at {healthPercent*100}%");
        }
        
        private static void BuzzExplosionAllEnemies(UnitStatus caster)
        {
            foreach (var enemy in GetEnemies(caster))
            {
                int buzzDamage = enemy.CurrentBuzz;
                enemy.TakeDamage(buzzDamage, caster.gameObject, false);
                // Fill their buzz
                var effects = GetStatusEffects(enemy);
                effects?.ApplyEffect(StatusEffect.CreateBuzzFilled(1, null));
            }
            Debug.Log("Buzz explosion: all enemies take damage equal to buzz");
        }
        
        private static void ShieldAllAllies(UnitStatus caster, int amount)
        {
            foreach (var ally in GetAllAllies(caster))
            {
                ally.RestoreHull(amount);
            }
            Debug.Log($"All allies gained {amount} shield");
        }
        
        private static void MassHealAllAllies(UnitStatus caster, float percent)
        {
            foreach (var ally in GetAllAllies(caster))
            {
                ally.Heal(Mathf.RoundToInt(ally.MaxHP * percent));
            }
            Debug.Log($"All allies healed {percent*100}%");
        }
        
        private static void FeastAllAllies(UnitStatus caster, float healthPercent, float moralePercent)
        {
            foreach (var ally in GetAllAllies(caster))
            {
                ally.Heal(Mathf.RoundToInt(ally.MaxHP * healthPercent));
                ally.RestoreMorale(Mathf.RoundToInt(ally.MaxMorale * moralePercent));
            }
            Debug.Log($"Feast: all allies healed {healthPercent*100}% HP and {moralePercent*100}% morale");
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
            Debug.Log($"Blade storm: {damage} damage to all enemies in range");
        }
        
        private static void ExecutePerfectShot(UnitStatus caster, UnitStatus target, float critMultiplier)
        {
            bool isMelee = caster.WeaponType == WeaponType.Melee;
            int baseDamage = isMelee 
                ? DamageCalculator.GetMeleeBaseDamage(caster)
                : DamageCalculator.GetRangedBaseDamage(caster);
            int critDamage = Mathf.RoundToInt(baseDamage * critMultiplier);
            
            // Ignore armor - deal to HP directly
            target.TakeDamage(critDamage, caster.gameObject, isMelee);
            Debug.Log($"Perfect shot: {critDamage} damage ignoring armor");
        }
        
        #endregion
    }
}