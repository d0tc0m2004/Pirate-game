#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TacticalGame.Equipment;
using TacticalGame.Enums;
using System.Collections.Generic;

namespace TacticalGame.Editor
{
    /// <summary>
    /// Editor script to generate all RoleWeaponEffects ScriptableObjects.
    /// Run from menu: Tools -> Tactical -> Generate All Role Effects
    /// </summary>
    public class RoleEffectsGenerator : EditorWindow
    {
        [MenuItem("Tools/Tactical/Generate All Role Effects")]
        public static void GenerateAllRoleEffects()
        {
            CreateFolderIfNeeded("Assets/Resources");
            CreateFolderIfNeeded("Assets/Resources/RoleEffects");

            // Generate effects for each role
            GenerateCaptainEffects();
            GenerateQuartermasterEffects();
            GenerateHelmsmasterEffects();
            GenerateBoatswainEffects();
            GenerateShipwrightEffects();
            GenerateMasterGunnerEffects();
            GenerateMasterAtArmsEffects();
            GenerateNavigatorEffects();
            GenerateSurgeonEffects();
            GenerateCookEffects();
            GenerateSwashbucklerEffects();
            GenerateDeckhandEffects();

            // Create database
            CreateRoleEffectsDatabase();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>✓ All 12 role effects generated!</color>");
            EditorUtility.DisplayDialog("Role Effects Generated",
                "All role weapon effects have been created.\n" +
                "RoleEffectsDatabase created in Assets/Resources/", "OK");
        }

        static void CreateFolderIfNeeded(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path);
                string folder = System.IO.Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        static RoleWeaponEffects CreateRoleEffect(UnitRole role, string roleName,
            string e1Name, string e1Desc, WeaponRelicEffectType e1Type, float e1v1, float e1v2, int e1Dur,
            string e2Name, string e2Desc, WeaponRelicEffectType e2Type, float e2v1, float e2v2, int e2Dur,
            string e3Name, string e3Desc, WeaponRelicEffectType e3Type, float e3v1, float e3v2, int e3Dur)
        {
            RoleWeaponEffects effects = ScriptableObject.CreateInstance<RoleWeaponEffects>();
            effects.role = role;

            // Effect 1 - Common
            effects.effect1Name = e1Name;
            effects.effect1Description = e1Desc;
            effects.effect1Type = e1Type;
            effects.effect1Value1 = e1v1;
            effects.effect1Value2 = e1v2;
            effects.effect1Duration = e1Dur;

            // Effect 2 - Uncommon (+20% damage)
            effects.effect2Name = e2Name;
            effects.effect2Description = e2Desc;
            effects.effect2Type = e2Type;
            effects.effect2Value1 = e2v1;
            effects.effect2Value2 = e2v2;
            effects.effect2Duration = e2Dur;
            effects.effect2BonusDamage = 0.20f;

            // Effect 3 - Rare (+40% damage)
            effects.effect3Name = e3Name;
            effects.effect3Description = e3Desc;
            effects.effect3Type = e3Type;
            effects.effect3Value1 = e3v1;
            effects.effect3Value2 = e3v2;
            effects.effect3Duration = e3Dur;
            effects.effect3BonusDamage = 0.40f;

            string path = $"Assets/Resources/RoleEffects/RoleEffects_{roleName}.asset";
            AssetDatabase.CreateAsset(effects, path);
            return effects;
        }

        // === ROLE EFFECT DEFINITIONS ===

        static void GenerateCaptainEffects()
        {
            CreateRoleEffect(UnitRole.Captain, "Captain",
                // Effect 1 - Common
                "Energy Recovery", "On-hit: If it kills or surrenders an enemy, restore 1 energy",
                WeaponRelicEffectType.RestoreEnergyOnKill, 1f, 0f, 0,
                // Effect 2 - Uncommon
                "Unspent Power", "On-hit: Deal 20% extra damage for every 1 unspent energy this turn",
                WeaponRelicEffectType.BonusDamagePerUnspentEnergy, 0.20f, 0f, 0,
                // Effect 3 - Rare
                "Mark & Strike", "On-hit: Marks target for that round, if another unit hits it restores 1 energy",
                WeaponRelicEffectType.MarkTargetRestoreEnergy, 1f, 0f, 1
            );
        }

