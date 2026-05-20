using UnityEngine;
using UnityEditor;
using TacticalGame.Equipment;

public class CombatMathMigrator
{
    [MenuItem("Tools/Davy Jones/Migrate Combat Numbers (v3)")]
    public static void MigrateNumbers()
    {
        // 1. Migrate Weapons
        string[] weaponGuids = AssetDatabase.FindAssets("t:WeaponData");
        int weaponCount = 0;
        foreach (string guid in weaponGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            WeaponData weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (weapon != null && weapon.baseDamage > 10)
            {
                weapon.baseDamage = Mathf.Max(1, Mathf.RoundToInt(weapon.baseDamage / 20f));
                EditorUtility.SetDirty(weapon);
                weaponCount++;
            }
        }
        
        // 2. Migrate Relics
        string[] relicGuids = AssetDatabase.FindAssets("t:RelicData");
        int relicCount = 0;
        foreach (string guid in relicGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RelicData relic = AssetDatabase.LoadAssetAtPath<RelicData>(path);
            if (relic != null && relic.baseValue > 10)
            {
                relic.baseValue = Mathf.Max(1, Mathf.RoundToInt(relic.baseValue / 20f));
                EditorUtility.SetDirty(relic);
                relicCount++;
            }
        }

        // 3. Migrate Role Effects (+20% -> +1 Flat, +40% -> +2 Flat)
        string[] roleEffectsGuids = AssetDatabase.FindAssets("t:RoleWeaponEffects");
        int effectsCount = 0;
        foreach (string guid in roleEffectsGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RoleWeaponEffects effects = AssetDatabase.LoadAssetAtPath<RoleWeaponEffects>(path);
            if (effects != null)
            {
                bool dirty = false;
                if (effects.effect2BonusDamage > 0f && effects.effect2BonusDamage < 1f)
                {
                    effects.effect2BonusDamage = 1f; // Flat +1
                    dirty = true;
                }
                if (effects.effect3BonusDamage > 0f && effects.effect3BonusDamage < 1f)
                {
                    effects.effect3BonusDamage = 2f; // Flat +2
                    dirty = true;
                }
                
                if (dirty)
                {
                    EditorUtility.SetDirty(effects);
                    effectsCount++;
                }
            }
        }

        // 4. Migrate GameConfig
        string[] configGuids = AssetDatabase.FindAssets("t:GameConfig");
        if (configGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(configGuids[0]);
            TacticalGame.Config.GameConfig config = AssetDatabase.LoadAssetAtPath<TacticalGame.Config.GameConfig>(path);
            var fresh = ScriptableObject.CreateInstance<TacticalGame.Config.GameConfig>();
            string json = JsonUtility.ToJson(fresh);
            JsonUtility.FromJsonOverwrite(json, config);
            EditorUtility.SetDirty(config);
            Debug.Log("Reset GameConfig.asset to v3 default ranges.");
        }

        // 5. Migrate Unit Prefabs
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        int unitCount = 0;
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                var unitStatus = prefab.GetComponent<TacticalGame.Units.UnitStatus>();
                if (unitStatus != null)
                {
                    SerializedObject so = new SerializedObject(unitStatus);
                    bool dirty = false;
                    
                    var maxHPProp = so.FindProperty("maxHP");
                    if (maxHPProp != null && maxHPProp.intValue > 50) { maxHPProp.intValue = Mathf.Max(1, Mathf.RoundToInt(maxHPProp.intValue / 20f)); dirty = true; }
                    
                    var currentHPProp = so.FindProperty("currentHP");
                    if (currentHPProp != null && maxHPProp != null && currentHPProp.intValue > 50) { currentHPProp.intValue = maxHPProp.intValue; dirty = true; }
                    
                    var maxMoraleProp = so.FindProperty("maxMorale");
                    if (maxMoraleProp != null && maxMoraleProp.intValue > 100) { maxMoraleProp.intValue = Mathf.Max(1, Mathf.RoundToInt(maxMoraleProp.intValue / 20f)); dirty = true; }
                    
                    var currentMoraleProp = so.FindProperty("currentMorale");
                    if (currentMoraleProp != null && maxMoraleProp != null && currentMoraleProp.intValue > 100) { currentMoraleProp.intValue = maxMoraleProp.intValue; dirty = true; }
                    
                    var powerProp = so.FindProperty("power");
                    if (powerProp != null && powerProp.intValue > 10) { powerProp.intValue = Mathf.Max(1, Mathf.RoundToInt(powerProp.intValue / 5f)); dirty = true; }
                    
                    var aimProp = so.FindProperty("aim");
                    if (aimProp != null && aimProp.intValue > 10) { aimProp.intValue = Mathf.Max(1, Mathf.RoundToInt(aimProp.intValue / 5f)); dirty = true; }
                    
                    var tacticsProp = so.FindProperty("tactics");
                    if (tacticsProp != null && tacticsProp.intValue > 10) { tacticsProp.intValue = Mathf.Max(1, Mathf.RoundToInt(tacticsProp.intValue / 5f)); dirty = true; }
                    
                    var skillProp = so.FindProperty("skill");
                    if (skillProp != null && skillProp.intValue > 10) { skillProp.intValue = Mathf.Max(1, Mathf.RoundToInt(skillProp.intValue / 5f)); dirty = true; }
                    
                    var proficiencyProp = so.FindProperty("proficiency");
                    if (proficiencyProp != null && proficiencyProp.intValue > 10) { proficiencyProp.intValue = Mathf.Max(1, Mathf.RoundToInt(proficiencyProp.intValue / 5f)); dirty = true; }
                    
                    var gritProp = so.FindProperty("grit");
                    if (gritProp != null && gritProp.intValue > 10) { gritProp.intValue = Mathf.Max(1, Mathf.RoundToInt(gritProp.intValue / 5f)); dirty = true; }
                    
                    var speedProp = so.FindProperty("speed");
                    if (speedProp != null && speedProp.intValue > 10) { speedProp.intValue = Mathf.Max(1, Mathf.RoundToInt(speedProp.intValue / 5f)); dirty = true; }
                    
                    var maxHullProp = so.FindProperty("maxHullPool");
                    if (maxHullProp != null && maxHullProp.intValue > 20) { maxHullProp.intValue = Mathf.Max(0, Mathf.RoundToInt(maxHullProp.intValue / 20f)); dirty = true; }
                    
                    var currentHullProp = so.FindProperty("currentHullPool");
                    if (currentHullProp != null && maxHullProp != null && currentHullProp.intValue > 20) { currentHullProp.intValue = maxHullProp.intValue; dirty = true; }
                    
                    if (dirty)
                    {
                        so.ApplyModifiedProperties();
                        unitCount++;
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"<color=green>Migration Complete! Updated {weaponCount} Weapons, {relicCount} Relics, {effectsCount} Role Effects, and {unitCount} Prefabs to v3 numbers.</color>");
    }
}
