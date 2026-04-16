using UnityEngine.UIElements;

/// <summary>
/// 修正 Unity UI Toolkit Button 按住修飾鍵 (Shift / Ctrl / Alt / Cmd) 時點擊失效的問題。
///
/// 成因：
///   UI Toolkit 的 Button 底層使用 Clickable manipulator。
///   Clickable 預設只有一個 activator：{ button = LeftMouse, modifiers = None }。
///   其過濾條件是「修飾鍵必須完全相等」，因此按住 Shift / Ctrl / Alt 會被濾掉，
///   clicked 事件不會觸發，讓使用者以為按鈕壞掉。
///
/// 解法：
///   為每顆 Button 把 activators 展開成「所有修飾鍵組合 + 左鍵」，
///   讓任何修飾鍵狀態都能正常觸發 clicked。
/// </summary>
public static class UIToolkitModifierFix
{
    // 所有修飾鍵組合（Shift / Control / Alt / Command 的 4 位元組合 = 16 種）
    private static readonly EventModifiers[] AllModifierCombinations = new[]
    {
        EventModifiers.None,
        EventModifiers.Shift,
        EventModifiers.Control,
        EventModifiers.Alt,
        EventModifiers.Command,
        EventModifiers.Shift   | EventModifiers.Control,
        EventModifiers.Shift   | EventModifiers.Alt,
        EventModifiers.Shift   | EventModifiers.Command,
        EventModifiers.Control | EventModifiers.Alt,
        EventModifiers.Control | EventModifiers.Command,
        EventModifiers.Alt     | EventModifiers.Command,
        EventModifiers.Shift   | EventModifiers.Control | EventModifiers.Alt,
        EventModifiers.Shift   | EventModifiers.Control | EventModifiers.Command,
        EventModifiers.Shift   | EventModifiers.Alt     | EventModifiers.Command,
        EventModifiers.Control | EventModifiers.Alt     | EventModifiers.Command,
        EventModifiers.Shift   | EventModifiers.Control | EventModifiers.Alt | EventModifiers.Command,
    };

    /// <summary>讓單一 Button 的左鍵點擊在任何修飾鍵狀態下都有效。</summary>
    public static void AllowAnyModifier(this Button button)
    {
        if (button == null) return;

        var clickable = button.clickable;
        if (clickable == null)
        {
            // 理論上 Button 一定有 clickable；若沒有就補一個空的，確保後續 clicked += 能用
            clickable = new Clickable(() => { });
            button.clickable = clickable;
        }

        clickable.activators.Clear();
        foreach (var mods in AllModifierCombinations)
        {
            clickable.activators.Add(new ManipulatorActivationFilter
            {
                button = MouseButton.LeftMouse,
                modifiers = mods,
            });
        }
    }

    /// <summary>將 root 底下（含自身）所有 Button 都套用修飾鍵放寬。</summary>
    public static void AllowAnyModifierForAll(this VisualElement root)
    {
        if (root == null) return;
        root.Query<Button>().ForEach(b => b.AllowAnyModifier());
    }
}
