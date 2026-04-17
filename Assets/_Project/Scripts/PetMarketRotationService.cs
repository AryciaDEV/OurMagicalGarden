using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

public class PetMarketRotationService : MonoBehaviourPunCallbacks
{
    public const string PROP_PET_MARKET = "petMarket";
    public const string PROP_PET_MARKET_TS = "petMarketTs";

    [Header("Rotation")]
    [SerializeField] private int rotationSeconds = 600;

    [Header("Egg Definitions (ALL eggs here)")]
    public List<EggDefinition> eggDefs = new();

    [Header("How many eggs shown? (0 = show all)")]
    public int eggsInRotation = 0;

    public long LastMarketTs { get; private set; }
    public int RotationSeconds => rotationSeconds;

    public event Action<string> OnMarketPackedUpdated;

    private Dictionary<string, EggDefinition> _eggById;

    private void Awake()
    {
        _eggById = new Dictionary<string, EggDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in eggDefs)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.eggId)) continue;
            _eggById[e.eggId.Trim()] = e;
        }
    }

    private void Start()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
            EnsureMarketFresh(force: true);
    }

    private void Update()
    {
        if (!PhotonNetwork.InRoom) return;
        if (!PhotonNetwork.IsMasterClient) return;

        EnsureMarketFresh(force: false);
    }

    private void EnsureMarketFresh(bool force)
    {
        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        long ts = 0;
        if (room.CustomProperties != null && room.CustomProperties.TryGetValue(PROP_PET_MARKET_TS, out object v))
        {
            if (v is int i) ts = i;
            else if (v is long l) ts = l;
        }

        bool expired = (ts <= 0) || (now - ts >= rotationSeconds);
        if (!force && !expired) return;

        var items = BuildRotationItems();
        string packed = Pack(items);

        var ht = new Hashtable
        {
            { PROP_PET_MARKET, packed },
            { PROP_PET_MARKET_TS, now }
        };
        room.SetCustomProperties(ht);

        LastMarketTs = now;
    }

    private List<(string eggId, int qty)> BuildRotationItems()
    {
        var list = new List<(string eggId, int qty)>();

        var valid = eggDefs
            .Where(e => e != null && !string.IsNullOrWhiteSpace(e.eggId))
            .ToList();

        if (valid.Count == 0) return list;

        List<EggDefinition> chosen;
        if (eggsInRotation <= 0 || eggsInRotation >= valid.Count)
        {
            chosen = valid;
        }
        else
        {
            chosen = valid.OrderBy(_ => UnityEngine.Random.value).Take(eggsInRotation).ToList();
        }

        foreach (var e in chosen)
        {
            int minQ = Mathf.Min(e.minQty, e.maxQty);
            int maxQ = Mathf.Max(e.minQty, e.maxQty);
            int qty = UnityEngine.Random.Range(minQ, maxQ + 1);
            list.Add((e.eggId.Trim(), qty));
        }

        return list;
    }

    public string GetCurrentPacked()
    {
        if (!PhotonNetwork.InRoom) return "";
        var room = PhotonNetwork.CurrentRoom;
        if (room?.CustomProperties == null) return "";
        return room.CustomProperties.TryGetValue(PROP_PET_MARKET, out object v) ? (v as string ?? "") : "";
    }

    public void ForceRefresh()
    {
        if (!PhotonNetwork.InRoom) return;

        var room = PhotonNetwork.CurrentRoom;
        if (room?.CustomProperties == null) return;

        if (room.CustomProperties.TryGetValue(PROP_PET_MARKET, out object packedObj))
        {
            string packed = packedObj as string ?? "";
            if (!string.IsNullOrEmpty(packed))
            {
                if (room.CustomProperties.TryGetValue(PROP_PET_MARKET_TS, out object tsObj))
                {
                    if (tsObj is int i) LastMarketTs = i;
                    else if (tsObj is long l) LastMarketTs = l;
                }

                Debug.Log($"[PetMarketRotationService] Force refresh: ts={LastMarketTs}");
                OnMarketPackedUpdated?.Invoke(packed);
            }
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged == null) return;

        if (propertiesThatChanged.TryGetValue(PROP_PET_MARKET, out object packedObj))
        {
            string packed = packedObj as string ?? "";
            OnMarketPackedUpdated?.Invoke(packed);
        }

        if (propertiesThatChanged.TryGetValue(PROP_PET_MARKET_TS, out object tsObj))
        {
            if (tsObj is int i) LastMarketTs = i;
            else if (tsObj is long l) LastMarketTs = l;
        }
    }

    public static string Pack(List<(string eggId, int qty)> items)
    {
        if (items == null || items.Count == 0) return "";
        return string.Join(";", items.Select(x => $"{x.eggId}|{x.qty}"));
    }

    public static List<(string eggId, int qty)> Unpack(string packed)
    {
        var list = new List<(string eggId, int qty)>();
        if (string.IsNullOrWhiteSpace(packed)) return list;

        var rows = packed.Split(';');
        foreach (var r in rows)
        {
            if (string.IsNullOrWhiteSpace(r)) continue;
            var p = r.Split('|');
            if (p.Length < 2) continue;

            string eggId = (p[0] ?? "").Trim();
            if (string.IsNullOrWhiteSpace(eggId)) continue;

            if (!int.TryParse(p[1], out int qty)) qty = 0;
            list.Add((eggId, qty));
        }
        return list;
    }
}