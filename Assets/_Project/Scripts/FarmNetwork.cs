using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[Serializable]
public class PlotState
{
    public int ownerActor;
    public bool occupied;
    public string seedId;
    public long plantUnix;
    public float growSeconds;
    public float weight;
    public int version;
}

[Serializable]
public class PlotStateSaveView
{
    public int farmIndex;
    public int x;
    public int y;
    public int ownerActor;
    public bool occupied;
    public string seedId;
    public long plantUnix;
    public float growSeconds;
    public float weight;
    public int version;
}

[Serializable]
public class PlotRestoreRequest
{
    public int previousSavedFarmIndex = -1;
    public int targetFarmIndex = -1;
    public List<PlotSaveData> plots = new List<PlotSaveData>();
    public string playFabId;
}

public class FarmNetwork : MonoBehaviourPunCallbacks
{
    private const int FARMS = 8;
    private const int W = 3;
    private const int H = 3;

    [SerializeField] private float defaultGrowSeconds = 30f;
    [SerializeField] private int plantSeedCost = 1;
    [SerializeField] private int fallbackHarvestCoin = 5;
    [SerializeField] private List<SeedDefinition> seedDefs;
    [SerializeField] private float interactCooldown = 0.25f;

    [Header("Anti Spam")]
    [SerializeField] private float requestLockSeconds = 0.6f;
    [SerializeField] private float harvestReplantLockSeconds = 0.9f;

    public AudioClip myAudioClip;

    private PlotState[,,] grid = new PlotState[FARMS, W, H];
    private readonly Dictionary<(int f, int x, int y), Renderer> _plotRenderers = new Dictionary<(int f, int x, int y), Renderer>();
    private readonly Dictionary<(int f, int x, int y), float> _lastInteractTime = new Dictionary<(int f, int x, int y), float>();
    private readonly Dictionary<(int f, int x, int y), float> _localPendingUntil = new Dictionary<(int f, int x, int y), float>();
    private readonly Dictionary<(int f, int x, int y), float> _masterPendingUntil = new Dictionary<(int f, int x, int y), float>();

    private readonly Dictionary<int, int> _actorFarmAssignment = new Dictionary<int, int>();
    private readonly Dictionary<string, int> _playFabIdToFarm = new Dictionary<string, int>();

    private Dictionary<string, SeedDefinition> _seedById;
    private int _globalVersionCounter = 0;

    public event Action<int, int, int> OnPlotStateChanged;
    public event Action OnFullStateRefreshed;

    // YENI: Restore tamamlandi mi?
    private bool _localRestoreCompleted = false;
    public bool IsLocalRestoreCompleted => _localRestoreCompleted;

    private void Awake()
    {
        for (int f = 0; f < FARMS; f++)
        {
            for (int x = 0; x < W; x++)
            {
                for (int y = 0; y < H; y++)
                {
                    grid[f, x, y] = new PlotState();
                }
            }
        }

        _seedById = new Dictionary<string, SeedDefinition>(StringComparer.OrdinalIgnoreCase);
        if (seedDefs != null)
        {
            foreach (var d in seedDefs)
            {
                if (d == null || string.IsNullOrWhiteSpace(d.seedId))
                    continue;

                _seedById[d.seedId.Trim()] = d;
            }
        }

        Debug.Log($"[FarmNetwork] SeedDefs count={seedDefs?.Count ?? 0} dictCount={_seedById?.Count ?? 0}");
        if (_seedById != null)
            Debug.Log("[FarmNetwork] Keys sample: " + string.Join(", ", _seedById.Keys));

        CachePlotRenderers();
    }

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
            CleanupStalePlots();

