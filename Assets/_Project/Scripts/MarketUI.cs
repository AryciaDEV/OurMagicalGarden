using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class MarketUI : MonoBehaviour
{
    [Header("Refs")]
    public Transform rowsRoot;
    public TMP_Text timerText;
    public MarketRotationService rotationService;
    public MarketPurchaseService purchaseService;

    [Header("Seed Definitions (icons from here)")]
    public List<SeedDefinition> seedDefs = new();

    private Dictionary<string, SeedDefinition> _seedById;
    private MarketRowUI[] rows;

    private void Awake()
    {
        if (!rowsRoot)
        {
            Debug.LogError("[MarketUI] rowsRoot is NULL. Assign RowsContainer.");
            rows = new MarketRowUI[0];
            return;
        }

        rows = rowsRoot.GetComponentsInChildren<MarketRowUI>(true)
            .OrderBy(r => r.transform.GetSiblingIndex())
            .ToArray();

        _seedById = new Dictionary<string, SeedDefinition>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var d in seedDefs)
        {
            if (d == null || string.IsNullOrWhiteSpace(d.seedId)) continue;
            _seedById[d.seedId] = d;
        }

        Debug.Log($"[MarketUI] Found {rows.Length} rows. seedDefs={seedDefs.Count}");
    }

    private void OnEnable()
    {
        if (rotationService != null)
            rotationService.OnMarketPackedUpdated += OnMarketUpdated;

        // Panel açýlýr açýlmaz bas
        if (rotationService != null)
        {
            string packed = rotationService.GetCurrentPacked();
            if (!string.IsNullOrWhiteSpace(packed))
                OnMarketUpdated(packed);
        }
    }

    private void OnDisable()
    {
        if (rotationService != null)
            rotationService.OnMarketPackedUpdated -= OnMarketUpdated;
    }

    private void Update()
    {
        if (!timerText || rotationService == null) return;

        long ts = rotationService.LastMarketTs;
        if (ts <= 0) { timerText.text = "--:--"; return; }

        long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long elapsed = now - ts;
        long remain = rotationService.RotationSeconds - elapsed;
        if (remain < 0) remain = 0;

        long mm = remain / 60;
        long ss = remain % 60;
        timerText.text = $"{mm:00}:{ss:00}";
    }

    private void OnMarketUpdated(string packed)
    {
        if (purchaseService == null)
        {
            Debug.LogError("[MarketUI] purchaseService is NULL.");
            return;
        }

        var items = MarketRotationService.Unpack(packed);

        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];

            if (i >= items.Count)
            {
                SetEmpty(row);
                continue;
            }

            var it = items[i];

            row.seedId = it.seedId;
            row.price = it.buyPrice;
            row.qty = it.qty;

            if (row.seedText) row.seedText.text = it.seedId;
            //if (row.priceText) row.priceText.text = it.buyPrice.ToString();
            if (row.priceText) row.priceText.text = NumberShortener.Format(it.buyPrice);
            if (row.qtyText) row.qtyText.text = it.qty.ToString();

            if (row.iconImage)
            {
                if (_seedById != null && _seedById.TryGetValue(it.seedId, out var def) && def != null && def.icon != null)
                {
                    row.iconImage.enabled = true;
                    row.iconImage.sprite = def.icon;
                }
                else
                {
                    row.iconImage.enabled = false;
                    row.iconImage.sprite = null;
                }
            }

            bool stockOk = it.qty > 0;
            bool moneyOk = PlayerEconomy.Local == null || PlayerEconomy.Local.CanAfford(it.buyPrice);

            if (row.buyButton)
                row.buyButton.interactable = stockOk && moneyOk;

            // ? tek yerden bind
            row.BindBuy(purchaseService, it.seedId, it.buyPrice);
        }
    }

    private void SetEmpty(MarketRowUI row)
    {
        row.seedId = "";
        row.price = 0;
        row.qty = 0;

        if (row.seedText) row.seedText.text = "-";
        if (row.priceText) row.priceText.text = "-";
        if (row.qtyText) row.qtyText.text = "-";

        if (row.iconImage)
        {
            row.iconImage.enabled = false;
            row.iconImage.sprite = null;
        }

        //row.ClearBuy();
    }
}