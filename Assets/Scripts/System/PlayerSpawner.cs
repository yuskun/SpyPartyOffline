using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class PlayerSpawner : MonoBehaviour
{

    public GameObject[] characterPrefabs; // 四個造型
    public Transform[] spawnPoints;

    public static PlayerSpawner instance;

    /// <summary>
    /// 玩家識別色票（hex 字串，8 色循環）。透過 GetPaletteHex(int) 依 index 取色。
    /// 對應 PlayerIdentify.TintColor 套到 Skin material 的 _BaseColor。
    /// </summary>
    public static readonly string[] PlayerPalette = new string[]
    {
        "#FF6464", // 紅
        "#66B3FF", // 藍
        "#80E680", // 綠
        "#FFD966", // 黃
        "#D980FF", // 紫
        "#FFA666", // 橙
        "#66F2F2", // 青
        "#FF8CD9", // 粉
    };

    /// <summary>依索引取 palette hex 字串（自動 mod，負數也 OK）</summary>
    public static string GetPaletteHex(int index)
    {
        int n = PlayerPalette.Length;
        if (n == 0) return "#FFFFFF";
        int i = ((index % n) + n) % n;
        return PlayerPalette[i];
    }

    /// <summary>
    /// HEX 字串轉 Unity Color（內部轉換用）。
    /// 支援 #RGB / #RGBA / #RRGGBB / #RRGGBBAA / 顏色名稱（"red"）。
    /// 解析失敗回傳 null。
    /// </summary>
    public static Color? HexToColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        string h = hex.Trim();
        if (h[0] != '#') h = "#" + h;
        if (ColorUtility.TryParseHtmlString(h, out Color c)) return c;
        Debug.LogWarning($"[PlayerSpawner] 無法解析 hex 色票：'{hex}'");
        return null;
    }

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
    /// <summary>
    /// 生成玩家角色。
    /// </summary>
    /// <param name="hexColor">
    /// 角色色票（hex 字串，例如 "#FF6464"）。
    /// null / 空字串 / 解析失敗 → 不套色（材質保留 prefab 共用，不動）。
    /// 有效 hex → 套到 PlayerIdentify.TintColor [Networked]，同步給所有 client，
    /// 在 Skin 物件上找 material 名稱為 "Skin" 的那顆，改 _BaseColor。
    /// </param>
    public void SpawnPlayer(NetworkRunner runner, int? index, PlayerRef player, string name,
                            bool IsPrepare = false, string hexColor = null)
    {
        // 隨機選擇一個生成點
        Transform chosenSpawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];

        int chosenIndex = (index.HasValue && index.Value >= 0 && index.Value < characterPrefabs.Length)
             ? index.Value
             : UnityEngine.Random.Range(0, characterPrefabs.Length);

        GameObject chosenCharacterPrefab = characterPrefabs[chosenIndex];

        // 解析 hex（失敗 / 空 → null，視為不套色）
        Color? tintColor = HexToColor(hexColor);

        runner.Spawn(chosenCharacterPrefab, chosenSpawnPoint.position, Quaternion.identity, null, (runner, obj) =>
        {
            Debug.Log($"[PlayerSpawner] 玩家 {player} 生成於 {chosenSpawnPoint.position}，使用角色 {chosenCharacterPrefab.name}。");
            obj.GetComponent<NetworkPlayer>().PlayerId = player;
            obj.GetComponent<NetworkPlayer>().isPrepare = IsPrepare;

            var identify = obj.GetComponent<PlayerIdentify>();
            identify.PlayerName = name;
            identify.SkinIndex  = chosenIndex;
            if (tintColor.HasValue)
            {
                // 確保 alpha > 0（PlayerIdentify 用 alpha == 0 當「未指定」sentinel）
                Color c = tintColor.Value;
                if (c.a <= 0.001f) c.a = 1f;
                identify.TintColor = c;
            }

            SkinChange.instance.SetSpawnedPlayer(obj.GetComponent<NetworkObject>());
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
