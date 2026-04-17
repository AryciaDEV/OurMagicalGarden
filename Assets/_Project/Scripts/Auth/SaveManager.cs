using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SAVE_KEY = "player_save";

    public PlayerSaveData CurrentSave { get; private set; }

    public event Action<PlayerSaveData> OnSaveLoaded;
    public event Action OnSaveWritten;

    private bool _isSaving;
    private bool _saveQueued;
    private PlayerSaveData _queuedSave;

    private float _lastSaveTime = -999f;
    [SerializeField] private float minSaveInterval = 2.5f;

    private void Awake()
    {
        Instance = this;
    }

    public void LoadSave()
    {
        Debug.Log("[SaveManager] Loading save from PlayFab...");

        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            if (result.Data != null && result.Data.TryGetValue(SAVE_KEY, out var record))
            {
                string json = record.Value;

                // KRITIK: JSON parse hatasi kontrolu
                try
                {
                    CurrentSave = JsonUtility.FromJson<PlayerSaveData>(json);
                    Debug.Log($"[SaveManager] Save loaded: {json.Length} chars, plots={CurrentSave?.plots?.Count ?? 0}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveManager] Failed to parse save JSON: {ex.Message}");
                    CurrentSave = CreateDefaultSave();
                }
            }

            if (CurrentSave == null)
            {
                CurrentSave = CreateDefaultSave();
                Debug.Log("[SaveManager] No save found, created default");
            }

            if (string.IsNullOrWhiteSpace(CurrentSave.username))
                CurrentSave.username = AuthSession.Username;

            CurrentSave.isGuest = AuthSession.IsGuest;
            CurrentSave.playerId = AuthSession.PlayFabId;

            // KRITIK: Plot validasyonu - gecersiz plot'lari temizle
            if (CurrentSave.plots != null)
            {
                int occupied = 0;
                int invalid = 0;

                for (int i = CurrentSave.plots.Count - 1; i >= 0; i--)
                {
                    var p = CurrentSave.plots[i];
                    if (p == null)
                    {
                        CurrentSave.plots.RemoveAt(i);
                        continue;
                    }

                    // Gecersiz plot kontrolu
                    if (p.occupied)
                    {
                        if (string.IsNullOrWhiteSpace(p.seedId) || p.plantUnix <= 0)
                        {
                            Debug.LogWarning($"[SaveManager] Invalid plot found: ({p.x},{p.y}) occupied=true but seedId='{p.seedId}', plantUnix={p.plantUnix}");
                            p.occupied = false; // Duzelt
                            invalid++;
                        }
                        else
                        {
                            occupied++;
                            Debug.Log($"[SaveManager] Valid plot: ({p.x},{p.y}) farm={p.farmIndex} seed={p.seedId}");
                        }
                    }
                }

                Debug.Log($"[SaveManager] Total occupied plots: {occupied}, invalid fixed: {invalid}");
            }
            else
            {
                CurrentSave.plots = new List<PlotSaveData>();
            }

            OnSaveLoaded?.Invoke(CurrentSave);
        },
        error =>
        {
            Debug.LogError("[SaveManager] Load failed: " + error.GenerateErrorReport());

            CurrentSave = CreateDefaultSave();
            CurrentSave.username = AuthSession.Username;
            CurrentSave.isGuest = AuthSession.IsGuest;
            CurrentSave.playerId = AuthSession.PlayFabId;

            OnSaveLoaded?.Invoke(CurrentSave);
        });
    }

    public void SaveNow(PlayerSaveData data)
    {
        if (data == null) return;

        // DEBUG: Ne kaydediliyor gorelim
        int occupiedPlots = 0;
        if (data.plots != null)
        {
            foreach (var p in data.plots)
            {
                if (p?.occupied == true) occupiedPlots++;
            }
        }
        Debug.Log($"[SaveManager] SaveNow called: coins={data.coins}, occupiedPlots={occupiedPlots}, totalPlots={data.plots?.Count ?? 0}");

        CurrentSave = data;
        _queuedSave = data;
        _saveQueued = true;
    }

    private void Update()
    {
        if (!_saveQueued) return;
        if (_isSaving) return;
        if (Time.unscaledTime - _lastSaveTime < minSaveInterval) return;
        if (_queuedSave == null) return;

        WriteQueuedSave();
    }

    private void WriteQueuedSave()
    {
        if (_queuedSave == null) return;

        _isSaving = true;
        _saveQueued = false;

        // KRITIK: Kaydetmeden once plot validasyonu
        if (_queuedSave.plots != null)
        {
            foreach (var p in _queuedSave.plots)
            {
                if (p != null && p.occupied)
                {
                    // Gecersiz veri varsa duzelt
                    if (string.IsNullOrWhiteSpace(p.seedId) || p.plantUnix <= 0)
                    {
                        Debug.LogWarning($"[SaveManager] Fixing invalid plot before save: ({p.x},{p.y})");
                        p.occupied = false;
                    }
                }
            }
        }

        string json = JsonUtility.ToJson(_queuedSave);
        Debug.Log($"[SaveManager] Writing save: {json.Length} chars");

        var req = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { SAVE_KEY, json }
            }
        };

        PlayFabClientAPI.UpdateUserData(req, result =>
        {
            _isSaving = false;
            _lastSaveTime = Time.unscaledTime;

            Debug.Log("[SaveManager] Save OK");
            OnSaveWritten?.Invoke();
        },
        error =>
        {
            _isSaving = false;
            _lastSaveTime = Time.unscaledTime;

            Debug.LogError("[SaveManager] Save failed: " + error.GenerateErrorReport());
            _saveQueued = true; // Tekrar dene
        });
    }

    public void FlushNow()
    {
        if (_queuedSave == null)
        {
            Debug.LogWarning("[SaveManager] FlushNow called but no save queued");
            return;
        }

        if (_isSaving)
        {
            Debug.Log("[SaveManager] FlushNow - save in progress, forcing immediate write");
            StopAllCoroutines();
            WriteQueuedSave();
            return;
        }

        Debug.Log("[SaveManager] FlushNow executing immediately");
        WriteQueuedSave();
    }

    // YENI: Senkron save (oyun kapanirken icin)
    public void ForceSynchronousSave(PlayerSaveData data)
    {
        if (data == null) return;

        Debug.Log("[SaveManager] ForceSynchronousSave - emergency save");
        CurrentSave = data;

        // Plot validasyonu
        if (data.plots != null)
        {
            foreach (var p in data.plots)
            {
                if (p != null && p.occupied && (string.IsNullOrWhiteSpace(p.seedId) || p.plantUnix <= 0))
                {
                    p.occupied = false;
                }
            }
        }

        string json = JsonUtility.ToJson(data);

        // PlayFab'a gonder ama bekleme
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { SAVE_KEY, json } }
        },
        result => Debug.Log("[SaveManager] Emergency save OK"),
        error => Debug.LogError("[SaveManager] Emergency save failed: " + error.GenerateErrorReport()));

        // Kritik: Biraz bekle ki API cagrisi gonderilsin
        System.Threading.Thread.Sleep(500);
    }

    private System.Collections.IEnumerator WaitAndFlush()
    {
        float timeout = Time.time + 5f;
        while (_isSaving && Time.time < timeout)
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (!_isSaving && _queuedSave != null)
        {
            WriteQueuedSave();
        }
        else
        {
            Debug.LogError("[SaveManager] Flush timeout or no save to flush");
        }
    }

    private PlayerSaveData CreateDefaultSave()
    {
        return new PlayerSaveData
        {
            playerId = AuthSession.PlayFabId,
            username = AuthSession.Username,
            isGuest = AuthSession.IsGuest,
            coins = 100,
            plots = new List<PlotSaveData>() // Bos liste ile baslat
        };
    }
}