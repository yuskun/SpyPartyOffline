using UnityEngine;
using System.Collections.Generic;
using Fusion;

public class ObjectSpawner : MonoBehaviour
{
    public static ObjectSpawner Instance;

    [Header("要生成的 Prefab")]
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private GameObject PlayerItem;
    [SerializeField] private List<GameObject> WeaponItem;




    [Header("生成區域 (可放多個 Collider)")]
    public List<Collider> spawnAreas = new List<Collider>();

    [Header("生成間隔 (秒)")]
    [SerializeField] private float spawnInterval = 2f;

    [Header("離地高度偏移")]
    [SerializeField] private float spawnHeightOffset = 0.5f;

    [Header("最大嘗試次數")]
    [SerializeField] private int maxAttempts = 30;

    [Header("碰撞檢查半徑")]
    [SerializeField] private float radiusCheck = 0.5f;

    [Header("檢測射線高度")]
    [SerializeField] private float rayHeight = 10f;

    [Header("最大生成數量")]
    [SerializeField] private int maxTotal = 40;

    [Header("初始生成數量")]
    [SerializeField] private int initialSpawnCount = 5;

    [Header("武器生成機率 (0~1)")]
    [SerializeField] private float weaponSpawnChance = 0.35f;

    [Header("任務卡救援設定")]
    [Tooltip("每隔幾秒檢查一次任務卡是否遺失")]
    [SerializeField] private float rescueCheckInterval = 2f;
    [Tooltip("Y 座標低於此值視為掉出世界，觸發救援")]
    [SerializeField] private float fallThreshold = -20f;

    private float timer;
    private List<GameObject> spawnedObjects = new List<GameObject>();

    // === 任務卡救援追蹤 ===
    private class TrackedLostCard
    {
        public PlayerItem item;          // 可能 Unity fake-null
        public CardData cardData;
        public Transform ownerTransform; // 原本的玩家 transform（用於重生時的附近落點）
    }
    private List<TrackedLostCard> trackedLostCards = new List<TrackedLostCard>();
    private float _rescueTimer;



