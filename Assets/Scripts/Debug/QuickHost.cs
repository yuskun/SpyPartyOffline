using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using OodlesEngine;

public class QuickHost : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("玩家 Prefab")]
    public NetworkPrefabRef playerPrefab;   // 拖進你的玩家 prefab (NetworkObject)

    private NetworkRunner runner;

    async void Start()
    {
        Debug.Log("🚀 QuickHost 啟動中...");
        await StartGame(GameMode.Host);
    }

    // ✅ 建立房間
    async Task StartGame(GameMode mode)
    {
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;

        // Host 模式會自動建立房間名稱
        string roomName = "Room_" + Random.Range(1000, 9999);

        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = roomName,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            Debug.Log($"✅ Host 房間建立成功：{roomName}");
        }
        else
        {
            Debug.LogError($"❌ Host 建立失敗: {result.ShutdownReason}");
        }
    }

    // 玩家加入時自動生成角色
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            Debug.Log($"👤 玩家加入: {player.PlayerId}");
            Vector3 spawnPos = new Vector3(0, 1, 0); // 你可以根據需要更改出生位置
            runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, null, (runner1, obj) =>
            {
                obj.GetComponent<NetworkPlayer>().PlayerId = player;
            });
        }
    }

    #region Fusion Callbacks (不重要但要有)
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
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) => request.Accept();
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new System.NotImplementedException();
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new System.NotImplementedException();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        throw new System.NotImplementedException();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        throw new System.NotImplementedException();
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data)
    {
        throw new System.NotImplementedException();
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        throw new System.NotImplementedException();
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        throw new System.NotImplementedException();
    }
    #endregion
}
