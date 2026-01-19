using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Enums;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Database containing all relic effects.
    /// 8 categories x 12 roles x 2 variants = 192 effects total.
    /// </summary>
    [CreateAssetMenu(fileName = "RelicEffectsDatabase", menuName = "Tactical/Equipment/Relic Effects Database")]
    public class RelicEffectsDatabase : ScriptableObject
    {
        [Header("All Relic Effects")]
        public List<RelicEffectData> allEffects = new List<RelicEffectData>();

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
        /// Get effect for a specific category, role, and variant (1 or 2).
        /// </summary>
        public RelicEffectData GetEffect(RelicCategory category, UnitRole roleTag, int variant = 1)
        {
            var matches = allEffects.Where(e => e.category == category && e.roleTag == roleTag).ToList();
            if (matches.Count == 0) return null;
            if (matches.Count == 1) return matches[0];
            
            // Return based on variant (0-indexed internally)
            int index = Mathf.Clamp(variant - 1, 0, matches.Count - 1);
            return matches[index];
        }

        /// <summary>
        /// Get all effects for a category.
        /// </summary>
        public List<RelicEffectData> GetEffectsByCategory(RelicCategory category)
        {
            return allEffects.Where(e => e.category == category).ToList();
        }

        /// <summary>
        /// Get all effects for a role.
        /// </summary>
        public List<RelicEffectData> GetEffectsByRole(UnitRole roleTag)
        {
            return allEffects.Where(e => e.roleTag == roleTag).ToList();
        }

        /// <summary>
        /// Get a random variant (1 or 2) for a category/role combo.
        /// </summary>
        public RelicEffectData GetRandomEffect(RelicCategory category, UnitRole roleTag)
        {
            int variant = Random.value > 0.5f ? 2 : 1;
            return GetEffect(category, roleTag, variant);
        }

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
        /// Populate all 192 effects (8 categories x 12 roles x 2 variants).
        /// </summary>
        public void PopulateAllEffects()
        {
            allEffects.Clear();
            
            // ==================== BOOTS V1 ====================
            AddEffect(RelicCategory.Boots, UnitRole.Captain, 2, 1, false,
                RelicEffectType.Boots_SwapWithUnit,
                "Swap location with another unit", 0, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Quartermaster, 2, 1, false,
                RelicEffectType.Boots_MoveAlly,
                "Move any allied unit 2 tiles", 2, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Helmsmaster, 2, 1, false,
                RelicEffectType.Boots_MoveRestoreMorale,
                "Move 2 tiles and restore 10% morale", 2, 0.10f, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Boatswain, 2, 1, false,
                RelicEffectType.Boots_AllyFreeMoveLowestMorale,
                "Lowest morale ally can move free this turn", 0, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Shipwright, 2, 1, false,
                RelicEffectType.Boots_MoveClearBuzz,
                "Move 2 tiles and clear buzz meter", 2, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.MasterGunner, 2, 1, false,
                RelicEffectType.Boots_FreeIfGrog,
                "Move 2 tiles. Free if grog available", 2, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.MasterAtArms, 2, 1, false,
                RelicEffectType.Boots_MoveReduceDamage,
                "Move 2 tiles, take 20% less damage next turn", 2, 0.20f, 1);
            AddEffect(RelicCategory.Boots, UnitRole.Navigator, 2, 1, false,
                RelicEffectType.Boots_MoveAnyIfHighestHP,
                "Move any distance if highest HP, else 2 tiles", 2, 99, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Surgeon, 2, 1, false,
                RelicEffectType.Boots_MoveToNeutral,
                "Move to any tile in neutral zone", 0, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Cook, 2, 1, false,
                RelicEffectType.Boots_MoveGainGrit,
                "Move 2 tiles, gain 20% Grit for 2 turns", 2, 0.20f, 2);
            AddEffect(RelicCategory.Boots, UnitRole.Swashbuckler, 2, 1, false,
                RelicEffectType.Boots_MoveGainAim,
                "Move 2 tiles, gain 50% Aim this turn", 2, 0.50f, 1);
            AddEffect(RelicCategory.Boots, UnitRole.Deckhand, 2, 1, false,
                RelicEffectType.Boots_MoveReduceRangedCost,
                "Move 1 tile, reduce next ranged cost by 1", 1, 1, 0);

            // ==================== BOOTS V2 ====================
            AddEffect(RelicCategory.Boots, UnitRole.Captain, 2, 1, false,
                RelicEffectType.Boots_MoveAnyAlly,
                "Move any allied unit 2 tiles", 2, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Quartermaster, 2, 1, false,
                RelicEffectType.Boots_LowestMoraleAllyFree,
                "Lowest morale ally moves free this turn", 0, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Helmsmaster, 2, 1, false,
                RelicEffectType.Boots_FreeIfGrogAvailable,
                "Move 2 tiles. Costs 0 if grog available", 2, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Boatswain, 2, 1, false,
                RelicEffectType.Boots_MoveAnyDistanceHighHP,
                "Move any distance if highest HP, else 2", 2, 99, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Shipwright, 2, 1, false,
                RelicEffectType.Boots_MoveGainGritStat,
                "Move 2 tiles, +20% Grit for 2 turns", 2, 0.20f, 2);
            AddEffect(RelicCategory.Boots, UnitRole.MasterGunner, 2, 1, false,
                RelicEffectType.Boots_MoveReduceRangedWeapon,
                "Move 1 tile, reduce next ranged weapon cost by 1", 1, 1, 0);
            AddEffect(RelicCategory.Boots, UnitRole.MasterAtArms, 2, 1, false,
                RelicEffectType.Boots_MoveDestroyObstacle,
                "Move to obstacle in 2 tiles, destroy it", 2, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Navigator, 2, 0, false,
                RelicEffectType.Boots_MoveFreeZeroCost,
                "Move 2 tiles (costs 0 energy)", 2, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Surgeon, 2, 1, false,
                RelicEffectType.Boots_SwapLowestHealthAlly,
                "Swap location with lowest health ally", 0, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Cook, 2, 1, false,
                RelicEffectType.Boots_MoveGainProficiency,
                "Move 2 tiles, +100% Proficiency this turn", 2, 1.0f, 1);
            AddEffect(RelicCategory.Boots, UnitRole.Swashbuckler, 2, 1, false,
                RelicEffectType.Boots_MoveRowUnlimited,
                "Move unlimited on row, 1 tile on column", 99, 1, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Deckhand, 2, 1, false,
                RelicEffectType.Boots_MoveRestoreHull,
                "Move 2 tiles, restore 50 hull shield", 2, 50, 0);

            // ==================== GLOVES V1 ====================
            AddEffect(RelicCategory.Gloves, UnitRole.Captain, 2, 1, false,
                RelicEffectType.Gloves_AttackReduceEnemyDraw,
                "Attack, enemy draws 1 less next turn", 0, 1, 1);
            AddEffect(RelicCategory.Gloves, UnitRole.Quartermaster, 2, 1, false,
                RelicEffectType.Gloves_AttackIncreaseEnemyCost,
                "Attack, enemy next card costs +1", 0, 1, 1);
            AddEffect(RelicCategory.Gloves, UnitRole.Helmsmaster, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusByMissingMorale,
                "Attack, +damage by enemy missing morale", 0, 0, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Boatswain, 2, 1, false,
                RelicEffectType.Gloves_AttackMarkMoraleFocus,
                "Attack, mark for morale focus 2 turns", 0, 0, 2);
            AddEffect(RelicCategory.Gloves, UnitRole.Shipwright, 2, 1, false,
                RelicEffectType.Gloves_AttackPreventBuzzReduce,
                "Attack, prevent buzz reduction 2 turns", 0, 0, 2);
            AddEffect(RelicCategory.Gloves, UnitRole.MasterGunner, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusPerGrog,
                "Attack, +20% damage per grog token", 0, 0.20f, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.MasterAtArms, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusIfMoreHP,
                "Attack, +20% if more HP than target", 0, 0.20f, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Navigator, 2, 1, false,
                RelicEffectType.Gloves_AttackLowerEnemyHealth,
                "Attack, lower enemy health stat 30% for 2 turns", 0, 0.30f, 2);
            AddEffect(RelicCategory.Gloves, UnitRole.Surgeon, 2, 1, false,
                RelicEffectType.Gloves_AttackPushForward,
                "Attack, push target forward 1 tile", 0, 1, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Cook, 2, 1, false,
                RelicEffectType.Gloves_AttackForceTargetClosest,
                "Attack, target forced to attack closest next turn", 0, 0, 1);
            AddEffect(RelicCategory.Gloves, UnitRole.Swashbuckler, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusPerCardPlayed,
                "Attack, +10% per card played this round", 0, 0.10f, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Deckhand, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusPerGunnerRelic,
                "Attack, +10% per gunner relic used this game", 0, 0.10f, 0);

            // ==================== GLOVES V2 ====================
            AddEffect(RelicCategory.Gloves, UnitRole.Captain, 2, 1, false,
                RelicEffectType.Gloves_AttackEnemyCostIncrease,
                "Attack, enemy next card costs +1 energy", 0, 1, 1);
            AddEffect(RelicCategory.Gloves, UnitRole.Quartermaster, 2, 1, false,
                RelicEffectType.Gloves_AttackMarkMoraleFocus2,
                "Attack, mark morale focus for 2 turns", 0, 0, 2);
            AddEffect(RelicCategory.Gloves, UnitRole.Helmsmaster, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusPerGrogToken,
                "Attack, +20% per grog token", 0, 0.20f, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Boatswain, 2, 1, false,
                RelicEffectType.Gloves_AttackLowerHealthStat,
                "Attack, lower enemy health stat 30% for 2 turns", 0, 0.30f, 2);
            AddEffect(RelicCategory.Gloves, UnitRole.Shipwright, 2, 1, false,
                RelicEffectType.Gloves_AttackForceTargetNearest,
                "Attack, target forced to attack closest next turn", 0, 0, 1);
            AddEffect(RelicCategory.Gloves, UnitRole.MasterGunner, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusPerGunnerUsed,
                "Attack, +10% per gunner relic used this game", 0, 0.10f, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.MasterAtArms, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusPerMArmCard,
                "Attack, +10% per Master-at-Arms card in hand", 0, 0.10f, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Navigator, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusPerBootsCard,
                "Attack, +30% per boots card in deck", 0, 0.30f, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Surgeon, 2, 1, true,
                RelicEffectType.Gloves_AttackOnEnemyHeal,
                "Passive: Attack any enemy that gets healed", 0, 0, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Cook, 2, 1, false,
                RelicEffectType.Gloves_AttackStasisTarget,
                "Put closest target in stasis 1 turn", 0, 0, 1);
            AddEffect(RelicCategory.Gloves, UnitRole.Swashbuckler, 2, 1, false,
                RelicEffectType.Gloves_AttackTwice,
                "Attack with default weapon 2 times", 2, 0, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Deckhand, 2, 1, false,
                RelicEffectType.Gloves_AttackHullDestroyEnergy,
                "Attack, if hull destroyed gain 1 energy", 0, 1, 0);

            // ==================== HAT V1 ====================
            AddEffect(RelicCategory.Hat, UnitRole.Captain, 2, 1, false,
                RelicEffectType.Hat_DrawCardsVulnerable,
                "Draw 2 cards, take 200% damage for 2 turns", 2, 2.0f, 2);
            AddEffect(RelicCategory.Hat, UnitRole.Quartermaster, 2, 1, false,
                RelicEffectType.Hat_DrawUltimate,
                "Draw an ultimate ability card", 1, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Helmsmaster, 2, 1, false,
                RelicEffectType.Hat_RestoreMoraleLowest,
                "Restore 30% morale to lowest morale ally", 0, 0.30f, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Boatswain, 2, 1, false,
                RelicEffectType.Hat_RestoreMoraleNearby,
                "10% morale to allies in 1 tile", 0, 0.10f, 0, 1);
            AddEffect(RelicCategory.Hat, UnitRole.Shipwright, 2, 1, false,
                RelicEffectType.Hat_FreeRumUsage,
                "This round 3 rum uses cost no grog", 3, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.MasterGunner, 2, 1, false,
                RelicEffectType.Hat_GenerateGrog,
                "Generate 2 grog tokens", 2, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.MasterAtArms, 2, 1, false,
                RelicEffectType.Hat_ReturnDamage,
                "Return 1 damage instance for 2 turns", 1, 0, 2);
            AddEffect(RelicCategory.Hat, UnitRole.Navigator, 2, 1, false,
                RelicEffectType.Hat_IncreaseHealthStat,
                "+25% health stat for 2 turns", 0, 0.25f, 2);
            AddEffect(RelicCategory.Hat, UnitRole.Surgeon, 2, 1, false,
                RelicEffectType.Hat_EnergyOnKnockback,
                "+2 energy if knocked back next turn", 2, 0, 1);
            AddEffect(RelicCategory.Hat, UnitRole.Cook, 2, 1, false,
                RelicEffectType.Hat_SwapEnemyByGrit,
                "Swap highest/lowest grit enemy positions", 0, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Swashbuckler, 2, 1, false,
                RelicEffectType.Hat_WeaponUseTwice,
                "Next weapon relic can be used twice", 0, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Deckhand, 2, 1, false,
                RelicEffectType.Hat_DrawWeaponRelic,
                "Draw a weapon relic card", 1, 0, 0);

            // ==================== HAT V2 ====================
            AddEffect(RelicCategory.Hat, UnitRole.Captain, 2, 1, false,
                RelicEffectType.Hat_DrawUltimateAbility,
                "Draw an ultimate ability card", 1, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Quartermaster, 2, 1, false,
                RelicEffectType.Hat_RestoreMoraleNearbyAllies,
                "10% morale to allies in 1 tile range", 0, 0.10f, 0, 1);
            AddEffect(RelicCategory.Hat, UnitRole.Helmsmaster, 2, 1, false,
                RelicEffectType.Hat_GenerateGrogTokens,
                "Generate 2 grog tokens", 2, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Boatswain, 2, 1, false,
                RelicEffectType.Hat_IncreaseHealthStatBuff,
                "+25% health stat for 2 turns", 0, 0.25f, 2);
            AddEffect(RelicCategory.Hat, UnitRole.Shipwright, 2, 1, false,
                RelicEffectType.Hat_SwapEnemyGritPositions,
                "Swap highest/lowest grit enemy positions", 0, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.MasterGunner, 2, 1, false,
                RelicEffectType.Hat_DrawWeaponRelicCard,
                "Draw a weapon relic card", 1, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.MasterAtArms, 2, 1, false,
                RelicEffectType.Hat_IncreaseEnemyWeaponCost,
                "Enemy next weapon relic costs +1", 0, 1, 1);
            AddEffect(RelicCategory.Hat, UnitRole.Navigator, 2, 1, false,
                RelicEffectType.Hat_DrawBootsRelicCard,
                "Draw a boots relic card", 1, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Surgeon, 2, 1, false,
                RelicEffectType.Hat_HealOnCaptainDamage,
                "Allies heal 10% when damaging enemy captain", 0, 0.10f, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Cook, 2, 1, false,
                RelicEffectType.Hat_MoveForwardAndHeal,
                "Move ally forward 1 tile, heal 10%", 1, 0.10f, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Swashbuckler, 2, 1, false,
                RelicEffectType.Hat_StealEnemyCard,
                "Steal random enemy card, reduce weapon cost", 1, 1, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Deckhand, 2, 1, false,
                RelicEffectType.Hat_DestroyObstaclesGainHull,
                "Destroy soft obstacles, +20% hull each", 0, 0.20f, 0);

            // ==================== COAT V1 ====================
            AddEffect(RelicCategory.Coat, UnitRole.Captain, 2, 1, false,
                RelicEffectType.Coat_BuffNearbyAimPower,
                "+20% Aim/Power to allies in 1 tile for 2 turns", 0, 0.20f, 2, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Quartermaster, 2, 1, false,
                RelicEffectType.Coat_DrawOnEnemyAttack,
                "Draw per enemy attack (3 max), enemy discards", 3, 0, 2);
            AddEffect(RelicCategory.Coat, UnitRole.Helmsmaster, 2, 1, false,
                RelicEffectType.Coat_ReduceMoraleDamage,
                "Allies take 30% less morale damage 2 turns", 0, 0.30f, 2);
            AddEffect(RelicCategory.Coat, UnitRole.Boatswain, 2, 1, false,
                RelicEffectType.Coat_PreventSurrender,
                "If ally would surrender, restore 20% morale", 0, 0.20f, 2);
            AddEffect(RelicCategory.Coat, UnitRole.Shipwright, 2, 1, false,
                RelicEffectType.Coat_ReduceRumEffect,
                "Nearby allies reduced rum effect this turn", 0, 0.50f, 1, 1);
            AddEffect(RelicCategory.Coat, UnitRole.MasterGunner, 2, 1, false,
                RelicEffectType.Coat_EnemyBuzzOnDamage,
                "Enemy buzz fills when dealing damage next turn", 0, 0, 1);
            AddEffect(RelicCategory.Coat, UnitRole.MasterAtArms, 2, 1, false,
                RelicEffectType.Coat_PreventDisplacement,
                "Nearby allies can't be knocked back next turn", 0, 0, 1, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Navigator, 2, 1, false,
                RelicEffectType.Coat_ProtectLowHP,
                "Lowest HP only targeted by lower HP enemies", 0, 0, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Surgeon, 2, 1, false,
                RelicEffectType.Coat_RowCantBeTargeted,
                "Allies behind in row can't be targeted 2 turns", 0, 0, 2);
            AddEffect(RelicCategory.Coat, UnitRole.Cook, 2, 1, false,
                RelicEffectType.Coat_ColumnDamageBoost,
                "+40% damage to allies in same column", 0, 0.40f, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Swashbuckler, 2, 1, false,
                RelicEffectType.Coat_FreeStow,
                "Next 2 stows are free", 2, 0, 0);
            AddEffect(RelicCategory.Coat, UnitRole.Deckhand, 2, 1, false,
                RelicEffectType.Coat_RowRangedProtection,
                "Row takes 50% less ranged damage next turn", 0, 0.50f, 1);

            // ==================== COAT V2 ====================
            AddEffect(RelicCategory.Coat, UnitRole.Captain, 2, 1, false,
                RelicEffectType.Coat_DrawOnEnemyAttackDiscard,
                "Per attack (3 max), draw card, enemy discards", 3, 0, 2);
            AddEffect(RelicCategory.Coat, UnitRole.Quartermaster, 2, 1, false,
                RelicEffectType.Coat_PreventSurrenderRestore,
                "If ally would surrender, restore 20% morale", 0, 0.20f, 2);
            AddEffect(RelicCategory.Coat, UnitRole.Helmsmaster, 2, 1, false,
                RelicEffectType.Coat_EnemyBuzzOnDealDamage,
                "Enemy buzz fills every time they deal damage", 0, 0, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Boatswain, 2, 1, false,
                RelicEffectType.Coat_ProtectLowestHP,
                "Lowest HP only targeted by lower HP enemies", 0, 0, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Shipwright, 2, 1, false,
                RelicEffectType.Coat_ColumnDamageBoostAllies,
                "+40% damage to allies in same column", 0, 0.40f, 1);
            AddEffect(RelicCategory.Coat, UnitRole.MasterGunner, 2, 1, false,
                RelicEffectType.Coat_RowRangedProtect,
                "Row takes 50% less ranged damage next turn", 0, 0.50f, 1);
            AddEffect(RelicCategory.Coat, UnitRole.MasterAtArms, 2, 1, false,
                RelicEffectType.Coat_NearbyDamageBoost,
                "+20% damage to nearby allies in 1 tile", 0, 0.20f, 1, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Navigator, 2, 1, false,
                RelicEffectType.Coat_DodgeFirstAttack,
                "First attacked ally dodges by moving 1 tile back", 0, 0, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Surgeon, 2, 1, true,
                RelicEffectType.Coat_KnockbackOnAllyDeath,
                "Passive: Knockback enemy when ally dies nearby", 0, 0, 0, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Cook, 2, 1, false,
                RelicEffectType.Coat_ClearDebuffsNearby,
                "Clear all debuffs from nearby allies", 0, 0, 0, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Swashbuckler, 2, 1, false,
                RelicEffectType.Coat_CurseTileTrapped,
                "Curse enemy tile: trapped + 10% more damage", 0, 0.10f, 0);
            AddEffect(RelicCategory.Coat, UnitRole.Deckhand, 2, 1, false,
                RelicEffectType.Coat_BuffRandomTile,
                "Buff tile: -15% damage taken, +15% dealt", 0, 0.15f, 0);

            // ==================== TRINKET V1 (Passive) ====================
            AddEffect(RelicCategory.Trinket, UnitRole.Captain, 2, 1, true,
                RelicEffectType.Trinket_BonusDamagePerCard,
                "Passive: +20% weapon damage per card in hand", 0, 0.20f, 0);
            AddEffect(RelicCategory.Trinket, UnitRole.Quartermaster, 2, 1, true,
                RelicEffectType.Trinket_BonusVsCaptain,
                "Passive: +20% damage vs enemy captain", 0, 0.20f, 0);
            AddEffect(RelicCategory.Trinket, UnitRole.Helmsmaster, 2, 1, true,
                RelicEffectType.Trinket_ImmuneMoraleFocusFire,
                "Passive: Immune to morale focus fire", 0, 0, 0);
            AddEffect(RelicCategory.Trinket, UnitRole.Boatswain, 2, 1, true,
                RelicEffectType.Trinket_EnemySurrenderEarly,
                "Passive: Enemies surrender at 30% morale", 0, 0.30f, 0);
            AddEffect(RelicCategory.Trinket, UnitRole.Shipwright, 2, 1, true,
                RelicEffectType.Trinket_DamageByBuzz,
                "Passive: +damage based on own buzz", 0, 0, 0);
            AddEffect(RelicCategory.Trinket, UnitRole.MasterGunner, 2, 1, true,
                RelicEffectType.Trinket_KnockbackIncreasesBuzz,
                "Passive: Knockback increases enemy buzz", 0, 0, 0);
            AddEffect(RelicCategory.Trinket, UnitRole.MasterAtArms, 2, 1, true,
                RelicEffectType.Trinket_ReduceDamageFromClosest,
                "Passive: Closest enemy does -20% damage", 0, 0.20f, 0);
            AddEffect(RelicCategory.Trinket, UnitRole.Navigator, 2, 1, true,
                RelicEffectType.Trinket_DrawIfHighHP,
                "Passive: Draw extra if HP above 60%", 0, 0.60f, 0);
            AddEffect(RelicCategory.Trinket, UnitRole.Surgeon, 2, 1, true,
                RelicEffectType.Trinket_TauntFirstAttack,
                "Passive: Taunt first attack per enemy turn", 0, 0, 0);
            AddEffect(RelicCategory.Trinket, UnitRole.Cook, 2, 1, true,
                RelicEffectType.Trinket_KnockbackAttacker,
                "Passive: Knockback attacker once per turn", 0, 0, 0);
            AddEffect(RelicCategory.Trinket, UnitRole.Swashbuckler, 2, 1, true,
                RelicEffectType.Trinket_RowEnemiesLessDamage,
                "Passive: Enemies in row do -10% damage", 0, 0.10f, 0);
            AddEffect(RelicCategory.Trinket, UnitRole.Deckhand, 2, 1, true,
                RelicEffectType.Trinket_RowEnemiesTakeMore,
                "Passive: Enemies in row take +10% damage", 0, 0.10f, 0);

            // ==================== TRINKET V2 (Passive) ====================
            AddEffect(RelicCategory.Trinket, UnitRole.Captain, 2, 1, true,
                RelicEffectType.Trinket_BonusVsCaptainTarget,
                "Passive: +20% damage vs Captain targets", 0, 0.20f, 0);
            AddEffect(RelicCategory.Trinket, UnitRole.Quartermaster, 2, 1, true,
                RelicEffectType.Trinket_EnemySurrenderAt30,
                "Passive: Enemies surrender at 30% morale", 0, 0.30f, 0);
            AddEffect(RelicCategory.Trinket, UnitRole.Helmsmaster, 2, 1, true,
                RelicEffectType.Trinket_KnockbackFillsBuzz,
                "Passive: When enemies knocked back, buzz increases", 0, 0, 0);
            AddEffect(RelicCategory.Trinket, UnitRole.Boatswain, 2, 1, true,
                RelicEffectType.Trinket_DrawIfHighHealth,
                "Passive: Draw extra if HP above 60%", 0, 0.60f, 0);
            AddEffect(RelicCategory.Trinket, UnitRole.Shipwright, 2, 1, true,
                RelicEffectType.Trinket_KnockbackAttackerOnce,
                "Passive: Knockback attacker 1 tile once per turn", 0, 1, 0);
            AddEffect(RelicCategory.Trinket, UnitRole.MasterGunner, 2, 1, true,
                RelicEffectType.Trinket_RowEnemiesTakeMoreDmg,
                "Passive: Enemies in row take +10% damage", 0, 0.10f, 0);
            AddEffect(RelicCategory.Trinket, UnitRole.MasterAtArms, 2, 1, true,
                RelicEffectType.Trinket_NearbyAlliesPowerBuff,
                "Passive: Nearby allies +30% Power stat", 0, 0.30f, 0, 1);
            AddEffect(RelicCategory.Trinket, UnitRole.Navigator, 2, 1, true,
                RelicEffectType.Trinket_NearbyIgnoreObstacles,
                "Passive: Nearby allies ignore soft obstacles", 0, 0, 0, 1);
            AddEffect(RelicCategory.Trinket, UnitRole.Surgeon, 2, 1, true,
                RelicEffectType.Trinket_GlobalAllyRadius,
                "Passive: Nearby allies radius = whole board", 0, 0, 0);
            AddEffect(RelicCategory.Trinket, UnitRole.Cook, 2, 1, true,
                RelicEffectType.Trinket_DrawIfLowHP,
                "Passive: Draw extra if below 50% HP", 0, 0.50f, 0);
            AddEffect(RelicCategory.Trinket, UnitRole.Swashbuckler, 2, 1, true,
                RelicEffectType.Trinket_EnemiesLoseSpeed,
                "Passive: All enemies -10% Speed stat", 0, 0.10f, 0);
            AddEffect(RelicCategory.Trinket, UnitRole.Deckhand, 2, 1, true,
                RelicEffectType.Trinket_HullRegenOnSurvive,
                "Passive: If hull survives attack, discard enemy card", 0, 0, 0);

            // ==================== TOTEM V1 ====================
            AddEffect(RelicCategory.Totem, UnitRole.Captain, 2, 1, false,
                RelicEffectType.Totem_SummonCannon,
                "Summon cannon (250 HP), attacks random enemy", 250, 0, 0);
            AddEffect(RelicCategory.Totem, UnitRole.Quartermaster, 2, 1, false,
                RelicEffectType.Totem_CurseCaptainReflect,
                "Enemy captain damage reflects to allies this turn", 0, 0, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Helmsmaster, 2, 1, false,
                RelicEffectType.Totem_RallyNoMoraleDamage,
                "Rally: Nearby allies no morale damage next turn", 0, 0, 1, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Boatswain, 2, 1, true,
                RelicEffectType.Totem_EnemyDeathMoraleSwing,
                "Passive: Enemy death = morale loss + player gain", 0, 0.05f, 0);
            AddEffect(RelicCategory.Totem, UnitRole.Shipwright, 2, 1, false,
                RelicEffectType.Totem_SummonHighQualityRum,
                "Add 2 high quality rum to inventory", 2, 0, 0);
            AddEffect(RelicCategory.Totem, UnitRole.MasterGunner, 2, 0, false,
                RelicEffectType.Totem_ConvertGrogToEnergy,
                "Convert 2 grog tokens into 1 energy", 2, 1, 0);
            AddEffect(RelicCategory.Totem, UnitRole.MasterAtArms, 2, 1, false,
                RelicEffectType.Totem_StunOnKnockback,
                "If knocked back next turn, stun attacker", 0, 0, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Navigator, 2, 1, false,
                RelicEffectType.Totem_SummonAnchorHealthBuff,
                "Summon anchor: +25% health to nearby 2 turns", 0, 0.25f, 2, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Surgeon, 2, 1, false,
                RelicEffectType.Totem_SummonTargetDummy,
                "Summon target dummy in front row (250 HP)", 250, 0, 0);
            AddEffect(RelicCategory.Totem, UnitRole.Cook, 2, 1, false,
                RelicEffectType.Totem_SummonObstacleDisplace,
                "Summon obstacle at target, displace them", 0, 0, 0);
            AddEffect(RelicCategory.Totem, UnitRole.Swashbuckler, 2, 1, false,
                RelicEffectType.Totem_SummonExplodingBarrels,
                "Summon 3 barrels, explode in 2 turns", 3, 0, 2, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Deckhand, 2, 1, false,
                RelicEffectType.Totem_CurseRangedWeapons,
                "Enemy ranged -50% damage next turn", 0, 0.50f, 1);

            // ==================== TOTEM V2 ====================
            AddEffect(RelicCategory.Totem, UnitRole.Captain, 2, 1, false,
                RelicEffectType.Totem_CurseCaptainDamageReflect,
                "Captain damage reflects to ALL enemy allies", 0, 0, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Quartermaster, 2, 1, true,
                RelicEffectType.Totem_EnemyDeathMoraleSwap,
                "Passive: Enemy death = enemies lose, allies gain morale", 0, 0.05f, 0);
            AddEffect(RelicCategory.Totem, UnitRole.Helmsmaster, 2, 0, false,
                RelicEffectType.Totem_ConvertGrogToEnergyFree,
                "Convert 2 grog to 1 energy (0 cost)", 2, 1, 0);
            AddEffect(RelicCategory.Totem, UnitRole.Boatswain, 2, 1, false,
                RelicEffectType.Totem_SummonAnchorHealthAura,
                "Summon anchor: +25% health nearby 2 turns", 0, 0.25f, 2, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Shipwright, 2, 1, false,
                RelicEffectType.Totem_SummonObstacleDisplaceTarget,
                "Summon obstacle at target, displace target", 0, 0, 0);
            AddEffect(RelicCategory.Totem, UnitRole.MasterGunner, 2, 1, false,
                RelicEffectType.Totem_CurseRangedWeaponsDamage,
                "Enemy ranged weapons -50% damage", 0, 0.50f, 1);
            AddEffect(RelicCategory.Totem, UnitRole.MasterAtArms, 2, 1, false,
                RelicEffectType.Totem_EarthquakeHazards,
                "3 earthquake hazards, displace units at end", 3, 0, 0);
            AddEffect(RelicCategory.Totem, UnitRole.Navigator, 2, 1, false,
                RelicEffectType.Totem_DisableEnemyRelics,
                "Disable enemy non-weapon relics 1 turn", 0, 0, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Surgeon, 2, 1, false,
                RelicEffectType.Totem_SummonHealingPotions,
                "Summon 3 healing potions (200 HP) on map", 3, 200, 0);
            AddEffect(RelicCategory.Totem, UnitRole.Cook, 2, 1, false,
                RelicEffectType.Totem_SummonDebuffObstacle,
                "Summon obstacle: -50% stats to nearby enemies", 0, 0.50f, 0, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Swashbuckler, 2, 1, false,
                RelicEffectType.Totem_DisableEnemyPassives,
                "Enemies can't use passives next turn", 0, 0, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Deckhand, 2, 1, false,
                RelicEffectType.Totem_PullEnemiestoRow,
                "Pull nearby enemies to same row", 0, 0, 0, 1);

            // ==================== ULTIMATE V1 ====================
            AddEffect(RelicCategory.Ultimate, UnitRole.Captain, 1, 3, false,
                RelicEffectType.Ultimate_ShipCannon,
                "Fire cannon: 3 shots of 200 damage + fire", 200, 3, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Quartermaster, 1, 3, false,
                RelicEffectType.Ultimate_MarkCaptainOnly,
                "Attack captain, mark as only target this turn", 0, 0, 1, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Helmsmaster, 1, 3, false,
                RelicEffectType.Ultimate_ReflectMoraleDamage,
                "Reflect ally morale damage to enemies", 0, 0, 1, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Boatswain, 1, 3, false,
                RelicEffectType.Ultimate_ReviveAlly,
                "Revive dead/surrendered at 30% HP/morale", 0, 0.30f, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Shipwright, 1, 3, false,
                RelicEffectType.Ultimate_FullBuzzAttack,
                "Attack, target buzz full for 2 turns", 0, 0, 2, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.MasterGunner, 1, 3, false,
                RelicEffectType.Ultimate_RumBottleAoE,
                "200 dmg AoE, rum spill increases buzz", 200, 0, 3, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.MasterAtArms, 1, 3, false,
                RelicEffectType.Ultimate_SummonHardObstacles,
                "Summon 3 hard obstacles front row, 2 turns", 3, 0, 2, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Navigator, 1, 3, false,
                RelicEffectType.Ultimate_IgnoreHighestHP,
                "Highest HP enemy ignored this turn", 0, 0, 1, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Surgeon, 1, 3, false,
                RelicEffectType.Ultimate_KnockbackToLastColumn,
                "Attack, knockback to last column", 0, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Cook, 1, 3, false,
                RelicEffectType.Ultimate_AttackKnockbackNearby,
                "Attack, knockback nearby enemies", 0, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Swashbuckler, 1, 3, false,
                RelicEffectType.Ultimate_StunAoE,
                "Attack, stun target + nearby enemies", 0, 0, 1, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Deckhand, 1, 3, false,
                RelicEffectType.Ultimate_MassiveSingleTarget,
                "+300% damage if no nearby enemies", 0, 3.0f, 0, 1, RelicRarity.Unique);

            // ==================== ULTIMATE V2 ====================
            AddEffect(RelicCategory.Ultimate, UnitRole.Captain, 1, 3, false,
                RelicEffectType.Ultimate_AttackCaptainMark,
                "Attack captain, mark as only target", 0, 0, 1, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Quartermaster, 1, 3, false,
                RelicEffectType.Ultimate_ReviveAllyFull,
                "Revive dead/surrendered at 30%", 0, 0.30f, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Helmsmaster, 1, 3, false,
                RelicEffectType.Ultimate_RumBottleAoEBuzz,
                "200 dmg AoE, rum spill increases buzz", 200, 0, 3, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Boatswain, 1, 3, false,
                RelicEffectType.Ultimate_IgnoreHighestHPEnemy,
                "Highest HP (not captain) ignored", 0, 0, 1, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Shipwright, 1, 3, false,
                RelicEffectType.Ultimate_AttackKnockbackNearbyAll,
                "Attack, knockback all nearby 1 tile", 0, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.MasterGunner, 1, 3, false,
                RelicEffectType.Ultimate_MassiveSingleTargetBonus,
                "+300% if no nearby enemies", 0, 3.0f, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.MasterAtArms, 1, 3, false,
                RelicEffectType.Ultimate_AttackAllEnemiesRow,
                "Attack closest + 350 damage to row", 350, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Navigator, 1, 3, false,
                RelicEffectType.Ultimate_SwapClosestFurthest,
                "Swap closest and furthest enemy positions", 0, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Surgeon, 1, 3, false,
                RelicEffectType.Ultimate_FullHealthRestore,
                "Fully restore any unit's health", 0, 1.0f, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Cook, 1, 3, false,
                RelicEffectType.Ultimate_SetColumnOnFire,
                "Set closest target's column on fire", 0, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Swashbuckler, 1, 3, true,
                RelicEffectType.Ultimate_FourWeaponsSurrender,
                "Passive: 4 weapons on same target = surrender", 4, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Deckhand, 1, 3, false,
                RelicEffectType.Ultimate_ClearHazardsPrevent,
                "Clear hazards, prevent new ones", 0, 0, 1, 1, RelicRarity.Unique);

            // ==================== PASSIVE UNIQUE V1 ====================
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Captain, 0, 0, true,
                RelicEffectType.PassiveUnique_ExtraEnergy,
                "Passive: +1 max energy each turn", 1, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Quartermaster, 0, 0, true,
                RelicEffectType.PassiveUnique_ExtraCards,
                "Passive: +2 cards each turn", 2, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Helmsmaster, 0, 0, true,
                RelicEffectType.PassiveUnique_DeathStrikeByMorale,
                "Passive: Higher morale = attack on death chance", 0, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Boatswain, 0, 0, true,
                RelicEffectType.PassiveUnique_LowerSurrenderThreshold,
                "Passive: Allies surrender at 10%", 0, 0.10f, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Shipwright, 0, 0, true,
                RelicEffectType.PassiveUnique_NoBuzzDownside,
                "Passive: No penalty when buzz full", 0, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.PassiveUnique, UnitRole.MasterGunner, 0, 0, true,
                RelicEffectType.PassiveUnique_DrawPerGrog,
                "Passive: Draw extra per grog", 0, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.PassiveUnique, UnitRole.MasterAtArms, 0, 0, true,
                RelicEffectType.PassiveUnique_DrawOnLowDamage,
                "Passive: Draw if <20% HP damage taken", 0, 0.20f, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Navigator, 0, 0, true,
                RelicEffectType.PassiveUnique_CounterAttack,
                "Passive: Attack back when ally damaged", 0, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Surgeon, 0, 0, true,
                RelicEffectType.PassiveUnique_GritAura,
                "Passive: Nearby allies +5% of unit's grit", 0, 0.05f, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Cook, 0, 0, true,
                RelicEffectType.PassiveUnique_BonusVsLowGrit,
                "Passive: +20% vs lower grit targets", 0, 0.20f, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Swashbuckler, 0, 0, true,
                RelicEffectType.PassiveUnique_IgnoreRoles,
                "Passive: Ignore Shipwright/Boatswain roles", 0, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Deckhand, 0, 0, true,
                RelicEffectType.PassiveUnique_BonusVsLowHP,
                "Passive: Bonus vs <50% HP targets", 0, 0.50f, 0, 1, RelicRarity.Unique);

            // ==================== PASSIVE UNIQUE V2 ====================
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Captain, 0, 0, true,
                RelicEffectType.PassiveUnique_ExtraCardsEachTurn,
                "Passive: +2 cards each turn", 2, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Quartermaster, 0, 0, true,
                RelicEffectType.PassiveUnique_LowerAllySurrender,
                "Passive: Allies surrender at 10%", 0, 0.10f, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Helmsmaster, 0, 0, true,
                RelicEffectType.PassiveUnique_DrawPerGrogToken,
                "Passive: Draw extra per grog token", 0, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Boatswain, 0, 0, true,
                RelicEffectType.PassiveUnique_CounterAttackAlly,
                "Passive: Attack back when any ally damaged", 0, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Shipwright, 0, 0, true,
                RelicEffectType.PassiveUnique_BonusVsLowGritTarget,
                "Passive: +20% vs lower grit targets", 0, 0.20f, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.PassiveUnique, UnitRole.MasterGunner, 0, 0, true,
                RelicEffectType.PassiveUnique_BonusVsLowHPTarget,
                "Passive: Bonus vs <50% HP targets", 0, 0.50f, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.PassiveUnique, UnitRole.MasterAtArms, 0, 0, true,
                RelicEffectType.PassiveUnique_KillRestoreHealth,
                "Passive: Kill/surrender restores 20% HP", 0, 0.20f, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Navigator, 0, 0, true,
                RelicEffectType.PassiveUnique_AllAlliesExtraMove,
                "Passive: All allies +1 movement", 1, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Surgeon, 0, 0, true,
                RelicEffectType.PassiveUnique_KillRestoreAllyHP,
                "Passive: Kill heals all allies 5%", 0, 0.05f, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Cook, 0, 0, true,
                RelicEffectType.PassiveUnique_RelicsNotConsumed,
                "Passive: Relics can be replayed if energy allows", 0, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Swashbuckler, 0, 0, true,
                RelicEffectType.PassiveUnique_EnemyBootsLimited,
                "Passive: Enemies limited to 1 tile movement", 1, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Deckhand, 0, 0, true,
                RelicEffectType.PassiveUnique_BonusDmgPerHullDestroyed,
                "Passive: +30% damage per hull destroyed", 0, 0.30f, 0, 1, RelicRarity.Unique);

            Debug.Log($"RelicEffectsDatabase populated with {allEffects.Count} effects (192 expected)");
        }

        private void AddEffect(RelicCategory category, UnitRole role, int copies, int cost, bool isPassive,
            RelicEffectType effectType, string description, float val1, float val2, int duration, int tileRange = 1)
        {
            AddEffect(category, role, copies, cost, isPassive, effectType, description, val1, val2, duration, tileRange, RelicRarity.Common);
        }

        private void AddEffect(RelicCategory category, UnitRole role, int copies, int cost, bool isPassive,
            RelicEffectType effectType, string description, float val1, float val2, int duration, int tileRange, RelicRarity rarity)
        {
            string effectName = $"{GetRoleDisplayName(role)} {category}";
            
            allEffects.Add(new RelicEffectData
            {
                category = category,
                roleTag = role,
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
    }
}