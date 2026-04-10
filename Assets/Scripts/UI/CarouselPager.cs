using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 簡單輪播器：把指定容器（H）的 anchoredPosition.x 以固定 step 平移，
/// 每 step 讓下一個子物件落到畫面正中央。
///
/// 使用情境：
///   父物件 (可視範圍/遮罩)
///     └─ H (這個 RectTransform)
///           ├─ Prefab A   ← 初始在中間
///           └─ Prefab B   ← Next() 後滑進中間
///
/// 第 i 頁時 H.anchoredPosition.x = baseX + i * stepX，
/// 預設 stepX = -54（往左推一張）。
///
/// 外部呼叫：
///   Next() / Prev() / GoTo(int) ——也可以直接綁到 Unity Button 的 OnClick。
/// 子物件數量會在每次翻頁時即時讀取，所以 Runtime 動態 Instantiate 的也沒問題。
/// </summary>
[DisallowMultipleComponent]
public class CarouselPager : MonoBehaviour
{
    [Header("輪播容器（留空則用自己）")]
    [Tooltip("要被平移的 RectTransform，通常就是 H。留空的話會用這個 component 所在的 RectTransform。")]
    public RectTransform content;

    [Header("平移設定")]
    [Tooltip("每翻一頁 H.anchoredPosition 要加上的 X 位移量。預設 -54 代表下一張往左推進 54 單位。")]
    public float stepX = -54f;

    [Tooltip("翻頁動畫時間（秒）。0 代表瞬間切換。")]
    public float duration = 0.25f;

    [Tooltip("是否循環（最後一頁 Next 回到第一頁、第一頁 Prev 跳到最後一頁）")]
    public bool loop = true;

    [Header("事件")]
    public UnityEvent<int> onPageChanged;

    /// <summary>目前第幾頁（0-based）</summary>
    public int CurrentIndex { get; private set; } = 0;

    /// <summary>目前容器裡可輪播的頁數 = 子物件數量</summary>
    public int PageCount => content != null ? content.childCount : 0;

    private Vector2 _baseAnchoredPos;
    private Coroutine _tween;

    private void Awake()
    {
        if (content == null) content = GetComponent<RectTransform>();
        if (content != null) _baseAnchoredPos = content.anchoredPosition;
    }

    private void OnEnable()
    {
        // 每次開啟面板回到第一頁（避免上一次看到一半的位置殘留）
        if (content != null)
        {
            CurrentIndex = 0;
            content.anchoredPosition = _baseAnchoredPos;
            onPageChanged?.Invoke(CurrentIndex);
        }
    }

    // ═══════════════════════════════════════════════
    //  公開 API
    // ═══════════════════════════════════════════════

    /// <summary>下一張</summary>
    public void Next() => GoTo(CurrentIndex + 1);

    /// <summary>上一張</summary>
    public void Prev() => GoTo(CurrentIndex - 1);

    /// <summary>直接跳到指定頁（會套用 loop 設定 / clamp）</summary>
    public void GoTo(int index)
    {
        int count = PageCount;
        if (count <= 0 || content == null) return;

        if (loop)
        {
            // 支援負數的模數
            index = ((index % count) + count) % count;
        }
        else
        {
            index = Mathf.Clamp(index, 0, count - 1);
        }

        if (index == CurrentIndex) return;

        CurrentIndex = index;
        Vector2 target = _baseAnchoredPos + new Vector2(stepX * index, 0f);

        if (_tween != null) StopCoroutine(_tween);
        if (duration <= 0f)
        {
            content.anchoredPosition = target;
            onPageChanged?.Invoke(CurrentIndex);
        }
        else
        {
            _tween = StartCoroutine(TweenTo(target));
        }
    }

    /// <summary>強制把位置重設到第一頁（不播動畫）</summary>
    public void ResetToFirst()
    {
        if (content == null) return;
        if (_tween != null) StopCoroutine(_tween);
        CurrentIndex = 0;
        content.anchoredPosition = _baseAnchoredPos;
        onPageChanged?.Invoke(CurrentIndex);
    }

    // ═══════════════════════════════════════════════
    //  Internals
    // ═══════════════════════════════════════════════

    private IEnumerator TweenTo(Vector2 target)
    {
        Vector2 start = content.anchoredPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            content.anchoredPosition = Vector2.LerpUnclamped(start, target, k);
            yield return null;
        }
        content.anchoredPosition = target;
        _tween = null;
        onPageChanged?.Invoke(CurrentIndex);
    }
}
