using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[Serializable]
public class PendingSeedGrant
{
    public string seedId;
    public int amount;
    public int tx;
}

public class MarketPurchaseService : MonoBehaviourPunCallbacks
{
    private const string PROP_MARKET = "market";
    private const string P_COINS = "coins";

    public event Action<bool, string, string> OnBuyResultLocal;

    [Header("Anti Spam")]
    [SerializeField] private float masterBuyCooldown = 0.20f;
    [SerializeField] private float localBuyCooldown = 0.25f;

    public AudioClip myAudioClip;

    // Master: spam / double request engeli
    private readonly Dictionary<int, float> _lastBuyByActor = new();

    // Master: actor -> tx counter
    private readonly Dictionary<int, int> _txByActor = new();

    // Local: duplicate tx ignore
    private int _lastAcceptedTx = 0;

    // Local: request spam lock
    private float _lastLocalBuyTime = -999f;
    private bool _buyPendingLocal;

    // Local: PlayerSeedBag hazýr deðilse seed grant buffer
    private static readonly List<PendingSeedGrant> _pendingSeedGrants = new();

    private void Update()
    {
        FlushPendingSeedsToLocal();
    }

    public void RequestBuy(string seedId)
    {
        if (!PhotonNetwork.InRoom) return;
        if (string.IsNullOrWhiteSpace(seedId)) return;

        float now = Time.time;

        // Local anti spam
        if (_buyPendingLocal) return;
        if (now - _lastLocalBuyTime < localBuyCooldown) return;

        _lastLocalBuyTime = now;
        _buyPendingLocal = true;

        photonView.RPC(nameof(RPC_RequestBuy), RpcTarget.MasterClient, seedId.Trim());
    }

    [PunRPC]
    private void RPC_RequestBuy(string seedId, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!PhotonNetwork.InRoom) return;
        if (string.IsNullOrWhiteSpace(seedId)) return;

        int actor = info.Sender != null ? info.Sender.ActorNumber : -1;
        if (actor < 0) return;

        // Master cooldown
        float now = Time.time;
        if (_lastBuyByActor.TryGetValue(actor, out float last) && (now - last) < masterBuyCooldown)
        {
            photonView.RPC(nameof(RPC_BuyResult), info.Sender, false, seedId, "Cooldown");
            return;
        }
        _lastBuyByActor[actor] = now;

        var room = PhotonNetwork.CurrentRoom;
        if (room == null || room.CustomProperties == null || !room.CustomProperties.ContainsKey(PROP_MARKET))
        {
            photonView.RPC(nameof(RPC_BuyResult), info.Sender, false, seedId, "NoMarket");
            return;
        }

        string packed = (string)room.CustomProperties[PROP_MARKET];
        var items = MarketRotationService.Unpack(packed);

        int idx = items.FindIndex(i => string.Equals(i.seedId, seedId, StringComparison.OrdinalIgnoreCase));
        if (idx < 0)
        {
            photonView.RPC(nameof(RPC_BuyResult), info.Sender, false, seedId, "NotFound");
            return;
        }

        var it = items[idx];
        if (it.qty <= 0)
        {
            photonView.RPC(nameof(RPC_BuyResult), info.Sender, false, seedId, "OutOfStock");
            return;
        }

        int price = Mathf.Max(1, it.buyPrice);
        int coins = GetPlayerCoins(info.Sender);

        if (coins < price)
        {
            photonView.RPC(nameof(RPC_BuyResult), info.Sender, false, seedId, "NotEnoughCoins");
            return;
        }

        // ===== Atomic purchase on MASTER =====
        // 1) coins düþ
        SetPlayerCoins(info.Sender, coins - price);

        // 2) stok düþ
        it.qty -= 1;
        items[idx] = it;

        var newPacked = MarketRotationService.PackItems(items);
        room.SetCustomProperties(new Hashtable { { PROP_MARKET, newPacked } });

        // 3) tx üret
        if (!_txByActor.TryGetValue(actor, out int tx))
            tx = 0;

        tx += 1;
        _txByActor[actor] = tx;

        // 4) önce seed grant, sonra result
        photonView.RPC(nameof(RPC_GiveSeed), info.Sender, seedId, 1, tx);
        photonView.RPC(nameof(RPC_BuyResult), info.Sender, true, seedId, "");

        if (SoundFXManager.Instance != null && myAudioClip != null)
            SoundFXManager.Instance.PlaySound(myAudioClip);

        Debug.Log($"[MarketPurchase] MASTER success actor={actor} seed={seedId} price={price} tx={tx}");
    }

    [PunRPC]
    private void RPC_BuyResult(bool ok, string seedId, string reason)
    {
        _buyPendingLocal = false;

        if (!ok)
            Debug.Log($"[MarketPurchase] Buy failed seed={seedId} reason={reason}");
        else
            Debug.Log($"[MarketPurchase] OK seed={seedId}");

        OnBuyResultLocal?.Invoke(ok, seedId, reason);
    }

    [PunRPC]
    private void RPC_GiveSeed(string seedId, int amount, int tx)
    {
        // Duplicate protection
        if (tx <= _lastAcceptedTx)
        {
            Debug.LogWarning($"[MarketPurchase] IGNORE duplicate GIVE seed={seedId} x{amount} tx={tx} last={_lastAcceptedTx}");
            return;
        }

        _lastAcceptedTx = tx;

        if (PlayerSeedBag.Local != null)
        {
            Debug.Log($"[MarketPurchase] GIVE seed={seedId} x{amount} tx={tx}");
            PlayerSeedBag.Local.AddSeed(seedId, amount);
        }
        else
        {
            _pendingSeedGrants.Add(new PendingSeedGrant
            {
                seedId = seedId,
                amount = amount,
                tx = tx
            });

            Debug.LogWarning($"[MarketPurchase] Buffered seed grant seed={seedId} x{amount} tx={tx}");
        }
    }

    private void FlushPendingSeedsToLocal()
    {
        if (PlayerSeedBag.Local == null) return;
        if (_pendingSeedGrants.Count == 0) return;

        for (int i = 0; i < _pendingSeedGrants.Count; i++)
        {
            var g = _pendingSeedGrants[i];
            PlayerSeedBag.Local.AddSeed(g.seedId, g.amount);
            Debug.Log($"[MarketPurchase] FLUSH buffered seed={g.seedId} x{g.amount} tx={g.tx}");
        }

        _pendingSeedGrants.Clear();
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
}