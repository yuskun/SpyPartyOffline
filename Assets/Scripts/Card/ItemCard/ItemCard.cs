
using UnityEngine;
using Fusion;
using Unity.VisualScripting;

[CreateAssetMenu(menuName = "Card/ItemCard")]
public class ItemCard : Card
{
    [Header("生成的物件 Prefab (必須帶有 NetworkObject)")]
    public GameObject itemPrefab;
    public bool WeaponItem = false;
    public bool needTarget = false;

  

 
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

        PlayerInventory User = PlayerInventoryManager.Instance.GetPlayer(parameters.UserId).GetComponent<PlayerInventory>();

        // ✅ 生成位置：使用 Client 端送來的 Camera/SpawnObject 世界座標（= 預覽的位置）
        //    若 SpawnPosition 為 (0,0,0)（舊 client 或 fallback 失敗），退回玩家身上的 SpawnObject。
        Vector3 spawnWorldPos = parameters.SpawnPosition;
        if (spawnWorldPos == Vector3.zero)
        {
            Transform fallback = playerParent.transform.Find("Ragdoll/SpawnObject");
            if (fallback != null) spawnWorldPos = fallback.position;
            else
            {
                Debug.LogError($"[ItemCard] Player {parameters.UserId} 找不到 SpawnPosition 與 Ragdoll/SpawnObject，放棄生成。");
                return;
            }
        }

        // ✅ 呼叫 ObjectSpawner 生成（Host 負責 Spawn，同步到所有 Client）
        ObjectSpawner.Instance.objectToSpawn(itemPrefab, spawnWorldPos);
        User.RemoveCard(parameters.UseCardIndex);
        if (CardHistoryManager.Instance != null)
        {
            CardHistoryManager.Instance.Record(
                new CardHistoryEntry(
                    parameters.UserId,
                    parameters.TargetId,
                    name,
                    CardType.Item
                    //result
                )
            );
        }
        User.SetCooldownEnd(this.cardData);
        
    }
}

   
