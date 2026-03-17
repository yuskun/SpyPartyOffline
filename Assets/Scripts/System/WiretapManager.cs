using System.Collections.Generic;
using UnityEngine;

public class WiretapManager : MonoBehaviour
{
    public static WiretapManager Instance;

    private WiretapNotificationUI notificationUI;

    private PlayerInventory watchTarget;
    private CardData[] lastSnapshot;
    private float expiryTime;
    private int lastVersion = -1;
    private bool isActive = false;
    private int currentTargetPlayerId = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        TryCacheNotificationUI();
    }

    private void TryCacheNotificationUI()
    {
        if (notificationUI != null) return;

        if (GameUIManager.Instance == null)
        {
            Debug.LogWarning("[Wiretap] GameUIManager.Instance 為 null");
            return;
        }

        if (GameUIManager.Instance.Notification == null)
        {
            Debug.LogWarning("[Wiretap] GameUIManager.Notification 未指定");
            return;
        }

        notificationUI = GameUIManager.Instance.Notification.GetComponent<WiretapNotificationUI>();

        if (notificationUI == null)
        {
            Debug.LogWarning("[Wiretap] Notification 物件上找不到 WiretapNotificationUI");
        }
    }

    public void StartWiretap(int targetPlayerId, float duration)
    {
        GameObject targetObj = PlayerInventoryManager.Instance.GetPlayer(targetPlayerId);
        if (targetObj == null)
        {
            Debug.LogWarning($"[Wiretap] 找不到 targetPlayerId={targetPlayerId}");
            return;
        }

        TryCacheNotificationUI();

        watchTarget = targetObj.GetComponent<PlayerInventory>();
        if (watchTarget == null)
        {
            Debug.LogWarning($"[Wiretap] targetPlayerId={targetPlayerId} 沒有 PlayerInventory");
            return;
        }

        lastSnapshot = CloneCards(watchTarget.GetAllCards());
        lastVersion = watchTarget.InventoryVersion;
        expiryTime = Time.time + duration;
        currentTargetPlayerId = targetPlayerId;
        isActive = true;

        Debug.Log($"[Wiretap] 開始監聽 player={targetPlayerId}，持續 {duration} 秒");
    }

    private void Update()
    {
        if (!isActive || watchTarget == null)
            return;

        if (Time.time > expiryTime)
        {
            StopWiretap();
            return;
        }

        int currentVersion = watchTarget.InventoryVersion;
        if (currentVersion == lastVersion)
            return;

        lastVersion = currentVersion;

        CardData[] current = CloneCards(watchTarget.GetAllCards());
        List<CardData> newCards = FindNewCards(lastSnapshot, current);
        lastSnapshot = current;

        foreach (var card in newCards)
        {
            notificationUI?.ShowNotification(card, currentTargetPlayerId);
        }
    }

    private void StopWiretap()
    {
        isActive = false;
        watchTarget = null;
        lastSnapshot = null;
        lastVersion = -1;
        currentTargetPlayerId = -1;

        Debug.Log("[Wiretap] 竊聽結束");
    }

    private CardData[] CloneCards(CardData[] source)
    {
        if (source == null)
            return null;

        CardData[] copy = new CardData[source.Length];
        System.Array.Copy(source, copy, source.Length);
        return copy;
    }

    private List<CardData> FindNewCards(CardData[] prev, CardData[] curr)
    {
        var result = new List<CardData>();

        if (curr == null)
            return result;

        if (prev == null)
            prev = new CardData[0];

        var prevFreq = new Dictionary<(CardType, int), int>();
        var currFreq = new Dictionary<(CardType, int), int>();

        foreach (var c in prev)
        {
            if (c.IsEmpty()) continue;

            var key = (c.type, c.id);
            prevFreq.TryGetValue(key, out int count);
            prevFreq[key] = count + 1;
        }

        foreach (var c in curr)
        {
            if (c.IsEmpty()) continue;

            var key = (c.type, c.id);
            currFreq.TryGetValue(key, out int count);
            currFreq[key] = count + 1;
        }

        foreach (var kvp in currFreq)
        {
            prevFreq.TryGetValue(kvp.Key, out int prevCount);
            int addedCount = kvp.Value - prevCount;

            if (addedCount <= 0)
                continue;

            for (int i = 0; i < addedCount; i++)
            {
                result.Add(new CardData
                {
                    type = kvp.Key.Item1,
                    id = kvp.Key.Item2
                });
            }
        }

        return result;
    }
}