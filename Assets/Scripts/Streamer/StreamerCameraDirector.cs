using UnityEngine;
using UnityEngine.SceneManagement;
using OodlesEngine;

/// <summary>
/// Streamer 鏡頭排程器：
///   - 確保場景有 SpectatorCamera（沒有就自己生一台 Camera + AudioListener + SpectatorCamera）
///   - 偵測場景進入遊戲後，每 switchInterval 秒切換到下一個玩家
///   - 跳過自己被 host spawn 出來的角色（透過 NetworkObject.HasInputAuthority 判斷）
///   - 隱藏自己角色的 Renderer（避免畫面入鏡）
/// </summary>
public class StreamerCameraDirector : MonoBehaviour
{
    [Header("切換設定")]
    [Tooltip("每幾秒切換一個玩家")]
    [SerializeField] private float switchInterval = 20f;
    [Tooltip("剛進入遊戲場景後等多久才開始切（讓場景穩定）")]
    [SerializeField] private float startDelay = 2f;
    [Tooltip("玩家清單刷新頻率")]
    [SerializeField] private float listRefreshInterval = 1.5f;

    [Header("視角（簡單第三人稱跟隨）")]
    [Tooltip("瞄準點相對玩家身體的偏移（朝這點 LookAt，預設頭部高度）")]
    [SerializeField] private Vector3 cameraPivotOffset = new Vector3(0f, 1.5f, 0f);
    [Tooltip("相機相對瞄準點的偏移：負 Z = 後方，正 Y = 上方。預設 (0, 4, -8)")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 4f, -8f);
    [Tooltip("位置平滑速度（越大越貼）")]
    [SerializeField] private float cameraSmooth = 6f;
    [Tooltip("水平角(yaw)平滑速度。越小 = 鏡頭轉向越鈍、越穩定。串流建議 2~3")]
    [SerializeField] private float cameraYawSmooth = 2.5f;
    [Tooltip("玩家轉動小於此角度（度）就忽略，過濾 idle 動畫微抖。1~3 度合理")]
    [SerializeField] private float cameraYawDeadzone = 1.5f;

    // 公開給 Overlay 讀
    public string CurrentTargetName { get; private set; } = "—";
    public float TimeUntilSwitch    { get; private set; } = 0f;
    public bool  IsCycling          { get; private set; } = false;
    public int   CurrentIndex       { get; private set; } = -1;
    public int   PlayerCount        { get; private set; } = 0;

    private SpectatorCamera _spec;
    private float _nextSwitchTime = 0f;
    private float _nextRefreshTime = 0f;
    private float _readyTime = 0f;
    private bool  _started = false;
    private string _lastSceneName = "";

    void Start()
    {
        _readyTime = Time.time + startDelay;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 切場景重置狀態，讓 Director 在新場景重新等 startDelay 後再開始切
        _started = false;
        _readyTime = Time.time + startDelay;
        CurrentTargetName = "—";
        IsCycling = false;
        _lastSceneName = scene.name;
    }

    void Update()
    {
        EnsureSpectatorCamera();
        HideOwnCharacterRenderers();

        if (_spec == null) return;

        // 定期刷新玩家清單（過濾掉自己）
        if (Time.time >= _nextRefreshTime)
        {
            _spec.RefreshPlayerList();
            _nextRefreshTime = Time.time + listRefreshInterval;
        }

        int total = _spec.PlayerCount;
        int valid = CountValidTargets();
        PlayerCount = valid;

        if (Time.time < _readyTime || total == 0 || valid == 0)
        {
            CurrentTargetName = (total == 0) ? "Waiting for players..." : "—";
            IsCycling = false;
            TimeUntilSwitch = 0f;
            return;
        }

        // 第一次進入有效狀態：找第一個非自己的玩家開始
        if (!_started)
        {
            int first = FindNextValidIndex(-1);
            if (first < 0) return;
            _spec.SetFollowTarget(first);
            CurrentIndex = first;
            UpdateNameDisplay();
            _nextSwitchTime = Time.time + switchInterval;
            _started = true;
            IsCycling = true;
        }

        TimeUntilSwitch = Mathf.Max(0f, _nextSwitchTime - Time.time);

        // 時間到，切下一個（且驗證當前 target 沒消失）
        if (Time.time >= _nextSwitchTime || !IsCurrentTargetValid())
        {
            int next = FindNextValidIndex(CurrentIndex);
            if (next >= 0)
            {
                _spec.SetFollowTarget(next);
                CurrentIndex = next;
                UpdateNameDisplay();
            }
            _nextSwitchTime = Time.time + switchInterval;
        }
    }

