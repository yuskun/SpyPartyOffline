
using UnityEngine;

public class MiniMap : MonoBehaviour
{
    public Transform target;  // 追蹤的物件（例如玩家）
    public static MiniMap instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {

        if (target == null) return;

        // 保持相機原本的 Y，不跟隨 target 的 Y
        Vector3 newPos = target.position;
        newPos.y = transform.position.y;

        transform.position = newPos;
    }
}
