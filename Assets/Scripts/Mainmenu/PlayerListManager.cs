using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.UIElements;
using System.Collections.Generic;


public class PlayerListManager : NetworkBehaviour
{
    [Networked, Capacity(8)]
    public NetworkDictionary<int, NetworkString<_16>> PlayerNames { get; }
    [Networked] public int PlayerVersion { get; set; }
    public int lastRevision = 0;
    public Color otherPlayerColor;
    public Color myPlayerColor;
    private bool hasOpenedGameRoom = false;
    private TextMeshProUGUI[] nameTexts;

    private List<VisualElement> slotElements = new List<VisualElement>();
    [Networked, Capacity(8)]
    public NetworkDictionary<int, int> PlayerSkinIndexes { get; } // key=PlayerRef.AsIndex, value=skinIndex


    public override void Spawned()
    {
        MenuUIManager.instance.playerlistmanager= this;
        int count = MenuUIManager.instance.PlayerList.transform.childCount;
        nameTexts = new TextMeshProUGUI[count];
        for (int i = 0; i < count; i++)
        {
            nameTexts[i] =  MenuUIManager.instance.PlayerList.transform.GetChild(i).GetComponentInChildren<TextMeshProUGUI>(true);
            nameTexts[i].text = "空間";
        }

        // ── 新版 UI Toolkit 初始化 ──
        var root = MenuUIManager.instance.hostRoomDocument.rootVisualElement;
        slotElements.Clear();
        root.Query<VisualElement>(className: "slot").ForEach(slot => slotElements.Add(slot));
        Debug.Log($"找到 {slotElements.Count} 個 slot");

        PlayerVersion = 0;
    }
    public void Check()
    {

        if (PlayerVersion != lastRevision)
        {
            lastRevision = PlayerVersion;
            OnPlayerListChanged();
        }
        if (PlayerVersion == 1&&!hasOpenedGameRoom)
        {
            MenuUIManager.instance.ShowGameroom(GameMode.Host, NetworkManager2.Instance != null ? NetworkManager2.Instance.CurrentRoomCode : "");
            hasOpenedGameRoom=true;
        }
    }
    public void RegisterPlayer(PlayerRef player, string playerName, int skinIndex)
    {

        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning("只有 StateAuthority (Host) 才能修改 NetworkArray");
            return;
        }

        if (!string.IsNullOrEmpty(playerName))
        {
            PlayerNames.Set(player.AsIndex, playerName);
            PlayerSkinIndexes.Set(player.AsIndex, skinIndex);
            Debug.Log($"✅ 註冊玩家 {playerName} 角色{skinIndex} 到索引 {player.AsIndex}");
            PlayerVersion++;
        }
    }

    public void OnPlayerListChanged()
    {
        Debug.Log("玩家列表已更新");

        int Slotindex=0;

        for (int i = 0; i < 8; i++)
        {
            nameTexts[i].text = "空間";
            nameTexts[i].color = Color.black;
        }
        foreach (var kvp in PlayerNames)
        {
            string name = kvp.Value.ToString();
            nameTexts[Slotindex].text = name;
            nameTexts[Slotindex].color = kvp.Key == Runner.LocalPlayer.AsIndex ? myPlayerColor : otherPlayerColor;
            Slotindex++;
        }
        
        // 新版UI(可直接取代) ──
        for (int i = 0; i < slotElements.Count; i++)
        {
            ResetSlotToEmpty(slotElements[i]);
        }

        int slotIndex = 0;
        foreach (var kvp in PlayerNames)
        {
            if (slotIndex >= slotElements.Count) break;

            string playerName = kvp.Value.ToString();
            bool isHost = (slotIndex == 0);
            bool isLocal = (kvp.Key == Runner.LocalPlayer.AsIndex);

            // skinIndex 預設 0，找不到也不會炸
            PlayerSkinIndexes.TryGet(kvp.Key, out int skinIndex);

            SetSlotOccupied(slotElements[slotIndex], playerName, skinIndex, isHost, isLocal);
            slotIndex++;
        }
    }

    public void UpdateSkinIndex(PlayerRef player, int skinIndex)
    {
        if (!Object.HasStateAuthority) return;
        PlayerSkinIndexes.Set(player.AsIndex, skinIndex);
        PlayerVersion++; // 觸發 UI 刷新
    }

    // ── 空閒狀態 ──
    void ResetSlotToEmpty(VisualElement slot)
    {
        slot.AddToClassList("empty");
        slot.RemoveFromClassList("is-host");

        // 名字
        var nameLabel = slot.Q<Label>(className: "player-name");
        if (nameLabel != null) nameLabel.text = "空閒";

        // YOU tag 隱藏
        var youTag = slot.Q<Label>(className: "you-tag");
        if (youTag != null) youTag.style.display = DisplayStyle.None;

        // host-badge 隱藏
        var hostBadge = slot.Q<VisualElement>(className: "host-badge");
        if (hostBadge != null) hostBadge.style.display = DisplayStyle.None;

        // avatar-wrap 隱藏
        var avatarWrap = slot.Q<VisualElement>(className: "avatar-wrap");
        if (avatarWrap != null) avatarWrap.style.display = DisplayStyle.None;
    }

    // ── 有玩家的狀態 ──
    void SetSlotOccupied(VisualElement slot, string playerName, int skinIndex, bool isHost, bool isLocal)
    {
        slot.RemoveFromClassList("empty");
        
        // avatar-wrap 顯示
        var avatarWrap = slot.Q<VisualElement>(className: "avatar-wrap");
        if (avatarWrap != null) avatarWrap.style.display = DisplayStyle.Flex;

        // 名字
        var nameLabel = slot.Q<Label>(className: "player-name");
        if (nameLabel != null) nameLabel.text = playerName;

        // 頭像
        var avatarImg = slot.Q<UnityEngine.UIElements.Image>(className: "avatar-img");
        if (avatarImg != null && SkinChange.instance?.characterAvatarDatabase != null)
        {
            Debug.Log($"正在為玩家 {playerName} 設定頭像，Index: {skinIndex}");
            avatarImg.sprite = SkinChange.instance.characterAvatarDatabase.GetAvatar(skinIndex);
        }
        
        // YOU tag
        var youTag = slot.Q<Label>(className: "you-tag");
        if (youTag != null)
            youTag.style.display = isLocal ? DisplayStyle.Flex : DisplayStyle.None;

        // host-badge
        var hostBadge = slot.Q<VisualElement>(className: "host-badge");
        if (hostBadge != null)
            hostBadge.style.display = isHost ? DisplayStyle.Flex : DisplayStyle.None;

        if (isHost) slot.AddToClassList("is-host");
        else slot.RemoveFromClassList("is-host");
    }

}
