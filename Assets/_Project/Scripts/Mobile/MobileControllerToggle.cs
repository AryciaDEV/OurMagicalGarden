using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MobileControllerToggle : MonoBehaviour
{
    [Header("Mobile Controller UI")]
    [SerializeField] private GameObject mobileControllerPanel;
    [SerializeField] private FixedJoystick movementJoystick;
    [SerializeField] private Button jumpButton;

    [Header("Toggle Button")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private TMP_Text toggleButtonText;
    [SerializeField] private string onText = "MOBIL KONTROL: ACIK";
    [SerializeField] private string offText = "MOBIL KONTROL: KAPALI";

    private const string MOBILE_CONTROL_KEY = "MobileControlEnabled";
    private bool isEnabled;

    // ===== STATIC EVENT - PlayerMovement dinleyecek =====
    public static System.Action<bool> OnMobileControlToggled;

    private void Start()
    {
        // Kayýtlý ayarý yükle
        LoadSetting();

        // Butonu dinle
        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleMobileControl);

        // UI'ý güncelle
        UpdateUI();

        // Event'i tetikle (baþlangýç durumu)
        OnMobileControlToggled?.Invoke(isEnabled);

        Debug.Log($"[MobileController] Started with: {(isEnabled ? "ON" : "OFF")}");
    }

    private void ToggleMobileControl()
    {
        isEnabled = !isEnabled;

        // Kaydet
        PlayerPrefs.SetInt(MOBILE_CONTROL_KEY, isEnabled ? 1 : 0);
        PlayerPrefs.Save();

        // UI'ý güncelle
        UpdateUI();

        // Event'i tetikle (PlayerMovement bunu dinleyecek)
        OnMobileControlToggled?.Invoke(isEnabled);

        Debug.Log($"[MobileController] Toggled: {(isEnabled ? "ON" : "OFF")}");
    }

    private void UpdateUI()
    {
        // Panel'i aç/kapa
        if (mobileControllerPanel != null)
            mobileControllerPanel.SetActive(isEnabled);

        // Buton text'ini güncelle
        if (toggleButtonText != null)
            toggleButtonText.text = isEnabled ? onText : offText;
    }

    private void LoadSetting()
    {
        isEnabled = PlayerPrefs.GetInt(MOBILE_CONTROL_KEY, 0) == 1;
        Debug.Log($"[MobileController] Loaded setting: {(isEnabled ? "ON" : "OFF")}");
    }

    // Diðer script'lerin kontrol edebilmesi için
    public bool IsMobileControlEnabled()
    {
        return isEnabled;
    }

    // Joystick ve butonlara eriþim için
    public FixedJoystick GetJoystick() => movementJoystick;
    public Button GetJumpButton() => jumpButton;
}