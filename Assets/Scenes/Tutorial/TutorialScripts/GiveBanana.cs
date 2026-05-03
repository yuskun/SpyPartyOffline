using UnityEngine;

public class Givebanana : MonoBehaviour
{
    [Tooltip("拖 Assets/Scripts/Card/AllCard/Banana.asset 進來")]
    [SerializeField] private Card cardToGive;

    private bool hasGiven = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasGiven) return;
        if (!other.CompareTag("Player")) return;
        if (other.transform.root.GetComponent<OodlesEngine.LocalPlayer>() == null) return;

        GiveItem(other.transform);
        hasGiven = true;

        NotifyTutorial();
        gameObject.SetActive(false);
    }

    void GiveItem(Transform playerTransform)
    {
        if (cardToGive == null) { Debug.LogWarning("[Givebanana] 未設定 cardToGive"); return; }
        var inv = playerTransform.root.GetComponentInChildren<TutorialInventory>();
        if (inv == null) inv = Object.FindFirstObjectByType<TutorialInventory>();
        if (inv == null) { Debug.LogWarning("[Givebanana] 找不到 TutorialInventory"); return; }
        bool added = inv.AddCard(cardToGive.cardData);
        Debug.Log($"[Givebanana] 給 Banana 卡 → {(added ? "成功" : "背包滿")}");
    }

    void NotifyTutorial()
    {
        if (TutorialManager.Instance != null) TutorialManager.Instance.OnPickBanana();
    }
}
