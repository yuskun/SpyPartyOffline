using UnityEngine;

public class NameFollowCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                 // 要跟隨的目標，例如玩家頭頂 Anchor
    public Vector3 worldOffset = new Vector3(0f, 2f, 0f);

    [Header("Follow")]
    public bool followPosition = true;       // 是否跟隨目標位置
    public bool smoothFollow = true;         // 是否平滑跟隨
    public float smoothSpeed = 15f;          // 平滑速度

    [Header("Look At Camera")]
    public bool faceCamera = true;           // 是否朝向攝影機
    public bool onlyYAxis = true;            // 是否只沿 Y 軸旋轉，避免文字傾斜

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        if (target == null) return;

        // 1. 跟隨位置
        if (followPosition)
        {
            Vector3 targetPos = target.position + worldOffset;

            if (smoothFollow)
            {
                transform.position = Vector3.Lerp(
                    transform.position,
                    targetPos,
                    smoothSpeed * Time.deltaTime
                );
            }
            else
            {
                transform.position = targetPos;
            }
        }

        // 2. 朝向攝影機
        if (faceCamera)
        {
            if (onlyYAxis)
            {
                Vector3 lookDir = transform.position - mainCamera.transform.position;
                lookDir.y = 0f;

                if (lookDir.sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }
            else
            {
                transform.LookAt(
                    transform.position + mainCamera.transform.rotation * Vector3.forward,
                    mainCamera.transform.rotation * Vector3.up
                );
            }
        }
    }
}