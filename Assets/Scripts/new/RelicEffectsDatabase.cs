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
            RelicEffectType.Boots_V1_Captain,
            "**Position Swap** — Swap location with another (allied) unit. *Target: ally.*", 0, 0, 0);
        AddEffect(RelicCategory.Boots, UnitRole.Captain, true, 2, 1, false,
            RelicEffectType.Boots_V2_Captain,
            "**Ally Step** — Move any allied unit 2 tiles. *Target: ally.*", 0, 0, 0);
        AddEffect(RelicCategory.Boots, UnitRole.Quartermaster, false, 2, 1, false,
            RelicEffectType.Boots_V1_Quartermaster,
            "**Morale March** — Move 2 tiles and restore Morale Tier morale (to self).", 0, 0, 0);
        AddEffect(RelicCategory.Boots, UnitRole.Quartermaster, true, 2, 1, false,
            RelicEffectType.Boots_V2_Quartermaster,
            "**Rally Step** — Make the lowest-morale ally move free this turn (1×/battle).", 0, 0, 0);
        AddEffect(RelicCategory.Boots, UnitRole.Helmsmaster, false, 2, 1, false,
            RelicEffectType.Boots_V1_Helmsmaster,
            "**Sober Stride** — Move 2 tiles and clear the Buzz meter.", 0, 0, 0);
        AddEffect(RelicCategory.Boots, UnitRole.Helmsmaster, true, 2, 1, false,
            RelicEffectType.Boots_V2_Helmsmaster,
            "**Grog Step** — Move 2 tiles. If you have any Grog Tokens, this costs 0 Energy.", 0, 0, 0);
        AddEffect(RelicCategory.Boots, UnitRole.Boatswain, false, 2, 1, false,
            RelicEffectType.Boots_V1_Boatswain,
            "**Bulwark Stride** — Move 2 tiles. Take −2 dmg during enemy next turn.", 0, 0, 0);
        AddEffect(RelicCategory.Boots, UnitRole.Boatswain, true, 2, 1, false,
            RelicEffectType.Boots_V2_Boatswain,
            "**Iron Stride** — If this unit has the highest current HP, can move any distance; otherwise move 2 tiles.", 0, 0, 0);
        AddEffect(RelicCategory.Boots, UnitRole.Shipwright, false, 2, 1, false,
            RelicEffectType.Boots_V1_Shipwright,
            "**Neutral Walk** — Can move to any tile inside the Neutral Zone.", 0, 0, 0);
        AddEffect(RelicCategory.Boots, UnitRole.Shipwright, true, 2, 1, false,
            RelicEffectType.Boots_V2_Shipwright,
            "**Forge Step** — Move 2 tiles and gain +2 Grit for 2 turns.", 0, 0, 0);
        AddEffect(RelicCategory.Boots, UnitRole.MasterGunner, false, 2, 1, false,
            RelicEffectType.Boots_V1_MasterGunner,
            "**Sniper Step** — Move 2 tiles and gain +5 Aim for that turn.", 0, 0, 0);
        AddEffect(RelicCategory.Boots, UnitRole.MasterGunner, true, 2, 1, false,
            RelicEffectType.Boots_V2_MasterGunner,
            "**Cold Reload** — Move 1 tile and reduce your next ranged-weapon-relic cost by 1 this turn.", 0, 0, 0);
        AddEffect(RelicCategory.Boots, UnitRole.Navigator, false, 2, 1, false,
            RelicEffectType.Boots_V1_Navigator,
            "**Quick Step** — Move 4 tiles in any direction.", 0, 0, 0);
        AddEffect(RelicCategory.Boots, UnitRole.Navigator, true, 2, 1, false,
            RelicEffectType.Boots_V2_Navigator,
            "**Free Step** — Move 2 tiles in any direction. **Cost: 0 Energy.**", 0, 0, 0);
        AddEffect(RelicCategory.Boots, UnitRole.Surgeon, false, 2, 1, false,
            RelicEffectType.Boots_V1_Surgeon,
            "**Healing Step** — Move 2 tiles and restore Health Tier HP (to self).", 0, 0, 0);
        AddEffect(RelicCategory.Boots, UnitRole.Surgeon, true, 2, 1, false,
            RelicEffectType.Boots_V2_Surgeon,
            "**Lifeline Swap** — Swap location with the lowest-health ally. *Target: lowest-HP ally.*", 0, 0, 0);
        AddEffect(RelicCategory.Boots, UnitRole.Cook, false, 2, 1, false,
            RelicEffectType.Boots_V1_Cook,
            "**Kitchen Sprint** — Move 1 tile and draw a card; if the drawn card is a Cook relic, reduce its cost by 1.", 0, 0, 0);
        AddEffect(RelicCategory.Boots, UnitRole.Cook, true, 2, 1, false,
            RelicEffectType.Boots_V2_Cook,
            "**Proof Stance** — Move 2 tiles and increase your Proficiency by 100% for this turn (effectively unlocks all empowered lines).", 0, 0, 0);
        AddEffect(RelicCategory.Boots, UnitRole.Swashbuckler, false, 2, 1, false,
            RelicEffectType.Boots_V1_Swashbuckler,
            "**Lightning Step** — Move 2 tiles; if this unit has the highest Speed, move 4 tiles instead.", 0, 0, 0);
        AddEffect(RelicCategory.Boots, UnitRole.Swashbuckler, true, 2, 1, false,
            RelicEffectType.Boots_V2_Swashbuckler,
            "**Skirmisher Row** — Move to any tile in the same row, but only 1 tile on a column.", 0, 0, 0);
        AddEffect(RelicCategory.Boots, UnitRole.Deckhand, false, 2, 1, false,
            RelicEffectType.Boots_V1_Deckhand,
            "**Column Brace** — Move to any tile in the same column, but only 1 tile on a row.", 0, 0, 0);
        AddEffect(RelicCategory.Boots, UnitRole.Deckhand, true, 2, 1, false,
            RelicEffectType.Boots_V2_Deckhand,
            "**Hull Step** — Move 2 tiles in any direction and restore 5 Hull.", 0, 0, 0);

//             AddEffect(RelicCategory.Boots, UnitRole.Captain, false, 2, 1, false,
//                 RelicEffectType.Boots_SwapWithUnit,
//                 "Swap location with another unit", 0, 0, 0);
//             AddEffect(RelicCategory.Boots, UnitRole.Quartermaster, false, 2, 1, false,
//                 RelicEffectType.Boots_MoveRestoreMorale,
//                 "Move 2 tiles and restore 10% morale", 2, 0.10f, 0);
//             AddEffect(RelicCategory.Boots, UnitRole.Helmsmaster, false, 2, 1, false,
//                 RelicEffectType.Boots_MoveClearBuzz,
//                 "Move 2 tiles and clear the buzz meter", 2, 0, 0);
//             AddEffect(RelicCategory.Boots, UnitRole.Boatswain, false, 2, 1, false,
//                 RelicEffectType.Boots_MoveReduceDamage,
//                 "Move 2 tiles, 20% reduced damage next enemy turn", 2, 0.20f, 1);
//             AddEffect(RelicCategory.Boots, UnitRole.Shipwright, false, 2, 1, false,
//                 RelicEffectType.Boots_MoveToNeutral,
//                 "Move to any tile in neutral zone", 0, 0, 0);
//             AddEffect(RelicCategory.Boots, UnitRole.MasterGunner, false, 2, 1, false,
//                 RelicEffectType.Boots_MoveGainAim,
//                 "Move 2 tiles, gain 50% increased Aim stat this turn", 2, 0.50f, 1);
            AddEffect(RelicCategory.Boots, UnitRole.MasterAtArms, false, 2, 1, false,
                RelicEffectType.Boots_V1_MasterAtArms,
                "Striking Step - Move 2 tiles, 30% increased damage to next weapon attack", 2, 0.30f, 0);
//             AddEffect(RelicCategory.Boots, UnitRole.Navigator, false, 2, 1, false,
//                 RelicEffectType.Boots_MoveFarDistance,
//                 "Move 4 tiles in any direction", 4, 0, 0);
//             AddEffect(RelicCategory.Boots, UnitRole.Surgeon, false, 2, 1, false,
//                 RelicEffectType.Boots_MoveRestoreHealth,
//                 "Move 2 tiles, restore 20% health", 2, 0.20f, 0);
//             AddEffect(RelicCategory.Boots, UnitRole.Cook, false, 2, 1, false,
//                 RelicEffectType.Boots_MoveDrawCard,
//                 "Move 1 tile, draw a card, if cook relic reduce cost by 1", 1, 1, 0);
//             AddEffect(RelicCategory.Boots, UnitRole.Swashbuckler, false, 2, 1, false,
//                 RelicEffectType.Boots_MoveBySpeed,
//                 "Move 2 tiles, if highest speed move 4 tiles", 2, 4, 0);
//             AddEffect(RelicCategory.Boots, UnitRole.Deckhand, false, 2, 1, false,
//                 RelicEffectType.Boots_MoveColumnOnly,
//                 "Move any tile in same column, 1 tile on row", 0, 1, 0);

            // ==================== BOOTS V2 ====================
//             AddEffect(RelicCategory.Boots, UnitRole.Captain, true, 2, 1, false,
//                 RelicEffectType.Boots_MoveAlly,
//                 "Move any allied unit 2 tiles", 2, 0, 0);
//             AddEffect(RelicCategory.Boots, UnitRole.Quartermaster, true, 2, 1, false,
//                 RelicEffectType.Boots_AllyFreeMoveLowestMorale,
//                 "Lowest morale ally can move for free this turn 1 time", 0, 0, 0);
//             AddEffect(RelicCategory.Boots, UnitRole.Helmsmaster, true, 2, 1, false,
//                 RelicEffectType.Boots_FreeIfGrog,
//                 "Move 2 tiles, costs 0 energy if Grog tokens are available", 2, 0, 0);
//             AddEffect(RelicCategory.Boots, UnitRole.Boatswain, true, 2, 1, false,
//                 RelicEffectType.Boots_MoveAnyIfHighestHP,
//                 "If highest current HP move any distance, otherwise move 2 tiles", 2, 0, 0);
//             AddEffect(RelicCategory.Boots, UnitRole.Shipwright, true, 2, 1, false,
//                 RelicEffectType.Boots_MoveGainGrit,
//                 "Move 2 tiles, gain 20% Grit stat for 2 turns", 2, 0.20f, 2);
//             AddEffect(RelicCategory.Boots, UnitRole.MasterGunner, true, 2, 1, false,
//                 RelicEffectType.Boots_MoveReduceRangedCost,
//                 "Move 1 tile, reduce cost of next ranged weapon relic by 1 this turn", 1, 1, 0);
            AddEffect(RelicCategory.Boots, UnitRole.MasterAtArms, true, 2, 1, false,
                RelicEffectType.Boots_V2_MasterAtArms,
                "Bullrush - In 2 tile radius can move to an obstacle tile, destroying it", 2, 0, 0);
