using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WiretapNotificationUI : MonoBehaviour
{
    [Header("UI 元件")]
    [SerializeField] private GameObject rootObject;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI cardNameText;

    [Header("顯示時間")]
    [SerializeField] private float displayDuration = 3f;

    private struct NotificationData
    {
        public CardData card;
        public int playerId;
    }

    private readonly Queue<NotificationData> pending = new Queue<NotificationData>();
    private bool isShowing = false;

    private void Start()
    {
        if (rootObject != null)
            rootObject.SetActive(false);
    }

    public void ShowNotification(CardData card, int playerId)
    {
        pending.Enqueue(new NotificationData { card = card, playerId = playerId });

        if (!isShowing)
            StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        isShowing = true;

        while (pending.Count > 0)
        {
            NotificationData data = pending.Dequeue();
            Card cardSO = CardManager.Instance?.GetCardScriptObject(data.card);

            if (iconImage != null)
                iconImage.sprite = cardSO != null ? cardSO.image : null;

            if (titleText != null)
                titleText.text = $"玩家({data.playerId}) 拿到了";

            if (cardNameText != null)
                cardNameText.text = cardSO != null ? cardSO.name : "Unknown";

            if (rootObject != null)
                rootObject.SetActive(true);

            yield return new WaitForSeconds(displayDuration);

            if (rootObject != null)
                rootObject.SetActive(false);
        }

        isShowing = false;
    }
}