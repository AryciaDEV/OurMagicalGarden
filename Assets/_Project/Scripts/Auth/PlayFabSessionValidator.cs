using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using System;
using System.Collections.Generic;

public static class PlayFabSessionValidator
{
    private const string ONLINE_KEY = "is_online";
    private const string SESSION_KEY = "session_id";
    private const string LAST_SEEN_KEY = "last_seen";

    public static void CheckConcurrentSession(Action<bool, string> onResult)
    {
        if (!AuthSession.IsLoggedIn)
        {
            onResult?.Invoke(false, "Not logged in");
            return;
        }

        PlayFabClientAPI.GetUserData(new GetUserDataRequest
        {
            PlayFabId = AuthSession.PlayFabId
        },
        result =>
        {
            bool isOnlineElsewhere = false;
            string existingSession = "";

            if (result.Data != null)
            {
                if (result.Data.TryGetValue(ONLINE_KEY, out var onlineData))
                    isOnlineElsewhere = onlineData.Value == "true";

                if (result.Data.TryGetValue(SESSION_KEY, out var sessionData))
                {
                    existingSession = sessionData.Value;

                    if (isOnlineElsewhere && existingSession != AuthSession.SessionId)
                    {
                        // Stale session kontrolü (5 dakika)
                        if (result.Data.TryGetValue(LAST_SEEN_KEY, out var lastSeenData))
                        {
                            if (long.TryParse(lastSeenData.Value, out long lastSeen))
                            {
                                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                if (now - lastSeen > 30) // 5 dk
                                {
                                    isOnlineElsewhere = false;
                                    Debug.Log("[SessionValidator] Stale session, allowing login");
                                }
                            }
                        }
                    }
                    else if (existingSession == AuthSession.SessionId)
                    {
                        isOnlineElsewhere = false;
                    }
                }
            }

            if (isOnlineElsewhere)
            {
                onResult?.Invoke(false, "This account is currently in use on another device.");
            }
            else
            {
                MarkOnline();
                onResult?.Invoke(true, "");
            }
        },
        error =>
        {
            Debug.LogError("[SessionValidator] Check failed: " + error.GenerateErrorReport());
            onResult?.Invoke(true, ""); // Hata durumunda izin ver
        });
    }

    public static void MarkOnline()
    {
        if (!AuthSession.IsLoggedIn) return;

        var data = new Dictionary<string, string>
        {
            { ONLINE_KEY, "true" },
            { SESSION_KEY, AuthSession.SessionId },
            { LAST_SEEN_KEY, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }
        };

        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = data
        },
        result => Debug.Log("[SessionValidator] Marked online"),
        error => Debug.LogError("[SessionValidator] Mark online failed: " + error.GenerateErrorReport()));
    }

    public static void MarkOffline()
    {
        if (!AuthSession.IsLoggedIn) return;

        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { ONLINE_KEY, "false" },
                { LAST_SEEN_KEY, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }
            }
        },
        result => Debug.Log("[SessionValidator] Marked offline"),
        error => Debug.LogError("[SessionValidator] Mark offline failed: " + error.GenerateErrorReport()));
    }

    public static void SendHeartbeat()
    {
        if (!AuthSession.IsLoggedIn) return;

        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { LAST_SEEN_KEY, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }
            }
        }, null, null);
    }
}