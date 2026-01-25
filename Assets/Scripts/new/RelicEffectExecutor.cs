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
                    BuffNearbyAlliesAimPower(caster, effect.value2, effect.tileRange);
                    break;
                    
                case RelicEffectType.Coat_DrawOnEnemyAttack:
                    ApplyDrawOnEnemyAttack(caster, 1, effect.duration);
                    break;
                    
                case RelicEffectType.Coat_ReduceMoraleDamage:
                    ApplyMoraleDamageReductionNearby(caster, effect.value2, effect.duration, effect.tileRange);
                    break;
                    
                case RelicEffectType.Coat_PreventSurrender:
                    ApplyPreventSurrender(caster, effect.value2, effect.duration);
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
                    ApplyCaptainDamageReflect(caster, effect.duration);
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
                    ReviveAlly(caster, effect.value2);
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
        
        private static void ExecuteSwapWithUnit(UnitStatus caster, UnitStatus target)
        {
            if (caster == null || target == null) return;
            
            Vector3 casterPos = caster.transform.position;
            Vector3 targetPos = target.transform.position;
            
            caster.transform.position = targetPos;
            target.transform.position = casterPos;
            
            Debug.Log($"Swapped {caster.UnitName} with {target.UnitName}");
        }
        
        private static void ExecuteMoveAlly(UnitStatus caster, UnitStatus ally, int tiles)
        {
            if (ally == null || ally.Team != caster.Team) return;
            
            var movement = ally.GetComponent<UnitMovement>();
            if (movement != null)
            {
                // Use RelicTargetSelector to let player choose destination
                RelicTargetSelector.Instance.SelectTile(
                    $"Select destination for {ally.UnitName} (up to {tiles} tiles)",
                    (destinationCell) => {
                        // Validate range
                        var gridManager = ServiceLocator.Get<GridManager>();
                        if (gridManager != null)
                        {
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
                                Debug.Log("Destination too far!");
                            }
                        }
                    },
                    () => Debug.Log("Movement cancelled"),
                    true // only empty tiles
                );
            }
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
        
        private static void BuffNearbyAlliesAimPower(UnitStatus caster, float percent, int range)
        {
            foreach (var ally in GetAlliesInRange(caster, range))
            {
                ApplyAimBoost(ally, percent, 1);
                var effects = GetStatusEffects(ally);
                effects?.ApplyEffect(StatusEffect.CreatePowerBoost(1, percent, null));
            }
            Debug.Log($"Buffed nearby allies +{percent*100}% Aim/Power");
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
            Debug.Log($"Summoned cannon with {hp} HP (placeholder)");
        }
        
        private static void SummonAnchor(UnitStatus caster, GridCell cell, float healthBoost, int range)
        {
            Debug.Log($"Summoned anchor with +{healthBoost*100}% health buff in {range} range (placeholder)");
        }
        
        private static void SummonTargetDummy(UnitStatus caster, GridCell cell, int hp)
        {
            Debug.Log($"Summoned target dummy with {hp} HP (placeholder)");
        }
        
        private static void SummonObstacleAndDisplace(GridCell cell, UnitStatus target)
        {
            if (target != null)
            {
                var gridManager = ServiceLocator.Get<GridManager>();
                if (gridManager == null) return;
                
                // Find adjacent cell for displacement
                Vector2Int pos = gridManager.WorldToGridPosition(target.transform.position);
                var adjacent = gridManager.GetCell(pos.x + 1, pos.y) ?? gridManager.GetCell(pos.x - 1, pos.y);
                
                if (adjacent != null && adjacent.CanPlaceUnit())
                {
                    target.transform.position = adjacent.GetWorldPosition();
                }
            }
            Debug.Log($"Summoned obstacle and displaced target (placeholder)");
        }
        
        private static void SummonExplodingBarrels(UnitStatus caster, int count, int delay)
        {
            Debug.Log($"Summoned {count} exploding barrels with {delay} turn delay (placeholder)");
        }
        
        private static void SummonHardObstacles(UnitStatus caster, int count, int duration)
        {
            Debug.Log($"Summoned {count} hard obstacles for {duration} turns (placeholder)");
        }
        
        #endregion
        
        #region Ultimate Helpers
        
        private static void ExecuteShipCannonUltimate(UnitStatus caster, int damage, int shots)
        {
            var enemies = GetEnemies(caster);
            for (int i = 0; i < shots && enemies.Count > 0; i++)
            {
                var target = enemies[Random.Range(0, enemies.Count)];
                target.TakeDamage(damage, caster.gameObject, false);
            }
            Debug.Log($"Ship cannon fired {shots} shots for {damage} damage each");
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
        
        private static void ReviveAlly(UnitStatus caster, float healthPercent)
        {
            // Find surrendered or dead allies
            var allUnits = GameObject.FindGameObjectsWithTag("Untagged")
                .Select(go => go.GetComponent<UnitStatus>())
                .Where(u => u != null && u.HasSurrendered && u.Team == caster.Team)
                .ToList();
            
            if (allUnits.Count > 0)
            {
                var ally = allUnits[0];
                ally.Heal(Mathf.RoundToInt(ally.MaxHP * healthPercent));
                ally.RestoreMorale(Mathf.RoundToInt(ally.MaxMorale * healthPercent));
                Debug.Log($"Revived {ally.UnitName} at {healthPercent*100}%");
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
            return GetEnemies(caster)
                .OrderBy(e => Vector3.Distance(caster.transform.position, e.transform.position))
                .FirstOrDefault();
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
                int distance = Mathf.Abs(casterPos.x - allyPos.x) + Mathf.Abs(casterPos.y - allyPos.y);
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
                int distance = Mathf.Abs(centerPos.x - enemyPos.x) + Mathf.Abs(centerPos.y - enemyPos.y);
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