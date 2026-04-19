using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

[Serializable]
public class MarketItem
{
    public string seedId;
    public int qty;
    public int buyPrice;
}

public class MarketRotationService : MonoBehaviourPunCallbacks
{
    private const string PROP_MARKET = "market";
    private const string PROP_MARKET_TS = "marketTs";

    [SerializeField] private List<SeedDefinition> allSeeds;     // Market s�ras� buradan gelecek
    [SerializeField] private int rotationSeconds = 300;         // 5 dk

    public event Action<string> OnMarketPackedUpdated;
    public event Action OnMarketRotated;

    private long _lastMarketTs;
    private string _lastKnownPacked;
    public long LastMarketTs => _lastMarketTs;
    public int RotationSeconds => rotationSeconds;

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
            EnsureMarketInitialized();

        // Odaya sonradan girenler i�in: mevcut props'u uygula
        ApplyMarketFromRoomProps();
    }

    private void Update()
    {
        if (!PhotonNetwork.InRoom) return;

        if (PhotonNetwork.IsMasterClient)
        {
            if (TryGetMarketStart(out long ts))
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (now - ts >= rotationSeconds)
                    PublishNewMarket();
            }
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(PROP_MARKET) || propertiesThatChanged.ContainsKey(PROP_MARKET_TS))
            ApplyMarketFromRoomProps(fromRotation: true);
    }

    public string GetCurrentPacked()
    {
        if (!PhotonNetwork.InRoom) return "";
        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_MARKET)) return "";
        return (string)PhotonNetwork.CurrentRoom.CustomProperties[PROP_MARKET];
    }

    private void EnsureMarketInitialized()
    {
        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_MARKET))
            PublishNewMarket();
    }

    private void PublishNewMarket()
    {
        long seed = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var items = GenerateRotation(seed);

        string packed = Pack(items);
        long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var props = new Hashtable
        {
            { PROP_MARKET, packed },
            { PROP_MARKET_TS, ts }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public void ForceRefreshCurrentMarket()
    {
        ApplyMarketFromRoomProps(fromRotation: false);
    }

    // S�ra: allSeeds list s�ras�
    // Qty: 0..20, rarity artt�k�a 0 gelme olas�l��� artar
    private List<MarketItem> GenerateRotation(long seed)
    {
        var rng = new System.Random((int)(seed % int.MaxValue));
        var result = new List<MarketItem>(allSeeds.Count);

        foreach (var def in allSeeds)
        {
            if (def == null || string.IsNullOrWhiteSpace(def.seedId)) continue;

            // 0 gelme olas�l��� (rarity y�kseldik�e artar)
            float zeroChance = def.rarity switch
            {
                Rarity.Common => 0.05f,
                Rarity.Uncommon => 0.12f,
                Rarity.Rare => 0.25f,
                Rarity.Epic => 0.45f,
                Rarity.Legendary => 0.65f,
                _ => 0.10f
            };

            int qty;
            if (rng.NextDouble() < zeroChance) qty = 0;
            else qty = rng.Next(0, 21); // 0..20 (0 dahil)

            // F�YAT: SeedDefinition sat�n alma fiyat�
            int price = Mathf.Max(1, def.seedPrice);

            result.Add(new MarketItem { seedId = def.seedId, qty = qty, buyPrice = price });
        }

        return result;
    }

    private bool TryGetMarketStart(out long ts)
    {
        ts = 0;
        if (!PhotonNetwork.InRoom) return false;
        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_MARKET_TS)) return false;

        object v = PhotonNetwork.CurrentRoom.CustomProperties[PROP_MARKET_TS];
        if (v is long l) { ts = l; return true; }
        if (v is int i) { ts = i; return true; }
        return false;
    }

    private void ApplyMarketFromRoomProps(bool fromRotation = false)
    {
        if (!PhotonNetwork.InRoom) return;
        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_MARKET)) return;

        _lastMarketTs = 0;
        if (TryGetMarketStart(out long ts)) _lastMarketTs = ts;

        string packed = (string)PhotonNetwork.CurrentRoom.CustomProperties[PROP_MARKET];
        Debug.Log($"Market updated: {packed} (ts={_lastMarketTs})");

        bool wasSet = !string.IsNullOrEmpty(_lastKnownPacked);
        bool changed = packed != _lastKnownPacked;
        _lastKnownPacked = packed;

        OnMarketPackedUpdated?.Invoke(packed);

        if (fromRotation && wasSet && changed && !string.IsNullOrEmpty(packed))
            OnMarketRotated?.Invoke();
    }

    // seedId|qty|price;seedId|qty|price
    private static string Pack(List<MarketItem> items)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < items.Count; i++)
        {
            if (i > 0) sb.Append(';');
            sb.Append(items[i].seedId).Append('|')
              .Append(items[i].qty).Append('|')
              .Append(items[i].buyPrice);
        }
        return sb.ToString();
    }

    public static List<MarketItem> Unpack(string s)
    {
        var result = new List<MarketItem>();
        if (string.IsNullOrWhiteSpace(s)) return result;

        var entries = s.Split(';');
        foreach (var e in entries)
        {
            var parts = e.Split('|');
            if (parts.Length != 3) continue;

            if (!int.TryParse(parts[1], out int qty)) continue;
            if (!int.TryParse(parts[2], out int price)) continue;

            result.Add(new MarketItem { seedId = parts[0], qty = qty, buyPrice = price });
        }
        return result;
    }

    public static string PackItems(List<MarketItem> items) => Pack(items);
}