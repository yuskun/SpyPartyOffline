using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class CharSelectBridge : MonoBehaviour
{
    private UIDocument _doc;
    private List<Button> _portraitBtns = new List<Button>();

    // UI 元件
    private VisualElement _hueWheel;
    private VisualElement _svSquare;
    private VisualElement _colorPreview;
    private VisualElement _hueMarker;
    private VisualElement _svMarker;

    private SliderInt _hueSlider;
    private SliderInt _satSlider;
    private SliderInt _valSlider;

    private Label _hueValueLabel;
    private Label _satValueLabel;
    private Label _valValueLabel;
    private Label _rgbR;
    private Label _rgbG;
    private Label _rgbB;
    private Label _hexLabel;

    // 當前 HSV（0-1 範圍）
    private float _currentH = 0.56f; // 對應 H=201
    private float _currentS = 0.50f;
    private float _currentV = 0.96f;

    // 每個角色的預設 HSV（按頭像順序：mouse, snake, shark, crocodile, lahn）
    // 數值 = 該角色 "Skin" material asset 的 _BaseColor 實際 HSV，避免覆寫掉 prefab 原色
    //   0 老鼠 #ADADAD / 1 蛇 #FFBD2F / 2 鯊魚 #4FCEF3 / 3 鱷魚 #519F4F / 4 小白人 #61F9FF
    // ※ 若要改某角色「預設色」，記得 array 與對應的 Skin.mat 一起改。
    private readonly float[] _defaultH = { 0.000f, 0.114f, 0.538f, 0.328f, 0.506f };
    private readonly float[] _defaultS = { 0.000f, 0.816f, 0.675f, 0.505f, 0.618f };
    private readonly float[] _defaultV = { 0.679f, 1.000f, 0.953f, 0.623f, 1.000f };

    private int _activeIndex = 0;

    void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        var root = _doc.rootVisualElement;

        // ── 頭像按鈕 ──────────────────────────────────────
        _portraitBtns = root.Query<Button>(className: "portrait").ToList();
        for (int i = 0; i < _portraitBtns.Count; i++)
        {
            int index = i;
            _portraitBtns[i].focusable = false;
            _portraitBtns[i].clicked += () =>
            {
                SkinChange.instance.changeSkin(index);
                _activeIndex = index;
                // 切換角色時載入該角色上次的顏色
                _currentH = _defaultH[index];
                _currentS = _defaultS[index];
                _currentV = _defaultV[index];
                UpdateUISelection(index);
                RefreshAll();
                UISFXManager.Instance.PlayChangeCharacterSound();
            };
        }

        // ── 調色盤元件 ────────────────────────────────────
        _hueWheel    = root.Q<VisualElement>("HueWheel");
        _svSquare    = root.Q<VisualElement>("SVSquare");
        _colorPreview = root.Q<VisualElement>("ColorPreview");
        _hueMarker   = root.Q<VisualElement>("HueMarker");
        _svMarker    = root.Q<VisualElement>("SVMarker");

        _hueSlider   = root.Q<SliderInt>("HueSlider");
        _satSlider   = root.Q<SliderInt>("SatSlider");
        _valSlider   = root.Q<SliderInt>("ValSlider");

        _hueValueLabel = root.Q<Label>("HueValueLabel");
        _satValueLabel = root.Q<Label>("SatValueLabel");
        _valValueLabel = root.Q<Label>("ValValueLabel");
        _rgbR = root.Q<Label>("RgbR");
        _rgbG = root.Q<Label>("RgbG");
        _rgbB = root.Q<Label>("RgbB");
        _hexLabel = root.Q<Label>("HexLabel");

        // ── 色環互動 ──────────────────────────────────────
        if (_hueWheel != null)
        {
            _hueWheel.RegisterCallback<PointerDownEvent>(e => UpdateHue(e.localPosition));
            _hueWheel.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (e.pressedButtons != 1) return;
                UpdateHue(e.localPosition);
            });
        }

        // ── SV 方塊互動 ───────────────────────────────────
        if (_svSquare != null)
        {
            _svSquare.RegisterCallback<PointerDownEvent>(e => UpdateSV(e.localPosition));
            _svSquare.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (e.pressedButtons != 1) return;
                UpdateSV(e.localPosition);
            });
        }

        // ── 滑桿 ──────────────────────────────────────────
        if (_hueSlider != null)
            _hueSlider.RegisterValueChangedCallback(e =>
            {
                _currentH = e.newValue / 360f;
                RefreshAll(skipSliders: true);
            });

        if (_satSlider != null)
            _satSlider.RegisterValueChangedCallback(e =>
            {
                _currentS = e.newValue / 100f;
                RefreshAll(skipSliders: true);
            });

        if (_valSlider != null)
            _valSlider.RegisterValueChangedCallback(e =>
            {
                _currentV = e.newValue / 100f;
                RefreshAll(skipSliders: true);
            });

        // ── Reset 按鈕 ────────────────────────────────────
        var resetBtn = root.Q<Button>("ResetBtn");
        if (resetBtn != null)
        {
            resetBtn.focusable = false;
            resetBtn.clicked += () =>
            {
                SkinChange.instance.ResetSkinPreviewColor();
                _currentH = _defaultH[_activeIndex];
                _currentS = _defaultS[_activeIndex];
                _currentV = _defaultV[_activeIndex];
                RefreshAll();
            };
        }

        // ── 確定按鈕 ──────────────────────────────────────
        var selectBtn = root.Q<Button>("SelectBtn");
        if (selectBtn != null)
        {
            selectBtn.focusable = false;
            selectBtn.clicked += () =>
            {
                MenuUIManager.instance.ConfirmCharcterBtn.onClick.Invoke();
                this.gameObject.GetComponent<UniversalUIController>().HideCurrentUI();
            };
        }

        // ── 返回按鈕 ──────────────────────────────────────
        var backBtn = root.Q<Button>("BackBtn");
        if (backBtn != null)
        {
            backBtn.focusable = false;
            backBtn.clicked += () => SkinChange.instance.BackAndCloseAllUI();
        }

        // 初始化畫面
        RefreshAll();
    }

    // ── 更新頭像選中框 ────────────────────────────────────
    void UpdateUISelection(int selectedIndex)
    {
        for (int i = 0; i < _portraitBtns.Count; i++)
        {
            if (i == selectedIndex) _portraitBtns[i].AddToClassList("selected");
            else _portraitBtns[i].RemoveFromClassList("selected");
        }
    }

    // ── 色環點擊 → 更新 H ────────────────────────────────
    void UpdateHue(Vector2 localPos)
    {
        Debug.Log($"[Hue] 點擊位置: {localPos}, 寬高: {_hueWheel.layout.width}x{_hueWheel.layout.height}");
        float cx = _hueWheel.layout.width  / 2f;
        float cy = _hueWheel.layout.height / 2f;
        float angle = Mathf.Atan2(localPos.y - cy, localPos.x - cx) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        _currentH = angle / 360f;
        RefreshAll();
    }

    // ── SV 方塊點擊 → 更新 S, V ──────────────────────────
    void UpdateSV(Vector2 localPos)
    {
        Debug.Log($"[SV] 點擊位置: {localPos}, 寬高: {_svSquare.layout.width}x{_svSquare.layout.height}");
        _currentS = Mathf.Clamp01(localPos.x / _svSquare.layout.width);
        _currentV = Mathf.Clamp01(1f - localPos.y / _svSquare.layout.height);
        RefreshAll();
    }

    // ── 統一更新所有 UI ───────────────────────────────────
    // skipSliders：由滑桿事件呼叫時傳 true，避免循環觸發
    void RefreshAll(bool skipSliders = false)
    {
        Color c = Color.HSVToRGB(_currentH, _currentS, _currentV);
        int r = Mathf.RoundToInt(c.r * 255);
        int g = Mathf.RoundToInt(c.g * 255);
        int b = Mathf.RoundToInt(c.b * 255);

        // 顏色預覽 & SV 底色
        if (_colorPreview != null) _colorPreview.style.backgroundColor = c;
        if (_svSquare    != null) _svSquare.style.backgroundColor = Color.HSVToRGB(_currentH, 1f, 1f);

        // 數值標籤
        if (_hueValueLabel != null) _hueValueLabel.text = Mathf.RoundToInt(_currentH * 360).ToString();
        if (_satValueLabel != null) _satValueLabel.text = Mathf.RoundToInt(_currentS * 100) + "%";
        if (_valValueLabel != null) _valValueLabel.text = Mathf.RoundToInt(_currentV * 100) + "%";
        if (_rgbR != null) _rgbR.text = r.ToString();
        if (_rgbG != null) _rgbG.text = g.ToString();
        if (_rgbB != null) _rgbB.text = b.ToString();
        if (_hexLabel != null) _hexLabel.text = $"#{r:X2}{g:X2}{b:X2}";

        // 滑桿同步（不觸發 callback）
        if (!skipSliders)
        {
            _hueSlider?.SetValueWithoutNotify(Mathf.RoundToInt(_currentH * 360));
            _satSlider?.SetValueWithoutNotify(Mathf.RoundToInt(_currentS * 100));
            _valSlider?.SetValueWithoutNotify(Mathf.RoundToInt(_currentV * 100));
        }

        var hueTracker = _hueSlider?.Q("unity-tracker");
        var satTracker = _satSlider?.Q("unity-tracker");
        var valTracker = _valSlider?.Q("unity-tracker");

        if (hueTracker != null && hueTracker.style.backgroundImage == StyleKeyword.Null)
            hueTracker.style.backgroundImage = Background.FromTexture2D(CreateHueTexture());

        if (satTracker != null)
            satTracker.style.backgroundImage = Background.FromTexture2D(CreateSatTexture(_currentH, _currentV));

        if (valTracker != null)
            valTracker.style.backgroundImage = Background.FromTexture2D(CreateValTexture(_currentH, _currentS));
        // 指針位置
        UpdateHueMarker();
        UpdateSVMarker();

        // 套用到角色材質（取消下方註解後啟用）
        // SkinChange.instance.SetColor(c);
        if (SkinChange.instance != null)
        {
            SkinChange.instance.SetSkinPreviewColor(c); 
        }
    }

    // ── 色環指針位置 ──────────────────────────────────────
    void UpdateHueMarker()
    {
        if (_hueMarker == null || _hueWheel == null) return;
        float w  = _hueWheel.layout.width;
        float h  = _hueWheel.layout.height;
        // 指針落在色環帶的中間：外圓半徑 - 色帶寬一半
        float ringR = w / 2f - 14f;
        float angle = _currentH * 360f * Mathf.Deg2Rad;
        _hueMarker.style.left = w / 2f + Mathf.Cos(angle) * ringR - 9f;
        _hueMarker.style.top  = h / 2f + Mathf.Sin(angle) * ringR - 9f;
    }

    // ── SV 方塊指針位置 ───────────────────────────────────
    void UpdateSVMarker()
    {
        if (_svMarker == null || _svSquare == null) return;
        _svMarker.style.left = _currentS * _svSquare.layout.width  - 6f;
        _svMarker.style.top  = (1f - _currentV) * _svSquare.layout.height - 6f;
    }

    // ── 生成色相漸層貼圖 ──────────────────────────────────
    private Texture2D CreateHueTexture()
    {
        Texture2D tex = new Texture2D(360, 1);
        for (int i = 0; i < 360; i++)
        {
            tex.SetPixel(i, 0, Color.HSVToRGB(i / 360f, 1f, 1f));
        }
        tex.Apply();
        return tex;
    }

    // ── 生成飽和度漸層貼圖 ────────────────────────────────
    private Texture2D CreateSatTexture(float h, float v)
    {
        Texture2D tex = new Texture2D(100, 1);
        for (int i = 0; i < 100; i++)
        {
            tex.SetPixel(i, 0, Color.HSVToRGB(h, i / 100f, v));
        }
        tex.Apply();
        return tex;
    }

    // ── 生成亮度漸層貼圖 ──────────────────────────────────
    private Texture2D CreateValTexture(float h, float s)
    {
        Texture2D tex = new Texture2D(100, 1);
        for (int i = 0; i < 100; i++)
        {
            tex.SetPixel(i, 0, Color.HSVToRGB(h, s, i / 100f));
        }
        tex.Apply();
        return tex;
    }
}