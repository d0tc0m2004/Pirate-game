using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Enums;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Database containing all 192 relic effects (96 V1 + 96 V2).
    /// 8 categories x 12 roles x 2 variants = 192 effects total.
    /// </summary>
    [CreateAssetMenu(fileName = "RelicEffectsDatabase", menuName = "Tactical/Equipment/Relic Effects Database")]
    public class RelicEffectsDatabase : ScriptableObject
    {
        [Header("All Relic Effects")]
        public List<RelicEffectData> allEffects = new List<RelicEffectData>();

        // Singleton
        private static RelicEffectsDatabase _instance;
        public static RelicEffectsDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<RelicEffectsDatabase>("RelicEffectsDatabase");
                    if (_instance == null)
                    {
                        Debug.LogWarning("RelicEffectsDatabase not found in Resources, creating default.");
                        _instance = CreateDefaultDatabase();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Get effect for a specific category and role (V1 by default).
        /// </summary>
        public RelicEffectData GetEffect(RelicCategory category, UnitRole roleTag, bool variant2 = false)
        {
            return allEffects.FirstOrDefault(e => 
                e.category == category && 
                e.roleTag == roleTag && 
                e.isVariant2 == variant2);
        }

        /// <summary>
        /// Get effect by effect type.
        /// </summary>
        public RelicEffectData GetEffect(RelicEffectType effectType)
        {
            return allEffects.FirstOrDefault(e => e.effectType == effectType);
        }

        /// <summary>
        /// Get all effects for a category (both V1 and V2).
        /// </summary>
        public List<RelicEffectData> GetEffectsByCategory(RelicCategory category)
        {
            return allEffects.Where(e => e.category == category).ToList();
        }

        /// <summary>
        /// Get all effects for a role (both V1 and V2).
        /// </summary>
        public List<RelicEffectData> GetEffectsByRole(UnitRole roleTag)
        {
            return allEffects.Where(e => e.roleTag == roleTag).ToList();
        }

        /// <summary>
        /// Get all V1 effects.
        /// </summary>
        public List<RelicEffectData> GetV1Effects()
        {
            return allEffects.Where(e => !e.isVariant2).ToList();
        }

        /// <summary>
        /// Get all V2 effects.
        /// </summary>
        public List<RelicEffectData> GetV2Effects()
        {
            return allEffects.Where(e => e.isVariant2).ToList();
        }

        /// <summary>
        /// Create the default database with all 192 effects.
        /// </summary>
        public static RelicEffectsDatabase CreateDefaultDatabase()
        {
            var db = ScriptableObject.CreateInstance<RelicEffectsDatabase>();
            db.PopulateAllEffects();
            return db;
        }

        private string GetRoleDisplayName(UnitRole role)
        {
            return role switch
            {
                UnitRole.MasterGunner => "Master Gunner",
                UnitRole.MasterAtArms => "Master-at-Arms",
                UnitRole.Helmsmaster => "Helmsman",
                _ => role.ToString()
            };
        }

        /// <summary>
        /// Populate all 192 effects.
        /// </summary>
        public void PopulateAllEffects()
        {
            allEffects.Clear();
            
            // ==================== BOOTS V1 ====================
            AddEffect(RelicCategory.Boots, UnitRole.Captain, false, 2, 1, false,
                RelicEffectType.Boots_SwapWithUnit,
                "Swap location with another unit", 0, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Quartermaster, false, 2, 1, false,
                RelicEffectType.Boots_MoveAlly,
                "Move any allied unit 2 tiles", 2, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Helmsmaster, false, 2, 1, false,
                RelicEffectType.Boots_MoveRestoreMorale,
                "Move 2 tiles and restore 10% morale", 2, 0.10f, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Boatswain, false, 2, 1, false,
                RelicEffectType.Boots_AllyFreeMoveLowestMorale,
                "Lowest morale ally can move free this turn", 0, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Shipwright, false, 2, 1, false,
                RelicEffectType.Boots_MoveClearBuzz,
                "Move 2 tiles and clear buzz meter", 2, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.MasterGunner, false, 2, 1, false,
                RelicEffectType.Boots_FreeIfGrog,
                "Move 2 tiles. Free if grog available", 2, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.MasterAtArms, false, 2, 1, false,
                RelicEffectType.Boots_MoveReduceDamage,
                "Move 2 tiles, take 20% less damage next turn", 2, 0.20f, 1);
            AddEffect(RelicCategory.Boots, UnitRole.Navigator, false, 2, 1, false,
                RelicEffectType.Boots_MoveAnyIfHighestHP,
                "Move any distance if highest HP, else 2 tiles", 2, 99, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Surgeon, false, 2, 1, false,
                RelicEffectType.Boots_MoveToNeutral,
                "Move to any tile in neutral zone", 0, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Cook, false, 2, 1, false,
                RelicEffectType.Boots_MoveGainGrit,
                "Move 2 tiles, gain 20% Grit for 2 turns", 2, 0.20f, 2);
            AddEffect(RelicCategory.Boots, UnitRole.Swashbuckler, false, 2, 1, false,
                RelicEffectType.Boots_MoveGainAim,
                "Move 2 tiles, gain 50% Aim this turn", 2, 0.50f, 1);
            AddEffect(RelicCategory.Boots, UnitRole.Deckhand, false, 2, 1, false,
                RelicEffectType.Boots_MoveReduceRangedCost,
                "Move 1 tile, reduce next ranged cost by 1", 1, 1, 0);

            // ==================== BOOTS V2 ====================
            AddEffect(RelicCategory.Boots, UnitRole.Captain, true, 2, 1, false,
                RelicEffectType.Boots_V2_SwapWithEnemy,
                "Swap location with an enemy", 0, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Quartermaster, true, 2, 1, false,
                RelicEffectType.Boots_V2_MoveAllyGainShield,
                "Move ally 2 tiles, they gain 30 shield", 2, 30, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Helmsmaster, true, 2, 1, false,
                RelicEffectType.Boots_V2_MoveGainMoraleOnKill,
                "Move 2 tiles, gain 20% morale on next kill", 2, 0.20f, 1);
            AddEffect(RelicCategory.Boots, UnitRole.Boatswain, true, 2, 1, false,
                RelicEffectType.Boots_V2_AllAlliesMove1,
                "All allies can move 1 tile free this turn", 1, 0, 1);
            AddEffect(RelicCategory.Boots, UnitRole.Shipwright, true, 2, 1, false,
                RelicEffectType.Boots_V2_MoveGainBuzzReduction,
                "Move 2 tiles, buzz gain reduced 50% for 2 turns", 2, 0.50f, 2);
            AddEffect(RelicCategory.Boots, UnitRole.MasterGunner, true, 2, 1, false,
                RelicEffectType.Boots_V2_MoveGainGrog,
                "Move 2 tiles, gain 1 grog", 2, 1, 0);
            AddEffect(RelicCategory.Boots, UnitRole.MasterAtArms, true, 2, 1, false,
                RelicEffectType.Boots_V2_MoveGainArmor,
                "Move 2 tiles, gain 50 hull", 2, 50, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Navigator, true, 2, 1, false,
                RelicEffectType.Boots_V2_MoveExtraIfLowHP,
                "Move 2 tiles (+2 if below 50% HP)", 2, 2, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Surgeon, true, 2, 1, false,
                RelicEffectType.Boots_V2_MoveHealAdjacent,
                "Move 2 tiles, heal adjacent allies 10%", 2, 0.10f, 0, 1);
            AddEffect(RelicCategory.Boots, UnitRole.Cook, true, 2, 1, false,
                RelicEffectType.Boots_V2_MovePoisonTile,
                "Move 2 tiles, leave poison on previous tile", 2, 10, 3);
            AddEffect(RelicCategory.Boots, UnitRole.Swashbuckler, true, 2, 1, false,
                RelicEffectType.Boots_V2_MoveGainDodge,
                "Move 2 tiles, 30% dodge next attack", 2, 0.30f, 1);
            AddEffect(RelicCategory.Boots, UnitRole.Deckhand, true, 2, 1, false,
                RelicEffectType.Boots_V2_MoveDrawCard,
                "Move 1 tile, draw a card", 1, 1, 0);

            // ==================== GLOVES V1 ====================
            AddEffect(RelicCategory.Gloves, UnitRole.Captain, false, 2, 1, false,
                RelicEffectType.Gloves_AttackReduceEnemyDraw,
                "Attack, enemy draws 1 less next turn", 0, 1, 1);
            AddEffect(RelicCategory.Gloves, UnitRole.Quartermaster, false, 2, 1, false,
                RelicEffectType.Gloves_AttackIncreaseEnemyCost,
                "Attack, enemy next card costs +1", 0, 1, 1);
            AddEffect(RelicCategory.Gloves, UnitRole.Helmsmaster, false, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusByMissingMorale,
                "Attack, +damage by enemy missing morale", 0, 0, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Boatswain, false, 2, 1, false,
                RelicEffectType.Gloves_AttackMarkMoraleFocus,
                "Attack, mark for morale focus 2 turns", 0, 0, 2);
            AddEffect(RelicCategory.Gloves, UnitRole.Shipwright, false, 2, 1, false,
                RelicEffectType.Gloves_AttackPreventBuzzReduce,
                "Attack, prevent buzz reduction 2 turns", 0, 0, 2);
            AddEffect(RelicCategory.Gloves, UnitRole.MasterGunner, false, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusPerGrog,
                "Attack, +20% per grog token", 0, 0.20f, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.MasterAtArms, false, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusIfMoreHP,
                "Attack, +20% if more HP than target", 0, 0.20f, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Navigator, false, 2, 1, false,
                RelicEffectType.Gloves_AttackLowerEnemyHealth,
                "Attack, lower enemy health stat 30% for 2 turns", 0, 0.30f, 2);
            AddEffect(RelicCategory.Gloves, UnitRole.Surgeon, false, 2, 1, false,
                RelicEffectType.Gloves_AttackPushForward,
                "Attack, push target forward 1 tile", 0, 1, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Cook, false, 2, 1, false,
                RelicEffectType.Gloves_AttackForceTargetClosest,
                "Attack, force enemy to attack closest next turn", 0, 0, 1);
            AddEffect(RelicCategory.Gloves, UnitRole.Swashbuckler, false, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusPerCardPlayed,
                "Attack, +10% per card played this round", 0, 0.10f, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Deckhand, false, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusPerGunnerRelic,
                "Attack, +10% per gunner relic used this game", 0, 0.10f, 0);

            // ==================== GLOVES V2 ====================
            AddEffect(RelicCategory.Gloves, UnitRole.Captain, true, 2, 1, false,
                RelicEffectType.Gloves_V2_AttackStealBuff,
                "Attack, steal one buff from target", 0, 1, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Quartermaster, true, 2, 1, false,
                RelicEffectType.Gloves_V2_AttackDiscard,
                "Attack, enemy discards 1 card", 0, 1, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Helmsmaster, true, 2, 1, false,
                RelicEffectType.Gloves_V2_AttackMoraleDamage,
                "Attack deals 50% bonus morale damage", 0, 0.50f, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Boatswain, true, 2, 1, false,
                RelicEffectType.Gloves_V2_AttackHealAlly,
                "Attack, heal lowest HP ally 15%", 0, 0.15f, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Shipwright, true, 2, 1, false,
                RelicEffectType.Gloves_V2_AttackReduceBuzz,
                "Attack, reduce own buzz by 20", 0, 20, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.MasterGunner, true, 2, 1, false,
                RelicEffectType.Gloves_V2_AttackSpendGrogBonus,
                "Spend 1 grog for +50% damage", 1, 0.50f, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.MasterAtArms, true, 2, 1, false,
                RelicEffectType.Gloves_V2_AttackGainHullOnKill,
                "Attack, kill grants 30 hull", 0, 30, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Navigator, true, 2, 1, false,
                RelicEffectType.Gloves_V2_AttackSlowEnemy,
                "Attack, slow enemy 1 tile for 2 turns", 0, 1, 2);
            AddEffect(RelicCategory.Gloves, UnitRole.Surgeon, true, 2, 1, false,
                RelicEffectType.Gloves_V2_AttackPullEnemy,
                "Attack, pull enemy 1 tile toward you", 0, 1, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Cook, true, 2, 1, false,
                RelicEffectType.Gloves_V2_AttackApplyPoison,
                "Attack applies poison (10 dmg/turn, 3 turns)", 0, 10, 3);
            AddEffect(RelicCategory.Gloves, UnitRole.Swashbuckler, true, 2, 1, false,
                RelicEffectType.Gloves_V2_AttackBonusVsDebuffed,
                "Attack, +30% vs debuffed enemies", 0, 0.30f, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Deckhand, true, 2, 1, false,
                RelicEffectType.Gloves_V2_AttackChainToAdjacent,
                "Attack chains 50% damage to adjacent enemy", 0, 0.50f, 0, 1);

            // ==================== HAT V1 ====================
            AddEffect(RelicCategory.Hat, UnitRole.Captain, false, 2, 1, false,
                RelicEffectType.Hat_DrawCardsVulnerable,
                "Draw 2, take 200% damage for 2 turns", 2, 2.0f, 2);
            AddEffect(RelicCategory.Hat, UnitRole.Quartermaster, false, 2, 1, false,
                RelicEffectType.Hat_DrawUltimate,
                "Draw an ultimate ability", 1, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Helmsmaster, false, 2, 1, false,
                RelicEffectType.Hat_RestoreMoraleLowest,
                "Restore 30% morale to lowest ally", 0, 0.30f, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Boatswain, false, 2, 1, false,
                RelicEffectType.Hat_RestoreMoraleNearby,
                "10% morale to allies in 1 tile", 0, 0.10f, 0, 1);
            AddEffect(RelicCategory.Hat, UnitRole.Shipwright, false, 2, 1, false,
                RelicEffectType.Hat_FreeRumUsage,
                "3 rum usage free this round", 3, 0, 1);
            AddEffect(RelicCategory.Hat, UnitRole.MasterGunner, false, 2, 1, false,
                RelicEffectType.Hat_GenerateGrog,
                "Generate 2 grog tokens", 2, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.MasterAtArms, false, 2, 1, false,
                RelicEffectType.Hat_ReturnDamage,
                "Return 1 damage instance for 2 turns", 1, 0, 2);
            AddEffect(RelicCategory.Hat, UnitRole.Navigator, false, 2, 1, false,
                RelicEffectType.Hat_IncreaseHealthStat,
                "+25% health stat for 2 turns", 0, 0.25f, 2);
            AddEffect(RelicCategory.Hat, UnitRole.Surgeon, false, 2, 1, false,
                RelicEffectType.Hat_EnergyOnKnockback,
                "+2 energy if knocked back next turn", 2, 0, 1);
            AddEffect(RelicCategory.Hat, UnitRole.Cook, false, 2, 1, false,
                RelicEffectType.Hat_SwapEnemyByGrit,
                "Swap highest/lowest grit enemies", 0, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Swashbuckler, false, 2, 1, false,
                RelicEffectType.Hat_WeaponUseTwice,
                "Next weapon can be used twice", 0, 0, 1);
            AddEffect(RelicCategory.Hat, UnitRole.Deckhand, false, 2, 1, false,
                RelicEffectType.Hat_DrawWeaponRelic,
                "Draw a weapon relic", 1, 0, 0);

            // ==================== HAT V2 ====================
            AddEffect(RelicCategory.Hat, UnitRole.Captain, true, 2, 1, false,
                RelicEffectType.Hat_V2_DrawAndShield,
                "Draw 1 card, gain 30 shield", 1, 30, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Quartermaster, true, 2, 1, false,
                RelicEffectType.Hat_V2_DrawBootsRelic,
                "Draw a boots relic", 1, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Helmsmaster, true, 2, 1, false,
                RelicEffectType.Hat_V2_RestoreMoraleAll,
                "All allies restore 5% morale", 0, 0.05f, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Boatswain, true, 2, 1, false,
                RelicEffectType.Hat_V2_PreventMoraleLoss,
                "Allies can't lose morale this turn", 0, 0, 1);
            AddEffect(RelicCategory.Hat, UnitRole.Shipwright, true, 2, 1, false,
                RelicEffectType.Hat_V2_RumHealsMore,
                "Rum heals 50% more this turn", 0, 0.50f, 1);
            AddEffect(RelicCategory.Hat, UnitRole.MasterGunner, true, 2, 1, false,
                RelicEffectType.Hat_V2_GrogOnEnemyKill,
                "Gain 1 grog per enemy killed this turn", 1, 0, 1);
            AddEffect(RelicCategory.Hat, UnitRole.MasterAtArms, true, 2, 1, false,
                RelicEffectType.Hat_V2_DamageReductionBuff,
                "Gain 30% damage reduction for 2 turns", 0, 0.30f, 2);
            AddEffect(RelicCategory.Hat, UnitRole.Navigator, true, 2, 1, false,
                RelicEffectType.Hat_V2_SpeedBoost,
                "+2 movement this turn", 2, 0, 1);
            AddEffect(RelicCategory.Hat, UnitRole.Surgeon, true, 2, 1, false,
                RelicEffectType.Hat_V2_HealOnCardPlay,
                "Heal 5% HP per card played this turn", 0, 0.05f, 1);
            AddEffect(RelicCategory.Hat, UnitRole.Cook, true, 2, 1, false,
                RelicEffectType.Hat_V2_BuffFoodEffects,
                "Food effects doubled this turn", 0, 2.0f, 1);
            AddEffect(RelicCategory.Hat, UnitRole.Swashbuckler, true, 2, 1, false,
                RelicEffectType.Hat_V2_DrawPerEnemyInRange,
                "Draw 1 card per enemy in 2 tiles", 0, 1, 0, 2);
            AddEffect(RelicCategory.Hat, UnitRole.Deckhand, true, 2, 1, false,
                RelicEffectType.Hat_V2_ReduceAllCosts,
                "All card costs -1 this turn", 0, 1, 1);

            // ==================== COAT V1 ====================
            AddEffect(RelicCategory.Coat, UnitRole.Captain, false, 2, 1, false,
                RelicEffectType.Coat_BuffNearbyAimPower,
                "+20% Aim/Power to allies in 1 tile", 0, 0.20f, 1, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Quartermaster, false, 2, 1, false,
                RelicEffectType.Coat_DrawOnEnemyAttack,
                "Draw card per enemy attack, enemy discards", 1, 1, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Helmsmaster, false, 2, 1, false,
                RelicEffectType.Coat_ReduceMoraleDamage,
                "Allies take 30% less morale damage 2 turns", 0, 0.30f, 2);
            AddEffect(RelicCategory.Coat, UnitRole.Boatswain, false, 2, 1, false,
                RelicEffectType.Coat_PreventSurrender,
                "If ally would surrender, restore 20% morale", 0, 0.20f, 0);
            AddEffect(RelicCategory.Coat, UnitRole.Shipwright, false, 2, 1, false,
                RelicEffectType.Coat_ReduceRumEffect,
                "Nearby allies reduced rum effect", 0, 0.50f, 0, 1);
            AddEffect(RelicCategory.Coat, UnitRole.MasterGunner, false, 2, 1, false,
                RelicEffectType.Coat_EnemyBuzzOnDamage,
                "Enemy buzz fills when dealing damage to you", 0, 20, 2);
            AddEffect(RelicCategory.Coat, UnitRole.MasterAtArms, false, 2, 1, false,
                RelicEffectType.Coat_PreventDisplacement,
                "Allies can't be knocked back this turn", 0, 0, 1, 2);
            AddEffect(RelicCategory.Coat, UnitRole.Navigator, false, 2, 1, false,
                RelicEffectType.Coat_ProtectLowHP,
                "Lowest HP can only be targeted by lower HP units", 0, 0, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Surgeon, false, 2, 1, false,
                RelicEffectType.Coat_RowCantBeTargeted,
                "Allies behind in row can't be targeted", 0, 0, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Cook, false, 2, 1, false,
                RelicEffectType.Coat_ColumnDamageBoost,
                "+40% damage to allies in same column", 0, 0.40f, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Swashbuckler, false, 2, 1, false,
                RelicEffectType.Coat_FreeStow,
                "Next 2 stows free", 2, 0, 0);
            AddEffect(RelicCategory.Coat, UnitRole.Deckhand, false, 2, 1, false,
                RelicEffectType.Coat_RowRangedProtection,
                "Row takes 50% less ranged damage", 0, 0.50f, 2);

            // ==================== COAT V2 ====================
            AddEffect(RelicCategory.Coat, UnitRole.Captain, true, 2, 1, false,
                RelicEffectType.Coat_V2_ShieldNearby,
                "Nearby allies gain 25 shield", 25, 0, 0, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Quartermaster, true, 2, 1, false,
                RelicEffectType.Coat_V2_CounterOnAllyHit,
                "Counter-attack when ally is hit", 0, 0, 2);
            AddEffect(RelicCategory.Coat, UnitRole.Helmsmaster, true, 2, 1, false,
                RelicEffectType.Coat_V2_MoraleShield,
                "Absorb next 50 morale damage for allies", 50, 0, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Boatswain, true, 2, 1, false,
                RelicEffectType.Coat_V2_RevivePrevent,
                "Prevent one ally death this turn (1 HP)", 1, 0, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Shipwright, true, 2, 1, false,
                RelicEffectType.Coat_V2_BuzzImmunity,
                "Nearby allies immune to buzz effects", 0, 0, 2, 1);
            AddEffect(RelicCategory.Coat, UnitRole.MasterGunner, true, 2, 1, false,
                RelicEffectType.Coat_V2_GrogShield,
                "Spend 1 grog, gain 50 shield", 1, 50, 0);
            AddEffect(RelicCategory.Coat, UnitRole.MasterAtArms, true, 2, 1, false,
                RelicEffectType.Coat_V2_ThornsAura,
                "Attackers take 20 damage for 2 turns", 20, 0, 2);
            AddEffect(RelicCategory.Coat, UnitRole.Navigator, true, 2, 1, false,
                RelicEffectType.Coat_V2_DodgeAura,
                "Nearby allies 15% dodge chance for 2 turns", 0, 0.15f, 2, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Surgeon, true, 2, 1, false,
                RelicEffectType.Coat_V2_HealingAura,
                "Nearby allies heal 5% at turn end for 2 turns", 0, 0.05f, 2, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Cook, true, 2, 1, false,
                RelicEffectType.Coat_V2_WellFed,
                "Nearby allies +10% max HP for 2 turns", 0, 0.10f, 2, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Swashbuckler, true, 2, 1, false,
                RelicEffectType.Coat_V2_Evasion,
                "Gain 25% evasion for 2 turns", 0, 0.25f, 2);
            AddEffect(RelicCategory.Coat, UnitRole.Deckhand, true, 2, 1, false,
                RelicEffectType.Coat_V2_RangedBlock,
                "Block next ranged attack completely", 1, 0, 1);

            // ==================== TRINKET V1 (Passive) ====================
            AddPassive(RelicCategory.Trinket, UnitRole.Captain, false,
                RelicEffectType.Trinket_BonusDamagePerCard,
                "Passive: +20% weapon damage per card in hand", 0.20f, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Quartermaster, false,
                RelicEffectType.Trinket_BonusVsCaptain,
                "Passive: +20% damage vs enemy captain", 0.20f, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Helmsmaster, false,
                RelicEffectType.Trinket_ImmuneMoraleFocusFire,
                "Passive: Immune to morale focus fire", 0, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Boatswain, false,
                RelicEffectType.Trinket_EnemySurrenderEarly,
                "Passive: Enemies surrender at 30% morale", 0.30f, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Shipwright, false,
                RelicEffectType.Trinket_DamageByBuzz,
                "Passive: +damage based on own buzz", 0, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.MasterGunner, false,
                RelicEffectType.Trinket_KnockbackIncreasesBuzz,
                "Passive: Knockback increases enemy buzz", 20, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.MasterAtArms, false,
                RelicEffectType.Trinket_ReduceDamageFromClosest,
                "Passive: Closest enemy does -20% damage", 0.20f, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Navigator, false,
                RelicEffectType.Trinket_DrawIfHighHP,
                "Passive: Draw extra if HP above 60%", 0.60f, 1);
            AddPassive(RelicCategory.Trinket, UnitRole.Surgeon, false,
                RelicEffectType.Trinket_TauntFirstAttack,
                "Passive: Taunt first attack per enemy turn", 1, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Cook, false,
                RelicEffectType.Trinket_KnockbackAttacker,
                "Passive: Knockback attacker once per turn", 1, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Swashbuckler, false,
                RelicEffectType.Trinket_RowEnemiesLessDamage,
                "Passive: Enemies in row do -10% damage", 0.10f, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Deckhand, false,
                RelicEffectType.Trinket_RowEnemiesTakeMore,
                "Passive: Enemies in row take +10% damage", 0.10f, 0);

            // ==================== TRINKET V2 (Passive) ====================
            AddPassive(RelicCategory.Trinket, UnitRole.Captain, true,
                RelicEffectType.Trinket_V2_BonusDamagePerAlly,
                "Passive: +5% damage per ally alive", 0.05f, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Quartermaster, true,
                RelicEffectType.Trinket_V2_DrawOnCaptainHit,
                "Passive: Draw card when captain takes damage", 1, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Helmsmaster, true,
                RelicEffectType.Trinket_V2_MoraleOnKill,
                "Passive: Gain 10% morale on enemy kill", 0.10f, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Boatswain, true,
                RelicEffectType.Trinket_V2_AllySurrenderLater,
                "Passive: Allies surrender at 10% instead of 20%", 0.10f, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Shipwright, true,
                RelicEffectType.Trinket_V2_NoBuzzPenalty,
                "Passive: No penalty when buzz is full", 0, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.MasterGunner, true,
                RelicEffectType.Trinket_V2_GrogOnTurnStart,
                "Passive: 25% chance for free grog each turn", 0.25f, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.MasterAtArms, true,
                RelicEffectType.Trinket_V2_ArmorOnLowHP,
                "Passive: +50% armor when below 30% HP", 0.50f, 0.30f);
            AddPassive(RelicCategory.Trinket, UnitRole.Navigator, true,
                RelicEffectType.Trinket_V2_SpeedOnHighHP,
                "Passive: +1 movement when above 70% HP", 1, 0.70f);
            AddPassive(RelicCategory.Trinket, UnitRole.Surgeon, true,
                RelicEffectType.Trinket_V2_HealOnTurnEnd,
                "Passive: Heal 3% at turn end", 0.03f, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Cook, true,
                RelicEffectType.Trinket_V2_FoodDoubleDuration,
                "Passive: Food buffs last twice as long", 2, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Swashbuckler, true,
                RelicEffectType.Trinket_V2_CritChance,
                "Passive: 15% chance for +50% damage", 0.15f, 0.50f);
            AddPassive(RelicCategory.Trinket, UnitRole.Deckhand, true,
                RelicEffectType.Trinket_V2_BonusVsFullHP,
                "Passive: +25% damage vs full HP targets", 0.25f, 0);

            // ==================== TOTEM V1 ====================
            AddEffect(RelicCategory.Totem, UnitRole.Captain, false, 2, 1, false,
                RelicEffectType.Totem_SummonCannon,
                "Summon cannon, 250 HP, attacks random enemy", 250, 0, 0);
            AddEffect(RelicCategory.Totem, UnitRole.Quartermaster, false, 2, 1, false,
                RelicEffectType.Totem_CurseCaptainReflect,
                "Captain damage reflects to nearby allies", 0, 0.50f, 2);
            AddEffect(RelicCategory.Totem, UnitRole.Helmsmaster, false, 2, 1, false,
                RelicEffectType.Totem_RallyNoMoraleDamage,
                "Nearby allies no morale damage next turn", 0, 0, 1, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Boatswain, false, 2, 1, false,
                RelicEffectType.Totem_EnemyDeathMoraleSwing,
                "Enemy death: enemies lose morale, allies gain", 0, 0.05f, 0);
            AddEffect(RelicCategory.Totem, UnitRole.Shipwright, false, 2, 1, false,
                RelicEffectType.Totem_SummonHighQualityRum,
                "Add 2 high quality rum", 2, 0, 0);
            AddEffect(RelicCategory.Totem, UnitRole.MasterGunner, false, 2, 1, false,
                RelicEffectType.Totem_ConvertGrogToEnergy,
                "Convert 2 grog to 1 energy", 2, 1, 0);
            AddEffect(RelicCategory.Totem, UnitRole.MasterAtArms, false, 2, 1, false,
                RelicEffectType.Totem_StunOnKnockback,
                "If knocked back, stun attacker 1 turn", 0, 0, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Navigator, false, 2, 1, false,
                RelicEffectType.Totem_SummonAnchorHealthBuff,
                "Summon anchor, +25% health to nearby allies", 0, 0.25f, 0, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Surgeon, false, 2, 1, false,
                RelicEffectType.Totem_SummonTargetDummy,
                "Summon target dummy in front row (250 HP)", 250, 0, 0);
            AddEffect(RelicCategory.Totem, UnitRole.Cook, false, 2, 1, false,
                RelicEffectType.Totem_SummonObstacleDisplace,
                "Summon obstacle at target, displace them", 0, 0, 0);
            AddEffect(RelicCategory.Totem, UnitRole.Swashbuckler, false, 2, 1, false,
                RelicEffectType.Totem_SummonExplodingBarrels,
                "Summon 3 barrels that explode after 2 turns", 3, 100, 2);
            AddEffect(RelicCategory.Totem, UnitRole.Deckhand, false, 2, 1, false,
                RelicEffectType.Totem_CurseRangedWeapons,
                "Curse: enemy ranged -50% damage next turn", 0, 0.50f, 1);

            // ==================== TOTEM V2 ====================
            AddEffect(RelicCategory.Totem, UnitRole.Captain, true, 2, 1, false,
                RelicEffectType.Totem_V2_SummonHealingTotem,
                "Summon totem that heals nearby allies 10/turn", 10, 0, 3, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Quartermaster, true, 2, 1, false,
                RelicEffectType.Totem_V2_CurseWeakness,
                "Curse enemy: -20% damage for 3 turns", 0, 0.20f, 3);
            AddEffect(RelicCategory.Totem, UnitRole.Helmsmaster, true, 2, 1, false,
                RelicEffectType.Totem_V2_RallyDamageBoost,
                "Nearby allies +15% damage for 2 turns", 0, 0.15f, 2, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Boatswain, true, 2, 1, false,
                RelicEffectType.Totem_V2_SummonMoraleBanner,
                "Banner prevents morale loss in radius for 2 turns", 0, 0, 2, 2);
            AddEffect(RelicCategory.Totem, UnitRole.Shipwright, true, 2, 1, false,
                RelicEffectType.Totem_V2_SummonGrogBarrel,
                "Barrel gives 2 grog when destroyed", 2, 0, 0);
            AddEffect(RelicCategory.Totem, UnitRole.MasterGunner, true, 2, 1, false,
                RelicEffectType.Totem_V2_TrapTile,
                "Place trap that stuns for 1 turn", 0, 0, 1);
            AddEffect(RelicCategory.Totem, UnitRole.MasterAtArms, true, 2, 1, false,
                RelicEffectType.Totem_V2_SummonShieldGenerator,
                "Generator gives 10 shield/turn for 3 turns", 10, 0, 3, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Navigator, true, 2, 1, false,
                RelicEffectType.Totem_V2_SummonSpeedBooster,
                "Tile gives +1 movement to allies for 3 turns", 1, 0, 3);
            AddEffect(RelicCategory.Totem, UnitRole.Surgeon, true, 2, 1, false,
                RelicEffectType.Totem_V2_SummonHealingWell,
                "Well heals unit standing on it 15%/turn", 0, 0.15f, 3);
            AddEffect(RelicCategory.Totem, UnitRole.Cook, true, 2, 1, false,
                RelicEffectType.Totem_V2_PoisonCloud,
                "Poison cloud: 15 dmg/turn for 3 turns", 15, 0, 3, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Swashbuckler, true, 2, 1, false,
                RelicEffectType.Totem_V2_SummonDecoy,
                "Decoy taunts enemies for 2 turns", 0, 0, 2);
            AddEffect(RelicCategory.Totem, UnitRole.Deckhand, true, 2, 1, false,
                RelicEffectType.Totem_V2_CurseSlow,
                "Curse enemy: -2 movement for 2 turns", 2, 0, 2);

            // ==================== ULTIMATE V1 ====================
            AddEffect(RelicCategory.Ultimate, UnitRole.Captain, false, 1, 3, false,
                RelicEffectType.Ultimate_ShipCannon,
                "Fire ship cannon: 3 shots, 200 damage + fire hazard", 200, 3, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Quartermaster, false, 1, 3, false,
                RelicEffectType.Ultimate_MarkCaptainOnly,
                "Attack enemy captain, mark as only target this turn", 0, 0, 1, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Helmsmaster, false, 1, 3, false,
                RelicEffectType.Ultimate_ReflectMoraleDamage,
                "Morale damage to allies reflects to enemies next turn", 0, 0, 1, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Boatswain, false, 1, 3, false,
                RelicEffectType.Ultimate_ReviveAlly,
                "Revive dead or surrendered ally at 30%", 0, 0.30f, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Shipwright, false, 1, 3, false,
                RelicEffectType.Ultimate_FullBuzzAttack,
                "Attack, make target buzz full for 2 turns", 0, 0, 2, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.MasterGunner, false, 1, 3, false,
                RelicEffectType.Ultimate_RumBottleAoE,
                "200 damage AoE, rum spill increases buzz 3 turns", 200, 0, 3, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.MasterAtArms, false, 1, 3, false,
                RelicEffectType.Ultimate_SummonHardObstacles,
                "Summon 3 hard obstacles in front row for 2 turns", 3, 0, 2, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Navigator, false, 1, 3, false,
                RelicEffectType.Ultimate_IgnoreHighestHP,
                "Highest HP enemy (except captain) ignored this turn", 0, 0, 1, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Surgeon, false, 1, 3, false,
                RelicEffectType.Ultimate_KnockbackToLastColumn,
                "Attack, knockback target to last column", 0, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Cook, false, 1, 3, false,
                RelicEffectType.Ultimate_AttackKnockbackNearby,
                "Attack, knockback nearby enemies 1 tile", 0, 1, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Swashbuckler, false, 1, 3, false,
                RelicEffectType.Ultimate_StunAoE,
                "Attack, stun target and nearby enemies 1 turn", 0, 0, 1, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Deckhand, false, 1, 3, false,
                RelicEffectType.Ultimate_MassiveSingleTarget,
                "+300% damage if no nearby enemies", 0, 3.0f, 0, 1, RelicRarity.Unique);

            // ==================== ULTIMATE V2 ====================
            AddEffect(RelicCategory.Ultimate, UnitRole.Captain, true, 1, 3, false,
                RelicEffectType.Ultimate_V2_TeamwideBuff,
                "All allies +25% damage and armor for 2 turns", 0, 0.25f, 2, 99, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Quartermaster, true, 1, 3, false,
                RelicEffectType.Ultimate_V2_ExecuteBelow20,
                "Instantly kill enemy below 20% HP", 0, 0.20f, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Helmsmaster, true, 1, 3, false,
                RelicEffectType.Ultimate_V2_FullMoraleRestore,
                "Fully restore all allies' morale", 0, 1.0f, 0, 99, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Boatswain, true, 1, 3, false,
                RelicEffectType.Ultimate_V2_MassRevive,
                "Revive all dead allies at 20% HP", 0, 0.20f, 0, 99, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Shipwright, true, 1, 3, false,
                RelicEffectType.Ultimate_V2_BuzzExplosion,
                "All enemies buzz fills, take damage = buzz", 0, 0, 0, 99, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.MasterGunner, true, 1, 3, false,
                RelicEffectType.Ultimate_V2_GrogRain,
                "Gain 5 grog instantly", 5, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.MasterAtArms, true, 1, 3, false,
                RelicEffectType.Ultimate_V2_Fortress,
                "All allies gain 100 shield", 100, 0, 0, 99, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Navigator, true, 1, 3, false,
                RelicEffectType.Ultimate_V2_Teleport,
                "Teleport any ally to any tile", 0, 0, 0, 99, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Surgeon, true, 1, 3, false,
                RelicEffectType.Ultimate_V2_MassHeal,
                "Heal all allies 40% HP", 0, 0.40f, 0, 99, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Cook, true, 1, 3, false,
                RelicEffectType.Ultimate_V2_Feast,
                "All allies heal 30% HP and gain 20% morale", 0.30f, 0.20f, 0, 99, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Swashbuckler, true, 1, 3, false,
                RelicEffectType.Ultimate_V2_BladeStorm,
                "Attack all enemies in range for 75% damage", 0, 0.75f, 0, 2, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Deckhand, true, 1, 3, false,
                RelicEffectType.Ultimate_V2_PerfectShot,
                "Guaranteed crit (200% damage) ignoring armor", 0, 2.0f, 0, 99, RelicRarity.Unique);

            // ==================== PASSIVE UNIQUE V1 ====================
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Captain, false,
                RelicEffectType.PassiveUnique_ExtraEnergy,
                "Passive: +1 max energy each turn", 1, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Quartermaster, false,
                RelicEffectType.PassiveUnique_ExtraCards,
                "Passive: +2 cards each turn", 2, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Helmsmaster, false,
                RelicEffectType.PassiveUnique_DeathStrikeByMorale,
                "Passive: Higher morale = chance to attack on death", 0, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Boatswain, false,
                RelicEffectType.PassiveUnique_LowerSurrenderThreshold,
                "Passive: Allies surrender at 10% instead of 20%", 0.10f, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Shipwright, false,
                RelicEffectType.PassiveUnique_NoBuzzDownside,
                "Passive: No penalty when buzz full", 0, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.MasterGunner, false,
                RelicEffectType.PassiveUnique_DrawPerGrog,
                "Passive: Draw extra cards based on grog", 1, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.MasterAtArms, false,
                RelicEffectType.PassiveUnique_DrawOnLowDamage,
                "Passive: Draw card if damage taken < 20% HP", 0.20f, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Navigator, false,
                RelicEffectType.PassiveUnique_CounterAttack,
                "Passive: Attack back when ally damaged", 0, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Surgeon, false,
                RelicEffectType.PassiveUnique_GritAura,
                "Passive: Nearby allies +5% of this unit's grit", 0.05f, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Cook, false,
                RelicEffectType.PassiveUnique_BonusVsLowGrit,
                "Passive: +20% damage vs lower grit targets", 0.20f, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Swashbuckler, false,
                RelicEffectType.PassiveUnique_IgnoreRoles,
                "Passive: Ignore Shipwright/Boatswain roles", 0, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Deckhand, false,
                RelicEffectType.PassiveUnique_BonusVsLowHP,
                "Passive: Bonus damage vs <50% HP targets", 0.50f, 0, RelicRarity.Unique);

            // ==================== PASSIVE UNIQUE V2 ====================
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Captain, true,
                RelicEffectType.PassiveUnique_V2_TeamLeader,
                "Passive: Allies in 2 tiles +10% all stats", 0.10f, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Quartermaster, true,
                RelicEffectType.PassiveUnique_V2_CardMaster,
                "Passive: Cards cost 1 less (min 0)", 1, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Helmsmaster, true,
                RelicEffectType.PassiveUnique_V2_Inspiring,
                "Passive: Allies gain 5% morale when this unit attacks", 0.05f, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Boatswain, true,
                RelicEffectType.PassiveUnique_V2_LastStand,
                "Passive: 50% chance ally survives at 1 HP", 0.50f, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Shipwright, true,
                RelicEffectType.PassiveUnique_V2_DrunkMaster,
                "Passive: Buzz gives bonuses instead of penalties", 0, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.MasterGunner, true,
                RelicEffectType.PassiveUnique_V2_Efficient,
                "Passive: 50% chance grog not consumed", 0.50f, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.MasterAtArms, true,
                RelicEffectType.PassiveUnique_V2_Unstoppable,
                "Passive: Can't be stunned or slowed", 0, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Navigator, true,
                RelicEffectType.PassiveUnique_V2_Scout,
                "Passive: See enemy cards and cooldowns", 0, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Surgeon, true,
                RelicEffectType.PassiveUnique_V2_Medic,
                "Passive: Heals are 25% more effective", 0.25f, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Cook, true,
                RelicEffectType.PassiveUnique_V2_Nourishing,
                "Passive: Food heals 10% HP additionally", 0.10f, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Swashbuckler, true,
                RelicEffectType.PassiveUnique_V2_Riposte,
                "Passive: 30% chance to counter-attack", 0.30f, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Deckhand, true,
                RelicEffectType.PassiveUnique_V2_Sniper,
                "Passive: +20% damage at max range", 0.20f, 0, RelicRarity.Unique);

            Debug.Log($"RelicEffectsDatabase populated with {allEffects.Count} effects (expected 192)");
        }

        // Helper to add active effects
        private void AddEffect(RelicCategory category, UnitRole role, bool isV2, int copies, int cost, bool isPassive,
            RelicEffectType effectType, string description, float val1, float val2, int duration, int tileRange = 1, RelicRarity rarity = RelicRarity.Common)
        {
            string suffix = isV2 ? " V2" : "";
            string effectName = $"{GetRoleDisplayName(role)} {category}{suffix}";
            
            allEffects.Add(new RelicEffectData
            {
                category = category,
                roleTag = role,
                isVariant2 = isV2,
                effectName = effectName,
                rarity = rarity,
                copies = copies,
                energyCost = cost,
                isPassive = isPassive,
                effectType = effectType,
                description = description,
                value1 = val1,
                value2 = val2,
                duration = duration,
                tileRange = tileRange
            });
        }

        // Helper to add passive effects
        private void AddPassive(RelicCategory category, UnitRole role, bool isV2,
            RelicEffectType effectType, string description, float val1, float val2, RelicRarity rarity = RelicRarity.Common)
        {
            AddEffect(category, role, isV2, 0, 0, true, effectType, description, val1, val2, 0, 1, rarity);
        }
    }
}