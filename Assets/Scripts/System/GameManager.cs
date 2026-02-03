using System.Collections.Generic;
using System.Linq;
using Fusion;
using GLTFast.Schema;
using OodlesEngine;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class GameManager : NetworkBehaviour
{
    public GameObject GameScene;
    public static GameManager instance;
    private CountdownTimer countdownTimer;
    public GameObject AI;
    public int AICount = 0;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            MenuUIManager.instance.StartButton.onClick.AddListener(OnHostPressStart);
            MenuUIManager.instance.missionUIManager.AddMission(new MissionData(1, "切換", "使用Tab切換任務。", 1));
            MenuUIManager.instance.missionUIManager.AddMission(new MissionData(2, "移動", "WASD進行移動。", 4));
            MenuUIManager.instance.missionUIManager.AddMission(new MissionData(3, "跳躍", "Space進行跳躍。", 1));
            countdownTimer = GetComponent<CountdownTimer>();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_RequestUseCard(CardUseParameters cardUseParameters)
    {
        CardManager.Instance.UseCard(cardUseParameters);

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
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Gameover(int winnerID)
    {
        if (LocalBackpack.Instance.userInventory.gameObject.GetComponent<PlayerIdentify>().PlayerID == winnerID)
        {
            GameUIManager.Instance.Win();
            Debug.Log("你贏了！");
        }
        else
            GameUIManager.Instance.Gameover();
    }

    /// <summary>
    /// RPC：讓所有 Client（與 Host 自己）都執行 GameStart。
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StartGame()
    {
        GameStart();
    }

    void SetSpawnArea()
    {
        GameObject[] spawnAreaObjs = GameObject.FindGameObjectsWithTag("SpawnArea");
        ObjectSpawner.Instance.spawnAreas.Clear();

        foreach (GameObject obj in spawnAreaObjs)
        {
            Collider col = obj.GetComponent<Collider>();
            if (col != null)
            {
                ObjectSpawner.Instance.spawnAreas.Add(col);
            }
        }
    }

    private void GameStart()
    {
        MenuUIManager.instance.Gameroom.SetActive(false);
        MenuUIManager.instance.MenuScene.SetActive(false);
        GameUIManager.Instance.HUDUI.SetActive(true);
        NetworkManager.instance.GameScene.SetActive(true);
        LocalBackpack.Instance.SetUpdateEnabled(true);


        if (Runner.IsServer)
        {
            PlayerSpawner.instance.RefreshSpawnPoints();
            SetSpawnArea();
            SpawnAI();
            AllPlayerTeleport();
            countdownTimer.StartTimer();
            MissionWinSystem.Instance.ResetFightStats();
            RandomAssignMissionCard();
            PlayerInventoryManager.Instance.Refresh();
            MissionWinSystem.Instance.FightWinCount = PlayerInventoryManager.Instance.playerInventories.Count - 1;
            InitMissionData();
            ObjectSpawner.Instance.enabled = true;


        }

    }
    public void InitMissionData()
    {
        RPC_InitMissionData(0, 1);
        RPC_InitMissionData(1, 1);
        RPC_InitMissionData(2, MissionWinSystem.Instance.FightWinCount);
    }
    public void SpawnAI()
    {
        for (int i = 0; i < 4 - Runner.ActivePlayers.Count(); i++)
        {
            PlayerSpawner.instance.SpawnPlayer(Runner, null, PlayerRef.None, "AI_" + i);
        }

    }

    public void AllPlayerTeleport()
    {
        PlayerInventoryManager.Instance.init();
        PlayerInventoryManager.Instance.Refresh();
        PlayerInventoryManager.Instance.playerParents.ForEach(player =>
        {
            var playerObj = player.GetComponent<NetworkPlayer>();
            playerObj.TeleportTo(PlayerSpawner.instance.spawnPoints[Random.Range(0, PlayerSpawner.instance.spawnPoints.Length)].position);
        });
    }
    public void RandomAssignMissionCard()
    {
        List<CardData> datas = CardManager.Instance.GetAllMissionCardData();
        List<PlayerInventory> players = PlayerInventoryManager.Instance.playerInventories;

        if (datas == null || datas.Count == 0 || players == null || players.Count == 0)
        {
            Debug.LogWarning("[CardManager] 無法分配任務卡：資料或玩家列表為空。");
            return;
        }

        System.Random rand = new System.Random();

        // 記錄每位玩家目前已分配幾張
        Dictionary<PlayerInventory, int> assignedCounts = new Dictionary<PlayerInventory, int>();
        foreach (var p in players)
            assignedCounts[p] = 0;

        foreach (var data in datas)
        {
            // 🔸建立加權池（越少卡的玩家，機率越高）
            List<PlayerInventory> weightedPool = new List<PlayerInventory>();
            foreach (var p in players)
            {
                int currentCount = assignedCounts[p];
                // 權重反比於已拿數：拿越少，越多機率被抽到
                int weight = Mathf.Max(1, 5 - currentCount); // 5 可調整權重敏感度
                for (int i = 0; i < weight; i++)
                    weightedPool.Add(p);
            }

            // 🔸隨機選出一名玩家
            PlayerInventory chosen = weightedPool[rand.Next(weightedPool.Count)];

            // 嘗試加入卡片
            bool added = chosen.AddCard(data);
            if (added)
            {
                assignedCounts[chosen]++;
                Debug.Log($"[CardManager] 分配 {data.type} 卡(ID={data.id}) 給玩家 {chosen.name}");
            }
            else
            {
                Debug.LogWarning($"[CardManager] 玩家 {chosen.name} 無法接收卡片 ID={data.id}");
            }
            TraceMission.Instance.ProcessPlayerCards();
        }

        Debug.Log("[CardManager] 任務卡公平分配完成 ✅");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_InitMissionData(int missionID, int newGoal)
    {
        CardManager.Instance.UpdateMissionData(missionID, newGoal);
    }

}
