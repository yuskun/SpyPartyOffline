using System.Collections.Generic;
using UnityEngine;

public class PlayerScanner : MonoBehaviour
{
    [Header("掃描設定")]
    public bool enableScan = false;   // 是否啟用掃描
    public float scanRadius = 5f;
    public float viewAngle = 60f;     // 可視角度範圍
    public LayerMask playerLayer;

    public List<GameObject> nearbyPlayers = new();

    public GameObject currentTarget { get; private set; }
    private GameObject previousTarget; // 前一個目標

    // 掃描範圍
    public void ScanNearbyPlayers()
    {
        nearbyPlayers.Clear();
        Collider[] hits = Physics.OverlapSphere(transform.position, scanRadius, playerLayer);

        foreach (var hit in hits)
        {
            if (hit.transform.root == transform.root) continue;

            GameObject player = hit.transform.root.gameObject;
            if (!nearbyPlayers.Contains(player))
            {
                nearbyPlayers.Add(player);
            }
        }
    }

    // 視野內最近的
    public GameObject GetVisibleNearestPlayer()
    {
        GameObject nearest = null;
        float minDist = float.MaxValue;

        Vector3 forward = transform.forward;

        foreach (var player in nearbyPlayers)
        {
            if (player == null) continue;

            Vector3 toTarget = (player.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(forward, toTarget);

            if (angle > viewAngle / 2f)
                continue;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = player;
            }
        }

        return nearest;
    }

    private void Update()
    {
        if (!enableScan) return;

        ScanNearbyPlayers();
        currentTarget = GetVisibleNearestPlayer();
        // 如果目標改變了，更新 outline 狀態
        if (previousTarget != currentTarget)
        {
            if (previousTarget != null)
            {
                Outline prevOutline = previousTarget.GetComponent<Outline>();
                if (prevOutline != null) prevOutline.enabled = false;
            }

            if (currentTarget != null)
            {
                Outline newOutline = currentTarget.GetComponent<Outline>();
                if (newOutline != null) newOutline.enabled = true;
                Debug.Log($"✅ 鎖定新目標：{currentTarget.name}");
            }
            else
            {
                Debug.Log("❌ 沒有鎖定目標");
            }

            previousTarget = currentTarget;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, scanRadius);

        Gizmos.color = Color.yellow;
        Vector3 rightLimit = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward * scanRadius;
        Vector3 leftLimit = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward * scanRadius;

        Gizmos.DrawRay(transform.position, rightLimit);
        Gizmos.DrawRay(transform.position, leftLimit);
    }
}
