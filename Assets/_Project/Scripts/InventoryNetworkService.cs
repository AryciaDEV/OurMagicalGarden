using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[Serializable]
public class ItemSaveListWrapper
{
    public List<ItemStackData> items = new List<ItemStackData>();
}

public class InventoryNetworkService : MonoBehaviourPunCallbacks
{
    public static InventoryNetworkService Instance { get; private set; }

    private const string P_COINS = "coins";

    [Header("Seed Definitions (sellPrice from here)")]
    public List<SeedDefinition> seedDefs = new List<SeedDefinition>();

    private Dictionary<string, SeedDefinition> _seedById;

    private readonly Dictionary<int, Dictionary<int, InventoryItem>> _inv = new Dictionary<int, Dictionary<int, InventoryItem>>();
    private readonly Dictionary<int, int> _nextUid = new Dictionary<int, int>();

    private static readonly List<InventoryItem> _pendingLocal = new List<InventoryItem>();

    private readonly HashSet<int> _restoreCompletedActors = new HashSet<int>();

    private void Awake()
    {
        Instance = this;

        _seedById = new Dictionary<string, SeedDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in seedDefs)
        {
            if (d == null || string.IsNullOrWhiteSpace(d.seedId)) continue;
            _seedById[d.seedId.Trim()] = d;
        }

