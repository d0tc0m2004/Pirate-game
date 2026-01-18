using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Enums;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Database containing all relic effects.
    /// 8 categories x 12 roles = 96 effects total.
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
        /// Get effect for a specific category and role.
        /// </summary>
        public RelicEffectData GetEffect(RelicCategory category, UnitRole roleTag)
        {
            return allEffects.FirstOrDefault(e => e.category == category && e.roleTag == roleTag);
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
        /// Create the default database with all 96 effects.
        /// </summary>
        public static RelicEffectsDatabase CreateDefaultDatabase()
        {
            var db = ScriptableObject.CreateInstance<RelicEffectsDatabase>();
            db.PopulateAllEffects();
            return db;
        }

        /// <summary>
        /// Get role display name for effect naming.
        /// </summary>
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
        /// Populate all effects from the design spreadsheet.
        /// </summary>
        public void PopulateAllEffects()
        {
            allEffects.Clear();
            
            // ==================== BOOTS (2 copies, 1 energy) ====================
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

            // ==================== GLOVES (2 copies, 1 energy) ====================
            AddEffect(RelicCategory.Gloves, UnitRole.Captain, 2, 1, false,
                RelicEffectType.Gloves_AttackReduceEnemyDraw,
                "Attack with default weapon, enemy draws 1 less next turn", 0, 1, 1);
                
            AddEffect(RelicCategory.Gloves, UnitRole.Quartermaster, 2, 1, false,
                RelicEffectType.Gloves_AttackIncreaseEnemyCost,
                "Attack with default weapon, enemy next card costs +1", 0, 1, 1);
                
            AddEffect(RelicCategory.Gloves, UnitRole.Helmsmaster, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusByMissingMorale,
                "Attack with default weapon, +damage by enemy missing morale", 0, 0, 0);
                
            AddEffect(RelicCategory.Gloves, UnitRole.Boatswain, 2, 1, false,
                RelicEffectType.Gloves_AttackMarkMoraleFocus,
                "Attack with default weapon, mark for morale focus fire 2 turns", 0, 0, 2);
                
            AddEffect(RelicCategory.Gloves, UnitRole.Shipwright, 2, 1, false,
                RelicEffectType.Gloves_AttackPreventBuzzReduce,
                "Attack with default weapon, prevent target buzz reduction 2 turns", 0, 0, 2);
                
            AddEffect(RelicCategory.Gloves, UnitRole.MasterGunner, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusPerGrog,
                "Attack with default weapon, +20% damage per grog token", 0, 0.20f, 0);
                
            AddEffect(RelicCategory.Gloves, UnitRole.MasterAtArms, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusIfMoreHP,
                "Attack with default weapon, +20% if more HP than target", 0, 0.20f, 0);
                
            AddEffect(RelicCategory.Gloves, UnitRole.Navigator, 2, 1, false,
                RelicEffectType.Gloves_AttackLowerEnemyHealth,
                "Attack with default weapon, lower enemy health stat 30% for 2 turns", 0, 0.30f, 2);
                
            AddEffect(RelicCategory.Gloves, UnitRole.Surgeon, 2, 1, false,
                RelicEffectType.Gloves_AttackPushForward,
                "Attack with default weapon, push target forward 1 tile", 0, 1, 0);
                
            AddEffect(RelicCategory.Gloves, UnitRole.Cook, 2, 1, false,
                RelicEffectType.Gloves_AttackForceTargetClosest,
                "Attack with default weapon, target must attack closest next turn", 0, 0, 1);
                
            AddEffect(RelicCategory.Gloves, UnitRole.Swashbuckler, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusPerCardPlayed,
                "Attack with default weapon, +10% per card played this round", 0, 0.10f, 0);
                
            AddEffect(RelicCategory.Gloves, UnitRole.Deckhand, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusPerGunnerRelic,
                "Attack with default weapon, +10% per Master Gunner relic used this game", 0, 0.10f, 0);

            // ==================== HAT (2 copies, 1 energy) ====================
            AddEffect(RelicCategory.Hat, UnitRole.Captain, 2, 1, false,
                RelicEffectType.Hat_DrawCardsVulnerable,
                "Draw 2 cards, take 200% damage for 2 turns", 2, 2.0f, 2);
                
            AddEffect(RelicCategory.Hat, UnitRole.Quartermaster, 2, 1, false,
                RelicEffectType.Hat_DrawUltimate,
                "Draw an ultimate ability card", 0, 0, 0);
                
            AddEffect(RelicCategory.Hat, UnitRole.Helmsmaster, 2, 1, false,
                RelicEffectType.Hat_RestoreMoraleLowest,
                "Restore 30% morale to lowest morale ally", 0, 0.30f, 0);
                
            AddEffect(RelicCategory.Hat, UnitRole.Boatswain, 2, 1, false,
                RelicEffectType.Hat_RestoreMoraleNearby,
                "Restore 10% morale to allies in 1 tile", 0, 0.10f, 0, 1);
                
            AddEffect(RelicCategory.Hat, UnitRole.Shipwright, 2, 1, false,
                RelicEffectType.Hat_FreeRumUsage,
                "3 rum uses cost no grog this round", 3, 0, 0);
                
            AddEffect(RelicCategory.Hat, UnitRole.MasterGunner, 2, 1, false,
                RelicEffectType.Hat_GenerateGrog,
                "Generate 2 grog tokens", 2, 0, 0);
                
            AddEffect(RelicCategory.Hat, UnitRole.MasterAtArms, 2, 1, false,
                RelicEffectType.Hat_ReturnDamage,
                "Return 1 damage instance for 2 turns", 1, 0, 2);
                
            AddEffect(RelicCategory.Hat, UnitRole.Navigator, 2, 1, false,
                RelicEffectType.Hat_IncreaseHealthStat,
                "Increase health stat by 25% for 2 turns", 0, 0.25f, 2);
                
            AddEffect(RelicCategory.Hat, UnitRole.Surgeon, 2, 1, false,
                RelicEffectType.Hat_EnergyOnKnockback,
                "Gain 2 energy if knocked back next turn", 2, 0, 1);
                
            AddEffect(RelicCategory.Hat, UnitRole.Cook, 2, 1, false,
                RelicEffectType.Hat_SwapEnemyByGrit,
                "Swap highest and lowest grit enemy positions", 0, 0, 0);
                
            AddEffect(RelicCategory.Hat, UnitRole.Swashbuckler, 2, 1, false,
                RelicEffectType.Hat_WeaponUseTwice,
                "Next weapon relic can be used twice", 0, 0, 0);
                
            AddEffect(RelicCategory.Hat, UnitRole.Deckhand, 2, 1, false,
                RelicEffectType.Hat_DrawWeaponRelic,
                "Draw a weapon relic card", 0, 0, 0);

            // ==================== COAT (2 copies, 1 energy) ====================
            AddEffect(RelicCategory.Coat, UnitRole.Captain, 2, 1, false,
                RelicEffectType.Coat_BuffNearbyAimPower,
                "Allies in 1 tile gain +20% Aim and Power for 2 turns", 0, 0.20f, 2, 1);
                
            AddEffect(RelicCategory.Coat, UnitRole.Quartermaster, 2, 1, false,
                RelicEffectType.Coat_DrawOnEnemyAttack,
                "Per enemy attack (max 3): draw card, enemy discards 1 next turn", 3, 0, 2);
                
            AddEffect(RelicCategory.Coat, UnitRole.Helmsmaster, 2, 1, false,
                RelicEffectType.Coat_ReduceMoraleDamage,
                "Allies take 30% less morale damage for 2 turns", 0, 0.30f, 2);
                
            AddEffect(RelicCategory.Coat, UnitRole.Boatswain, 2, 1, false,
                RelicEffectType.Coat_PreventSurrender,
                "Buff ally: if would surrender, restore 20% morale instead", 0, 0.20f, 2);
                
            AddEffect(RelicCategory.Coat, UnitRole.Shipwright, 2, 1, false,
                RelicEffectType.Coat_ReduceRumEffect,
                "Nearby allies have reduced rum negative effects", 0, 0, 1, 1);
                
            AddEffect(RelicCategory.Coat, UnitRole.MasterGunner, 2, 1, false,
                RelicEffectType.Coat_EnemyBuzzOnDamage,
                "Enemies' buzz fills when they deal damage next turn", 0, 0, 1);
                
            AddEffect(RelicCategory.Coat, UnitRole.MasterAtArms, 2, 1, false,
                RelicEffectType.Coat_PreventDisplacement,
                "Nearby allies can't be knocked back next enemy turn", 0, 0, 1, 1);
                
            AddEffect(RelicCategory.Coat, UnitRole.Navigator, 2, 1, false,
                RelicEffectType.Coat_ProtectLowHP,
                "Lowest HP ally can only be targeted by lower HP enemies", 0, 0, 1);
                
            AddEffect(RelicCategory.Coat, UnitRole.Surgeon, 2, 1, false,
                RelicEffectType.Coat_RowCantBeTargeted,
                "Allies behind this unit in row can't be targeted for 2 turns", 0, 0, 2);
                
            AddEffect(RelicCategory.Coat, UnitRole.Cook, 2, 1, false,
                RelicEffectType.Coat_ColumnDamageBoost,
                "Allies in same column deal +40% damage", 0, 0.40f, 1);
                
            AddEffect(RelicCategory.Coat, UnitRole.Swashbuckler, 2, 1, false,
                RelicEffectType.Coat_FreeStow,
                "Next 2 stows have no cost", 2, 0, 0);
                
            AddEffect(RelicCategory.Coat, UnitRole.Deckhand, 2, 1, false,
                RelicEffectType.Coat_RowRangedProtection,
                "Allies in row take 50% less ranged damage next turn", 0, 0.50f, 1);

            // ==================== TRINKET (Passive, no copies) ====================
            AddEffect(RelicCategory.Trinket, UnitRole.Captain, 0, 0, true,
                RelicEffectType.Trinket_BonusDamagePerCard,
                "Passive: +20% weapon damage per card in hand", 0, 0.20f, 0);
                
            AddEffect(RelicCategory.Trinket, UnitRole.Quartermaster, 0, 0, true,
                RelicEffectType.Trinket_BonusVsCaptain,
                "Passive: +20% damage vs enemy captain", 0, 0.20f, 0);
                
            AddEffect(RelicCategory.Trinket, UnitRole.Helmsmaster, 0, 0, true,
                RelicEffectType.Trinket_ImmuneMoraleFocusFire,
                "Passive: Immune to morale focus fire", 0, 0, 0);
                
            AddEffect(RelicCategory.Trinket, UnitRole.Boatswain, 0, 0, true,
                RelicEffectType.Trinket_EnemySurrenderEarly,
                "Passive: Enemies surrender at 30% morale", 0, 0.30f, 0);
                
            AddEffect(RelicCategory.Trinket, UnitRole.Shipwright, 0, 0, true,
                RelicEffectType.Trinket_DamageByBuzz,
                "Passive: Bonus damage based on own buzz level", 0, 0, 0);
                
            AddEffect(RelicCategory.Trinket, UnitRole.MasterGunner, 0, 0, true,
                RelicEffectType.Trinket_KnockbackIncreasesBuzz,
                "Passive: Knockback increases enemy buzz", 0, 0, 0);
                
            AddEffect(RelicCategory.Trinket, UnitRole.MasterAtArms, 0, 0, true,
                RelicEffectType.Trinket_ReduceDamageFromClosest,
                "Passive: Closest enemy deals -20% damage to this unit", 0, 0.20f, 0);
                
            AddEffect(RelicCategory.Trinket, UnitRole.Navigator, 0, 0, true,
                RelicEffectType.Trinket_DrawIfHighHP,
                "Passive: Draw extra card each turn if HP above 60%", 0, 0.60f, 0);
                
            AddEffect(RelicCategory.Trinket, UnitRole.Surgeon, 0, 0, true,
                RelicEffectType.Trinket_TauntFirstAttack,
                "Passive: Taunt first attack per enemy turn", 0, 0, 0);
                
            AddEffect(RelicCategory.Trinket, UnitRole.Cook, 0, 0, true,
                RelicEffectType.Trinket_KnockbackAttacker,
                "Passive: Knockback attacker once per turn when attacked", 1, 0, 0);
                
            AddEffect(RelicCategory.Trinket, UnitRole.Swashbuckler, 0, 0, true,
                RelicEffectType.Trinket_RowEnemiesLessDamage,
                "Passive: Enemies in same row deal -10% damage", 0, 0.10f, 0);
                
            AddEffect(RelicCategory.Trinket, UnitRole.Deckhand, 0, 0, true,
                RelicEffectType.Trinket_RowEnemiesTakeMore,
                "Passive: Enemies in same row take +10% damage", 0, 0.10f, 0);

            // ==================== TOTEM (2 copies, 1 energy) ====================
            AddEffect(RelicCategory.Totem, UnitRole.Captain, 2, 1, false,
                RelicEffectType.Totem_SummonCannon,
                "Summon cannon (250 HP) that attacks random enemy each turn", 250, 0, 0);
                
            AddEffect(RelicCategory.Totem, UnitRole.Quartermaster, 2, 1, false,
                RelicEffectType.Totem_CurseCaptainReflect,
                "Curse: enemy captain damage reflects to all enemy allies this turn", 0, 0, 1);
                
            AddEffect(RelicCategory.Totem, UnitRole.Helmsmaster, 2, 1, false,
                RelicEffectType.Totem_RallyNoMoraleDamage,
                "Rally: allies in 1 tile take no morale damage next turn", 0, 0, 1, 1);
                
            AddEffect(RelicCategory.Totem, UnitRole.Boatswain, 0, 0, true,
                RelicEffectType.Totem_EnemyDeathMoraleSwing,
                "Passive: Enemy death = enemies lose morale, you gain morale", 0, 0, 0);
                
            AddEffect(RelicCategory.Totem, UnitRole.Shipwright, 2, 1, false,
                RelicEffectType.Totem_SummonHighQualityRum,
                "Add 2 high quality rum to inventory", 2, 0, 0);
                
            AddEffect(RelicCategory.Totem, UnitRole.MasterGunner, 2, 0, false,
                RelicEffectType.Totem_ConvertGrogToEnergy,
                "Convert 2 grog tokens into 1 energy", 2, 1, 0);
                
            AddEffect(RelicCategory.Totem, UnitRole.MasterAtArms, 2, 1, false,
                RelicEffectType.Totem_StunOnKnockback,
                "If this target is knocked back next turn, stun attacker", 0, 0, 1);
                
            AddEffect(RelicCategory.Totem, UnitRole.Navigator, 2, 1, false,
                RelicEffectType.Totem_SummonAnchorHealthBuff,
                "Summon anchor: +25% health to nearby allies for 2 turns", 0, 0.25f, 2, 1);
                
            AddEffect(RelicCategory.Totem, UnitRole.Surgeon, 2, 1, false,
                RelicEffectType.Totem_SummonTargetDummy,
                "Summon target dummy in front row (250 HP)", 250, 0, 0);
                
            AddEffect(RelicCategory.Totem, UnitRole.Cook, 2, 1, false,
                RelicEffectType.Totem_SummonObstacleDisplace,
                "Summon soft obstacle at target location, displace target", 0, 0, 0);
                
            AddEffect(RelicCategory.Totem, UnitRole.Swashbuckler, 2, 1, false,
                RelicEffectType.Totem_SummonExplodingBarrels,
                "Summon 3 barrels on enemy side that explode after 2 turns", 3, 0, 2, 1);
                
            AddEffect(RelicCategory.Totem, UnitRole.Deckhand, 2, 1, false,
                RelicEffectType.Totem_CurseRangedWeapons,
                "Curse enemy ranged weapons: 50% less damage next turn", 0, 0.50f, 1);

            // ==================== ULTIMATE (1 copy, 3 energy) ====================
            AddEffect(RelicCategory.Ultimate, UnitRole.Captain, 1, 3, false,
                RelicEffectType.Ultimate_ShipCannon,
                "Fire ship cannon at 3 random enemy locations: 200 damage + fire hazard", 200, 3, 0, 1, RelicRarity.Unique);
                
            AddEffect(RelicCategory.Ultimate, UnitRole.Quartermaster, 1, 3, false,
                RelicEffectType.Ultimate_MarkCaptainOnly,
                "Attack enemy captain, mark as only target this turn", 0, 0, 1, 1, RelicRarity.Unique);
                
            AddEffect(RelicCategory.Ultimate, UnitRole.Helmsmaster, 1, 3, false,
                RelicEffectType.Ultimate_ReflectMoraleDamage,
                "Next enemy turn: morale damage to allies is reflected to enemies", 0, 0, 1, 1, RelicRarity.Unique);
                
            AddEffect(RelicCategory.Ultimate, UnitRole.Boatswain, 1, 3, false,
                RelicEffectType.Ultimate_ReviveAlly,
                "Revive dead or surrendered ally at 30% health and morale", 0, 0.30f, 0, 1, RelicRarity.Unique);
                
            AddEffect(RelicCategory.Ultimate, UnitRole.Shipwright, 1, 3, false,
                RelicEffectType.Ultimate_FullBuzzAttack,
                "Attack target with default weapon, make their buzz full for 2 turns", 0, 0, 2, 1, RelicRarity.Unique);
                
            AddEffect(RelicCategory.Ultimate, UnitRole.MasterGunner, 1, 3, false,
                RelicEffectType.Ultimate_RumBottleAoE,
                "Throw rum bottle: 200 damage in 1 tile, rum spill increases buzz 3 turns", 200, 0, 3, 1, RelicRarity.Unique);
                
            AddEffect(RelicCategory.Ultimate, UnitRole.MasterAtArms, 1, 3, false,
                RelicEffectType.Ultimate_SummonHardObstacles,
                "Summon 3 hard obstacles in front row for 2 turns", 3, 0, 2, 1, RelicRarity.Unique);
                
            AddEffect(RelicCategory.Ultimate, UnitRole.Navigator, 1, 3, false,
                RelicEffectType.Ultimate_IgnoreHighestHP,
                "This turn, highest HP enemy (except captain) is ignored", 0, 0, 1, 1, RelicRarity.Unique);
                
            AddEffect(RelicCategory.Ultimate, UnitRole.Surgeon, 1, 3, false,
                RelicEffectType.Ultimate_KnockbackToLastColumn,
                "Attack with default weapon, knockback target to last column", 0, 0, 0, 1, RelicRarity.Unique);
                
            AddEffect(RelicCategory.Ultimate, UnitRole.Cook, 1, 3, false,
                RelicEffectType.Ultimate_AttackKnockbackNearby,
                "Attack with default weapon, knockback nearby enemies 1 tile", 0, 0, 0, 1, RelicRarity.Unique);
                
            AddEffect(RelicCategory.Ultimate, UnitRole.Swashbuckler, 1, 3, false,
                RelicEffectType.Ultimate_StunAoE,
                "Attack with default weapon, stun target and nearby enemies 1 turn", 0, 0, 1, 1, RelicRarity.Unique);
                
            AddEffect(RelicCategory.Ultimate, UnitRole.Deckhand, 1, 3, false,
                RelicEffectType.Ultimate_MassiveSingleTarget,
                "Attack with default weapon, +300% damage if no nearby enemies", 0, 3.0f, 0, 1, RelicRarity.Unique);

            // ==================== PASSIVE UNIQUE (Passive, no copies) ====================
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Captain, 0, 0, true,
                RelicEffectType.PassiveUnique_ExtraEnergy,
                "Passive: Start each turn with +1 maximum energy", 1, 0, 0, 1, RelicRarity.Unique);
                
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Quartermaster, 0, 0, true,
                RelicEffectType.PassiveUnique_ExtraCards,
                "Passive: Gain 2 extra cards each turn", 2, 0, 0, 1, RelicRarity.Unique);
                
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Helmsmaster, 0, 0, true,
                RelicEffectType.PassiveUnique_DeathStrikeByMorale,
                "Passive: Higher morale = higher chance to attack on death", 0, 0, 0, 1, RelicRarity.Unique);
                
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Boatswain, 0, 0, true,
                RelicEffectType.PassiveUnique_LowerSurrenderThreshold,
                "Passive: Allies surrender at 10% morale instead of 20%", 0, 0.10f, 0, 1, RelicRarity.Unique);
                
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Shipwright, 0, 0, true,
                RelicEffectType.PassiveUnique_NoBuzzDownside,
                "Passive: No penalty when buzz meter is full", 0, 0, 0, 1, RelicRarity.Unique);
                
            AddEffect(RelicCategory.PassiveUnique, UnitRole.MasterGunner, 0, 0, true,
                RelicEffectType.PassiveUnique_DrawPerGrog,
                "Passive: Draw extra cards based on available grog", 0, 0, 0, 1, RelicRarity.Unique);
                
            AddEffect(RelicCategory.PassiveUnique, UnitRole.MasterAtArms, 0, 0, true,
                RelicEffectType.PassiveUnique_DrawOnLowDamage,
                "Passive: Draw card next turn if damage taken < 20% HP", 0, 0.20f, 0, 1, RelicRarity.Unique);
                
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Navigator, 0, 0, true,
                RelicEffectType.PassiveUnique_CounterAttack,
                "Passive: When ally takes damage, attack that enemy with default weapon", 0, 0, 0, 1, RelicRarity.Unique);
                
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Surgeon, 0, 0, true,
                RelicEffectType.PassiveUnique_GritAura,
                "Passive: Nearby allies gain +5% of this unit's total grit", 0, 0.05f, 0, 1, RelicRarity.Unique);
                
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Cook, 0, 0, true,
                RelicEffectType.PassiveUnique_BonusVsLowGrit,
                "Passive: +20% weapon damage vs targets with lower grit", 0, 0.20f, 0, 1, RelicRarity.Unique);
                
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Swashbuckler, 0, 0, true,
                RelicEffectType.PassiveUnique_IgnoreRoles,
                "Passive: Shipwright and Boatswain unit roles are ignored", 0, 0, 0, 1, RelicRarity.Unique);
                
            AddEffect(RelicCategory.PassiveUnique, UnitRole.Deckhand, 0, 0, true,
                RelicEffectType.PassiveUnique_BonusVsLowHP,
                "Passive: Bonus damage vs targets below 50% HP", 0, 0.50f, 0, 1, RelicRarity.Unique);

            Debug.Log($"RelicEffectsDatabase populated with {allEffects.Count} effects");
        }

        /// <summary>
        /// Add effect with default Common rarity.
        /// </summary>
        private void AddEffect(RelicCategory category, UnitRole role, int copies, int cost, bool isPassive,
            RelicEffectType effectType, string description, float val1, float val2, int duration, int tileRange = 1)
        {
            AddEffect(category, role, copies, cost, isPassive, effectType, description, val1, val2, duration, tileRange, RelicRarity.Common);
        }

        /// <summary>
        /// Add effect with specified rarity.
        /// </summary>
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