//             AddEffect(RelicCategory.Boots, UnitRole.Navigator, true, 2, 0, false,
//                 RelicEffectType.Boots_V2_MoveFree,
//                 "Move 2 tiles in any direction", 2, 0, 0);
//             AddEffect(RelicCategory.Boots, UnitRole.Surgeon, true, 2, 1, false,
//                 RelicEffectType.Boots_V2_SwapLowestHealthAlly,
//                 "Swap with lowest health ally (0 energy)", 0, 0, 0);
//             AddEffect(RelicCategory.Boots, UnitRole.Cook, true, 2, 1, false,
//                 RelicEffectType.Boots_V2_MoveBoostProficiency,
//                 "Move 2 tiles, +100% proficiency this turn", 2, 1.0f, 1);
//             AddEffect(RelicCategory.Boots, UnitRole.Swashbuckler, true, 2, 1, false,
//                 RelicEffectType.Boots_V2_MoveRowOnly,
//                 "Move any tile in same row, 1 tile on column", 0, 1, 0);
//             AddEffect(RelicCategory.Boots, UnitRole.Deckhand, true, 2, 1, false,
//                 RelicEffectType.Boots_V2_MoveRestoreHull,
//                 "Move 2 tiles, restore 50 hull shield", 2, 50, 0);

            // ==================== GLOVES V1 ====================
        // AddEffect(RelicCategory.Gloves, UnitRole.Captain, false, 2, 1, false,
        //          RelicEffectType.Gloves_V1_Captain,
        //          "**Captain's Strike** — Default attack. Enemy draws 1 fewer card next turn.", 0, 0, 0);
        // AddEffect(RelicCategory.Gloves, UnitRole.Captain, true, 2, 1, false,
        //          RelicEffectType.Gloves_V2_Captain,
        //          "**Pressing Order** — Default attack. Forces target's next card to cost +1 Energy.", 0, 0, 0);
        AddEffect(RelicCategory.Gloves, UnitRole.Captain, false, 2, 1, false,
                  RelicEffectType.Gloves_V1_Captain,
                  "**Captain's Strike** — Default attack. Enemy draws 1 fewer card next turn.", 0, 0, 0);
        AddEffect(RelicCategory.Gloves, UnitRole.Captain, true, 2, 1, false,
                  RelicEffectType.Gloves_V2_Captain,
                  "**Pressing Order** — Default attack. Forces target's next card to cost +1 Energy.", 0, 0, 0);
        // AddEffect(RelicCategory.Gloves, UnitRole.Quartermaster, false, 2, 1, false,
        //          RelicEffectType.Gloves_V1_Quartermaster,
        //          "**Press Advantage** — Default attack. Bonus dmg scales with target's missing morale (+1 per missing block, max +3).", 0, 0, 0);
        // AddEffect(RelicCategory.Gloves, UnitRole.Quartermaster, true, 2, 1, false,
        //          RelicEffectType.Gloves_V2_Quartermaster,
        //          "**Morale Mark** — Default attack. Apply Morale-Marked 2 (focus-fire bonus +1 morale dmg per hit, does not expire for 2 turns).", 0, 0, 0);
        AddEffect(RelicCategory.Gloves, UnitRole.Quartermaster, false, 2, 1, false,
                  RelicEffectType.Gloves_V1_Quartermaster,
                  "**Press Advantage** — Default attack. Bonus dmg scales with target's missing morale (+1 per missing block, max +3).", 0, 0, 0);
        AddEffect(RelicCategory.Gloves, UnitRole.Quartermaster, true, 2, 1, false,
                  RelicEffectType.Gloves_V2_Quartermaster,
                  "**Morale Mark** — Default attack. Apply Morale-Marked 2 (focus-fire bonus +1 morale dmg per hit, does not expire for 2 turns).", 0, 0, 0);
        // AddEffect(RelicCategory.Gloves, UnitRole.Helmsmaster, false, 2, 1, false,
        //          RelicEffectType.Gloves_V1_Helmsmaster,
        //          "**Buzz Lock** — Default attack. Applies a debuff: target cannot reduce its Buzz meter for 2 turns.", 0, 0, 0);
        // AddEffect(RelicCategory.Gloves, UnitRole.Helmsmaster, true, 2, 1, false,
        //          RelicEffectType.Gloves_V2_Helmsmaster,
        //          "**Drunk Hit** — Default attack. +1 dmg per Grog Token currently available (max +Buzz Tier).", 0, 0, 0);
        AddEffect(RelicCategory.Gloves, UnitRole.Helmsmaster, false, 2, 1, false,
                  RelicEffectType.Gloves_V1_Helmsmaster,
                  "**Buzz Lock** — Default attack. Applies a debuff: target cannot reduce its Buzz meter for 2 turns.", 0, 0, 0);
        AddEffect(RelicCategory.Gloves, UnitRole.Helmsmaster, true, 2, 1, false,
                  RelicEffectType.Gloves_V2_Helmsmaster,
                  "**Drunk Hit** — Default attack. +1 dmg per Grog Token currently available (max +Buzz Tier).", 0, 0, 0);
        // AddEffect(RelicCategory.Gloves, UnitRole.Boatswain, false, 2, 1, false,
        //          RelicEffectType.Gloves_V1_Boatswain,
        //          "**Heavy Hit** — Default attack. +2 dmg if target has less current HP than this unit.", 0, 0, 0);
        // AddEffect(RelicCategory.Gloves, UnitRole.Boatswain, true, 2, 1, false,
        //          RelicEffectType.Gloves_V2_Boatswain,
        //          "**Health Drop** — Default attack. Lower target's Health stat by Health Tier for 2 turns.", 0, 0, 0);
        AddEffect(RelicCategory.Gloves, UnitRole.Boatswain, false, 2, 1, false,
                  RelicEffectType.Gloves_V1_Boatswain,
                  "**Heavy Hit** — Default attack. +2 dmg if target has less current HP than this unit.", 0, 0, 0);
        AddEffect(RelicCategory.Gloves, UnitRole.Boatswain, true, 2, 1, false,
                  RelicEffectType.Gloves_V2_Boatswain,
                  "**Health Drop** — Default attack. Lower target's Health stat by Health Tier for 2 turns.", 0, 0, 0);
        // AddEffect(RelicCategory.Gloves, UnitRole.Shipwright, false, 2, 1, false,
        //          RelicEffectType.Gloves_V1_Shipwright,
        //          "**Push Hit** — Default attack. Target is forced forward 1 tile.", 0, 0, 0);
        // AddEffect(RelicCategory.Gloves, UnitRole.Shipwright, true, 2, 1, false,
        //          RelicEffectType.Gloves_V2_Shipwright,
        //          "**Taunt Hit** — Default attack and apply a debuff: that target's next turn it can only attack the closest target.", 0, 0, 0);
        AddEffect(RelicCategory.Gloves, UnitRole.Shipwright, false, 2, 1, false,
                  RelicEffectType.Gloves_V1_Shipwright,
                  "**Push Hit** — Default attack. Target is forced forward 1 tile.", 0, 0, 0);
        AddEffect(RelicCategory.Gloves, UnitRole.Shipwright, true, 2, 1, false,
                  RelicEffectType.Gloves_V2_Shipwright,
                  "**Taunt Hit** — Default attack and apply a debuff: that target's next turn it can only attack the closest target.", 0, 0, 0);
        // AddEffect(RelicCategory.Gloves, UnitRole.MasterGunner, false, 2, 1, false,
        //          RelicEffectType.Gloves_V1_MasterGunner,
        //          "**Cadence Hit** — Default attack. +1 dmg per card already played this round.", 0, 0, 0);
        // AddEffect(RelicCategory.Gloves, UnitRole.MasterGunner, true, 2, 1, false,
        //          RelicEffectType.Gloves_V2_MasterGunner,
        //          "**Legacy Hit** — Default attack. +1 dmg per Master Gunner relic used this game.", 0, 0, 0);
        AddEffect(RelicCategory.Gloves, UnitRole.MasterGunner, false, 2, 1, false,
                  RelicEffectType.Gloves_V1_MasterGunner,
                  "**Cadence Hit** — Default attack. +1 dmg per card already played this round.", 0, 0, 0);
        AddEffect(RelicCategory.Gloves, UnitRole.MasterGunner, true, 2, 1, false,
                  RelicEffectType.Gloves_V2_MasterGunner,
                  "**Legacy Hit** — Default attack. +1 dmg per Master Gunner relic used this game.", 0, 0, 0);
        // AddEffect(RelicCategory.Gloves, UnitRole.Navigator, false, 2, 1, false,
        //          RelicEffectType.Gloves_V1_Navigator,
        //          "**Disabling Hit** — Cast: disable enemy weapons' role effect next turn (damage still happens but without the role-tag rider).", 0, 0, 0);
        // AddEffect(RelicCategory.Gloves, UnitRole.Navigator, true, 2, 1, false,
        //          RelicEffectType.Gloves_V2_Navigator,
        //          "**Boots Synergy Hit** — Default attack. +3 dmg per Boots relic card in your deck.", 0, 0, 0);
        AddEffect(RelicCategory.Gloves, UnitRole.Navigator, false, 2, 1, false,
                  RelicEffectType.Gloves_V1_Navigator,
                  "**Disabling Hit** — Cast: disable enemy weapons' role effect next turn (damage still happens but without the role-tag rider).", 0, 0, 0);
        AddEffect(RelicCategory.Gloves, UnitRole.Navigator, true, 2, 1, false,
                  RelicEffectType.Gloves_V2_Navigator,
                  "**Boots Synergy Hit** — Default attack. +3 dmg per Boots relic card in your deck.", 0, 0, 0);
        // AddEffect(RelicCategory.Gloves, UnitRole.Surgeon, false, 2, 1, false,
        //          RelicEffectType.Gloves_V1_Surgeon,
        //          "**Field Strike** — Default attack and restore 8 HP to the lowest-HP allied unit.", 0, 0, 0);
        // AddEffect(RelicCategory.Gloves, UnitRole.Surgeon, true, 2, 1, true,
        //          RelicEffectType.Gloves_V2_Surgeon,
        //          "**Healed-Target Hit** *(Passive)* — Whenever an enemy gets healed during their turn, this unit attacks that target with the default weapon.", 0, 0, 0);
        AddEffect(RelicCategory.Gloves, UnitRole.Surgeon, false, 2, 1, false,
                  RelicEffectType.Gloves_V1_Surgeon,
                  "**Field Strike** — Default attack and restore 8 HP to the lowest-HP allied unit.", 0, 0, 0);
        AddEffect(RelicCategory.Gloves, UnitRole.Surgeon, true, 2, 1, true,
                  RelicEffectType.Gloves_V2_Surgeon,
                  "**Healed-Target Hit** *(Passive)* — Whenever an enemy gets healed during their turn, this unit attacks that target with the default weapon.", 0, 0, 0);
        // AddEffect(RelicCategory.Gloves, UnitRole.Cook, false, 2, 1, false,
        //          RelicEffectType.Gloves_V1_Cook,
        //          "**Delayed Detonation** — Default attack and apply a debuff. Next time the target attacks, the debuff detonates for 8 dmg to all nearby enemies in 1-tile radius, scaled by turns it remained on the target.", 0, 0, 0);
        // AddEffect(RelicCategory.Gloves, UnitRole.Cook, true, 2, 1, false,
        //          RelicEffectType.Gloves_V2_Cook,
        //          "**Stasis Strike** — Put the closest target into Stasis for 1 turn (can be ally or enemy; target cannot attack, be attacked, or use any relics).", 0, 0, 0);
        AddEffect(RelicCategory.Gloves, UnitRole.Cook, false, 2, 1, false,
                  RelicEffectType.Gloves_V1_Cook,
                  "**Delayed Detonation** — Default attack and apply a debuff. Next time the target attacks, the debuff detonates for 8 dmg to all nearby enemies in 1-tile radius, scaled by turns it remained on the target.", 0, 0, 0);
        AddEffect(RelicCategory.Gloves, UnitRole.Cook, true, 2, 1, false,
                  RelicEffectType.Gloves_V2_Cook,
                  "**Stasis Strike** — Put the closest target into Stasis for 1 turn (can be ally or enemy; target cannot attack, be attacked, or use any relics).", 0, 0, 0);
        // AddEffect(RelicCategory.Gloves, UnitRole.Swashbuckler, false, 2, 1, false,
        //          RelicEffectType.Gloves_V1_Swashbuckler,
        //          "**Double Tap** — Default attack 2 times.", 0, 0, 0);
        // AddEffect(RelicCategory.Gloves, UnitRole.Swashbuckler, true, 2, 1, false,
        //          RelicEffectType.Gloves_V2_Swashbuckler,
        //          "**Tempo Trap** — Default attack. For 2 turns: if the target moves, it is stunned for 1 turn.", 0, 0, 0);
        AddEffect(RelicCategory.Gloves, UnitRole.Swashbuckler, false, 2, 1, false,
                  RelicEffectType.Gloves_V1_Swashbuckler,
                  "**Double Tap** — Default attack 2 times.", 0, 0, 0);
        AddEffect(RelicCategory.Gloves, UnitRole.Swashbuckler, true, 2, 1, false,
                  RelicEffectType.Gloves_V2_Swashbuckler,
                  "**Tempo Trap** — Default attack. For 2 turns: if the target moves, it is stunned for 1 turn.", 0, 0, 0);
        // AddEffect(RelicCategory.Gloves, UnitRole.Deckhand, false, 2, 1, false,
        //          RelicEffectType.Gloves_V1_Deckhand,
        //          "**Hull-Break Draw** — Default attack. If the attack destroys the target's Hull shield, draw 1 card.", 0, 0, 0);
        // AddEffect(RelicCategory.Gloves, UnitRole.Deckhand, true, 2, 1, false,
        //          RelicEffectType.Gloves_V2_Deckhand,
        //          "**Hull-Break Energy** — Default attack. If the attack destroys the target's Hull shield, gain 1 Energy.", 0, 0, 0);
        AddEffect(RelicCategory.Gloves, UnitRole.Deckhand, false, 2, 1, false,
                  RelicEffectType.Gloves_V1_Deckhand,
                  "**Hull-Break Draw** — Default attack. If the attack destroys the target's Hull shield, draw 1 card.", 0, 0, 0);
        AddEffect(RelicCategory.Gloves, UnitRole.Deckhand, true, 2, 1, false,
                  RelicEffectType.Gloves_V2_Deckhand,
                  "**Hull-Break Energy** — Default attack. If the attack destroys the target's Hull shield, gain 1 Energy.", 0, 0, 0);

