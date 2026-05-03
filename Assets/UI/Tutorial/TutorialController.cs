using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class TutorialController : MonoBehaviour
{
    public enum ModalKind { None, Items, MissionCard, Final }

    [Serializable]
    public class TutorialStep
    {
        public string title;
        public string summary;        // collapsed-state short description; auto-fallback if empty
        [TextArea(2, 5)] public string body;
        public string[] keys;         // ["W","A","S","D"], ["E"], ["RMB 右鍵"], ["1","2","3","4","5","6","🖱 滾輪"], ...
        public string target;         // optional yellow goal chip text
        public bool video;            // show 16:9 video placeholder
        public ModalKind modal = ModalKind.None;
    }

    [Header("Steps")]
    [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

    [Header("Behavior")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool startCollapsed = false;
    [Tooltip("由 TutorialFlow 從外部控制時，關掉自動播放與 Enter→下一步")]
    [SerializeField] public bool externallyControlled = false;

    public int CurrentStep => _cur;
    public int StepCount => steps != null ? steps.Count : 0;
    public TutorialStep CurrentStepData => (steps != null && _cur >= 0 && _cur < steps.Count) ? steps[_cur] : null;

    public void GoToStep(int idx)
    {
        if (steps == null || steps.Count == 0) return;
        _cur = Mathf.Clamp(idx, 0, steps.Count - 1);
        Render();
    }

    [Header("Font (optional override for CJK)")]
    [SerializeField] private Font tutorialFont;

    // ---- visual element refs ----
    private UIDocument _doc;
    private VisualElement _root;
    private VisualElement _dialogue;
    private Label _stepBadge;
    private Label _stepTitle;
    private Label _stepSummary;
    private Label _stepBody;
    private VisualElement _keysRow;
    private Label _stepTarget;
    private VisualElement _videoSlot;
    private Label _kbdHintText;
    private VisualElement _progressTrack;
    private Label _progressText;
    private VisualElement _modalItems;
    private VisualElement _modalMissionCard;
    private VisualElement _modalFinal;

    private int _cur = 0;
    private bool _bound = false;

    // ---------- lifecycle ----------
    void OnEnable()
    {
        EnsureDefaultSteps();

        _doc = GetComponent<UIDocument>();
        if (_doc == null || _doc.rootVisualElement == null) return;
        _root = _doc.rootVisualElement;

        QueryElements();
        BindEvents();
        BuildProgressDots();
        ApplyFont();

        if (autoStart && !externallyControlled) Run();
    }

    void OnDisable()
    {
        if (_root == null) return;
        _root.UnregisterCallback<KeyDownEvent>(OnKeyDown);
    }

    private void QueryElements()
    {
        _dialogue        = _root.Q<VisualElement>("dialogue");
        _stepBadge       = _root.Q<Label>("step-badge");
        _stepTitle       = _root.Q<Label>("step-title");
        _stepSummary     = _root.Q<Label>("step-summary");
        _stepBody        = _root.Q<Label>("step-body");
        _keysRow         = _root.Q<VisualElement>("keys-row");
        _stepTarget      = _root.Q<Label>("step-target");
        _videoSlot       = _root.Q<VisualElement>("video-slot");
        _kbdHintText     = _root.Q<Label>("kbd-hint-text");
        _progressTrack   = _root.Q<VisualElement>("progress-track");
        _progressText    = _root.Q<Label>("progress-text");
        _modalItems      = _root.Q<VisualElement>("modal-items");
        _modalMissionCard= _root.Q<VisualElement>("modal-mission-card");
        _modalFinal      = _root.Q<VisualElement>("modal-final");
    }

    private void BindEvents()
    {
        if (_bound) return;
        _bound = true;

        _root.focusable = true;
        _root.RegisterCallback<KeyDownEvent>(OnKeyDown);

        var btnCloseItems  = _root.Q<Button>("btn-close-items");
        var btnCloseMC     = _root.Q<Button>("btn-close-mission-card");
        var btnRestart     = _root.Q<Button>("btn-restart");
        var btnEnterGame   = _root.Q<Button>("btn-enter-game");

        if (btnCloseItems != null)  btnCloseItems.clicked  += () => { CloseModal(); Next(); };
        if (btnCloseMC != null)     btnCloseMC.clicked     += () => { CloseModal(); Next(); };
        if (btnRestart != null)     btnRestart.clicked     += () => { CloseModal(); _cur = 0; Render(); };
        if (btnEnterGame != null)   btnEnterGame.clicked   += () => { CloseModal(); Hide(); /* hook your scene logic here */ };
    }

    private void ApplyFont()
    {
        if (tutorialFont == null) return;
        // 套到所有有文字的元素
        ApplyFontTo(_root);
    }
    private void ApplyFontTo(VisualElement ve)
    {
        if (ve is TextElement t) t.style.unityFont = tutorialFont;
        for (int i = 0; i < ve.childCount; i++) ApplyFontTo(ve[i]);
    }

    // ---------- public API ----------
    public void Run()
    {
        _cur = 0;
        if (startCollapsed) _dialogue?.AddToClassList("collapsed");
        else                _dialogue?.RemoveFromClassList("collapsed");
        Render();
    }

    public void Next()
    {
        if (_cur < steps.Count - 1) { _cur++; Render(); }
        else                          ShowModal(ModalKind.Final);
    }

    public void Prev()
    {
        if (_cur > 0) { _cur--; Render(); }
    }

    public void Skip()
    {
        _cur = steps.Count - 1;
        Render();
        ShowModal(ModalKind.Final);
    }

    public void ToggleCollapse()
    {
        if (_dialogue == null) return;
        if (_dialogue.ClassListContains("collapsed")) _dialogue.RemoveFromClassList("collapsed");
        else                                          _dialogue.AddToClassList("collapsed");
    }

    public void Hide()
    {
        if (_root != null) _root.style.display = DisplayStyle.None;
    }
    public void Show()
    {
        if (_root != null) _root.style.display = DisplayStyle.Flex;
    }

    // ---------- rendering ----------
    private void Render()
    {
        if (steps == null || steps.Count == 0 || _stepTitle == null) return;
        var s = steps[Mathf.Clamp(_cur, 0, steps.Count - 1)];

        _stepBadge.text = (_cur + 1).ToString();
        _stepTitle.text = s.title ?? "";
        _stepBody.text  = s.body  ?? "";

        // summary fallback: summary > target > body first sentence
        string sum = !string.IsNullOrEmpty(s.summary) ? s.summary
                   : !string.IsNullOrEmpty(s.target) ? s.target
                   : FirstSentence(s.body);
        if (!string.IsNullOrEmpty(sum) && sum.Length > 24) sum = sum.Substring(0, 24) + "…";
        _stepSummary.text = sum ?? "";

        // keys row
        _keysRow.Clear();
        if (s.keys != null)
        {
            foreach (var k in s.keys)
            {
                var lbl = new Label(k);
                lbl.AddToClassList("key");
                _keysRow.Add(lbl);
            }
        }

        // target chip
        if (!string.IsNullOrEmpty(s.target))
        {
            _stepTarget.text = s.target;
            _stepTarget.AddToClassList("show");
        }
        else _stepTarget.RemoveFromClassList("show");

        // video slot
        if (s.video) _videoSlot.AddToClassList("show");
        else         _videoSlot.RemoveFromClassList("show");

        // kbd hint text
        bool isAction = !string.IsNullOrEmpty(s.target) && s.modal == ModalKind.None;
        if (_kbdHintText != null)
        {
            if (isAction)                       _kbdHintText.text = "完成目標後自動繼續";
            else if (_cur == steps.Count - 1)   _kbdHintText.text = "完成";
            else                                _kbdHintText.text = "繼續";
        }

        // modal
        CloseModal();
        if (s.modal != ModalKind.None) ShowModal(s.modal);

        UpdateProgress();
    }

    private static string FirstSentence(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        int i = s.IndexOfAny(new[] { '。', '！', '？', '!', '?', '.' });
        return (i > 0) ? s.Substring(0, i) : s;
    }

    // ---------- progress dots ----------
    private void BuildProgressDots()
    {
        if (_progressTrack == null) return;
        _progressTrack.Clear();
        for (int i = 0; i < steps.Count; i++)
        {
            var d = new VisualElement();
            d.AddToClassList("pdot");
            _progressTrack.Add(d);
        }
    }
    private void UpdateProgress()
    {
        if (_progressTrack == null) return;
        for (int i = 0; i < _progressTrack.childCount; i++)
        {
            var d = _progressTrack[i];
            d.RemoveFromClassList("done");
            d.RemoveFromClassList("active");
            if (i < _cur)      d.AddToClassList("done");
            else if (i == _cur) d.AddToClassList("active");
        }
        if (_progressText != null) _progressText.text = $"{_cur + 1} / {steps.Count}";
    }

    // ---------- modals ----------
    private void ShowModal(ModalKind kind)
    {
        CloseModal();
        var m = ModalFor(kind);
        if (m != null) m.AddToClassList("show");
    }
    private void CloseModal()
    {
        _modalItems?.RemoveFromClassList("show");
        _modalMissionCard?.RemoveFromClassList("show");
        _modalFinal?.RemoveFromClassList("show");
    }
    private VisualElement ModalFor(ModalKind k)
    {
        switch (k)
        {
            case ModalKind.Items:       return _modalItems;
            case ModalKind.MissionCard: return _modalMissionCard;
            case ModalKind.Final:       return _modalFinal;
            default:                    return null;
        }
    }
    private bool IsModalOpen()
    {
        return (_modalItems       != null && _modalItems.ClassListContains("show"))
            || (_modalMissionCard != null && _modalMissionCard.ClassListContains("show"))
            || (_modalFinal       != null && _modalFinal.ClassListContains("show"));
    }

    // ---------- keyboard ----------
    private void OnKeyDown(KeyDownEvent e)
    {
        // Tab：debug 用，UIDocument 自己管 cursor
        if (e.keyCode == KeyCode.Tab) { e.StopPropagation(); return; }

        // Modal 開啟時：Enter 關 modal 並推進；其他鍵不處理
        if (IsModalOpen())
        {
            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                // final modal 由按鈕控制，不自動關
                if (_modalFinal != null && _modalFinal.ClassListContains("show")) return;
                CloseModal();
                Next();
                e.StopPropagation();
            }
            return;
        }

        switch (e.keyCode)
        {
            case KeyCode.Space:
                ToggleCollapse();
                e.StopPropagation();
                break;
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                if (!externallyControlled) { Next(); e.StopPropagation(); }
                break;
            case KeyCode.Escape:
                if (!externallyControlled) { Skip(); e.StopPropagation(); }
                break;
            case KeyCode.LeftArrow:
                if (!externallyControlled) { Prev(); e.StopPropagation(); }
                break;
            case KeyCode.RightArrow:
                if (!externallyControlled) { Next(); e.StopPropagation(); }
                break;
        }
    }

    // ---------- default 15 steps (initialized only if list is empty) ----------
    private void EnsureDefaultSteps()
    {
        if (steps != null && steps.Count > 0) return;
        steps = new List<TutorialStep>
        {
            new TutorialStep{ title="移動", summary="WASD 走動 5 秒",
                body="用 W A S D 在場景中走動。試著熟悉一下手感。",
                keys=new[]{"W","A","S","D"}, target="走動 5 秒", video=true },

            new TutorialStep{ title="拾取道具", summary="走近禮物盒撿起",
                body="走近發光的禮物盒，碰到就會自動撿起裡面的道具。",
                target="撿起 1 個禮物盒" },

            new TutorialStep{ title="切換道具", summary="1-6 鍵或滾輪切換",
                body="按數字鍵 1–6 或滾動滑鼠滾輪，切換你目前選中的道具。",
                keys=new[]{"1","2","3","4","5","6","滾輪"} },

            new TutorialStep{ title="對假人使用道具", summary="對假人按 E 鍵",
                body="面向假人，按 E 對它使用目前選中的道具。",
                keys=new[]{"E"}, target="對假人使用 1 次道具", video=true },

            new TutorialStep{ title="道具圖鑑", summary="查看所有道具",
                body="你已經學會基本的拿與用，這裡看一下所有道具的功能。",
                modal=ModalKind.Items },

            new TutorialStep{ title="練習拾取", summary="用過 3 種道具",
                body="場景中會出現更多禮物盒，撿起來自由練習各種道具的效果。",
                target="用過 3 種以上道具" },

            new TutorialStep{ title="道具說明", summary="看右上角介紹",
                body="右上角會顯示你目前選中的道具詳細介紹。試著切換道具觀察文字變化。" },

            new TutorialStep{ title="撿取武器", summary="右鍵撿起武器",
                body="走到武器旁邊，按右鍵把它撿起來。",
                keys=new[]{"RMB 右鍵"} },

            new TutorialStep{ title="揮擊", summary="左鍵打倒假人",
                body="拿著武器按左鍵攻擊。試著打倒假人。",
                keys=new[]{"LMB 左鍵"}, target="打倒 1 個假人", video=true },

            new TutorialStep{ title="假人會掉卡片", summary="撿起掉落卡片",
                body="倒下的假人會掉出一張卡片，撿起它。" },

            new TutorialStep{ title="你的任務：小偷", summary="你的任務卡：小偷",
                body="你拿到了「小偷」任務卡。完成卡片上的任務就能獲勝。",
                modal=ModalKind.MissionCard },

            new TutorialStep{ title="偷取目標", summary="靠近目標 + E 偷取",
                body="場上有發光的目標物體。靠近它，按 E 使用任務卡偷走它。",
                keys=new[]{"E"}, target="偷取 1 個目標 (1/3)" },

            new TutorialStep{ title="完成任務", summary="再偷剩下 2 個",
                body="再偷取剩下 2 個目標。注意：場上的電腦會干擾你，記得避開或先處理掉。",
                target="偷取剩下 2 個目標 (3/3)" },

            new TutorialStep{ title="教學完成 🎉", summary="全部步驟完成！",
                body="你已經學會所有基本操作，準備好進入正式遊戲。",
                modal=ModalKind.Final },
        };
    }
}
