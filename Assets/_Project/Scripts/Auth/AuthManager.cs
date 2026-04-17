using System;
using Photon.Pun;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    public event Action<bool, string> OnAuthResult;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnApplicationQuit()
    {
        // Oyundan çýkarken offline iþaretle
        PlayFabSessionValidator.MarkOffline();
    }

    public void LoginGuest()
    {
        string customId = GuestIdentity.GetOrCreateGuestId();

        var req = new LoginWithCustomIDRequest
        {
            CustomId = customId,
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithCustomID(req,
            result =>
            {
                AuthSession.InitSession();
                string guestName = "Guest" + UnityEngine.Random.Range(1000, 9999);

                AuthSession.PlayFabId = result.PlayFabId;
                AuthSession.Username = guestName;
                AuthSession.IsGuest = true;

                PhotonNetwork.NickName = guestName;

                Debug.Log("[Auth] Guest login OK: " + result.PlayFabId);
                ValidateAndProceed(guestName);
            },
            error =>
            {
                Debug.LogError("[Auth] Guest login failed: " + error.GenerateErrorReport());
                OnAuthResult?.Invoke(false, error.ErrorMessage);
            });
    }

    public void Register(string username, string password)
    {
        username = (username ?? "").Trim();
        password = (password ?? "").Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            OnAuthResult?.Invoke(false, "Kullanýcý adý ve þifre gerekli.");
            return;
        }

        string fakeEmail = $"{username.ToLowerInvariant()}_{Guid.NewGuid():N}@nomail.local";

        var req = new RegisterPlayFabUserRequest
        {
            Username = username,
            Password = password,
            Email = fakeEmail,
            RequireBothUsernameAndEmail = false,
            DisplayName = username
        };

        PlayFabClientAPI.RegisterPlayFabUser(req,
            result =>
            {
                AuthSession.InitSession();
                AuthSession.PlayFabId = result.PlayFabId;
                AuthSession.Username = username;
                AuthSession.IsGuest = false;

                PhotonNetwork.NickName = username;

                Debug.Log("[Auth] Register OK: " + username);
                ValidateAndProceed(username);
            },
            error =>
            {
                Debug.LogError("[Auth] Register failed: " + error.GenerateErrorReport());
                OnAuthResult?.Invoke(false, error.ErrorMessage);
            });
    }

    public void Login(string username, string password)
    {
        username = (username ?? "").Trim();
        password = (password ?? "").Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            OnAuthResult?.Invoke(false, "Kullanýcý adý ve þifre gerekli.");
            return;
        }

        var req = new LoginWithPlayFabRequest
        {
            Username = username,
            Password = password
        };

        PlayFabClientAPI.LoginWithPlayFab(req,
            result =>
            {
                AuthSession.InitSession();
                AuthSession.PlayFabId = result.PlayFabId;
                AuthSession.Username = username;
                AuthSession.IsGuest = false;

                PhotonNetwork.NickName = username;

                Debug.Log("[Auth] Login OK: " + username);
                ValidateAndProceed(username);
            },
            error =>
            {
                Debug.LogError("[Auth] Login failed: " + error.GenerateErrorReport());
                OnAuthResult?.Invoke(false, error.ErrorMessage);
            });
    }

    // YENÝ: Çift giriþ kontrolü
    private void ValidateAndProceed(string displayName)
    {
        PlayFabSessionValidator.CheckConcurrentSession((ok, error) =>
        {
            if (!ok)
            {
                AuthSession.Clear();
                OnAuthResult?.Invoke(false, error);
                return;
            }

            PlayFabClientAPI.UpdateUserTitleDisplayName(
                new UpdateUserTitleDisplayNameRequest { DisplayName = displayName },
                r =>
                {
                    Debug.Log("[Auth] DisplayName set: " + displayName);
                    NotifyAuthSuccess();
                },
                e =>
                {
                    Debug.LogWarning("[Auth] DisplayName set failed: " + e.GenerateErrorReport());
                    NotifyAuthSuccess();
                });
        });
    }

    private void NotifyAuthSuccess()
    {
        if (PhotonNetwork.InRoom)
            FarmAssignmentService.RegisterLocalPlayFabId(AuthSession.PlayFabId);

        OnAuthResult?.Invoke(true, "");
    }
}