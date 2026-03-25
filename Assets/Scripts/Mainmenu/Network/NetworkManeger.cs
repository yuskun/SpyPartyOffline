using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using OodlesEngine;
using System.Linq;
using System.Threading.Tasks;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    static public NetworkManager instance;
    [HideInInspector] public NetworkRunner _runner;
    public GameObject HostSystem;
    private GameObject Host;
    public NetworkObject gameManager;
    private SessionInfo RandomSeseion;
    public GameObject GameScene;
    private bool isClientJoining = false;


    public string PlayerName;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;


            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    async void StartGame(GameMode mode)
    {
        // 1️⃣ Runner 初始化（只做一次）
        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);
            gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        MenuUIManager.instance.showUI(MenuUIManager.instance.LoadingScreen);

        // 2️⃣ 進 Lobby（不假設 SessionList 已存在）
        var lobbyResult = await _runner.JoinSessionLobby(SessionLobby.ClientServer);
        if (!lobbyResult.Ok)
        {
            Debug.LogError("❌ Join Lobby 失敗");
            await _runner.Shutdown();
            MenuUIManager.instance.showUI(MenuUIManager.instance.Menu);
            return;
        }

        // 3️⃣ Host / Client 分流
        if (mode == GameMode.Host)
        {
            await StartAsHostInternal();
        }
        else
        {
            // Client 不在這裡直接 StartGame
            Debug.Log("🟡 Client 已進 Lobby，等待房間列表");
        }

    }
    async void JoinAsClientInternal()
    {
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = RandomSeseion.Name,
            SceneManager = GetComponent<NetworkSceneManagerDefault>()
        });

        if (!result.Ok)
        {
            Debug.LogError($"❌ Client 加入失敗：{result.ShutdownReason}");
            isClientJoining = false;
            return;
        }

        Debug.Log("✅ Client 成功加入房間");

        MenuUIManager.instance.ShowGameroom(GameMode.Client);
        MenuUIManager.instance.CloseAi();
    }
    async Task StartAsHostInternal()
    {
        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = Guid.NewGuid().ToString(),
            Scene = scene,
            SceneManager = GetComponent<NetworkSceneManagerDefault>(),
            IsVisible = true,
            IsOpen = true
        });

        if (!result.Ok)
        {
            Debug.LogError("❌ Host StartGame 失敗");
            return;
        }

        Debug.Log("✅ 建立房間成功");

        MenuUIManager.instance.ShowGameroom(GameMode.Host);
        MenuUIManager.instance.CloseAi();

        Host = Instantiate(HostSystem);

        var obj = _runner.Spawn(gameManager);
     
    }

    public void StartAsHost() => StartGame(GameMode.Host);
    public void StartAsClient() => StartGame(GameMode.Client);
    public void Leave() => LeaveRoom();
    public async void LeaveRoom()
    {
        if (_runner == null)
            return;

        Debug.Log("🔴 離開房間中...");

        MenuUIManager.instance.showUI(MenuUIManager.instance.LoadingScreen);

        // 防止 SessionList callback 又亂觸發
        isClientJoining = false;
        RandomSeseion = null;
        if (Host != null)
        {
            Destroy(Host);
            Host = null;
        }
        await _runner.Shutdown(false);

        Destroy(_runner);
        _runner = null;

        Debug.Log("✅ 已成功離線");

        MenuUIManager.instance.showUI(MenuUIManager.instance.Menu);
    }

    // -------------------------
    // 以下為 INetworkRunnerCallbacks 介面方法的實作
    // -------------------------

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"🟢 玩家 {player} 已加入");
        if (_runner.IsServer)
        {
            Debug.Log("TEST");
            PlayerSpawner.instance.SpawnPlayer(_runner, 2, player, PlayerName);
            int skinIndex = PlayerPrefs.GetInt("Choosenindex", 0);
            MenuUIManager.instance.playerlistmanager.RegisterPlayer(player, PlayerName, skinIndex);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"🔴 Runner Shutdown: {shutdownReason}");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"🔴 與伺服器斷線: {reason}");
        LeaveRoom();
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        throw new NotImplementedException();
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        throw new NotImplementedException();
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        throw new NotImplementedException();
    }

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
                      InputManager.Get().GetDoAction2(),
                     InputManager.Get().GetCameraLook(),
                     runner.DeltaTime, runner.Tick);

        input.Set(pci);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        throw new NotImplementedException();
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {


        if (isClientJoining) return;

        var target = sessionList.FirstOrDefault(s => s.IsOpen && s.IsVisible);
        if (target == null)
        {
            Debug.Log("❌ 沒有可加入的房間");

            return;
        }

        RandomSeseion = target;
        isClientJoining = true;

        JoinAsClientInternal();
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        throw new NotImplementedException();
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        throw new NotImplementedException();
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

}


