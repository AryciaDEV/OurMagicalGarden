using System;
using System.Collections.Generic;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GlobalLeaderboardUI : MonoBehaviour
{
    [Header("UI")]
    public Transform rowsRoot;
    public GlobalLeaderboardRowUI rowPrefab;
    public TMP_Text lastUpdatedText;
    public TMP_Text statusText;
    public Button refreshButton;

    [Header("Config")]
    [Range(1, 100)] public int maxResults = 25;
    public bool refreshOnEnable = true;
    public bool autoRefresh = false;
    public float autoRefreshSeconds = 30f;

    private readonly List<GlobalLeaderboardRowUI> _spawnedRows = new();
    private float _timer;
    private bool _loading;

    private void OnEnable()
    {
        if (refreshButton)
        {
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(Refresh);
        }

        if (refreshOnEnable)
            Refresh();
    }

    private void OnDisable()
    {
        if (refreshButton)
            refreshButton.onClick.RemoveAllListeners();
    }

    private void Update()
    {
        if (!autoRefresh || _loading) return;

        _timer += Time.deltaTime;
        if (_timer >= autoRefreshSeconds)
        {
            _timer = 0f;
            Refresh();
        }
    }

    public void Refresh()
    {
        if (_loading) return;
        if (!AuthSession.IsLoggedIn)
        {
            SetStatus("Leaderboard için giriþ gerekli.");
            return;
        }

        if (!rowsRoot || !rowPrefab)
        {
            SetStatus("Leaderboard UI referanslarý eksik.");
            return;
        }

        _loading = true;
        SetStatus("Leaderboard yükleniyor...");

        if (refreshButton)
            refreshButton.interactable = false;

        PlayFabLeaderboardService.GetCoinsLeaderboard(
            0,
            maxResults,
            OnLeaderboardLoaded,
            OnLeaderboardFailed
        );
    }

    private void OnLeaderboardLoaded(GetLeaderboardResult result)
    {
        _loading = false;
        _timer = 0f;

        if (refreshButton)
            refreshButton.interactable = true;

        ClearRows();

        if (result == null || result.Leaderboard == null || result.Leaderboard.Count == 0)
        {
            SetStatus("Leaderboard boþ.");
            UpdateLastUpdatedTime();
            return;
        }

        for (int i = 0; i < result.Leaderboard.Count; i++)
        {
            PlayerLeaderboardEntry entry = result.Leaderboard[i];
            var row = Instantiate(rowPrefab, rowsRoot);
            row.gameObject.SetActive(true);

            string nickname = ResolveDisplayName(entry);
            int rank = entry.Position + 1;
            int coins = entry.StatValue;

            row.Bind(rank, nickname, coins);
            _spawnedRows.Add(row);
        }

        SetStatus("");
        UpdateLastUpdatedTime();
    }

    private void OnLeaderboardFailed(PlayFab.PlayFabError error)
    {
        _loading = false;

        if (refreshButton)
            refreshButton.interactable = true;

        SetStatus(string.IsNullOrWhiteSpace(error?.ErrorMessage)
            ? "Leaderboard yüklenemedi."
            : error.ErrorMessage);

        UpdateLastUpdatedTime();
    }

    private void ClearRows()
    {
        for (int i = 0; i < _spawnedRows.Count; i++)
        {
            if (_spawnedRows[i] != null)
                Destroy(_spawnedRows[i].gameObject);
        }
        _spawnedRows.Clear();
    }

    private string ResolveDisplayName(PlayerLeaderboardEntry entry)
    {
        if (entry == null) return "-";

        if (!string.IsNullOrWhiteSpace(entry.DisplayName))
            return entry.DisplayName;

        if (!string.IsNullOrWhiteSpace(entry.PlayFabId))
        {
            // DisplayName gelmezse fallback
            if (entry.PlayFabId.Length > 8)
                return "Player-" + entry.PlayFabId.Substring(0, 8);

            return "Player-" + entry.PlayFabId;
        }

        return "-";
    }

    private void UpdateLastUpdatedTime()
    {
        if (!lastUpdatedText) return;

        DateTime now = DateTime.Now;
        lastUpdatedText.text = $"Last Updated: {now:HH:mm:ss}";
    }

    private void SetStatus(string s)
    {
        if (statusText) statusText.text = s;
    }
}