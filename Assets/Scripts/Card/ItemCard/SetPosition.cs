using Fusion;
using UnityEngine;

public class SetPosition : NetworkBehaviour
{
   [Networked] public Vector3 pos { get; set; }
    public override void Spawned()
    {
        this.gameObject.transform.position = pos;
    }
    public void Setpos(Vector3 pos)
    {
        if (Runner.IsServer)
        {
            this.pos = pos;
        }
    }
}
