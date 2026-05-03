using UnityEngine;

public class GivePeek : MonoBehaviour
{
    [Tooltip("拖 Assets/Scripts/Card/AllCard/Peek.asset 進來")]
    [SerializeField] private Card cardToGive;

    private bool hasGiven = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasGiven) return;
        if (!other.CompareTag("Player")) return;
        // 必須是真的 LocalPlayer（避免 AI 誤觸發）
        if (other.transform.root.GetComponent<OodlesEngine.LocalPlayer>() == null) return;

        GiveItem(other.transform);
        hasGiven = true;

        // ⭐ 通知教學系統
        NotifyTutorial();

        // 可選：消失
        gameObject.SetActive(false);
    }

    void GiveItem(Transform playerTransform)
    {
        if (cardToGive == null)
        {
            Debug.LogWarning("[GivePeek] 未設定 cardToGive (請拖 Peek.asset)");
            return;
        }
        // 找最近的 TutorialInventory（玩家身上）
        var inv = playerTransform.root.GetComponentInChildren<TutorialInventory>();
        if (inv == null) inv = Object.FindFirstObjectByType<TutorialInventory>();
        if (inv == null) { Debug.LogWarning("[GivePeek] 找不到 TutorialInventory"); return; }
        bool added = inv.AddCard(cardToGive.cardData);
        Debug.Log($"[GivePeek] 給 Peek 卡 → {(added ? "成功" : "背包滿")}");
    }

    void NotifyTutorial()
    {
        if (TutorialManager.Instance != null) TutorialManager.Instance.OnPickPeek();
    }
}
