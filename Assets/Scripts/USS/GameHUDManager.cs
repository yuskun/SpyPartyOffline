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
    {
        Instance = this;
    }
    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _topTimeLabel = root.Q<Label>("TopTime");

        _playerNameLabel = root.Q<Label>(className: "player-name");
        RefreshPlayerName(); // 執行名字更新

        var mapContent = root.Q<VisualElement>(className: "map-content");
        if (mapContent != null)
            mapContent.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(minimapRenderTexture));
        
        // 頭像部分
        if (SkinChange.instance?.characterAvatarDatabase == null) return;

        int skinIndex = PlayerPrefs.GetInt("Choosenindex", 0);
        Sprite avatar = SkinChange.instance.characterAvatarDatabase.GetAvatar(skinIndex);
        if (avatar == null) return;

        var avatarImg = root.Q<UnityEngine.UIElements.Image>(className: "avatar-img");
        if (avatarImg != null)
            avatarImg.sprite = avatar;

        // Mic
        micBtn = root.Q<Button>("MicBtn");
        if (micBtn != null){
            micBtn.focusable = false; // 關鍵：禁止聚焦
            micBtn.clicked += ToggleMic;
        }
    }

    public void SetTopTime(string timeText)
    {
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