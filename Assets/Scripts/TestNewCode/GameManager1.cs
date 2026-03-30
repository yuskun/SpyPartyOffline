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

    [Header("小偷任務目標道具（設定3個 Item CardData）")]
    public List<CardData> stealTargetCardDatas = new List<CardData>();
    [Networked] private NetworkBool HasStarted { get; set; }
    [Networked] private TickTimer StartDelay { get; set; }

    public override void Spawned()
    {
         instance = this;
        Rpc_Ready(PlayerPrefs.GetInt("Choosenindex"), default);
        MenuUIManager.instance.Gameroom.SetActive(false);
        GameUIManager.Instance.HUDUI.SetActive(true);
        //GameHUDManager.Instance.ShowHUD();
        LocalBackpack.Instance.SetUpdateEnabled(true);
        
        if (Runner.IsServer)
        {
           
            HasStarted = false;

        }
    }
    public void StartGameRequest()
    {

        if (!Runner.IsServer) return;

        if (HasStarted) return;
       

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

        // 從 CardCatalog.CanSpwanCard 隨機挑 3 張 ItemCard 注入 MissionWinSystem
        MissionWinSystem.Instance.stealTargetItems.Clear();
        var spawnableItems = new List<ItemCard>();
        var seen = new HashSet<ItemCard>();
        foreach (var card in CardManager.Instance.Catalog.CanSpwanCard)
        {
            if (card is ItemCard ic && seen.Add(ic))
                spawnableItems.Add(ic);
        }
        for (int i = spawnableItems.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (spawnableItems[i], spawnableItems[j]) = (spawnableItems[j], spawnableItems[i]);
        }
        int pickCount = Mathf.Min(3, spawnableItems.Count);
        for (int i = 0; i < pickCount; i++)
            MissionWinSystem.Instance.stealTargetItems.Add(spawnableItems[i]);
        Debug.Log($"[GameManager] 本局竊取目標：{string.Join(", ", MissionWinSystem.Instance.stealTargetItems.ConvertAll(x => x.name))}");
    

        MenuUIManager.instance.LoadingScreen.SetActive(false);

    }
    public override void FixedUpdateNetwork()
    {
        // 押送系統 Tick（Host 驅動，遊戲進行中持續執行）
        if (HasStarted && Runner.IsServer && MissionWinSystem.Instance != null)
            MissionWinSystem.Instance.TickEscort(Runner.DeltaTime);

        if (HasStarted) return;

        if (!StartDelay.IsRunning) return;

        if (StartDelay.Expired(Runner))
        {
            HasStarted = true;
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
    public void Rpc_SetWiretap(PlayerRef tapperRef, int targetId, float duration)
    {
        if (Runner.LocalPlayer != tapperRef) return;
        WiretapManager.Instance?.StartWiretap(targetId, duration);
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
     [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Draw()
    {
        GameUIManager.Instance.Draw();
    }

    /// <summary>押送開始：廣播給所有 Client，顯示範圍圓圈（僅對 Catch 玩家可見）</summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_EscortStart(int catcherID, int targetID)
    {
        EscortRangeIndicator.SetEscort(catcherID, targetID);
    }

    /// <summary>押送結束（成功或中斷）：廣播給所有 Client，隱藏範圍圓圈</summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_EscortEnd()
    {
        EscortRangeIndicator.ClearEscort();
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
                NetworkManager2.Instance.PlayerName
            );
        }
    }
}
