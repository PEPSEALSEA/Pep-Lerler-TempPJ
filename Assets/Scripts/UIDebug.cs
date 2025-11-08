using UnityEngine;

public static class UIDebug
{
    public static bool Enabled = true;

    public static void Log(object sender, string message)
    {
        if (!Enabled) return;
        Debug.Log($"[UI][{sender}] {message}");
    }

    public static void Warn(object sender, string message)
    {
        if (!Enabled) return;
        Debug.LogWarning($"[UI][{sender}] {message}");
    }

    public static void Error(object sender, string message)
    {
        if (!Enabled) return;
        Debug.LogError($"[UI][{sender}] {message}");
    }
}

