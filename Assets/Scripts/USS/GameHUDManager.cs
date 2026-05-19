using System.Diagnostics;
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
    
    private bool _isHintsCollapsed = false; 
    private VisualElement _hintsContainer;
    private Label _toggleHintLabel;

    private void Awake()
    {  if(Instance != null && Instance != this)
        {
            UnityEngine.Debug.LogWarning("GameHUDManager 已存在，正在銷毀重複的實例。");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    void OnEnable()
    {
        InitHUD();
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

        _hintsContainer = root.Q<VisualElement>("ControlHints");
        _toggleHintLabel = root.Q<Label>("ToggleHintText");
        ApplyHintState(); // 每次開啟面板時，根據記錄的 _isHintsCollapsed 恢復 UI

        //頭像
        var database = Resources.Load<CharacterAvatarData>("Characters/CharacterAvatarData");
        if (database != null)
        {
            // 從 PlayerPrefs 讀取玩家選定的 Index (這是跨場景傳遞最簡單的方式)
            int skinIndex = PlayerPrefs.GetInt("Choosenindex", 0);

            Sprite avatar = database.GetAvatar(skinIndex);
            if (avatar != null)
            {
                var avatarImg = root.Q<UnityEngine.UIElements.Image>(className: "avatar-img");
                if (avatarImg != null)
                {
                    avatarImg.sprite = avatar;
                }
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
        // SetActive / 顯示重建後，舊引用可能 detach（panel == null），需重新抓
        if (_playerNameLabel == null || _playerNameLabel.panel == null)
        {
            var doc = GetComponent<UIDocument>();
            if (doc != null && doc.rootVisualElement != null)
                _playerNameLabel = doc.rootVisualElement.Q<Label>(className: "player-name");
        }
        if (_playerNameLabel == null) return;

        string pName = null;

        // 優先：從本地玩家的 PlayerIdentify 讀（[Networked]，server 同步，client 一樣有正確值）
        var localIdentify = FindLocalPlayerIdentify();
        if (localIdentify != null && !string.IsNullOrEmpty(localIdentify.PlayerName))
            pName = localIdentify.PlayerName;

        // 後備：本地 typed name（NetworkManager2，有可能 timing 沒齊）
        if (string.IsNullOrEmpty(pName) && NetworkManager2.Instance != null)
            pName = NetworkManager2.Instance.PlayerName;

        if (string.IsNullOrEmpty(pName)) pName = "Guest";

        _playerNameLabel.text = pName;
        UnityEngine.Debug.Log($"[GameHUD] 名字已更新為: {pName}");
    }

    /// <summary>找場景中具有 InputAuthority 的玩家（即本地玩家自己）</summary>
    private PlayerIdentify FindLocalPlayerIdentify()
    {
        var all = FindObjectsOfType<PlayerIdentify>();
        for (int i = 0; i < all.Length; i++)
        {
            var p = all[i];
            if (p == null) continue;
            // PlayerIdentify : NetworkBehaviour，所以可以直接讀 HasInputAuthority
            if (p.HasInputAuthority) return p;
        }
        return null;
    }

    public void ToggleControlHints()
    {
        _isHintsCollapsed = !_isHintsCollapsed;
        ApplyHintState();
    }
    private void ApplyHintState()
    {
        if (_hintsContainer == null) return;

        if (_isHintsCollapsed)
        {
            _hintsContainer.AddToClassList("collapsed");
            if (_toggleHintLabel != null) _toggleHintLabel.text = "開啟操作提示";
        }
        else
        {
            _hintsContainer.RemoveFromClassList("collapsed");
            if (_toggleHintLabel != null) _toggleHintLabel.text = "關閉操作提示";
        }
    }
}