    void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        for (int i = 0; i < initialSpawnCount; i++)
            RandomSpawnObject(1);
    }

    private void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            RandomSpawnObject();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            RandomSpawnObject();
        }

        // === 任務卡救援巡邏（僅 Host） ===
        if (NetworkManager2.Instance != null
            && NetworkManager2.Instance.runner != null
            && NetworkManager2.Instance.runner.IsServer)
        {
            _rescueTimer += Time.fixedDeltaTime;
            if (_rescueTimer >= rescueCheckInterval)
            {
                _rescueTimer = 0f;
                RescueLostCards();
            }
        }
    }
    bool TryFindGroundPosition(Collider area, out Vector3 spawnPosition)
    {
        Debug.Log($"嘗試在區域 {area.name} 中尋找生成點...");
        Bounds bounds = area.bounds;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            attempts++;

            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);

            // ✅ 起點設在 bounds 中心上方，不用固定 rayHeight
            Vector3 origin = new Vector3(randomX, bounds.center.y + rayHeight, randomZ);

            // 可視化偵測線（方便 Debug）
            Debug.DrawRay(origin, Vector3.down * (rayHeight * 2), Color.yellow, 2f);


            if (Physics.Raycast(
    origin,
    Vector3.down,
    out RaycastHit hit,
    rayHeight * 2,
    ~0,
    QueryTriggerInteraction.Ignore
))
            {
                // 🟢 同時支援 Tag 或 Layer
                bool isGroundTag = hit.collider.CompareTag("SpawnArea");


                if (isGroundTag)
                {
                    spawnPosition = hit.point + Vector3.up * spawnHeightOffset;
                    return true;

                    // // 🔒 檢查不被遮擋
                    // if (!Physics.CheckSphere(spawnPosition, radiusCheck, ~0, QueryTriggerInteraction.Ignore))
                    // {
                    //     return true;
                    // }
                }
            }
        }

        spawnPosition = Vector3.zero;
        return false;
    }
    GameObject GetRandomSpawnPrefab()
    {
        float roll = Random.value;

        if (roll >= weaponSpawnChance)
        {
            return prefabToSpawn;
        }
        else
        {
            if (WeaponItem == null || WeaponItem.Count == 0)
                return prefabToSpawn;

            return WeaponItem[Random.Range(0, WeaponItem.Count)];
        }
    }
    private int nextAreaIndex = 0;

    public void RandomSpawnObject(int spawnCount = -1)
    {
        if (prefabToSpawn == null || spawnAreas.Count == 0)
            return;

        if (spawnCount < 0)
            spawnCount = Random.Range(2, 5);

        for (int i = 0; i < spawnCount; i++)
        {
            if (spawnedObjects.Count >= maxTotal)
                break;

            bool spawned = false;
            int attempts = 0;

            // 輪流分配區域，確保每片區域平均生成
            Collider area = spawnAreas[nextAreaIndex % spawnAreas.Count];
            nextAreaIndex++;

            while (!spawned && attempts < maxAttempts)
            {
                attempts++;

                if (TryFindGroundPosition(area, out Vector3 pos))
                {
                    GameObject spawnPrefab = GetRandomSpawnPrefab();

                    GameObject newObj =
                        NetworkManager2.Instance.runner
                        .Spawn(spawnPrefab, pos, Quaternion.identity, null,
                        (runner, obj) =>
                        {
                            if (obj.GetComponent<SetPosition>() != null)
                                obj.GetComponent<SetPosition>().Setpos(pos);
                        }).gameObject;

                    spawnedObjects.Add(newObj);
                    spawned = true;
                }
            }

            if (!spawned)
                Debug.LogWarning($"⚠️ 無法在區域 {area.name} 找到可用生成點。");
        }
    }




    /// <summary>從隨機生成區域找一個合法的地面位置，供外部（如 GameManager）使用</summary>
    public bool TryGetRandomSpawnPosition(out Vector3 spawnPosition)
    {
        if (spawnAreas.Count == 0) { spawnPosition = Vector3.zero; return false; }
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Collider area = spawnAreas[Random.Range(0, spawnAreas.Count)];
            if (TryFindGroundPosition(area, out spawnPosition))
                return true;
        }
        spawnPosition = Vector3.zero;
        return false;
    }

    public void RemoveObject(GameObject obj)
    {
        if (spawnedObjects.Contains(obj))
        {
            spawnedObjects.Remove(obj);
        }
    }

    /// <summary>清理列表中已被 Despawn 或銷毀的 null 項目</summary>
    public void CleanupList()
    {
        spawnedObjects.RemoveAll(o => o == null);
    }

    /// <summary>Despawn 所有還存在的生成物件</summary>
    public void DespawnAll()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj == null) continue;
            var netObj = obj.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsValid)
                NetworkManager2.Instance.runner.Despawn(netObj);
            else
                Destroy(obj);
        }
        spawnedObjects.Clear();
    }

    /// <summary>隱藏所有還存在的生成物件（不 Despawn）</summary>
    public void HideAll()
    {
        CleanupList();
        foreach (var obj in spawnedObjects)
        {
            if (obj != null) obj.SetActive(false);
        }
    }

    /// <summary>目前追蹤中的存活物件數量</summary>
    public int AliveCount
    {
        get
        {
            CleanupList();
            return spawnedObjects.Count;
        }
    }

    public void objectToSpawn(GameObject obj, Transform position)
    {
        NetworkManager2.Instance.runner.Spawn(obj, position.position, null, null, (runner, obj) =>
        {
            if (obj.GetComponent<SetPosition>() != null)
                obj.GetComponent<SetPosition>().Setpos(position.position);
                NetworkPlayer.Local.RPC_PlayGlobalSFX(CharacterSFXManager.SFXType.UseCard,NetworkPlayer.Local.PlayerId);
        });
    }


    public void LostCard(Transform centerTransform, List<CardData> cardDatas, float minDropDistance = 3f, float maxDropDistance = 5f)
    {
        if (PlayerItem == null)
        {
            Debug.LogError("❌ LostCard: PlayerItem 尚未設定！");
            return;
        }

        if (cardDatas == null || cardDatas.Count == 0)
        {
            Debug.LogWarning("⚠️ LostCard: 傳入的 CardData 清單為空");
            return;
        }

        // ownerTransform 可能已銷毀（玩家退出／救援時原物件消失），要容忍 null
        bool hasCenter = centerTransform != null;
        Vector3 playerPos = hasCenter ? centerTransform.position : Vector3.zero;

        foreach (var cardData in cardDatas)
        {
            bool isMissionCard = cardData.type == CardType.Mission;
            Vector3 spawnPos = Vector3.zero;
            bool gotPos = false;

            // === 階段 1：玩家附近（3~5m）=== 沒 centerTransform 就跳過
            if (hasCenter)
                gotPos = TryFindDropPosition(playerPos, minDropDistance, maxDropDistance, maxAttempts, out spawnPos);

            // === 階段 2：擴大範圍（6~12m）===
            if (!gotPos && hasCenter)
            {
                Debug.LogWarning($"[LostCard] 階段1 失敗，擴大搜尋範圍 (6~12m)");
                gotPos = TryFindDropPosition(playerPos, 6f, 12f, maxAttempts, out spawnPos);
            }

            // === 階段 3：任意 SpawnArea（沿用 TryGetRandomSpawnPosition）===
            if (!gotPos)
            {
                Debug.LogWarning($"[LostCard] 改用場景任意 SpawnArea");
                gotPos = TryGetRandomSpawnPosition(out spawnPos);
            }

            // === 階段 4：保底（絕對不會消失）===
            if (!gotPos)
            {
                Debug.LogError($"[LostCard] 所有搜尋失敗，保底位置生成（cardType={cardData.type}, id={cardData.id}）");
                spawnPos = hasCenter ? (playerPos + Vector3.up * 1.2f) : Vector3.up * 5f;
            }

            // === 任務卡額外墊高，確保明顯可見 ===
            if (isMissionCard)
                spawnPos += Vector3.up * 0.5f;

            // === 生成 ===
            var obj = NetworkManager2.Instance.runner.Spawn(PlayerItem, spawnPos, null, null, (runner, netObj) =>
            {
                if (netObj.GetComponent<SetPosition>() != null)
                    netObj.GetComponent<SetPosition>().Setpos(spawnPos);
            });

            PlayerItem itemComp = obj.GetComponent<PlayerItem>();
            if (itemComp != null)
                itemComp.cardData = cardData;

            // === 只追蹤任務卡（一般道具消失就算了）===
            if (isMissionCard && itemComp != null)
            {
                trackedLostCards.Add(new TrackedLostCard
                {
                    item = itemComp,
                    cardData = cardData,
                    ownerTransform = centerTransform  // 可能為 null，之後救援會 fallback
                });
                Debug.Log($"[LostCard] 任務卡已註冊到救援追蹤（目前追蹤 {trackedLostCards.Count} 張）");
            }

            Debug.Log($"[LostCard] ✅ 生成卡片 type={cardData.type}, id={cardData.id}, pos={spawnPos}, 任務卡={isMissionCard}");
        }
    }

    /// <summary>
    /// Playitem 被玩家撿走時呼叫：從救援追蹤列表移除，避免被誤判為遺失而重生。
    /// </summary>
    public void UnregisterLostCard(PlayerItem item)
    {
        if (item == null) return;
        int removed = trackedLostCards.RemoveAll(t => ReferenceEquals(t.item, item));
        if (removed > 0)
            Debug.Log($"[LostCard Rescue] 任務卡被撿走，移除追蹤（剩 {trackedLostCards.Count} 張）");
    }

    /// <summary>
    /// 任務卡救援巡邏（僅 Host）：
    /// - 物件 null（已銷毀但沒經過撿走流程）→ 救援
    /// - 物件 inactive 但沒 Unregister → 保險也救
    /// - Y 座標低於 fallThreshold（掉出世界）→ 救援
    /// </summary>
    private void RescueLostCards()
    {
        if (trackedLostCards.Count == 0) return;

        for (int i = trackedLostCards.Count - 1; i >= 0; i--)
        {
            var t = trackedLostCards[i];
            bool needsRescue = false;
            string reason = "";

            if (t.item == null)
            {
                needsRescue = true;
                reason = "物件已消失（未經撿走流程）";
            }
            else if (!t.item.gameObject.activeInHierarchy)
            {
                needsRescue = true;
                reason = "物件 inactive";
            }
            else if (t.item.transform.position.y < fallThreshold)
            {
                needsRescue = true;
                reason = $"Y={t.item.transform.position.y:F2} < {fallThreshold}";
            }

            if (!needsRescue) continue;

            Debug.LogWarning($"[LostCard Rescue] 🚨 救援任務卡 id={t.cardData.id}，原因：{reason}");

            // 先從追蹤列表移除這筆（LostCard 會重新 Add）
            var oldCardData = t.cardData;
            var oldOwner = t.ownerTransform;
            var oldItem = t.item;
            trackedLostCards.RemoveAt(i);

            // 把殘留物件 Despawn（若還存在且有效）
            if (oldItem != null && oldItem.Object != null && oldItem.Object.IsValid)
            {
                NetworkManager2.Instance.runner.Despawn(oldItem.Object);
            }

            // 重新走 LostCard 4 階段流程
            LostCard(oldOwner, new List<CardData> { oldCardData });
        }
    }

    /// <summary>
    /// 嘗試在指定玩家位置的環形範圍內，找到 SpawnArea 上的掉落點
    /// </summary>
    private bool TryFindDropPosition(Vector3 center, float minDist, float maxDist, int attempts, out Vector3 spawnPos)
    {
        for (int i = 0; i < attempts; i++)
        {
            float distance = Random.Range(minDist, maxDist);
            float angle    = Random.Range(0f, 360f);
            Vector3 offset = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad), 0f,
                Mathf.Sin(angle * Mathf.Deg2Rad)) * distance;

            Vector3 origin = center + offset + Vector3.up * rayHeight;
            Debug.DrawRay(origin, Vector3.down * (rayHeight * 2), Color.yellow, 2f);

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayHeight * 2, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.CompareTag("SpawnArea"))
                {
                    Debug.DrawRay(origin, Vector3.down * (rayHeight * 2), Color.green, 2f);
                    spawnPos = hit.point + Vector3.up * spawnHeightOffset;
                    return true;
                }
                else
                {
                    Debug.DrawRay(origin, Vector3.down * (rayHeight * 2), Color.red, 2f);
                }
            }
        }
        spawnPos = Vector3.zero;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnAreas != null && spawnAreas.Count > 0)
        {
            Gizmos.color = Color.green;
            foreach (var area in spawnAreas)
            {
                if (area != null)
                    Gizmos.DrawWireCube(area.bounds.center, area.bounds.size);
            }
        }
    }
}

