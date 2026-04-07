using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using OodlesEngine;
using NUnit.Framework;

public class NetworkManager2 : MonoBehaviour, INetworkRunnerCallbacks
{

    private int PrepareGameIndex = 1;
    public string PlayerName;
    public static NetworkManager2 Instance;
    private GameObject runnerRoot;

    public NetworkRunner runner;
    private NetworkSceneManagerDefault sceneManager;

    public enum NetMode { Idle, Host, Client, Spectator }
    public NetMode mode = NetMode.Idle;

    public static bool IsSpectator { get; private set; } = false;

    private bool waitingQuickJoin = false;
    public string CurrentRoomCode { get; private set; } = "";

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // =============================
    // Public API
    // =============================

    public void Host()
    {
        Debug.Log("Host button clicked");
        _ = HostAsync();
    }

    public void JoinByCode(string code)
    {
        _ = JoinByCodeAsync(code);
    }

    public void QuickJoin()
    {
        _ = QuickJoinAsync();
    }

    public void Leave()
    {
        _ = LeaveAsync();
    }

    public void QuickJoinAsSpectator()
    {
        _ = QuickJoinAsSpectatorAsync();
    }
    public void SwitchScene(int buildIndex)
    {
        _ = SwitchSceneAsync(buildIndex);
    }

    // =============================
    // Core
    // =============================

    private Task InitRunner()
    {
        if (runner != null) return Task.CompletedTask;

        // 1) 建立獨立 Runner 物件
        runnerRoot = new GameObject("FusionRunnerRoot");
        DontDestroyOnLoad(runnerRoot);

        // 2) 把 Runner 掛到新物件上
        runner = runnerRoot.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;
        runner.AddCallbacks(this);

        // 3) SceneManager 也掛在同一個 RunnerRoot 上（推薦）
        sceneManager = runnerRoot.AddComponent<NetworkSceneManagerDefault>();
        if (PlayerName == "")
        {
            PlayerName = "1234";
        }

        return Task.CompletedTask;
    }