//             AddEffect(RelicCategory.Gloves, UnitRole.Captain, false, 2, 1, false,
//                 RelicEffectType.Gloves_AttackReduceEnemyDraw,
//                 "Attack, enemy draws 1 less next turn", 0, 1, 1);
//             AddEffect(RelicCategory.Gloves, UnitRole.Quartermaster, false, 2, 1, false,
//                 RelicEffectType.Gloves_AttackBonusByMissingMorale,
//                 "Attack with weapon, increased damage based on enemy missing morale", 0, 0, 0);
//             AddEffect(RelicCategory.Gloves, UnitRole.Helmsmaster, false, 2, 1, false,
//                 RelicEffectType.Gloves_AttackPreventBuzzReduce,
//                 "Attack with weapon, prevent target from reducing buzz for 2 turns", 0, 0, 2);
//             AddEffect(RelicCategory.Gloves, UnitRole.Boatswain, false, 2, 1, false,
//                 RelicEffectType.Gloves_AttackBonusIfMoreHP,
//                 "Attack with weapon, +20% damage if target has less current HP than this unit", 0, 0.20f, 0);
//             AddEffect(RelicCategory.Gloves, UnitRole.Shipwright, false, 2, 1, false,
//                 RelicEffectType.Gloves_AttackPushForward,
//                 "Attack with weapon, push target forward 1 tile", 0, 1, 0);
//             AddEffect(RelicCategory.Gloves, UnitRole.MasterGunner, false, 2, 1, false,
//                 RelicEffectType.Gloves_AttackBonusPerCardPlayed,
//                 "Attack with weapon, +10% damage for each card played this round", 0, 0.10f, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.MasterAtArms, false, 2, 1, false,
                RelicEffectType.Gloves_V1_MasterAtArms,
                "Brawler's Hit - Attack with weapon, +20% bonus damage for each nearby allied unit in 1 tile radius", 0, 0.20f, 0, 1);
//             AddEffect(RelicCategory.Gloves, UnitRole.Navigator, false, 2, 1, false,
//                 RelicEffectType.Gloves_DisableWeaponEffect,
//                 "Disable enemy weapon role effects next turn, damage still applies", 0, 0, 1);
//             AddEffect(RelicCategory.Gloves, UnitRole.Surgeon, false, 2, 1, false,
//                 RelicEffectType.Gloves_AttackHealLowestAlly,
//                 "Attack, restore 200 HP to lowest ally", 0, 200, 0);
//             AddEffect(RelicCategory.Gloves, UnitRole.Cook, false, 2, 1, false,
//                 RelicEffectType.Gloves_AttackDetonateBuff,
//                 "Attack applies debuff, detonates on next hit for 200 AoE per turn remained", 200, 0, 0, 1);
//             AddEffect(RelicCategory.Gloves, UnitRole.Swashbuckler, false, 2, 1, false,
//                 RelicEffectType.Gloves_AttackTwice,
//                 "Attack with default weapon 2 times", 2, 0, 0);
//             AddEffect(RelicCategory.Gloves, UnitRole.Deckhand, false, 2, 1, false,
//                 RelicEffectType.Gloves_AttackDrawOnHullDestroyed,
//                 "Attack with default weapon, if hull destroyed draw 1 card", 0, 1, 0);

            // ==================== GLOVES V2 ====================
//             AddEffect(RelicCategory.Gloves, UnitRole.Captain, true, 2, 1, false,
//                 RelicEffectType.Gloves_AttackIncreaseEnemyCost,
//                 "Attack with weapon, enemy next card costs +1 energy", 0, 1, 1);
//             AddEffect(RelicCategory.Gloves, UnitRole.Quartermaster, true, 2, 1, false,
//                 RelicEffectType.Gloves_AttackMarkMoraleFocus,
//                 "Attack with weapon, mark target for morale focus fire for 2 turns", 0, 0, 2);
//             AddEffect(RelicCategory.Gloves, UnitRole.Helmsmaster, true, 2, 1, false,
//                 RelicEffectType.Gloves_AttackBonusPerGrog,
//                 "Attack with weapon, +20% damage per available Grog token", 0, 0.20f, 0);
//             AddEffect(RelicCategory.Gloves, UnitRole.Boatswain, true, 2, 1, false,
//                 RelicEffectType.Gloves_AttackLowerEnemyHealth,
//                 "Attack with weapon, lower enemy health stat by 30% for 2 turns", 0, 0.30f, 2);
//             AddEffect(RelicCategory.Gloves, UnitRole.Shipwright, true, 2, 1, false,
//                 RelicEffectType.Gloves_AttackForceTargetClosest,
//                 "Attack with weapon, debuff forces target to only attack closest target next turn", 0, 0, 1);
//             AddEffect(RelicCategory.Gloves, UnitRole.MasterGunner, true, 2, 1, false,
//                 RelicEffectType.Gloves_AttackBonusPerGunnerRelic,
//                 "Attack with weapon, +10% damage for each Master Gunner relic used this game", 0, 0.10f, 0);
            AddEffect(RelicCategory.Gloves, UnitRole.MasterAtArms, true, 2, 1, false,
                RelicEffectType.Gloves_V2_MasterAtArms,
                "Pummel - Attack with weapon, +10% damage for each Master-at-Arms relic card in hand", 0, 0.10f, 0);
//             AddEffect(RelicCategory.Gloves, UnitRole.Navigator, true, 2, 1, false,
//                 RelicEffectType.Gloves_V2_AttackBonusPerBootsCard,
//                 "Attack with weapon, +30% bonus damage for each boots relic card in deck", 0, 0.30f, 0);
            AddPassive(RelicCategory.Gloves, UnitRole.Surgeon, true,
                RelicEffectType.Gloves_V2_AttackHealedEnemy,
                "Passive: Attack any enemy that gets healed", 0, 0);
//             AddEffect(RelicCategory.Gloves, UnitRole.Cook, true, 2, 1, false,
//                 RelicEffectType.Gloves_V2_StasisClosest,
//                 "Stasis closest target 1 turn, can't attack or be attacked", 0, 0, 1);
//             AddEffect(RelicCategory.Gloves, UnitRole.Swashbuckler, true, 2, 1, false,
//                 RelicEffectType.Gloves_V2_AttackStunOnMove,
//                 "Attack, if target moves in 2 turns stun for 1 turn", 0, 0, 2);
//             AddEffect(RelicCategory.Gloves, UnitRole.Deckhand, true, 2, 1, false,
//                 RelicEffectType.Gloves_V2_AttackEnergyOnHullDestroyed,
//                 "Attack with default weapon, if hull destroyed get 1 energy", 0, 1, 0);

            // ==================== HAT V1 ====================
        // AddEffect(RelicCategory.Hat, UnitRole.Captain, false, 2, 1, false, RelicEffectType.Hat_V1_Captain, "**Tricorn of Command** — Draw 2 cards. For 2 turns, the Captain takes +200% damage taken (i.e. double damage taken).", 0, 0, 0);
        // AddEffect(RelicCategory.Hat, UnitRole.Captain, true, 2, 1, false, RelicEffectType.Hat_V2_Captain, "**Crown of the Sea** — Draw an Ultimate ability card immediately.", 0, 0, 0);
        AddEffect(RelicCategory.Hat, UnitRole.Captain, false, 2, 1, false, RelicEffectType.Hat_V1_Captain, "**Tricorn of Command** — Draw 2 cards. For 2 turns, the Captain takes +200% damage taken (i.e. double damage taken).", 0, 0, 0);
        AddEffect(RelicCategory.Hat, UnitRole.Captain, true, 2, 1, false, RelicEffectType.Hat_V2_Captain, "**Crown of the Sea** — Draw an Ultimate ability card immediately.", 0, 0, 0);
        // AddEffect(RelicCategory.Hat, UnitRole.Quartermaster, false, 2, 1, false, RelicEffectType.Hat_V1_Quartermaster, "**Ledger Cap** — Restore 10 + Morale Tier morale to the lowest-morale ally.", 0, 0, 0);
        // AddEffect(RelicCategory.Hat, UnitRole.Quartermaster, true, 2, 1, false, RelicEffectType.Hat_V2_Quartermaster, "**Rally Cap** — All nearby allies within 1 tile restore Morale Tier morale each.", 0, 0, 0);
        AddEffect(RelicCategory.Hat, UnitRole.Quartermaster, false, 2, 1, false, RelicEffectType.Hat_V1_Quartermaster, "**Ledger Cap** — Restore 10 + Morale Tier morale to the lowest-morale ally.", 0, 0, 0);
        AddEffect(RelicCategory.Hat, UnitRole.Quartermaster, true, 2, 1, false, RelicEffectType.Hat_V2_Quartermaster, "**Rally Cap** — All nearby allies within 1 tile restore Morale Tier morale each.", 0, 0, 0);
        // AddEffect(RelicCategory.Hat, UnitRole.Helmsmaster, false, 2, 1, false, RelicEffectType.Hat_V1_Helmsmaster, "**Brewmaster's Cap** — This round, all rum usage costs 0 Grog Tokens (next 3 rum uses).", 0, 0, 0);
        // AddEffect(RelicCategory.Hat, UnitRole.Helmsmaster, true, 2, 1, false, RelicEffectType.Hat_V2_Helmsmaster, "**Grog Crown** — Generate 2 Grog Tokens.", 0, 0, 0);
        AddEffect(RelicCategory.Hat, UnitRole.Helmsmaster, false, 2, 1, false, RelicEffectType.Hat_V1_Helmsmaster, "**Brewmaster's Cap** — This round, all rum usage costs 0 Grog Tokens (next 3 rum uses).", 0, 0, 0);
        AddEffect(RelicCategory.Hat, UnitRole.Helmsmaster, true, 2, 1, false, RelicEffectType.Hat_V2_Helmsmaster, "**Grog Crown** — Generate 2 Grog Tokens.", 0, 0, 0);
        // AddEffect(RelicCategory.Hat, UnitRole.Boatswain, false, 2, 1, false, RelicEffectType.Hat_V1_Boatswain, "**Reflect Helm** — Last 2 turns: returns 1 instance of damage back to attacker per hit.", 0, 0, 0);
        // AddEffect(RelicCategory.Hat, UnitRole.Boatswain, true, 2, 1, false, RelicEffectType.Hat_V2_Boatswain, "**Hull-Helm** — Last 2 turns: this unit's Health stat is increased by 25% (+Health Tier×2).", 0, 0, 0);
        AddEffect(RelicCategory.Hat, UnitRole.Boatswain, false, 2, 1, false, RelicEffectType.Hat_V1_Boatswain, "**Reflect Helm** — Last 2 turns: returns 1 instance of damage back to attacker per hit.", 0, 0, 0);
        AddEffect(RelicCategory.Hat, UnitRole.Boatswain, true, 2, 1, false, RelicEffectType.Hat_V2_Boatswain, "**Hull-Helm** — Last 2 turns: this unit's Health stat is increased by 25% (+Health Tier×2).", 0, 0, 0);
        // AddEffect(RelicCategory.Hat, UnitRole.Shipwright, false, 2, 1, false, RelicEffectType.Hat_V1_Shipwright, "**Mariner's Bandana** — Gain 2 extra Energy next turn if this unit is knocked back.", 0, 0, 0);
        // AddEffect(RelicCategory.Hat, UnitRole.Shipwright, true, 2, 1, false, RelicEffectType.Hat_V2_Shipwright, "**Grit Swap Cap** — Swap the position of the enemy unit with the highest Grit with the one with the lowest Grit.", 0, 0, 0);
        AddEffect(RelicCategory.Hat, UnitRole.Shipwright, false, 2, 1, false, RelicEffectType.Hat_V1_Shipwright, "**Mariner's Bandana** — Gain 2 extra Energy next turn if this unit is knocked back.", 0, 0, 0);
        AddEffect(RelicCategory.Hat, UnitRole.Shipwright, true, 2, 1, false, RelicEffectType.Hat_V2_Shipwright, "**Grit Swap Cap** — Swap the position of the enemy unit with the highest Grit with the one with the lowest Grit.", 0, 0, 0);
        // AddEffect(RelicCategory.Hat, UnitRole.MasterGunner, false, 2, 1, false, RelicEffectType.Hat_V1_MasterGunner, "**Sharpshooter Hat** — Your next weapon relic can be used twice this turn. (Bonus damage applies to any weapon used.)", 0, 0, 0);
        // AddEffect(RelicCategory.Hat, UnitRole.MasterGunner, true, 2, 1, false, RelicEffectType.Hat_V2_MasterGunner, "**Spotter's Cap** — Draw a weapon relic from your deck.", 0, 0, 0);
        AddEffect(RelicCategory.Hat, UnitRole.MasterGunner, false, 2, 1, false, RelicEffectType.Hat_V1_MasterGunner, "**Sharpshooter Hat** — Your next weapon relic can be used twice this turn. (Bonus damage applies to any weapon used.)", 0, 0, 0);
        AddEffect(RelicCategory.Hat, UnitRole.MasterGunner, true, 2, 1, false, RelicEffectType.Hat_V2_MasterGunner, "**Spotter's Cap** — Draw a weapon relic from your deck.", 0, 0, 0);
        // AddEffect(RelicCategory.Hat, UnitRole.Navigator, false, 2, 1, false, RelicEffectType.Hat_V1_Navigator, "**Compass Cap** — Enemies cannot use Ultimate abilities next turn.", 0, 0, 0);
        // AddEffect(RelicCategory.Hat, UnitRole.Navigator, true, 2, 1, false, RelicEffectType.Hat_V2_Navigator, "**Charting Cap** — Cast to get a Boots relic card in hand.", 0, 0, 0);
        AddEffect(RelicCategory.Hat, UnitRole.Navigator, false, 2, 1, false, RelicEffectType.Hat_V1_Navigator, "**Compass Cap** — Enemies cannot use Ultimate abilities next turn.", 0, 0, 0);
        AddEffect(RelicCategory.Hat, UnitRole.Navigator, true, 2, 1, false, RelicEffectType.Hat_V2_Navigator, "**Charting Cap** — Cast to get a Boots relic card in hand.", 0, 0, 0);
        // AddEffect(RelicCategory.Hat, UnitRole.Surgeon, false, 2, 1, false, RelicEffectType.Hat_V1_Surgeon, "**Field Cap** — Draw a Trinket relic card and reduce its cost by 1.", 0, 0, 0);
        // AddEffect(RelicCategory.Hat, UnitRole.Surgeon, true, 2, 1, false, RelicEffectType.Hat_V2_Surgeon, "**Healing Buff** — Buff: one ally that does damage to an enemy is healed by 10% HP (≈ +Health Tier HP) this turn.", 0, 0, 0);
        AddEffect(RelicCategory.Hat, UnitRole.Surgeon, false, 2, 1, false, RelicEffectType.Hat_V1_Surgeon, "**Field Cap** — Draw a Trinket relic card and reduce its cost by 1.", 0, 0, 0);
        AddEffect(RelicCategory.Hat, UnitRole.Surgeon, true, 2, 1, false, RelicEffectType.Hat_V2_Surgeon, "**Healing Buff** — Buff: one ally that does damage to an enemy is healed by 10% HP (≈ +Health Tier HP) this turn.", 0, 0, 0);
        // AddEffect(RelicCategory.Hat, UnitRole.Cook, false, 2, 1, false, RelicEffectType.Hat_V1_Cook, "**Tasting Cap** — This turn, reduce by 1 the cost of relic cards of the lowest-HP allied unit.", 0, 0, 0);
        // AddEffect(RelicCategory.Hat, UnitRole.Cook, true, 2, 1, false, RelicEffectType.Hat_V2_Cook, "**Carry Cap** — Move a unit forward 1 tile and heal it for 10% HP (≈ +Health Tier HP).", 0, 0, 0);
        AddEffect(RelicCategory.Hat, UnitRole.Cook, false, 2, 1, false, RelicEffectType.Hat_V1_Cook, "**Tasting Cap** — This turn, reduce by 1 the cost of relic cards of the lowest-HP allied unit.", 0, 0, 0);
        AddEffect(RelicCategory.Hat, UnitRole.Cook, true, 2, 1, false, RelicEffectType.Hat_V2_Cook, "**Carry Cap** — Move a unit forward 1 tile and heal it for 10% HP (≈ +Health Tier HP).", 0, 0, 0);
        // AddEffect(RelicCategory.Hat, UnitRole.Swashbuckler, false, 2, 1, false, RelicEffectType.Hat_V1_Swashbuckler, "**Plumed Hat** — Draw a card; if it's a weapon relic, reduce its cost by 1.", 0, 0, 0);
        // AddEffect(RelicCategory.Hat, UnitRole.Swashbuckler, true, 2, 1, false, RelicEffectType.Hat_V2_Swashbuckler, "**Pickpocket Hat** — Steal a random enemy card; if it's a weapon, reduce its cost by 1.", 0, 0, 0);
        AddEffect(RelicCategory.Hat, UnitRole.Swashbuckler, false, 2, 1, false, RelicEffectType.Hat_V1_Swashbuckler, "**Plumed Hat** — Draw a card; if it's a weapon relic, reduce its cost by 1.", 0, 0, 0);
        AddEffect(RelicCategory.Hat, UnitRole.Swashbuckler, true, 2, 1, false, RelicEffectType.Hat_V2_Swashbuckler, "**Pickpocket Hat** — Steal a random enemy card; if it's a weapon, reduce its cost by 1.", 0, 0, 0);
        // AddEffect(RelicCategory.Hat, UnitRole.Deckhand, false, 2, 1, false, RelicEffectType.Hat_V1_Deckhand, "**Hard Hat** — Nearby allies within 1 tile have their Hull shield increased by 3 (≈ +30%).", 0, 0, 0);
        // AddEffect(RelicCategory.Hat, UnitRole.Deckhand, true, 2, 1, false, RelicEffectType.Hat_V2_Deckhand, "**Salvage Cap** — Destroy all soft obstacles on the map; gain +20% Hull for each destroyed (≈ +2 Hull per obstacle).", 0, 0, 0);
        AddEffect(RelicCategory.Hat, UnitRole.Deckhand, false, 2, 1, false, RelicEffectType.Hat_V1_Deckhand, "**Hard Hat** — Nearby allies within 1 tile have their Hull shield increased by 3 (≈ +30%).", 0, 0, 0);
        AddEffect(RelicCategory.Hat, UnitRole.Deckhand, true, 2, 1, false, RelicEffectType.Hat_V2_Deckhand, "**Salvage Cap** — Destroy all soft obstacles on the map; gain +20% Hull for each destroyed (≈ +2 Hull per obstacle).", 0, 0, 0);

