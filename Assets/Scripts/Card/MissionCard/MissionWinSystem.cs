using System.Collections.Generic;
using System.Runtime.InteropServices;
using ExitGames.Client.Photon.StructWrapping;
using OodlesEngine;
using UnityEngine;

public class MissionWinSystem : MonoBehaviour
{
    public bool isGameOver=false;
    public static MissionWinSystem Instance;
    public int FightWinCount;
    public int FightCount;
    private HashSet<OodlesCharacter> knockedTargets = new HashSet<OodlesCharacter>();

    private MissionUIManager missionUIManager;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Start()
    {
        GameObject hud = GameUIManager.Instance.HUDUI.transform.Find("HUDMissionlist").gameObject;
        missionUIManager = hud.GetComponent<MissionUIManager>();
    }

    public bool CatchWin = false;
    int CatchID = -1;
    public bool StealWin = false;
    public int StealID = -1;
    public bool FightWin = false;
    int FightID = -1;

    [Header("收集勝利條件（需持有以下全部道具）")]
    public List<Card> collectRequiredCards = new List<Card>();
    public bool CollectWin = false;
    public int CollectWinnerID = -1;

    [Header("小偷收集勝利條件")]
    [HideInInspector] public List<ItemCard> stealTargetItems = new List<ItemCard>(); // 由 GameManager.GameInit() 注入
    public bool StealCollectWin = false;

    [Header("押送設定")]
    public float escortDuration = 20f;
    public float escortRadius = 5f;

    public bool isEscorting = false;
    public int escortCatcherID = -1;
    public int escortTargetID = -1;
    private float _escortTimer = 0f;

    /// <summary>開始押送流程，由 Catch 卡成功使用後呼叫（僅 Host）</summary>
    public void StartEscort(int catcherID, int targetID)
    {
        if (isEscorting || isGameOver) return;

        isEscorting = true;
        escortCatcherID = catcherID;
        escortTargetID = targetID;
        _escortTimer = 0f;

        // 重置押送進度（MissionStates[0] = 0，UI 顯示 0/20）
        PlayerInventoryManager.Instance.playerInventories[catcherID].MissionStates.Set(0, 0);

        Debug.Log($"[Escort] 押送開始！抓人者:{catcherID} 目標:{targetID}，需維持 {escortDuration} 秒");
        GameManager.instance.RPC_EscortStart(catcherID, targetID);
    }

    /// <summary>目標是否正在被押送（用於鎖定卡片使用，僅 Host 判斷）</summary>
    public bool IsEscortTarget(int playerID)
    {
        return isEscorting && playerID == escortTargetID;
    }

    /// <summary>每 FixedUpdateNetwork Tick 由 GameManager 呼叫（僅 Server）</summary>
    public void TickEscort(float deltaTime)
    {
        if (!isEscorting) return;
        if (isGameOver) { _CancelEscort(); return; }

        GameObject catcher = PlayerInventoryManager.Instance?.GetPlayer(escortCatcherID);
        GameObject target  = PlayerInventoryManager.Instance?.GetPlayer(escortTargetID);

        if (catcher == null || target == null)
        {
            _CancelEscort();
            return;
        }

        float distance = Vector3.Distance(_GetPlayerPos(catcher), _GetPlayerPos(target));

        if (distance > escortRadius)
        {
            if (_escortTimer > 0f)
            {
                int prev = Mathf.FloorToInt(_escortTimer);
                _escortTimer = 0f;
                // 超出範圍：進度歸零，UI 顯示 0/20
                if (prev > 0)
                    PlayerInventoryManager.Instance.playerInventories[escortCatcherID].MissionStates.Set(0, 0);
                Debug.Log("[Escort] 超出押送範圍，計時重置");
            }
        }
        else
        {
            int secondsBefore = Mathf.FloorToInt(_escortTimer);
            _escortTimer += deltaTime;
            int secondsAfter = Mathf.FloorToInt(_escortTimer);

            // 每過一整秒才更新一次 MissionStates，避免每 Tick 都寫網路狀態
            if (secondsAfter > secondsBefore)
                PlayerInventoryManager.Instance.playerInventories[escortCatcherID].MissionStates.Set(0, secondsAfter);

            if (_escortTimer >= escortDuration)
                _CompleteEscort();
        }
    }

