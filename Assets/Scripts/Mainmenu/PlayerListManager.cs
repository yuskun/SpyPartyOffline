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

    private Button startGameBtn;

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
        
        // 只有房主可以點擊開始
        startGameBtn = root.Q<Button>("StartGameBtn");
        RefreshStartButtonAuthority();

        PlayerVersion = 0;
    }
    public void Check()
    {

        if (PlayerVersion != lastRevision)
        {
            lastRevision = PlayerVersion;
            OnPlayerListChanged();
        }
        if (PlayerVersion == 1 && !hasOpenedGameRoom)
        {
            Debug.Log("玩家列表已更新，顯示遊戲房間界面");
            string code = NetworkManager2.Instance != null ? NetworkManager2.Instance.CurrentRoomCode : "";
            GameMode mode = Runner.IsServer ? GameMode.Host : GameMode.Client;
            MenuUIManager.instance.ShowGameroom(mode, code);
            hasOpenedGameRoom = true;
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

    public void UnregisterPlayer(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;

        int key = player.AsIndex;
        if (PlayerNames.ContainsKey(key))
        {
            PlayerNames.Remove(key);
            Debug.Log($"🗑 移除玩家名稱 index={key}");
        }
        if (PlayerSkinIndexes.ContainsKey(key))
        {
            PlayerSkinIndexes.Remove(key);
            Debug.Log($"🗑 移除玩家皮膚 index={key}");
        }
        PlayerVersion++;
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

        var practiceUI = FindAnyObjectByType<PracticeUIManager>(FindObjectsInactive.Include);
        if (practiceUI != null && practiceUI.gameObject.activeInHierarchy)
        {
            practiceUI.RefreshAvatar();
        }
        RefreshStartButtonAuthority();
    }

    // 1. 這是對外的窗口：所有人都可以呼叫，但只有 Host 會執行內部的邏輯
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_RequestSkinUpdate(PlayerRef player, int skinIndex)
    {
        // 只有 Host 執行到這裡，確保資料能寫入 NetworkDictionary 並同步給所有人
        UpdateSkinIndex(player, skinIndex);
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

    // PlayerListManager.cs
    void Update()
    {
        // 只有在已經連線並生成 (Spawned) 後才檢查
        if (Object != null && Object.IsValid)
        {
            Check();
        }
    }

    // 新增一個方法專門處理按鈕權限
    private void RefreshStartButtonAuthority()
    {
        if (startGameBtn != null)
        {
            bool isHost = Runner.IsServer; // 或使用 Object.HasStateAuthority
            startGameBtn.SetEnabled(isHost);

            // 如果想讓 Client 看到不同的文字
            var btnLabel = startGameBtn.Q<Label>(className: "btn-text");
            if (btnLabel != null)
            {
                btnLabel.text = isHost ? "開始遊戲" : "等待房主開始";
            }
        }
    }
}