    // ============================================================

    /// <summary>確保場景中有 SpectatorCamera 可用，沒有就建一台</summary>
    void EnsureSpectatorCamera()
    {
        if (_spec != null) return;
        if (SpectatorCamera.Instance != null)
        {
            _spec = SpectatorCamera.Instance;
            ApplyCameraSettings();
            return;
        }

        // 自己生一台
        var go = new GameObject("[StreamerCamera]");
        go.transform.SetParent(transform, worldPositionStays: false);
        var cam = go.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.depth = 100; // 高於遊戲內主相機
        go.AddComponent<AudioListener>();

        // 場景上若還有其他 AudioListener，關掉以避免警告
        var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        foreach (var l in listeners)
        {
            if (l == null) continue;
            if (l.gameObject != go) l.enabled = false;
        }

        _spec = go.AddComponent<SpectatorCamera>();
        ApplyCameraSettings();
        Debug.Log("[Streamer] Spawned local SpectatorCamera (auto-track ON).");
    }

    /// <summary>把 auto-track 設定推給 SpectatorCamera</summary>
    void ApplyCameraSettings()
    {
        if (_spec == null) return;
        _spec.autoTrackTarget        = true;
        _spec.autoTrackPivotOffset   = cameraPivotOffset;
        _spec.autoTrackCameraOffset  = cameraOffset;
        _spec.autoTrackSmooth        = cameraSmooth;
        _spec.autoTrackYawSmooth     = cameraYawSmooth;
        _spec.autoTrackYawDeadzone   = cameraYawDeadzone;
    }

    /// <summary>把自己 host 端被 spawn 出來的角色 renderer 全關，避免入鏡</summary>
    void HideOwnCharacterRenderers()
    {
        var locals = FindObjectsByType<LocalPlayer>(FindObjectsSortMode.None);
        foreach (var lp in locals)
        {
            if (lp == null) continue;
            // LocalPlayer 代表「本機輸入權的玩家」
            foreach (var r in lp.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || !r.enabled) continue;
                r.enabled = false;
            }
        }
    }

    /// <summary>數一下有幾個非自己的玩家可以跟隨</summary>
    int CountValidTargets()
    {
        if (_spec == null) return 0;
        int n = 0;
        for (int i = 0; i < _spec.PlayerCount; i++)
        {
            if (IsValidTarget(_spec.GetPlayer(i))) n++;
        }
        return n;
    }

    bool IsValidTarget(PlayerIdentify p)
    {
        if (p == null) return false;
        var no = p.GetComponent<Fusion.NetworkObject>();
        if (no == null) return true; // 沒網路標識就視為他人
        return !no.HasInputAuthority; // 自己（streamer client）持 input authority → 跳過
    }

    /// <summary>從 currentIdx+1 起找下一個非自己的索引，找不到回 -1</summary>
    int FindNextValidIndex(int currentIdx)
    {
        if (_spec == null) return -1;
        int total = _spec.PlayerCount;
        if (total == 0) return -1;

        for (int step = 1; step <= total; step++)
        {
            int idx = ((currentIdx + step) % total + total) % total;
            if (IsValidTarget(_spec.GetPlayer(idx))) return idx;
        }
        return -1;
    }

    bool IsCurrentTargetValid()
    {
        if (_spec == null || CurrentIndex < 0) return false;
        var p = _spec.GetPlayer(CurrentIndex);
        return IsValidTarget(p);
    }

    void UpdateNameDisplay()
    {
        if (_spec == null) { CurrentTargetName = "—"; return; }
        var p = _spec.GetPlayer(CurrentIndex);
        if (p == null) { CurrentTargetName = "—"; return; }
        CurrentTargetName = !string.IsNullOrEmpty(p.PlayerName)
            ? p.PlayerName
            : $"Player {CurrentIndex + 1}";
    }
}