//             AddEffect(RelicCategory.Hat, UnitRole.Captain, false, 2, 1, false,
//                 RelicEffectType.Hat_DrawCardsVulnerable,
//                 "Draw 2, take 200% damage for 2 turns", 2, 2.0f, 2);
//             AddEffect(RelicCategory.Hat, UnitRole.Quartermaster, false, 2, 1, false,
//                 RelicEffectType.Hat_RestoreMoraleLowest,
//                 "Restore 30% morale to lowest morale ally", 0, 0.30f, 0);
//             AddEffect(RelicCategory.Hat, UnitRole.Helmsmaster, false, 2, 1, false,
//                 RelicEffectType.Hat_FreeRumUsage,
//                 "This round 3 rum usage cost no Grog", 3, 0, 0);
//             AddEffect(RelicCategory.Hat, UnitRole.Boatswain, false, 2, 1, false,
//                 RelicEffectType.Hat_ReturnDamage,
//                 "For 2 turns, return 1 instance of damage back", 1, 0, 2);
//             AddEffect(RelicCategory.Hat, UnitRole.Shipwright, false, 2, 1, false,
//                 RelicEffectType.Hat_EnergyOnKnockback,
//                 "Get 2 extra energy next turn if this unit is knocked back", 2, 0, 1);
//             AddEffect(RelicCategory.Hat, UnitRole.MasterGunner, false, 2, 1, false,
//                 RelicEffectType.Hat_WeaponUseTwice,
//                 "Next weapon relic can be used twice", 0, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.MasterAtArms, false, 2, 1, false,
                RelicEffectType.Hat_V1_MasterAtArms,
                "Drill Cap - Reduce the cost of your next ultimate ability by 2", 2, 0, 0);
//             AddEffect(RelicCategory.Hat, UnitRole.Navigator, false, 2, 1, false,
//                 RelicEffectType.Hat_DisableEnemyUltimates,
//                 "Enemies can't use ultimate abilities next turn", 0, 0, 1);
//             AddEffect(RelicCategory.Hat, UnitRole.Surgeon, false, 2, 1, false,
//                 RelicEffectType.Hat_DrawTrinketReduceCost,
//                 "Draw trinket card, reduce cost by 1", 1, 0, 0);
//             AddEffect(RelicCategory.Hat, UnitRole.Cook, false, 2, 1, false,
//                 RelicEffectType.Hat_ReduceLowestAllyCardCost,
//                 "Reduce cost of lowest health ally's relic cards by 1 this turn", 1, 0, 1);
//             AddEffect(RelicCategory.Hat, UnitRole.Swashbuckler, false, 2, 1, false,
//                 RelicEffectType.Hat_DrawWeaponReduceCost,
//                 "Draw a card, if weapon reduce cost by 1", 1, 0, 0);
//             AddEffect(RelicCategory.Hat, UnitRole.Deckhand, false, 2, 1, false,
//                 RelicEffectType.Hat_NearbyHullIncrease,
//                 "Nearby allies in 1 tile radius have hull shield increased by 30%", 0.30f, 0, 0, 1);

            // ==================== HAT V2 ====================
//             AddEffect(RelicCategory.Hat, UnitRole.Captain, true, 2, 1, false,
//                 RelicEffectType.Hat_DrawUltimate,
//                 "Draw an ultimate ability", 1, 0, 0);
//             AddEffect(RelicCategory.Hat, UnitRole.Quartermaster, true, 2, 1, false,
//                 RelicEffectType.Hat_RestoreMoraleNearby,
//                 "All nearby allies in 1 tile range have 10% morale restored", 0, 0.10f, 0, 1);
//             AddEffect(RelicCategory.Hat, UnitRole.Helmsmaster, true, 2, 1, false,
//                 RelicEffectType.Hat_GenerateGrog,
//                 "Generate 2 Grog tokens", 2, 0, 0);
//             AddEffect(RelicCategory.Hat, UnitRole.Boatswain, true, 2, 1, false,
//                 RelicEffectType.Hat_IncreaseHealthStat,
//                 "For 2 turns, health stat of this unit is increased by 25%", 0, 0.25f, 2);
//             AddEffect(RelicCategory.Hat, UnitRole.Shipwright, true, 2, 1, false,
//                 RelicEffectType.Hat_SwapEnemyByGrit,
//                 "Swap position of enemy with highest grit stat with enemy with lowest grit stat", 0, 0, 0);
//             AddEffect(RelicCategory.Hat, UnitRole.MasterGunner, true, 2, 1, false,
//                 RelicEffectType.Hat_DrawWeaponRelic,
//                 "Draw a weapon relic", 1, 0, 0);
            AddEffect(RelicCategory.Hat, UnitRole.MasterAtArms, true, 2, 1, false,
                RelicEffectType.Hat_V2_MasterAtArms,
                "Forge Helm - Increase the cost of enemy next weapon relic by 1", 1, 0, 0);
