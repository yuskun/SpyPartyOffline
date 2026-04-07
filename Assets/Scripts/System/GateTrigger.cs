using Fusion;
using UnityEngine;

public class GateTrigger : NetworkBehaviour
{
    [Header("柵門物件（左右兩片）")]
    public Transform gateLeft;
    public Transform gateRight;

    [Header("開門設定")]
    public float openDistance = 2f;
    public float openSpeed = 3f;

    [Header("自動關門")]
    public bool autoClose = true;
    public float closeDelay = 3f;

    private Vector3 leftClosedPos;
    private Vector3 rightClosedPos;
    private Vector3 leftOpenPos;
    private Vector3 rightOpenPos;

    [Networked] private NetworkBool IsOpen { get; set; }
    [Networked] private float CloseTimer { get; set; }
    [Networked] private int PlayersInside { get; set; }

    public override void Spawned()
    {
        leftClosedPos = gateLeft.localPosition;
        rightClosedPos = gateRight.localPosition;

        leftOpenPos = leftClosedPos + Vector3.forward * openDistance;
        rightOpenPos = rightClosedPos + Vector3.back * openDistance;
    }

    public override void Render()
    {
        Vector3 leftTarget = IsOpen ? leftOpenPos : leftClosedPos;
        Vector3 rightTarget = IsOpen ? rightOpenPos : rightClosedPos;

        gateLeft.localPosition = Vector3.Lerp(gateLeft.localPosition, leftTarget, Time.deltaTime * openSpeed);
        gateRight.localPosition = Vector3.Lerp(gateRight.localPosition, rightTarget, Time.deltaTime * openSpeed);
    }

    // Host 端驅動自動關門計時
    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsServer) return;

        if (autoClose && IsOpen && PlayersInside <= 0)
        {
            CloseTimer -= Runner.DeltaTime;
            if (CloseTimer <= 0f)
                IsOpen = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        
        if (!Runner.IsServer) return;
        if (!other.CompareTag("CanBeGrabbed")) return;
        Debug.Log(" gate trigger");
        PlayersInside++;
        IsOpen = true;
        CloseTimer = closeDelay;
    }

    void OnTriggerExit(Collider other)
    {
        if (!Runner.IsServer) return;
        if (!other.CompareTag("CanBeGrabbed")) return;
        PlayersInside = Mathf.Max(0, PlayersInside - 1);
        CloseTimer = closeDelay;
    }
}
