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
                        Debug.Log("<color=orange>RelicEffectsDatabase: Not found in Resources, creating runtime database...</color>");
                        _instance = CreateDefaultDatabase();
                    }
                    else
                    {
                        Debug.Log($"<color=cyan>RelicEffectsDatabase: Loaded from Resources with {_instance.allEffects?.Count ?? 0} effects</color>");
                    }
                }

                // Auto-populate if the database exists but is empty
                if (_instance != null && (_instance.allEffects == null || _instance.allEffects.Count == 0))
                {
                    Debug.Log("<color=orange>RelicEffectsDatabase: Empty, populating with default effects...</color>");
                    _instance.PopulateAllEffects();
                    Debug.Log($"<color=cyan>RelicEffectsDatabase: Now has {_instance.allEffects.Count} effects</color>");
                }

                return _instance;
            }
        }

        /// <summary>
        /// Get effect for a specific category and role (V1 by default).
        /// </summary>
        public RelicEffectData GetEffect(RelicCategory category, UnitRole roleTag, bool variant2 = false)
        {
            var result = allEffects.FirstOrDefault(e =>
                e.category == category &&
                e.roleTag == roleTag &&
                e.isVariant2 == variant2);

            if (result == null)
            {
                Debug.LogWarning($"<color=red>RelicEffectsDatabase: No effect found for {category}+{roleTag} (v2={variant2}). DB has {allEffects.Count} effects.</color>");
            }
            else
            {
                Debug.Log($"<color=green>RelicEffectsDatabase: Found effect for {category}+{roleTag}: {result.description}</color>");
            }

            return result;
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
                RelicEffectType.Boots_MoveRestoreMorale,
                "Move 2 tiles and restore 10% morale", 2, 0.10f, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Helmsmaster, false, 2, 1, false,
                RelicEffectType.Boots_MoveClearBuzz,
                "Move 2 tiles and clear the buzz meter", 2, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Boatswain, false, 2, 1, false,
                RelicEffectType.Boots_MoveReduceDamage,
                "Move 2 tiles, 20% reduced damage next enemy turn", 2, 0.20f, 1);
            AddEffect(RelicCategory.Boots, UnitRole.Shipwright, false, 2, 1, false,
                RelicEffectType.Boots_MoveToNeutral,
                "Move to any tile in neutral zone", 0, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.MasterGunner, false, 2, 1, false,
                RelicEffectType.Boots_MoveGainAim,
                "Move 2 tiles, gain 50% increased Aim stat this turn", 2, 0.50f, 1);
            AddEffect(RelicCategory.Boots, UnitRole.MasterAtArms, false, 2, 1, false,
                RelicEffectType.Boots_MoveBonusWeaponDamage,
                "Move 2 tiles, 30% increased damage to next weapon attack", 2, 0.30f, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Navigator, false, 2, 1, false,
                RelicEffectType.Boots_MoveFarDistance,
                "Move 4 tiles in any direction", 4, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Surgeon, false, 2, 1, false,
                RelicEffectType.Boots_MoveRestoreHealth,
                "Move 2 tiles, restore 20% health", 2, 0.20f, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Cook, false, 2, 1, false,
                RelicEffectType.Boots_MoveDrawCard,
                "Move 1 tile, draw a card, if cook relic reduce cost by 1", 1, 1, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Swashbuckler, false, 2, 1, false,
                RelicEffectType.Boots_MoveBySpeed,
                "Move 2 tiles, if highest speed move 4 tiles", 2, 4, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Deckhand, false, 2, 1, false,
                RelicEffectType.Boots_MoveColumnOnly,
                "Move any tile in same column, 1 tile on row", 0, 1, 0);

            // ==================== BOOTS V2 ====================
            AddEffect(RelicCategory.Boots, UnitRole.Captain, true, 2, 1, false,
                RelicEffectType.Boots_MoveAlly,
                "Move any allied unit 2 tiles", 2, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Quartermaster, true, 2, 1, false,
                RelicEffectType.Boots_AllyFreeMoveLowestMorale,
                "Lowest morale ally can move for free this turn 1 time", 0, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Helmsmaster, true, 2, 1, false,
                RelicEffectType.Boots_FreeIfGrog,
                "Move 2 tiles, costs 0 energy if Grog tokens are available", 2, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Boatswain, true, 2, 1, false,
                RelicEffectType.Boots_MoveAnyIfHighestHP,
                "If highest current HP move any distance, otherwise move 2 tiles", 2, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Shipwright, true, 2, 1, false,
                RelicEffectType.Boots_MoveGainGrit,
                "Move 2 tiles, gain 20% Grit stat for 2 turns", 2, 0.20f, 2);
            AddEffect(RelicCategory.Boots, UnitRole.MasterGunner, true, 2, 1, false,
                RelicEffectType.Boots_MoveReduceRangedCost,
                "Move 1 tile, reduce cost of next ranged weapon relic by 1 this turn", 1, 1, 0);
            AddEffect(RelicCategory.Boots, UnitRole.MasterAtArms, true, 2, 1, false,
                RelicEffectType.Boots_V2_MoveDestroyObstacle,
                "In 2 tile radius can move to an obstacle tile, destroying it", 2, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Navigator, true, 2, 0, false,
                RelicEffectType.Boots_V2_MoveFree,
                "Move 2 tiles in any direction", 2, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Surgeon, true, 2, 1, false,
                RelicEffectType.Boots_V2_SwapLowestHealthAlly,
                "Swap with lowest health ally (0 energy)", 0, 0, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Cook, true, 2, 1, false,
                RelicEffectType.Boots_V2_MoveBoostProficiency,
                "Move 2 tiles, +100% proficiency this turn", 2, 1.0f, 1);
            AddEffect(RelicCategory.Boots, UnitRole.Swashbuckler, true, 2, 1, false,
                RelicEffectType.Boots_V2_MoveRowOnly,
                "Move any tile in same row, 1 tile on column", 0, 1, 0);
            AddEffect(RelicCategory.Boots, UnitRole.Deckhand, true, 2, 1, false,
                RelicEffectType.Boots_V2_MoveRestoreHull,
                "Move 2 tiles, restore 50 hull shield", 2, 50, 0);

            // ==================== GLOVES V1 ====================
            AddEffect(RelicCategory.Gloves, UnitRole.Captain, false, 2, 1, false,
                RelicEffectType.Gloves_AttackReduceEnemyDraw,
                "Attack, enemy draws 1 less next turn", 0, 1, 1);
            AddEffect(RelicCategory.Gloves, UnitRole.Quartermaster, false, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusByMissingMorale,
                "Attack with weapon, increased damage based on enemy missing morale", 0, 0, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Helmsmaster, false, 2, 1, false,
                RelicEffectType.Gloves_AttackPreventBuzzReduce,
                "Attack with weapon, prevent target from reducing buzz for 2 turns", 0, 0, 2);
            AddEffect(RelicCategory.Gloves, UnitRole.Boatswain, false, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusIfMoreHP,
                "Attack with weapon, +20% damage if target has less current HP than this unit", 0, 0.20f, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Shipwright, false, 2, 1, false,
                RelicEffectType.Gloves_AttackPushForward,
                "Attack with weapon, push target forward 1 tile", 0, 1, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.MasterGunner, false, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusPerCardPlayed,
                "Attack with weapon, +10% damage for each card played this round", 0, 0.10f, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.MasterAtArms, false, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusPerNearbyAlly,
                "Attack with weapon, +20% bonus damage for each nearby allied unit in 1 tile radius", 0, 0.20f, 0, 1);
            AddEffect(RelicCategory.Gloves, UnitRole.Navigator, false, 2, 1, false,
                RelicEffectType.Gloves_DisableWeaponEffect,
                "Disable enemy weapon role effects next turn, damage still applies", 0, 0, 1);
            AddEffect(RelicCategory.Gloves, UnitRole.Surgeon, false, 2, 1, false,
                RelicEffectType.Gloves_AttackHealLowestAlly,
                "Attack, restore 200 HP to lowest ally", 0, 200, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Cook, false, 2, 1, false,
                RelicEffectType.Gloves_AttackDetonateBuff,
                "Attack applies debuff, detonates on next hit for 200 AoE per turn remained", 200, 0, 0, 1);
            AddEffect(RelicCategory.Gloves, UnitRole.Swashbuckler, false, 2, 1, false,
                RelicEffectType.Gloves_AttackTwice,
                "Attack with default weapon 2 times", 2, 0, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Deckhand, false, 2, 1, false,
                RelicEffectType.Gloves_AttackDrawOnHullDestroyed,
                "Attack with default weapon, if hull destroyed draw 1 card", 0, 1, 0);

            // ==================== GLOVES V2 ====================
            AddEffect(RelicCategory.Gloves, UnitRole.Captain, true, 2, 1, false,
                RelicEffectType.Gloves_AttackIncreaseEnemyCost,
                "Attack with weapon, enemy next card costs +1 energy", 0, 1, 1);
            AddEffect(RelicCategory.Gloves, UnitRole.Quartermaster, true, 2, 1, false,
                RelicEffectType.Gloves_AttackMarkMoraleFocus,
                "Attack with weapon, mark target for morale focus fire for 2 turns", 0, 0, 2);
            AddEffect(RelicCategory.Gloves, UnitRole.Helmsmaster, true, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusPerGrog,
                "Attack with weapon, +20% damage per available Grog token", 0, 0.20f, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Boatswain, true, 2, 1, false,
                RelicEffectType.Gloves_AttackLowerEnemyHealth,
                "Attack with weapon, lower enemy health stat by 30% for 2 turns", 0, 0.30f, 2);
            AddEffect(RelicCategory.Gloves, UnitRole.Shipwright, true, 2, 1, false,
                RelicEffectType.Gloves_AttackForceTargetClosest,
                "Attack with weapon, debuff forces target to only attack closest target next turn", 0, 0, 1);
            AddEffect(RelicCategory.Gloves, UnitRole.MasterGunner, true, 2, 1, false,
                RelicEffectType.Gloves_AttackBonusPerGunnerRelic,
                "Attack with weapon, +10% damage for each Master Gunner relic used this game", 0, 0.10f, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.MasterAtArms, true, 2, 1, false,
                RelicEffectType.Gloves_V2_AttackBonusPerRelicInHand,
                "Attack with weapon, +10% damage for each Master-at-Arms relic card in hand", 0, 0.10f, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Navigator, true, 2, 1, false,
                RelicEffectType.Gloves_V2_AttackBonusPerBootsCard,
                "Attack with weapon, +30% bonus damage for each boots relic card in deck", 0, 0.30f, 0);
            AddPassive(RelicCategory.Gloves, UnitRole.Surgeon, true,
                RelicEffectType.Gloves_V2_AttackHealedEnemy,
                "Passive: Attack any enemy that gets healed", 0, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.Cook, true, 2, 1, false,
                RelicEffectType.Gloves_V2_StasisClosest,
                "Stasis closest target 1 turn, can't attack or be attacked", 0, 0, 1);
            AddEffect(RelicCategory.Gloves, UnitRole.Swashbuckler, true, 2, 1, false,
                RelicEffectType.Gloves_V2_AttackStunOnMove,
                "Attack, if target moves in 2 turns stun for 1 turn", 0, 0, 2);
            AddEffect(RelicCategory.Gloves, UnitRole.Deckhand, true, 2, 1, false,
                RelicEffectType.Gloves_V2_AttackEnergyOnHullDestroyed,
                "Attack with default weapon, if hull destroyed get 1 energy", 0, 1, 0);

            // ==================== HAT V1 ====================
            AddEffect(RelicCategory.Hat, UnitRole.Captain, false, 2, 1, false,
                RelicEffectType.Hat_DrawCardsVulnerable,
                "Draw 2, take 200% damage for 2 turns", 2, 2.0f, 2);
            AddEffect(RelicCategory.Hat, UnitRole.Quartermaster, false, 2, 1, false,
                RelicEffectType.Hat_RestoreMoraleLowest,
                "Restore 30% morale to lowest morale ally", 0, 0.30f, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Helmsmaster, false, 2, 1, false,
                RelicEffectType.Hat_FreeRumUsage,
                "This round 3 rum usage cost no Grog", 3, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Boatswain, false, 2, 1, false,
                RelicEffectType.Hat_ReturnDamage,
                "For 2 turns, return 1 instance of damage back", 1, 0, 2);
            AddEffect(RelicCategory.Hat, UnitRole.Shipwright, false, 2, 1, false,
                RelicEffectType.Hat_EnergyOnKnockback,
                "Get 2 extra energy next turn if this unit is knocked back", 2, 0, 1);
            AddEffect(RelicCategory.Hat, UnitRole.MasterGunner, false, 2, 1, false,
                RelicEffectType.Hat_WeaponUseTwice,
                "Next weapon relic can be used twice", 0, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.MasterAtArms, false, 2, 1, false,
                RelicEffectType.Hat_ReduceUltimateCost,
                "Reduce the cost of your next ultimate ability by 2", 2, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Navigator, false, 2, 1, false,
                RelicEffectType.Hat_DisableEnemyUltimates,
                "Enemies can't use ultimate abilities next turn", 0, 0, 1);
            AddEffect(RelicCategory.Hat, UnitRole.Surgeon, false, 2, 1, false,
                RelicEffectType.Hat_DrawTrinketReduceCost,
                "Draw trinket card, reduce cost by 1", 1, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Cook, false, 2, 1, false,
                RelicEffectType.Hat_ReduceLowestAllyCardCost,
                "Reduce cost of lowest health ally's relic cards by 1 this turn", 1, 0, 1);
            AddEffect(RelicCategory.Hat, UnitRole.Swashbuckler, false, 2, 1, false,
                RelicEffectType.Hat_DrawWeaponReduceCost,
                "Draw a card, if weapon reduce cost by 1", 1, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Deckhand, false, 2, 1, false,
                RelicEffectType.Hat_NearbyHullIncrease,
                "Nearby allies in 1 tile radius have hull shield increased by 30%", 0.30f, 0, 0, 1);

            // ==================== HAT V2 ====================
            AddEffect(RelicCategory.Hat, UnitRole.Captain, true, 2, 1, false,
                RelicEffectType.Hat_DrawUltimate,
                "Draw an ultimate ability", 1, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Quartermaster, true, 2, 1, false,
                RelicEffectType.Hat_RestoreMoraleNearby,
                "All nearby allies in 1 tile range have 10% morale restored", 0, 0.10f, 0, 1);
            AddEffect(RelicCategory.Hat, UnitRole.Helmsmaster, true, 2, 1, false,
                RelicEffectType.Hat_GenerateGrog,
                "Generate 2 Grog tokens", 2, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Boatswain, true, 2, 1, false,
                RelicEffectType.Hat_IncreaseHealthStat,
                "For 2 turns, health stat of this unit is increased by 25%", 0, 0.25f, 2);
            AddEffect(RelicCategory.Hat, UnitRole.Shipwright, true, 2, 1, false,
                RelicEffectType.Hat_SwapEnemyByGrit,
                "Swap position of enemy with highest grit stat with enemy with lowest grit stat", 0, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.MasterGunner, true, 2, 1, false,
                RelicEffectType.Hat_DrawWeaponRelic,
                "Draw a weapon relic", 1, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.MasterAtArms, true, 2, 1, false,
                RelicEffectType.Hat_V2_IncreaseEnemyWeaponCost,
                "Increase the cost of enemy next weapon relic by 1", 1, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Navigator, true, 2, 1, false,
                RelicEffectType.Hat_V2_DrawBootsCard,
                "Get a boots card relic in hand", 1, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Surgeon, true, 2, 1, false,
                RelicEffectType.Hat_V2_HealOnCaptainDamage,
                "Allies that damage captain healed 10%", 0, 0.10f, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Cook, true, 2, 1, false,
                RelicEffectType.Hat_V2_MoveForwardHeal,
                "Move unit forward 1 tile, heal 10%", 1, 0.10f, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Swashbuckler, true, 2, 1, false,
                RelicEffectType.Hat_V2_StealEnemyCard,
                "Steal random enemy card, if weapon reduce cost by 1", 1, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.Deckhand, true, 2, 1, false,
                RelicEffectType.Hat_V2_DestroyObstaclesGainHull,
                "Destroy all soft obstacles, +20% hull per obstacle destroyed", 0, 0.20f, 0);

            // ==================== COAT V1 ====================
            AddEffect(RelicCategory.Coat, UnitRole.Captain, false, 2, 1, false,
                RelicEffectType.Coat_BuffNearbyAimPower,
                "Allies in 1 tile radius receive +20% Aim and Power for 2 turns", 0, 0.20f, 2, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Quartermaster, false, 2, 1, false,
                RelicEffectType.Coat_ReduceMoraleDamage,
                "For 2 turns allies take 30% less morale damage", 0, 0.30f, 2);
            AddEffect(RelicCategory.Coat, UnitRole.Helmsmaster, false, 2, 1, false,
                RelicEffectType.Coat_ReduceRumEffect,
                "Nearby allies in 1 tile radius have reduced rum effect this turn", 0, 0, 1, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Boatswain, false, 2, 1, false,
                RelicEffectType.Coat_PreventDisplacement,
                "Allies in 1 tile radius can't be displaced or knocked back next enemy turn", 0, 0, 1, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Shipwright, false, 2, 1, false,
                RelicEffectType.Coat_RowCantBeTargeted,
                "For 2 turns allies in same row behind this unit can't be targeted", 0, 0, 2);
            AddEffect(RelicCategory.Coat, UnitRole.MasterGunner, false, 2, 1, false,
                RelicEffectType.Coat_FreeStow,
                "Next 2 stows have no cost", 2, 0, 0);
            AddEffect(RelicCategory.Coat, UnitRole.MasterAtArms, false, 2, 1, false,
                RelicEffectType.Coat_BonusDamageNearbyAllies,
                "Give 20% extra damage to all nearby allies in 1 tile radius", 0, 0.20f, 0, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Navigator, false, 2, 1, false,
                RelicEffectType.Coat_HealthDamageImmunity,
                "Take 0 health damage from next attack for 2 turns, morale damage still counts", 0, 0, 2);
            AddEffect(RelicCategory.Coat, UnitRole.Surgeon, false, 2, 1, false,
                RelicEffectType.Coat_DoubleAllyStats,
                "+100% primary+secondary stat 1 turn", 1.0f, 0, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Cook, false, 2, 1, false,
                RelicEffectType.Coat_StunOnAllyAttacked,
                "Buff closest ally 1 turn, if attacked stun enemy 1 turn", 0, 0, 1);
            AddPassive(RelicCategory.Coat, UnitRole.Swashbuckler, false,
                RelicEffectType.Coat_NearbyAllyDamageReduction,
                "Passive: Nearby allies in 1 tile take 15% less damage if attacker has lower speed", 0.15f, 0);
            AddEffect(RelicCategory.Coat, UnitRole.Deckhand, false, 2, 1, false,
                RelicEffectType.Coat_HullBonusDamage,
                "Bonus weapon damage = 50% of hull shield for self and nearby allies 1 tile", 0, 0.50f, 1, 1);

            // ==================== COAT V2 ====================
            AddEffect(RelicCategory.Coat, UnitRole.Captain, true, 2, 1, false,
                RelicEffectType.Coat_DrawOnEnemyAttack,
                "For each enemy attack (3 max) next 2 turns, draw a card and enemy discards 1 next turn", 3, 1, 2);
            AddEffect(RelicCategory.Coat, UnitRole.Quartermaster, true, 2, 1, false,
                RelicEffectType.Coat_PreventSurrender,
                "For 2 turns buff an ally, if that unit would surrender restore 20% morale instead", 0, 0.20f, 2);
            AddEffect(RelicCategory.Coat, UnitRole.Helmsmaster, true, 2, 1, false,
                RelicEffectType.Coat_EnemyBuzzOnDamage,
                "Next turn enemies buzz meter fills every time they do damage", 0, 0, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Boatswain, true, 2, 1, false,
                RelicEffectType.Coat_ProtectLowHP,
                "Lowest HP ally can only be targeted next turn by enemies with lower HP", 0, 0, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Shipwright, true, 2, 1, false,
                RelicEffectType.Coat_ColumnDamageBoost,
                "Give 40% increased damage to all allied units in the same column", 0, 0.40f, 0);
            AddEffect(RelicCategory.Coat, UnitRole.MasterGunner, true, 2, 1, false,
                RelicEffectType.Coat_RowRangedProtection,
                "Allies in same row take 50% less damage from ranged attacks next turn", 0, 0.50f, 1);
            AddEffect(RelicCategory.Coat, UnitRole.MasterAtArms, true, 2, 1, false,
                RelicEffectType.Coat_V2_ReduceEnemyPower,
                "All enemies next turn have 35% less Power stat", 0, 0.35f, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Navigator, true, 2, 1, false,
                RelicEffectType.Coat_V2_DodgeFirstAttack,
                "Next turn first ally that gets attacked dodges by moving 1 tile back", 0, 0, 1);
            AddPassive(RelicCategory.Coat, UnitRole.Surgeon, true,
                RelicEffectType.Coat_V2_KnockbackOnAllyDeath,
                "Passive: Knockback enemy on ally death in 1 tile", 1, 0);
            AddEffect(RelicCategory.Coat, UnitRole.Cook, true, 2, 1, false,
                RelicEffectType.Coat_V2_ClearDebuffsNearby,
                "Clear all debuffs from nearby allies 1 tile radius", 0, 0, 0, 1);
            AddEffect(RelicCategory.Coat, UnitRole.Swashbuckler, true, 2, 1, false,
                RelicEffectType.Coat_V2_CurseEmptyTile,
                "Curse random empty tile on enemy side, enemy can't leave and takes 10% more damage", 0, 0.10f, 0);
            AddEffect(RelicCategory.Coat, UnitRole.Deckhand, true, 2, 1, false,
                RelicEffectType.Coat_V2_BuffTileDamageExchange,
                "Buff random tile, units take 15% damage and do 15% more damage", 0.15f, 0.15f, 0);

            // ==================== TRINKET V1 (Passive) ====================
            AddPassive(RelicCategory.Trinket, UnitRole.Captain, false,
                RelicEffectType.Trinket_BonusDamagePerCard,
                "Passive: +20% weapon damage per card in hand", 0.20f, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Quartermaster, false,
                RelicEffectType.Trinket_ImmuneMoraleFocusFire,
                "Passive: Unit is immune to morale focus fire effect", 0, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Helmsmaster, false,
                RelicEffectType.Trinket_DamageByBuzz,
                "Passive: Increased damage based on own buzz state", 0, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Boatswain, false,
                RelicEffectType.Trinket_ReduceDamageFromClosest,
                "Passive: Closest enemy does 20% less damage to this unit", 0.20f, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Shipwright, false,
                RelicEffectType.Trinket_TauntFirstAttack,
                "Passive: 1 time per enemy turn, taunt the first attack", 0, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.MasterGunner, false,
                RelicEffectType.Trinket_RowEnemiesLessDamage,
                "Passive: Enemy units in the same row do 10% less damage", 0.10f, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.MasterAtArms, false,
                RelicEffectType.Trinket_CounterAttackOnHit,
                "Passive: If attacked, hits back with default weapon", 0, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Navigator, false,
                RelicEffectType.Trinket_NearbyTacticsBoost,
                "Passive: Nearby allies in 1 tile radius have tactics stat increased by 30%", 0.30f, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Surgeon, false,
                RelicEffectType.Trinket_BlockEnemyRowMovement,
                "Passive: Enemies can't move in same row", 0, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Cook, false,
                RelicEffectType.Trinket_HazardSizeIncrease,
                "Passive: All hazards on enemy side spawn with +1 tile size", 1, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Swashbuckler, false,
                RelicEffectType.Trinket_BonusDamageIfAlone,
                "Passive: +20% bonus damage if no nearby allies in 1 tile radius", 0.20f, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Deckhand, false,
                RelicEffectType.Trinket_HullFullRegen,
                "Passive: If hull is not fully destroyed it will fully regen", 0, 0);

            // ==================== TRINKET V2 (Passive) ====================
            AddPassive(RelicCategory.Trinket, UnitRole.Captain, true,
                RelicEffectType.Trinket_BonusVsCaptain,
                "Passive: +20% extra damage vs enemy Captain", 0.20f, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Quartermaster, true,
                RelicEffectType.Trinket_EnemySurrenderEarly,
                "Passive: Enemy units surrender at 30% morale", 0.30f, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Helmsmaster, true,
                RelicEffectType.Trinket_KnockbackIncreasesBuzz,
                "Passive: Knockback increases enemy buzz meter", 0, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Boatswain, true,
                RelicEffectType.Trinket_DrawIfHighHP,
                "Passive: Draw an extra card each turn if HP is above 60%", 0.60f, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Shipwright, true,
                RelicEffectType.Trinket_KnockbackAttacker,
                "Passive: 1 time per turn, knock back attacker 1 tile when attacked", 0, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.MasterGunner, true,
                RelicEffectType.Trinket_RowEnemiesTakeMore,
                "Passive: Enemies in the same row take 10% increased damage", 0.10f, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.MasterAtArms, true,
                RelicEffectType.Trinket_V2_NearbyPowerBoost,
                "Passive: Nearby allies in 1 tile radius have Power stat increased by 30%", 0.30f, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Navigator, true,
                RelicEffectType.Trinket_V2_IgnoreSoftObstacles,
                "Passive: Nearby allies in 1 tile ignore soft obstacles when attacking", 0, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Surgeon, true,
                RelicEffectType.Trinket_V2_GlobalRadius,
                "Passive: Nearby radius = whole board", 0, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Cook, true,
                RelicEffectType.Trinket_V2_DrawExtraBelow50,
                "Passive: If below 50% HP draw an extra card each turn", 0.50f, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Swashbuckler, true,
                RelicEffectType.Trinket_V2_EnemySpeedReduction,
                "Passive: All enemies lose 10% from their speed stat", 0.10f, 0);
            AddPassive(RelicCategory.Trinket, UnitRole.Deckhand, true,
                RelicEffectType.Trinket_V2_HullDiscardOnSurvive,
                "Passive: If hull survives enemy attack, discard an enemy card", 0, 0);

            // ==================== TOTEM V1 ====================
            AddEffect(RelicCategory.Totem, UnitRole.Captain, false, 2, 1, false,
                RelicEffectType.Totem_SummonCannon,
                "Summon cannon, 250 HP, attacks random enemy", 250, 0, 0);
            AddEffect(RelicCategory.Totem, UnitRole.Quartermaster, false, 2, 1, false,
                RelicEffectType.Totem_RallyNoMoraleDamage,
                "Rally: allies in 1 tile radius won't suffer morale damage next turn", 0, 0, 1, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Helmsmaster, false, 2, 1, false,
                RelicEffectType.Totem_SummonHighQualityRum,
                "Summon 2 high quality rum to inventory", 2, 0, 0);
            AddEffect(RelicCategory.Totem, UnitRole.Boatswain, false, 2, 1, false,
                RelicEffectType.Totem_StunOnKnockback,
                "If target is knocked back next enemy turn, stun that target for that turn", 0, 0, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Shipwright, false, 2, 1, false,
                RelicEffectType.Totem_SummonTargetDummy,
                "Summon a target dummy in the front row with 250 HP", 250, 0, 0);
            AddEffect(RelicCategory.Totem, UnitRole.MasterGunner, false, 2, 1, false,
                RelicEffectType.Totem_SummonExplodingBarrels,
                "Summon 3 barrels at random enemy tiles, explode in 1 tile radius after 2 turns dealing damage and stunning for 1 turn", 3, 0, 2);
            AddEffect(RelicCategory.Totem, UnitRole.MasterAtArms, false, 2, 1, false,
                RelicEffectType.Totem_DisableEnemyWeapons,
                "Disable enemy default weapons for next turn", 0, 0, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Navigator, false, 2, 1, false,
                RelicEffectType.Totem_DisableEnemyMovement,
                "Disable all enemy movement for 1 turn, they can still attack", 0, 0, 1);
            AddPassive(RelicCategory.Totem, UnitRole.Surgeon, false,
                RelicEffectType.Totem_StunHealedEnemy,
                "Passive: Stun enemy that gets healed", 0, 0);
            AddPassive(RelicCategory.Totem, UnitRole.Cook, false,
                RelicEffectType.Totem_HealLowestOnDamage,
                "Passive: Each time this unit takes damage heal the lowest health ally", 0, 0);
            AddEffect(RelicCategory.Totem, UnitRole.Swashbuckler, false, 2, 1, false,
                RelicEffectType.Totem_SummonInvisibleTraps,
                "Summon 2 invisible traps in random tiles, enemies entering are stunned 1 turn", 2, 0, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Deckhand, false, 2, 1, false,
                RelicEffectType.Totem_CreateSoftObstacles,
                "Create 2 soft obstacles in 2 random tiles", 2, 0, 0);

            // ==================== TOTEM V2 ====================
            AddEffect(RelicCategory.Totem, UnitRole.Captain, true, 2, 1, false,
                RelicEffectType.Totem_CurseCaptainReflect,
                "Curse enemy captain this turn, damage captain suffers reflects to all other enemies", 0, 0, 1);
            AddPassive(RelicCategory.Totem, UnitRole.Quartermaster, true,
                RelicEffectType.Totem_EnemyDeathMoraleSwing,
                "Passive: When enemy surrenders or dies, enemies lose morale and all player units gain 5% morale", 0, 0.05f);
            AddEffect(RelicCategory.Totem, UnitRole.Helmsmaster, true, 2, 0, false,
                RelicEffectType.Totem_ConvertGrogToEnergy,
                "Convert 2 Grog tokens into 1 energy", 2, 1, 0);
            AddEffect(RelicCategory.Totem, UnitRole.Boatswain, true, 2, 1, false,
                RelicEffectType.Totem_SummonAnchorHealthBuff,
                "Summon anchor on nearby tile, +25% health stat to allies in 1 tile radius for 2 turns", 0, 0.25f, 2, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Shipwright, true, 2, 1, false,
                RelicEffectType.Totem_SummonObstacleDisplace,
                "Summon soft obstacle at target location, displace target to nearby available tile", 0, 0, 0);
            AddEffect(RelicCategory.Totem, UnitRole.MasterGunner, true, 2, 1, false,
                RelicEffectType.Totem_CurseRangedWeapons,
                "Curse enemy ranged weapons next turn, they do 50% less damage", 0, 0.50f, 1);
            AddEffect(RelicCategory.Totem, UnitRole.MasterAtArms, true, 2, 1, false,
                RelicEffectType.Totem_V2_EarthquakeHazard,
                "3 random tiles get earthquake hazard, units on tile at end of round are moved to nearby empty tile", 3, 0, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Navigator, true, 2, 1, false,
                RelicEffectType.Totem_V2_DisableNonWeaponRelics,
                "Disable all enemy non-weapon relics for 1 turn", 0, 0, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Surgeon, true, 2, 1, false,
                RelicEffectType.Totem_V2_SummonHealingPotions,
                "3 random healing potions 200 HP", 3, 200, 0);
            AddEffect(RelicCategory.Totem, UnitRole.Cook, true, 2, 1, false,
                RelicEffectType.Totem_V2_SummonStatDebuffObstacle,
                "Summon soft obstacle, -50% primary+secondary to nearby enemies 1 tile radius", 0, 0.50f, 0, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Swashbuckler, true, 2, 1, false,
                RelicEffectType.Totem_V2_DisableEnemyPassives,
                "Next turn enemies can't use any passive effects", 0, 0, 1);
            AddEffect(RelicCategory.Totem, UnitRole.Deckhand, true, 2, 1, false,
                RelicEffectType.Totem_V2_PullNearbyToRow,
                "Pull all nearby enemies in 1 tile to same row as this unit", 0, 0, 0, 1);

            // ==================== ULTIMATE V1 ====================
            AddEffect(RelicCategory.Ultimate, UnitRole.Captain, false, 1, 3, false,
                RelicEffectType.Ultimate_ShipCannon,
                "Fire ship cannon: 3 shots, 200 damage + fire hazard", 200, 3, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Quartermaster, false, 1, 3, false,
                RelicEffectType.Ultimate_ReflectMoraleDamage,
                "During enemy next turn, any morale allies suffer is reflected on enemies too", 0, 0, 1, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Helmsmaster, false, 1, 3, false,
                RelicEffectType.Ultimate_FullBuzzAttack,
                "Attack target with weapon, target buzz meter full for 2 turns", 0, 0, 2, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Boatswain, false, 1, 3, false,
                RelicEffectType.Ultimate_SummonHardObstacles,
                "Summon 3 hard obstacles in front row empty tiles, last 2 turns", 3, 0, 2, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Shipwright, false, 1, 3, false,
                RelicEffectType.Ultimate_KnockbackToLastColumn,
                "Attack with weapon, knock target back to last column", 0, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.MasterGunner, false, 1, 3, false,
                RelicEffectType.Ultimate_StunAoE,
                "Attack with weapon, stun target and all nearby enemies in 1 tile radius for next turn", 0, 0, 1, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.MasterAtArms, false, 1, 3, false,
                RelicEffectType.Ultimate_AttackAllEnemies,
                "Attack with default weapon all enemies 1 time", 0, 0, 0, 99, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Navigator, false, 1, 3, false,
                RelicEffectType.Ultimate_MarkReflectToCaptain,
                "Mark target this round, any damage target suffers is reflected on the captain", 0, 0, 1, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Surgeon, false, 1, 3, false,
                RelicEffectType.Ultimate_PreventDeath,
                "Prevent unit from dying/surrendering", 0, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Cook, false, 1, 3, false,
                RelicEffectType.Ultimate_SwapHealthClosest,
                "Swap this unit's health with closest enemy", 0, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Swashbuckler, false, 1, 3, false,
                RelicEffectType.Ultimate_ForceLowestAndCaptainFight,
                "Lowest health unit and captain attack each other with default weapons", 0, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Deckhand, false, 1, 3, false,
                RelicEffectType.Ultimate_MassiveHullBuff,
                "Give a unit 300% hull for 2 turns", 0, 3.0f, 2, 1, RelicRarity.Unique);

            // ==================== ULTIMATE V2 ====================
            AddEffect(RelicCategory.Ultimate, UnitRole.Captain, true, 1, 3, false,
                RelicEffectType.Ultimate_MarkCaptainOnly,
                "Attack enemy captain with weapon, mark as only target this turn", 0, 0, 1, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Quartermaster, true, 1, 3, false,
                RelicEffectType.Ultimate_ReviveAlly,
                "Revive a dead or surrendered ally with 30% health and morale", 0, 0.30f, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Helmsmaster, true, 1, 3, false,
                RelicEffectType.Ultimate_RumBottleAoE,
                "Throw rum bottle at target, 200 damage in 1 tile radius, rum spill increases buzz for 3 turns", 200, 0, 3, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Boatswain, true, 1, 3, false,
                RelicEffectType.Ultimate_IgnoreHighestHP,
                "This turn, highest HP enemy besides captain is ignored", 0, 0, 1, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Shipwright, true, 1, 3, false,
                RelicEffectType.Ultimate_AttackKnockbackNearby,
                "Attack with weapon, nearby enemies in 1 tile radius are knocked back 1 tile", 0, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.MasterGunner, true, 1, 3, false,
                RelicEffectType.Ultimate_MassiveSingleTarget,
                "Attack with weapon, +300% bonus damage if no nearby enemies in 1 tile radius", 0, 3.0f, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.MasterAtArms, true, 1, 3, false,
                RelicEffectType.Ultimate_V2_AttackRowDamage,
                "Attack closest target with weapon, deal weapon damage + 350 damage to all units in same row", 350, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Navigator, true, 1, 3, false,
                RelicEffectType.Ultimate_V2_SwapClosestFurthest,
                "Swap the position of the closest enemy with the furthest enemy", 0, 0, 0, 1, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Surgeon, true, 1, 3, false,
                RelicEffectType.Ultimate_V2_FullHealthRestore,
                "Fully restore health of any unit", 0, 1.0f, 0, 99, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Cook, true, 1, 3, false,
                RelicEffectType.Ultimate_V2_FireColumn,
                "Set fire to closest target's whole column", 0, 0, 0, 1, RelicRarity.Unique);
            AddPassive(RelicCategory.Ultimate, UnitRole.Swashbuckler, true,
                RelicEffectType.Ultimate_V2_SurrenderOn4Weapons,
                "Passive: Attacking same target with 4 weapons in a single turn = instant surrender", 4, 0, RelicRarity.Unique);
            AddEffect(RelicCategory.Ultimate, UnitRole.Deckhand, true, 1, 3, false,
                RelicEffectType.Ultimate_V2_ClearHazardsPlayerSide,
                "Clear all hazards on player side and prevent new ones from appearing", 0, 0, 0, 99, RelicRarity.Unique);

            // ==================== PASSIVE UNIQUE V1 ====================
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Captain, false,
                RelicEffectType.PassiveUnique_ExtraEnergy,
                "Passive: +1 max energy each turn", 1, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Quartermaster, false,
                RelicEffectType.PassiveUnique_DeathStrikeByMorale,
                "Passive: Higher morale on death = higher chance to do a default weapon attack before dying", 0, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Helmsmaster, false,
                RelicEffectType.PassiveUnique_NoBuzzDownside,
                "Passive: No downside if the buzz meter is full", 0, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Boatswain, false,
                RelicEffectType.PassiveUnique_DrawOnLowDamage,
                "Passive: Draw a card next turn if this unit takes less than 20% current HP damage in 1 turn", 0.20f, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Shipwright, false,
                RelicEffectType.PassiveUnique_GritAura,
                "Passive: Nearby allies in 1 tile radius have 5% increased grit from this unit's total grit", 0.05f, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.MasterGunner, false,
                RelicEffectType.PassiveUnique_IgnoreRoles,
                "Passive: Attacks skip enemy Shipwright and Boatswain units when selecting targets", 0, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.MasterAtArms, false,
                RelicEffectType.PassiveUnique_WeaponRelicOnKill,
                "Passive: Killing enemies or making them surrender gets a melee weapon relic in hand", 0, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Navigator, false,
                RelicEffectType.PassiveUnique_FreeMovement,
                "Passive: Can move 3 tiles without boots relic", 3, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Surgeon, false,
                RelicEffectType.PassiveUnique_HealingAura,
                "Passive: 5% heal to nearby allies at turn end", 0.05f, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Cook, false,
                RelicEffectType.PassiveUnique_DisplaceOnWeaponUse,
                "Passive: Every time a unit uses a weapon card relic, displace it to a random nearby empty tile", 0, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Swashbuckler, false,
                RelicEffectType.PassiveUnique_EnemyDiscardOnBoot,
                "Passive: Enemy discards a card every time they use a boots relic card", 0, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Deckhand, false,
                RelicEffectType.PassiveUnique_HullDestroyedRestoreHealth,
                "Passive: When hull is destroyed restore health", 0, 0, RelicRarity.Unique);

            // ==================== PASSIVE UNIQUE V2 ====================
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Captain, true,
                RelicEffectType.PassiveUnique_ExtraCards,
                "Passive: Gain 2 extra cards each turn", 2, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Quartermaster, true,
                RelicEffectType.PassiveUnique_LowerSurrenderThreshold,
                "Passive: Lower the surrender threshold of allies to 10%", 0.10f, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Helmsmaster, true,
                RelicEffectType.PassiveUnique_DrawPerGrog,
                "Passive: Each turn draw extra cards based on available Grog", 0, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Boatswain, true,
                RelicEffectType.PassiveUnique_CounterAttack,
                "Passive: Every time an ally takes damage, counter-attack that enemy with default weapon", 0, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Shipwright, true,
                RelicEffectType.PassiveUnique_BonusVsLowGrit,
                "Passive: +20% bonus weapon damage if target grit stat is lower", 0.20f, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.MasterGunner, true,
                RelicEffectType.PassiveUnique_BonusVsLowHP,
                "Passive: Bonus damage on all attacks if target is below 50% health", 0.50f, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.MasterAtArms, true,
                RelicEffectType.PassiveUnique_V2_HealOnKill,
                "Passive: Killing enemies or making them surrender restores 20% health", 0.20f, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Navigator, true,
                RelicEffectType.PassiveUnique_V2_AllyMovementBoost,
                "Passive: All allies can move 1 tile distance extra", 1, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Surgeon, true,
                RelicEffectType.PassiveUnique_V2_TeamHealOnKill,
                "Passive: Kill/surrender restores 5% to all allies", 0.05f, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Cook, true,
                RelicEffectType.PassiveUnique_V2_RelicsNotConsumed,
                "Passive: Relic cards not consumed when played, can replay if energy allows, discard at end of turn", 0, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Swashbuckler, true,
                RelicEffectType.PassiveUnique_V2_EnemyBootsLimit,
                "Passive: Enemies can only move 1 tile with their boots relics", 1, 0, RelicRarity.Unique);
            AddPassive(RelicCategory.PassiveUnique, UnitRole.Deckhand, true,
                RelicEffectType.PassiveUnique_V2_HullDestroyedDamageBonus,
                "Passive: +30% weapon damage per hull destroyed this game (allies and enemies)", 0.30f, 0, RelicRarity.Unique);

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