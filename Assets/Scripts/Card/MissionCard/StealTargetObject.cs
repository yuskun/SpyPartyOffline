using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class StealTargetObject : NetworkBehaviour
{
    private Outline outline;

    /// <summary>所有場景中存活的 StealTargetObject（Client 也可查）</summary>
    public static readonly List<StealTargetObject> All = new List<StealTargetObject>();

    /// <summary>在 Inspector 手動設定 0/1/2，作為 TargetId 傳遞</summary>
    public int StealIndex = 0;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        if (outline == null) outline = GetComponentInChildren<Outline>();
    }

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    public void SetHighlight(bool on)
    {
        if (outline != null) outline.enabled = on;
    }

    /// <summary>由 Server 呼叫，從網路上移除此物件</summary>
    public void BeStolen()
    {
        if (!Runner.IsServer) return;
        Runner.Despawn(Object);
    }
}
