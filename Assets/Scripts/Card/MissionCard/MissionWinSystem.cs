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
        isGameOver = true;

        // 押送完成：UI 顯示 goal/goal = 完成
        PlayerInventoryManager.Instance.playerInventories[winnerId].MissionStates.Set(0, (int)escortDuration);

        escortCatcherID = -1;
        escortTargetID = -1;
        Debug.Log($"[Escort] 押送成功！玩家 {winnerId} 獲勝！");
        GameManager.instance.RPC_EscortEnd();
        GameManager.instance.RPC_Gameover(winnerId);
    }

    private void _CancelEscort()
    {
        isEscorting = false;
        _escortTimer = 0f;
        escortCatcherID = -1;
        escortTargetID = -1;
        Debug.Log("[Escort] 押送中斷");
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
        isGameOver = true;
        Debug.Log($"[CollectWin] 玩家 {playerId} 集齊所有指定道具，獲勝！");
        GameManager.instance.RPC_Gameover(playerId);
    }

    public int GetFightID()
    {
        return FightID;
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
                GameOver();
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
            CatchWin = false;

            // 新持有者：設定 current=0，goal=押送秒數
            inventories[id].MissionStates.Set(0, 0);
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
            if (StealID != -1 && StealID >= 0 && StealID < inventories.Count)
            {
                inventories[StealID].MissionStates.Remove(1);
                inventories[StealID].MissionGoals.Remove(1);
            }
            StealID = -1;
            return;
        }

        if (StealID != id)
        {
            StealWin = false;

            // 新持有者
            inventories[id].MissionStates.Set(1, 0);
            inventories[id].MissionGoals.Set(1, 1);

            // 舊持有者
            if (StealID != -1 && StealID < inventories.Count)
            {
                inventories[StealID].MissionStates.Remove(1);
                inventories[StealID].MissionGoals.Remove(1);
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