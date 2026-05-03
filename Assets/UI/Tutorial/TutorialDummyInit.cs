using UnityEngine;

/// <summary>
/// 教學假人初始化：在 Start 時把指定的 Card 放進自己的 TutorialInventory。
/// 用來預先給假人持有「小偷任務卡」之類的，方便教學中 Peek 看穿時有東西可看。
/// </summary>
[RequireComponent(typeof(TutorialInventory))]
public class TutorialDummyInit : MonoBehaviour
{
    [Tooltip("依序對應 6 個 slot（多的會忽略）。可空格表示該格留空。")]
    [SerializeField] private Card[] startingCards = new Card[TutorialInventory.MaxSlots];

    void Start()
    {
        var inv = GetComponent<TutorialInventory>();
        if (inv == null || startingCards == null) return;

        int n = Mathf.Min(startingCards.Length, TutorialInventory.MaxSlots);
        for (int i = 0; i < n; i++)
        {
            var c = startingCards[i];
            if (c == null) continue;
            inv.SetCardAt(i, c.cardData);
        }
    }
}
