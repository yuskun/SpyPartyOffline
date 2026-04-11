using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using OodlesEngine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SocialPlatforms;

public class GameManager : NetworkBehaviour
{
    public GameObject HostSystem;
    public static GameManager instance;
    private Dictionary<PlayerRef, int> playerCharacterIndex = new Dictionary<PlayerRef, int>();
    private Dictionary<PlayerRef, string> playerNames = new Dictionary<PlayerRef, string>();
    public CountdownTimer countdownTimer;
    public int AICount = 0;

    /// <summary>Server 端追蹤旁觀者 PlayerRef</summary>
    private HashSet<PlayerRef> spectatorPlayers = new HashSet<PlayerRef>();

    [Header("結算點位")]
    public Transform WinnerPoint;
    public Transform LoserPoint;
    public float resultDelay = 3f;

    [Header("結算鏡頭")]
    public Transform ResultsCameraPoint;
    public float resultsCamMoveDuration = 1f;

    [Header("結算動畫")]
    public Animator resultsAnimator;
    public string resultsAnimBoolName = "Show";
    [Tooltip("結算動畫播放時間（秒）；動畫結束後會再等 endUIDelayAfterAnim 才跳出 EndUI")]
    public float resultsAnimDuration = 2f;
    [Tooltip("結算動畫結束後，再等幾秒才打開 EndUI（舊版 UGUI 結算面板）")]
    public float endUIDelayAfterAnim = 5f;

    [Networked] private NetworkBool HasStarted { get; set; }
    [Networked] private TickTimer StartDelay { get; set; }

    public override void Spawned()
    {
        instance = this;

        // 進入遊戲場景一律清掉上一局殘留的結算資料（Host + Client 都清）
        CurrentWinnerData = null;

        // 旁觀者：不生角色、不送 Ready，改用自由相機
        if (NetworkManager2.IsSpectator)
        {
            Rpc_RegisterSpectator();
            MenuUIManager.instance.Gameroom.SetActive(false);
            MenuUIManager.instance.LoadingScreen.SetActive(false);
            GameUIManager.Instance.HUDUI.SetActive(false);
            CameraFollow.Get().enable = false;
            // 掛 SpectatorCamera 到主相機
            Camera cam = Camera.main;
            if (cam != null && cam.GetComponent<SpectatorCamera>() == null)
                cam.gameObject.AddComponent<SpectatorCamera>();
            return;
        }

        string localName = NetworkManager2.Instance != null ? NetworkManager2.Instance.PlayerName : "Player";
        Rpc_Ready(PlayerPrefs.GetInt("Choosenindex"), localName, default);
        MenuUIManager.instance.Gameroom.SetActive(false);
      
        GameUIManager.Instance.HUDUI.SetActive(true);
        //GameHUDManager.Instance.ShowHUD();

        // ✅ 啟用新版 GameHUD UIDocument（Host 由 StartGameBtn 的 Inspector 事件
        //    SetActive(true) GameHUDPanel；Client 沒按鈕，這裡統一處理。
        //    放在 GameManager.Spawned() 裡可以天然綁定「遊戲場景」這個條件，
        //    不用在 NetworkManager.OnSceneLoadStart 猜 build index。）
        if (GameUIManager.Instance.GameHudUI != null)
        {
            var hudGo = GameUIManager.Instance.GameHudUI.gameObject;
            if (!hudGo.activeSelf) hudGo.SetActive(true);
            GameUIManager.Instance.GameHudUI.SetVisible(true);
        }

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
        KnockdownTracker.Reset();
        CurrentWinnerData = null;
        SpawnAI();
        SpawnAllPlayers();
        PlayerInventoryManager.Instance.init();
        PlayerInventoryManager.Instance.Refresh();
        MissionWinSystem.Instance.FightWinCount = PlayerInventoryManager.Instance.playerInventories.Count - 1;

        // Host 生成完全部玩家後，延遲 2 秒統一關閉所有人的 Loading
        StartCoroutine(DelayedHideLoading(2f));
    }

