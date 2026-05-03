using UnityEngine;

/// <summary>
/// 判斷目前是否處於 streamer（直播觀察者）模式。
/// 啟用方式：
///   1. 命令列 -streamer 或 --streamer（正式 build）
///   2. 編輯器選單 "Streamer/Toggle Editor Streamer Mode"（Editor 測試用）
/// </summary>
public static class StreamerMode
{
    private const string EditorPrefKey = "SpyParty_StreamerMode";

    /// <summary>是否啟用 streamer 模式（Bootstrap 在 Awake 階段呼叫此判斷）</summary>
    public static bool IsActive
    {
        get
        {
            // 命令列參數
            foreach (var a in System.Environment.GetCommandLineArgs())
            {
                if (a == "-streamer" || a == "--streamer") return true;
            }
            // 編輯器測試開關
            if (Application.isEditor && PlayerPrefs.GetInt(EditorPrefKey, 0) == 1) return true;

            return false;
        }
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Streamer/Toggle Editor Streamer Mode")]
    static void ToggleEditorMode()
    {
        int v = PlayerPrefs.GetInt(EditorPrefKey, 0);
        int n = (v == 0) ? 1 : 0;
        PlayerPrefs.SetInt(EditorPrefKey, n);
        PlayerPrefs.Save();
        Debug.Log($"[Streamer] Editor mode: {(n == 1 ? "ON (next Play)" : "OFF")}");
    }

    [UnityEditor.MenuItem("Streamer/Show Editor Streamer Mode Status")]
    static void ShowStatus()
    {
        int v = PlayerPrefs.GetInt(EditorPrefKey, 0);
        Debug.Log($"[Streamer] Editor mode is currently: {(v == 1 ? "ON" : "OFF")}");
    }
#endif
}
