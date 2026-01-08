using UnityEngine;
using Fusion;

public class PlayerSpawner : MonoBehaviour
{

    public GameObject[] characterPrefabs; // 四個造型
    public Transform[] spawnPoints;
    public static PlayerSpawner instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            LoadCharacterPrefabs();
            RefreshSpawnPoints();

        }
        else
        {
            Debug.LogWarning("Multiple instances of PlayerSpawner detected. Destroying duplicate.");
        }
    }

    private void LoadCharacterPrefabs()
    {
        // 從 Resources/Characters 載入所有 prefab
        characterPrefabs = Resources.LoadAll<GameObject>("Characters");

        if (characterPrefabs == null || characterPrefabs.Length == 0)
            Debug.LogError("[PlayerSpawner] 無法載入角色 Prefabs，請確認 Resources/Characters 資料夾是否存在。");
        else
            Debug.Log($"[PlayerSpawner] 已載入 {characterPrefabs.Length} 個角色 Prefab。");
    }
    public void SpawnPlayer(NetworkRunner runner, int? index, PlayerRef player, string name)
    {
        // 隨機選擇一個生成點
        Transform chosenSpawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];

        int chosenIndex = (index.HasValue && index.Value >= 0 && index.Value < characterPrefabs.Length)
             ? index.Value
             : UnityEngine.Random.Range(0, characterPrefabs.Length);
        // 隨機選擇一個角色造型
        GameObject chosenCharacterPrefab = characterPrefabs[chosenIndex];
        runner.Spawn(chosenCharacterPrefab, chosenSpawnPoint.position, Quaternion.identity, null, (runner, obj) =>
        {
            Debug.Log($"[PlayerSpawner] 玩家 {player} 生成於 {chosenSpawnPoint.position}，使用角色 {chosenCharacterPrefab.name}。");
            obj.GetComponent<NetworkPlayer>().PlayerId = player;
            obj.GetComponent<PlayerIdentify>().name = name;

        });

    }
    public void RefreshSpawnPoints()
    {
        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
        spawnPoints = new Transform[spawnPointObjects.Length];
        for (int i = 0; i < spawnPointObjects.Length; i++)
        {
            spawnPoints[i] = spawnPointObjects[i].transform;
        }
       
    }

}
