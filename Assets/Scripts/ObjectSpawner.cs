using UnityEngine;

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

    private float timer;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            RandomSpawnObject();
        }

        // 測試用按鍵
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

        Bounds bounds = spawnArea.bounds;
        int spawnCount = Random.Range(1, 4);

        for (int i = 0; i < spawnCount; i++)
        {
            bool spawned = false;
            int attempts = 0;

            while (!spawned && attempts < maxAttempts)
            {
                attempts++;

                float randomX = Random.Range(bounds.min.x, bounds.max.x);
                float randomZ = Random.Range(bounds.min.z, bounds.max.z);

                // 從上往下打射線
                Ray ray = new Ray(new Vector3(randomX, bounds.max.y + rayHeight, randomZ), Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, rayHeight * 2))
                {
                    Vector3 spawnPosition = hit.point + Vector3.up * spawnHeightOffset;

                    // 檢查半徑內是否有碰撞
                    if (!Physics.CheckSphere(spawnPosition, radiusCheck))
                    {
                        Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
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
    public void objectToSpawn(GameObject obj,Transform position)
    {
  
        Instantiate(obj, position.position, Quaternion.identity);
    }

    // 可視化生成區與檢查半徑
    private void OnDrawGizmosSelected()
    {
        if (spawnArea != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(spawnArea.bounds.center, spawnArea.bounds.size);
        }
    }
}
