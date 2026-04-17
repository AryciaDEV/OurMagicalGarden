using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    [Header("Auth UI")]
    public GameObject authPanel;
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public Button guestButton;
    public Button registerButton;
    public Button loginButton;
    public TMP_Text statusText;

    private bool _busy;

    private void OnEnable()
    {
        if (guestButton) guestButton.onClick.AddListener(OnGuestClicked);
        if (registerButton) registerButton.onClick.AddListener(OnRegisterClicked);
        if (loginButton) loginButton.onClick.AddListener(OnLoginClicked);

        if (AuthManager.Instance != null)
            AuthManager.Instance.OnAuthResult += OnAuthResult;

        if (SaveManager.Instance != null)
            SaveManager.Instance.OnSaveLoaded += OnSaveLoaded;
    }

    private void OnDisable()
    {
        if (guestButton) guestButton.onClick.RemoveListener(OnGuestClicked);
        if (registerButton) registerButton.onClick.RemoveListener(OnRegisterClicked);
        if (loginButton) loginButton.onClick.RemoveListener(OnLoginClicked);

        if (AuthManager.Instance != null)
            AuthManager.Instance.OnAuthResult -= OnAuthResult;

        if (SaveManager.Instance != null)
            SaveManager.Instance.OnSaveLoaded -= OnSaveLoaded;
    }

    private void Start()
    {
        _busy = false;
        SetButtons(true);
        SetStatus("Log in or continue as a guest.");
    }

    private void OnGuestClicked()
    {
        if (_busy) return;

        _busy = true;
        SetButtons(false);
        SetStatus("Guest is logging in...");

        if (AuthManager.Instance == null)
        {
            FailEarly("Error. Contact support.");
            return;
        }

        AuthManager.Instance.LoginGuest();
    }

    private void OnRegisterClicked()
    {
        if (_busy) return;

        string user = usernameInput ? usernameInput.text.Trim() : "";
        string pass = passwordInput ? passwordInput.text.Trim() : "";

        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            SetStatus("Username and password are required.");
            return;
        }

        _busy = true;
        SetButtons(false);
        SetStatus("Account registration is being created...");

        if (AuthManager.Instance == null)
        {
            FailEarly("Error. Contact support.");
            return;
        }

        AuthManager.Instance.Register(user, pass);
    }

    private void OnLoginClicked()
    {
        if (_busy) return;

        string user = usernameInput ? usernameInput.text.Trim() : "";
        string pass = passwordInput ? passwordInput.text.Trim() : "";

        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            SetStatus("Username and password are required.");
            return;
        }

        _busy = true;
        SetButtons(false);
        SetStatus("Logging in...");

        if (AuthManager.Instance == null)
        {
            FailEarly("Error. Contact support.");
            return;
        }

        AuthManager.Instance.Login(user, pass);
    }

    private void OnAuthResult(bool ok, string error)
    {
        if (!ok)
        {
            _busy = false;
            SetButtons(true);

            // YENÝ: Çift giriþ hatasýný vurgula
            if (error.Contains("baþka bir cihazda") || error.Contains("kullanýmda"))
            {
                SetStatus("?? " + error);
            }
            else
            {
                SetStatus(string.IsNullOrWhiteSpace(error) ? "Operation failed." : error);
            }

            return;
        }

        SetStatus("Loading account registration...");

        if (SaveManager.Instance == null)
        {
            FailEarly("Error. Contact support.");
            return;
        }

        SaveManager.Instance.LoadSave();
    }

    private void OnSaveLoaded(PlayerSaveData data)
    {
        SetStatus("Connecting to the server...");

        if (NetworkBootstrap.Instance == null)
        {
            FailEarly("Error. Contact support.");
            return;
        }

        NetworkBootstrap.Instance.Connect();
    }

    private void FailEarly(string msg)
    {
        _busy = false;
        SetButtons(true);
        SetStatus(msg);
        Debug.LogError("[LoginUI] " + msg);
    }

    private void SetStatus(string s)
    {
        if (statusText) statusText.text = s;
    }

    private void SetButtons(bool on)
    {
        if (guestButton) guestButton.interactable = on;
        if (registerButton) registerButton.interactable = on;
        if (loginButton) loginButton.interactable = on;
    }
}