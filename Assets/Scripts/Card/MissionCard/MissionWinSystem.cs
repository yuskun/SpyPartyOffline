using System.Collections.Generic;
using ExitGames.Client.Photon.StructWrapping;
using OodlesEngine;
using UnityEngine;

public class MissionWinSystem : MonoBehaviour
{
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
        GameObject hud = GameObject.Find("HUDMissionlist");
        missionUIManager = hud.GetComponent<MissionUIManager>();
       
    }

    public bool CatchWin = false;
    int CatchID = -1;
    public bool StealWin = false;
    public int StealID = -1;
    public bool FightWin = false;
    int FightID = -1;

    public void ResetFightStats()
    {
        FightCount = 0;                     // 把計數歸零
        knockedTargets.Clear();            // 清除已擊倒的列表
        Debug.LogWarning(FightCount + "/" + FightWinCount);
    }
    public void GameOver()
    {
        Debug.Log("StealID：" + StealID);
        Debug.Log("CatchID：" + CatchID);
        Debug.Log("FightID：" + FightID);
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
            }
            else
                Debug.Log($"玩家 {playerId} ❌ 尚未完成任務");
        }
    }
    public int GetFightID()
    {
        return FightID;
    }
    public void UpdateFightCount(OodlesCharacter target)
    {
        if (!knockedTargets.Contains(target))
        {
            knockedTargets.Add(target); // 記錄對象
            FightCount++;

            PlayerInventoryManager.Instance.playerInventories[FightID].MissionStates.Set(2, 1);

            // 任務檢查
            if (FightCount == FightWinCount)
            {
                FightWin = true;
                GameOver();
            }
            TraceMission.Instance.ProcessPlayerCards();
        }
    }


    //任務追蹤器
    public void Catch(int id)
    {

        // 如果 Catch 任務的持有者變了，重置進度
        if (CatchID != id)
        {
            Debug.Log("Catch 任務持有者變更，重置進度");
            CatchWin = false;  // 重置完成狀態
            PlayerInventoryManager.Instance.playerInventories[id].MissionStates.Set(0, 0);
            if (CatchID != -1)
                PlayerInventoryManager.Instance.playerInventories[CatchID].MissionStates.Remove(0);

            // GameManager.instance.RPC_UpdateMission(id, 0, "逮捕", "抓到會偷東西的人", 1, true);
            // GameManager.instance.RPC_UpdateMission(CatchID, 0, "逮捕", "抓到會偷東西的人", 1, false);
        }

        CatchID = id; // 更新目前玩家 ID
    }

    public void Steal(int id)
    {
        if (StealID != id || StealID == null)
        {
            StealWin = false;
            PlayerInventoryManager.Instance.playerInventories[id].MissionStates.Set(1, 0);
            if (StealID != -1)
                PlayerInventoryManager.Instance.playerInventories[StealID].MissionStates.Remove(1);
            // GameManager.instance.RPC_UpdateMission(id, 1, "躲起來", "在遊戲結束前不要被逮捕到", 1, true);
            // GameManager.instance.RPC_UpdateMission(StealID, 1, "躲起來", "在遊戲結束前不要被逮捕到", 1, false);
        }

        StealID = id;
    }

    public void Fight(int id)
    {
        if (FightID != id)
        {
            FightWin = false;
            ResetFightStats();
            PlayerInventoryManager.Instance.playerInventories[id].MissionStates.Set(2, 0);
            if (FightID != -1)
                PlayerInventoryManager.Instance.playerInventories[FightID].MissionStates.Remove(2);
            // GameManager.instance.RPC_UpdateMission(id, 2, "戰鬥", "擊倒所有人前不要被擊倒", FightWinCount, true);
            // GameManager.instance.RPC_UpdateMission(FightID, 2, "戰鬥", "擊倒所有人前不要被擊倒", FightWinCount, false);


        }

        FightID = id;
    }



}