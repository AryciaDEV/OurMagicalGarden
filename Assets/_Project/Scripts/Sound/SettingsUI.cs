using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("Grafik Ayarları")]
    [SerializeField] private Button lowQualityButton;
    [SerializeField] private Button mediumQualityButton;
    [SerializeField] private Button highQualityButton;

    [Header("Ses Ayarları")]
    [SerializeField] private Slider soundFxSlider;
    [SerializeField] private Slider musicSlider;

    [Header("Mobile Controller Toggle")]
    [SerializeField] private Button mobileControllerToggleButton;

    private const string QUALITY_PREF_KEY = "QualityLevel";

    private void Start()
    {
        // Grafik butonları
        if (lowQualityButton != null)
            lowQualityButton.onClick.AddListener(() => SetQualityLevel(0));

        if (mediumQualityButton != null)
            mediumQualityButton.onClick.AddListener(() => SetQualityLevel(1));

        if (highQualityButton != null)
            highQualityButton.onClick.AddListener(() => SetQualityLevel(2));

        // Ses slider'ları - 0-100 aralığından 0-1 aralığına çevir
        if (soundFxSlider != null && SoundFXManager.Instance != null)
        {
            // Slider 0-100, ama manager 0-1 bekliyor
            soundFxSlider.value = SoundFXManager.Instance.GetVolume() * 100f;
            soundFxSlider.onValueChanged.AddListener(OnSoundFxVolumeChanged);
        }

        if (musicSlider != null && MusicManager.Instance != null)
        {
            // Slider 0-100, ama manager 0-1 bekliyor
            musicSlider.value = MusicManager.Instance.GetVolume() * 100f;
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (mobileControllerToggleButton != null)
        {
            mobileControllerToggleButton.onClick.AddListener(OpenMobileControllerPanel);
        }

        LoadQualitySettings();
    }

    private void OpenMobileControllerPanel()
    {
        MobileControllerToggle toggle = FindObjectOfType<MobileControllerToggle>();
        if (toggle != null)
        {
            Button toggleButton = toggle.GetComponent<Button>();
            if (toggleButton != null)
                toggleButton.onClick.Invoke();
        }
    }

    private void SetQualityLevel(int level)
    {
        PlayerPrefs.SetInt(QUALITY_PREF_KEY, level);
        PlayerPrefs.Save();
        ApplyQualitySettings(level);
        Debug.Log($"[Settings] Graphics: {(level == 0 ? "LOW" : level == 1 ? "MEDIUM" : "HIGH")}");
    }

    private void ApplyQualitySettings(int level)
    {
        switch (level)
        {
            case 0:
                QualitySettings.SetQualityLevel(0, true);
                QualitySettings.shadowResolution = ShadowResolution.Low;
                QualitySettings.antiAliasing = 0;
                QualitySettings.masterTextureLimit = 2;
                break;
            case 1:
                QualitySettings.SetQualityLevel(2, true);
                QualitySettings.shadowResolution = ShadowResolution.Medium;
                QualitySettings.antiAliasing = 2;
                QualitySettings.masterTextureLimit = 1;
                break;
            case 2:
                QualitySettings.SetQualityLevel(4, true);
                QualitySettings.shadowResolution = ShadowResolution.High;
                QualitySettings.antiAliasing = 4;
                QualitySettings.masterTextureLimit = 0;
                break;
        }
    }

    private void LoadQualitySettings()
    {
        int savedQuality = PlayerPrefs.GetInt(QUALITY_PREF_KEY, 1);
        SetQualityLevel(savedQuality);
    }

    // DÜZELTİLDİ: Slider 0-100 → Volume 0-1
    private void OnSoundFxVolumeChanged(float sliderValue)
    {
        if (SoundFXManager.Instance != null)
        {
            float normalizedVolume = sliderValue / 100f; // 0-100 → 0-1
            SoundFXManager.Instance.SetVolume(normalizedVolume);
        }
    }

    // DÜZELTİLDİ: Slider 0-100 → Volume 0-1
    private void OnMusicVolumeChanged(float sliderValue)
    {
        if (MusicManager.Instance != null)
        {
            float normalizedVolume = sliderValue / 100f; // 0-100 → 0-1
            MusicManager.Instance.SetVolume(normalizedVolume);
        }
    }
}