using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 多人勝利結算面板填充器：把勝利玩家一一放進 MULTIPYWIN 的容器底下。
///
/// 預期 Prefab 結構：
///   Player (Image)
///     ├─ Image
///     ├─ ICON
///     │   └─ Image     ← 頭像（會被換成該玩家的 skin avatar）
///     ├─ Image
///     └─ Name (TMP)    ← 玩家名稱
///
/// 使用方式：
///   1. 掛在 MULTIPYWIN（或它的任一子物件）上
///   2. Inspector 指定 playerEntryPrefab (Player Prefab) 與 playerListContainer
///      （通常是 MULTIPYWIN 底下那顆當列表容器用的 Image）
///   3. GameUIManager.ShowMultiWinUI(winnerIDs) 會把 winnerIDs 丟進 Pending 欄位，
///      SetActive(true) 後 OnEnable 自動填充
///
/// 不會動容器原生的子物件 — 只會銷毀自己 Instantiate 的 Player Prefab。
/// </summary>
public class MultiWinnerPanelPopulator : MonoBehaviour
{
    [Header("Prefab（Player 根物件：Image + ICON/Image + Name）")]
    public GameObject playerEntryPrefab;

    [Header("容器（勝利者列表的父物件）")]
    [Tooltip("MULTIPYWIN 底下裝 Player Prefab 的容器（通常是那顆 Image）")]
    public Transform playerListContainer;

    [Header("外觀")]
    [Tooltip("每個 Player entry 的 localScale（配合結算面板縮放）")]
    public float entryScale = 1f;

    /// <summary>由 GameUIManager.ShowMultiWinUI 寫入，OnEnable 會讀取</summary>
    public static int[] PendingWinnerIDs;

    // 只追蹤自己生的 entry，Clear 時不誤殺容器原生子物件
    private readonly List<GameObject> _spawned = new List<GameObject>();

    private void OnEnable()
    {
        Populate(PendingWinnerIDs);
    }

    /// <summary>用指定的勝利者 ID 陣列填充；null / 空陣列會清空列表</summary>
    public void Populate(int[] winnerIDs)
    {
        ClearSpawned();

        if (winnerIDs == null || winnerIDs.Length == 0) return;
        if (playerListContainer == null || playerEntryPrefab == null) return;

        foreach (var id in winnerIDs)
        {
            SpawnWinnerEntry(id);
        }
    }

    // ═══════════════════════════════════════════════
    //  Spawn helpers
    // ═══════════════════════════════════════════════

    private void SpawnWinnerEntry(int playerId)
    {
        GameObject go = Instantiate(playerEntryPrefab, playerListContainer);
        go.transform.localScale = Vector3.one * entryScale;
        _spawned.Add(go);

        // ── 頭像（ICON/Image）──
        var iconRoot = go.transform.Find("ICON");
        if (iconRoot != null)
        {
            // 抓 ICON 底下第一顆 Image 當頭像
            var iconImage = iconRoot.GetComponentInChildren<Image>(true);
            if (iconImage != null)
            {
                var avatar = LookupPlayerAvatar(playerId);
                if (avatar != null)
                {
                    iconImage.sprite = avatar;
                    iconImage.enabled = true;
                }
            }
        }

        // ── 玩家名稱（Name → TMP_Text）──
        var nameRoot = go.transform.Find("Name");
        TMP_Text nameText = null;
        if (nameRoot != null)
        {
            nameText = nameRoot.GetComponent<TMP_Text>();
            if (nameText == null) nameText = nameRoot.GetComponentInChildren<TMP_Text>(true);
        }
        // fallback：如果 prefab 結構改過，直接抓任一 TMP_Text
        if (nameText == null) nameText = go.GetComponentInChildren<TMP_Text>(true);

        if (nameText != null) nameText.text = LookupPlayerName(playerId);
    }

    private void ClearSpawned()
    {
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            if (_spawned[i] != null) Destroy(_spawned[i]);
        }
        _spawned.Clear();
    }

    // ═══════════════════════════════════════════════
    //  Player lookup
    // ═══════════════════════════════════════════════

    /// <summary>
    /// 找 PlayerID 對應的 PlayerIdentify：
    /// 先試 PlayerInventoryManager（Host 端有 init），找不到就掃整個場景（Client fallback）。
    /// </summary>
    private static PlayerIdentify FindPlayerIdentifyById(int playerId)
    {
        if (PlayerInventoryManager.Instance != null
            && PlayerInventoryManager.Instance.playerParents != null
            && PlayerInventoryManager.Instance.playerParents.Count > 0)
        {
            var playerObj = PlayerInventoryManager.Instance.GetPlayer(playerId);
            if (playerObj != null)
            {
                var pi = playerObj.GetComponent<PlayerIdentify>();
                if (pi != null && pi.PlayerID == playerId) return pi;
            }
        }

        // Client fallback：直接從場景掃 PlayerIdentify（PlayerID 是 [Networked]）
        var all = UnityEngine.Object.FindObjectsOfType<PlayerIdentify>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].PlayerID == playerId)
                return all[i];
        }
        return null;
    }

    private static string LookupPlayerName(int playerId)
    {
        var identify = FindPlayerIdentifyById(playerId);
        return identify != null && !string.IsNullOrEmpty(identify.PlayerName)
            ? identify.PlayerName
            : $"P{playerId}";
    }

    /// <summary>用 CardManager.AvatarData 查對應 SkinIndex 的頭像</summary>
    private static Sprite LookupPlayerAvatar(int playerId)
    {
        var identify = FindPlayerIdentifyById(playerId);
        int skinIdx = identify != null ? identify.SkinIndex : 0;

        var db = CardManager.Instance != null ? CardManager.Instance.AvatarData : null;
        return db != null ? db.GetAvatar(skinIdx) : null;
    }
}