//             AddEffect(RelicCategory.Hat, UnitRole.Navigator, true, 2, 1, false,
//                 RelicEffectType.Hat_V2_DrawBootsCard,
//                 "Get a boots card relic in hand", 1, 0, 0);
//             AddEffect(RelicCategory.Hat, UnitRole.Surgeon, true, 2, 1, false,
//                 RelicEffectType.Hat_V2_HealOnCaptainDamage,
//                 "Allies that damage captain healed 10%", 0, 0.10f, 0);
//             AddEffect(RelicCategory.Hat, UnitRole.Cook, true, 2, 1, false,
//                 RelicEffectType.Hat_V2_MoveForwardHeal,
//                 "Move unit forward 1 tile, heal 10%", 1, 0.10f, 0);
//             AddEffect(RelicCategory.Hat, UnitRole.Swashbuckler, true, 2, 1, false,
//                 RelicEffectType.Hat_V2_StealEnemyCard,
//                 "Steal random enemy card, if weapon reduce cost by 1", 1, 0, 0);
//             AddEffect(RelicCategory.Hat, UnitRole.Deckhand, true, 2, 1, false,
//                 RelicEffectType.Hat_V2_DestroyObstaclesGainHull,
//                 "Destroy all soft obstacles, +20% hull per obstacle destroyed", 0, 0.20f, 0);

            // ==================== COAT V1 ====================
        // AddEffect(RelicCategory.Coat, UnitRole.Captain, false, 2, 1, false, RelicEffectType.Coat_V1_Captain, "Captain's Coat of Authority - Allies in 1-tile radius gain +2 Aim and +2 Power for 2 turns.", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.Captain, true, 2, 1, false, RelicEffectType.Coat_V2_Captain, "Privateer's Greatcoat - For 2 turns (max 3 enemy attacks): when enemies attack, draw 1 card and the enemy discards 1 next turn. If the enemy doesn't attack for 2 turns, the effect expires.", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.Captain, false, 2, 1, false, RelicEffectType.Coat_V1_Captain, "Captain's Coat of Authority - Allies in 1-tile radius gain +2 Aim and +2 Power for 2 turns.", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.Captain, true, 2, 1, false, RelicEffectType.Coat_V2_Captain, "Privateer's Greatcoat - For 2 turns (max 3 enemy attacks): when enemies attack, draw 1 card and the enemy discards 1 next turn. If the enemy doesn't attack for 2 turns, the effect expires.", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.Quartermaster, false, 2, 1, false, RelicEffectType.Coat_V1_Quartermaster, "Tally Coat - For 2 turns, allies take -3 morale damage (30% less).", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.Quartermaster, true, 2, 1, false, RelicEffectType.Coat_V2_Quartermaster, "Surrender Cloak - For 2 turns, buff an ally unit: if that unit would surrender, restore 5 + Morale Tier morale instead. Target: ally.", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.Quartermaster, false, 2, 1, false, RelicEffectType.Coat_V1_Quartermaster, "Tally Coat - For 2 turns, allies take -3 morale damage (30% less).", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.Quartermaster, true, 2, 1, false, RelicEffectType.Coat_V2_Quartermaster, "Surrender Cloak - For 2 turns, buff an ally unit: if that unit would surrender, restore 5 + Morale Tier morale instead. Target: ally.", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.Helmsmaster, false, 2, 1, false, RelicEffectType.Coat_V1_Helmsmaster, "Drunkard's Greatcoat - Nearby allies in 1 tile have reduced rum effect for that turn.", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.Helmsmaster, true, 2, 1, false, RelicEffectType.Coat_V2_Helmsmaster, "Brewer's Mantle - Next turn, enemies' Buzz meter fills completely whenever they deal damage.", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.Helmsmaster, false, 2, 1, false, RelicEffectType.Coat_V1_Helmsmaster, "Drunkard's Greatcoat - Nearby allies in 1 tile have reduced rum effect for that turn.", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.Helmsmaster, true, 2, 1, false, RelicEffectType.Coat_V2_Helmsmaster, "Brewer's Mantle - Next turn, enemies' Buzz meter fills completely whenever they deal damage.", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.Boatswain, false, 2, 1, false, RelicEffectType.Coat_V1_Boatswain, "Tide-Iron Coat - Allied units within 1 tile cannot be displaced or knocked back during enemy next turn.", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.Boatswain, true, 2, 1, false, RelicEffectType.Coat_V2_Boatswain, "Stormwarden - The lowest-HP ally can only be targeted next turn by enemies with lower HP than themselves.", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.Boatswain, false, 2, 1, false, RelicEffectType.Coat_V1_Boatswain, "Tide-Iron Coat - Allied units within 1 tile cannot be displaced or knocked back during enemy next turn.", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.Boatswain, true, 2, 1, false, RelicEffectType.Coat_V2_Boatswain, "Stormwarden - The lowest-HP ally can only be targeted next turn by enemies with lower HP than themselves.", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.Shipwright, false, 2, 1, false, RelicEffectType.Coat_V1_Shipwright, "Carpenter's Coat - For 2 turns, allies in the same row behind this unit cannot be targeted.", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.Shipwright, true, 2, 1, false, RelicEffectType.Coat_V2_Shipwright, "Volley Vest - Give +4 dmg (+40%) to all allied units in the same column.", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.Shipwright, false, 2, 1, false, RelicEffectType.Coat_V1_Shipwright, "Carpenter's Coat - For 2 turns, allies in the same row behind this unit cannot be targeted.", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.Shipwright, true, 2, 1, false, RelicEffectType.Coat_V2_Shipwright, "Volley Vest - Give +4 dmg (+40%) to all allied units in the same column.", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.MasterGunner, false, 2, 1, false, RelicEffectType.Coat_V1_MasterGunner, "Stowmaster's Coat - Your next 2 Stows have no Energy cost.", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.MasterGunner, true, 2, 1, false, RelicEffectType.Coat_V2_MasterGunner, "Volley Cover - Allies in the same row take 50% less damage from Ranged attacks next turn (5 dmg cap).", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.MasterGunner, false, 2, 1, false, RelicEffectType.Coat_V1_MasterGunner, "Stowmaster's Coat - Your next 2 Stows have no Energy cost.", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.MasterGunner, true, 2, 1, false, RelicEffectType.Coat_V2_MasterGunner, "Volley Cover - Allies in the same row take 50% less damage from Ranged attacks next turn (5 dmg cap).", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.MasterAtArms, false, 2, 1, false, RelicEffectType.Coat_V1_MasterAtArms, "Sergeant's Coat - Gives +2 (+20%) extra weapon damage to all nearby allies in 1-tile radius.", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.MasterAtArms, true, 2, 1, false, RelicEffectType.Coat_V2_MasterAtArms, "Charge Coat - All enemies next turn have -3 Power (-35% Power stat).", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.MasterAtArms, false, 2, 1, false, RelicEffectType.Coat_V1_MasterAtArms, "Sergeant's Coat - Gives +2 (+20%) extra weapon damage to all nearby allies in 1-tile radius.", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.MasterAtArms, true, 2, 1, false, RelicEffectType.Coat_V2_MasterAtArms, "Charge Coat - All enemies next turn have -3 Power (-35% Power stat).", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.Navigator, false, 2, 1, false, RelicEffectType.Coat_V1_Navigator, "Cartographer's Cloak - Take 0 HP damage from the next attack for 2 turns. (Morale damage still counts. Expires in 2 turns if no attack happens.)", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.Navigator, true, 2, 1, false, RelicEffectType.Coat_V2_Navigator, "Skyline Mantle - Next turn, the first ally that gets attacked dodges by moving 1 tile back.", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.Navigator, false, 2, 1, false, RelicEffectType.Coat_V1_Navigator, "Cartographer's Cloak - Take 0 HP damage from the next attack for 2 turns. (Morale damage still counts. Expires in 2 turns if no attack happens.)", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.Navigator, true, 2, 1, false, RelicEffectType.Coat_V2_Navigator, "Skyline Mantle - Next turn, the first ally that gets attacked dodges by moving 1 tile back.", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.Surgeon, false, 2, 1, false, RelicEffectType.Coat_V1_Surgeon, "Medic's Cloak - Increase the Primary and Secondary stat of an allied unit by 100% (double both) for 1 turn.", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.Surgeon, true, 2, 1, true, RelicEffectType.Coat_V2_Surgeon, "Last-Stand Coat (Passive) - When an enemy kills or makes an ally surrender, the enemy in 1-tile radius is knocked back 1 tile.", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.Surgeon, false, 2, 1, false, RelicEffectType.Coat_V1_Surgeon, "Medic's Cloak - Increase the Primary and Secondary stat of an allied unit by 100% (double both) for 1 turn.", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.Surgeon, true, 2, 1, true, RelicEffectType.Coat_V2_Surgeon, "Last-Stand Coat (Passive) - When an enemy kills or makes an ally surrender, the enemy in 1-tile radius is knocked back 1 tile.", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.Cook, false, 2, 1, false, RelicEffectType.Coat_V1_Cook, "Stove Cloak - Apply a buff to the closest ally for 1 turn: if that ally is attacked next turn, the attacker is stunned for 1 turn.", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.Cook, true, 2, 1, false, RelicEffectType.Coat_V2_Cook, "Spice Mantle - Clear all debuffs from nearby allies in 1-tile radius.", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.Cook, false, 2, 1, false, RelicEffectType.Coat_V1_Cook, "Stove Cloak - Apply a buff to the closest ally for 1 turn: if that ally is attacked next turn, the attacker is stunned for 1 turn.", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.Cook, true, 2, 1, false, RelicEffectType.Coat_V2_Cook, "Spice Mantle - Clear all debuffs from nearby allies in 1-tile radius.", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.Swashbuckler, false, 2, 1, true, RelicEffectType.Coat_V1_Swashbuckler, "Duelist's Coat (Passive) - Nearby allies within 1-tile radius take 15% less damage when attacked by an enemy with lower Speed.", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.Swashbuckler, true, 2, 1, false, RelicEffectType.Coat_V2_Swashbuckler, "Snare Coat - Curse a random empty tile on enemy side. Any enemy that steps in cannot leave it anymore and takes +1 dmg (+10% incoming damage).", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.Swashbuckler, false, 2, 1, true, RelicEffectType.Coat_V1_Swashbuckler, "Duelist's Coat (Passive) - Nearby allies within 1-tile radius take 15% less damage when attacked by an enemy with lower Speed.", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.Swashbuckler, true, 2, 1, false, RelicEffectType.Coat_V2_Swashbuckler, "Snare Coat - Curse a random empty tile on enemy side. Any enemy that steps in cannot leave it anymore and takes +1 dmg (+10% incoming damage).", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.Deckhand, false, 2, 1, false, RelicEffectType.Coat_V1_Deckhand, "Hull-Surge Coat - This turn, gain bonus weapon damage equal to 50% of available Hull shield for yourself and nearby allies in 1-tile radius. Bonus damage applies to any weapon used.", 0, 0, 0);
        // AddEffect(RelicCategory.Coat, UnitRole.Deckhand, true, 2, 1, false, RelicEffectType.Coat_V2_Deckhand, "Tile Charm Coat - Buff a random tile. Units that stay on that tile take -1 dmg (-15%) and deal +1 dmg (+15%).", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.Deckhand, false, 2, 1, false, RelicEffectType.Coat_V1_Deckhand, "Hull-Surge Coat - This turn, gain bonus weapon damage equal to 50% of available Hull shield for yourself and nearby allies in 1-tile radius. Bonus damage applies to any weapon used.", 0, 0, 0);
        AddEffect(RelicCategory.Coat, UnitRole.Deckhand, true, 2, 1, false, RelicEffectType.Coat_V2_Deckhand, "Tile Charm Coat - Buff a random tile. Units that stay on that tile take -1 dmg (-15%) and deal +1 dmg (+15%).", 0, 0, 0);

//             AddEffect(RelicCategory.Coat, UnitRole.Captain, false, 2, 1, false,
//                 RelicEffectType.Coat_BuffNearbyAimPower,
//                 "Allies in 1 tile radius receive +20% Aim and Power for 2 turns", 0, 0.20f, 2, 1);
//             AddEffect(RelicCategory.Coat, UnitRole.Quartermaster, false, 2, 1, false,
//                 RelicEffectType.Coat_ReduceMoraleDamage,
//                 "For 2 turns allies take 30% less morale damage", 0, 0.30f, 2);
//             AddEffect(RelicCategory.Coat, UnitRole.Helmsmaster, false, 2, 1, false,
//                 RelicEffectType.Coat_ReduceRumEffect,
//                 "Nearby allies in 1 tile radius have reduced rum effect this turn", 0, 0, 1, 1);
//             AddEffect(RelicCategory.Coat, UnitRole.Boatswain, false, 2, 1, false,
//                 RelicEffectType.Coat_PreventDisplacement,
//                 "Allies in 1 tile radius can't be displaced or knocked back next enemy turn", 0, 0, 1, 1);
//             AddEffect(RelicCategory.Coat, UnitRole.Shipwright, false, 2, 1, false,
//                 RelicEffectType.Coat_RowCantBeTargeted,
//                 "For 2 turns allies in same row behind this unit can't be targeted", 0, 0, 2);
//             AddEffect(RelicCategory.Coat, UnitRole.MasterGunner, false, 2, 1, false,
//                 RelicEffectType.Coat_FreeStow,
//                 "Next 2 stows have no cost", 2, 0, 0);
//             AddEffect(RelicCategory.Coat, UnitRole.MasterAtArms, false, 2, 1, false,
//                 RelicEffectType.Coat_BonusDamageNearbyAllies,
//                 "Give 20% extra damage to all nearby allies in 1 tile radius", 0, 0.20f, 0, 1);
//             AddEffect(RelicCategory.Coat, UnitRole.Navigator, false, 2, 1, false,
//                 RelicEffectType.Coat_HealthDamageImmunity,
//                 "Take 0 health damage from next attack for 2 turns, morale damage still counts", 0, 0, 2);
//             AddEffect(RelicCategory.Coat, UnitRole.Surgeon, false, 2, 1, false,
//                 RelicEffectType.Coat_DoubleAllyStats,
//                 "+100% primary+secondary stat 1 turn", 1.0f, 0, 1);
//             AddEffect(RelicCategory.Coat, UnitRole.Cook, false, 2, 1, false,
//                 RelicEffectType.Coat_StunOnAllyAttacked,
//                 "Buff closest ally 1 turn, if attacked stun enemy 1 turn", 0, 0, 1);
            AddPassive(RelicCategory.Coat, UnitRole.Swashbuckler, false,
                RelicEffectType.Coat_NearbyAllyDamageReduction,
                "Passive: Nearby allies in 1 tile take 15% less damage if attacker has lower speed", 0.15f, 0);