    private void _CompleteEscort()
    {
        int winnerId = escortCatcherID;
        CatchWin = true;
        isEscorting = false;

        // 押送完成：UI 顯示 goal/goal = 完成
        PlayerInventoryManager.Instance.playerInventories[winnerId].MissionStates.Set(0, (int)escortDuration);

        escortCatcherID = -1;
        escortTargetID = -1;
        Debug.Log($"[Escort] 押送成功！玩家 {winnerId}");
        GameManager.instance.RPC_EscortEnd();

        if (CheckAllMissionsComplete(winnerId))
        {
            isGameOver = true;
            Debug.Log($"[Escort] 玩家 {winnerId} 完成所有任務，獲勝！");
            GameManager.instance.RPC_Gameover(winnerId);
        }
        else
        {
            Debug.Log($"[Escort] 玩家 {winnerId} 還有其他任務未完成，暫不結束遊戲");
        }
    }

    private void _CancelEscort()
    {
        isEscorting = false;
        _escortTimer = 0f;

        // 重置押送進度 UI → 退回步驟0「尋找小偷」
        var inventories = PlayerInventoryManager.Instance?.playerInventories;
        if (inventories != null && escortCatcherID >= 0 && escortCatcherID < inventories.Count)
            inventories[escortCatcherID].MissionStates.Set(0, -1);

        escortCatcherID = -1;
        escortTargetID = -1;
        Debug.Log("[Escort] 押送中斷，需重新抓取");
        GameManager.instance.RPC_EscortEnd();
    }

    private Vector3 _GetPlayerPos(GameObject player)
    {
        Transform ragdoll = player.transform.Find("Ragdoll");
        return ragdoll != null ? ragdoll.position : player.transform.position;
    }

    public void ResetFightStats()
    {
        FightCount = 0;
        knockedTargets.Clear();
        Debug.LogWarning(FightCount + "/" + FightWinCount);
    }

    public void GameOver()
    {
        if(!isGameOver)return;
        HashSet<int> completedPlayers = new HashSet<int>();
        completedPlayers.Remove(-1);

        foreach (int playerId in completedPlayers)
        {
            var missionCards = PlayerInventoryManager.Instance.GetCardsByPlayer(playerId)
                .FindAll(c => c.type == CardType.Mission);

            // 如果該玩家沒有任何任務卡，跳過
            if (missionCards.Count == 0)
            {
                Debug.Log($"玩家 {playerId} 沒有任務卡，跳過");
                continue;
            }

            bool playerWin = true;

            foreach (var c in missionCards)
            {
                if (c.id == 0 && (!CatchWin || CatchID != playerId))
                    playerWin = false;
                if (c.id == 1 && (!StealWin || StealID != playerId))
                    playerWin = false;
                if (c.id == 2 && (!FightWin || FightID != playerId))
                    playerWin = false;
            }

            if (playerWin)
            {
                Debug.Log($"玩家 {playerId} ✅ 完成所有任務，勝利！");
                GameManager.instance.RPC_Gameover(playerId);
                isGameOver=true;
            }
            else
            {
                Debug.Log($"玩家 {playerId} ❌ 尚未完成任務");
            }
        }
    }
    public void Draw()
    {
        GameManager.instance.RPC_Draw();
        isGameOver=true;
    }

    /// <summary>
    /// 計時結束時：持有 Steal 任務卡的玩家獲勝。
    /// 若無人持有 Steal 卡則平局。
    /// </summary>
    public void StealTimerWin()
    {
        if (isGameOver) return;

        if (StealID < 0)
        {
            Draw();
            return;
        }

        var inventories = PlayerInventoryManager.Instance.playerInventories;
        if (StealID < inventories.Count)
            inventories[StealID].MissionStates.Set(1, 1);

        StealWin = true;
        isGameOver = true;
        GameManager.instance.RPC_Gameover(StealID);
    }

    /// <summary>
    /// 檢查指定玩家是否已持有所有收集勝利道具。
    /// 每次背包更新後呼叫。
    /// </summary>
    public void CheckCollectWin(int playerId)
    {
        if (isGameOver) return;
        if (collectRequiredCards == null || collectRequiredCards.Count == 0) return;

        var inventories = PlayerInventoryManager.Instance.playerInventories;
        if (playerId < 0 || playerId >= inventories.Count) return;

        PlayerInventory inv = inventories[playerId];
        foreach (var card in collectRequiredCards)
        {
            if (card == null) continue;
            if (!inv.HasCard(card.cardData)) return;
        }

        CollectWin = true;
        CollectWinnerID = playerId;
        Debug.Log($"[CollectWin] 玩家 {playerId} 集齊所有指定道具");

        if (CheckAllMissionsComplete(playerId))
        {
            isGameOver = true;
            Debug.Log($"[CollectWin] 玩家 {playerId} 完成所有任務，獲勝！");
            GameManager.instance.RPC_Gameover(playerId);
        }
        else
        {
            Debug.Log($"[CollectWin] 玩家 {playerId} 還有其他任務未完成，暫不結束遊戲");
        }
    }

