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
    public bool freezeYPosition = false;     // true = Y 軸鎖死、不跟隨 target

    [Header("Look At Camera")]
    public bool faceCamera = true;           // 是否朝向攝影機
    public bool onlyYAxis = true;            // 是否只沿 Y 軸旋轉，避免文字傾斜

    private Camera mainCamera;

    // freezeYPosition latch：等 target 同步到非 origin 才鎖 Y，避免 client 端 spawn 第一幀 parent 還沒同步時鎖到 0
    private bool _yLocked = false;
    private float _lockedY;

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

            // freezeYPosition = 鎖 Y，X/Z 仍跟隨 target
            // Lock 點：等 target 同步到非 origin（spawn 完成後）才 latch，避免 client 端踩到第一幀 parent 還在 (0,0,0) 的時序
            if (freezeYPosition)
            {
                if (!_yLocked && target.position.sqrMagnitude > 0.01f)
                {
                    _lockedY = target.position.y + worldOffset.y;
                    _yLocked = true;
                }
                // 已 lock 用 lock 值；未 lock 期間先用 target.y + offset，避免飄到 origin
                targetPos.y = _yLocked ? _lockedY : (target.position.y + worldOffset.y);
            }
            else
            {
                _yLocked = false; // 取消鎖時 reset，下次再勾起來會重新 latch
            }

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