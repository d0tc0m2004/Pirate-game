using UnityEngine;
using UnityEditor;

public class GlobalMissingScriptCleaner : Editor
{
    [MenuItem("Tactical Game/Clean ALL Missing Scripts")]
    public static void CleanAllMissingScripts()
    {
        int totalRemoved = 0;

        // Clean all prefabs
        string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in allPrefabs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            
            int removed = CleanGameObject(prefab);
            
            if (removed > 0)
            {
                totalRemoved += removed;
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                Debug.Log($"Removed {removed} missing scripts from prefab: {path}");
            }
            
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        // Clean current scene
        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (removed > 0)
            {
                totalRemoved += removed;
                Debug.Log($"Removed {removed} missing scripts from scene object: {go.name}");
            }
        }

        AssetDatabase.SaveAssets(); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log($"<color=green>Finished cleaning! Total missing scripts removed globally: {totalRemoved}</color>");
    }

    private static int CleanGameObject(GameObject go)
    {
        int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        foreach (Transform child in go.transform)
        {
            removed += CleanGameObject(child.gameObject);
        }
        return removed;
    }
}
