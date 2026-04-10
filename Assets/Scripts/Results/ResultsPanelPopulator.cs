using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 結算面板填充器：把 <see cref="WinnerData"/> 的任務卡、道具使用、擊倒紀錄
/// 轉成 Prefab 塞進 MISSIONCOMPLETE / ITEMUSE / KNOCKDOWN 三個 H 容器底下。
///
/// 預期 Prefab 結構：
///   Root (Image)
///     └─ Text (TMP)     ← 抓子層第一個 TMP_Text 填字
///
/// 使用方式：
///   1. 掛在 ResultsPanel（或任一子物件）上。
///   2. 在 Inspector 把 entryPrefab 跟三個 H 容器拖進來。
///   3. GameUIManager.ShowResultsPanel() 會 SetActive(true)，OnEnable 自動抓
///      <see cref="GameManager.CurrentWinnerData"/> 填充。
///
/// ⚠️ 目前 CurrentWinnerData 只有 Host 會在 RPC_Gameover 裡 BuildWinnerData，
///    Client 端會拿到 null → 三區會顯示空的。若要 Client 也能看，需要把
///    WinnerData 同步下去（例如透過 RPC 或 Networked 結構）。
/// </summary>
public class ResultsPanelPopulator : MonoBehaviour
{
    [Header("Prefab（根：Image，子：TMP Text）")]
    public GameObject entryPrefab;

    [Header("容器（每個區塊的 H 父物件）")]
    [Tooltip("MISSIONCOMPLETE/H")] public Transform missionContainer;
    [Tooltip("ITEMUSE/H")]         public Transform itemUseContainer;
    [Tooltip("KNOCKDOWN/H")]       public Transform knockdownContainer;

    [Header("勝利玩家資訊")]
    [Tooltip("勝利玩家頭像 Image")]  public Image winnerAvatarImage;
    [Tooltip("勝利玩家名稱 TMP_Text")] public TMP_Text winnerNameText;

    // ⚠️ 只追蹤「自己 Instantiate 出來的」entry，Clear 的時候只銷毀這些。
    //    這樣就算 Inspector 不小心把 container 拖到 MISSIONCOMPLETE 而非 MISSIONCOMPLETE/H，
    //    也不會把旁邊的 H / Image 一起炸掉。
    private readonly List<GameObject> _spawnedMission   = new List<GameObject>();
    private readonly List<GameObject> _spawnedItemUse   = new List<GameObject>();
    private readonly List<GameObject> _spawnedKnockdown = new List<GameObject>();

    private void OnEnable()
    {
        // ResultsPanel 每次被 SetActive(true) 時都會觸發這裡
        Populate(GameManager.CurrentWinnerData);
    }

    /// <summary>用指定的 <see cref="WinnerData"/> 填三個區塊；null 會把三區清空</summary>
    public void Populate(WinnerData data)
    {
        ClearSpawned(_spawnedMission);
        ClearSpawned(_spawnedItemUse);
        ClearSpawned(_spawnedKnockdown);

        // 先把勝利玩家頭像 / 名稱清空（data 為 null 時維持空狀態）
        if (winnerAvatarImage != null)
        {
            winnerAvatarImage.sprite = null;
            winnerAvatarImage.enabled = false;
        }
        if (winnerNameText != null)
        {
            winnerNameText.text = string.Empty;
        }

        if (data == null) return;

        // ── 勝利玩家頭像 + 名稱 ──
        if (winnerAvatarImage != null)
        {
            var avatar = LookupPlayerAvatar(data.winnerID);
            if (avatar != null)
            {
                winnerAvatarImage.sprite = avatar;
                winnerAvatarImage.enabled = true;
            }
        }
        if (winnerNameText != null)
        {
            winnerNameText.text = LookupPlayerName(data.winnerID);
        }

        // ── 完成任務 ──
        foreach (var m in data.missionCards)
        {
            string title = LookupCardName(m.card);
            SpawnEntry(missionContainer, m.image, title, _spawnedMission);
        }

        // ── 道具使用 ──
        foreach (var u in data.cardUsages)
        {
            string label = $"{LookupCardName(u.card)} x{u.useCount}";
            SpawnEntry(itemUseContainer, u.image, label, _spawnedItemUse);
        }

        // ── 擊倒玩家 ──
        foreach (var k in data.knockdowns)
        {
            Sprite avatar = LookupPlayerAvatar(k.targetPlayerId);
            string label  = $"{LookupPlayerName(k.targetPlayerId)} x{k.knockdownCount}";
            SpawnEntry(knockdownContainer, avatar, label, _spawnedKnockdown);
        }
    }

