using System;
using System.Collections.Generic;
using UnityEngine;

public class CardPreviewSystem : MonoBehaviour
{
    public static CardPreviewSystem Instance { get; private set; }

    [Header("預覽位置")]
    public Transform previewAnchor;

    [Header("預覽物件列表")]
    public List<CardPreviewEntry> previewEntries = new List<CardPreviewEntry>();

    private GameObject currentPreview;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 傳入 Card 顯示對應預覽物件；傳入 null 則關閉預覽。
    /// </summary>
    public void ShowPreview(Card card)
    {
        HidePreview();

        if (card == null) return;

        CardPreviewEntry entry = previewEntries.Find(e => e.card == card);
        if (entry == null || entry.previewObject == null)
        {
            Debug.LogWarning($"[CardPreviewSystem] 找不到 {card.name} 對應的預覽物件");
            return;
        }

        currentPreview = Instantiate(entry.previewObject, previewAnchor.position, entry.previewObject.transform.rotation, previewAnchor);
    }

    /// <summary>
    /// 關閉目前顯示的預覽物件。
    /// </summary>
    public void HidePreview()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }
    }
}

[Serializable]
public class CardPreviewEntry
{
    public Card card;
    public GameObject previewObject;
}
