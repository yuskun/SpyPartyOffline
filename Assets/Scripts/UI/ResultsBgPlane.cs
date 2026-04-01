using System.Collections;
using UnityEngine;

/// <summary>
/// 掛在結算背景 3D Plane 上。
/// 呼叫 SlideIn() 後，Plane 從右側滑入到原始位置。
/// </summary>
public class ResultsBgPlane : MonoBehaviour
{
    public static ResultsBgPlane Instance { get; private set; }

    [Tooltip("滑入動畫時間（秒）")]
    public float slideInDuration = 0.5f;

    [Tooltip("從右側偏移多少距離開始（世界單位），依 Plane 大小調整）")]
    public float slideOffsetX = 30f;

    private Vector3 _finalPosition;

    private void Awake()
    {
        Instance = this;
        // 記錄 Plane 在場景中擺好的最終位置，然後先移到螢幕右側外隱藏
        _finalPosition = transform.localPosition;  // 記錄相對 Camera 的最終位置
        transform.localPosition = _finalPosition + new Vector3(slideOffsetX, 0f, 0f);
    }

    /// <summary>由 GameUIManager.ShowResultsPanel() 呼叫</summary>
    public void SlideIn()
    {
        StopAllCoroutines();
        StartCoroutine(DoSlideIn());
    }

    private IEnumerator DoSlideIn()
    {
        Vector3 startPos = _finalPosition + new Vector3(slideOffsetX, 0f, 0f);
        transform.localPosition = startPos;

        float elapsed = 0f;
        while (elapsed < slideInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideInDuration);
            transform.localPosition = Vector3.Lerp(startPos, _finalPosition, t);
            yield return null;
        }

        transform.localPosition = _finalPosition;
    }
}