        if (PhotonNetwork.IsMasterClient)
            InvokeRepeating(nameof(PeriodicFullSync), 3f, 5f);
    }

    private void OnDestroy()
    {
        CancelInvoke();
    }

    private void PeriodicFullSync()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (PhotonNetwork.CurrentRoom == null) return;

        RebroadcastFullState();
    }

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient)
            CleanupStalePlots();

        for (int f = 0; f < FARMS; f++)
        {
            for (int x = 0; x < W; x++)
            {
                for (int y = 0; y < H; y++)
                {
                    if (grid[f, x, y].occupied)
                        UpdatePlotVisual(f, x, y);
                }
            }
        }
    }

    public PlotState GetPlotState(int farmIndex, int x, int y)
    {
        if (!InBounds(farmIndex, x, y)) return null;
        var ps = grid[farmIndex, x, y];
        return ps;
    }

    public bool TryGetRemainingSeconds(int f, int x, int y, out int remainingSec, out bool ready)
    {
        remainingSec = 0;
        ready = false;

        if (!InBounds(f, x, y)) return false;

        var ps = grid[f, x, y];
        if (!ps.occupied) return false;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long end = ps.plantUnix + (long)Mathf.Ceil(ps.growSeconds);
        long rem = end - now;

        if (rem <= 0)
        {
            remainingSec = 0;
            ready = true;
        }
        else
        {
            remainingSec = (int)rem;
            ready = false;
        }

        return true;
    }

    private void CachePlotRenderers()
    {
        _plotRenderers.Clear();

        var farmsRoot = GameObject.Find("FarmsRoot");
        if (!farmsRoot)
        {
            Debug.LogWarning("[FarmNetwork] FarmsRoot not found. Plot visuals may not update.");
            return;
        }

        for (int f = 0; f < FARMS; f++)
        {
            var farm = farmsRoot.transform.Find($"Farm_{f}");
            if (!farm) continue;

            var plots = farm.Find("Plots");
            if (!plots) continue;

            for (int x = 0; x < W; x++)
            {
                for (int y = 0; y < H; y++)
                {
                    var p = plots.Find($"Plot_{x}_{y}");
                    if (!p) continue;

                    var r = p.GetComponent<Renderer>();
                    if (!r) continue;

                    _plotRenderers[(f, x, y)] = r;
                }
            }
        }
    }

    private bool TryConsumeSeedLocal(string seedId, int amount)
    {
        if (PlayerSeedBag.Local == null) return true;
        return PlayerSeedBag.Local.TrySpendSeed(seedId, amount);
    }

    public void InteractPlot(int farmIndex, int x, int y, string seedIdIfPlant = "Carrot")
    {
        Debug.Log($"[FarmNetwork] InteractPlot CALLED: farm={farmIndex}, plot=({x},{y}), seed={seedIdIfPlant}");

        var key = (farmIndex, x, y);
        float nowT = Time.time;

        if (_lastInteractTime.TryGetValue(key, out float last) && nowT - last < interactCooldown)
        {
            Debug.Log("[FarmNetwork] Cooldown active!");
            return;
        }

        if (_localPendingUntil.TryGetValue(key, out float pendingUntil) && nowT < pendingUntil)
        {
            Debug.Log("[FarmNetwork] Pending lock active!");
            return;
        }

        _lastInteractTime[key] = nowT;
        _localPendingUntil[key] = nowT + requestLockSeconds;

        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("[FarmNetwork] Not in room!");
            return;
        }

        if (!InBounds(farmIndex, x, y))
        {
            Debug.LogError($"[FarmNetwork] Out of bounds: ({farmIndex},{x},{y})");
            _localPendingUntil.Remove(key);
            return;
        }

        if (!FarmAuth.CanEditFarm(farmIndex))
        {
            Debug.LogError($"[FarmNetwork] AUTH FAILED: Cannot edit farm {farmIndex}");
            _localPendingUntil.Remove(key);
            return;
        }

        bool wantsPlant = !grid[farmIndex, x, y].occupied;
        Debug.Log($"[FarmNetwork] WantsPlant={wantsPlant}, Occupied={grid[farmIndex, x, y].occupied}");

        if (wantsPlant)
        {
            if (!TryConsumeSeedLocal(seedIdIfPlant, plantSeedCost))
            {
                Debug.Log("[FarmNetwork] No seeds!");
                _localPendingUntil.Remove(key);
                return;
            }
        }

        Debug.Log("[FarmNetwork] Sending RPC to MasterClient...");
        photonView.RPC(nameof(RPC_InteractPlot), RpcTarget.MasterClient, farmIndex, x, y, seedIdIfPlant, _globalVersionCounter);
    }

    private bool IsReadyToHarvest(int f, int x, int y)
    {
        var ps = grid[f, x, y];
        if (!ps.occupied) return false;
        if (ps.plantUnix <= 0) return false;
        if (ps.growSeconds <= 0.01f) return false;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return now >= ps.plantUnix + (long)Mathf.Ceil(ps.growSeconds);
    }

    public int GetHarvestCoins(string seedId, float weight)
    {
        string key = string.IsNullOrWhiteSpace(seedId) ? "" : seedId.Trim();

        if (_seedById != null && _seedById.TryGetValue(key, out var def) && def != null)
        {
            int basePrice = def.sellPrice;

            float w01 = 0.5f;
            if (def.maxWeight > def.minWeight)
                w01 = Mathf.InverseLerp(def.minWeight, def.maxWeight, weight);

            float bonusMul = Mathf.Lerp(1f, 1.3f, w01);
            int final = Mathf.RoundToInt(basePrice * bonusMul);

            return Mathf.Max(0, final);
        }

        return fallbackHarvestCoin;
    }

    public void RequestHalveRemainingGrowTime(int farmIndex, int x, int y)
    {
        if (!PhotonNetwork.InRoom) return;
        if (!InBounds(farmIndex, x, y)) return;

        photonView.RPC(nameof(RPC_RequestHalveRemainingGrowTime), RpcTarget.MasterClient, farmIndex, x, y);
    }

    [PunRPC]
    private void RPC_RequestHalveRemainingGrowTime(int farmIndex, int x, int y, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!InBounds(farmIndex, x, y)) return;

        var ps = grid[farmIndex, x, y];
        if (!ps.occupied) return;
        if (ps.plantUnix <= 0 || ps.growSeconds <= 0f) return;

        if (ps.ownerActor != info.Sender.ActorNumber)
        {
            Debug.LogWarning($"[FarmNetwork] Speed-up denied. Sender={info.Sender.ActorNumber}, owner={ps.ownerActor}");
            return;
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        float elapsed = Mathf.Max(0f, (float)(now - ps.plantUnix));
        float remaining = Mathf.Max(0f, ps.growSeconds - elapsed);

        if (remaining <= 0f) return;

        float newRemaining = remaining * 0.5f;
        float newGrowSeconds = elapsed + newRemaining;

        ps.growSeconds = newGrowSeconds;
        ps.version = ++_globalVersionCounter;

        BroadcastPlotToAll(farmIndex, x, y, ps);

        Debug.Log($"[FarmNetwork] Ad speed-up applied plot=({farmIndex},{x},{y}) remaining {remaining} -> {newRemaining}");
    }

    [PunRPC]
    private void RPC_InteractPlot(int farmIndex, int x, int y, string seedIdIfPlant, int clientVersion, PhotonMessageInfo info)
    {
        Debug.Log($"[FarmNetwork] RPC_InteractPlot RECEIVED: farm={farmIndex}, plot=({x},{y}), sender={info.Sender.ActorNumber}");

        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[FarmNetwork] Not MasterClient, ignoring!");
            return;
        }

        if (!InBounds(farmIndex, x, y))
        {
            Debug.LogError("[FarmNetwork] RPC: Out of bounds!");
            return;
        }

        var key = (farmIndex, x, y);
        float nowT = Time.time;

        if (_masterPendingUntil.TryGetValue(key, out float pendingUntil) && nowT < pendingUntil)
        {
            Debug.Log("[FarmNetwork] RPC: Master cooldown active!");
            photonView.RPC(nameof(RPC_InteractRejected), info.Sender, farmIndex, x, y, "Cooldown");
            return;
        }

        _masterPendingUntil[key] = nowT + requestLockSeconds;

        var ps = grid[farmIndex, x, y];

        if (ps.version > clientVersion + 10)
        {
            Debug.LogWarning($"[FarmNetwork] Version conflict detected. Server:{ps.version}, Client:{clientVersion}");
        }

        if (ps.occupied && !IsActorStillInRoom(ps.ownerActor))
        {
            Debug.Log($"[FarmNetwork] Clearing stale plot of disconnected player {ps.ownerActor}");
            ClearPlotLocal(farmIndex, x, y);
            BroadcastClearPlot(farmIndex, x, y);
            ps = grid[farmIndex, x, y];
        }

        int senderActor = info.Sender.ActorNumber;
        int senderFarm = GetFarmForActor(senderActor);

        Debug.Log($"[FarmNetwork] RPC: Sender={senderActor}, SenderFarm={senderFarm}, TargetFarm={farmIndex}");

        if (senderFarm != farmIndex)
        {
            Debug.LogError($"[FarmNetwork] RPC: FARM MISMATCH! Sender's farm={senderFarm}, target={farmIndex}");
            photonView.RPC(nameof(RPC_InteractRejected), info.Sender, farmIndex, x, y, "WrongFarm");
            return;
        }

        if (!ps.occupied)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            float growSeconds = defaultGrowSeconds;
            float weight = 1f;

            string normalizedSeedId = string.IsNullOrWhiteSpace(seedIdIfPlant) ? "Carrot" : seedIdIfPlant.Trim();

            if (_seedById != null && _seedById.TryGetValue(normalizedSeedId, out var def) && def != null)
            {
                growSeconds = def.growSeconds;

                var petSvc = PetNetworkService.Instance;
                if (petSvc != null)
                {
                    int actor = info.Sender.ActorNumber;
                    var (growReducePct, _, _) = petSvc.Master_GetActorEquippedBonuses(actor);
                    float mul = Mathf.Clamp01(1f - (growReducePct / 100f));
                    growSeconds *= mul;
                }

                float minW = def.minWeight;
                float maxW = def.maxWeight;
                if (maxW < minW) (minW, maxW) = (maxW, minW);

                weight = UnityEngine.Random.Range(minW, maxW);
            }

            ps.ownerActor = info.Sender.ActorNumber;
            ps.occupied = true;
            ps.seedId = normalizedSeedId;
            ps.plantUnix = now;
            ps.growSeconds = growSeconds;
            ps.weight = weight;
            ps.version = ++_globalVersionCounter;

            Debug.Log($"[FarmNetwork] Plant '{normalizedSeedId}' owner={ps.ownerActor} farm={farmIndex} growSeconds={growSeconds} version={ps.version}");

            BroadcastPlotToAll(farmIndex, x, y, ps);

            if (SoundFXManager.Instance != null && myAudioClip != null)
                SoundFXManager.Instance.PlaySound(myAudioClip);

            return;
        }

        if (ps.ownerActor != info.Sender.ActorNumber)
        {
            Debug.LogWarning($"[FarmNetwork] Harvest denied. Sender={info.Sender.ActorNumber}, owner={ps.ownerActor}");
            photonView.RPC(nameof(RPC_InteractRejected), info.Sender, farmIndex, x, y, "NotOwner");
            return;
        }

        if (IsReadyToHarvest(farmIndex, x, y))
        {
            var inv = InventoryNetworkService.Instance != null
                ? InventoryNetworkService.Instance
                : FindFirstObjectByType<InventoryNetworkService>();

            if (inv != null)
                inv.Master_AddItem(info.Sender, ps.seedId, ps.weight);
            else
                Debug.LogError("[FarmNetwork] InventoryNetworkService not found in scene!");

            ClearPlotLocal(farmIndex, x, y);
            BroadcastClearPlot(farmIndex, x, y);

            _masterPendingUntil[key] = Time.time + harvestReplantLockSeconds;

            if (SoundFXManager.Instance != null && myAudioClip != null)
                SoundFXManager.Instance.PlaySound(myAudioClip);

            return;
        }
        else
        {
            photonView.RPC(nameof(RPC_InteractRejected), info.Sender, farmIndex, x, y, "NotReady");
        }
    }

    [PunRPC]
    private void RPC_InteractRejected(int farmIndex, int x, int y, string reason)
    {
        Debug.Log($"[FarmNetwork] Interact rejected: {reason} at ({farmIndex},{x},{y})");
        _localPendingUntil.Remove((farmIndex, x, y));

        UpdatePlotVisual(farmIndex, x, y);
    }

    // DUZELTILMIS: Restore metodu - guclendirilmis versiyon
    public void RequestRestoreFromSaveForLocal(List<PlotSaveData> plots, int targetFarmIndex, int previousSavedFarmIndex, string playFabId)
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("[FarmNetwork] RequestRestoreFromSaveForLocal: Not in room!");
            return;
        }
        if (PhotonNetwork.LocalPlayer == null)
        {
            Debug.LogError("[FarmNetwork] RequestRestoreFromSaveForLocal: LocalPlayer null!");
            return;
        }

        // KRITIK: Plot listesi bossa bile gonder (temizlik icin)
        var wrapper = new PlotRestoreRequest
        {
            previousSavedFarmIndex = previousSavedFarmIndex,
            targetFarmIndex = targetFarmIndex,
            plots = plots ?? new List<PlotSaveData>(),
            playFabId = playFabId ?? AuthSession.PlayFabId
        };

        string json = JsonUtility.ToJson(wrapper);
        Debug.Log($"[FarmNetwork] Sending RPC_RestorePlotsFromSave, json length={json.Length}, plots={wrapper.plots.Count}");

        // Log each plot for debugging
        foreach (var p in wrapper.plots)
        {
            if (p != null && p.occupied)
            {
                Debug.Log($"[FarmNetwork] Sending plot to restore: ({p.x},{p.y}) seed={p.seedId}, plantUnix={p.plantUnix}");
            }
        }

        photonView.RPC(
            nameof(RPC_RestorePlotsFromSave),
            RpcTarget.MasterClient,
            PhotonNetwork.LocalPlayer.ActorNumber,
            json
        );
    }

    [PunRPC]
    private void RPC_RestorePlotsFromSave(int ownerActor, string json, PhotonMessageInfo info)
    {
        Debug.Log($"[FarmNetwork] RPC_RestorePlotsFromSave START: ownerActor={ownerActor}");

        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[FarmNetwork] Not MasterClient, ignoring restore");
            return;
        }

        var wrapper = JsonUtility.FromJson<PlotRestoreRequest>(json);
        if (wrapper == null)
        {
            Debug.LogError("[FarmNetwork] Failed to parse restore request");
            return;
        }

        int targetFarmIndex = wrapper.targetFarmIndex >= 0 ? wrapper.targetFarmIndex : -1;
        int previousSavedFarmIndex = wrapper.previousSavedFarmIndex;
        List<PlotSaveData> plots = wrapper.plots ?? new List<PlotSaveData>();
        string playFabId = wrapper.playFabId;

        Debug.Log($"[FarmNetwork] Restore request: actor={ownerActor}, targetFarm={targetFarmIndex}, plots={plots.Count}, playFabId={playFabId}");

        if (targetFarmIndex < 0 || targetFarmIndex >= FARMS)
        {
            if (info.Sender.CustomProperties.ContainsKey(FarmAssignmentService.PROP_FARM))
            {
                targetFarmIndex = (int)info.Sender.CustomProperties[FarmAssignmentService.PROP_FARM];
                Debug.Log($"[FarmNetwork] Using sender's farm assignment: {targetFarmIndex}");
            }
            else
            {
                Debug.LogError($"[FarmNetwork] Invalid target farm index: {targetFarmIndex}");
                return;
            }
        }

        // PlayFabId -> Farm eslestirmesini kaydet
        if (!string.IsNullOrEmpty(playFabId))
        {
            _playFabIdToFarm[playFabId] = targetFarmIndex;
            Debug.Log($"[FarmNetwork] Saved mapping: {playFabId} -> Farm {targetFarmIndex}");
        }

        _actorFarmAssignment[ownerActor] = targetFarmIndex;

        // Onceki farm'deki plot'lari temizle
        if (previousSavedFarmIndex >= 0 && previousSavedFarmIndex != targetFarmIndex && previousSavedFarmIndex < FARMS)
        {
            Debug.Log($"[FarmNetwork] Clearing previous farm {previousSavedFarmIndex} for actor {ownerActor}");
            ClearPlotsOwnedByActor(ownerActor, previousSavedFarmIndex);
        }

        // Hedef farm'deki baska oyunculara ait plot'lari temizle
        for (int x = 0; x < W; x++)
        {
            for (int y = 0; y < H; y++)
            {
                var existing = grid[targetFarmIndex, x, y];
                if (existing.occupied && existing.ownerActor != ownerActor && existing.ownerActor != 0)
                {
                    Debug.LogWarning($"[FarmNetwork] Clearing plot ({targetFarmIndex},{x},{y}) occupied by {existing.ownerActor} for {ownerActor}");
                    ClearPlotLocal(targetFarmIndex, x, y);
                    BroadcastClearPlot(targetFarmIndex, x, y);
                }
            }
        }

        // Plot'lari restore et - DUZELTILMIS
        int restoredCount = 0;
        foreach (PlotSaveData p in plots)
        {
            if (p == null) continue;
            if (p.x < 0 || p.x >= W || p.y < 0 || p.y >= H) continue;

            var ps = grid[targetFarmIndex, p.x, p.y];

            Debug.Log($"[FarmNetwork] Processing plot ({p.x},{p.y}): save.occupied={p.occupied}, save.seed={p.seedId}, save.plantUnix={p.plantUnix}, current.occupied={ps.occupied}");

            // Eger plot su an baska birine aitse atla
            if (ps.occupied && ps.ownerActor != ownerActor)
            {
                Debug.LogWarning($"[FarmNetwork] Plot ({targetFarmIndex},{p.x},{p.y}) currently owned by {ps.ownerActor}, skipping");
                continue;
            }

            // Save'de occupied=false ise temizle
            if (!p.occupied)
            {
                if (ps.occupied)
                {
                    ClearPlotLocal(targetFarmIndex, p.x, p.y);
                    BroadcastClearPlot(targetFarmIndex, p.x, p.y);
                }
                continue;
            }

            // KRITIK: Gecerli bir seedId var mi kontrol et
            if (string.IsNullOrWhiteSpace(p.seedId))
            {
                Debug.LogWarning($"[FarmNetwork] Plot ({p.x},{p.y}) has occupied=true but empty seedId, skipping!");
                continue;
            }

            // KRITIK: Gecerli bir plantUnix var mi?
            if (p.plantUnix <= 0)
            {
                Debug.LogWarning($"[FarmNetwork] Plot ({p.x},{p.y}) has invalid plantUnix={p.plantUnix}, using current time");
                // Devam et, su anki zamani kullanacagiz
            }

            // DUZELTILMIS: Plot'u doldur - tum alanlari eksiksiz
            ps.ownerActor = ownerActor;
            ps.occupied = true; // KESINLIKLE true
            ps.seedId = p.seedId.Trim();
            ps.plantUnix = p.plantUnix > 0 ? p.plantUnix : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            ps.growSeconds = p.growSeconds > 0 ? p.growSeconds : defaultGrowSeconds;
            ps.weight = p.weight > 0 ? p.weight : 1f;
            ps.version = ++_globalVersionCounter;

            Debug.Log($"[FarmNetwork] RESTORED plot ({targetFarmIndex},{p.x},{p.y}): seed={ps.seedId}, plantUnix={ps.plantUnix}, occupied={ps.occupied}, growSeconds={ps.growSeconds}");

            BroadcastPlotToAll(targetFarmIndex, p.x, p.y, ps);
            restoredCount++;
        }

        Debug.Log($"[FarmNetwork] Restore complete: {restoredCount} plots restored for farm {targetFarmIndex}");

        // Sonuclari gonder
        photonView.RPC(nameof(RPC_RestoreComplete), info.Sender, targetFarmIndex, restoredCount);
        photonView.RPC(nameof(RPC_NotifyFarmRestored), RpcTarget.Others, targetFarmIndex, ownerActor);
    }

    [PunRPC]
    private void RPC_RestoreComplete(int farmIndex, int count)
    {
        Debug.Log($"[FarmNetwork] RPC_RestoreComplete: farm={farmIndex}, count={count}");
        _localRestoreCompleted = true;

        // Tum plot'lari kontrol et ve gorselleri guncelle
        int actualPlots = 0;
        for (int x = 0; x < W; x++)
        {
            for (int y = 0; y < H; y++)
            {
                var ps = grid[farmIndex, x, y];
                if (ps != null)
                {
                    UpdatePlotVisual(farmIndex, x, y);
                    OnPlotStateChanged?.Invoke(farmIndex, x, y);

                    if (ps.occupied)
                    {
                        actualPlots++;
                        Debug.Log($"[FarmNetwork] Restored plot verified: ({x},{y}) seed={ps.seedId}");
                    }
                }
            }
        }

        Debug.Log($"[FarmNetwork] Restore verification: RPC said {count}, actual {actualPlots} occupied plots");

        // Save ile karsilastir
        var save = SaveManager.Instance?.CurrentSave;
        int expectedPlots = 0;
        if (save?.plots != null)
        {
            foreach (var p in save.plots)
            {
                if (p?.occupied == true && p.farmIndex == farmIndex)
                    expectedPlots++;
            }
        }

        if (expectedPlots != actualPlots)
        {
            Debug.LogWarning($"[FarmNetwork] MISMATCH! Expected {expectedPlots}, found {actualPlots}");
        }

        OnFullStateRefreshed?.Invoke();
    }

    [PunRPC]
    private void RPC_NotifyFarmRestored(int farmIndex, int ownerActor)
    {
        Debug.Log($"[FarmNetwork] Farm {farmIndex} restored notification for actor {ownerActor}");

        for (int x = 0; x < W; x++)
        {
            for (int y = 0; y < H; y++)
            {
                UpdatePlotVisual(farmIndex, x, y);
            }
        }
    }

    [PunRPC]
    private void RPC_GiveCoins(int amount)
    {
        if (PlayerEconomy.Local != null)
            PlayerEconomy.Local.AddCoins(amount);
    }

    // DUZELTILMIS: RPC_ApplyPlot - versiyon kontrolu esnetildi
    [PunRPC]
    private void RPC_ApplyPlot(int farmIndex, int x, int y, int ownerActor, bool occupied, string seedId, long plantUnix, float growSeconds, float weight, int version)
    {
        if (!InBounds(farmIndex, x, y)) return;

        var ps = grid[farmIndex, x, y];

        // DUZELTILMIS: Versiyon kontrolunu esnet - restore sirasinda eski versiyonlar gelebilir
        // Sadece cok yeni bir versiyon varsa ignore et (2 versiyon fark)
        if (version > 0 && ps.version > version + 2)
        {
            Debug.Log($"[FarmNetwork] Ignoring stale update v{version} < current v{ps.version} (diff > 2)");
            return;
        }

        // Eger su anki plot bize aitse ve yeni gelen baskasina aitse, ignore et (race condition)
        if (ps.occupied && ps.ownerActor == PhotonNetwork.LocalPlayer.ActorNumber && ownerActor != ps.ownerActor)
        {
            Debug.LogWarning($"[FarmNetwork] Ignoring plot update that would steal our plot!");
            return;
        }

        ps.ownerActor = ownerActor;
        ps.occupied = occupied;
        ps.seedId = seedId ?? "";
        ps.plantUnix = plantUnix;
        ps.growSeconds = growSeconds;
        ps.weight = weight;

        // Versiyonu sadece gercekten yeni ise guncelle
        if (version > ps.version)
        {
            ps.version = version;
        }

        _localPendingUntil.Remove((farmIndex, x, y));

        UpdatePlotVisual(farmIndex, x, y);
        OnPlotStateChanged?.Invoke(farmIndex, x, y);

        if (occupied)
        {
            Debug.Log($"[FarmNetwork] Applied plot update: ({farmIndex},{x},{y}) seed={seedId}, version={version}");
        }
    }

    private void UpdatePlotVisual(int farmIndex, int x, int y)
    {
        if (!_plotRenderers.TryGetValue((farmIndex, x, y), out var r) || r == null)
            return;

        var ps = grid[farmIndex, x, y];

        if (!ps.occupied)
        {
            r.material.color = Color.gray;
            return;
        }

        r.material.color = IsReadyToHarvest(farmIndex, x, y) ? Color.yellow : Color.green;
    }

    public void ApplyPlotStateFromSave(int farmIndex, int x, int y, bool occupied, string seedId, long plantUnix, float growSeconds, float weight, int ownerActor = 0, int version = 0)
    {
        if (!InBounds(farmIndex, x, y)) return;

        var ps = grid[farmIndex, x, y];
        ps.ownerActor = ownerActor;
        ps.occupied = occupied;
        ps.seedId = seedId ?? "";
        ps.plantUnix = plantUnix;
        ps.growSeconds = growSeconds;
        ps.weight = weight;
        ps.version = version;

        UpdatePlotVisual(farmIndex, x, y);
        OnPlotStateChanged?.Invoke(farmIndex, x, y);
    }

    public List<PlotStateSaveView> GetAllPlotStatesForSave()
    {
        var list = new List<PlotStateSaveView>();

        for (int f = 0; f < FARMS; f++)
        {
            for (int x = 0; x < W; x++)
            {
                for (int y = 0; y < H; y++)
                {
                    var ps = grid[f, x, y];
                    list.Add(new PlotStateSaveView
                    {
                        farmIndex = f,
                        x = x,
                        y = y,
                        ownerActor = ps.ownerActor,
                        occupied = ps.occupied,
                        seedId = ps.seedId,
                        plantUnix = ps.plantUnix,
                        growSeconds = ps.growSeconds,
                        weight = ps.weight,
                        version = ps.version
                    });
                }
            }
        }

        return list;
    }

    public void ClearFarm(int farmIndex)
    {
        if (farmIndex < 0 || farmIndex >= FARMS) return;

        for (int x = 0; x < W; x++)
        {
            for (int y = 0; y < H; y++)
            {
                ClearPlotLocal(farmIndex, x, y);
                BroadcastClearPlot(farmIndex, x, y);
            }
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log($"[FarmNetwork] Player left: actor={otherPlayer.ActorNumber}. Cleaning owned plots...");

        // YENI: Oyuncunun plot'larini hemen kaydet (eger local oyuncu ise)
        if (otherPlayer == PhotonNetwork.LocalPlayer)
        {
            var saveWriter = FindFirstObjectByType<PlayerSaveWriter>();
            if (saveWriter != null)
            {
                Debug.Log("[FarmNetwork] Local player leaving - triggering emergency save");
                saveWriter.ForceSaveNow();
            }
        }

        ClearPlotsOwnedByActor(otherPlayer.ActorNumber);
        _actorFarmAssignment.Remove(otherPlayer.ActorNumber);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log($"[FarmNetwork] Player entered: actor={newPlayer.ActorNumber}. Sending snapshot...");
        StartCoroutine(SendSnapshotWhenReady(newPlayer));
    }

    private IEnumerator SendSnapshotWhenReady(Player target)
    {
        float timeout = Time.time + 5f;

        while (Time.time < timeout)
        {
            if (target?.CustomProperties?.ContainsKey(FarmAssignmentService.PROP_FARM) == true)
            {
                SendFullSnapshotTo(target);
                yield break;
            }
            yield return new WaitForSeconds(0.2f);
        }

        Debug.LogWarning($"[FarmNetwork] Timeout waiting for farm assignment of {target?.ActorNumber}");
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log("[FarmNetwork] New Master: " + newMasterClient.ActorNumber);

        if (!PhotonNetwork.IsMasterClient) return;

        StartCoroutine(DelayedRebroadcast());
    }

    private IEnumerator DelayedRebroadcast()
    {
        yield return new WaitForSeconds(1f);
        CleanupStalePlots();
        RebroadcastFullState();
    }

    private void ClearPlotsOwnedByActor(int actorNumber, int specificFarm = -1)
    {
        for (int f = 0; f < FARMS; f++)
        {
            if (specificFarm >= 0 && f != specificFarm) continue;

            for (int x = 0; x < W; x++)
            {
                for (int y = 0; y < H; y++)
                {
                    var ps = grid[f, x, y];

                    if (ps.occupied && ps.ownerActor == actorNumber)
                    {
                        Debug.Log($"[FarmNetwork] Clearing plot of actor={actorNumber} at ({f},{x},{y})");
                        ClearPlotLocal(f, x, y);
                        BroadcastClearPlot(f, x, y);
                    }
                }
            }
        }
    }

    private void CleanupStalePlots()
    {
        for (int f = 0; f < FARMS; f++)
        {
            for (int x = 0; x < W; x++)
            {
                for (int y = 0; y < H; y++)
                {
                    var ps = grid[f, x, y];
                    if (!ps.occupied) continue;

                    if (!IsActorStillInRoom(ps.ownerActor))
                    {
                        Debug.LogWarning($"[FarmNetwork] Cleanup stale plot owner={ps.ownerActor} at ({f},{x},{y})");
                        ClearPlotLocal(f, x, y);
                        BroadcastClearPlot(f, x, y);
                    }
                }
            }
        }
    }

    private void RebroadcastFullState()
    {
        Debug.Log("[FarmNetwork] Rebroadcasting full state to all clients");
        for (int f = 0; f < FARMS; f++)
        {
            for (int x = 0; x < W; x++)
            {
                for (int y = 0; y < H; y++)
                {
                    var ps = grid[f, x, y];
                    BroadcastPlotToAll(f, x, y, ps);
                }
            }
        }
    }

    private void SendFullSnapshotTo(Player target)
    {
        if (target == null) return;

        if (!target.CustomProperties.ContainsKey(FarmAssignmentService.PROP_FARM))
        {
            Debug.LogWarning($"[FarmNetwork] Target {target.ActorNumber} has no farm assignment yet!");
        }

        int targetFarm = -1;
        if (target.CustomProperties.ContainsKey(FarmAssignmentService.PROP_FARM))
            targetFarm = (int)target.CustomProperties[FarmAssignmentService.PROP_FARM];

        Debug.Log($"[FarmNetwork] Sending snapshot to actor {target.ActorNumber} (farm: {targetFarm})");

        for (int f = 0; f < FARMS; f++)
        {
            for (int x = 0; x < W; x++)
            {
                for (int y = 0; y < H; y++)
                {
                    var ps = grid[f, x, y];
                    photonView.RPC(
                        nameof(RPC_ApplyPlot),
                        target,
                        f, x, y,
                        ps.ownerActor,
                        ps.occupied,
                        ps.seedId ?? "",
                        ps.plantUnix,
                        ps.growSeconds,
                        ps.weight,
                        ps.version
                    );
                }
            }
        }

        Debug.Log($"[FarmNetwork] Snapshot sent to actor {target.ActorNumber}");
    }

    private void BroadcastPlotToAll(int farmIndex, int x, int y, PlotState ps)
    {
        photonView.RPC(
            nameof(RPC_ApplyPlot),
            RpcTarget.All,
            farmIndex, x, y,
            ps.ownerActor,
            ps.occupied,
            ps.seedId ?? "",
            ps.plantUnix,
            ps.growSeconds,
            ps.weight,
            ps.version
        );
    }

    private void BroadcastClearPlot(int farmIndex, int x, int y)
    {
        photonView.RPC(
            nameof(RPC_ApplyPlot),
            RpcTarget.All,
            farmIndex, x, y,
            0,
            false,
            "",
            0L,
            0f,
            0f,
            ++_globalVersionCounter
        );
    }

    private void ClearPlotLocal(int farmIndex, int x, int y)
    {
        if (!InBounds(farmIndex, x, y)) return;

        var ps = grid[farmIndex, x, y];
        ps.ownerActor = 0;
        ps.occupied = false;
        ps.seedId = "";
        ps.plantUnix = 0;
        ps.growSeconds = 0f;
        ps.weight = 0f;
    }

    private bool IsActorStillInRoom(int actorNumber)
    {
        if (actorNumber <= 0) return false;
        if (PhotonNetwork.CurrentRoom == null) return false;
        return PhotonNetwork.CurrentRoom.Players != null &&
               PhotonNetwork.CurrentRoom.Players.ContainsKey(actorNumber);
    }

    private bool InBounds(int f, int x, int y)
    {
        return f >= 0 && f < FARMS && x >= 0 && x < W && y >= 0 && y < H;
    }

    public int? GetFarmForPlayFabId(string playFabId)
    {
        if (string.IsNullOrEmpty(playFabId)) return null;

        // Once local cache'e bak
        if (_playFabIdToFarm.TryGetValue(playFabId, out int farm))
            return farm;

        // Room properties'e bak
        var room = PhotonNetwork.CurrentRoom;
        if (room?.CustomProperties != null)
        {
            string mappingKey = $"farmMap_{playFabId}";
            if (room.CustomProperties.TryGetValue(mappingKey, out object farmObj))
            {
                if (farmObj is int farmFromProps)
                {
                    _playFabIdToFarm[playFabId] = farmFromProps;
                    return farmFromProps;
                }
            }
        }

        // Oyuncu custom properties'lerine bak
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue(FarmAssignmentService.PROP_PLAYFAB_ID, out object pfIdObj))
            {
                if (pfIdObj?.ToString() == playFabId)
                {
                    if (player.CustomProperties.TryGetValue(FarmAssignmentService.PROP_FARM, out object farmObj))
                    {
                        if (farmObj is int playerFarm)
                        {
                            _playFabIdToFarm[playFabId] = playerFarm;
                            return playerFarm;
                        }
                    }
                }
            }
        }

        return null;
    }

    private int GetFarmForActor(int actorNumber)
    {
        if (_actorFarmAssignment.TryGetValue(actorNumber, out int farm))
        {
            Debug.Log($"[FarmNetwork] GetFarmForActor {actorNumber}: Found in cache = {farm}");
            return farm;
        }

        var player = PhotonNetwork.CurrentRoom?.GetPlayer(actorNumber);
        if (player != null)
        {
            if (player.CustomProperties.ContainsKey(FarmAssignmentService.PROP_FARM))
            {
                int farmFromProps = (int)player.CustomProperties[FarmAssignmentService.PROP_FARM];
                Debug.Log($"[FarmNetwork] GetFarmForActor {actorNumber}: Found in props = {farmFromProps}");
                _actorFarmAssignment[actorNumber] = farmFromProps;
                return farmFromProps;
            }
            else
            {
                Debug.LogWarning($"[FarmNetwork] GetFarmForActor {actorNumber}: No farm property!");
            }
        }
        else
        {
            Debug.LogError($"[FarmNetwork] GetFarmForActor {actorNumber}: Player not found!");
        }

        return -1;
    }
}