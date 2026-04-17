using Photon.Pun;
using System.Collections;
using UnityEngine;

public class SaveApplyManager : MonoBehaviour
{
    [Header("Refs")]
    public PlayerEconomy playerEconomy;
    public PlayerSeedBag playerSeedBag;
    public PlayerPetInventory playerPetInventory;
    public PlayerInventory playerInventory;   // <-- satýlacak item envanteri
    public FarmNetwork farmNetwork;
    public InventoryNetworkService inventoryNetworkService;
    public PetNetworkService petNetworkService;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() =>
            SaveManager.Instance != null &&
            SaveManager.Instance.CurrentSave != null);

        yield return new WaitUntil(() =>
            FindFirstObjectByType<PlayerEconomy>() != null &&
            FindFirstObjectByType<PlayerSeedBag>() != null &&
            FindFirstObjectByType<PlayerPetInventory>() != null &&
            FindFirstObjectByType<PlayerInventory>() != null
        );

        playerEconomy = FindFirstObjectByType<PlayerEconomy>();
        playerSeedBag = FindFirstObjectByType<PlayerSeedBag>();
        playerPetInventory = FindFirstObjectByType<PlayerPetInventory>();
        playerInventory = FindFirstObjectByType<PlayerInventory>();
        if (!farmNetwork) farmNetwork = FindFirstObjectByType<FarmNetwork>();
        if (!inventoryNetworkService) inventoryNetworkService = FindFirstObjectByType<InventoryNetworkService>();
        if (!petNetworkService) petNetworkService = FindFirstObjectByType<PetNetworkService>();

        ApplySave(SaveManager.Instance.CurrentSave);
    }

    public void ApplySave(PlayerSaveData save)
    {
        if (save == null) return;

        if (playerEconomy != null)
            ApplyCoins(save.coins);

        if (playerSeedBag != null)
            ApplySeeds(save);

        if (playerInventory != null)
            ApplyItems(save);

        if (playerPetInventory != null)
            ApplyPets(save);

        if (farmNetwork != null)
            ApplyPlots(save);

        Debug.Log("[SaveApply] Save applied.");

        ApplyAuthoritativeRestore(save);
    }

    private void ApplyCoins(int coins)
    {
        int current = playerEconomy != null ? playerEconomy.Coins : 0;
        int diff = coins - current;

        if (diff > 0) playerEconomy.AddCoins(diff);
        else if (diff < 0) playerEconomy.SpendCoins(-diff);

        if (playerEconomy != null && AuthSession.IsLoggedIn)
            PlayFabLeaderboardService.SubmitCoins(playerEconomy.Coins);
    }

    private void ApplySeeds(PlayerSaveData save)
    {
        playerSeedBag.ClearAllFromSave();

        foreach (var s in save.seeds)
        {
            if (s == null) continue;
            if (string.IsNullOrWhiteSpace(s.seedId)) continue;
            if (s.count <= 0) continue;

            playerSeedBag.AddSeed(s.seedId, s.count);
        }
    }

    private void ApplyItems(PlayerSaveData save)
    {
        playerInventory.ClearAllFromSave();

        foreach (var it in save.items)
        {
            if (it == null) continue;

            playerInventory.LocalAdd(new InventoryItem
            {
                uid = it.uid,
                seedId = it.seedId,
                weight = it.weight
            });
        }

        playerInventory.NotifyChangedFromSave();
    }

    private void ApplyPets(PlayerSaveData save)
    {
        playerPetInventory.ClearAllFromSave();

        foreach (var p in save.pets)
        {
            if (p == null) continue;

            playerPetInventory.LocalAdd(new PetItem
            {
                uid = p.uid,
                petId = p.petId,
                eggId = p.eggId
            });
        }

        playerPetInventory.LocalSetEquipped(save.equippedPetUid);
        playerPetInventory.NotifyChangedFromSave();

        // ? Equip'li pet tekrar girince diðer oyuncular da görsün
        if (petNetworkService != null)
            petNetworkService.SyncLocalEquippedToPhotonProps();
    }

    private void ApplyPlots(PlayerSaveData save)
    {
        foreach (var p in save.plots)
        {
            if (p == null) continue;

            farmNetwork.ApplyPlotStateFromSave(
                p.farmIndex,
                p.x,
                p.y,
                p.occupied,
                p.seedId,
                p.plantUnix,
                p.growSeconds,
                p.weight
            );
        }
    }

    private void ApplyAuthoritativeRestore(PlayerSaveData save)
    {
        if (!PhotonNetwork.InRoom) return;
        if (!PhotonNetwork.IsMasterClient) return;

        int actor = PhotonNetwork.LocalPlayer.ActorNumber;

        if (inventoryNetworkService != null)
            inventoryNetworkService.RestoreFromSaveAsMaster(actor, save.items);

        if (petNetworkService != null)
            petNetworkService.RestoreFromSaveAsMaster(actor, save.pets, save.equippedPetUid);
    }
}