    private IEnumerator DelayedHideLoading(float delay)
    {
        yield return new WaitForSeconds(delay);
        Rpc_HideLoading();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_HideLoading()
    {
        if (MenuUIManager.instance != null && MenuUIManager.instance.LoadingScreen != null)
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

            // 關閉房間，禁止新玩家加入
            Runner.SessionInfo.IsOpen = false;

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
        // 重置上一場殘留的禁止狀態（所有端）
        if (!NetworkManager2.IsSpectator && LocalBackpack.Instance != null)
        {
            LocalBackpack.Instance.OnEscortEnd();
        }

        if (Runner.IsServer)
        {
            RandomAssignMissionCard();
            countdownTimer.StartTimer();
            ObjectSpawner.Instance.enabled = true;
        }
    }
    public void SpawnAI()
    {
        int realPlayerCount = Runner.ActivePlayers.Count() - spectatorPlayers.Count;
        Debug.Log($"ActivePlayers: {Runner.ActivePlayers.Count()}, Spectators: {spectatorPlayers.Count}, RealPlayers: {realPlayerCount}");
        for (int i = 0; i < 4 - realPlayerCount; i++)
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

    /// <summary>Client/Host 請求播放失敗動畫（Host 端執行 SetTrigger，自動同步回所有端）</summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_PlayFailAnimation(int playerId, NetworkString<_16> triggerName)
    {
        var playerObj = PlayerInventoryManager.Instance?.GetPlayer(playerId);
        if (playerObj == null) return;
        var character = playerObj.GetComponent<OodlesEngine.OodlesCharacter>();
        if (character != null && character.animatorPlayer != null)
            character.animatorPlayer.SetTrigger(triggerName.ToString());
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_RegisterSpectator(RpcInfo info = default)
    {
        if (!Runner.IsServer) return;
        spectatorPlayers.Add(info.Source);
        Debug.Log($"[Spectator] Registered spectator: {info.Source} (total: {spectatorPlayers.Count})");

        // 旁觀者註冊後重新檢查：其他玩家可能已經全部 Ready 了
        CheckAllReady();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void Rpc_Ready(int chosenIndex, string playerName, RpcInfo info)
    {
        if (Runner.IsServer)
        {
            playerCharacterIndex[info.Source] = chosenIndex;
            playerNames[info.Source] = playerName;
            Debug.Log($"[Ready] {info.Source} name={playerName} skin={chosenIndex}");
            CheckAllReady();
        }
    }

    private void CheckAllReady()
    {
        int expectedPlayers = Runner.ActivePlayers.Count() - spectatorPlayers.Count;
        if (playerCharacterIndex.Count >= expectedPlayers && expectedPlayers > 0)
        {
            StartGameRequest();
        }
    }
    /// <summary>遊戲結束後停止所有進行中的系統</summary>
    private void StopGameplay()
    {
        // 1) 卡片使用禁用（所有端）
        if (!NetworkManager2.IsSpectator && LocalBackpack.Instance != null)
        {
            LocalBackpack.Instance.SetUpdateEnabled(false);
            if (LocalBackpack.Instance.userInventory != null)
                LocalBackpack.Instance.userInventory.CanUseCard = false;
        }

        // 2) Steal 目標 Outline 關閉（所有端）
        foreach (var obj in StealTargetObject.All)
            if (obj != null) obj.SetHighlight(false);

        // === 以下僅 Host 端 ===
        if (!Runner.IsServer) return;

        // 3) 物件生成停止
        if (ObjectSpawner.Instance != null)
            ObjectSpawner.Instance.enabled = false;

        // 4) 計時器停止
        if (countdownTimer != null)
            countdownTimer.StopTimer();

        // 5) 任務判斷停止（押送等）
        if (MissionWinSystem.Instance != null)
            MissionWinSystem.Instance.isGameOver = true;
    }

    /// <summary>Host 清空所有玩家背包（結算資料收集完後呼叫）</summary>
    private void ClearAllPlayerInventories()
    {
        if (PlayerInventoryManager.Instance == null) return;
        foreach (var inv in PlayerInventoryManager.Instance.playerInventories)
        {
            if (inv != null) inv.ClearAll();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Gameover(int winnerID)
    {
        StopGameplay();

        // Host 端組裝結算資料 → 再清空背包
        if (Runner.IsServer)
        {
            BuildWinnerData(winnerID);
            ClearAllPlayerInventories();
        }

        if (NetworkManager2.IsSpectator)
        {
            StartCoroutine(SpectatorResultsSequence());
            return;
        }

        if (LocalBackpack.Instance.userInventory.gameObject.GetComponent<PlayerIdentify>().PlayerID == winnerID)
        {
            GameUIManager.Instance.Win();
            Debug.Log("你贏了！");
        }
        else
            GameUIManager.Instance.Gameover();

        StartCoroutine(ResultsSequence(new int[] { winnerID }));
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_MultipleWinners(int[] winnerIDs)
    {
        StopGameplay();

        // Host 端組裝結算資料 → 再清空背包
        if (Runner.IsServer)
        {
            if (winnerIDs != null && winnerIDs.Length > 0)
                BuildWinnerData(winnerIDs[0]);
            ClearAllPlayerInventories();
        }

        if (NetworkManager2.IsSpectator)
        {
            StartCoroutine(SpectatorResultsSequence(winnerIDs));
            return;
        }

        int localID = LocalBackpack.Instance.userInventory.gameObject.GetComponent<PlayerIdentify>().PlayerID;
        if (System.Array.IndexOf(winnerIDs, localID) >= 0)
        {
            GameUIManager.Instance.Win();
            Debug.Log("你贏了！");
        }
        else
            GameUIManager.Instance.Gameover();

        StartCoroutine(ResultsSequence(winnerIDs));
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Draw()
    {
        StopGameplay();

        // 平局：所有端（含 Client）清空 CurrentWinnerData，避免殘留上一局資料
        CurrentWinnerData = null;

        if (Runner.IsServer)
        {
            ClearAllPlayerInventories();
        }

        if (NetworkManager2.IsSpectator)
        {
            StartCoroutine(SpectatorResultsSequence());
            return;
        }

        GameUIManager.Instance.Draw();
        StartCoroutine(ResultsSequence(new int[0]));
    }

    // ====== 結算資料系統 ======

    /// <summary>結算資料（Host 端組裝，所有端可讀）</summary>
    public static WinnerData CurrentWinnerData;

    /// <summary>Host 端組裝勝利者的結算資料</summary>
    private void BuildWinnerData(int winnerID)
    {
        var data = new WinnerData();
        data.winnerID = winnerID;

        // 1. 任務卡 — 從勝利者背包抓，附帶圖片
        if (PlayerInventoryManager.Instance != null)
        {
            var cards = PlayerInventoryManager.Instance.GetCardsByPlayer(winnerID);
            if (cards != null)
            {
                foreach (var c in cards)
                {
                    if (c.type == CardType.Mission)
                    {
                        Sprite img = null;
                        var catalogCard = CardManager.Instance?.Catalog?.cards?.Find(
                            x => x.cardData.id == c.id && x.cardData.type == c.type);
                        if (catalogCard != null) img = catalogCard.image;

                        data.missionCards.Add(new MissionCardEntry
                        {
                            card = c,
                            image = img
                        });
                    }
                }
            }
        }

        // 2. 道具/功能卡使用紀錄（不含任務卡）— 從 CardHistoryManager 過濾
        if (CardHistoryManager.Instance != null)
        {
            // key = "cardName_cardType" → CardUsageEntry
            var usageMap = new Dictionary<string, CardUsageEntry>();
            foreach (var entry in CardHistoryManager.Instance.GetAllHistory())
            {
                if (entry.userId != winnerID) continue;
                if (entry.cardType == CardType.Mission) continue; // 排除任務卡

                string key = $"{entry.cardName}_{(int)entry.cardType}";
                if (usageMap.ContainsKey(key))
                {
                    usageMap[key].useCount++;
                }
                else
                {
                    // 從 Catalog 反查出完整的 CardData + 圖片
                    CardData cardData = default;
                    Sprite img = null;
                    if (CardManager.Instance?.Catalog != null)
                    {
                        var card = CardManager.Instance.Catalog.cards.Find(c =>
                            c.GetType().Name == entry.cardName && c.cardData.type == entry.cardType);
                        if (card != null)
                        {
                            cardData = card.cardData;
                            img = card.image;
                        }
                    }

                    usageMap[key] = new CardUsageEntry
                    {
                        card = cardData,
                        image = img,
                        useCount = 1
                    };
                }
            }

            foreach (var kv in usageMap)
                data.cardUsages.Add(kv.Value);
        }

        // 3. 擊倒記錄 — 從 KnockdownTracker
        var koRecords = KnockdownTracker.GetRecords(winnerID);
        foreach (var kv in koRecords)
        {
            data.knockdowns.Add(new KnockdownEntry
            {
                targetPlayerId = kv.Key,
                knockdownCount = kv.Value
            });
        }

        CurrentWinnerData = data;

        Debug.Log($"[Results] WinnerData 組裝完成: winnerID={winnerID}, " +
                  $"任務卡={data.missionCards.Count}, 道具使用={data.cardUsages.Count}種, " +
                  $"擊倒目標={data.knockdowns.Count}人");

        // Host 端組好後，把扁平化的資料廣播給所有 Client（包含自己），
        // 讓 Client 也能在 EndUI 顯示完整的勝利者結算。
        int mCount = data.missionCards.Count;
        int[] mIds = new int[mCount];
        int[] mTypes = new int[mCount];
        for (int i = 0; i < mCount; i++)
        {
            mIds[i] = data.missionCards[i].card.id;
            mTypes[i] = (int)data.missionCards[i].card.type;
        }

        int uCount = data.cardUsages.Count;
        int[] uIds = new int[uCount];
        int[] uTypes = new int[uCount];
        int[] uCounts = new int[uCount];
        for (int i = 0; i < uCount; i++)
        {
            uIds[i] = data.cardUsages[i].card.id;
            uTypes[i] = (int)data.cardUsages[i].card.type;
            uCounts[i] = data.cardUsages[i].useCount;
        }

        int kCount = data.knockdowns.Count;
        int[] kTargets = new int[kCount];
        int[] kCounts = new int[kCount];
        for (int i = 0; i < kCount; i++)
        {
            kTargets[i] = data.knockdowns[i].targetPlayerId;
            kCounts[i] = data.knockdowns[i].knockdownCount;
        }

        Rpc_SyncWinnerData(winnerID, mIds, mTypes, uIds, uTypes, uCounts, kTargets, kCounts);
    }

    /// <summary>
    /// 把 Host 組好的勝利者資料用扁平 int 陣列廣播到所有 Client（含自己）。
    /// Client 端用 CardManager.Catalog 反查出 CardData 跟 Sprite，重建 CurrentWinnerData。
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_SyncWinnerData(
        int winnerID,
        int[] missionIds, int[] missionTypes,
        int[] usageIds, int[] usageTypes, int[] usageCounts,
        int[] knockTargets, int[] knockCounts)
    {
        var data = new WinnerData { winnerID = winnerID };

        var catalog = CardManager.Instance != null ? CardManager.Instance.Catalog : null;

        // --- 任務卡 ---
        if (missionIds != null)
        {
            for (int i = 0; i < missionIds.Length; i++)
            {
                var cd = new CardData { id = missionIds[i], type = (CardType)missionTypes[i] };
                Sprite img = null;
                if (catalog != null)
                {
                    var catalogCard = catalog.cards.Find(
                        c => c.cardData.id == cd.id && c.cardData.type == cd.type);
                    if (catalogCard != null) img = catalogCard.image;
                }
                data.missionCards.Add(new MissionCardEntry { card = cd, image = img });
            }
        }

        // --- 道具/功能卡使用 ---
        if (usageIds != null)
        {
            for (int i = 0; i < usageIds.Length; i++)
            {
                var cd = new CardData { id = usageIds[i], type = (CardType)usageTypes[i] };
                Sprite img = null;
                if (catalog != null)
                {
                    var catalogCard = catalog.cards.Find(
                        c => c.cardData.id == cd.id && c.cardData.type == cd.type);
                    if (catalogCard != null) img = catalogCard.image;
                }
                data.cardUsages.Add(new CardUsageEntry
                {
                    card = cd,
                    image = img,
                    useCount = usageCounts[i]
                });
            }
        }

        // --- 擊倒紀錄 ---
        if (knockTargets != null)
        {
            for (int i = 0; i < knockTargets.Length; i++)
            {
                data.knockdowns.Add(new KnockdownEntry
                {
                    targetPlayerId = knockTargets[i],
                    knockdownCount = knockCounts[i]
                });
            }
        }

        CurrentWinnerData = data;

        Debug.Log($"[Results] 收到 Rpc_SyncWinnerData: winnerID={winnerID}, " +
                  $"任務卡={data.missionCards.Count}, 道具使用={data.cardUsages.Count}種, " +
                  $"擊倒目標={data.knockdowns.Count}人");
    }

    /// <summary>旁觀者的結算流程：關閉自由相機、顯示結算 UI 與返回按鈕</summary>
    private IEnumerator SpectatorResultsSequence(int[] winnerIDs = null)
    {
        yield return new WaitForSeconds(resultDelay);

        // 關閉 SpectatorCamera，讓相機停在當前位置
        var specCam = Camera.main?.GetComponent<SpectatorCamera>();
        if (specCam != null) specCam.enabled = false;

        GameUIManager.Instance.ShowResultsPanel();


        GameObject camPointObj = ResultsCameraPoint != null
            ? ResultsCameraPoint.gameObject
            : GameObject.FindWithTag("ResultsCam");

        if (camPointObj != null)
        {
            float slideIn = ResultsBgPlane.Instance != null ? ResultsBgPlane.Instance.slideInDuration : 2f;
            yield return new WaitForSeconds(slideIn);
            CameraFollow.Get().SnapTo(camPointObj.transform);
        }

        // 旁觀者不觸發動畫，但仍保留相同節奏才跳 EndUI / MULTIPYWIN
        yield return new WaitForSeconds(resultsAnimDuration);
        yield return new WaitForSeconds(endUIDelayAfterAnim);

        if (winnerIDs != null && winnerIDs.Length > 1)
        {
            GameUIManager.Instance.ShowMultiWinUI(winnerIDs);
        }
        else
        {
            GameUIManager.Instance.ShowEndUI();
        }
    }

    private IEnumerator ResultsSequence(int[] winnerIDs)
    {
        yield return new WaitForSeconds(resultDelay);

        // 1. 主相機解綁，停在原地
        CameraFollow.Get().enable = false;

        // 2. 傳送玩家（Host Only）
        if (Runner.IsServer)
        {
            foreach (var parent in PlayerInventoryManager.Instance.playerParents)
            {
                int pid = parent.GetComponent<PlayerIdentify>().PlayerID;
                bool isWinner = System.Array.IndexOf(winnerIDs, pid) >= 0;
                parent.GetComponent<NetworkPlayer>().TeleportTo(
                    isWinner ? WinnerPoint.position : LoserPoint.position);
            }
        }

        // 3. 結算背景滑入
        GameUIManager.Instance.ShowResultsPanel();


        // 4. 等待滑入動畫完成後，將相機移到結算鏡頭點
        // 用 Tag 在 scene 中尋找（避免 prefab-spawned NetworkBehaviour 的 scene reference 在 client 為 null）
        GameObject camPointObj = ResultsCameraPoint != null
            ? ResultsCameraPoint.gameObject
            : GameObject.FindWithTag("ResultsCam");

        if (camPointObj != null)
        {
            float slideIn = ResultsBgPlane.Instance != null ? ResultsBgPlane.Instance.slideInDuration : 2f;
            yield return new WaitForSeconds(slideIn);
            CameraFollow.Get().SnapTo(camPointObj.transform);
        }

        // 相機到位後，Host 端觸發結算動畫
        if (Runner.IsServer && resultsAnimator != null)
        {
            resultsAnimator.SetBool(resultsAnimBoolName, true);
        }

        // 等結算動畫播完
        yield return new WaitForSeconds(endUIDelayAfterAnim);

        // 多人勝利用 MULTIPYWIN，單人勝利用 EndUI
        if (winnerIDs != null && winnerIDs.Length > 1)
        {
            GameUIManager.Instance.ShowMultiWinUI(winnerIDs);
        }
        else
        {
            GameUIManager.Instance.ShowEndUI();
        }
    }

    /// <summary>押送開始：廣播給所有 Client，顯示範圍圓圈（僅對 Catch 玩家可見）</summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_EscortStart(int catcherID, int targetID)
    {
        EscortRangeIndicator.SetEscort(catcherID, targetID);
        if (!NetworkManager2.IsSpectator)
            LocalBackpack.Instance.OnEscortStart(catcherID, targetID);
    }

    /// <summary>押送結束（成功或中斷）：廣播給所有 Client，隱藏範圍圓圈</summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_EscortEnd()
    {
        EscortRangeIndicator.ClearEscort();
        if (!NetworkManager2.IsSpectator)
            LocalBackpack.Instance.OnEscortEnd();
    }
    private void SpawnAllPlayers()
    {
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            // 跳過旁觀者
            if (spectatorPlayers.Contains(player)) continue;

            if (!playerCharacterIndex.ContainsKey(player))
            {
                Debug.LogError($"Missing character index for {player}");
                continue;
            }

            // 從 Rpc_Ready 收集的名字取得
            string playerName = playerNames.ContainsKey(player) ? playerNames[player] : "Player";

            PlayerSpawner.instance.SpawnPlayer(
                Runner,
                playerCharacterIndex[player],
                player,
                playerName
            );
        }
    }
}
