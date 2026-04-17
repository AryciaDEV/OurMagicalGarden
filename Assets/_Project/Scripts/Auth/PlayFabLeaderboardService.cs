using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public static class PlayFabLeaderboardService
{
    public const string STAT_COINS = "Coins";

    public static void SubmitCoins(int coins)
    {
        if (!AuthSession.IsLoggedIn) return;

        PlayFabClientAPI.UpdatePlayerStatistics(
            new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate
                    {
                        StatisticName = STAT_COINS,
                        Value = Mathf.Max(0, coins)
                    }
                }
            },
            result =>
            {
                Debug.Log("[Leaderboard] Coins submitted: " + coins);
            },
            error =>
            {
                Debug.LogError("[Leaderboard] SubmitCoins failed: " + error.GenerateErrorReport());
            });
    }

    public static void GetCoinsLeaderboard(
        int startPosition,
        int maxResults,
        Action<GetLeaderboardResult> onSuccess,
        Action<PlayFabError> onError)
    {
        PlayFabClientAPI.GetLeaderboard(
            new GetLeaderboardRequest
            {
                StatisticName = STAT_COINS,
                StartPosition = Mathf.Max(0, startPosition),
                MaxResultsCount = Mathf.Clamp(maxResults, 1, 100)
            },
            result =>
            {
                Debug.Log($"[Leaderboard] Loaded {result.Leaderboard?.Count ?? 0} rows.");
                onSuccess?.Invoke(result);
            },
            error =>
            {
                Debug.LogError("[Leaderboard] GetCoinsLeaderboard failed: " + error.GenerateErrorReport());
                onError?.Invoke(error);
            });
    }
}