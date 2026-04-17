using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class FarmRestoreCoordinator : MonoBehaviourPunCallbacks
{
    public static FarmRestoreCoordinator Instance { get; private set; }

    [Header("Retry Settings")]
    [SerializeField] private int maxRetries = 5;
    [SerializeField] private float retryDelay = 2f;
    [SerializeField] private float initialDelay = 1.5f;
    [SerializeField] private float farmAssignmentTimeout = 15f;

    private bool _restoreCompleted;
    private int _retryCount;
    private bool _isRestoring;

    private void Awake()
    {
        Debug.Log("[FarmRestoreCoordinator] AWAKE");

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Debug.Log($"[FarmRestoreCoordinator] START - photonView.IsMine={photonView?.IsMine}, InRoom={PhotonNetwork.InRoom}");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[FarmRestoreCoordinator] OnJoinedRoom - photonView.IsMine={photonView.IsMine}");

        // YENI: photonView.IsMine kontrolunu kaldir - her zaman calissin
        // if (!photonView.IsMine) return;

        Debug.Log("[FarmRestoreCoordinator] Joined room, starting restore sequence...");
        _restoreCompleted = false;
        _retryCount = 0;
        _isRestoring = false;

        StartCoroutine(RestoreSequence());
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"[FarmRestoreCoordinator] OnMasterClientSwitched - photonView.IsMine={photonView.IsMine}");

        // if (!photonView.IsMine) return;

        if (!_restoreCompleted && !_isRestoring)
        {
            Debug.Log("[FarmRestoreCoordinator] Master switched, re-attempting restore...");
            StartCoroutine(RestoreSequence());
        }
    }

    private IEnumerator RestoreSequence()
    {
        Debug.Log("[FarmRestoreCoordinator] RestoreSequence STARTED");

        if (_isRestoring)
        {
            Debug.Log("[FarmRestoreCoordinator] Already restoring, skipping");
            yield break;
        }
        _isRestoring = true;

        // KRITIK: Farm assignment'i bekle - daha uzun ve sabirli bekle
        yield return new WaitForSeconds(initialDelay);

        float waitStart = Time.time;
        while (!FarmAuth.TryGetLocalFarmIndex(out int testFarm))
        {
            if (Time.time - waitStart > farmAssignmentTimeout)
            {
                Debug.LogError("[FarmRestoreCoordinator] Farm assignment timeout!");
                _isRestoring = false;
                yield break;
            }
            Debug.Log("[FarmRestoreCoordinator] Waiting for farm assignment...");
            yield return new WaitForSeconds(0.5f); // Daha sik kontrol
        }

        FarmAuth.TryGetLocalFarmIndex(out int assignedFarm);
        Debug.Log($"[FarmRestoreCoordinator] Farm {assignedFarm} assigned");

        // Save bekle - daha uzun timeout
        if (SaveManager.Instance?.CurrentSave == null)
        {
            Debug.Log("[FarmRestoreCoordinator] Waiting for save load...");
            float saveTimeout = Time.time + 15f; // 15 saniye timeout
            while (SaveManager.Instance?.CurrentSave == null && Time.time < saveTimeout)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        // Save hala yoksa devam et (bos save ile)
        if (SaveManager.Instance?.CurrentSave == null)
        {
            Debug.LogWarning("[FarmRestoreCoordinator] Save not loaded, proceeding with empty save");
        }
        else
        {
            var save = SaveManager.Instance.CurrentSave;
            Debug.Log($"[FarmRestoreCoordinator] Save loaded: coins={save.coins}, plots={save.plots?.Count ?? 0}");

            if (save.plots != null)
            {
                int occupiedCount = 0;
                foreach (var p in save.plots)
                {
                    if (p?.occupied == true)
                    {
                        occupiedCount++;
                        Debug.Log($"[FarmRestoreCoordinator] Save has plot: ({p.x},{p.y}) seed={p.seedId}, farmIndex={p.farmIndex}");
                    }
                }
                Debug.Log($"[FarmRestoreCoordinator] Total occupied plots in save: {occupiedCount}");
            }
        }

        // Restore dene - daha fazla retry
        while (_retryCount < maxRetries && !_restoreCompleted)
        {
            Debug.Log($"[FarmRestoreCoordinator] Restore attempt {_retryCount + 1}/{maxRetries}");

            bool success = ExecuteRestore();

            if (success)
            {
                // KRITIK: Restore'un tamamlanmasini bekle
                yield return new WaitForSeconds(1f);

                // FarmNetwork'ten kontrol et
                var farmNetwork = FindFirstObjectByType<FarmNetwork>();
                if (farmNetwork != null && farmNetwork.IsLocalRestoreCompleted)
                {
                    _restoreCompleted = true;
                    Debug.Log("[FarmRestoreCoordinator] Restore completed successfully!");
                    _isRestoring = false;
                    yield break;
                }
                else
                {
                    Debug.LogWarning("[FarmRestoreCoordinator] Restore RPC sent but not confirmed, will retry...");
                }
            }

            _retryCount++;
            if (_retryCount < maxRetries)
            {
                Debug.LogWarning($"[FarmRestoreCoordinator] Attempt failed, retrying in {retryDelay}s...");
                yield return new WaitForSeconds(retryDelay);
            }
        }

        if (!_restoreCompleted)
        {
            Debug.LogError("[FarmRestoreCoordinator] All restore attempts failed!");
        }

        _isRestoring = false;
    }

    private bool ExecuteRestore()
    {
        Debug.Log("[FarmRestoreCoordinator] ExecuteRestore STARTED");

        var save = SaveManager.Instance?.CurrentSave;
        if (save == null)
        {
            Debug.Log("[FarmRestoreCoordinator] No save data (first time player)");
            return true;
        }

        if (!FarmAuth.TryGetLocalFarmIndex(out int targetFarm))
        {
            Debug.LogError("[FarmRestoreCoordinator] No farm assigned!");
            return false;
        }

        // KRITIK: Save'deki farmIndex'i guncelle - HER ZAMAN su anki farm'a
        int previousFarm = -1;
        if (save.plots != null && save.plots.Count > 0)
        {
            // Onceki farm index'i kaydet
            previousFarm = save.plots[0].farmIndex;

            // TUM plot'lari su anki farm'a tasi
            foreach (var p in save.plots)
            {
                if (p != null)
                {
                    p.farmIndex = targetFarm;
                }
            }

            Debug.Log($"[FarmRestoreCoordinator] Updated all plots from farm {previousFarm} to {targetFarm}");
        }

        Debug.Log($"[FarmRestoreCoordinator] Farm info: target={targetFarm}, previous={previousFarm}");

        // Economy
        if (PlayerEconomy.Local != null)
        {
            PlayerEconomy.Local.SetCoins(save.coins, false);
            Debug.Log($"[FarmRestoreCoordinator] Coins set: {save.coins}");
        }

        // Seeds
        if (PlayerSeedBag.Local != null)
        {
            PlayerSeedBag.Local.ClearAllFromSave();
            if (save.seeds != null)
            {
                foreach (var s in save.seeds)
                {
                    if (s?.seedId != null && s.count > 0)
                        PlayerSeedBag.Local.AddSeed(s.seedId, s.count);
                }
            }
            Debug.Log("[FarmRestoreCoordinator] Seeds restored");
        }

        // Inventory
        var invService = InventoryNetworkService.Instance;
        if (invService != null && save.items != null)
        {
            invService.RequestRestoreFromSaveForLocal(save.items);
            Debug.Log("[FarmRestoreCoordinator] Inventory restore requested");
        }

        // Pets
        var petService = PetNetworkService.Instance;
        if (petService != null && save.pets != null)
        {
            petService.RequestRestoreFromSaveForLocal(save.pets, save.equippedPetUid);
            Debug.Log("[FarmRestoreCoordinator] Pets restore requested");
        }

        // FARM RESTORE - KRITIK KISIM
        var farmNetwork = FindFirstObjectByType<FarmNetwork>();
        if (farmNetwork == null)
        {
            Debug.LogError("[FarmRestoreCoordinator] FarmNetwork not found!");
            return false;
        }

        // Occupied plot'lari topla - save'deki TUM plot'lari gonder
        var occupiedPlots = new System.Collections.Generic.List<PlotSaveData>();
        if (save.plots != null)
        {
            foreach (var p in save.plots)
            {
                if (p == null) continue;

                // KRITIK: occupied=true olanlari ekle, farmIndex'i guncelle
                if (p.occupied)
                {
                    p.farmIndex = targetFarm; // Farm index'i guncelle
                    occupiedPlots.Add(p);
                    Debug.Log($"[FarmRestoreCoordinator] Adding to restore: plot ({p.x},{p.y}) seed={p.seedId}, plantUnix={p.plantUnix}");
                }
            }
        }

        Debug.Log($"[FarmRestoreCoordinator] Requesting restore: farm={targetFarm}, plots={occupiedPlots.Count}");

        // HEMEN gonder
        farmNetwork.RequestRestoreFromSaveForLocal(
            occupiedPlots,
            targetFarm,
            previousFarm,
            AuthSession.PlayFabId
        );

        StartCoroutine(DelayedMarketRefresh());

        return true;
    }

    private IEnumerator DelayedMarketRefresh()
    {
        yield return new WaitForSeconds(2f);

        var petMarket = FindFirstObjectByType<PetMarketRotationService>();
        petMarket?.ForceRefresh();

        var seedMarket = FindFirstObjectByType<MarketRotationService>();
        seedMarket?.ForceRefreshCurrentMarket();
    }

    public void ForceRetryRestore()
    {
        Debug.Log("[FarmRestoreCoordinator] ForceRetryRestore called");

        if (_isRestoring)
        {
            Debug.Log("[FarmRestoreCoordinator] Already restoring, skipping force retry");
            return;
        }

        _restoreCompleted = false;
        _retryCount = 0;
        StartCoroutine(RestoreSequence());
    }
}