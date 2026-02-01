using UnityEngine;
using System.IO;
using System.Text;

/// <summary>
/// Captures all Unity console logs to a text file.
/// Add this to any GameObject in your scene (or it auto-creates itself).
/// Logs are saved to: [Project]/Logs/console_log.txt
/// </summary>
public class LogCapture : MonoBehaviour
{
    private static LogCapture _instance;
    private StringBuilder logBuilder = new StringBuilder();
    private string logFilePath;
    private bool isCapturing = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        if (_instance == null)
        {
            var go = new GameObject("LogCapture");
            _instance = go.AddComponent<LogCapture>();
            DontDestroyOnLoad(go);
        }
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Create Logs folder in project root
        string logsFolder = Path.Combine(Application.dataPath, "..", "Logs");
        Directory.CreateDirectory(logsFolder);
        logFilePath = Path.Combine(logsFolder, "console_log.txt");

        // Clear previous log
        File.WriteAllText(logFilePath, $"=== Log Capture Started: {System.DateTime.Now} ===\n\n");

        // Subscribe to log events
        Application.logMessageReceived += HandleLog;
        isCapturing = true;

        Debug.Log($"LogCapture: Saving logs to {logFilePath}");
    }

    void OnDestroy()
    {
        if (isCapturing)
        {
            Application.logMessageReceived -= HandleLog;
            SaveLog();
        }
    }

    void OnApplicationQuit()
    {
        SaveLog();
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Strip rich text color tags for readability
        string cleanLog = System.Text.RegularExpressions.Regex.Replace(logString, "<.*?>", "");

        string prefix = type switch
        {
            LogType.Error => "[ERROR]",
            LogType.Warning => "[WARN]",
            LogType.Exception => "[EXCEPTION]",
            _ => "[LOG]"
        };

        logBuilder.AppendLine($"{prefix} {cleanLog}");

        // For errors/exceptions, include stack trace
        if (type == LogType.Error || type == LogType.Exception)
        {
            logBuilder.AppendLine($"  Stack: {stackTrace}");
        }

        // Auto-save every 50 messages to avoid losing logs on crash
        if (logBuilder.Length > 5000)
        {
            SaveLog();
        }
    }

    void SaveLog()
    {
        if (logBuilder.Length > 0)
        {
            File.AppendAllText(logFilePath, logBuilder.ToString());
            logBuilder.Clear();
        }
    }

    // Manual save button (call from inspector or code)
    [ContextMenu("Save Log Now")]
    public void ForceSave()
    {
        SaveLog();
        Debug.Log($"Log saved to: {logFilePath}");
    }
}
