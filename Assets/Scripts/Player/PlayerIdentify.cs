using Fusion;
using UnityEngine;

public class PlayerIdentify : NetworkBehaviour
{
    [Networked]public int PlayerID { get; set; }
    [Networked] public string PlayerName { get; set; }
    public override void Spawned()
    {
        base.Spawned();
        Debug.Log("spwan");
    }
}
