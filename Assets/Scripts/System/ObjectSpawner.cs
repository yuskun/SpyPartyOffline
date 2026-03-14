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
    [SerializeField] private float spawnInterval = 10f;

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

    private float timer;
    private List<GameObject> spawnedObjects = new List<GameObject>();



    void Awake()
    {
        Instance = this;
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
        // 70% 普通道具，30% 武器（比例你可自己調）
        float roll = Random.value;

        if (roll < 0.7f)
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
    public void RandomSpawnObject()
    {
        if (prefabToSpawn == null || spawnAreas.Count == 0)
            return;

        int spawnCount = Random.Range(1, 3);
        for (int i = 0; i < spawnCount; i++)
        {
            if (spawnedObjects.Count >= maxTotal)
                break;

            bool spawned = false;
            int attempts = 0;

            while (!spawned && attempts < maxAttempts)
            {
                attempts++;

                Collider area = spawnAreas[Random.Range(0, spawnAreas.Count)];
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
                Debug.LogWarning($"⚠️ 無法在任何區域找到可用生成點。");
        }
    }




    public void RemoveObject(GameObject obj)
    {
        if (spawnedObjects.Contains(obj))
        {
            spawnedObjects.Remove(obj);
        }
    }

    public void objectToSpawn(GameObject obj, Transform position)
    {
        NetworkManager2.Instance.runner.Spawn(obj, position.position, null, null, (runner, obj) =>
        {
            if (obj.GetComponent<SetPosition>() != null)
                obj.GetComponent<SetPosition>().Setpos(position.position);
                CharacterSFXManager.Instance?.PlayUseCard();
        });
    }


    public void LostCard(Transform centerTransform, List<CardData> cardDatas, float minDropDistance = 3f, float maxDropDistance = 5f)
    {
        Debug.Log("TESTTTTTT");
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

        foreach (var cardData in cardDatas)
        {
            bool spawned = false;
            int attempts = 0;
            while (!spawned && attempts < maxAttempts)
            {
                attempts++;

                // 🎯 隨機生成玩家周圍 3~5 公尺範圍
                float distance = Random.Range(minDropDistance, maxDropDistance);
                float angle = Random.Range(0f, 360f);
                Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad)) * distance;

                // 從玩家上方射線往下
                Vector3 origin = centerTransform.position + offset + Vector3.up * rayHeight;

                // 預設畫黃色（代表還沒命中）
                Debug.DrawRay(origin, Vector3.down * (rayHeight * 2), Color.yellow, 2f);

                // 打到任何東西
                if (Physics.Raycast(
     origin,
     Vector3.down,
     out RaycastHit hit,
     rayHeight * 2,
     ~0,
     QueryTriggerInteraction.Ignore
 ))
                {
                    // 🎨 顏色顯示狀態
                    if (hit.collider.CompareTag("SpawnArea"))
                    {
                        Debug.DrawRay(origin, Vector3.down * (rayHeight * 2), Color.green, 2f);

                        // ✅ 使用命中的點作為生成座標
                        Vector3 spawnPos = hit.point + Vector3.up * spawnHeightOffset;

                        // ✅ 避免生成在牆裡或其他物件裡

                        // ✅ 生成掉落物
                        // var obj = Instantiate(PlayerItem, spawnPos, Quaternion.identity);
                        var obj = NetworkManager2.Instance.runner.Spawn(PlayerItem, spawnPos, null, null, (runner, obj) =>
        {
            if (obj.GetComponent<SetPosition>() != null)
                obj.GetComponent<SetPosition>().Setpos(spawnPos);
        }); ;
                        PlayerItem item = obj.GetComponent<PlayerItem>();
                        if (item != null)
                            item.cardData = cardData;

                        spawned = true;

                    }
                    else
                    {
                        Debug.DrawRay(origin, Vector3.down * (rayHeight * 2), Color.red, 2f);
                    }
                }
            }
        }
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