    public int GetFightID()
    {
        return FightID;
    }

    /// <summary>
    /// 計算 Steal 持卡者背包中已有幾個目標道具，更新 MissionStates[1]。
    /// 集齊3個後觸發 StealCollectWin。
    /// 由 Steal.UseSkill 及 TraceMission.ProcessPlayerCards 呼叫（僅 Host）。
    /// </summary>
    public void CheckStealItemProgress(int playerId)
    {
        if (isGameOver) return;
        if (StealID != playerId) return;
        if (stealTargetItems == null || stealTargetItems.Count == 0) return;

        var inventories = PlayerInventoryManager.Instance.playerInventories;
        if (playerId < 0 || playerId >= inventories.Count) return;

        PlayerInventory inv = inventories[playerId];
        int count = 0;
        foreach (var item in stealTargetItems)
        {
            if (item == null) continue;
            if (inv.HasCard(new CardData(-1, item.cardData.id, CardType.Item, 0f)))
                count++;
        }

        inv.MissionStates.Set(1, count);
        Debug.Log($"[StealCollect] 玩家 {playerId} 已收集 {count}/{stealTargetItems.Count} 個目標道具");

        if (count >= stealTargetItems.Count)
        {
            StealCollectWin = true;
            Debug.Log($"[StealCollect] 玩家 {playerId} 集齊所有目標道具！");
            if (CheckAllMissionsComplete(playerId))
            {
                isGameOver = true;
                GameManager.instance.RPC_Gameover(playerId);
            }
        }
    }

    /// <summary>
    /// 剩餘1分鐘時切換 Steal UI 為「等待時間結束」模式。
    /// 由 CountdownTimer 廣播觸發（所有端執行，但 MissionStates 由 Host 寫入）。
    /// </summary>
    public void SwitchStealToTimerMode()
    {
        if (StealID < 0) return;
        var inventories = PlayerInventoryManager.Instance?.playerInventories;
        if (inventories == null || StealID >= inventories.Count) return;

        inventories[StealID].MissionStates.Set(11, 1);
        Debug.Log($"[Steal] 剩餘1分鐘，玩家 {StealID} 切換至計時模式");
    }

    /// <summary>
    /// 檢查玩家身上所有任務卡是否全部完成。
    /// 若沒有任何任務卡則視為通過（其他勝利條件已觸發）。
    /// 小偷計時任務（StealTimerWin）不經此方法，不受此限制。
    /// </summary>
    private bool CheckAllMissionsComplete(int playerId)
    {
        var missionCards = PlayerInventoryManager.Instance.GetCardsByPlayer(playerId)
            .FindAll(c => c.type == CardType.Mission);

        foreach (var c in missionCards)
        {
            if (c.id == 0 && (!CatchWin || CatchID != playerId)) return false;
            if (c.id == 1 && !StealWin && !StealCollectWin) return false;
            if (c.id == 2 && (!FightWin || FightID != playerId)) return false;
        }

        return true;
    }

    public void UpdateFightCount(OodlesCharacter target)
    {
        if (!knockedTargets.Contains(target))
        {
            knockedTargets.Add(target);
            FightCount++;
            Debug.Log("FightCount"+FightCount);

            PlayerInventoryManager.Instance.playerInventories[FightID].MissionStates.Set(2, FightCount);

            if (FightCount == FightWinCount)
            {
                FightWin = true;
                if (CheckAllMissionsComplete(FightID))
                {
                    isGameOver = true;
                    Debug.Log($"[Fight] 玩家 {FightID} 完成所有任務，獲勝！");
                    GameManager.instance.RPC_Gameover(FightID);
                }
                else
                {
                    Debug.Log($"[Fight] 玩家 {FightID} 還有其他任務未完成，暫不結束遊戲");
                }
            }
            TraceMission.Instance.ProcessPlayerCards();
        }
    }

