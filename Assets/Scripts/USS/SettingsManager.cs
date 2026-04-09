using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    private UIDocument _doc;
    private VisualElement _root;

    private int _pendingQualityLevel = 1;
    private List<Button> _qualityButtons = new List<Button>();

    // 定義儲存用的 Key 名稱
    private const string QUALITY_KEY = "UserQualityLevel";
    private const string MASTER_VOL_KEY = "MasterVolume";
    private const string MUSIC_VOL_KEY = "MusicVolume";
    private const string SFX_VOL_KEY = "SFXVolume";

    private void Awake()
    {
        _doc = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        _root = _doc.rootVisualElement;

        // 1. 初始化各項元件
        SetupQualityButtons();
        SetupVolumeSlider("MasterVolume", MASTER_VOL_KEY);
        SetupVolumeSlider("MusicVolume", MUSIC_VOL_KEY);
        SetupVolumeSlider("SFXVolume", SFX_VOL_KEY);

        // 2. 載入上次儲存的紀錄
        LoadSavedSettings();

        // 3. 按鈕事件
        var confirmBtn = _root.Q<Button>("ConfirmBtn");
        if (confirmBtn != null) confirmBtn.clicked += ApplyAndSaveSettings;
    }

    private void SetupQualityButtons()
    {
        _qualityButtons.Clear();
        _qualityButtons.Add(_root.Q<Button>("LowBtn"));
        _qualityButtons.Add(_root.Q<Button>("MidBtn"));
        _qualityButtons.Add(_root.Q<Button>("HighBtn"));

        for (int i = 0; i < _qualityButtons.Count; i++)
        {
            int level = i;
            _qualityButtons[i].clicked += () => SelectQualityUI(level);
        }
    }

    private void SelectQualityUI(int level)
    {
        _pendingQualityLevel = Mathf.Clamp(level, 0, _qualityButtons.Count - 1);
        for (int i = 0; i < _qualityButtons.Count; i++)
        {
            if (i == _pendingQualityLevel) _qualityButtons[i].AddToClassList("active");
            else _qualityButtons[i].RemoveFromClassList("active");
        }
    }

    private void SetupVolumeSlider(string sliderName, string saveKey)
    {
        var slider = _root.Q<Slider>(sliderName);
        var valueLabel = slider?.parent?.Q<Label>(null, "value-pill");

        if (slider != null && valueLabel != null)
        {
            slider.RegisterValueChangedCallback(evt => {
                valueLabel.text = Mathf.RoundToInt(evt.newValue).ToString();
            });
        }
    }

    // --- 關鍵邏輯：讀取紀錄 ---
    private void LoadSavedSettings()
    {
        // 讀取畫質 (預設值為 1)
        int savedQuality = PlayerPrefs.GetInt(QUALITY_KEY, 1);
        SelectQualityUI(savedQuality);
        QualitySettings.SetQualityLevel(savedQuality);

        // 讀取音量 (預設值為 80)
        LoadSliderValue("MasterVolume", MASTER_VOL_KEY, 80f);
        LoadSliderValue("MusicVolume", MUSIC_VOL_KEY, 60f);
        LoadSliderValue("SFXVolume", SFX_VOL_KEY, 70f);

        // 立即應用全域音量
        AudioListener.volume = PlayerPrefs.GetFloat(MASTER_VOL_KEY, 80f) / 100f;
    }

    private void LoadSliderValue(string sliderName, string saveKey, float defaultValue)
    {
        var slider = _root.Q<Slider>(sliderName);
        float savedVal = PlayerPrefs.GetFloat(saveKey, defaultValue);
        if (slider != null)
        {
            slider.value = savedVal;
            var valueLabel = slider.parent?.Q<Label>(null, "value-pill");
            if (valueLabel != null) valueLabel.text = Mathf.RoundToInt(savedVal).ToString();
        }
    }

    // --- 關鍵邏輯：應用並儲存 ---
    public void ApplyAndSaveSettings()
    {
        // 1. 儲存畫質
        QualitySettings.SetQualityLevel(_pendingQualityLevel);
        PlayerPrefs.SetInt(QUALITY_KEY, _pendingQualityLevel);

        // 2. 儲存各個 Slider 數值
        SaveSliderValue("MasterVolume", MASTER_VOL_KEY);
        SaveSliderValue("MusicVolume", MUSIC_VOL_KEY);
        SaveSliderValue("SFXVolume", SFX_VOL_KEY);

        // 3. 真正寫入磁碟
        PlayerPrefs.Save();
        
        Debug.Log("設定已儲存至系統紀錄！");
    }

    private void SaveSliderValue(string sliderName, string saveKey)
    {
        var slider = _root.Q<Slider>(sliderName);
        if (slider != null)
        {
            PlayerPrefs.SetFloat(saveKey, slider.value);
            if (sliderName == "MasterVolume") AudioListener.volume = slider.value / 100f;
        }
    }
}