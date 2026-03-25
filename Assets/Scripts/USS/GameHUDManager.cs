using UnityEngine;
using UnityEngine.UIElements;

public class GameHUDManager : MonoBehaviour
{
    public static GameHUDManager Instance;
    private Label _topTimeLabel;
    public RenderTexture minimapRenderTexture;

    private void Awake()
    {
        Instance = this;
    }
    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _topTimeLabel = root.Q<Label>("TopTime");

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
    }

    public void SetTopTime(string timeText)
    {
        if (_topTimeLabel != null)
            _topTimeLabel.text = timeText;
    }
}