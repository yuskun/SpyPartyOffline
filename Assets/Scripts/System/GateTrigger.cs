using UnityEngine;

public class GateTrigger : MonoBehaviour
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

    private bool isOpen = false;
    private float closeTimer = 0f;
    private int playersInside = 0;

    void Start()
    {
        leftClosedPos = gateLeft.localPosition;
        rightClosedPos = gateRight.localPosition;

        leftOpenPos = leftClosedPos + Vector3.forward * openDistance;
        rightOpenPos = rightClosedPos + Vector3.back * openDistance;
    }

    void Update()
    {
        Vector3 leftTarget = isOpen ? leftOpenPos : leftClosedPos;
        Vector3 rightTarget = isOpen ? rightOpenPos : rightClosedPos;

        gateLeft.localPosition = Vector3.Lerp(gateLeft.localPosition, leftTarget, Time.deltaTime * openSpeed);
        gateRight.localPosition = Vector3.Lerp(gateRight.localPosition, rightTarget, Time.deltaTime * openSpeed);

        if (autoClose && isOpen && playersInside <= 0)
        {
            closeTimer -= Time.deltaTime;
            if (closeTimer <= 0f)
                isOpen = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (NetworkManager2.Instance == null || NetworkManager2.Instance.mode != NetworkManager2.NetMode.Host) return;
        Debug.Log($"GateTrigger: {other.name} entered. Tag: {other.tag}");
        if (!other.CompareTag("CanBeGrabbed")) return;
        playersInside++;
        isOpen = true;
        closeTimer = closeDelay;
    }

    void OnTriggerExit(Collider other)
    {
        if (NetworkManager2.Instance == null || NetworkManager2.Instance.mode != NetworkManager2.NetMode.Host) return;
        if (!other.CompareTag("CanBeGrabbed")) return;
        playersInside = Mathf.Max(0, playersInside - 1);
        closeTimer = closeDelay;
    }
}
