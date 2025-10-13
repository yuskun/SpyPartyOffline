using System.Collections.Generic;
using UnityEngine;

public class TraceMission : MonoBehaviour
{
    //追蹤每個任務卡在誰身上
    public static TraceMission Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ProcessPlayerCards()
    {
        if (PlayerInventoryManager.Instance == null || MissionWinSystem.Instance == null)
        {
            Debug.LogWarning("找不到 PlayerInventoryManager 或 MissionWinSystem");
            return;
        }

        PlayerInventoryManager.Instance.Refresh(); // 確保資料最新

        int index = 0;
        while (true)
        {
            GameObject player = PlayerInventoryManager.Instance.GetPlayer(index);
            if (player == null) break;

            PlayerIdentify identify = player.GetComponent<PlayerIdentify>();
            /*if (identify == null)
            {
                index++;
                continue;
            }*/

            int playerId = identify.PlayerID;
            var cards = PlayerInventoryManager.Instance.GetCardsByPlayer(playerId);

            foreach (var c in cards)
            {
                Debug.Log($"玩家 {playerId} 卡片: id={c.id}, type={c.type}");

                if (c.type == CardType.Mission)
                {
                    if (c.id == 1)
                    {
                        // 呼叫 MissionWinSystem 的 A()
                        MissionWinSystem.Instance.Catch(playerId);
                    }
                    else if (c.id == 2)
                    {
                        // 呼叫 MissionWinSystem 的 B()
                        MissionWinSystem.Instance.Steal(playerId);
                    }
                    else if (c.id == 3)
                    {
                        // 呼叫 MissionWinSystem 的 B()
                        MissionWinSystem.Instance.Fight(playerId);
                    }
                }
            }

            index++;
        }
    }
}