        static void GenerateQuartermasterEffects()
        {
            CreateRoleEffect(UnitRole.Quartermaster, "Quartermaster",
                "Morale Theft", "On-hit: Steals 50 morale",
                WeaponRelicEffectType.StealMorale, 50f, 0f, 0,
                "Morale Pressure", "On-hit: Does 20% extra damage if the target has lower current morale than this unit",
                WeaponRelicEffectType.BonusDamageIfLowerMorale, 0.20f, 0f, 0,
                "Rally Cry", "On-hit: Restores morale to all allied units based on 20% from this unit's current morale",
                WeaponRelicEffectType.RestoreMoraleToAllies, 0.20f, 0f, 0
            );
        }

        static void GenerateHelmsmasterEffects()
        {
            CreateRoleEffect(UnitRole.Helmsmaster, "Helmsmaster",
                "Buzz Strike", "On-hit: Increase enemy Buzz meter by 25%",
                WeaponRelicEffectType.IncreaseBuzzMeter, 0.25f, 0f, 0,
                "Drunk Target", "On-hit: Does bonus damage based on enemy buzz state (more buzz = more damage)",
                WeaponRelicEffectType.BonusDamageByBuzzState, 0.02f, 0f, 0,
                "Disorienting Blow", "On-hit: Applies a debuff for 2 turns causing 50% chance for attacks to miss",
                WeaponRelicEffectType.ApplyMissDebuff, 0.50f, 0f, 2
            );
        }

        static void GenerateBoatswainEffects()
        {
            CreateRoleEffect(UnitRole.Boatswain, "Boatswain",
                "Life Steal", "On-hit: Steal 10% health",
                WeaponRelicEffectType.StealHealth, 0.10f, 0f, 0,
                "Close Combat", "On-hit: Does increased damage based on how close the target is",
                WeaponRelicEffectType.BonusDamageByProximity, 0.05f, 0f, 0,
                "Heavy Strike", "On-hit: Does increased damage based on 20% from total health",
                WeaponRelicEffectType.DamageBasedOnHealthPercent, 0.20f, 0f, 0
            );
        }

        static void GenerateShipwrightEffects()
        {
            CreateRoleEffect(UnitRole.Shipwright, "Shipwright",
                "Armor Break", "On-hit: Reduce enemy Grit on hit",
                WeaponRelicEffectType.ReduceEnemyGrit, 10f, 0f, 2,
                "Desperate Strength", "On-hit: Increase damage based on missing health from this unit (lower health = higher damage)",
                WeaponRelicEffectType.BonusDamageByMissingHealth, 0.01f, 0f, 0,
                "Fortified Strike", "On-hit: Gets 50% of unit grit for 2 turns and does damage based on gained Grit amount",
                WeaponRelicEffectType.GainGritDealBonusDamage, 0.50f, 0f, 2
            );
        }

        static void GenerateMasterGunnerEffects()
        {
            CreateRoleEffect(UnitRole.MasterGunner, "MasterGunner",
                "Killing Spree", "On-hit: If enemy dies gain permanent 10% Primary Stat (for that battle)",
                WeaponRelicEffectType.GainPrimaryStatOnKill, 0.10f, 0f, 0,
                "Second Chance", "On-hit: If unit dies from the attack you can re-use the ability again",
                WeaponRelicEffectType.ReuseAbilityOnKill, 0f, 0f, 0,
                "Stowed Power", "On-hit: Does 20% increased damage for each time this weapon was stowed",
                WeaponRelicEffectType.BonusDamagePerStowedWeapon, 0.20f, 0f, 0
            );
        }

        static void GenerateMasterAtArmsEffects()
        {
            CreateRoleEffect(UnitRole.MasterAtArms, "MasterAtArms",
                "Efficient Combat", "On-hit: Reduce other weapon relic cost by 1",
                WeaponRelicEffectType.ReduceWeaponRelicCost, 1f, 0f, 1,
                "Static Assault", "On-hit: Does 20% more damage if the unit didn't move in the past turn",
                WeaponRelicEffectType.BonusDamageIfNotMoved, 0.20f, 0f, 0,
                "Execute", "On-hit: Executes targets below 15% Health",
                WeaponRelicEffectType.ExecuteLowHealth, 0.15f, 0f, 0
            );
        }

