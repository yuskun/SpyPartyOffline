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

        if (inventories == null)
        {
            Debug.LogError("[Catch] playerInventories is null");
            return;
        }

        if (id < 0 || id >= inventories.Count)
        {
            Debug.LogError($"[Catch] invalid id: {id}, inventories.Count: {inventories.Count}");
            return;
        }

        // ✅ 修正：檢查 CatchID 的合法性，而不是誤用其他 ID
        if (CatchID != -1 && (CatchID < 0 || CatchID >= inventories.Count))
        {
            Debug.LogError($"[Catch] invalid previous CatchID: {CatchID}, inventories.Count: {inventories.Count}");
            CatchID = -1;
        }

        if (CatchID != id)
        {
            Debug.Log("Catch 任務持有者變更，重置進度");
            CatchWin = false;

            inventories[id].MissionStates.Set(0, 0);

            if (CatchID != -1)
                inventories[CatchID].MissionStates.Remove(0);
        }

        CatchID = id;
    }

    public void Steal(int id)
    {
        var inventories = PlayerInventoryManager.Instance.playerInventories;

        if (inventories == null)
        {
            Debug.LogError("[Steal] playerInventories is null");
            return;
        }

        if (id < 0 || id >= inventories.Count)
        {
            Debug.LogError($"[Steal] invalid id: {id}, inventories.Count: {inventories.Count}");
            return;
        }

        // ✅ 修正：檢查 StealID 的合法性（原本錯誤檢查了 CatchID）
        if (StealID != -1 && (StealID < 0 || StealID >= inventories.Count))
        {
            Debug.LogError($"[Steal] invalid previous StealID: {StealID}, inventories.Count: {inventories.Count}");
            StealID = -1;
        }

        if (StealID != id)
        {
            StealWin = false;
            inventories[id].MissionStates.Set(1, 0);

            if (StealID != -1)
                inventories[StealID].MissionStates.Remove(1);
        }

        StealID = id;
    }

    public void Fight(int id)
    {
        var inventories = PlayerInventoryManager.Instance.playerInventories;

        if (inventories == null)
        {
            Debug.LogError("[Fight] playerInventories is null");
            return;
        }

        if (id < 0 || id >= inventories.Count)
        {
            Debug.LogError($"[Fight] invalid id: {id}, inventories.Count: {inventories.Count}");
            return;
        }

        // ✅ 修正：檢查 FightID 的合法性（原本錯誤檢查了 CatchID）
        if (FightID != -1 && (FightID < 0 || FightID >= inventories.Count))
        {
            Debug.LogError($"[Fight] invalid previous FightID: {FightID}, inventories.Count: {inventories.Count}");
            FightID = -1;
        }

        if (FightID != id)
        {
            FightWin = false;
            ResetFightStats();
            inventories[id].MissionStates.Set(2, 0);

            if (FightID != -1)
                inventories[FightID].MissionStates.Remove(2);
        }

        FightID = id;
    }
}