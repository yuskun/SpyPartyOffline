using UnityEngine;

/// <summary>
/// World-space UI Billboard：讓 UI 永遠面向「本地玩家」攝影機。
/// 用於頭頂名牌、血條、互動提示等。
/// </summary>
[DisallowMultipleComponent]
public class FacePlayerUI : MonoBehaviour
{
    [Header("Target Camera")]
    [Tooltip("指定要面向的相機；留空會自動找本地可用相機。")]
    public Camera targetCamera;

    [Header("Rotation")]
    [Tooltip("只繞 Y 軸旋轉（推薦用於名牌/血條，避免上下歪斜）。")]
    public bool yawOnly = true;

    [Tooltip("旋轉平滑度，0 = 不平滑(立即轉)。")]
    [Range(0f, 30f)]
    public float smooth = 12f;

    [Tooltip("如果 UI 會顯示鏡像，打勾可反轉面向。")]
    public bool flipForward = false;

    void Awake()
    {
        ResolveCamera();
    }

    void OnEnable()
    {
        ResolveCamera();
    }

    void LateUpdate()
    {
        if (targetCamera == null)
        {
            ResolveCamera();
            if (targetCamera == null) return;
        }

        Vector3 toCam = targetCamera.transform.position - transform.position;
        if (yawOnly) toCam.y = 0f;

        // 避免相機剛好在正上/正下導致方向向量太小
        if (toCam.sqrMagnitude < 0.0001f) return;

        Quaternion look = Quaternion.LookRotation(flipForward ? toCam : -toCam, Vector3.up);

        if (smooth <= 0f)
            transform.rotation = look;
        else
            transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * smooth);
    }

    void ResolveCamera()
    {
        // 1) 優先用 MainCamera（常見）
        if (targetCamera == null && Camera.main != null)
            targetCamera = Camera.main;

        // 2) 再找任意啟用中的相機
        if (targetCamera == null)
        {
            var cams = Camera.allCamerasCount;
            if (cams > 0)
            {
                Camera[] all = new Camera[cams];
                Camera.GetAllCameras(all);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].isActiveAndEnabled)
                    {
                        targetCamera = all[i];
                        break;
                    }
                }
            }
        }
    }
}