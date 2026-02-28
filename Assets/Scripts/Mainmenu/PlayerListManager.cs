using UnityEngine;
using Fusion;
using TMPro;

public class PlayerListManager : NetworkBehaviour
{
    [Networked, Capacity(8)]
    public NetworkDictionary<int, NetworkString<_16>> PlayerNames { get; }
    [Networked] public int PlayerVersion { get; set; }
    public int lastRevision = 0;



    public Color otherPlayerColor;
    public Color myPlayerColor;


    private TextMeshProUGUI[] nameTexts;


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
        PlayerVersion = 0;
    }
    public void Check()
    {

        if (PlayerVersion != lastRevision)
        {
            lastRevision = PlayerVersion;
            OnPlayerListChanged();
        }
        if (PlayerVersion == 1)
        {
            MenuUIManager.instance.ShowGameroom(GameMode.Host);
        }
    }
    public void RegisterPlayer(PlayerRef player, string playerName)
    {

        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning("只有 StateAuthority (Host) 才能修改 NetworkArray");
            return;
        }

        if (!string.IsNullOrEmpty(playerName))
        {
            PlayerNames.Set(player.AsIndex, playerName);
            Debug.Log($"✅ 註冊玩家 {playerName} 到索引 {player.AsIndex}");
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
    }

}
