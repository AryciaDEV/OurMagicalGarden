using System;
using System.Collections.Generic;
using UnityEngine;

public class SeedPriceResolver : MonoBehaviour
{
    public static SeedPriceResolver Instance { get; private set; }

    [Header("All Seed Definitions")]
    public List<SeedDefinition> seedDefs = new();

    private Dictionary<string, SeedDefinition> _byId;

    private void Awake()
    {
        Instance = this;

        _byId = new Dictionary<string, SeedDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in seedDefs)
        {
            if (d == null || string.IsNullOrWhiteSpace(d.seedId)) continue;
            _byId[d.seedId.Trim()] = d;
        }

        Debug.Log($"[SeedPriceResolver] defs={seedDefs.Count} dict={_byId.Count}");
    }

    public bool TryGetSellPrice(string seedId, out int price)
    {
        price = 0;
        if (string.IsNullOrWhiteSpace(seedId)) return false;

        string key = seedId.Trim();
        if (_byId != null && _byId.TryGetValue(key, out var def) && def != null)
        {
            price = def.sellPrice;
            return true;
        }
        return false;
    }
}