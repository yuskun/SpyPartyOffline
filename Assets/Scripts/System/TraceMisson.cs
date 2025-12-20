using System.Collections.Generic;
using UnityEngine;

public class TraceMission : MonoBehaviour
{
    //�l�ܨC�ӥ��ȥd�b�֨��W
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
        Debug.Log("追蹤所有玩家的任務卡片狀態");
        if (PlayerInventoryManager.Instance == null || MissionWinSystem.Instance == null)
        {
            Debug.LogWarning("�䤣�� PlayerInventoryManager �� MissionWinSystem");
            return;
        }

        PlayerInventoryManager.Instance.Refresh(); // �T�O��Ƴ̷s

        int index = 0;
        bool HasCatch = false;
        bool HasSteal = false;
        bool HasFight = false;
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
                Debug.Log($"PlayerID {playerId} CARD: id={c.id}, type={c.type}");

                if (c.type == CardType.Mission)
                {
                    if (c.id == 0)
                    {
                        // �I�s MissionWinSystem �� A()
                        HasCatch = true;
                        MissionWinSystem.Instance.Catch(playerId);
                    }
                    else if (c.id == 1)
                    {
                        // �I�s MissionWinSystem �� B()
                        HasSteal = true;
                        MissionWinSystem.Instance.Steal(playerId);
                    }
                    else if (c.id == 2)
                    {
                        // �I�s MissionWinSystem �� B()
                        HasFight = true;
                        MissionWinSystem.Instance.Fight(playerId);
                    }
                }
            }

            index++;
        }
        
        if (HasCatch == false)
        {
            MissionWinSystem.Instance.Catch(-1);
            Debug.Log("Catch_Lost");
        }
        if (HasSteal == false)
        {
            MissionWinSystem.Instance.Steal(-1);
            Debug.Log("Steal_Lost");
        }
        if (HasFight == false)
        {
            MissionWinSystem.Instance.Fight(-1);
            Debug.Log("Fight_Lost");
        }
    }
}
