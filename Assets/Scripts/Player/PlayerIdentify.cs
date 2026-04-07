using Fusion;
using TMPro;
using UnityEngine;

public class PlayerIdentify : NetworkBehaviour
{
    [Networked] public int PlayerID { get; set; }

    [Networked, OnChangedRender(nameof(OnPlayerNameChanged))]
    public string PlayerName { get; set; }

    [Networked] public int SkinIndex { get; set; }
    public TextMeshProUGUI Text;

    public override void Spawned()
    {
        base.Spawned();
        RefreshNameText();
    }

    private void OnPlayerNameChanged()
    {
        RefreshNameText();
    }

    private void RefreshNameText()
    {
        if (Text != null)
            Text.text = PlayerName;
    }
}
