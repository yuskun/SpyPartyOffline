using UnityEngine;
using UnityEngine.UIElements;

public class SettingsManager : MonoBehaviour
{
    private UIDocument _doc;

    private void Awake()
    {
        _doc = GetComponent<UIDocument>();
    }

    // 1. 控制畫質與音量的主要邏輯 (給 ApplyButton 呼叫)
    public void ApplyAllSettings()
    {
        var root = _doc.rootVisualElement;

        // 處理畫質 (使用 Unity 內建的 QualitySettings)
        var qualityDropdown = root.Q<DropdownField>("QualityDropdown");
        QualitySettings.SetQualityLevel(qualityDropdown.index);

        // 處理音量 (假設你有 AudioSource 或 AudioManager)
        var volumeSlider = root.Q<Slider>("MasterVolume");
        float volume = volumeSlider.value / 100f; // 轉為 0~1
        AudioListener.volume = volume; // 直接控制全域音量

        Debug.Log($"設定已應用: 畫質 {qualityDropdown.value}, 音量 {volume}");
    }

    // 2. 開啟 Google 表單 (給 FeedbackButton 呼叫)
    public void OpenFeedbackForm()
    {
        Application.OpenURL("https://forms.gle/RSA6jyeBHTVxXYJ66"); // 換成你的網址
    }
}