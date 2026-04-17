using System;
using UnityEngine;

public static class GuestIdentity
{
    private const string KEY = "guest_custom_id";

    public static string GetOrCreateGuestId()
    {
        if (PlayerPrefs.HasKey(KEY))
            return PlayerPrefs.GetString(KEY);

        string id = "guest_" + Guid.NewGuid().ToString("N");
        PlayerPrefs.SetString(KEY, id);
        PlayerPrefs.Save();
        return id;
    }

    public static string GetGuestId()
    {
        return PlayerPrefs.GetString(KEY, "");
    }

    public static void ClearGuestId()
    {
        PlayerPrefs.DeleteKey(KEY);
        PlayerPrefs.Save();
    }
}