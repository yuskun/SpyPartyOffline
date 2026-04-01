using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class StealTargetObject : NetworkBehaviour
{
    private Outline[] outlines;

    /// <summary>本局被選中的 StealTargetObject（Client 也可查）</summary>
    public static readonly List<StealTargetObject> All = new List<StealTargetObject>();

    /// <summary>由 StealTargetSelector 在遊戲開始時網路同步指定（0/1/2）</summary>
    [Networked] public int StealIndex { get; set; }

    /// <summary>是否為本局被選中的目標物件，由 StealTargetSelector 設定</summary>
    [Networked, OnChangedRender(nameof(OnIsTargetChanged))]
    public NetworkBool IsTarget { get; set; }

    private void Awake()
    {
        outlines = GetComponentsInChildren<Outline>(includeInactive: true);
    }

    public override void Spawned()
    {
        if (IsTarget && !All.Contains(this)) All.Add(this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        All.Remove(this);
    }

    private void OnIsTargetChanged()
    {
        if (IsTarget)
        {
            if (!All.Contains(this)) All.Add(this);
        }
        else
        {
            All.Remove(this);
        }
    }

    public void SetHighlight(bool on)
    {
        if (outlines == null) return;
        foreach (var o in outlines) if (o != null) o.enabled = on;
    }

    /// <summary>由 Server 呼叫，從網路上移除此物件</summary>
    public void BeStolen()
    {
        if (!Runner.IsServer) return;
        Runner.Despawn(Object);
    }
}
