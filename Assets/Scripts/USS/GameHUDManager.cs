using UnityEngine;
using UnityEngine.UIElements;

public class GameHUDManager : MonoBehaviour
{
    public static GameHUDManager Instance;
    private Label _topTimeLabel;
    private Label _playerNameLabel;
    public RenderTexture minimapRenderTexture;
    private Button micBtn;
    private bool micOn = true;

    private void Awake()
    {  if(Instance != null && Instance != this)
        {
            Debug.LogWarning("GameHUDManager 已存在，正在銷毀重複的實例。");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    /// <summary>重新綁定所有 UI 元素引用（開始遊戲時呼叫一次）</summary>
    public void InitHUD()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null || doc.rootVisualElement == null) return;
        var root = doc.rootVisualElement;

        _topTimeLabel = root.Q<Label>("TopTime");
        _playerNameLabel = root.Q<Label>(className: "player-name");
        RefreshPlayerName();

        var mapContent = root.Q<VisualElement>(className: "map-content");
        if (mapContent != null)
            mapContent.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(minimapRenderTexture));

        // 頭像
        if (SkinChange.instance != null && SkinChange.instance.characterAvatarDatabase != null)
        {
            int skinIndex = PlayerPrefs.GetInt("Choosenindex", 0);
            Sprite avatar = SkinChange.instance.characterAvatarDatabase.GetAvatar(skinIndex);
            if (avatar != null)
            {
                var avatarImg = root.Q<UnityEngine.UIElements.Image>(className: "avatar-img");
                if (avatarImg != null)
                    avatarImg.sprite = avatar;
            }
        }

        // Mic
        micBtn = root.Q<Button>("MicBtn");
        if (micBtn != null)
        {
            micBtn.focusable = false;
            micBtn.clicked += ToggleMic;
        }
    }

    public void SetTopTime(string timeText)
    {
        // SetActive 重建視覺樹後，舊引用會脫離（parent == null），需重新抓
        if (_topTimeLabel == null || _topTimeLabel.panel == null)
        {
            var doc = GetComponent<UIDocument>();
            if (doc != null && doc.rootVisualElement != null)
                _topTimeLabel = doc.rootVisualElement.Q<Label>("TopTime");
        }

        if (_topTimeLabel != null)
            _topTimeLabel.text = timeText;
    }

    void ToggleMic()
    {
        micOn = !micOn;
        if (micOn)
        {
            micBtn.AddToClassList("is-open");
            micBtn.RemoveFromClassList("is-off");
        }
        else
        {
            micBtn.AddToClassList("is-off");
            micBtn.RemoveFromClassList("is-open");
        }
    }

    public void RefreshPlayerName()
    {
        if (_playerNameLabel == null) return;

        // 沿用妳在 Practice 使用的邏輯：優先從 NetworkManager2 讀取
        string pName = NetworkManager2.Instance != null ? NetworkManager2.Instance.PlayerName : "shaya";
        
        // 保底預設值
        if (string.IsNullOrEmpty(pName)) pName = "Guest";

        _playerNameLabel.text = pName;
        Debug.Log($"[GameHUD] 名字已更新為: {pName}");
    }
}