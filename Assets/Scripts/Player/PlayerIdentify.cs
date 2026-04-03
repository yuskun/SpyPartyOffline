using Fusion;
using TMPro;
using UnityEngine;

public class PlayerIdentify : NetworkBehaviour
{
    [Networked]public int PlayerID { get; set; }
    [Networked] public string PlayerName { get; set; }
    [Networked] public int SkinIndex { get; set; }
    public TextMeshProUGUI Text;
    public override void Spawned()
    {
        base.Spawned();
        Debug.Log("spwan");
        Text.text=PlayerName;
        
    }
}
