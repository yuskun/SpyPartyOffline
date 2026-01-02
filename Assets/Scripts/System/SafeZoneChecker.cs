using UnityEngine;

public class SafeZoneChecker : MonoBehaviour
{
    [Header("Trigger 設定")]
    [SerializeField] private string safeZoneTag = "CameraSafe";

    [Header("控制的物件")]
    [SerializeField] private GameObject targetObjectA;

    // 目前在幾個合法 Trigger 裡
    private int insideSafeZoneCount = 0;

    private void Start()
    {
        UpdateTargetState();
    }

    private void OnTriggerEnter(Collider other)
    {
       
        if (!other.CompareTag(safeZoneTag)) return;

        insideSafeZoneCount++;
        UpdateTargetState();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(safeZoneTag)) return;

        insideSafeZoneCount--;
        if (insideSafeZoneCount < 0)
            insideSafeZoneCount = 0;

        UpdateTargetState();
    }

    private void UpdateTargetState()
    {
        // 不在任何合法區域 → 開啟 A
        bool shouldEnable = insideSafeZoneCount == 0;

        if (targetObjectA.activeSelf != shouldEnable)
        {
            targetObjectA.SetActive(shouldEnable);
        }
    }
}
