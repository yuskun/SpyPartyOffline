using Fusion;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public GameObject GameScene;
    public static GameManager instance;
    private CountdownTimer countdownTimer;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            MenuUIManager.instance.StartButton.onClick.AddListener(OnHostPressStart);
            countdownTimer = GetComponent<CountdownTimer>();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 只有 Host 會觸發的事件，接著用 RPC 通知所有 Client 執行 GameStart。
    /// </summary>
    private void OnHostPressStart()
    {
        if (Runner.IsServer)
        {
            RPC_StartGame(); // 呼叫 RPC 廣播給所有人（包含自己）
        }
    }

    /// <summary>
    /// RPC：讓所有 Client（與 Host 自己）都執行 GameStart。
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StartGame()
    {
        GameStart();
    }

    private void GameStart()
    {


        Instantiate(GameScene);


        MenuUIManager.instance.MenuScene.SetActive(false);
        GameUIManager.Instance.HUDUI.SetActive(true);
       ObjectSpawner.Instance.spawnArea = GameObject.FindWithTag("SpawnArea").GetComponent<MeshCollider>();

        countdownTimer.StartTimer();

        if (Runner.IsServer)
        {
            
            AllPlayerTeleport();
        }

        Debug.Log("[GameManager] GameStart triggered on " + (Runner.IsServer ? "Host" : "Client"));
    }

    public void AllPlayerTeleport()
    {
        PlayerInventoryManager.Instance.playerParents.ForEach(player =>
        {
            var playerObj = player.GetComponent<NetworkPlayer>();
            playerObj.TeleportTo(new Vector3(68,3,29));
        });
    }
}
