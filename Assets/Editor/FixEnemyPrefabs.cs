using UnityEngine;
using UnityEditor;
using TacticalGame.AI;
using TacticalGame.UI;

public class FixEnemyPrefabs : Editor
{
    [MenuItem("Tactical Game/Fix Enemy Prefabs")]
    public static void FixPrefabs()
    {
        string prefabsPath = "Assets/Prefabs/Enemy_prefabs";
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabsPath });
        
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            
            // Remove missing scripts which were corrupted by AddComponent
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefab);
            
            // Correctly add components
            if (prefab.GetComponent<EnemyBrain>() == null)
            {
                prefab.AddComponent<EnemyBrain>();
            }
            if (prefab.GetComponent<IntentWarningUI>() == null)
            {
                prefab.AddComponent<IntentWarningUI>();
            }
            
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
            PrefabUtility.UnloadPrefabContents(prefab);
            
            Debug.Log($"Fixed {path}, removed {removed} corrupted scripts.");
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log("Finished fixing enemy prefabs!");
    }
}
