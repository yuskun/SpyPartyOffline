using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class CountdownController : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float numberDuration   = 1.0f;
    [SerializeField] private float goDuration       = 1.3f;
    [SerializeField] private float startDelay       = 0.6f;
    [SerializeField] private bool  autoPlayOnEnable = true;

    [Header("Font (optional)")]
    [SerializeField] private Font countdownFont;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool playProceduralAudio = true;

    private VisualElement _root;
    private VisualElement _stickerStack;
    private VisualElement _sparkleHost;
    private VisualElement _ringHost;
    private List<Label>   _stickers = new List<Label>();

    private void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null || doc.rootVisualElement == null) return;
        _root = doc.rootVisualElement.Q<VisualElement>("root");
        if (_root == null) return;

        _stickerStack = _root.Q<VisualElement>("sticker-stack");
        _sparkleHost  = _root.Q<VisualElement>("sparkle-host");
        _ringHost     = _root.Q<VisualElement>("ring-host");

        _stickers.Clear();
        for (int i = 9; i >= 1; i--) _stickers.Add(_root.Q<Label>("layer-" + i));
        _stickers.Add(_root.Q<Label>("layer-main"));

        if (countdownFont != null)
            foreach (var s in _stickers) if (s != null) s.style.unityFont = countdownFont;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (autoPlayOnEnable) StartCoroutine(RunDelayed());
    }

    public void Run()
    {
        StopAllCoroutines();
        StartCoroutine(RunCountdown());
    }

    private IEnumerator RunDelayed()
    {
        yield return new WaitForSeconds(startDelay);
        yield return RunCountdown();
    }

    private IEnumerator RunCountdown()
    {
        SetText("");
        SetGo(false);
        HideStack();

        var freqs = new float[] { 523f, 659f, 784f };
        var nums  = new string[] { "3", "2", "1" };
        for (int i = 0; i < nums.Length; i++)
        {
            SetText(nums[i]);
            SetGo(false);
            SpawnSparkles(6, false);
            PlayBlip(freqs[i], 0.18f);
            yield return AnimatePeel(numberDuration, false);
        }

        SetText("GO!");
        SetGo(true);
        SpawnSparkles(10, true);
        SpawnRingBurst();
        PlayChord();
        yield return AnimatePeel(goDuration, true);

        HideStack();
    }

    private void SetText(string s)
    {
        for (int i = 0; i < _stickers.Count; i++)
            if (_stickers[i] != null) _stickers[i].text = s;
    }

    private void SetGo(bool isGo)
    {
        for (int i = 0; i < _stickers.Count; i++)
        {
            var lbl = _stickers[i]; if (lbl == null) continue;
            if (isGo) lbl.AddToClassList("go");
            else      lbl.RemoveFromClassList("go");
        }
    }

    private void HideStack()
    {
        if (_stickerStack == null) return;
        _stickerStack.style.opacity = 0f;
        _stickerStack.style.scale   = new StyleScale(new Scale(new Vector3(0, 0, 1)));
    }

    // (time, scale, rotateDeg, translateY, opacity)
    private static readonly float[][] PeelKeys = new float[][] {
        new float[] { 0.00f, 0.00f, -22f,  40f, 0f },
        new float[] { 0.16f, 1.22f,  10f, -16f, 1f },
        new float[] { 0.32f, 0.95f,  -4f,   0f, 1f },
        new float[] { 0.48f, 1.06f,   2f,   0f, 1f },
        new float[] { 0.62f, 0.99f,   0f,   0f, 1f },
        new float[] { 0.78f, 1.02f,   0f,   0f, 1f },
        new float[] { 1.00f, 1.45f,   8f, -40f, 0f },
    };
    private static readonly float[][] PeelGoKeys = new float[][] {
        new float[] { 0.00f, 0.00f, -28f,  80f, 0f },
        new float[] { 0.12f, 1.50f,  12f, -22f, 1f },
        new float[] { 0.28f, 0.88f,  -5f,   0f, 1f },
        new float[] { 0.44f, 1.18f,   3f,   0f, 1f },
        new float[] { 0.62f, 0.96f,  -1f,   0f, 1f },
        new float[] { 0.80f, 1.02f,   0f,   0f, 1f },
        new float[] { 1.00f, 1.60f,   0f,   0f, 0f },
    };

    private IEnumerator AnimatePeel(float dur, bool isGo)
    {
        if (_stickerStack == null) yield break;
        var keys = isGo ? PeelGoKeys : PeelKeys;
        float t = 0f;
        while (t < dur)
        {
            float u = Mathf.Clamp01(t / dur);
            int i = 0;
            while (i < keys.Length - 1 && keys[i + 1][0] < u) i++;
            float a = keys[i][0], b = keys[i + 1][0];
            float k = (u - a) / Mathf.Max(0.0001f, b - a);
            float s  = Mathf.Lerp(keys[i][1], keys[i + 1][1], k);
            float r  = Mathf.Lerp(keys[i][2], keys[i + 1][2], k);
            float ty = Mathf.Lerp(keys[i][3], keys[i + 1][3], k);
            float op = Mathf.Lerp(keys[i][4], keys[i + 1][4], k);

            _stickerStack.style.scale     = new StyleScale(new Scale(new Vector3(s, s, 1)));
            _stickerStack.style.rotate    = new StyleRotate(new Rotate(new Angle(r, AngleUnit.Degree)));
            _stickerStack.style.translate = new StyleTranslate(new Translate(0, ty, 0));
            _stickerStack.style.opacity   = op;

            t += Time.deltaTime;
            yield return null;
        }
        var last = keys[keys.Length - 1];
        _stickerStack.style.scale   = new StyleScale(new Scale(new Vector3(last[1], last[1], 1)));
        _stickerStack.style.opacity = last[4];
    }

    private static readonly Color[] SparkleColors = new Color[] {
        new Color(1.00f, 0.85f, 0.39f), // yellow
        new Color(0.36f, 0.89f, 1.00f), // cyan
        new Color(1.00f, 1.00f, 1.00f), // white
        new Color(1.00f, 0.77f, 0.90f), // soft pink
        new Color(0.72f, 0.96f, 0.88f), // mint
    };

    private void SpawnSparkles(int count, bool isGo)
    {
        if (_sparkleHost == null) return;
        for (int i = 0; i < count; i++)
        {
            var s = new Label("★"); // ★
            s.AddToClassList("sparkle");
            s.pickingMode = PickingMode.Ignore;
            float angle = (Mathf.PI * 2f) * (i / (float)count) + Random.Range(-0.25f, 0.25f);
            float dist  = 80f + Random.value * 90f;
            float fx = Mathf.Cos(angle) * dist;
            float fy = Mathf.Sin(angle) * dist - 20f;
            float sz = 18f + Random.value * 14f;
            s.style.fontSize = sz;
            var col = isGo
                ? (Random.value < 0.5f ? new Color(1f, 0.85f, 0.39f) : Color.white)
                : SparkleColors[i % SparkleColors.Length];
            s.style.color = col;
            s.style.position = Position.Absolute;
            s.style.left = new StyleLength(new Length(50f, LengthUnit.Percent));
            s.style.top  = new StyleLength(new Length(50f, LengthUnit.Percent));
            s.style.opacity = 0f;
            _sparkleHost.Add(s);
            StartCoroutine(AnimateSparkle(s, fx, fy, sz));
        }
    }

    private IEnumerator AnimateSparkle(Label s, float fx, float fy, float sz)
    {
        const float dur = 0.55f;
        float t = 0f;
        while (t < dur)
        {
            float u = t / dur;
            float scale, opacity, x, y, rot;
            if (u < 0.25f)
            {
                float k = u / 0.25f;
                scale   = Mathf.Lerp(0f, 1.5f, k);
                opacity = k;
                x = Mathf.Lerp(0f, fx, k);
                y = Mathf.Lerp(0f, fy, k);
                rot = Mathf.Lerp(0f, 140f, k);
            }
            else
            {
                float k = (u - 0.25f) / 0.75f;
                scale   = Mathf.Lerp(1.5f, 0.4f, k);
                opacity = Mathf.Lerp(1f, 0f, k * k); // ease-in fade so it's bright longer
                x = Mathf.Lerp(fx, fx * 1.2f, k);
                y = Mathf.Lerp(fy, fy * 1.2f, k);
                rot = Mathf.Lerp(140f, 260f, k);
            }
            s.style.translate = new StyleTranslate(new Translate(x - sz * 0.5f, y - sz * 0.5f, 0));
            s.style.scale     = new StyleScale(new Scale(new Vector3(scale, scale, 1)));
            s.style.rotate    = new StyleRotate(new Rotate(new Angle(rot, AngleUnit.Degree)));
            s.style.opacity   = opacity;
            t += Time.deltaTime;
            yield return null;
        }
        if (s.parent != null) s.RemoveFromHierarchy();
    }

    private void SpawnRingBurst()
    {
        if (_ringHost == null) return;
        for (int i = 0; i < 3; i++)
        {
            var r = new VisualElement();
            r.AddToClassList("ring-burst");
            r.AddToClassList("r" + (i + 1));
            r.pickingMode = PickingMode.Ignore;
            _ringHost.Add(r);
            StartCoroutine(AnimateRingBurst(r, i * 0.1f));
        }
    }

    private IEnumerator AnimateRingBurst(VisualElement r, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        const float dur = 1.1f;
        float t = 0f;
        while (t < dur)
        {
            float u = t / dur;
            float scale = Mathf.Lerp(0f, 8f, u);
            float opacity = (u < 0.2f)
                ? Mathf.Lerp(0f, 1f, u / 0.2f)
                : Mathf.Lerp(1f, 0f, (u - 0.2f) / 0.8f);
            r.style.scale   = new StyleScale(new Scale(new Vector3(scale, scale, 1)));
            r.style.opacity = opacity;
            t += Time.deltaTime;
            yield return null;
        }
        if (r.parent != null) r.RemoveFromHierarchy();
    }

    // ----- procedural audio -----
    private void PlayBlip(float freq, float dur)
    {
        if (!playProceduralAudio || audioSource == null) return;
        audioSource.PlayOneShot(MakeTone(freq, dur, 0.18f, 1)); // 1=triangle
    }

    private void PlayChord()
    {
        if (!playProceduralAudio || audioSource == null) return;
        audioSource.PlayOneShot(MakeTone(523f, 0.45f, 0.10f, 3));
        audioSource.PlayOneShot(MakeTone(659f, 0.50f, 0.16f, 1));
        audioSource.PlayOneShot(MakeTone(784f, 0.55f, 0.16f, 1));
        StartCoroutine(DelayedTone(0.08f, 1046f, 0.65f, 0.14f));
    }

    private IEnumerator DelayedTone(float delay, float freq, float dur, float gain)
    {
        yield return new WaitForSeconds(delay);
        if (audioSource != null) audioSource.PlayOneShot(MakeTone(freq, dur, gain, 1));
    }

    // toneType: 0=sine, 1=triangle, 2=square, 3=saw
    private AudioClip MakeTone(float freq, float dur, float gain, int toneType)
    {
        const int sr = 44100;
        int n = Mathf.RoundToInt(dur * sr);
        var data = new float[n];
        float twoPi = Mathf.PI * 2f;
        float attack = sr * 0.005f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)sr;
            float phase = (freq * t) % 1f;
            float v;
            if      (toneType == 0) v = Mathf.Sin(twoPi * phase);
            else if (toneType == 1) v = 4f * Mathf.Abs(phase - 0.5f) - 1f;
            else if (toneType == 2) v = (phase < 0.5f) ? 1f : -1f;
            else                    v = 2f * phase - 1f;
            float env = (i < attack) ? (i / attack) : Mathf.Exp(-3f * t / dur);
            data[i] = v * gain * env;
        }
        var clip = AudioClip.Create("tone", n, 1, sr, false);
        clip.SetData(data, 0);
        return clip;
    }
}