    public void Catch(int id)
    {
        var inventories = PlayerInventoryManager.Instance.playerInventories;
        if (inventories == null) return;

        // id=-1 代表無人持有：清理舊持有者
        if (id < 0 || id >= inventories.Count)
        {
            // Catch 卡遺失：若正在押送則中斷，需重新抓取
            if (isEscorting)
            {
                Debug.Log("[Escort] 抓人者遺失 Catch 卡，押送中斷，需重新抓取");
                _CancelEscort();
            }
            if (CatchID != -1 && CatchID >= 0 && CatchID < inventories.Count)
            {
                inventories[CatchID].MissionStates.Remove(0);
                inventories[CatchID].MissionGoals.Remove(0);
            }
            CatchID = -1;
            return;
        }

        if (CatchID != id)
        {
            // Catch 卡易主：若正在押送則中斷，需重新抓取
            if (isEscorting)
            {
                Debug.Log("[Escort] Catch 卡易主，押送中斷，需重新抓取");
                _CancelEscort();
            }
            CatchWin = false;

            // 新持有者：設定 current=-1（步驟0：尋找小偷），goal=押送秒數
            inventories[id].MissionStates.Set(0, -1);
            inventories[id].MissionGoals.Set(0, (int)escortDuration);

            // 舊持有者：移除兩份資料
            if (CatchID != -1 && CatchID < inventories.Count)
            {
                inventories[CatchID].MissionStates.Remove(0);
                inventories[CatchID].MissionGoals.Remove(0);
            }
        }

        CatchID = id;
    }

    public void Steal(int id)
    {
        var inventories = PlayerInventoryManager.Instance.playerInventories;
        if (inventories == null) return;

        // id=-1 代表無人持有：清理舊持有者
        if (id < 0 || id >= inventories.Count)
        {
            // Steal 卡遺失：若正在押送則中斷（目標不再持有 Steal 卡），需重新抓取
            if (isEscorting)
            {
                Debug.Log("[Escort] 目標遺失 Steal 卡，押送中斷，需重新抓取");
                _CancelEscort();
            }
            if (StealID != -1 && StealID >= 0 && StealID < inventories.Count)
            {
                inventories[StealID].MissionStates.Remove(1);
                inventories[StealID].MissionStates.Remove(11);
                inventories[StealID].MissionGoals.Remove(1);
                inventories[StealID].MissionGoals.Remove(11);
                inventories[StealID].MissionGoals.Remove(12);
                inventories[StealID].MissionGoals.Remove(13);
            }
            StealCollectWin = false;
            StealID = -1;
            return;
        }

        if (StealID != id)
        {
            // Steal 卡易主：若正在押送且新持有者不是押送目標，則中斷
            if (isEscorting && id != escortTargetID)
            {
                Debug.Log("[Escort] 目標的 Steal 卡易主，押送中斷，需重新抓取");
                _CancelEscort();
            }
            StealWin = false;
            StealCollectWin = false;

            // 新持有者：收集進度=0，目標=3；寫入3個目標道具 CardID
            inventories[id].MissionStates.Set(1, 0);
            inventories[id].MissionStates.Remove(11); // 重設模式旗標（預設收集模式）
            inventories[id].MissionGoals.Set(1, 3);
            for (int i = 0; i < stealTargetItems.Count && i < 3; i++)
                inventories[id].MissionGoals.Set(11 + i, stealTargetItems[i].cardData.id);

            // 舊持有者：清除全部資料
            if (StealID != -1 && StealID < inventories.Count)
            {
                inventories[StealID].MissionStates.Remove(1);
                inventories[StealID].MissionStates.Remove(11);
                inventories[StealID].MissionGoals.Remove(1);
                inventories[StealID].MissionGoals.Remove(11);
                inventories[StealID].MissionGoals.Remove(12);
                inventories[StealID].MissionGoals.Remove(13);
            }
        }

        StealID = id;
    }

    public void Fight(int id)
    {
        var inventories = PlayerInventoryManager.Instance.playerInventories;
        if (inventories == null) return;

        // id=-1 代表無人持有：清理舊持有者
        if (id < 0 || id >= inventories.Count)
        {
            if (FightID != -1 && FightID >= 0 && FightID < inventories.Count)
            {
                inventories[FightID].MissionStates.Remove(2);
                inventories[FightID].MissionGoals.Remove(2);
            }
            FightID = -1;
            return;
        }

        if (FightID != id)
        {
            FightWin = false;
            ResetFightStats();

            // 新持有者
            inventories[id].MissionStates.Set(2, 0);
            inventories[id].MissionGoals.Set(2, FightWinCount);

            // 舊持有者
            if (FightID != -1 && FightID < inventories.Count)
            {
                inventories[FightID].MissionStates.Remove(2);
                inventories[FightID].MissionGoals.Remove(2);
            }
        }

        FightID = id;
    }
}