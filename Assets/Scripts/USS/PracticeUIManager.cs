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
        if (micBtn != null){
            micBtn.focusable = false; // 關鍵：禁止聚焦
            micBtn.clicked += ToggleMic;
        }
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

        // 取得本地玩家的正確索引
        // 優先從 NetworkRunner 取得當前玩家在房間內同步的 SkinIndex
        int skinIndex = 0;

        // 方案 A: 如果你有 PlayerListManager 在場景中，從同步字典抓
        if (MenuUIManager.instance?.playerlistmanager != null)
        {
            var runner = NetworkManager2.Instance.runner;
            if (MenuUIManager.instance.playerlistmanager.PlayerSkinIndexes.TryGet(runner.LocalPlayer.AsIndex, out int networkedIndex))
            {
                skinIndex = networkedIndex;
            }
        }
        else
        {
            // 方案 B: 保底方案，才讀取本地 PlayerPrefs
            skinIndex = PlayerPrefs.GetInt("Choosenindex", 0); 
        }

        Sprite avatar = SkinChange.instance.characterAvatarDatabase.GetAvatar(skinIndex); 
        if (avatar != null){
            avatarElement.style.backgroundImage = new StyleBackground(avatar);
            avatarElement.MarkDirtyRepaint();
            Debug.Log($"[Client] Practice 頭像已更新為 Index: {skinIndex}");
        }
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