using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using OodlesEngine;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    static public NetworkManager instance;
    private NetworkRunner _runner;
    public GameObject HostSystem;
    private SessionInfo RandomSeseion;


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
        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);

        }
        MenuUIManager.instance.showUI(MenuUIManager.instance.LoadingScreen);
        var Lobby = await _runner.JoinSessionLobby(SessionLobby.ClientServer, null);
        if (!Lobby.Ok)
        {
            await _runner.Shutdown();
            MenuUIManager.instance.showUI(MenuUIManager.instance.Menu);
            return;
        }



        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }

        if (mode == GameMode.Host)
        {
            var result = await _runner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = "TestRoom",
                Scene = scene,
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
                IsVisible = true,
                IsOpen = true,

            });

            if (result.Ok)
            {
                MenuUIManager.instance.ShowGameroom(mode);
                MenuUIManager.instance.CloseAi();
                Debug.Log("✅ 建立房間成功");
                Instantiate(HostSystem, transform);
            }
        }
        else if (mode == GameMode.Client)
        {
            if (RandomSeseion == null)
            {
                Debug.Log("❌ 沒有可加入的房間");
                MenuUIManager.instance.showUI(MenuUIManager.instance.Menu);
                return;
            }
            var result = await _runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Client,              // ✅ 固定為 Client
                SessionName = RandomSeseion.Name,                 // ✅ 使用該房名稱
                SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>()
            });
            if (result.Ok)
            {
                MenuUIManager.instance.ShowGameroom(mode);
                MenuUIManager.instance.CloseAi();
            }
            else
                Debug.LogError($"❌ 加入失敗：{result.ShutdownReason}");

        }

    }
    public void StartAsHost() => StartGame(GameMode.Host);
    public void StartAsClient() => StartGame(GameMode.Client);
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
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RegisterNameOnHost(string name, RpcInfo info = default)
    {
        MenuUIManager.instance.playerlistmanager.RegisterPlayer(info.Source, name);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"玩家 {player} 已加入遊戲");
        PlayerSpawner.instance.SpawnPlayer(runner, 0, player);


        if (runner.IsServer)
        {
            // Host 直接註冊
            MenuUIManager.instance.playerlistmanager.RegisterPlayer(player, PlayerName);
        }
        else if (runner.LocalPlayer == player)
        {
            // Client 告訴 Host 自己的名稱
            RPC_RegisterNameOnHost(PlayerName);
           
            
        }



    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        throw new NotImplementedException();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        throw new NotImplementedException();
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
                     InputManager.Get().GetCameraLook(),
                     Time.fixedDeltaTime, 0);

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


        if (sessionList.Count > 0)
        {
            // 隨機挑一個房
            var target = sessionList[UnityEngine.Random.Range(0, sessionList.Count)];

            if (target.IsOpen && target.IsVisible)
            {
                RandomSeseion = target;
            }
            else
            {
                Debug.Log("⚠️ 找到的房間已關閉或不可見");
            }
        }
        else
        {
            Debug.Log("❌ 大廳目前沒有任何可加入的房間");
        }
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
        if (runner.IsServer)
        {
            PlayerSpawner.instance.RefreshSpawnPoints();
        }
    }
}




