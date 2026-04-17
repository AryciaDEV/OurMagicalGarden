using UnityEngine;

public class SaveCollector : MonoBehaviour
{
    [Header("Refs")]
    public PlayerEconomy playerEconomy;
    public PlayerSeedBag playerSeedBag;
    public PlayerPetInventory playerPetInventory;
    public PlayerInventory playerInventory;   // <-- satýlacak item envanteri
    public FarmNetwork farmNetwork;

    [Header("Auto Save")]
    public bool autoSave = true;
    public float autoSaveSeconds = 20f;

    private float _timer;

    private void RefreshRefs()
    {
        if (!playerEconomy) playerEconomy = FindFirstObjectByType<PlayerEconomy>();
        if (!playerSeedBag) playerSeedBag = FindFirstObjectByType<PlayerSeedBag>();
        if (!playerPetInventory) playerPetInventory = FindFirstObjectByType<PlayerPetInventory>();
        if (!playerInventory) playerInventory = FindFirstObjectByType<PlayerInventory>();
        if (!farmNetwork) farmNetwork = FindFirstObjectByType<FarmNetwork>();
    }

    private void Start()
    {
        RefreshRefs();
    }

    private void Update()
    {
        if (!autoSave) return;
        if (SaveManager.Instance == null) return;
        if (!AuthSession.IsLoggedIn) return;

        _timer += Time.deltaTime;
        if (_timer >= autoSaveSeconds)
        {
            _timer = 0f;
            SaveNow();
        }
    }

    public void SaveNow()
    {
        RefreshRefs();

        if (SaveManager.Instance == null) return;
        var data = BuildSave();
        SaveManager.Instance.SaveNow(data);
    }

    public PlayerSaveData BuildSave()
    {
        var data = new PlayerSaveData
        {
            playerId = AuthSession.PlayFabId,
            username = AuthSession.Username,
            isGuest = AuthSession.IsGuest,
            coins = playerEconomy != null ? playerEconomy.Coins : 0,
            equippedPetUid = playerPetInventory != null ? playerPetInventory.EquippedUid : 0
        };

        if (playerSeedBag != null)
        {
            foreach (var kv in playerSeedBag.GetAll())
            {
                data.seeds.Add(new SeedStackData
                {
                    seedId = kv.Key,
                    count = kv.Value
                });
            }
        }

        if (playerInventory != null)
        {
            foreach (var it in playerInventory.GetAll())
            {
                if (it == null) continue;

                data.items.Add(new ItemStackData
                {
                    uid = it.uid,
                    seedId = it.seedId,
                    weight = it.weight
                });
            }
        }

        if (playerPetInventory != null)
        {
            foreach (var p in playerPetInventory.GetAll())
            {
                if (p == null) continue;

                data.pets.Add(new PetSaveData
                {
                    uid = p.uid,
                    petId = p.petId,
                    eggId = p.eggId
                });
            }
        }

        if (farmNetwork != null)
        {
            foreach (var ps in farmNetwork.GetAllPlotStatesForSave())
            {
                data.plots.Add(new PlotSaveData
                {
                    farmIndex = ps.farmIndex,
                    x = ps.x,
                    y = ps.y,
                    occupied = ps.occupied,
                    seedId = ps.seedId,
                    plantUnix = ps.plantUnix,
                    growSeconds = ps.growSeconds,
                    weight = ps.weight
                });
            }
        }

        return data;
    }

    private void OnApplicationQuit()
    {
        SaveNow();
    }
}