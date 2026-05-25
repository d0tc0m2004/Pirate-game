using UnityEngine;
using UnityEditor;
using TacticalGame.AI;
using TacticalGame.Managers;

public class CleanupSceneManagers : Editor
{
    [MenuItem("Tactical Game/Clean Up Duplicate Managers")]
    public static void CleanUp()
    {
        // 1. Find the parent 'Managers' object
        GameObject managersGO = GameObject.Find("Managers");
        if (managersGO == null)
        {
            Debug.LogError("Could not find 'Managers' GameObject in scene!");
            return;
        }

        // 2. Ensure Managers has all required AI components
        if (managersGO.GetComponent<EnemyAIManager>() == null)
            managersGO.AddComponent<EnemyAIManager>();
            
        if (managersGO.GetComponent<EnemyEnergyManager>() == null)
            managersGO.AddComponent<EnemyEnergyManager>();
            
        if (managersGO.GetComponent<EnemyDeckManager>() == null)
            managersGO.AddComponent<EnemyDeckManager>();

        // 3. Find and destroy the duplicate 'EnemyAIManager' GameObject created by the old script
        GameObject aiGO = GameObject.Find("EnemyAIManager");
        if (aiGO != null && aiGO != managersGO)
        {
            DestroyImmediate(aiGO);
            Debug.Log("Destroyed duplicate 'EnemyAIManager' GameObject.");
        }

        // 4. Save the scene
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        Debug.Log("<color=green>Successfully cleaned up duplicate managers and saved the scene!</color>");
    }
}
