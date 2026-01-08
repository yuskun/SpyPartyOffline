using System.Collections.Generic;
using UnityEngine;

public class PlayerScanner : MonoBehaviour
{
    [Header("掃描設定")]
    public bool enableScan = false;
    public float scanRadius = 6f;
    public float heightOffset = 1.0f;
    public LayerMask playerLayer;

    [Header("權重設定")]
    [Range(0, 1)] public float angleWeight = 0.7f;
    [Range(0, 1)] public float distanceWeight = 0.3f;

    [Header("鏡頭設定")]
    public Transform cameraTransform;

    [Header("Debug")]
    public bool debugDraw = true;
    public Color gizmoColor = new Color(0, 1, 0, 0.15f);
    public Color targetColor = Color.red;

    public GameObject currentTarget { get; private set; }
    public GameObject currentRagdoll { get; private set; }
    private GameObject previousTarget;
    private List<GameObject> nearbyPlayers = new();

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void FixedUpdate()
    {
        if (!enableScan) return;

        ScanNearbyPlayers();
        currentRagdoll = GetWeightedBestTarget();
        UpdateOutlineState();
        if (currentRagdoll != null)
        {
            currentTarget = currentRagdoll.transform.parent.gameObject;
        }
        else
        {
            currentTarget = null;
        }
    }

    private void ScanNearbyPlayers()
    {
        nearbyPlayers.Clear();
        Vector3 scanPos = transform.position + Vector3.up * heightOffset;

        Collider[] hits = Physics.OverlapSphere(scanPos, scanRadius, playerLayer);
        foreach (var hit in hits)
        {
            var scanner = hit.transform.GetComponentInParent<PlayerScanner>();
            if (scanner != null && scanner != this)
                nearbyPlayers.Add(scanner.gameObject);
        }
    }

    // 🧠 根據角度接近與距離加權選目標
    private GameObject GetWeightedBestTarget()
    {
        if (cameraTransform == null) return null;

        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 scanPos = transform.position + Vector3.up * heightOffset;

        GameObject best = null;
        float bestScore = 0f;

        foreach (var player in nearbyPlayers)
        {
            if (player == null) continue;

            Vector3 toTarget = player.transform.position - scanPos;
            float dist = toTarget.magnitude;
            if (dist > scanRadius) continue;

            Vector3 dir = toTarget.normalized;
            float angleDot = Vector3.Dot(camForward, dir); // 與鏡頭方向夾角相似度 (-1~1)
            angleDot = Mathf.Clamp01((angleDot + 1f) * 0.5f); // 映射到 0~1

            float distanceFactor = 1f - Mathf.Clamp01(dist / scanRadius);

            float weight = angleDot * angleWeight + distanceFactor * distanceWeight;

            if (weight > bestScore)
            {
                bestScore = weight;
                best = player;
            }
        }

        return best;
    }

    private void UpdateOutlineState()
    {
        if (previousTarget == currentRagdoll) return;

        if (previousTarget != null)
        {
            var prevOutline = previousTarget.GetComponent<Outline>();
            if (prevOutline != null) prevOutline.enabled = false;
        }

        if (currentRagdoll != null)
        {
            var newOutline = currentRagdoll.GetComponent<Outline>();
            if (newOutline != null)
                newOutline.enabled = true;
        }

        previousTarget = currentRagdoll;
    }

    private void OnDrawGizmos()
    {
        if (!debugDraw) return;

        Vector3 scanPos = transform.position + Vector3.up * heightOffset;

        // 範圍球
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(scanPos, scanRadius);

        // 鏡頭方向
        if (cameraTransform)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(scanPos, cameraTransform.forward * scanRadius);
        }

        // 目標線
        if (currentRagdoll)
        {
            Gizmos.color = targetColor;
            Gizmos.DrawLine(scanPos, currentRagdoll.transform.position);
        }
    }
}
