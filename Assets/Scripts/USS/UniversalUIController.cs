using System.Collections.Generic;
using System.Linq;
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

    [System.Serializable]
    public class HotkeyBinding
    {
        public KeyCode key;
        [Tooltip("對應 ButtonSettings 裡的按鈕名稱")]
        public string targetButtonName;
    }

    [Tooltip("勾選後，此 UI 在啟動時會自動隱藏")]
    public bool hideOnStart = false;

    [Tooltip("勾選後，Show 會顯示滑鼠，Hide 會隱藏並鎖定滑鼠")]
    public bool controlCursor = false;

    public List<ButtonBinding> buttonSettings = new List<ButtonBinding>();

    [Header("快捷鍵設定（僅在此 UI 顯示時生效）")]
    public List<HotkeyBinding> hotkeySettings = new List<HotkeyBinding>();

    private UIDocument _doc;

    // 追蹤所有 controlCursor 的面板
    private static HashSet<UniversalUIController> _cursorControllers = new HashSet<UniversalUIController>();

    // 防止同一幀多個面板重複觸發同一個快捷鍵
    private static int _lastHotkeyFrame = -1;

    private void Start()
    {
        // hideOnStart 只隱藏 UI 面板，不觸發 cursor 邏輯
        if (hideOnStart)
        {
            if (_doc == null) _doc = GetComponent<UIDocument>();
            if (_doc != null)
                _doc.rootVisualElement.style.display = DisplayStyle.None;
        }
    }

    /// <summary>判斷此 UI 是否正在顯示（用 resolvedStyle 取得實際渲染狀態）</summary>
    public bool IsVisible()
    {
        if (_doc == null) _doc = GetComponent<UIDocument>();
        if (_doc == null || _doc.rootVisualElement == null) return false;
        return _doc.rootVisualElement.resolvedStyle.display == DisplayStyle.Flex;
    }

    private void Update()
    {
        if (hotkeySettings == null || hotkeySettings.Count == 0) return;
        if (!IsVisible()) return;
        // 同一幀已經有其他面板觸發過快捷鍵，跳過
        if (_lastHotkeyFrame == Time.frameCount) return;

        foreach (var hotkey in hotkeySettings)
        {
            if (Input.GetKeyDown(hotkey.key))
            {
                // 標記這一幀已被消費，其他面板不再處理
                _lastHotkeyFrame = Time.frameCount;

                // 找到對應的 ButtonBinding 並觸發它的事件
                foreach (var btn in buttonSettings)
                {
                    if (btn.buttonName == hotkey.targetButtonName)
                    {
                        btn.OnClickEvents?.Invoke();
                        break;
                    }
                }
                break;
            }
        }
    }
    private void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        var root = _doc.rootVisualElement;

        // 修正：UI Toolkit Button 預設按住 Shift/Ctrl/Alt 會無法點擊，
        // 在此統一放寬整棵樹所有 Button 的修飾鍵過濾。
        root.AllowAnyModifierForAll();

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

        if (controlCursor)
        {
            _cursorControllers.Remove(this);
            RefreshCursorState();
        }
    }

    /// <summary>
    /// 提供給 Inspector 呼叫：顯示目前的 UI (Display = Flex)
    /// </summary>
    public void ShowCurrentUI()
    {
        if (_doc != null)
            _doc.rootVisualElement.style.display = DisplayStyle.Flex;

        if (controlCursor)
        {
            _cursorControllers.Add(this);
            RefreshCursorState();
        }
    }

    public void SetVisible(bool visible)
    {
        if (visible) { ShowCurrentUI(); return; }
        else { HideCurrentUI(); return; }
    }

    /// <summary>
    /// 根據目前所有 controlCursor 面板的狀態決定滑鼠顯示
    /// 只要有任何一個 controlCursor 面板正在顯示，滑鼠就保持開啟
    /// </summary>
    private static void RefreshCursorState()
    {
        // 清除已被銷毀的參考
        _cursorControllers.RemoveWhere(c => c == null);

        bool anyCursorPanelVisible = _cursorControllers.Count > 0;

        if (anyCursorPanelVisible)
        {
            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            UnityEngine.Cursor.visible = false;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        }
    }
}