    // ═══════════════════════════════════════════════
    //  Prefab spawn / helpers
    // ═══════════════════════════════════════════════

    private void SpawnEntry(Transform parent, Sprite sprite, string text, List<GameObject> tracker)
    {
        if (parent == null || entryPrefab == null) return;

        GameObject go = Instantiate(entryPrefab, parent);
        // 結算面板裡的 entry 需要縮成 0.35 才會剛好填進 H 的輪播框裡
        go.transform.localScale = Vector3.one * 0.35f;
        if (tracker != null) tracker.Add(go);

        // Prefab 根是 Image → 設圖
        var rootImg = go.GetComponent<Image>();
        if (rootImg != null && sprite != null)
        {
            rootImg.sprite = sprite;
            rootImg.enabled = true;
        }

        // 抓子層第一個 TMP Text（名字可能是 Text / Text (TMP) / TEXT，不硬編名字）
        var label = go.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = text;
        }
    }

    /// <summary>
    /// 只銷毀自己追蹤過的 entry，不碰 parent 的其他原生子物件（例如 H、Image）。
    /// </summary>
    private static void ClearSpawned(List<GameObject> spawned)
    {
        if (spawned == null) return;
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            if (spawned[i] != null) Destroy(spawned[i]);
        }
        spawned.Clear();
    }

    // ═══════════════════════════════════════════════
    //  Card / Player lookup
    // ═══════════════════════════════════════════════

    /// <summary>從 CardManager.Catalog 反查卡片名稱</summary>
    private static string LookupCardName(CardData card)
    {
        var catalog = CardManager.Instance != null ? CardManager.Instance.Catalog : null;
        if (catalog == null || catalog.cards == null) return string.Empty;

        var c = catalog.cards.Find(x => x.cardData.id == card.id && x.cardData.type == card.type);
        if (c == null) return string.Empty;

        // Card.name 是自訂字段；若空的就 fallback 到 type 名
        return !string.IsNullOrEmpty(c.name) ? c.name : c.GetType().Name;
    }

    /// <summary>
    /// 找 PlayerID 對應的 PlayerIdentify：
    /// 先試 PlayerInventoryManager（Host 端有 init），找不到就掃整個場景（Client fallback）。
    /// </summary>
    private static PlayerIdentify FindPlayerIdentifyById(int playerId)
    {
        // Host 路徑：PlayerInventoryManager.GetPlayer(index) 的 index 等同於 PlayerID
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

        // Client fallback：PlayerInventoryManager 沒 init，直接從場景掃 PlayerIdentify
        // （PlayerID / PlayerName / SkinIndex 都是 [Networked]，Client 掃到的就是同步後的值）
        var all = UnityEngine.Object.FindObjectsOfType<PlayerIdentify>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].PlayerID == playerId)
                return all[i];
        }
        return null;
    }

    /// <summary>從場景 PlayerIdentify 查玩家顯示名稱</summary>
    private static string LookupPlayerName(int playerId)
    {
        var identify = FindPlayerIdentifyById(playerId);
        return identify != null && !string.IsNullOrEmpty(identify.PlayerName)
            ? identify.PlayerName
            : $"P{playerId}";
    }

    /// <summary>
    /// 從 CardManager.AvatarData 取玩家頭像（以玩家的 SkinIndex 查 CharacterAvatarData）。
    /// </summary>
    private static Sprite LookupPlayerAvatar(int playerId)
    {
        var identify = FindPlayerIdentifyById(playerId);
        int skinIdx = identify != null ? identify.SkinIndex : 0;

        var db = CardManager.Instance != null ? CardManager.Instance.AvatarData : null;
        return db != null ? db.GetAvatar(skinIdx) : null;
    }
}
