using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Streamer 模式啟動點：在主場景（Mainmenu）載入完成後執行，
/// 若 StreamerMode.IsActive 則建立常駐 GameObject，掛上自動加房 + 鏡頭排程器 + Overlay。
/// 不影響原本的主選單流程（NetworkManager / MenuUIManager 仍存在但不會被觸發）。
/// </summary>
public static class StreamerBootstrap
{
    private static GameObject _root;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (!StreamerMode.IsActive)
        {
            Debug.Log("[Streamer] Inactive — skipping bootstrap.");
            return;
        }
        if (_root != null) return;

        Debug.Log("[Streamer] Bootstrap activating...");

        // 確保視窗化以利 OBS 擷取，並維持背景執行
        Application.runInBackground = true;

        _root = new GameObject("[Streamer]");
        Object.DontDestroyOnLoad(_root);

        _root.AddComponent<StreamerAutoJoin>();
        _root.AddComponent<StreamerCameraDirector>();
        _root.AddComponent<StreamerOverlay>();

        // 第一次隱藏 + 之後每次 scene load 都重新隱藏（host 斷線回 Mainmenu 時很重要）
        TryHideMainMenu();
        SceneManager.sceneLoaded += OnSceneLoaded;

        Debug.Log("[Streamer] Bootstrap ready.");
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 給場景上其他物件 Awake/Start 跑完一幀後再隱藏（避免漏抓延遲生成的 Canvas）
        TryHideMainMenu();
    }

    static void TryHideMainMenu()
    {
        if (_root == null) return;

        // 不刪除 MenuUIManager（其他系統可能依賴 .instance），只把場景上的 Canvas 物件關掉。
        var menus = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in menus)
        {
            if (c == null) continue;
            // 跳過 streamer 自己的 canvas（在 _root 底下）
            if (c.transform.IsChildOf(_root.transform)) continue;
            c.gameObject.SetActive(false);
        }
    }
}
