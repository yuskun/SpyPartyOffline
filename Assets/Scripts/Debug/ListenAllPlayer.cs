using System.Collections.Generic;
using UnityEngine;

public class PlayerInventoryManager : MonoBehaviour
{
    public static PlayerInventoryManager Instance;
    private string targetLayerName = "Player";

    // 所有玩家的最上層物件
    private List<GameObject> playerParents = new List<GameObject>();
    // 對應每個玩家的 PlayerInventory
    private List<PlayerInventory> playerInventories = new List<PlayerInventory>();
    // 集中所有玩家的 slots
    private List<CardData> allSlots = new List<CardData>();
    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

    }

    void Start()
    {
        FindAllPlayerParents();
        GetInventories();
        CollectAllSlots();
    }

    private void FindAllPlayerParents()
    {
        playerParents.Clear();
        int targetLayer = LayerMask.NameToLayer(targetLayerName);

        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        HashSet<GameObject> uniqueParents = new HashSet<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.layer != targetLayer)
                continue;

            Transform current = obj.transform;
            Transform topParent = current;

            while (current.parent != null && current.parent.gameObject.layer == targetLayer)
            {
                topParent = current.parent;
                current = current.parent;
            }

            uniqueParents.Add(topParent.gameObject);
        }

        playerParents.AddRange(uniqueParents);
        Debug.Log($"找到 {playerParents.Count} 個最上層 Player Layer 物件");
        AssignPlayerIDs();
    }

    private void GetInventories()
    {
        playerInventories.Clear();

        foreach (GameObject parent in playerParents)
        {
            PlayerInventory inv = parent.GetComponentInChildren<PlayerInventory>();
            if (inv != null)
            {
                playerInventories.Add(inv);
            }
        }

        Debug.Log($"找到 {playerInventories.Count} 個 PlayerInventory");
    }

    private void CollectAllSlots()
    {
        allSlots.Clear();

        foreach (PlayerInventory inv in playerInventories)
        {
            allSlots.AddRange(inv.slots);
        }

        Debug.Log($"總共收集 {allSlots.Count} 格卡片");
    }

    // 提供外部存取
    public List<CardData> GetAllSlots()
    {
        return allSlots;
    }
    public void Refresh()
    {
        FindAllPlayerParents();
        GetInventories();
        CollectAllSlots();
        Debug.Log("PlayerInventoryManager 已更新");
    }
    public GameObject GetPlayer(int index)
    {
        return playerParents[index];
    }
    private void AssignPlayerIDs()
    {
        for (int i = 0; i < playerParents.Count; i++)
        {
            PlayerIdentify identify = playerParents[i].GetComponent<PlayerIdentify>();
            if (identify == null)
            {
                identify = playerParents[i].AddComponent<PlayerIdentify>();
            }
            identify.PlayerID = i;
            Debug.Log($"分配 PlayerID {i} 給 {playerParents[i].name}");
        }
    }

}
