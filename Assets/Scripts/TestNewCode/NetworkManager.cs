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

    private enum NetMode { Idle, Host, Client }
    private NetMode mode = NetMode.Idle;

    private bool waitingQuickJoin = false;

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

        PlayerName = "1234";

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
            mode = NetMode.Idle;
            return;
        }

        Debug.Log("Joined by code");
        MenuUIManager.instance.ShowGameroom(GameMode.Client);
    }

    private async Task QuickJoinAsync()
    {
        if (mode != NetMode.Idle) return;
        MenuUIManager.instance.showUI(MenuUIManager.instance.LoadingScreen);


        await InitRunner();

        waitingQuickJoin = true;

        await runner.JoinSessionLobby(SessionLobby.ClientServer);
    }

    private async Task LeaveAsync()
    {
        SceneManager.LoadScene(0);
        if (runner == null) return;

        waitingQuickJoin = false;

        if (runner.IsRunning)
        {

            runner.RemoveCallbacks(this);
            await runner.Shutdown();
        }


        // 2) 直接把整個 RunnerRoot 砍掉（最乾淨）
        if (runnerRoot != null)
            Destroy(runnerRoot);

        runner = null;
        sceneManager = null;
        runnerRoot = null;


        mode = NetMode.Idle;

        Debug.Log("Disconnected");

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

    // =============================
    // Callbacks
    // =============================

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> list)
    {
        if (!waitingQuickJoin) return;

        var room = list.FirstOrDefault(s => s.IsOpen && s.IsVisible);
        if (room == null)
        {
            Debug.Log("No available room");
            return;
        }

        waitingQuickJoin = false;
        _ = JoinByCodeAsync(room.Name);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;

        Debug.Log($"Player joined: {player}");

        PlayerSpawner.instance.SpawnPlayer(runner, PlayerPrefs.GetInt("Choosenindex"), player, PlayerName);
        MenuUIManager.instance.playerlistmanager.RegisterPlayer(player, PlayerName);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player left: {player}");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        _ = LeaveAsync();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason reason) { }
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        OodlesCharacterInput pci = new OodlesCharacterInput(
                   InputManager.Get().GetVertical(),
                   InputManager.Get().GetHorizontal(),
                   InputManager.Get().GetJump(),
                   InputManager.Get().GetTouchMoveY(),
                   InputManager.Get().GetLeftHandUse(),
                   InputManager.Get().GetRightHandUse(),
                   InputManager.Get().GetDoAction1(),
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

    }
    public void OnSceneLoadStart(NetworkRunner r) { }
}