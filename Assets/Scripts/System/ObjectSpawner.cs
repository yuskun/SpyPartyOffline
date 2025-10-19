using UnityEngine;
using System.Collections.Generic;
using Fusion;

public class ObjectSpawner : MonoBehaviour
{
    public static ObjectSpawner Instance;

    [Header("要生成的 Prefab")]
    [SerializeField] private GameObject prefabToSpawn;

    [Header("生成範圍 (必須有 Collider)")]
    public Collider spawnArea;

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
    [SerializeField] private int maxTotal = 40;   // 最多 40

    private float timer;

    // 記錄生成的物件
    private List<GameObject> spawnedObjects = new List<GameObject>();

    private void Awake()
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

        // 測試用
        if (Input.GetKeyDown(KeyCode.F2))
        {
            RandomSpawnObject();
        }
    }

    public void RandomSpawnObject()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogError("Spawner 沒有設定 Prefab！");
            return;
        }

        // ✅ 超過最大數量就不生了
        if (spawnedObjects.Count >= maxTotal)
        {
            return;
        }

        Bounds bounds = spawnArea.bounds;
        int spawnCount = Random.Range(1, 4);

        for (int i = 0; i < spawnCount; i++)
        {
            if (spawnedObjects.Count >= maxTotal) break;

            bool spawned = false;
            int attempts = 0;

            while (!spawned && attempts < maxAttempts)
            {
                attempts++;

                float randomX = Random.Range(bounds.min.x, bounds.max.x);
                float randomZ = Random.Range(bounds.min.z, bounds.max.z);

                Ray ray = new Ray(new Vector3(randomX, bounds.max.y + rayHeight, randomZ), Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, rayHeight * 2))
                {
                    Vector3 spawnPosition = hit.point + Vector3.up * spawnHeightOffset;

                    if (!Physics.CheckSphere(spawnPosition, radiusCheck))
                    {
                        GameObject newObj = NetworkManager.instance._runner.Spawn(prefabToSpawn, spawnPosition, Quaternion.identity).gameObject;
                        spawnedObjects.Add(newObj);  // ✅ 加進清單
                        spawned = true;
                    }
                }
            }

            if (!spawned)
            {
                Debug.LogWarning($"物件 {i + 1} 超過 {maxAttempts} 次仍找不到可用位置。");
            }
        }
    }

    // 如果有物件被摧毀，要從清單移除
    public void RemoveObject(GameObject obj)
    {
        if (spawnedObjects.Contains(obj))
        {
            spawnedObjects.Remove(obj);
        }
    }
    public void objectToSpawn(GameObject obj, Transform position)
    {

         NetworkManager.instance._runner.Spawn(obj, position.position, Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnArea != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(spawnArea.bounds.center, spawnArea.bounds.size);
        }
    }
}



