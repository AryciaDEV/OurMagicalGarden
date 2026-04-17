using UnityEngine;

public static class AuthSession
{
    public static string PlayFabId { get; set; }
    public static string Username { get; set; }
    public static bool IsGuest { get; set; }

    // YENÝ: Benzersiz oturum ID'si
    public static string SessionId { get; private set; }

    public static bool IsLoggedIn => !string.IsNullOrWhiteSpace(PlayFabId);

    public static void InitSession()
    {
        SessionId = System.Guid.NewGuid().ToString("N");
        Debug.Log($"[AuthSession] New Session ID: {SessionId}");
    }

    public static void Clear()
    {
        PlayFabId = "";
        Username = "";
        IsGuest = false;
        SessionId = "";
    }
}