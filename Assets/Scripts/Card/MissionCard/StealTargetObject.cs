using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class StealTargetObject : NetworkBehaviour
{
    private Outline[] outlines;
    private Renderer[] renderers;
    private Collider[] colliders;

    /// <summary>本局被選中的 StealTargetObject（Client 也可查，僅含未被偷走的）</summary>
    public static readonly List<StealTargetObject> All = new List<StealTargetObject>();

    /// <summary>由 StealTargetSelector 在遊戲開始時網路同步指定（0/1/2）</summary>
    [Networked] public int StealIndex { get; set; }

    /// <summary>是否為本局被選中的目標物件，由 StealTargetSelector 設定</summary>
    [Networked, OnChangedRender(nameof(OnIsTargetChanged))]
    public NetworkBool IsTarget { get; set; }

    /// <summary>是否已被偷走（隱藏但不 Despawn，可重置）</summary>
    [Networked, OnChangedRender(nameof(OnIsStolenChanged))]
    public NetworkBool IsStolen { get; set; }

    private void Awake()
    {
        outlines  = GetComponentsInChildren<Outline>(includeInactive: true);
        renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        colliders = GetComponentsInChildren<Collider>(includeInactive: true);
    }

    public override void Spawned()
    {
        // 補上 late-join 狀態
        if (IsTarget)
        {
            if (!IsStolen && !All.Contains(this)) All.Add(this);
            ApplyVisual(!IsStolen);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        All.Remove(this);
    }

    private void OnIsTargetChanged()
    {
        if (IsTarget && !IsStolen && !All.Contains(this))
            All.Add(this);
        else if (!IsTarget)
            All.Remove(this);
    }

    private void OnIsStolenChanged()
    {
        if (IsStolen)
        {
            All.Remove(this);
            ApplyVisual(false);
        }
        else if (IsTarget)
        {
            if (!All.Contains(this)) All.Add(this);
            ApplyVisual(true);
        }
    }

    private void ApplyVisual(bool visible)
    {
        foreach (var r in renderers) if (r != null) r.enabled = visible;
        foreach (var c in colliders) if (c != null) c.enabled = visible;
        SetHighlight(false); // 重置 highlight
    }

    public void SetHighlight(bool on)
    {
        if (outlines == null) return;
        foreach (var o in outlines) if (o != null) o.enabled = on;
    }

    /// <summary>Host 呼叫：標記為已偷走（隱藏但保留 NetworkObject）</summary>
    public void BeStolen()
    {
        if (!Runner.IsServer) return;
        IsStolen = true;
    }

    /// <summary>Host 呼叫：重置為未偷走狀態（Steal 卡換手時使用）</summary>
    public void Reset()
    {
        if (!Runner.IsServer) return;
        IsStolen = false;
    }
}
