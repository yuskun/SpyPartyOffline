
using UnityEngine;
using Fusion;
using Unity.VisualScripting;

[CreateAssetMenu(menuName = "Card/ItemCard")]
public class ItemCard : Card
{
    [Header("生成的物件 Prefab (必須帶有 NetworkObject)")]
    public GameObject itemPrefab;
    public bool WeaponItem = false;

  

 
    public virtual void Execute(CardUseParameters parameters)
    {
        // ✅ 安全檢查
        if (ObjectSpawner.Instance == null)
        {
            Debug.LogError("[ItemCard] ObjectSpawner.Instance 為 null，請確認場景中存在該物件。");
            return;
        }

        if (PlayerInventoryManager.Instance == null)
        {
            Debug.LogError("[ItemCard] PlayerInventoryManager.Instance 為 null。");
            return;
        }

        // ✅ 檢查玩家是否存在
        var playerParent = PlayerInventoryManager.Instance.GetPlayer(parameters.UserId);
        if (playerParent == null)
        {
            Debug.LogError($"[ItemCard] 找不到 Player {parameters.UserId} 的 parent。");
            return;
        }

        // ✅ 嘗試找到生成點
        Transform spawnPoint = playerParent.transform.Find("Ragdoll/SpawnObject");
        if (spawnPoint == null)
        {
            Debug.LogError($"[ItemCard] Player {parameters.UserId} 缺少 Ragdoll/SpawnObject 節點。");
            return;
        }
        PlayerInventory User = PlayerInventoryManager.Instance.GetPlayer(parameters.UserId).GetComponent<PlayerInventory>();

        // ✅ 呼叫 ObjectSpawner 生成（Host 負責 Spawn，同步到所有 Client）
        ObjectSpawner.Instance.objectToSpawn(itemPrefab, spawnPoint.transform);
        User.RemoveCard(parameters.UseCardIndex);
        if (CardHistoryManager.Instance != null)
        {
            CardHistoryManager.Instance.Record(
                new CardHistoryEntry(
                    parameters.UserId,
                    parameters.TargetId,
                    name,
                    CardType.Item,
                    null
                    //result
                )
            );
        }
        User.SetCooldownEnd(this.cardData);
        
    }
}

   
