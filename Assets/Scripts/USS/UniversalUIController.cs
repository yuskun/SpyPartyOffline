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

    [Tooltip("勾選後，此 UI 在啟動時會自動隱藏")]
    public bool hideOnStart = false;
    public List<ButtonBinding> buttonSettings = new List<ButtonBinding>();
    private UIDocument _doc;

    private void Start()
    {
        if (hideOnStart) HideCurrentUI();
    }
    private void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        var root = _doc.rootVisualElement;

        foreach (var setting in buttonSettings)
        {
            var btn = root.Q<Button>(setting.buttonName);
            if (btn != null)
            {
                btn.clicked += setting.OnClickEvents.Invoke;
                setting.loadedButton = btn;
            }
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
}