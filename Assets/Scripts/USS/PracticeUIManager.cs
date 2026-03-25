using UnityEngine;
using UnityEngine.UIElements;

public class PracticeUIManager : MonoBehaviour
{
    private VisualElement avatarElement;
    private Button micBtn;
    private bool micOn = true;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // 頭像
        avatarElement = root.Q<VisualElement>(className: "avatar");
        RefreshAvatar();

        // Mic
        micBtn = root.Q<Button>("MicBtn");
        if (micBtn != null)
            micBtn.clicked += ToggleMic;
    }

    void OnDisable()
    {
        if (micBtn != null)
            micBtn.clicked -= ToggleMic;
    }

    public void RefreshAvatar()
    {
        if (avatarElement == null) return;
        if (SkinChange.instance?.characterAvatarDatabase == null) return;

        int skinIndex = PlayerPrefs.GetInt("Choosenindex", 0);
        Sprite avatar = SkinChange.instance.characterAvatarDatabase.GetAvatar(skinIndex);
        if (avatar != null)
            avatarElement.style.backgroundImage = new StyleBackground(avatar);
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
}