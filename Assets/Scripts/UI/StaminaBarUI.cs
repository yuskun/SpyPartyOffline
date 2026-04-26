using UnityEngine;
using UnityEngine.UI;
using OodlesEngine;

/// <summary>
/// 顯示本地玩家耐力值的 UI 條。
/// 把這個腳本掛在 Canvas 底下任一物件上，把 Image (Filled) 拉到 fillImage 即可。
/// </summary>
public class StaminaBarUI : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Image 的 Image Type 要設成 Filled，本腳本會改其 fillAmount")]
    [SerializeField] private Image fillImage;

    [Header("行為")]
    [Tooltip("耐力滿時是否自動隱藏整個 GameObject")]
    [SerializeField] private bool hideWhenFull = false;
    [Tooltip("數值平滑速度（每秒填充比例的最大改變量）")]
    [SerializeField, Range(1f, 20f)] private float lerpSpeed = 8f;

    [Header("低耐力警示")]
    [SerializeField] private Color normalColor = new Color(0.4f, 0.9f, 0.4f, 1f);
    [SerializeField] private Color lowColor = new Color(0.9f, 0.3f, 0.3f, 1f);
    [SerializeField, Range(0f, 1f)] private float lowThreshold = 0.3f;

    private OodlesCharacter _localChar;
    private float _displayedRatio;

    void Update()
    {
        if (_localChar == null)
        {
            _localChar = FindLocalCharacter();
            if (_localChar == null) return;
        }

        float target = _localChar.EnergyRatio;
        _displayedRatio = Mathf.MoveTowards(_displayedRatio, target, lerpSpeed * Time.deltaTime);

        if (fillImage != null)
        {
            fillImage.fillAmount = _displayedRatio;
            fillImage.color = (target < lowThreshold) ? lowColor : normalColor;
        }

        if (hideWhenFull)
        {
            bool shouldShow = target < 0.999f;
            if (gameObject.activeSelf != shouldShow)
                gameObject.SetActive(shouldShow);
        }
    }

    /// <summary>
    /// 從 SkinChange.SpawnedPlayers 找出 LocalPlayer 對應的 OodlesCharacter。
    /// 找不到就退而求其次：抓場景上第一個 OodlesCharacter（單人/練習場用）。
    /// </summary>
    private OodlesCharacter FindLocalCharacter()
    {
        // 多人連線時：透過 SkinChange 取本地角色
        var skin = SkinChange.instance;
        if (skin != null && skin.Runner != null)
        {
            foreach (var no in skin.SpawnedPlayers)
            {
                if (no == null) continue;
                var np = no.GetComponent<NetworkPlayer>();
                if (np != null && np.PlayerId == skin.Runner.LocalPlayer)
                {
                    return no.GetComponent<OodlesCharacter>();
                }
            }
        }

        // Fallback：場景第一個 OodlesCharacter（單人練習場）
        return FindObjectOfType<OodlesCharacter>();
    }
}