//             AddEffect(RelicCategory.Coat, UnitRole.Deckhand, false, 2, 1, false,
//                 RelicEffectType.Coat_HullBonusDamage,
//                 "Bonus weapon damage = 50% of hull shield for self and nearby allies 1 tile", 0, 0.50f, 1, 1);

            // ==================== COAT V2 ====================
//             AddEffect(RelicCategory.Coat, UnitRole.Captain, true, 2, 1, false,
//                 RelicEffectType.Coat_DrawOnEnemyAttack,
//                 "For each enemy attack (3 max) next 2 turns, draw a card and enemy discards 1 next turn", 3, 1, 2);
//             AddEffect(RelicCategory.Coat, UnitRole.Quartermaster, true, 2, 1, false,
//                 RelicEffectType.Coat_PreventSurrender,
//                 "For 2 turns buff an ally, if that unit would surrender restore 20% morale instead", 0, 0.20f, 2);
//             AddEffect(RelicCategory.Coat, UnitRole.Helmsmaster, true, 2, 1, false,
//                 RelicEffectType.Coat_EnemyBuzzOnDamage,
//                 "Next turn enemies buzz meter fills every time they do damage", 0, 0, 1);
//             AddEffect(RelicCategory.Coat, UnitRole.Boatswain, true, 2, 1, false,
//                 RelicEffectType.Coat_ProtectLowHP,
//                 "Lowest HP ally can only be targeted next turn by enemies with lower HP", 0, 0, 1);
//             AddEffect(RelicCategory.Coat, UnitRole.Shipwright, true, 2, 1, false,
//                 RelicEffectType.Coat_ColumnDamageBoost,
//                 "Give 40% increased damage to all allied units in the same column", 0, 0.40f, 0);
//             AddEffect(RelicCategory.Coat, UnitRole.MasterGunner, true, 2, 1, false,
//                 RelicEffectType.Coat_RowRangedProtection,
//                 "Allies in same row take 50% less damage from ranged attacks next turn", 0, 0.50f, 1);
//             AddEffect(RelicCategory.Coat, UnitRole.MasterAtArms, true, 2, 1, false,
//                 RelicEffectType.Coat_V2_ReduceEnemyPower,
//                 "All enemies next turn have 35% less Power stat", 0, 0.35f, 1);
//             AddEffect(RelicCategory.Coat, UnitRole.Navigator, true, 2, 1, false,
//                 RelicEffectType.Coat_V2_DodgeFirstAttack,
//                 "Next turn first ally that gets attacked dodges by moving 1 tile back", 0, 0, 1);
            AddPassive(RelicCategory.Coat, UnitRole.Surgeon, true,
                RelicEffectType.Coat_V2_KnockbackOnAllyDeath,
                "Passive: Knockback enemy on ally death in 1 tile", 1, 0);
//             AddEffect(RelicCategory.Coat, UnitRole.Cook, true, 2, 1, false,
//                 RelicEffectType.Coat_V2_ClearDebuffsNearby,
//                 "Clear all debuffs from nearby allies 1 tile radius", 0, 0, 0, 1);
//             AddEffect(RelicCategory.Coat, UnitRole.Swashbuckler, true, 2, 1, false,
//                 RelicEffectType.Coat_V2_CurseEmptyTile,
//                 "Curse random empty tile on enemy side, enemy can't leave and takes 10% more damage", 0, 0.10f, 0);
//             AddEffect(RelicCategory.Coat, UnitRole.Deckhand, true, 2, 1, false,
//                 RelicEffectType.Coat_V2_BuffTileDamageExchange,
//                 "Buff random tile, units take 15% damage and do 15% more damage", 0.15f, 0.15f, 0);

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
        // AddEffect(RelicCategory.Totem, UnitRole.Captain, false, 0, 1, false, RelicEffectType.Totem_V1_Captain, "Ship's Cannon Totem - Summon a cannon at a random location. Once per turn it fires at a random enemy with base weapon damage. 10 HP.", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.Captain, true, 0, 1, false, RelicEffectType.Totem_V2_Captain, "Captain's Curse Mark - Curse the enemy captain for that turn: any damage the enemy captain suffers is reflected to all other enemy allies.", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.Captain, false, 0, 1, false, RelicEffectType.Totem_V1_Captain, "Ship's Cannon Totem - Summon a cannon at a random location. Once per turn it fires at a random enemy with base weapon damage. 10 HP.", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.Captain, true, 0, 1, false, RelicEffectType.Totem_V2_Captain, "Captain's Curse Mark - Curse the enemy captain for that turn: any damage the enemy captain suffers is reflected to all other enemy allies.", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.Quartermaster, false, 0, 1, false, RelicEffectType.Totem_V1_Quartermaster, "Rally Totem - For 1-tile radius around the Quartermaster, all allies suffer no morale damage during enemy next turn.", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.Quartermaster, true, 0, 1, true, RelicEffectType.Totem_V2_Quartermaster, "Surrender Banner (Passive) - When an enemy unit surrenders or dies, all allies of that enemy lose extra morale and all player units gain +Morale Tier morale. (Amplifies the default behavior.)", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.Quartermaster, false, 0, 1, false, RelicEffectType.Totem_V1_Quartermaster, "Rally Totem - For 1-tile radius around the Quartermaster, all allies suffer no morale damage during enemy next turn.", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.Quartermaster, true, 0, 1, true, RelicEffectType.Totem_V2_Quartermaster, "Surrender Banner (Passive) - When an enemy unit surrenders or dies, all allies of that enemy lose extra morale and all player units gain +Morale Tier morale. (Amplifies the default behavior.)", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.Helmsmaster, false, 0, 1, false, RelicEffectType.Totem_V1_Helmsmaster, "Rum Effigy - Summon 2 high-quality rum bottles into your rum inventory.", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.Helmsmaster, true, 0, 0, false, RelicEffectType.Totem_V2_Helmsmaster, "Grog Converter - Convert 2 Grog Tokens into 1 Energy. Cost: 0 Energy.", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.Helmsmaster, false, 0, 1, false, RelicEffectType.Totem_V1_Helmsmaster, "Rum Effigy - Summon 2 high-quality rum bottles into your rum inventory.", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.Helmsmaster, true, 0, 0, false, RelicEffectType.Totem_V2_Helmsmaster, "Grog Converter - Convert 2 Grog Tokens into 1 Energy. Cost: 0 Energy.", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.Boatswain, false, 0, 1, false, RelicEffectType.Totem_V1_Boatswain, "Stun-on-Knockback - If this unit is knocked back during the next enemy turn, stun that target for that turn.", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.Boatswain, true, 0, 1, false, RelicEffectType.Totem_V2_Boatswain, "Anchor Totem - Summon an anchor on a nearby available tile that boosts Health stat by 25% (+Health Tier) for all nearby allies in 1 tile for 2 turns. (After 2 turns it disappears.)", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.Boatswain, false, 0, 1, false, RelicEffectType.Totem_V1_Boatswain, "Stun-on-Knockback - If this unit is knocked back during the next enemy turn, stun that target for that turn.", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.Boatswain, true, 0, 1, false, RelicEffectType.Totem_V2_Boatswain, "Anchor Totem - Summon an anchor on a nearby available tile that boosts Health stat by 25% (+Health Tier) for all nearby allies in 1 tile for 2 turns. (After 2 turns it disappears.)", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.Shipwright, false, 0, 1, false, RelicEffectType.Totem_V1_Shipwright, "Target Dummy - Summon a Target Dummy in the front row with 10 HP.", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.Shipwright, true, 0, 1, false, RelicEffectType.Totem_V2_Shipwright, "Soft-Obstacle Push - Summon a soft obstacle at target location, displacing the target to a nearby available tile. Target: tile.", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.Shipwright, false, 0, 1, false, RelicEffectType.Totem_V1_Shipwright, "Target Dummy - Summon a Target Dummy in the front row with 10 HP.", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.Shipwright, true, 0, 1, false, RelicEffectType.Totem_V2_Shipwright, "Soft-Obstacle Push - Summon a soft obstacle at target location, displacing the target to a nearby available tile. Target: tile.", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.MasterGunner, false, 0, 1, false, RelicEffectType.Totem_V1_MasterGunner, "Powder Keg Salvo - Summon 3 powder kegs at 3 random tiles on enemy side. After 2 turns each explodes in 1-tile radius dealing damage and stunning for 1 turn.", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.MasterGunner, true, 0, 1, false, RelicEffectType.Totem_V2_MasterGunner, "Ranged Curse - Cast to curse enemy ranged weapons next turn: their ranged attacks deal -5 dmg (-50%).", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.MasterGunner, false, 0, 1, false, RelicEffectType.Totem_V1_MasterGunner, "Powder Keg Salvo - Summon 3 powder kegs at 3 random tiles on enemy side. After 2 turns each explodes in 1-tile radius dealing damage and stunning for 1 turn.", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.MasterGunner, true, 0, 1, false, RelicEffectType.Totem_V2_MasterGunner, "Ranged Curse - Cast to curse enemy ranged weapons next turn: their ranged attacks deal -5 dmg (-50%).", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.MasterAtArms, false, 0, 1, false, RelicEffectType.Totem_V1_MasterAtArms, "Disable Weapons - Cast to disable enemy default weapons for next turn.", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.MasterAtArms, true, 0, 1, false, RelicEffectType.Totem_V2_MasterAtArms, "Earthquake Tiles - 3 random tiles get the Earthquake hazard. Any unit on an earthquake tile at end of round is moved to a nearby empty tile.", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.MasterAtArms, false, 0, 1, false, RelicEffectType.Totem_V1_MasterAtArms, "Disable Weapons - Cast to disable enemy default weapons for next turn.", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.MasterAtArms, true, 0, 1, false, RelicEffectType.Totem_V2_MasterAtArms, "Earthquake Tiles - 3 random tiles get the Earthquake hazard. Any unit on an earthquake tile at end of round is moved to a nearby empty tile.", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.Navigator, false, 0, 1, false, RelicEffectType.Totem_V1_Navigator, "Movement Lock - Cast to disable all enemies' movement for 1 turn (they can still attack).", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.Navigator, true, 0, 1, false, RelicEffectType.Totem_V2_Navigator, "Non-Weapon Lock - Cast to disable all enemies' non-weapon relics for 1 turn.", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.Navigator, false, 0, 1, false, RelicEffectType.Totem_V1_Navigator, "Movement Lock - Cast to disable all enemies' movement for 1 turn (they can still attack).", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.Navigator, true, 0, 1, false, RelicEffectType.Totem_V2_Navigator, "Non-Weapon Lock - Cast to disable all enemies' non-weapon relics for 1 turn.", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.Surgeon, false, 0, 1, true, RelicEffectType.Totem_V1_Surgeon, "Heal Punisher (Passive) - Stun any enemy that gets healed for 1 turn.", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.Surgeon, true, 0, 1, false, RelicEffectType.Totem_V2_Surgeon, "Random Healing Potion - Summon a Healing Potion at a random empty tile that restores 8 HP (200 HP raw) to any unit that steps on it.", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.Surgeon, false, 0, 1, true, RelicEffectType.Totem_V1_Surgeon, "Heal Punisher (Passive) - Stun any enemy that gets healed for 1 turn.", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.Surgeon, true, 0, 1, false, RelicEffectType.Totem_V2_Surgeon, "Random Healing Potion - Summon a Healing Potion at a random empty tile that restores 8 HP (200 HP raw) to any unit that steps on it.", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.Cook, false, 0, 1, true, RelicEffectType.Totem_V1_Cook, "Cook's Vigil (Passive) - Each time this unit takes damage, heal the lowest-HP ally for +Health Tier HP.", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.Cook, true, 0, 1, false, RelicEffectType.Totem_V2_Cook, "Kitchen Curse - Summon a soft obstacle on an empty enemy-side tile. While it stands, nearby enemies in 1-tile radius have Primary and Secondary stats halved (e.g., 8 -> 4).", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.Cook, false, 0, 1, true, RelicEffectType.Totem_V1_Cook, "Cook's Vigil (Passive) - Each time this unit takes damage, heal the lowest-HP ally for +Health Tier HP.", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.Cook, true, 0, 1, false, RelicEffectType.Totem_V2_Cook, "Kitchen Curse - Summon a soft obstacle on an empty enemy-side tile. While it stands, nearby enemies in 1-tile radius have Primary and Secondary stats halved (e.g., 8 -> 4).", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.Swashbuckler, false, 0, 1, false, RelicEffectType.Totem_V1_Swashbuckler, "Hidden Traps - Summon 2 invisible traps at 2 random tiles. Enemies entering a trap tile are stunned for 1 turn.", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.Swashbuckler, true, 0, 1, false, RelicEffectType.Totem_V2_Swashbuckler, "Passive Mute - Next turn, enemies cannot use any passive effects.", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.Swashbuckler, false, 0, 1, false, RelicEffectType.Totem_V1_Swashbuckler, "Hidden Traps - Summon 2 invisible traps at 2 random tiles. Enemies entering a trap tile are stunned for 1 turn.", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.Swashbuckler, true, 0, 1, false, RelicEffectType.Totem_V2_Swashbuckler, "Passive Mute - Next turn, enemies cannot use any passive effects.", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.Deckhand, false, 0, 1, false, RelicEffectType.Totem_V1_Deckhand, "Soft Wall - Create 2 soft obstacles at 2 random tiles.", 0, 0, 0);
        // AddEffect(RelicCategory.Totem, UnitRole.Deckhand, true, 0, 1, false, RelicEffectType.Totem_V2_Deckhand, "Anchor Pull - Pull all nearby enemies within 1-tile distance to be on the same row as this unit.", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.Deckhand, false, 0, 1, false, RelicEffectType.Totem_V1_Deckhand, "Soft Wall - Create 2 soft obstacles at 2 random tiles.", 0, 0, 0);
        AddEffect(RelicCategory.Totem, UnitRole.Deckhand, true, 0, 1, false, RelicEffectType.Totem_V2_Deckhand, "Anchor Pull - Pull all nearby enemies within 1-tile distance to be on the same row as this unit.", 0, 0, 0);

