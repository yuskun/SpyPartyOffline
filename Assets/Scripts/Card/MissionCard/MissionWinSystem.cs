using System.Collections.Generic;
using UnityEngine;

public class MissionWinSystem : MonoBehaviour
{
    public static MissionWinSystem Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public bool CatchWin = false;
    int CatchID;
    public bool StealWin = false;
    int StealID;
    public bool FightWin = false;
    int FightID;


    public void GameOver()
    {
        Debug.Log("StealID："+StealID);
        Debug.Log("遊戲結束，檢查任務完成狀態");
        // 每個任務 ID 只會有一個玩家完成
        HashSet<int> completedPlayers = new HashSet<int>();

        // Catch 任務
        //if (CatchWin)
            completedPlayers.Add(CatchID);

        // Steal 任務
        //if (StealWin)
            completedPlayers.Add(StealID);

        // Fight 任務
        //if (FightWin)
            completedPlayers.Add(FightID);

        // 逐個玩家判斷
        foreach (int playerId in completedPlayers)
        {
            var missionCards = PlayerInventoryManager.Instance.GetCardsByPlayer(playerId)
                .FindAll(c => c.type == CardType.Mission);

            // 如果該玩家所有 mission 都完成
            bool playerWin = true;
            //判定是否完成所有卡牌
            foreach (var c in missionCards)
            {
                if (c.id == 1 && (!CatchWin || CatchID != playerId))
                    playerWin = false;
                if (c.id == 2 && (!StealWin || StealID != playerId))
                    playerWin = false;
                if (c.id == 3 && (!FightWin || FightID != playerId))
                    playerWin = false;
            }

            if (playerWin)
                Debug.Log($"玩家 {playerId} ✅ 完成所有任務，勝利！");
            else
                Debug.Log($"玩家 {playerId} ❌ 尚未完成任務");
        }
    }


    //任務追蹤器
    public void Catch(int id)
    {
        // 如果 Catch 任務的持有者變了，重置進度
        if (CatchID != id)
        {
            CatchWin = false;  // 重置完成狀態
            Debug.Log($"Catch 任務持有者變更，重置進度 (原玩家 {CatchID} → 新玩家 {id})");
        }

        CatchID = id; // 更新目前玩家 ID
    }

    public void Steal(int id)
    {
        if (StealID != id)
        {
            StealWin = false;
            Debug.Log($"Steal 任務持有者變更，重置進度 (原玩家 {StealID} → 新玩家 {id})");
        }

        StealID = id;
    }

    public void Fight(int id)
    {
        if (FightID != id)
        {
            FightWin = false;
            Debug.Log($"Fight 任務持有者變更，重置進度 (原玩家 {FightID} → 新玩家 {id})");
        }

        FightID = id;
    }



}