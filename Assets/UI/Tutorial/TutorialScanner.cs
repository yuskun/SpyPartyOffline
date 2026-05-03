using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 教學場景的本地 Scanner — 參考 PlayerScanner 的設計，但只負責掃描教學裡的
/// 互動物件（禮物盒 / 武器 / 偷取目標 / 假人），並依距離 + 鏡頭角度算出
/// 「玩家現在最可能想互動的目標」，把該目標的 Outline 打開。
/// 跟原本 PlayerScanner 不同處：不掃其他玩家、不跑 Steal 的 mission 邏輯。
/// </summary>
public class TutorialScanner : MonoBehaviour
{
    [Header("掃描設定")]
    public bool enableScan = true;
    public float scanRadius = 4f;
    public float heightOffset = 1.0f;
    [Tooltip("只掃這些 Layer（留空則全部）")]
    public LayerMask interactableLayer = ~0;

    [Header("權重設定")]
    [Range(0, 1)] public float angleWeight    = 0.7f;
    [Range(0, 1)] public float distanceWeight = 0.3f;

    [Header("鏡頭設定")]
    public Transform cameraTransform;

    [Header("Debug")]
    public bool debugDraw = true;
    public Color gizmoColor  = new Color(0.36f, 0.89f, 1.0f, 0.20f); // 水藍
    public Color targetColor = new Color(1.00f, 0.85f, 0.39f, 1.0f); // 黃

    /// <summary>目前掃到「最佳」的互動物件（會被 Outline 高亮）</summary>
    public GameObject currentTarget { get; private set; }

    private GameObject previousTarget;
    private readonly List<GameObject> _candidates = new();
    private readonly Collider[] _hits = new Collider[24];

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void FixedUpdate()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (!enableScan) { ClearTarget(); return; }

        FindCandidates();
        currentTarget = PickBest();
        UpdateOutline();
    }

    /// <summary>找出範圍內所有教學互動物件</summary>
    private void FindCandidates()
    {
        _candidates.Clear();
        Vector3 scanPos = transform.position + Vector3.up * heightOffset;
        var selfRoot = transform.root.gameObject;

        int n = Physics.OverlapSphereNonAlloc(scanPos, scanRadius, _hits, interactableLayer, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            var col = _hits[i];
            if (col == null) continue;
            var root = col.transform.root.gameObject;
            if (root == selfRoot) continue; // 排除自己（player）
            if (IsTutorialInteractable(root) && !_candidates.Contains(root))
                _candidates.Add(root);
        }
    }

    /// <summary>判斷物件是否為教學互動目標（拾取 / 武器 / 假人 / 偷取）</summary>
    private static bool IsTutorialInteractable(GameObject go)
    {
        if (go == null) return false;
        if (go.GetComponent<GivePeek>()   != null) return true;
        if (go.GetComponent<Givebanana>() != null) return true;
        if (go.GetComponent<GiveSteal>()  != null) return true;
        // 武器：場景上的 Bat 帶 WeaponHandler 在子物件
        if (go.GetComponentInChildren<OodlesEngine.WeaponHandler>() != null) return true;
        // 假人 / AI：只要身上有 OodlesCharacter（且不是 LocalPlayer）就視為互動目標
        if (go.GetComponent<OodlesEngine.OodlesCharacter>() != null
            && go.GetComponent<OodlesEngine.LocalPlayer>() == null) return true;
        return false;
    }

    /// <summary>同 PlayerScanner：依鏡頭夾角 + 距離加權找最佳</summary>
    private GameObject PickBest()
    {
        if (_candidates.Count == 0) return null;
        if (cameraTransform == null) return null;

        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 scanPos = transform.position + Vector3.up * heightOffset;

        GameObject best = null;
        float bestScore = 0f;

        foreach (var go in _candidates)
        {
            if (go == null) continue;
            Vector3 toTarget = go.transform.position - scanPos;
            float dist = toTarget.magnitude;
            if (dist > scanRadius) continue;

            Vector3 dir = toTarget.normalized;
            float angleDot = Vector3.Dot(camForward, dir);
            angleDot = Mathf.Clamp01((angleDot + 1f) * 0.5f); // -1~1 → 0~1

            float distFactor = 1f - Mathf.Clamp01(dist / scanRadius);
            float score = angleDot * angleWeight + distFactor * distanceWeight;

            if (score > bestScore) { bestScore = score; best = go; }
        }
        return best;
    }

    /// <summary>切換 Outline 高亮（移自 PlayerScanner.UpdateOutlineState）</summary>
    private void UpdateOutline()
    {
        if (previousTarget == currentTarget) return;

        if (previousTarget != null)
        {
            var prev = previousTarget.GetComponentInChildren<Outline>();
            if (prev != null) prev.enabled = false;
        }

        if (currentTarget != null)
        {
            var cur = currentTarget.GetComponentInChildren<Outline>();
            if (cur != null) cur.enabled = true;
        }

        previousTarget = currentTarget;
    }

    private void ClearTarget()
    {
        if (previousTarget != null)
        {
            var prev = previousTarget.GetComponentInChildren<Outline>();
            if (prev != null) prev.enabled = false;
        }
        previousTarget = null;
        currentTarget = null;
    }

    void OnDrawGizmos()
    {
        if (!debugDraw) return;
        Vector3 scanPos = transform.position + Vector3.up * heightOffset;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(scanPos, scanRadius);

        if (cameraTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(scanPos, cameraTransform.forward * scanRadius);
        }

        if (currentTarget != null)
        {
            Gizmos.color = targetColor;
            Gizmos.DrawLine(scanPos, currentTarget.transform.position);
        }
    }
}
