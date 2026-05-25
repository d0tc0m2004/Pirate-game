using UnityEngine;
using UnityEditor;
using TacticalGame.AI;
using TacticalGame.UI;
using TacticalGame.Managers;
using System.IO;

public class EnemyAISetup : Editor
{
    [MenuItem("Tactical Game/Setup Enemy AI Prefabs and Scene")]
    public static void SetupEnemyAI()
    {
        // 1. Fix Enemy Prefabs
        string prefabsPath = "Assets/Prefabs/Enemy_prefabs";
        if (Directory.Exists(prefabsPath))
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabsPath });
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                bool changed = false;
                if (prefab.GetComponent<EnemyBrain>() == null)
                {
                    prefab.AddComponent<EnemyBrain>();
                    changed = true;
                }
                if (prefab.GetComponent<IntentWarningUI>() == null)
                {
                    prefab.AddComponent<IntentWarningUI>();
                    changed = true;
                }
                
                if (changed)
                {
                    EditorUtility.SetDirty(prefab);
                    Debug.Log($"[AI Setup] Added AI scripts to {prefab.name}");
                }
            }
            AssetDatabase.SaveAssets();
        }

        // 2. Fix Scene Managers
        var aiManager = FindAnyObjectByType<EnemyAIManager>();
        if (aiManager == null)
        {
            GameObject managersGO = GameObject.Find("Managers");
            if (managersGO == null) managersGO = new GameObject("Managers");

            GameObject aiGO = new GameObject("EnemyAIManager");
            aiGO.transform.SetParent(managersGO.transform);

            aiGO.AddComponent<EnemyAIManager>();
            aiGO.AddComponent<EnemyDeckManager>();
            aiGO.AddComponent<EnemyEnergyManager>();
            
            Debug.Log("[AI Setup] Created EnemyAIManager, EnemyDeckManager, and EnemyEnergyManager in the scene.");
        }
        else
        {
            Debug.Log("[AI Setup] EnemyAIManager already exists in the scene.");
        }
        
        Debug.Log("Enemy AI Setup Complete! You can now hit Play.");
    }
}