    private async Task HostAsync()
    {

        if (mode != NetMode.Idle) return;
        
        MenuUIManager.instance.showUI(MenuUIManager.instance.LoadingScreen);
       
        await InitRunner();
        await runner.JoinSessionLobby(SessionLobby.ClientServer);

        mode = NetMode.Host;

        string sessionName = GenerateRoomCode();
        CurrentRoomCode = sessionName;


        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Host,
            SessionName = sessionName,
            Scene = SceneRef.FromIndex(PrepareGameIndex),
            SceneManager = sceneManager,
            IsOpen = true,
            IsVisible = true,

        });

        if (!result.Ok)
        {
            Debug.LogError("Host failed");
            mode = NetMode.Idle;
            return;
        }

    }

    private async Task JoinByCodeAsync(string code)
    {
        if (mode != NetMode.Idle) return;
        if (string.IsNullOrWhiteSpace(code)) return;

        MenuUIManager.instance.showUI(MenuUIManager.instance.LoadingScreen);

        try
        {
            await InitRunner();

            mode = NetMode.Client;

            var result = await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Client,
                SessionName = code.Trim(),
                SceneManager = sceneManager
            });

            if (!result.Ok)
            {
                Debug.LogError("Join failed");
                QuickJoinFailed("加入房間失敗");
                return;
            }

            Debug.Log("Joined by code");
            MenuUIManager.instance.ShowGameroom(GameMode.Client, code.Trim());
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[JoinByCode] Exception: {e.Message}");
            QuickJoinFailed("加入房間發生錯誤");
        }
    }

    private async Task QuickJoinAsync()
    {
        if (mode != NetMode.Idle) return;
        MenuUIManager.instance.showUI(MenuUIManager.instance.LoadingScreen);

        try
        {
            await InitRunner();
            waitingQuickJoin = true;
            await runner.JoinSessionLobby(SessionLobby.ClientServer);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[QuickJoin] Exception: {e.Message}");
            QuickJoinFailed("快速加入發生錯誤");
        }
    }

    private async Task QuickJoinAsSpectatorAsync()
    {
        if (mode != NetMode.Idle) return;

        IsSpectator = true;
        MenuUIManager.instance.showUI(MenuUIManager.instance.LoadingScreen);

        try
        {
            await InitRunner();
            runner.ProvideInput = false; // 旁觀者不送任何輸入

            mode = NetMode.Spectator;
            waitingQuickJoin = true;

            await runner.JoinSessionLobby(SessionLobby.ClientServer);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[QuickJoinSpectator] Exception: {e.Message}");
            QuickJoinFailed("旁觀者快速加入發生錯誤");
        }
    }

    private bool isLeaving = false;
    private async Task LeaveAsync()
    {
        if (isLeaving) return;
        isLeaving = true;

        waitingQuickJoin = false;

        // 1) 先關閉 Runner（帶 try-catch 避免 Client 斷線時 Shutdown 拋例外）
        if (runner != null)
        {
            try
            {
                if (runner.IsRunning)
                {
                    runner.RemoveCallbacks(this);
                    await runner.Shutdown();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Runner shutdown error: {e.Message}");
            }

            if (runnerRoot != null)
                Destroy(runnerRoot);

            runner = null;
            sceneManager = null;
            runnerRoot = null;
        }

        mode = NetMode.Idle;
        IsSpectator = false;
        isLeaving = false;

        Debug.Log("Disconnected");

        // 2) 切回主選單場景並顯示 UI
        SceneManager.LoadScene(0);
        MenuUIManager.instance.showUI(MenuUIManager.instance.BulidOrJoin);
    }
    public async Task<bool> SwitchSceneAsync(int buildIndex)
    {
        // 1️⃣ 基本檢查
        if (runner == null)
        {
            Debug.LogError("SwitchScene failed: runner is null");
            return false;
        }

        if (!runner.IsRunning)
        {
            Debug.LogError("SwitchScene failed: runner not running");
            return false;
        }

        // 2️⃣ 只允許 Host / Server 切場景
        if (!runner.IsServer)
        {
            Debug.LogWarning("SwitchScene blocked: only server can change scene");
            return false;
        }

        // 3️⃣ 避免重複切同一個場景
        if (SceneManager.GetActiveScene().buildIndex == buildIndex)
        {
            Debug.Log("SwitchScene skipped: already in this scene");
            return false;
        }

        // 4️⃣ 確保場景存在於 Build Settings
        if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError("SwitchScene failed: invalid build index");
            return false;
        }

        // 5️⃣ 真正切場景（Fusion 同步）

        await runner.LoadScene(SceneRef.FromIndex(buildIndex), LoadSceneMode.Single);

        Debug.Log($"Scene switched to index {buildIndex}");
        return true;
    }

    private string GenerateRoomCode()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
    }
    public void PlayerNamgeChanged()
    {
        PlayerName = MenuUIManager.instance.PlayerNameInput.text;
    }

    // =============================
    // Callbacks
    // =============================

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> list)
    {
        if (!waitingQuickJoin) return;

        var room = list.FirstOrDefault(s => s.IsOpen && s.IsVisible);
        if (room == null)
        {
            Debug.Log("[QuickJoin] 找不到可加入的房間，直接返回選單");
            QuickJoinFailed("找不到可加入的房間");
            return;
        }

        waitingQuickJoin = false;

        if (IsSpectator)
            _ = JoinSpectatorByCodeAsync(room.Name);
        else
            _ = JoinByCodeAsync(room.Name);
    }

    /// <summary>旁觀者用房間名稱直接加入（由 QuickJoin 流程觸發）</summary>
    private async Task JoinSpectatorByCodeAsync(string code)
    {
        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Client,
            SessionName = code.Trim(),
            SceneManager = sceneManager
        });

        if (!result.Ok)
        {
            Debug.LogError("Spectator join failed");
            QuickJoinFailed("旁觀加入失敗");
            return;
        }

        Debug.Log("Joined as spectator");
        MenuUIManager.instance.ShowGameroom(GameMode.Client, code.Trim());
    }

    /// <summary>快速加入失敗：清理狀態、關閉 Loading、回到 BulidOrJoin</summary>
    private async void QuickJoinFailed(string reason)
    {
        Debug.LogWarning($"[QuickJoin] Failed: {reason}");

        waitingQuickJoin = false;

        // 關閉 Runner
        if (runner != null)
        {
            try
            {
                runner.RemoveCallbacks(this);
                await runner.Shutdown();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Runner shutdown error: {e.Message}");
            }

            if (runnerRoot != null) Destroy(runnerRoot);
            runner = null;
            sceneManager = null;
            runnerRoot = null;
        }

        mode = NetMode.Idle;
        IsSpectator = false;

        MenuUIManager.instance.showUI(MenuUIManager.instance.BulidOrJoin);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;
        Debug.Log($"Player joined: {player}");
        // Spawn 由 SkinChange.Rpc_RegisterAndSpawn 處理（Client 帶自己的 skinIndex 過來）
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player left: {player}");
        if (!runner.IsServer) return;

        // 1) Despawn 該玩家的角色並清除 SpawnedPlayers 欄位
        if (SkinChange.instance != null)
        {
            for (int i = 0; i < SkinChange.instance.SpawnedPlayers.Length; i++)
            {
                var obj = SkinChange.instance.SpawnedPlayers.Get(i);
                if (obj == null) continue;

                var np = obj.GetComponent<NetworkPlayer>();
                if (np != null && np.PlayerId == player)
                {
                    Debug.Log($"Despawning player object for {player} (slot {i})");
                    runner.Despawn(obj);
                    SkinChange.instance.SpawnedPlayers.Set(i, null);
                    break;
                }
            }
        }

        // 2) 從 PlayerListManager 移除名稱與皮膚紀錄
        var plm = MenuUIManager.instance?.playerlistmanager;
        if (plm != null && plm.Object != null && plm.Object.IsValid)
        {
            plm.UnregisterPlayer(player);
        }
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"[Network] OnDisconnectedFromServer: {reason}");
        _ = LeaveAsync();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        Debug.Log($"[Network] OnShutdown: {reason}");
        // Host 主動離開時 Client 可能只收到 OnShutdown 而非 OnDisconnectedFromServer
        // isLeaving guard 防止與 OnDisconnectedFromServer 重複執行
        _ = LeaveAsync();
    }
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (IsSpectator) return;

        OodlesCharacterInput pci = new OodlesCharacterInput(
                   InputManager.Get().GetVertical(),
                   InputManager.Get().GetHorizontal(),
                   InputManager.Get().GetJump(),
                   InputManager.Get().GetTouchMoveY(),
                   InputManager.Get().GetLeftHandUse(),
                   InputManager.Get().GetRightHandUse(),
                   InputManager.Get().GetDoAction1(),
                   InputManager.Get().GetDoAction2(),
                   InputManager.Get().GetCameraLook(),
                   Time.fixedDeltaTime, runner.Tick);

        input.Set(pci);
    }

    public void OnObjectEnterAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
    public void OnObjectExitAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
    public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner r, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner r, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner r, PlayerRef p, ReliableKey k, ArraySegment<byte> d) { }
    public void OnReliableDataProgress(NetworkRunner r, PlayerRef p, ReliableKey k, float f) { }
    public void OnInputMissing(NetworkRunner r, PlayerRef p, NetworkInput i) { }
    public void OnConnectedToServer(NetworkRunner r) { }
    public void OnCustomAuthenticationResponse(NetworkRunner r, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner r, HostMigrationToken t) { }
    public void OnSceneLoadDone(NetworkRunner r)
    {
        // Loading 由 NetworkPlayer.Spawned() 在玩家生成後關閉，這裡不再自動關
    }
    public void OnSceneLoadStart(NetworkRunner r)
    {
        // 場景開始載入 → 顯示 Loading（所有端，包含 Client）
        if (MenuUIManager.instance != null && MenuUIManager.instance.LoadingScreen != null)
            MenuUIManager.instance.LoadingScreen.SetActive(true);
    }
}