//             AddEffect(RelicCategory.Totem, UnitRole.Captain, false, 2, 1, false,
//                 RelicEffectType.Totem_SummonCannon,
//                 "Summon cannon, 250 HP, attacks random enemy", 250, 0, 0);
//             AddEffect(RelicCategory.Totem, UnitRole.Quartermaster, false, 2, 1, false,
//                 RelicEffectType.Totem_RallyNoMoraleDamage,
//                 "Rally: allies in 1 tile radius won't suffer morale damage next turn", 0, 0, 1, 1);
//             AddEffect(RelicCategory.Totem, UnitRole.Helmsmaster, false, 2, 1, false,
//                 RelicEffectType.Totem_SummonHighQualityRum,
//                 "Summon 2 high quality rum to inventory", 2, 0, 0);
//             AddEffect(RelicCategory.Totem, UnitRole.Boatswain, false, 2, 1, false,
//                 RelicEffectType.Totem_StunOnKnockback,
//                 "If target is knocked back next enemy turn, stun that target for that turn", 0, 0, 1);
//             AddEffect(RelicCategory.Totem, UnitRole.Shipwright, false, 2, 1, false,
//                 RelicEffectType.Totem_SummonTargetDummy,
//                 "Summon a target dummy in the front row with 250 HP", 250, 0, 0);
//             AddEffect(RelicCategory.Totem, UnitRole.MasterGunner, false, 2, 1, false,
//                 RelicEffectType.Totem_SummonExplodingBarrels,
//                 "Summon 3 barrels at random enemy tiles, explode in 1 tile radius after 2 turns dealing damage and stunning for 1 turn", 3, 0, 2);
//             AddEffect(RelicCategory.Totem, UnitRole.MasterAtArms, false, 2, 1, false,
//                 RelicEffectType.Totem_DisableEnemyWeapons,
//                 "Disable enemy default weapons for next turn", 0, 0, 1);
//             AddEffect(RelicCategory.Totem, UnitRole.Navigator, false, 2, 1, false,
//                 RelicEffectType.Totem_DisableEnemyMovement,
//                 "Disable all enemy movement for 1 turn, they can still attack", 0, 0, 1);
            AddPassive(RelicCategory.Totem, UnitRole.Surgeon, false,
                RelicEffectType.Totem_StunHealedEnemy,
                "Passive: Stun enemy that gets healed", 0, 0);
            AddPassive(RelicCategory.Totem, UnitRole.Cook, false,
                RelicEffectType.Totem_HealLowestOnDamage,
                "Passive: Each time this unit takes damage heal the lowest health ally", 0, 0);
//             AddEffect(RelicCategory.Totem, UnitRole.Swashbuckler, false, 2, 1, false,
//                 RelicEffectType.Totem_SummonInvisibleTraps,
//                 "Summon 2 invisible traps in random tiles, enemies entering are stunned 1 turn", 2, 0, 1);
//             AddEffect(RelicCategory.Totem, UnitRole.Deckhand, false, 2, 1, false,
//                 RelicEffectType.Totem_CreateSoftObstacles,
//                 "Create 2 soft obstacles in 2 random tiles", 2, 0, 0);

            // ==================== TOTEM V2 ====================
//             AddEffect(RelicCategory.Totem, UnitRole.Captain, true, 2, 1, false,
//                 RelicEffectType.Totem_CurseCaptainReflect,
//                 "Curse enemy captain this turn, damage captain suffers reflects to all other enemies", 0, 0, 1);
            AddPassive(RelicCategory.Totem, UnitRole.Quartermaster, true,
                RelicEffectType.Totem_EnemyDeathMoraleSwing,
                "Passive: When enemy surrenders or dies, enemies lose morale and all player units gain 5% morale", 0, 0.05f);
//             AddEffect(RelicCategory.Totem, UnitRole.Helmsmaster, true, 2, 0, false,
//                 RelicEffectType.Totem_ConvertGrogToEnergy,
//                 "Convert 2 Grog tokens into 1 energy", 2, 1, 0);
//             AddEffect(RelicCategory.Totem, UnitRole.Boatswain, true, 2, 1, false,
//                 RelicEffectType.Totem_SummonAnchorHealthBuff,
//                 "Summon anchor on nearby tile, +25% health stat to allies in 1 tile radius for 2 turns", 0, 0.25f, 2, 1);
//             AddEffect(RelicCategory.Totem, UnitRole.Shipwright, true, 2, 1, false,
//                 RelicEffectType.Totem_SummonObstacleDisplace,
//                 "Summon soft obstacle at target location, displace target to nearby available tile", 0, 0, 0);
//             AddEffect(RelicCategory.Totem, UnitRole.MasterGunner, true, 2, 1, false,
//                 RelicEffectType.Totem_CurseRangedWeapons,
//                 "Curse enemy ranged weapons next turn, they do 50% less damage", 0, 0.50f, 1);
//             AddEffect(RelicCategory.Totem, UnitRole.MasterAtArms, true, 2, 1, false,
//                 RelicEffectType.Totem_V2_EarthquakeHazard,
//                 "3 random tiles get earthquake hazard, units on tile at end of round are moved to nearby empty tile", 3, 0, 1);
//             AddEffect(RelicCategory.Totem, UnitRole.Navigator, true, 2, 1, false,
//                 RelicEffectType.Totem_V2_DisableNonWeaponRelics,
//                 "Disable all enemy non-weapon relics for 1 turn", 0, 0, 1);
//             AddEffect(RelicCategory.Totem, UnitRole.Surgeon, true, 2, 1, false,
//                 RelicEffectType.Totem_V2_SummonHealingPotions,
//                 "3 random healing potions 200 HP", 3, 200, 0);
//             AddEffect(RelicCategory.Totem, UnitRole.Cook, true, 2, 1, false,
//                 RelicEffectType.Totem_V2_SummonStatDebuffObstacle,
//                 "Summon soft obstacle, -50% primary+secondary to nearby enemies 1 tile radius", 0, 0.50f, 0, 1);
//             AddEffect(RelicCategory.Totem, UnitRole.Swashbuckler, true, 2, 1, false,
//                 RelicEffectType.Totem_V2_DisableEnemyPassives,
//                 "Next turn enemies can't use any passive effects", 0, 0, 1);
//             AddEffect(RelicCategory.Totem, UnitRole.Deckhand, true, 2, 1, false,
//                 RelicEffectType.Totem_V2_PullNearbyToRow,
//                 "Pull all nearby enemies in 1 tile to same row as this unit", 0, 0, 0, 1);

            // ==================== ULTIMATE V1 ====================
        // AddEffect(RelicCategory.Ultimate, UnitRole.Captain, false, 0, 3, false, RelicEffectType.Ultimate_V1_Captain, "Broadside - 3 random projectiles fall on 3 random tiles on enemy side, dealing 8 dmg each and creating a fire hazard at each location.", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.Captain, true, 0, 3, false, RelicEffectType.Ultimate_V2_Captain, "Captain's Mark - Attack the enemy Captain with the default weapon and mark it as the only valid target for that turn.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.Captain, false, 0, 3, false, RelicEffectType.Ultimate_V1_Captain, "Broadside - 3 random projectiles fall on 3 random tiles on enemy side, dealing 8 dmg each and creating a fire hazard at each location.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.Captain, true, 0, 3, false, RelicEffectType.Ultimate_V2_Captain, "Captain's Mark - Attack the enemy Captain with the default weapon and mark it as the only valid target for that turn.", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.Quartermaster, false, 0, 3, false, RelicEffectType.Ultimate_V1_Quartermaster, "Decisive Order - During enemy next turn, any morale your allies suffer is reflected on the enemies.", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.Quartermaster, true, 0, 3, false, RelicEffectType.Ultimate_V2_Quartermaster, "Last Stand Rally - Revive a dead or surrendered ally at 30% HP and morale (8 HP / 12 morale). Target: ally.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.Quartermaster, false, 0, 3, false, RelicEffectType.Ultimate_V1_Quartermaster, "Decisive Order - During enemy next turn, any morale your allies suffer is reflected on the enemies.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.Quartermaster, true, 0, 3, false, RelicEffectType.Ultimate_V2_Quartermaster, "Last Stand Rally - Revive a dead or surrendered ally at 30% HP and morale (8 HP / 12 morale). Target: ally.", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.Helmsmaster, false, 0, 3, false, RelicEffectType.Ultimate_V1_Helmsmaster, "Tide-Drunk - Attack the target with the default weapon and fill its Buzz meter completely for 2 turns.", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.Helmsmaster, true, 0, 3, false, RelicEffectType.Ultimate_V2_Helmsmaster, "Rum Bottle - Throw a rum bottle at the target location: 8 dmg in 1-tile radius and spill rum in 1-tile radius that increases Buzz for all enemies staying on the tile. Lasts 3 turns.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.Helmsmaster, false, 0, 3, false, RelicEffectType.Ultimate_V1_Helmsmaster, "Tide-Drunk - Attack the target with the default weapon and fill its Buzz meter completely for 2 turns.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.Helmsmaster, true, 0, 3, false, RelicEffectType.Ultimate_V2_Helmsmaster, "Rum Bottle - Throw a rum bottle at the target location: 8 dmg in 1-tile radius and spill rum in 1-tile radius that increases Buzz for all enemies staying on the tile. Lasts 3 turns.", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.Boatswain, false, 0, 3, false, RelicEffectType.Ultimate_V1_Boatswain, "Defensive Anchor - Summon 3 hard obstacles in the front row covering 3 column-tiles, where empty tiles are available (a piece of ship). Lasts 2 turns.", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.Boatswain, true, 0, 3, false, RelicEffectType.Ultimate_V2_Boatswain, "Skip-the-Wall - This turn, the highest-HP enemy (besides the Captain) is ignored.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.Boatswain, false, 0, 3, false, RelicEffectType.Ultimate_V1_Boatswain, "Defensive Anchor - Summon 3 hard obstacles in the front row covering 3 column-tiles, where empty tiles are available (a piece of ship). Lasts 2 turns.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.Boatswain, true, 0, 3, false, RelicEffectType.Ultimate_V2_Boatswain, "Skip-the-Wall - This turn, the highest-HP enemy (besides the Captain) is ignored.", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.Shipwright, false, 0, 3, false, RelicEffectType.Ultimate_V1_Shipwright, "Repair Knockback - Default-weapon attack and knock back the target to the last column.", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.Shipwright, true, 0, 3, false, RelicEffectType.Ultimate_V2_Shipwright, "Cleave Knockback - Default-weapon attack the target. Nearby enemies in 1-tile radius are knocked back 1 tile.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.Shipwright, false, 0, 3, false, RelicEffectType.Ultimate_V1_Shipwright, "Repair Knockback - Default-weapon attack and knock back the target to the last column.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.Shipwright, true, 0, 3, false, RelicEffectType.Ultimate_V2_Shipwright, "Cleave Knockback - Default-weapon attack the target. Nearby enemies in 1-tile radius are knocked back 1 tile.", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.MasterGunner, false, 0, 3, false, RelicEffectType.Ultimate_V1_MasterGunner, "Sniper Stun - Default-weapon attack stunning the target and all nearby enemies within 1-tile radius for the next turn.", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.MasterGunner, true, 0, 3, false, RelicEffectType.Ultimate_V2_MasterGunner, "Marksman's Strike - Default-weapon attack. +12 dmg (+300% bonus) if there are no other nearby enemies in 1-tile radius of the target.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.MasterGunner, false, 0, 3, false, RelicEffectType.Ultimate_V1_MasterGunner, "Sniper Stun - Default-weapon attack stunning the target and all nearby enemies within 1-tile radius for the next turn.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.MasterGunner, true, 0, 3, false, RelicEffectType.Ultimate_V2_MasterGunner, "Marksman's Strike - Default-weapon attack. +12 dmg (+300% bonus) if there are no other nearby enemies in 1-tile radius of the target.", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.MasterAtArms, false, 0, 3, false, RelicEffectType.Ultimate_V1_MasterAtArms, "Cleave All - Default-weapon attack on all enemies 1 time.", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.MasterAtArms, true, 0, 3, false, RelicEffectType.Ultimate_V2_MasterAtArms, "Marshal's Row Strike - Default-weapon attack the closest target and deal default weapon damage + 14 dmg (+350) to all units in the same row.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.MasterAtArms, false, 0, 3, false, RelicEffectType.Ultimate_V1_MasterAtArms, "Cleave All - Default-weapon attack on all enemies 1 time.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.MasterAtArms, true, 0, 3, false, RelicEffectType.Ultimate_V2_MasterAtArms, "Marshal's Row Strike - Default-weapon attack the closest target and deal default weapon damage + 14 dmg (+350) to all units in the same row.", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.Navigator, false, 0, 3, false, RelicEffectType.Ultimate_V1_Navigator, "Captain Mark Mirror - Mark a target for that round: any damage the target suffers is reflected on the (enemy) Captain.", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.Navigator, true, 0, 3, false, RelicEffectType.Ultimate_V2_Navigator, "Swap Distant - Swap the position of the closest enemy with the furthest enemy.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.Navigator, false, 0, 3, false, RelicEffectType.Ultimate_V1_Navigator, "Captain Mark Mirror - Mark a target for that round: any damage the target suffers is reflected on the (enemy) Captain.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.Navigator, true, 0, 3, false, RelicEffectType.Ultimate_V2_Navigator, "Swap Distant - Swap the position of the closest enemy with the furthest enemy.", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.Surgeon, false, 0, 3, false, RelicEffectType.Ultimate_V1_Surgeon, "Death Stop - Prevent a unit from dying or surrendering during the next enemy turn. Target: ally.", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.Surgeon, true, 0, 3, false, RelicEffectType.Ultimate_V2_Surgeon, "Full Restore - Fully restore the HP of any ally unit. Target: ally.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.Surgeon, false, 0, 3, false, RelicEffectType.Ultimate_V1_Surgeon, "Death Stop - Prevent a unit from dying or surrendering during the next enemy turn. Target: ally.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.Surgeon, true, 0, 3, false, RelicEffectType.Ultimate_V2_Surgeon, "Full Restore - Fully restore the HP of any ally unit. Target: ally.", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.Cook, false, 0, 3, false, RelicEffectType.Ultimate_V1_Cook, "Soul Swap - Swap this unit's current HP with the closest enemy's current HP.", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.Cook, true, 0, 3, false, RelicEffectType.Ultimate_V2_Cook, "Column Flame - Set on fire the closest target's whole column (fire hazard).", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.Cook, false, 0, 3, false, RelicEffectType.Ultimate_V1_Cook, "Soul Swap - Swap this unit's current HP with the closest enemy's current HP.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.Cook, true, 0, 3, false, RelicEffectType.Ultimate_V2_Cook, "Column Flame - Set on fire the closest target's whole column (fire hazard).", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.Swashbuckler, false, 0, 3, false, RelicEffectType.Ultimate_V1_Swashbuckler, "Forced Duel - Make the lowest-HP unit on the field and the (enemy) Captain attack each other 1 time with their default weapons.", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.Swashbuckler, true, 0, 0, true, RelicEffectType.Ultimate_V2_Swashbuckler, "Four-Weapon Strike (Passive) - If you attack the same target with 4 weapon relics in a single turn, that target instantly surrenders.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.Swashbuckler, false, 0, 3, false, RelicEffectType.Ultimate_V1_Swashbuckler, "Forced Duel - Make the lowest-HP unit on the field and the (enemy) Captain attack each other 1 time with their default weapons.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.Swashbuckler, true, 0, 0, true, RelicEffectType.Ultimate_V2_Swashbuckler, "Four-Weapon Strike (Passive) - If you attack the same target with 4 weapon relics in a single turn, that target instantly surrenders.", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.Deckhand, false, 0, 3, false, RelicEffectType.Ultimate_V1_Deckhand, "Hull Burst - Give an ally unit 3x max Hull (e.g. cap 10 -> 30) for 2 turns; restore Hull to the new max. Target: ally.", 0, 0, 0);
        // AddEffect(RelicCategory.Ultimate, UnitRole.Deckhand, true, 0, 3, false, RelicEffectType.Ultimate_V2_Deckhand, "Hazard Sweep - Clear all hazards on player side and prevent new ones from appearing.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.Deckhand, false, 0, 3, false, RelicEffectType.Ultimate_V1_Deckhand, "Hull Burst - Give an ally unit 3x max Hull (e.g. cap 10 -> 30) for 2 turns; restore Hull to the new max. Target: ally.", 0, 0, 0);
        AddEffect(RelicCategory.Ultimate, UnitRole.Deckhand, true, 0, 3, false, RelicEffectType.Ultimate_V2_Deckhand, "Hazard Sweep - Clear all hazards on player side and prevent new ones from appearing.", 0, 0, 0);

