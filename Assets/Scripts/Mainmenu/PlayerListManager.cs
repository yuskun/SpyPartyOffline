using UnityEngine;
using Fusion;
using TMPro;

public class PlayerListManager : NetworkBehaviour
{
    [Networked, Capacity(8), OnChangedRender(nameof(RPC_NotifyPlayerListChanged))]
    public NetworkArray<NetworkString<_16>> PlayerNames { get; }

    public Color otherPlayerColor;
    public Color myPlayerColor;


    private TextMeshProUGUI[] nameTexts;

    void Awake()
    {
        int count = transform.childCount;
        nameTexts = new TextMeshProUGUI[count];
        for (int i = 0; i < count; i++)
        {
            nameTexts[i] = transform.GetChild(i).GetComponentInChildren<TextMeshProUGUI>(true);
            nameTexts[i].text = "空間";
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_NotifyPlayerListChanged()
    {
        OnPlayerListChanged();
    }

    public void RegisterPlayer(PlayerRef player, string playerName)
    {
       
        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning("只有 StateAuthority (Host) 才能修改 NetworkArray");
            return;
        }

        for (int i = 0; i < PlayerNames.Length; i++)
        {
            if (string.IsNullOrEmpty(PlayerNames.Get(i).ToString()))
            {
                PlayerNames.Set(i, playerName);
                Debug.Log($"✅ 註冊玩家 {playerName} 到索引 {i}");
                return;
            }
        }
    }

    public void OnPlayerListChanged()
    {
        Debug.Log("玩家列表已更新");
        for (int i = 0; i < nameTexts.Length; i++)
        {
            string name = PlayerNames.Get(i).ToString();

            if (string.IsNullOrEmpty(name))
            {
                nameTexts[i].text = "空間";
                nameTexts[i].color = Color.black;
            }
            else
            {
                nameTexts[i].text = name;
                nameTexts[i].color = name == NetworkManager.instance.PlayerName ? myPlayerColor : otherPlayerColor;
            }
        }
    }

}
