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
    /// Handles all 192 relic effects (8 categories x 12 roles x 2 variants).
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
                // =====================================================================
                // BOOTS V1 (Original)
                // =====================================================================
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
                        caster.ReduceBuzz(caster.CurrentBuzz);
                    });
                    break;
                case RelicEffectType.Boots_FreeIfGrog:
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
                    
                // =====================================================================
                // BOOTS V2 (New Variant 2)
                // =====================================================================
                case RelicEffectType.Boots_MoveAnyAlly:
                    // Captain V2: Move 2 tiles any allied unit
                    ExecuteMoveAlly(caster, target, targetCell, 2);
                    break;
                case RelicEffectType.Boots_LowestMoraleAllyFree:
                    // Quartermaster V2: Lowest morale ally moves free this turn
                    ExecuteGrantFreeMove(GetLowestMoraleAlly(caster));
                    break;
                case RelicEffectType.Boots_FreeIfGrogAvailable:
                    // Helmsman V2: Move 2, free if grog available (cost handled by card system)
                    ExecuteMove(caster, targetCell, 2);
                    break;
                case RelicEffectType.Boots_MoveAnyDistanceHighHP:
                    // Boatswain V2: Move any distance if highest HP, else 2
                    int distV2 = IsHighestHP(caster) ? 99 : 2;
                    ExecuteMove(caster, targetCell, distV2);
                    break;
                case RelicEffectType.Boots_MoveGainGritStat:
                    // Shipwright V2: Move 2, +20% Grit for 2 turns
                    ExecuteMoveAndEffect(caster, targetCell, 2, () => {
                        ApplyGritBoost(caster, 0.2f, 2);
                    });
                    break;
                case RelicEffectType.Boots_MoveReduceRangedWeapon:
                    // MasterGunner V2: Move 1, reduce next ranged weapon cost by 1
                    ExecuteMoveAndEffect(caster, targetCell, 1, () => {
                        ApplyReduceNextRangedCost(caster, 1);
                    });
                    break;
                case RelicEffectType.Boots_MoveDestroyObstacle:
                    // MasterAtArms V2: Move to obstacle tile in 2 range, destroy it
                    ExecuteMoveToObstacleAndDestroy(caster, targetCell, 2);
                    break;
                case RelicEffectType.Boots_MoveFreeZeroCost:
                    // Navigator V2: Move 2 tiles, costs 0 energy
                    ExecuteMove(caster, targetCell, 2);
                    break;
                case RelicEffectType.Boots_SwapLowestHealthAlly:
                    // Surgeon V2: Swap with lowest health ally
                    ExecuteSwapWithUnit(caster, GetLowestHPAlly(caster));
                    break;
                case RelicEffectType.Boots_MoveGainProficiency:
                    // Cook V2: Move 2, +100% Proficiency this turn
                    ExecuteMoveAndEffect(caster, targetCell, 2, () => {
                        ApplyProficiencyBoost(caster, 1.0f, 1);
                    });
                    break;
                case RelicEffectType.Boots_MoveRowUnlimited:
                    // Swashbuckler V2: Move unlimited on row, 1 on column
                    ExecuteMoveRowUnlimitedColumn1(caster, targetCell);
                    break;
                case RelicEffectType.Boots_MoveRestoreHull:
                    // Deckhand V2: Move 2, restore 50 hull shield
                    ExecuteMoveAndEffect(caster, targetCell, 2, () => {
                        caster.RestoreHull(50);
                    });
                    break;

                // =====================================================================
                // GLOVES V1 (Original)
                // =====================================================================
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
                    int bonusDamage = Mathf.RoundToInt(50 * missingMoralePercent);
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
                        PushUnit(target, (int)effect.value2, true);
                    });
                    break;
                case RelicEffectType.Gloves_AttackForceTargetClosest:
                    ExecuteAttackWithEffect(caster, target, () => {
                        ApplyForceTargetClosest(target, effect.duration);
                    });
                    break;
                case RelicEffectType.Gloves_AttackBonusPerCardPlayed:
                    ExecuteAttack(caster, target);
                    break;
                case RelicEffectType.Gloves_AttackBonusPerGunnerRelic:
                    ExecuteAttack(caster, target);
                    break;
                    
                // =====================================================================
                // GLOVES V2 (New Variant 2)
                // =====================================================================
                case RelicEffectType.Gloves_AttackEnemyCostIncrease:
                    // Captain V2: Attack, enemy next card costs +1 energy
                    ExecuteAttackWithEffect(caster, target, () => {
                        ApplyIncreaseCost(target, 1, 1);
                    });
                    break;
                case RelicEffectType.Gloves_AttackMarkMoraleFocus2:
                    // Quartermaster V2: Attack, mark morale focus for 2 turns
                    ExecuteAttackWithEffect(caster, target, () => {
                        ApplyMoraleFocusMark(target, 2);
                    });
                    break;
                case RelicEffectType.Gloves_AttackBonusPerGrogToken:
                    // Helmsman V2: Attack, +20% per grog token
                    int grogBonusV2 = Mathf.RoundToInt(energyManager.GrogTokens * 0.2f * 100);
                    ExecuteAttackWithBonusDamage(caster, target, grogBonusV2);
                    break;
                case RelicEffectType.Gloves_AttackLowerHealthStat:
                    // Boatswain V2: Attack, lower enemy health stat 30% for 2 turns
                    ExecuteAttackWithEffect(caster, target, () => {
                        ApplyHealthStatReduction(target, 0.3f, 2);
                    });
                    break;
                case RelicEffectType.Gloves_AttackForceTargetNearest:
                    // Shipwright V2: Attack, debuff forces attack closest next turn
                    ExecuteAttackWithEffect(caster, target, () => {
                        ApplyForceTargetClosest(target, 1);
                    });
                    break;
                case RelicEffectType.Gloves_AttackBonusPerGunnerUsed:
                    // MasterGunner V2: Attack, +10% per gunner relic used this game
                    ExecuteAttackWithBonusDamage(caster, target, GetGunnerRelicsUsedBonus());
                    break;
                case RelicEffectType.Gloves_AttackBonusPerMArmCard:
                    // MasterAtArms V2: Attack, +10% per Master-at-Arms card in hand
                    ExecuteAttackWithBonusDamage(caster, target, GetMasterAtArmsCardsBonus());
                    break;
                case RelicEffectType.Gloves_AttackBonusPerBootsCard:
                    // Navigator V2: Attack, +30% per boots card in deck
                    ExecuteAttackWithBonusDamage(caster, target, GetBootsCardsInDeckBonus());
                    break;
                case RelicEffectType.Gloves_AttackOnEnemyHeal:
                    // Surgeon V2: Passive - attack any enemy that gets healed (registered elsewhere)
                    Debug.Log($"<color=cyan>Passive: {caster.name} will attack enemies that get healed</color>");
                    break;
                case RelicEffectType.Gloves_AttackStasisTarget:
                    // Cook V2: Put closest target in stasis for 1 turn
                    var closestTarget = GetClosestEnemy(caster);
                    if (closestTarget != null)
                        ApplyStasis(closestTarget, 1);
                    break;
                case RelicEffectType.Gloves_AttackTwice:
                    // Swashbuckler V2: Attack with default weapon 2 times
                    ExecuteAttack(caster, target);
                    ExecuteAttack(caster, target);
                    break;
                case RelicEffectType.Gloves_AttackHullDestroyEnergy:
                    // Deckhand V2: Attack, if hull destroyed gain 1 energy
                    ExecuteAttackWithEffect(caster, target, () => {
                        if (target.CurrentHullPool <= 0)
                            energyManager.TrySpendEnergy(-1); // Gain 1 energy
                    });
                    break;

                // =====================================================================
                // HAT V1 (Original)
                // =====================================================================
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
                    
                // =====================================================================
                // HAT V2 (New Variant 2)
                // =====================================================================
                case RelicEffectType.Hat_DrawUltimateAbility:
                    // Captain V2: Draw an ultimate ability
                    DrawUltimateCard(caster);
                    break;
                case RelicEffectType.Hat_RestoreMoraleNearbyAllies:
                    // Quartermaster V2: 10% morale to allies in 1 tile range
                    foreach (var ally in GetAlliesInRange(caster, 1))
                        ally.RestoreMorale(Mathf.RoundToInt(ally.MaxMorale * 0.1f));
                    break;
                case RelicEffectType.Hat_GenerateGrogTokens:
                    // Helmsman V2: Generate 2 grog tokens
                    energyManager.AddGrog(2);
                    break;
                case RelicEffectType.Hat_IncreaseHealthStatBuff:
                    // Boatswain V2: +25% health stat for 2 turns
                    ApplyHealthStatBoost(caster, 0.25f, 2);
                    break;
                case RelicEffectType.Hat_SwapEnemyGritPositions:
                    // Shipwright V2: Swap highest/lowest grit enemy positions
                    ExecuteSwapEnemiesByGrit(caster);
                    break;
                case RelicEffectType.Hat_DrawWeaponRelicCard:
                    // MasterGunner V2: Draw a weapon relic card
                    DrawWeaponRelicCard(caster);
                    break;
                case RelicEffectType.Hat_IncreaseEnemyWeaponCost:
                    // MasterAtArms V2: Enemy next weapon relic costs +1
                    ApplyEnemyWeaponCostIncrease(1);
                    break;
                case RelicEffectType.Hat_DrawBootsRelicCard:
                    // Navigator V2: Draw a boots relic card
                    DrawBootsRelicCard(caster);
                    break;
                case RelicEffectType.Hat_HealOnCaptainDamage:
                    // Surgeon V2: Allies heal 10% when damaging enemy captain
                    ApplyHealOnCaptainDamage(0.1f);
                    break;
                case RelicEffectType.Hat_MoveForwardAndHeal:
                    // Cook V2: Move ally forward 1 tile, heal 10%
                    if (target != null)
                    {
                        PushUnit(target, 1, true);
                        target.Heal(Mathf.RoundToInt(target.MaxHP * 0.1f));
                    }
                    break;
                case RelicEffectType.Hat_StealEnemyCard:
                    // Swashbuckler V2: Steal random enemy card, reduce weapon cost
                    StealEnemyCard(caster);
                    break;
                case RelicEffectType.Hat_DestroyObstaclesGainHull:
                    // Deckhand V2: Destroy soft obstacles, +20% hull each
                    DestroyAllSoftObstaclesAndGainHull(caster, 0.2f);
                    break;

                // =====================================================================
                // COAT V1 (Original)
                // =====================================================================
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
                    
                // =====================================================================
                // COAT V2 (New Variant 2)
                // =====================================================================
                case RelicEffectType.Coat_DrawOnEnemyAttackDiscard:
                    // Captain V2: Per enemy attack (3 max), draw card, enemy discards
                    ApplyDrawOnEnemyAttackDiscard(caster, 3, 2);
                    break;
                case RelicEffectType.Coat_PreventSurrenderRestore:
                    // Quartermaster V2: If ally would surrender, restore 20% morale
                    ApplyPreventSurrender(target ?? caster, 0.2f, 2);
                    break;
                case RelicEffectType.Coat_EnemyBuzzOnDealDamage:
                    // Helmsman V2: Enemy buzz fills every time they deal damage
                    ApplyEnemyBuzzOnDamage(1);
                    break;
                case RelicEffectType.Coat_ProtectLowestHP:
                    // Boatswain V2: Lowest HP only targeted by lower HP enemies
                    var lowestHPV2 = GetLowestHPAlly(caster);
                    if (lowestHPV2 != null)
                        ApplyOnlyLowerHPCanTarget(lowestHPV2, 1);
                    break;
                case RelicEffectType.Coat_ColumnDamageBoostAllies:
                    // Shipwright V2: +40% damage to allies in same column
                    foreach (var ally in GetAlliesInColumn(caster))
                        ApplyDamageBoost(ally, 0.4f, 1);
                    break;
                case RelicEffectType.Coat_RowRangedProtect:
                    // MasterGunner V2: Row takes 50% less ranged damage next turn
                    foreach (var ally in GetAlliesInRow(caster))
                        ApplyRangedDamageReduction(ally, 0.5f, 1);
                    break;
                case RelicEffectType.Coat_NearbyDamageBoost:
                    // MasterAtArms V2: +20% damage to nearby allies in 1 tile
                    foreach (var ally in GetAlliesInRange(caster, 1))
                        ApplyDamageBoost(ally, 0.2f, 1);
                    break;
                case RelicEffectType.Coat_DodgeFirstAttack:
                    // Navigator V2: First attacked ally dodges by moving 1 tile back
                    ApplyDodgeFirstAttack(caster);
                    break;
                case RelicEffectType.Coat_KnockbackOnAllyDeath:
                    // Surgeon V2: Knockback enemy 1 tile when ally dies nearby (passive)
                    Debug.Log($"<color=cyan>Passive: Knockback enemy when ally dies nearby</color>");
                    break;
                case RelicEffectType.Coat_ClearDebuffsNearby:
                    // Cook V2: Clear all debuffs from nearby allies
                    foreach (var ally in GetAlliesInRange(caster, 1))
                        ClearDebuffs(ally);
                    break;
                case RelicEffectType.Coat_CurseTileTrapped:
                    // Swashbuckler V2: Curse random enemy tile, trap + 10% more damage
                    CurseRandomEnemyTile();
                    break;
                case RelicEffectType.Coat_BuffRandomTile:
                    // Deckhand V2: Buff tile, units take 15% less, deal 15% more
                    BuffRandomTile();
                    break;

                // =====================================================================
                // TRINKET V1 (Original - Passive, registered elsewhere)
                // =====================================================================
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
                    Debug.Log($"<color=magenta>Passive trinket effect: {effect.effectType}</color>");
                    break;
                    
                // =====================================================================
                // TRINKET V2 (New Variant 2 - Passive)
                // =====================================================================
                case RelicEffectType.Trinket_BonusVsCaptainTarget:
                case RelicEffectType.Trinket_EnemySurrenderAt30:
                case RelicEffectType.Trinket_KnockbackFillsBuzz:
                case RelicEffectType.Trinket_DrawIfHighHealth:
                case RelicEffectType.Trinket_KnockbackAttackerOnce:
                case RelicEffectType.Trinket_RowEnemiesTakeMoreDmg:
                case RelicEffectType.Trinket_NearbyAlliesPowerBuff:
                case RelicEffectType.Trinket_NearbyIgnoreObstacles:
                case RelicEffectType.Trinket_GlobalAllyRadius:
                case RelicEffectType.Trinket_DrawIfLowHP:
                case RelicEffectType.Trinket_EnemiesLoseSpeed:
                case RelicEffectType.Trinket_HullRegenOnSurvive:
                    Debug.Log($"<color=magenta>Passive trinket V2 effect: {effect.effectType}</color>");
                    break;

                // =====================================================================
                // TOTEM V1 (Original)
                // =====================================================================
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
                        energyManager.TrySpendEnergy(-(int)effect.value2);
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
                    
                // =====================================================================
                // TOTEM V2 (New Variant 2)
                // =====================================================================
                case RelicEffectType.Totem_CurseCaptainDamageReflect:
                    // Captain V2: Captain damage reflects to all enemy allies
                    ApplyCaptainDamageReflectToAll(caster, 1);
                    break;
                case RelicEffectType.Totem_EnemyDeathMoraleSwap:
                    // Quartermaster V2: Enemy death = enemies lose morale, allies gain (passive)
                    Debug.Log($"<color=yellow>Passive: Enemy death causes morale swap</color>");
                    break;
                case RelicEffectType.Totem_ConvertGrogToEnergyFree:
                    // Helmsman V2: Convert 2 grog to 1 energy (0 cost)
                    if (energyManager.TrySpendGrog(2))
                        energyManager.TrySpendEnergy(-1);
                    break;
                case RelicEffectType.Totem_SummonAnchorHealthAura:
                    // Boatswain V2: Summon anchor, +25% health to nearby for 2 turns
                    SummonAnchor(caster, 0.25f, 2, 1);
                    break;
                case RelicEffectType.Totem_SummonObstacleDisplaceTarget:
                    // Shipwright V2: Summon obstacle at target, displace target
                    SummonObstacleAndDisplace(target, targetCell);
                    break;
                case RelicEffectType.Totem_CurseRangedWeaponsDamage:
                    // MasterGunner V2: Enemy ranged weapons -50% damage
                    ApplyCurseRangedWeapons(0.5f, 1);
                    break;
                case RelicEffectType.Totem_EarthquakeHazards:
                    // MasterAtArms V2: 3 random earthquake hazards
                    SummonEarthquakeHazards(3);
                    break;
                case RelicEffectType.Totem_DisableEnemyRelics:
                    // Navigator V2: Disable enemy non-weapon relics for 1 turn
                    DisableEnemyNonWeaponRelics(1);
                    break;
                case RelicEffectType.Totem_SummonHealingPotions:
                    // Surgeon V2: Summon 3 healing potions in random tiles
                    SummonHealingPotions(3, 200);
                    break;
                case RelicEffectType.Totem_SummonDebuffObstacle:
                    // Cook V2: Summon obstacle that reduces nearby enemy stats 50%
                    SummonDebuffObstacle(0.5f);
                    break;
                case RelicEffectType.Totem_DisableEnemyPassives:
                    // Swashbuckler V2: Enemies can't use passives next turn
                    DisableEnemyPassives(1);
                    break;
                case RelicEffectType.Totem_PullEnemiestoRow:
                    // Deckhand V2: Pull nearby enemies to same row
                    PullEnemiestoSameRow(caster, 1);
                    break;

                // =====================================================================
                // ULTIMATE V1 (Original)
                // =====================================================================
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
                        target.ReduceBuzz(-target.MaxBuzz);
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
                    
                // =====================================================================
                // ULTIMATE V2 (New Variant 2)
                // =====================================================================
                case RelicEffectType.Ultimate_AttackCaptainMark:
                    // Captain V2: Attack captain, mark as only target this turn
                    var enemyCaptain = GetEnemyCaptain(caster);
                    if (enemyCaptain != null)
                    {
                        ExecuteAttack(caster, enemyCaptain);
                        ApplyOnlyTargetThisTurn(enemyCaptain);
                    }
                    break;
                case RelicEffectType.Ultimate_ReviveAllyFull:
                    // Quartermaster V2: Revive dead/surrendered at 30% HP/morale
                    ReviveAlly(caster, 0.3f);
                    break;
                case RelicEffectType.Ultimate_RumBottleAoEBuzz:
                    // Helmsman V2: 200 dmg AoE, rum spill increases buzz
                    ExecuteRumBottleAoE(caster, targetCell, 200, 3, 1);
                    break;
                case RelicEffectType.Ultimate_IgnoreHighestHPEnemy:
                    // Boatswain V2: Highest HP enemy (not captain) ignored
                    ApplyIgnoreHighestHPNotCaptain(1);
                    break;
                case RelicEffectType.Ultimate_AttackKnockbackNearbyAll:
                    // Shipwright V2: Attack, knockback all nearby enemies 1 tile
                    ExecuteAttackWithEffect(caster, target, () => {
                        foreach (var nearby in GetEnemiesInRange(target, 1))
                            PushUnit(nearby, 1, false);
                    });
                    break;
                case RelicEffectType.Ultimate_MassiveSingleTargetBonus:
                    // MasterGunner V2: +300% damage if no nearby enemies
                    var nearbyV2 = GetEnemiesInRange(target, 1);
                    float multV2 = nearbyV2.Count == 0 ? 4.0f : 1f;
                    ExecuteAttackWithBonusDamage(caster, target, Mathf.RoundToInt(50 * multV2));
                    break;
                case RelicEffectType.Ultimate_AttackAllEnemiesRow:
                    // MasterAtArms V2: Attack closest + 350 damage to whole row
                    var closest = GetClosestEnemy(caster);
                    if (closest != null)
                    {
                        ExecuteAttack(caster, closest);
                        DamageEntireRow(closest, 350);
                    }
                    break;
                case RelicEffectType.Ultimate_SwapClosestFurthest:
                    // Navigator V2: Swap closest and furthest enemy positions
                    SwapClosestAndFurthestEnemy(caster);
                    break;
                case RelicEffectType.Ultimate_FullHealthRestore:
                    // Surgeon V2: Fully restore any unit's health
                    if (target != null)
                        target.Heal(target.MaxHP);
                    break;
                case RelicEffectType.Ultimate_SetColumnOnFire:
                    // Cook V2: Set closest target's whole column on fire
                    var closestV2 = GetClosestEnemy(caster);
                    if (closestV2 != null)
                        SetColumnOnFire(closestV2);
                    break;
                case RelicEffectType.Ultimate_FourWeaponsSurrender:
                    // Swashbuckler V2: Passive - 4 weapons on same target = surrender
                    Debug.Log($"<color=purple>Passive Ultimate: 4 weapons on same target = instant surrender</color>");
                    break;
                case RelicEffectType.Ultimate_ClearHazardsPrevent:
                    // Deckhand V2: Clear all hazards, prevent new ones
                    ClearAllHazardsOnPlayerSide();
                    PreventHazardsNextTurn();
                    break;

                // =====================================================================
                // PASSIVE UNIQUE V1 (handled elsewhere)
                // =====================================================================
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
                    Debug.Log($"<color=purple>Passive Unique V1: {effect.effectType}</color>");
                    break;
                    
                // =====================================================================
                // PASSIVE UNIQUE V2 (New Variant 2)
                // =====================================================================
                case RelicEffectType.PassiveUnique_ExtraCardsEachTurn:
                    // Captain V2: +2 cards each turn
                    Debug.Log($"<color=purple>Passive: +2 cards each turn</color>");
                    break;
                case RelicEffectType.PassiveUnique_LowerAllySurrender:
                    // Quartermaster V2: Allies surrender at 10% morale
                    Debug.Log($"<color=purple>Passive: Allies surrender at 10% morale</color>");
                    break;
                case RelicEffectType.PassiveUnique_DrawPerGrogToken:
                    // Helmsman V2: Draw extra cards per grog token
                    Debug.Log($"<color=purple>Passive: Draw extra per grog</color>");
                    break;
                case RelicEffectType.PassiveUnique_CounterAttackAlly:
                    // Boatswain V2: Attack back when any ally takes damage
                    Debug.Log($"<color=purple>Passive: Counter when ally damaged</color>");
                    break;
                case RelicEffectType.PassiveUnique_BonusVsLowGritTarget:
                    // Shipwright V2: +20% vs targets with lower grit
                    Debug.Log($"<color=purple>Passive: +20% vs low grit</color>");
                    break;
                case RelicEffectType.PassiveUnique_BonusVsLowHPTarget:
                    // MasterGunner V2: Bonus damage vs <50% HP targets
                    Debug.Log($"<color=purple>Passive: Bonus vs low HP</color>");
                    break;
                case RelicEffectType.PassiveUnique_KillRestoreHealth:
                    // MasterAtArms V2: Kill/surrender restores 20% health
                    Debug.Log($"<color=purple>Passive: Kill restores 20% HP</color>");
                    break;
                case RelicEffectType.PassiveUnique_AllAlliesExtraMove:
                    // Navigator V2: All allies can move +1 tile
                    Debug.Log($"<color=purple>Passive: All allies +1 movement</color>");
                    break;
                case RelicEffectType.PassiveUnique_KillRestoreAllyHP:
                    // Surgeon V2: Kill/surrender restores 5% HP to all allies
                    Debug.Log($"<color=purple>Passive: Kill heals all allies 5%</color>");
                    break;
                case RelicEffectType.PassiveUnique_RelicsNotConsumed:
                    // Cook V2: Relics not consumed, can replay if energy allows
                    Debug.Log($"<color=purple>Passive: Relics can be replayed</color>");
                    break;
                case RelicEffectType.PassiveUnique_EnemyBootsLimited:
                    // Swashbuckler V2: Enemies can only move 1 tile with boots
                    Debug.Log($"<color=purple>Passive: Enemies limited to 1 tile movement</color>");
                    break;
                case RelicEffectType.PassiveUnique_BonusDmgPerHullDestroyed:
                    // Deckhand V2: +30% weapon damage per hull destroyed
                    Debug.Log($"<color=purple>Passive: +30% per hull destroyed</color>");
                    break;
                    
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
            
            GridCell currentCell = gridManager.GetCell(currentPos.x, currentPos.y);
            if (currentCell != null) currentCell.RemoveUnit();
            
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
        
        private static void ExecuteMoveToObstacleAndDestroy(UnitStatus caster, GridCell targetCell, int maxRange)
        {
            if (targetCell == null) return;
            var gridManager = ServiceLocator.Get<GridManager>();
            
            // Check if target is blocked (obstacle/hazard) and not occupied
            if (targetCell.IsBlocked && !targetCell.IsOccupied)
            {
                Vector2Int currentPos = gridManager.WorldToGridPosition(caster.transform.position);
                int dist = Mathf.Abs(currentPos.x - targetCell.XPosition) + Mathf.Abs(currentPos.y - targetCell.YPosition);
                
                if (dist <= maxRange)
                {
                    // Clear hazard if present
                    if (targetCell.HasHazard)
                    {
                        targetCell.ClearHazard();
                    }
                    targetCell.isBlockedState = false;
                    ExecuteMove(caster, targetCell, maxRange);
                    Debug.Log($"<color=green>Cleared blocked tile and moved</color>");
                }
            }
            else
            {
                // Just move normally if not blocked
                ExecuteMove(caster, targetCell, maxRange);
            }
        }
        
        private static void ExecuteMoveRowUnlimitedColumn1(UnitStatus caster, GridCell targetCell)
        {
            if (targetCell == null) return;
            var gridManager = ServiceLocator.Get<GridManager>();
            Vector2Int currentPos = gridManager.WorldToGridPosition(caster.transform.position);
            
            int rowDiff = Mathf.Abs(targetCell.YPosition - currentPos.y);
            int colDiff = Mathf.Abs(targetCell.XPosition - currentPos.x);
            
            // Can move unlimited on row OR 1 tile on column
            if (rowDiff == 0 || (colDiff <= 1 && rowDiff <= 1))
            {
                ExecuteMove(caster, targetCell, 99);
            }
            else
            {
                Debug.Log("Invalid move: can only move unlimited on row or 1 tile on column");
            }
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
            GridCell targetCellObj = gridManager.GetCell(targetGrid.x, targetGrid.y);
            
            caster.transform.position = targetPos;
            target.transform.position = casterPos;
            
            if (casterCell != null) casterCell.PlaceUnit(target.gameObject);
            if (targetCellObj != null) targetCellObj.PlaceUnit(caster.gameObject);
            
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
            Debug.Log($"<color=green>{unit.name} granted free move</color>");
        }
        
        private static void PushUnit(UnitStatus unit, int tiles, bool forward)
        {
            if (unit == null) return;
            
            var gridManager = ServiceLocator.Get<GridManager>();
            Vector2Int pos = gridManager.WorldToGridPosition(unit.transform.position);
            
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
            
            UnitAttack attackComponent = attacker.GetComponent<UnitAttack>();
            if (attackComponent != null)
            {
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
        
        private static void DamageEntireRow(UnitStatus target, int damage)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            Vector2Int pos = gridManager.WorldToGridPosition(target.transform.position);
            
            var allUnits = GameObject.FindGameObjectsWithTag("Unit")
                .Select(go => go.GetComponent<UnitStatus>())
                .Where(u => u != null && !u.HasSurrendered);
                
            foreach (var unit in allUnits)
            {
                Vector2Int unitPos = gridManager.WorldToGridPosition(unit.transform.position);
                if (unitPos.y == pos.y && unit != target)
                {
                    unit.TakeDamage(damage, null, false);
                }
            }
            Debug.Log($"<color=red>Damaged entire row for {damage}</color>");
        }
        
        #endregion
        #region Status Effect Helpers
        
        private static void ApplyDamageReduction(UnitStatus unit, float percent, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} gains {percent*100}% damage reduction for {duration} turns</color>");
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
        
        private static void ApplyProficiencyBoost(UnitStatus unit, float percent, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} gains {percent*100}% Proficiency boost for {duration} turns</color>");
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
            Debug.Log($"<color=blue>{unit.name} has {reduction*100}% reduced rum effect for {duration} turns</color>");
        }
        
        private static void ApplyEnemyBuzzOnDamage(int duration)
        {
            Debug.Log($"<color=yellow>Enemy buzz fills when dealing damage for {duration} turns</color>");
        }
        
        private static void ApplyPreventDisplacement(UnitStatus unit, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} cannot be displaced for {duration} turns</color>");
        }
        
        private static void ApplyOnlyLowerHPCanTarget(UnitStatus unit, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} can only be targeted by lower HP enemies for {duration} turns</color>");
        }
        
        private static void ApplyRowCantBeTargeted(UnitStatus unit, int duration)
        {
            Debug.Log($"<color=blue>Allies behind {unit.name} in row can't be targeted for {duration} turns</color>");
        }
        
        private static void ApplyRangedDamageReduction(UnitStatus unit, float percent, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} takes {percent*100}% less ranged damage for {duration} turns</color>");
        }
        
        private static void ApplyStasis(UnitStatus unit, int duration)
        {
            Debug.Log($"<color=cyan>{unit.name} in stasis for {duration} turns - can't attack or be attacked</color>");
        }
        
        private static void ApplyStunOnKnockback(UnitStatus unit, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} will stun attacker if knocked back for {duration} turns</color>");
        }
        
        private static void ApplyCaptainDamageReflect(UnitStatus caster, int duration)
        {
            Debug.Log($"<color=red>Enemy captain damage reflects to allies for {duration} turns</color>");
        }
        
        private static void ApplyCaptainDamageReflectToAll(UnitStatus caster, int duration)
        {
            Debug.Log($"<color=red>Enemy captain damage reflects to ALL enemy allies for {duration} turns</color>");
        }
        
        private static void ApplyNoMoraleDamage(UnitStatus unit, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} takes no morale damage for {duration} turns</color>");
        }
        
        private static void ApplyCurseRangedWeapons(float reduction, int duration)
        {
            Debug.Log($"<color=red>Enemy ranged weapons do {reduction*100}% less damage for {duration} turns</color>");
        }
        
        private static void ApplyDamageReturn(UnitStatus unit, int instances, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} returns {instances} damage instances for {duration} turns</color>");
        }
        
        private static void ApplyHealthStatBoost(UnitStatus unit, float percent, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} health stat +{percent*100}% for {duration} turns</color>");
        }
        
        private static void ApplyEnergyOnKnockback(UnitStatus unit, int energy, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} gains {energy} energy if knocked back for {duration} turns</color>");
        }
        
        private static void ApplyWeaponUseTwice(UnitStatus unit)
        {
            Debug.Log($"<color=blue>{unit.name} can use next weapon twice</color>");
        }
        
        private static void ApplyReflectMoraleDamage(int duration)
        {
            Debug.Log($"<color=purple>Ally morale damage reflects to enemies for {duration} turns</color>");
        }
        
        private static void ApplyIgnoreHighestHP(int duration)
        {
            Debug.Log($"<color=purple>Highest HP enemy ignored for {duration} turns</color>");
        }
        
        private static void ApplyIgnoreHighestHPNotCaptain(int duration)
        {
            Debug.Log($"<color=purple>Highest HP enemy (not captain) ignored for {duration} turns</color>");
        }
        
        private static void ApplyOnlyTargetThisTurn(UnitStatus target)
        {
            Debug.Log($"<color=purple>{target.name} is the only valid target this turn</color>");
        }
        
        private static void ApplyDrawOnEnemyAttack(UnitStatus unit, int maxDraws, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} draws on enemy attack (max {maxDraws}) for {duration} turns</color>");
        }
        
        private static void ApplyDrawOnEnemyAttackDiscard(UnitStatus unit, int maxDraws, int duration)
        {
            Debug.Log($"<color=blue>{unit.name} draws on enemy attack (max {maxDraws}), enemy discards, for {duration} turns</color>");
        }
        
        private static void ApplyFreeStows(int count)
        {
            Debug.Log($"<color=blue>Next {count} stows are free</color>");
        }
        
        private static void ApplyDodgeFirstAttack(UnitStatus caster)
        {
            Debug.Log($"<color=blue>First attacked ally will dodge by moving back 1 tile</color>");
        }
        
        private static void ClearDebuffs(UnitStatus unit)
        {
            Debug.Log($"<color=green>Cleared all debuffs from {unit.name}</color>");
        }
        
        private static void ApplyEnemyWeaponCostIncrease(int amount)
        {
            Debug.Log($"<color=red>Enemy next weapon relic costs +{amount}</color>");
        }
        
        private static void ApplyHealOnCaptainDamage(float percent)
        {
            Debug.Log($"<color=green>Allies heal {percent*100}% when damaging enemy captain</color>");
        }
        
        private static void DisableEnemyNonWeaponRelics(int duration)
        {
            Debug.Log($"<color=red>Enemy non-weapon relics disabled for {duration} turns</color>");
        }
        
        private static void DisableEnemyPassives(int duration)
        {
            Debug.Log($"<color=red>Enemy passives disabled for {duration} turns</color>");
        }
        
        #endregion
        
        #region Card/Resource Helpers
        
        private static void DrawCards(int count, UnitStatus caster)
        {
            Debug.Log($"<color=cyan>Drew {count} cards</color>");
        }
        
        private static void DrawUltimateCard(UnitStatus caster)
        {
            Debug.Log($"<color=cyan>Drew an ultimate ability card</color>");
        }
        
        private static void DrawWeaponRelicCard(UnitStatus caster)
        {
            Debug.Log($"<color=cyan>Drew a weapon relic card</color>");
        }
        
        private static void DrawBootsRelicCard(UnitStatus caster)
        {
            Debug.Log($"<color=cyan>Drew a boots relic card</color>");
        }
        
        private static void StealEnemyCard(UnitStatus caster)
        {
            Debug.Log($"<color=cyan>Stole random enemy card</color>");
        }
        
        private static void ApplyFreeRumUsage(int count)
        {
            Debug.Log($"<color=cyan>Next {count} rum uses are free</color>");
        }
        
        private static void AddHighQualityRum(int count)
        {
            Debug.Log($"<color=cyan>Added {count} high quality rum</color>");
        }
        
        private static int GetGunnerRelicsUsedBonus()
        {
            // Would track gunner relics used this game
            return 0;
        }
        
        private static int GetMasterAtArmsCardsBonus()
        {
            // Would check Master-at-Arms cards in hand
            return 0;
        }
        
        private static int GetBootsCardsInDeckBonus()
        {
            // Would check boots cards in deck
            return 0;
        }
        
        #endregion
        
        #region Summon Helpers
        
        private static void SummonCannon(UnitStatus caster, int hp)
        {
            Debug.Log($"<color=yellow>Summoned cannon with {hp} HP</color>");
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
        
        private static void SummonEarthquakeHazards(int count)
        {
            Debug.Log($"<color=yellow>Created {count} earthquake hazards on random tiles</color>");
        }
        
        private static void SummonHealingPotions(int count, int healAmount)
        {
            Debug.Log($"<color=yellow>Summoned {count} healing potions ({healAmount} HP each)</color>");
        }
        
        private static void SummonDebuffObstacle(float statReduction)
        {
            Debug.Log($"<color=yellow>Summoned debuff obstacle: -{statReduction*100}% stats to nearby enemies</color>");
        }
        
        private static void DestroyAllSoftObstaclesAndGainHull(UnitStatus caster, float hullPerObstacle)
        {
            Debug.Log($"<color=yellow>Destroyed all soft obstacles, gained {hullPerObstacle*100}% hull each</color>");
        }
        
        private static void CurseRandomEnemyTile()
        {
            Debug.Log($"<color=red>Cursed random enemy tile: trapped + 10% more damage</color>");
        }
        
        private static void BuffRandomTile()
        {
            Debug.Log($"<color=green>Buffed random tile: -15% damage taken, +15% damage dealt</color>");
        }
        
        #endregion
        
        #region Ultimate Helpers
        
        private static void ExecuteShipCannonUltimate(int damage, int shots)
        {
            Debug.Log($"<color=purple>SHIP CANNON: {shots} shots of {damage} damage + fire hazard</color>");
        }
        
        private static void ExecuteMarkCaptainUltimate(UnitStatus caster)
        {
            Debug.Log($"<color=purple>MARK CAPTAIN: Enemy captain is only target this turn</color>");
        }
        
        private static void ReviveAlly(UnitStatus caster, float percentHP)
        {
            Debug.Log($"<color=purple>REVIVE: Revive ally at {percentHP*100}% HP/Morale</color>");
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
        
        private static void SwapClosestAndFurthestEnemy(UnitStatus caster)
        {
            var enemies = GetAllEnemies(caster);
            if (enemies.Count < 2) return;
            
            var closest = enemies.OrderBy(e => GetManhattanDistance(caster, e)).First();
            var furthest = enemies.OrderByDescending(e => GetManhattanDistance(caster, e)).First();
            
            ExecuteSwapWithUnit(closest, furthest);
            Debug.Log($"<color=purple>Swapped closest and furthest enemies</color>");
        }
        
        private static void SetColumnOnFire(UnitStatus target)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            Vector2Int pos = gridManager.WorldToGridPosition(target.transform.position);
            Debug.Log($"<color=red>Set column {pos.x} on fire</color>");
        }
        
        private static void ClearAllHazardsOnPlayerSide()
        {
            Debug.Log($"<color=green>Cleared all hazards on player side</color>");
        }
        
        private static void PreventHazardsNextTurn()
        {
            Debug.Log($"<color=green>No hazards can spawn next turn</color>");
        }
        
        private static void PullEnemiestoSameRow(UnitStatus caster, int range)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            Vector2Int casterPos = gridManager.WorldToGridPosition(caster.transform.position);
            
            foreach (var enemy in GetEnemiesInRange(caster, range))
            {
                Vector2Int enemyPos = gridManager.WorldToGridPosition(enemy.transform.position);
                if (enemyPos.y != casterPos.y)
                {
                    GridCell targetCell = gridManager.GetCell(enemyPos.x, casterPos.y);
                    if (targetCell != null && targetCell.CanPlaceUnit())
                    {
                        GridCell currentCell = gridManager.GetCell(enemyPos.x, enemyPos.y);
                        if (currentCell != null) currentCell.RemoveUnit();
                        
                        enemy.transform.position = targetCell.GetWorldPosition();
                        targetCell.PlaceUnit(enemy.gameObject);
                    }
                }
            }
            Debug.Log($"<color=yellow>Pulled nearby enemies to same row</color>");
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
        
        private static UnitStatus GetClosestEnemy(UnitStatus caster)
        {
            return GetAllEnemies(caster)
                .OrderBy(e => GetManhattanDistance(caster, e))
                .FirstOrDefault();
        }
        
        private static UnitStatus GetEnemyCaptain(UnitStatus caster)
        {
            return GetAllEnemies(caster)
                .FirstOrDefault(e => e.Role == UnitRole.Captain);
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