//             AddEffect(RelicCategory.Ultimate, UnitRole.Captain, false, 1, 3, false,
//                 RelicEffectType.Ultimate_ShipCannon,
//                 "Fire ship cannon: 3 shots, 200 damage + fire hazard", 200, 3, 0, 1, RelicRarity.Unique);
//             AddEffect(RelicCategory.Ultimate, UnitRole.Quartermaster, false, 1, 3, false,
//                 RelicEffectType.Ultimate_ReflectMoraleDamage,
//                 "During enemy next turn, any morale allies suffer is reflected on enemies too", 0, 0, 1, 1, RelicRarity.Unique);
//             AddEffect(RelicCategory.Ultimate, UnitRole.Helmsmaster, false, 1, 3, false,
//                 RelicEffectType.Ultimate_FullBuzzAttack,
//                 "Attack target with weapon, target buzz meter full for 2 turns", 0, 0, 2, 1, RelicRarity.Unique);
//             AddEffect(RelicCategory.Ultimate, UnitRole.Boatswain, false, 1, 3, false,
//                 RelicEffectType.Ultimate_SummonHardObstacles,
//                 "Summon 3 hard obstacles in front row empty tiles, last 2 turns", 3, 0, 2, 1, RelicRarity.Unique);
//             AddEffect(RelicCategory.Ultimate, UnitRole.Shipwright, false, 1, 3, false,
//                 RelicEffectType.Ultimate_KnockbackToLastColumn,
//                 "Attack with weapon, knock target back to last column", 0, 0, 0, 1, RelicRarity.Unique);
//             AddEffect(RelicCategory.Ultimate, UnitRole.MasterGunner, false, 1, 3, false,
//                 RelicEffectType.Ultimate_StunAoE,
//                 "Attack with weapon, stun target and all nearby enemies in 1 tile radius for next turn", 0, 0, 1, 1, RelicRarity.Unique);
//             AddEffect(RelicCategory.Ultimate, UnitRole.MasterAtArms, false, 1, 3, false,
//                 RelicEffectType.Ultimate_AttackAllEnemies,
//                 "Attack with default weapon all enemies 1 time", 0, 0, 0, 99, RelicRarity.Unique);
//             AddEffect(RelicCategory.Ultimate, UnitRole.Navigator, false, 1, 3, false,
//                 RelicEffectType.Ultimate_MarkReflectToCaptain,
//                 "Mark target this round, any damage target suffers is reflected on the captain", 0, 0, 1, 1, RelicRarity.Unique);
//             AddEffect(RelicCategory.Ultimate, UnitRole.Surgeon, false, 1, 3, false,
//                 RelicEffectType.Ultimate_PreventDeath,
//                 "Prevent unit from dying/surrendering", 0, 0, 0, 1, RelicRarity.Unique);
//             AddEffect(RelicCategory.Ultimate, UnitRole.Cook, false, 1, 3, false,
//                 RelicEffectType.Ultimate_SwapHealthClosest,
//                 "Swap this unit's health with closest enemy", 0, 0, 0, 1, RelicRarity.Unique);
//             AddEffect(RelicCategory.Ultimate, UnitRole.Swashbuckler, false, 1, 3, false,
//                 RelicEffectType.Ultimate_ForceLowestAndCaptainFight,
//                 "Lowest health unit and captain attack each other with default weapons", 0, 0, 0, 1, RelicRarity.Unique);
//             AddEffect(RelicCategory.Ultimate, UnitRole.Deckhand, false, 1, 3, false,
//                 RelicEffectType.Ultimate_MassiveHullBuff,
//                 "Give a unit 300% hull for 2 turns", 0, 3.0f, 2, 1, RelicRarity.Unique);

            // ==================== ULTIMATE V2 ====================
//             AddEffect(RelicCategory.Ultimate, UnitRole.Captain, true, 1, 3, false,
//                 RelicEffectType.Ultimate_MarkCaptainOnly,
//                 "Attack enemy captain with weapon, mark as only target this turn", 0, 0, 1, 1, RelicRarity.Unique);
//             AddEffect(RelicCategory.Ultimate, UnitRole.Quartermaster, true, 1, 3, false,
//                 RelicEffectType.Ultimate_ReviveAlly,
//                 "Revive a dead or surrendered ally with 30% health and morale", 0, 0.30f, 0, 1, RelicRarity.Unique);
//             AddEffect(RelicCategory.Ultimate, UnitRole.Helmsmaster, true, 1, 3, false,
//                 RelicEffectType.Ultimate_RumBottleAoE,
//                 "Throw rum bottle at target, 200 damage in 1 tile radius, rum spill increases buzz for 3 turns", 200, 0, 3, 1, RelicRarity.Unique);
//             AddEffect(RelicCategory.Ultimate, UnitRole.Boatswain, true, 1, 3, false,
//                 RelicEffectType.Ultimate_IgnoreHighestHP,
//                 "This turn, highest HP enemy besides captain is ignored", 0, 0, 1, 1, RelicRarity.Unique);
//             AddEffect(RelicCategory.Ultimate, UnitRole.Shipwright, true, 1, 3, false,
//                 RelicEffectType.Ultimate_AttackKnockbackNearby,
//                 "Attack with weapon, nearby enemies in 1 tile radius are knocked back 1 tile", 0, 0, 0, 1, RelicRarity.Unique);
//             AddEffect(RelicCategory.Ultimate, UnitRole.MasterGunner, true, 1, 3, false,
//                 RelicEffectType.Ultimate_MassiveSingleTarget,
//                 "Attack with weapon, +300% bonus damage if no nearby enemies in 1 tile radius", 0, 3.0f, 0, 1, RelicRarity.Unique);
//             AddEffect(RelicCategory.Ultimate, UnitRole.MasterAtArms, true, 1, 3, false,
//                 RelicEffectType.Ultimate_V2_AttackRowDamage,
//                 "Attack closest target with weapon, deal weapon damage + 350 damage to all units in same row", 350, 0, 0, 1, RelicRarity.Unique);
//             AddEffect(RelicCategory.Ultimate, UnitRole.Navigator, true, 1, 3, false,
//                 RelicEffectType.Ultimate_V2_SwapClosestFurthest,
//                 "Swap the position of the closest enemy with the furthest enemy", 0, 0, 0, 1, RelicRarity.Unique);
//             AddEffect(RelicCategory.Ultimate, UnitRole.Surgeon, true, 1, 3, false,
//                 RelicEffectType.Ultimate_V2_FullHealthRestore,
//                 "Fully restore health of any unit", 0, 1.0f, 0, 99, RelicRarity.Unique);
//             AddEffect(RelicCategory.Ultimate, UnitRole.Cook, true, 1, 3, false,
//                 RelicEffectType.Ultimate_V2_FireColumn,
//                 "Set fire to closest target's whole column", 0, 0, 0, 1, RelicRarity.Unique);
            AddPassive(RelicCategory.Ultimate, UnitRole.Swashbuckler, true,
                RelicEffectType.Ultimate_V2_SurrenderOn4Weapons,
                "Passive: Attacking same target with 4 weapons in a single turn = instant surrender", 4, 0, RelicRarity.Unique);
//             AddEffect(RelicCategory.Ultimate, UnitRole.Deckhand, true, 1, 3, false,
//                 RelicEffectType.Ultimate_V2_ClearHazardsPlayerSide,
//                 "Clear all hazards on player side and prevent new ones from appearing", 0, 0, 0, 99, RelicRarity.Unique);

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