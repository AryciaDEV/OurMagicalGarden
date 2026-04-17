using System.Collections;
using Photon.Pun;
using UnityEngine;

public class PlayerSaveLoader : MonoBehaviourPunCallbacks
{
    private IEnumerator Start()
    {
        if (!photonView.IsMine)
            yield break;

        PlayerSaveData data = null;
        bool done = false;

        // BURAYI kendi gerçek save/load koduna baðlayacaksýn.
        // Þimdilik compile alsýn diye boþ býrakýyoruz.
        RequestLoad(result =>
        {
            data = result;
            done = true;
        });

        while (!done)
            yield return null;

        if (data == null)
        {
            Debug.LogWarning("[SaveLoader] Save data is null.");
            yield break;
        }

        Debug.Log("[SaveLoader] Loaded save, applying...");

        // Coins
        var eco = PlayerEconomy.Local;
        if (eco != null)
            eco.SetCoins(data.coins, false);

        // Seeds
        if (PlayerSeedBag.Local != null)
        {
            PlayerSeedBag.Local.ClearAllFromSave();

            if (data.seeds != null)
            {
                foreach (var s in data.seeds)
                {
                    if (s == null) continue;
                    if (string.IsNullOrWhiteSpace(s.seedId)) continue;
                    if (s.count <= 0) continue;

                    PlayerSeedBag.Local.AddSeed(s.seedId, s.count);
                }
            }
        }

        // Inventory
        if (PhotonNetwork.IsMasterClient)
        {
            var inv = InventoryNetworkService.Instance;
            if (inv != null)
            {
                inv.RestoreFromSaveAsMaster(
                    PhotonNetwork.LocalPlayer.ActorNumber,
                    data.items
                );
            }
        }

        // Farm
        var farm = FindFirstObjectByType<FarmNetwork>();
        if (farm != null && data.plots != null)
        {
            foreach (var p in data.plots)
            {
                if (p == null) continue;

                farm.ApplyPlotStateFromSave(
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

        Debug.Log("[SaveLoader] Apply complete.");
    }

    private void RequestLoad(System.Action<PlayerSaveData> onLoaded)
    {
        // GEÇÝCÝ:
        // Compile alsýn diye null döndürüyoruz.
        // Bunu senin gerçek PlayFab load kodunla deðiþtireceðiz.
        onLoaded?.Invoke(null);
    }
}