        Debug.Log($"[InventoryNetwork] Awake seedDefs={seedDefs.Count} dict={_seedById.Count}");
    }

    public void RestoreFromSaveAsMaster(int actorNumber, List<ItemStackData> items)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (actorNumber <= 0) return;

        if (!_inv.TryGetValue(actorNumber, out var dict))
        {
            dict = new Dictionary<int, InventoryItem>();
            _inv[actorNumber] = dict;
        }
        else
        {
            dict.Clear();
        }

        int maxUid = 0;

        if (items != null)
        {
            foreach (var it in items)
            {
                if (it == null) continue;

                InventoryItem invItem = new InventoryItem
                {
                    uid = it.uid,
                    seedId = it.seedId,
                    weight = it.weight
                };

                dict[invItem.uid] = invItem;
                if (invItem.uid > maxUid) maxUid = invItem.uid;
            }
        }

        _nextUid[actorNumber] = maxUid + 1;
        _restoreCompletedActors.Add(actorNumber);

        Debug.Log($"[InventoryNetwork] Master restore complete for actor={actorNumber}, items={items?.Count ?? 0}");
    }

    public void RestoreLocalFromSave(List<ItemStackData> items)
    {
        if (PlayerInventory.Local == null)
        {
            Debug.LogWarning("[InventoryNetwork] PlayerInventory.Local not ready, buffering...");
            return;
        }

        PlayerInventory.Local.ClearAllLocal();

        if (items != null)
        {
            foreach (var it in items)
            {
                if (it == null) continue;

                PlayerInventory.Local.LocalAdd(new InventoryItem
                {
                    uid = it.uid,
                    seedId = it.seedId,
                    weight = it.weight
                });
            }
        }

        PlayerInventory.Local.NotifyChangedFromSave();
        Debug.Log($"[InventoryNetwork] RestoreLocalFromSave count={items?.Count ?? 0}");
    }

    public void RequestRestoreFromSaveForLocal(List<ItemStackData> items)
    {
        RestoreLocalFromSave(items);

        if (!PhotonNetwork.InRoom) return;
        if (PhotonNetwork.LocalPlayer == null) return;

        var wrapper = new ItemSaveListWrapper
        {
            items = items ?? new List<ItemStackData>()
        };

        string json = JsonUtility.ToJson(wrapper);

        photonView.RPC(
            nameof(RPC_RestoreInventoryFromSave),
            RpcTarget.MasterClient,
            PhotonNetwork.LocalPlayer.ActorNumber,
            json
        );
    }

    [PunRPC]
    private void RPC_RestoreInventoryFromSave(int actorNumber, string json)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var wrapper = JsonUtility.FromJson<ItemSaveListWrapper>(json);
        var items = wrapper != null && wrapper.items != null
            ? wrapper.items
            : new List<ItemStackData>();

        RestoreFromSaveAsMaster(actorNumber, items);

        Debug.Log($"[InventoryNetwork] MASTER restore actor={actorNumber} count={items.Count}");

        var sender = PhotonNetwork.CurrentRoom?.GetPlayer(actorNumber);
        if (sender != null)
        {
            photonView.RPC(nameof(RPC_RestoreInventoryComplete), sender, items.Count);
        }
    }

    [PunRPC]
    private void RPC_RestoreInventoryComplete(int count)
    {
        Debug.Log($"[InventoryNetwork] Inventory restore confirmed: {count} items");
    }

    public void RestorePlayerFromSave(Player owner, List<ItemStackData> items)
    {
        if (owner == null) return;

        if (PhotonNetwork.IsMasterClient)
            RestoreFromSaveAsMaster(owner.ActorNumber, items);

        if (owner == PhotonNetwork.LocalPlayer)
            RestoreLocalFromSave(items);
    }

    public static void FlushPendingToLocal()
    {
        if (PlayerInventory.Local == null) return;
        if (_pendingLocal.Count == 0) return;

        for (int i = 0; i < _pendingLocal.Count; i++)
            PlayerInventory.Local.LocalAdd(_pendingLocal[i]);

        _pendingLocal.Clear();
    }

    public void Master_AddItem(Player owner, string seedId, float weight)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (owner == null) return;

        string sid = string.IsNullOrWhiteSpace(seedId) ? "" : seedId.Trim();
        if (string.IsNullOrWhiteSpace(sid)) return;

        int actor = owner.ActorNumber;

        if (!_inv.TryGetValue(actor, out var dict))
        {
            dict = new Dictionary<int, InventoryItem>();
            _inv[actor] = dict;
        }

        if (!_nextUid.TryGetValue(actor, out int uid))
            uid = 1;

        InventoryItem it = new InventoryItem
        {
            uid = uid,
            seedId = sid,
            weight = weight
        };

        dict[it.uid] = it;
        _nextUid[actor] = uid + 1;

        photonView.RPC(nameof(RPC_AddItemLocal), owner, it.uid, it.seedId, it.weight);
    }

    public void RequestSell(int uid)
    {
        if (!PhotonNetwork.InRoom) return;
        if (uid <= 0) return;

        photonView.RPC(nameof(RPC_RequestSell), RpcTarget.MasterClient, uid);
    }

    [PunRPC]
    private void RPC_RequestSell(int uid, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var sender = info.Sender;
        if (sender == null) return;

        int actor = sender.ActorNumber;

        if (!_inv.TryGetValue(actor, out var dict) || !dict.TryGetValue(uid, out var it))
        {
            photonView.RPC(nameof(RPC_SellResult), sender, false, uid, 0, "NotFound");
            return;
        }

        int coinsToGive = ComputeSellCoins(actor, it.seedId, it.weight);

        dict.Remove(uid);

        int curCoins = GetPlayerCoins(sender);
        SetPlayerCoins(sender, curCoins + coinsToGive);

        photonView.RPC(nameof(RPC_RemoveItemLocal), sender, uid);
        photonView.RPC(nameof(RPC_SellResult), sender, true, uid, coinsToGive, "");
    }

    private int ComputeSellCoins(int actor, string seedId, float weight)
    {
        string key = string.IsNullOrWhiteSpace(seedId) ? "" : seedId.Trim();

        int coins = 0;

        if (_seedById != null && _seedById.TryGetValue(key, out var def) && def != null)
        {
            int basePrice = def.sellPrice;

            float w01 = 0.5f;
            if (def.maxWeight > def.minWeight)
                w01 = Mathf.InverseLerp(def.minWeight, def.maxWeight, weight);

            float mul = Mathf.Lerp(1f, 1.3f, w01);
            coins = Mathf.Max(0, Mathf.RoundToInt(basePrice * mul));
        }

        var petSvc = PetNetworkService.Instance;
        if (petSvc != null)
        {
            var (_, _, sellPct) = petSvc.Master_GetActorEquippedBonuses(actor);
            float sellMul = 1f + (sellPct / 100f);
            coins = Mathf.RoundToInt(coins * sellMul);
        }

        return Mathf.Max(0, coins);
    }

    [PunRPC]
    private void RPC_AddItemLocal(int uid, string seedId, float weight)
    {
        InventoryItem it = new InventoryItem { uid = uid, seedId = seedId, weight = weight };

        if (PlayerInventory.Local != null)
            PlayerInventory.Local.LocalAdd(it);
        else
            _pendingLocal.Add(it);
    }

    [PunRPC]
    private void RPC_RemoveItemLocal(int uid)
    {
        if (PlayerInventory.Local != null)
            PlayerInventory.Local.LocalRemove(uid);
    }

    [PunRPC]
    private void RPC_SellResult(bool ok, int uid, int coinsGained, string reason)
    {
        if (!ok)
            Debug.Log($"[Inventory] Sell failed uid={uid} reason={reason}");
        else
            Debug.Log($"[Inventory] Sold uid={uid} +{coinsGained} coins");
    }

    private int GetPlayerCoins(Player p)
    {
        if (p == null) return 0;
        if (p.CustomProperties != null && p.CustomProperties.TryGetValue(P_COINS, out object v))
        {
            if (v is int i) return i;
            if (v is long l) return (int)l;
        }
        return 0;
    }

    private void SetPlayerCoins(Player p, int newCoins)
    {
        if (p == null) return;
        p.SetCustomProperties(new Hashtable { { P_COINS, newCoins } });
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        _inv.Remove(otherPlayer.ActorNumber);
        _nextUid.Remove(otherPlayer.ActorNumber);
        _restoreCompletedActors.Remove(otherPlayer.ActorNumber);
    }
}