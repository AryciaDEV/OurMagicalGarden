using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class PlayerSaveWriter : MonoBehaviour
{
    private bool _saveQueued;
    private Coroutine _saveRoutine;
    private bool _isDirty;

    private float _lastSaveTime = -999f;
    [SerializeField] private float saveCooldown = 2f;

    [Header("Auto Save")]
    [SerializeField] private float autoSaveInterval = 5f; // Her 5 saniyede save

    private void Start()
    {
        StartCoroutine(BindWhenReady());
        Application.quitting += OnAppQuitting;
    }

    private void Update()
    {
        // Otomatik save
        if (Time.time - _lastSaveTime > autoSaveInterval && _isDirty)
        {
            Debug.Log("[PlayerSaveWriter] Auto-saving...");
            SaveCurrentState();
        }
    }

    private void OnDestroy()
    {
        Application.quitting -= OnAppQuitting;

        if (PlayerSeedBag.Local != null)
            PlayerSeedBag.Local.OnChanged -= QueueSave;

        if (PlayerInventory.Local != null)
            PlayerInventory.Local.OnChanged -= QueueSave;

        var farm = FindFirstObjectByType<FarmNetwork>();
        if (farm != null)
        {
            farm.OnPlotStateChanged -= OnPlotChanged;
        }
    }

    private void OnAppQuitting()
    {
        Debug.Log("[PlayerSaveWriter] OnAppQuitting - STARTING EMERGENCY SAVE");

        if (SaveManager.Instance != null)
        {
            // Hemen state'i topla
            if (!FarmAuth.TryGetLocalFarmIndex(out int myFarm))
            {
                Debug.LogError("[PlayerSaveWriter] Cannot save - no farm!");
                return;
            }

            var data = new PlayerSaveData
            {
                playerId = AuthSession.PlayFabId,
                username = AuthSession.Username,
                isGuest = AuthSession.IsGuest,
                coins = PlayerEconomy.Local != null ? PlayerEconomy.Local.Coins : 0
            };

            // HEMEN plot'lari topla - KRITIK: occupied kontrolu
            var farm = FindFirstObjectByType<FarmNetwork>();
            if (farm != null)
            {
                for (int x = 0; x < 3; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        var ps = farm.GetPlotState(myFarm, x, y);
                        if (ps == null) continue;

                        // KRITIK: Gecerli bir plot mu kontrol et
                        // Sadece seedId yoksa bos say; plantUnix eksikse su anki zamani kullan
                        if (!ps.occupied || string.IsNullOrWhiteSpace(ps.seedId))
                        {
                            // Bos plot olarak kaydet
                            data.plots.Add(new PlotSaveData
                            {
                                farmIndex = myFarm,
                                x = x,
                                y = y,
                                occupied = false,
                                seedId = "",
                                plantUnix = 0,
                                growSeconds = 0,
                                weight = 0,
                                version = ps.version
                            });
                            continue;
                        }

                        // Dolu plot - plantUnix eksikse su anki zamani kullan
                        data.plots.Add(new PlotSaveData
                        {
                            farmIndex = myFarm,
                            x = x,
                            y = y,
                            occupied = true,
                            seedId = ps.seedId,
                            plantUnix = ps.plantUnix > 0 ? ps.plantUnix : DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                            growSeconds = ps.growSeconds,
                            weight = ps.weight,
                            version = ps.version
                        });
                    }
                }
            }

            PopulatePets(data);

            Debug.Log($"[PlayerSaveWriter] Emergency save: {data.plots.Count} plots, {data.pets.Count} pets, coins={data.coins}");

            // Senkron save dene
            SaveManager.Instance.ForceSynchronousSave(data);
            SaveManager.Instance.SaveNow(data);
            SaveManager.Instance.FlushNow();

            // Kritik: 2 saniye bekle ki save tamamlansin
            Debug.Log("[PlayerSaveWriter] Waiting for save to complete...");
            float startTime = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - startTime < 2f)
            {
                // Busy wait - kotu ama gerekli
            }
            Debug.Log("[PlayerSaveWriter] OnAppQuitting - FINISHED");
        }
    }

    private IEnumerator BindWhenReady()
    {
        yield return new WaitUntil(() => PhotonNetwork.InRoom);
        yield return new WaitUntil(() => PlayerSeedBag.Local != null);
        yield return new WaitUntil(() => PlayerInventory.Local != null);

        var farm = FindFirstObjectByType<FarmNetwork>();

        if (PlayerSeedBag.Local != null)
            PlayerSeedBag.Local.OnChanged += QueueSave;

        if (PlayerInventory.Local != null)
            PlayerInventory.Local.OnChanged += QueueSave;

        if (farm != null)
        {
            farm.OnPlotStateChanged += OnPlotChanged;
            farm.OnFullStateRefreshed += () => { _isDirty = true; QueueSave(); };
        }

        Debug.Log("[PlayerSaveWriter] Bound to runtime events.");
    }

    private void OnPlotChanged(int farmIndex, int x, int y)
    {
        if (!FarmAuth.TryGetLocalFarmIndex(out int myFarm))
            return;

        if (farmIndex != myFarm)
            return;

        _isDirty = true;
        QueueSave();
    }

    public void QueueSave()
    {
        if (Time.time - _lastSaveTime < saveCooldown)
        {
            _saveQueued = true;
            return;
        }

        _saveQueued = true;

        if (_saveRoutine == null)
            _saveRoutine = StartCoroutine(CoSaveDebounced());
    }

    public void ForceSaveNow()
    {
        if (Time.time - _lastSaveTime < saveCooldown && _lastSaveTime > 0)
        {
            Debug.Log("[PlayerSaveWriter] Force save skipped - cooldown active");
            return;
        }

        SaveCurrentState();
    }

    private IEnumerator CoSaveDebounced()
    {
        yield return new WaitForSeconds(1.25f);

        if (_saveQueued && _isDirty)
        {
            _saveQueued = false;
            SaveCurrentState();
        }

        _saveRoutine = null;
    }

    public void SaveCurrentState()
    {
        if (!PhotonNetwork.InRoom) return;
        if (SaveManager.Instance == null) return;

        if (!FarmAuth.TryGetLocalFarmIndex(out int myFarm))
        {
            Debug.LogWarning("[PlayerSaveWriter] Cannot save - farm assignment not ready");
            return;
        }

        var data = new PlayerSaveData
        {
            playerId = AuthSession.PlayFabId,
            username = AuthSession.Username,
            isGuest = AuthSession.IsGuest,
            coins = PlayerEconomy.Local != null ? PlayerEconomy.Local.Coins : 0
        };

        if (PlayerSeedBag.Local != null)
        {
            foreach (var kv in PlayerSeedBag.Local.GetAll())
            {
                data.seeds.Add(new SeedStackData
                {
                    seedId = kv.Key,
                    count = kv.Value
                });
            }
        }

        var farm = FindFirstObjectByType<FarmNetwork>();
        if (farm != null)
        {
            int savedPlots = 0;
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    var ps = farm.GetPlotState(myFarm, x, y);

                    // KRITIK: Gecerli plot kontrolu
                    if (ps == null)
                    {
                        Debug.LogWarning($"[PlayerSaveWriter] Null plot state at ({x},{y})");
                        continue;
                    }

                    Debug.Log($"[PlayerSaveWriter] Checking plot ({x},{y}): occupied={ps.occupied}, seedId={ps.seedId}, plantUnix={ps.plantUnix}");

                    // KRITIK: Eger occupied=true ama seedId bossa, bu bir hata - yine de kaydet ama uyar
                    if (ps.occupied && string.IsNullOrWhiteSpace(ps.seedId))
                    {
                        Debug.LogError($"[PlayerSaveWriter] Plot ({x},{y}) is occupied but has no seedId!");
                    }

                    // KRITIK: Eger occupied=true ama plantUnix 0'sa, bu bir hata
                    if (ps.occupied && ps.plantUnix <= 0)
                    {
                        Debug.LogError($"[PlayerSaveWriter] Plot ({x},{y}) is occupied but has invalid plantUnix!");
                    }

                    // Her durumda kaydet - bos plotlar da dahil
                    // occupied degeri oldugu gibi saklanir; plantUnix eksikse su anki zaman kullanilir
                    data.plots.Add(new PlotSaveData
                    {
                        farmIndex = myFarm,
                        x = x,
                        y = y,
                        occupied = ps.occupied,
                        seedId = ps.seedId ?? "",
                        plantUnix = (ps.occupied && ps.plantUnix <= 0) ? DateTimeOffset.UtcNow.ToUnixTimeSeconds() : ps.plantUnix,
                        growSeconds = ps.growSeconds,
                        weight = ps.weight,
                        version = ps.version
                    });

                    if (ps.occupied && !string.IsNullOrWhiteSpace(ps.seedId))
                    {
                        savedPlots++;
                    }
                }
            }
            Debug.Log($"[PlayerSaveWriter] Saved {savedPlots} occupied plots for farm {myFarm}, total plots in save: {data.plots.Count}");
        }

        var inv = PlayerInventory.Local;
        if (inv != null)
        {
            foreach (var item in inv.GetAll())
            {
                data.items.Add(new ItemStackData
                {
                    uid = item.uid,
                    seedId = item.seedId,
                    weight = item.weight
                });
            }
        }

        PopulatePets(data);
        Debug.Log($"[PlayerSaveWriter] Saving {data.pets.Count} pets, equippedPetUid={data.equippedPetUid}");

        SaveManager.Instance.SaveNow(data);
        _isDirty = false;
        _lastSaveTime = Time.time;
        Debug.Log("[PlayerSaveWriter] SaveCurrentState queued.");
    }

    private static void PopulatePets(PlayerSaveData data)
    {
        var petInventory = PlayerPetInventory.Local;
        if (petInventory != null)
        {
            data.pets.Clear();
            foreach (var pet in petInventory.GetAll())
            {
                data.pets.Add(new PetSaveData
                {
                    uid = pet.uid,
                    petId = pet.petId,
                    eggId = pet.eggId
                });
            }
            data.equippedPetUid = petInventory.EquippedUid;
        }
        else if (SaveManager.Instance?.CurrentSave != null)
        {
            data.pets = SaveManager.Instance.CurrentSave.pets ?? new System.Collections.Generic.List<PetSaveData>();
            data.equippedPetUid = SaveManager.Instance.CurrentSave.equippedPetUid;
        }
    }

    private void OnApplicationQuit()
    {
        Debug.Log("[PlayerSaveWriter] OnApplicationQuit - forcing save");
        if (SaveManager.Instance != null)
        {
            SaveCurrentState();
            SaveManager.Instance.FlushNow();
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause && SaveManager.Instance != null)
        {
            Debug.Log("[PlayerSaveWriter] OnApplicationPause - forcing save");
            SaveCurrentState();
            SaveManager.Instance.FlushNow();
        }
    }
}