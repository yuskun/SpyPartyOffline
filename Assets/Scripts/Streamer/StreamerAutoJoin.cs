using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Streamer 端的自動加房流程：
///   1. 建立獨立 NetworkRunner（與主選單的 NetworkManager 互不干擾）
///   2. JoinSessionLobby → OnSessionListUpdated 挑第一個 IsOpen+IsVisible 的 session
///   3. 加房失敗 / 斷線 / 結束 → 自動重啟 runner 重新加 lobby 再掃
/// </summary>
public class StreamerAutoJoin : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Auto Join 設定")]
    [Tooltip("加房失敗後的冷卻時間（秒）")]
    [SerializeField] private float retryDelay = 5f;
    [Tooltip("斷線後重建 runner 的延遲（秒）")]
    [SerializeField] private float reconnectDelay = 2f;
    [Tooltip("host 斷線後要回到的場景名稱（必須在 Build Settings）")]
    [SerializeField] private string mainMenuSceneName = "Mainmenu";
    [Tooltip("找不到場景名時的 fallback build index")]
    [SerializeField] private int mainMenuSceneIndex = 0;

    private NetworkRunner _runner;
    private NetworkSceneManagerDefault _sceneMgr;
    private bool _joining = false;
    private bool _joined = false;
    private bool _inLobby = false;
    private float _retryReadyTime = 0f;
    private float _stateStartTime = 0f;
    private int   _lastSessionTotal = 0;
    private int   _lastSessionJoinable = 0;
    private string _currentSessionName = "";

    public NetworkRunner Runner => _runner;
    public bool IsJoined => _joined;
    public bool IsJoining => _joining;
    public bool IsInLobby => _inLobby;
    public int  RoomsVisible  => _lastSessionTotal;
    public int  RoomsJoinable => _lastSessionJoinable;
    public string CurrentSessionName => _currentSessionName;
    /// <summary>距離當前狀態開始的秒數（用來顯示「等了多久」）</summary>
    public float SecondsInState => Mathf.Max(0f, Time.time - _stateStartTime);

    public enum State { Booting, ConnectingLobby, ScanningRooms, Joining, JoinedWaiting }
    public State CurrentState
    {
        get
        {
            if (_runner == null) return State.Booting;
            if (!_inLobby) return State.ConnectingLobby;
            if (_joining) return State.Joining;
            if (_joined)  return State.JoinedWaiting;
            return State.ScanningRooms;
        }
    }

    void ResetStateTimer() { _stateStartTime = Time.time; }

    async void Start()
    {
        await SetupAndJoinLobby();
    }

    async Task SetupAndJoinLobby()
    {
        ResetStateTimer();
        _inLobby = false;
        _joined = false;
        _joining = false;
        _lastSessionTotal = 0;
        _lastSessionJoinable = 0;
        _currentSessionName = "";

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = false;        // streamer 不送輸入
        _runner.AddCallbacks(this);
        _sceneMgr = gameObject.AddComponent<NetworkSceneManagerDefault>();

        Debug.Log("[Streamer] Joining lobby...");
        var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer);
        if (!result.Ok)
        {
            Debug.LogError($"[Streamer] JoinSessionLobby failed: {result.ShutdownReason}. Retrying in {reconnectDelay}s...");
            await Task.Delay(Mathf.RoundToInt(reconnectDelay * 1000));
            CleanupRunner();
            await SetupAndJoinLobby();
            return;
        }
        _inLobby = true;
        ResetStateTimer();
        Debug.Log("[Streamer] Lobby joined. Waiting for sessions...");
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        // 更新 UI 用的統計（不管接下來能不能加都先記下）
        _lastSessionTotal = sessionList?.Count ?? 0;
        _lastSessionJoinable = 0;
        if (sessionList != null)
        {
            foreach (var s in sessionList)
                if (s != null && s.IsOpen && s.IsVisible && s.PlayerCount < s.MaxPlayers)
                    _lastSessionJoinable++;
        }

        if (_joining || _joined) return;
        if (Time.time < _retryReadyTime) return;
        if (_lastSessionTotal == 0) return; // 沒房間，繼續等下次 callback

        var target = sessionList.FirstOrDefault(s =>
            s != null && s.IsOpen && s.IsVisible && s.PlayerCount < s.MaxPlayers);
        if (target == null) return; // 全滿/全鎖

        Debug.Log($"[Streamer] Found joinable session: {target.Name} ({target.PlayerCount}/{target.MaxPlayers})");
        _joining = true;
        _currentSessionName = target.Name;
        ResetStateTimer();
        _ = JoinSession(target.Name);
    }

    async Task JoinSession(string sessionName)
    {
        var args = new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = sessionName,
            SceneManager = _sceneMgr,
            ConnectionToken = StreamerToken.Get(), // host 端會比對此簽名跳過 spawn
        };
        var result = await _runner.StartGame(args);
        if (!result.Ok)
        {
            Debug.LogError($"[Streamer] Join failed: {result.ShutdownReason}. Retrying after {retryDelay}s...");
            _joining = false;
            _currentSessionName = "";
            _retryReadyTime = Time.time + retryDelay;
            ResetStateTimer();
            return;
        }
        _joined = true;
        _joining = false;
        ResetStateTimer();
        Debug.Log($"[Streamer] ✅ Joined session: {sessionName}");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"[Streamer] Disconnected from server: {reason}");
        ResetSessionState();
        _ = ReconnectAfterDelay();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[Streamer] Runner shutdown: {shutdownReason}");
        ResetSessionState();
        _ = ReconnectAfterDelay();
    }

    /// <summary>立即把 session 相關狀態清成「初始 (Booting)」— 確保 Overlay 馬上切回最初 UI</summary>
    private void ResetSessionState()
    {
        _joined = false;
        _joining = false;
        _inLobby = false;
        _lastSessionTotal = 0;
        _lastSessionJoinable = 0;
        _currentSessionName = "";
        ResetStateTimer();
    }

    /// <summary>
    /// 給 Director 用：偵測到「卡死」（例如 host 突然死掉但 Photon 還沒回 callback）
    /// 時主動觸發重連流程，不等 OnDisconnectedFromServer。
    /// </summary>
    public void ForceReconnect(string reason)
    {
        if (!_joined && _runner == null) return; // 已在重連中
        Debug.Log($"[Streamer] ForceReconnect 觸發：{reason}");
        ResetSessionState();
        _ = ReconnectAfterDelay();
    }

    async Task ReconnectAfterDelay()
    {
        // 順序：先 cleanup runner + 立刻切回 Mainmenu（讓畫面馬上乾淨、Overlay 進 Idle 狀態）
        // 之後才延遲 + 重連 lobby（避免使用者看到 stale 的遊戲場景）
        CleanupRunner();
        await ReturnToMainMenu();

        // 延遲一下避免立刻重連又被打回（連線太快會撞到上次斷線殘留）
        await Task.Delay(Mathf.RoundToInt(reconnectDelay * 1000));

        await SetupAndJoinLobby();
    }

    async Task ReturnToMainMenu()
    {
        var current = SceneManager.GetActiveScene();
        if (current.name == mainMenuSceneName) return;

        Debug.Log($"[Streamer] Returning to Mainmenu (current: {current.name})...");

        AsyncOperation op = null;
        try { op = SceneManager.LoadSceneAsync(mainMenuSceneName); }
        catch { op = null; }

        if (op == null)
        {
            Debug.LogWarning($"[Streamer] Scene '{mainMenuSceneName}' not loadable; using build index {mainMenuSceneIndex} fallback.");
            op = SceneManager.LoadSceneAsync(mainMenuSceneIndex);
        }
        if (op == null)
        {
            Debug.LogError("[Streamer] LoadSceneAsync 失敗，無法回到 Mainmenu。");
            return;
        }

        while (!op.isDone) await Task.Yield();
        Debug.Log("[Streamer] Returned to Mainmenu.");
    }

    void CleanupRunner()
    {
        if (_runner != null) Destroy(_runner);
        if (_sceneMgr != null) Destroy(_sceneMgr);
        _runner = null;
        _sceneMgr = null;
    }

    // ==== 不使用的 INetworkRunnerCallbacks 全空實作（避免 throw NotImpl）====
    public void OnConnectedToServer(NetworkRunner runner) { Debug.Log("[Streamer] Connected to server."); }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        => Debug.LogWarning($"[Streamer] Connect failed: {reason}");
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { Debug.Log("[Streamer] Scene loaded."); }
    public void OnSceneLoadStart(NetworkRunner runner) { Debug.Log("[Streamer] Scene loading..."); }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}