        static void GenerateNavigatorEffects()
        {
            CreateRoleEffect(UnitRole.Navigator, "Navigator",
                "Line Shot", "On-hit: Does 20% bonus damage to target on the same row",
                WeaponRelicEffectType.BonusDamageSameRow, 0.20f, 0f, 0,
                "Punish Movement", "On-hit: Does 40% more damage if the target moved in the previous turn",
                WeaponRelicEffectType.BonusDamageIfTargetMoved, 0.40f, 0f, 0,
                "Hazard Creation", "On-hit: Creates a random hazard at target location",
                WeaponRelicEffectType.CreateHazardAtTarget, 0f, 0f, 0
            );
        }

        static void GenerateSurgeonEffects()
        {
            CreateRoleEffect(UnitRole.Surgeon, "Surgeon",
                "Combat Medic", "On-hit: Restores 150 health to closest ally unit",
                WeaponRelicEffectType.HealClosestAlly, 150f, 0f, 0,
                "Wound", "On-hit: Applies a debuff - for 2 turns the target can't restore health or morale",
                WeaponRelicEffectType.ApplyHealBlock, 0f, 0f, 2,
                "Team Synergy", "On-hit: Does 20% increased damage for each allied unit in 1 tile radius",
                WeaponRelicEffectType.BonusDamagePerAllyInRadius, 0.20f, 1f, 0
            );
        }

        static void GenerateCookEffects()
        {
            CreateRoleEffect(UnitRole.Cook, "Cook",
                "Burning Strike", "On-hit: Sets target on fire for 4 turns",
                WeaponRelicEffectType.ApplyFireDebuff, 0f, 0f, 4,
                "Debuff Mastery", "On-hit: Does 10% bonus damage for each unique debuff on the target",
                WeaponRelicEffectType.BonusDamagePerDebuff, 0.10f, 0f, 0,
                "Toxic Strike", "On-hit: Applies a debuff, if the target moves or is moved in the next 2 turns it will take 30% of current health damage",
                WeaponRelicEffectType.ApplyDebuffWithHealthDamage, 0.30f, 0f, 2
            );
        }

        static void GenerateSwashbucklerEffects()
        {
            CreateRoleEffect(UnitRole.Swashbuckler, "Swashbuckler",
                "Quick Strike", "On-hit: If the weapon attacks first in a turn its cost is 0",
                WeaponRelicEffectType.FreeCostIfFirst, 0f, 0f, 0,
                "Speed Advantage", "On-hit: Does 20% bonus damage if the target has lower speed stat",
                WeaponRelicEffectType.BonusDamageIfLowerSpeed, 0.20f, 0f, 0,
                "First Blood", "On-hit: If the unit attacks first in a turn it does 50% increased damage",
                WeaponRelicEffectType.BonusDamageIfFirstAttack, 0.50f, 0f, 0
            );
        }

        static void GenerateDeckhandEffects()
        {
            CreateRoleEffect(UnitRole.Deckhand, "Deckhand",
                "Hull Repair", "On-hit: Restore 30 Hull",
                WeaponRelicEffectType.RestoreHull, 30f, 0f, 0,
                "Hull Breaker", "On-hit: If the attack destroys enemy hull shield next turn the enemy has 1 less energy",
                WeaponRelicEffectType.ReduceEnemyEnergyOnHullBreak, 1f, 0f, 1,
                "Exposed Target", "On-hit: Does bonus 40% increased damage to targets with no hull shield",
                WeaponRelicEffectType.BonusDamageNoHull, 0.40f, 0f, 0
            );
        }

        static void CreateRoleEffectsDatabase()
        {
            RoleEffectsDatabase database = AssetDatabase.LoadAssetAtPath<RoleEffectsDatabase>("Assets/Resources/RoleEffectsDatabase.asset");

            if (database == null)
            {
                database = ScriptableObject.CreateInstance<RoleEffectsDatabase>();
                AssetDatabase.CreateAsset(database, "Assets/Resources/RoleEffectsDatabase.asset");
            }

            database.allRoleEffects.Clear();

            string[] guids = AssetDatabase.FindAssets("t:RoleWeaponEffects", new[] { "Assets/Resources/RoleEffects" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RoleWeaponEffects effects = AssetDatabase.LoadAssetAtPath<RoleWeaponEffects>(path);
                if (effects != null)
                {
                    database.allRoleEffects.Add(effects);
                }
            }

            EditorUtility.SetDirty(database);
            Debug.Log($"RoleEffectsDatabase populated with {database.allRoleEffects.Count} role effects");
        }
    }
}
#endif