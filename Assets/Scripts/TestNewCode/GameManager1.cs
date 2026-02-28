using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using GLTFast.Schema;
using OodlesEngine;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SocialPlatforms;

public class GameManager : NetworkBehaviour
{
    public GameObject HostSystem;
    public static GameManager instance;
    private Dictionary<PlayerRef, int> playerCharacterIndex = new Dictionary<PlayerRef, int>();
    public CountdownTimer countdownTimer;
    public int AICount = 0;
    [Networked] private NetworkBool HasStarted { get; set; }
    [Networked] private TickTimer StartDelay { get; set; }

    public override void Spawned()
    {
        Rpc_Ready(PlayerPrefs.GetInt("Choosenindex"), default);
        MenuUIManager.instance.Gameroom.SetActive(false);
        GameUIManager.Instance.HUDUI.SetActive(true);
        LocalBackpack.Instance.SetUpdateEnabled(true);
        if (Runner.IsServer)
        {
            instance = this;
            HasStarted = false;

        }
    }
    public void StartGameRequest()
    {

        if (!Runner.IsServer) return;

        if (HasStarted) return;
        Debug.Log("Test3");

        StartDelay = TickTimer.CreateFromSeconds(Runner, 1f);
    }

    public void GameInit()
    {
        Instantiate(HostSystem);
        SetSpawnArea();
        PlayerSpawner.instance.RefreshSpawnPoints();
        MissionWinSystem.Instance.ResetFightStats();
        SpawnAI();
        SpawnAllPlayers();
        PlayerInventoryManager.Instance.init();
        PlayerInventoryManager.Instance.Refresh();
        MissionWinSystem.Instance.FightWinCount = PlayerInventoryManager.Instance.playerInventories.Count - 1;
        InitMissionData();

    }
    public override void FixedUpdateNetwork()
    {
        
        if (HasStarted) return;
     
        if (!StartDelay.IsRunning) return;
    
        if (StartDelay.Expired(Runner))
        {
            HasStarted = true;                   // 這行是「全局鎖」
            GameInit();
            GameStart();
        }
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
        if (Runner.IsServer)
        {
            RandomAssignMissionCard();
            countdownTimer.StartTimer();
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
        Debug.Log("ActivePlayers Count: " + Runner.ActivePlayers.Count());
        for (int i = 0; i < 4 - Runner.ActivePlayers.Count(); i++)
        {
            PlayerSpawner.instance.SpawnPlayer(Runner, null, PlayerRef.None, "AI_" + i);
        }
    }
    public void RandomAssignMissionCard()
    {
        List<CardData> datas = CardManager.Instance.GetAllMissionCardData();
        List<PlayerInventory> players = PlayerInventoryManager.Instance.playerInventories;

        if (datas == null || datas.Count == 0 || players == null || players.Count == 0)
        {
            Debug.LogWarning(players.Count);
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
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_RequestUseCard(CardUseParameters cardUseParameters)
    {
        CardManager.Instance.UseCard(cardUseParameters);
    }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void Rpc_Ready(int chosenIndex, RpcInfo info)
    {
        if (Runner.IsServer)
        {
            playerCharacterIndex[info.Source] = chosenIndex;
            if (playerCharacterIndex.Count == Runner.ActivePlayers.Count())
            {
                StartGameRequest();
            }
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
    private void SpawnAllPlayers()
    {
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (!playerCharacterIndex.ContainsKey(player))
            {
                Debug.LogError($"Missing character index for {player}");
                continue;
            }

            PlayerSpawner.instance.SpawnPlayer(
                Runner,
                playerCharacterIndex[player],
                player,
                "Player_" + player.PlayerId
            );
        }
    }
}
