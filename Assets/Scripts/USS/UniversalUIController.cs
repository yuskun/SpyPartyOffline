using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Events;

public class UniversalUIController : MonoBehaviour
{
    [System.Serializable]
    public class ButtonBinding
    {
        public string buttonName;
        public UnityEvent OnClickEvents;
        [HideInInspector] public Button loadedButton;
    }

    [Tooltip("勾選後，此 UI 在首次啟動時會自動隱藏（僅第一次 OnEnable 生效）")]
    public bool hideOnStart = false;
    public List<ButtonBinding> buttonSettings = new List<ButtonBinding>();
    private UIDocument _doc;
    private bool _firstInit = true;

    private void OnEnable()
    {
        // 延後一幀，避免 UniversalUIController.OnEnable 早於 UIDocument.OnEnable
        // 導致 rootVisualElement 還沒建好
        StartCoroutine(InitNextFrame());
    }

    private IEnumerator InitNextFrame()
    {
        yield return null;

        _doc = GetComponent<UIDocument>();
        if (_doc == null) yield break;

        var root = _doc.rootVisualElement;
        if (root == null) yield break;

        // 綁定按鈕（先解再綁，避免重複堆疊）
        foreach (var setting in buttonSettings)
        {
            var btn = root.Q<Button>(setting.buttonName);
            if (btn != null)
            {
                btn.clicked -= setting.OnClickEvents.Invoke;
                btn.clicked += setting.OnClickEvents.Invoke;
                setting.loadedButton = btn;
            }
        }

        // 顯示狀態處理：
        // - 首次 OnEnable：依 hideOnStart 決定（取代原本 Start() 的邏輯）
        // - 非首次（SetActive(false)→true 再次啟用）：強制 Flex，清除上次 HideCurrentUI 殘留的 None
        if (_firstInit)
        {
            _firstInit = false;
            root.style.display = hideOnStart ? DisplayStyle.None : DisplayStyle.Flex;
        }
        else
        {
            root.style.display = DisplayStyle.Flex;
        }
    }

    private void OnDisable()
    {
        foreach (var setting in buttonSettings)
        {
            if (setting.loadedButton != null)
            {
                setting.loadedButton.clicked -= setting.OnClickEvents.Invoke;
                setting.loadedButton = null;
            }
        }
    }

    /// <summary>
    /// 提供給 Inspector 呼叫：隱藏目前的 UI (Display = None)
    /// </summary>
    public void HideCurrentUI()
    {
        if (_doc != null)
            _doc.rootVisualElement.style.display = DisplayStyle.None;
    }

    /// <summary>
    /// 提供給 Inspector 呼叫：顯示目前的 UI (Display = Flex)
    /// </summary>
    public void ShowCurrentUI()
    {
        if (_doc != null)
            _doc.rootVisualElement.style.display = DisplayStyle.Flex;
    }

    public void SetVisible(bool visible)
    {
        // 確保 _doc 已經被賦值
        if (_doc == null) _doc = GetComponent<UIDocument>();

        if (_doc != null && _doc.rootVisualElement != null)
        {
            // 切換 DisplayStyle 
            _doc.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}