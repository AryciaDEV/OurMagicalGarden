using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("UI")]
    public Transform slotsRoot;
    public InventorySlotUI slotPrefab;

    [Header("Services")]
    public InventoryNetworkService invNet;

    [Header("Seed Definitions (icon + sellPrice)")]
    public List<SeedDefinition> seedDefs = new();

    private Dictionary<string, SeedDefinition> _seedById;
    private readonly List<InventorySlotUI> _spawned = new();

    [SerializeField] private FarmNetwork farmNetwork;

    private bool _bound;

    public AudioClip myAudioClip;

    private void Awake()
    {
        _seedById = new Dictionary<string, SeedDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in seedDefs)
        {
            if (d == null || string.IsNullOrWhiteSpace(d.seedId)) continue;
            _seedById[d.seedId.Trim()] = d;
        }

        if (!invNet) invNet = InventoryNetworkService.Instance;
    }

    private void Update()
    {
        if (_bound) return;
        if (PlayerInventory.Local == null) return;

        PlayerInventory.Local.OnChanged += Refresh;
        _bound = true;
        Refresh();
    }

    private void OnDisable()
    {
        if (PlayerInventory.Local != null)
            PlayerInventory.Local.OnChanged -= Refresh;

        _bound = false;
    }

    public void Refresh()
    {
        if (PlayerInventory.Local == null) return;
        if (!slotsRoot || !slotPrefab) return;

        // clear
        foreach (var s in _spawned)
            if (s) Destroy(s.gameObject);
        _spawned.Clear();

        var items = PlayerInventory.Local.GetAll();
        foreach (var it in items)
        {
            var slot = Instantiate(slotPrefab, slotsRoot);
            slot.uid = it.uid;
            if (slot.nameText) slot.nameText.text = it.seedId;
            if (slot.weightText) slot.weightText.text = $"Weight: {it.weight:0.00}";

            // price display (client-side tahmin; master final verir)
            int price = farmNetwork.GetHarvestCoins(it.seedId, it.weight);
            if (_seedById.TryGetValue(it.seedId, out var def) && def != null)
                //price = def.sellPrice;

            //Burada yazýyor olabilir Envanterde ki satýþ fiyatý
            if (slot.priceText) slot.priceText.text = $"Sell: {price}";
            string formattedPrice = NumberShortener.Format(price);
            slot.priceText.text = $"Sell: {formattedPrice}";


            if (slot.iconImage)
            {
                if (def != null && def.icon != null)
                {
                    slot.iconImage.enabled = true;
                    slot.iconImage.sprite = def.icon;
                }
                else
                {
                    slot.iconImage.enabled = false;
                    slot.iconImage.sprite = null;
                }
            }

            if (slot.sellButton)
            {
                int capturedUid = it.uid;
                slot.sellButton.onClick.RemoveAllListeners();
                slot.sellButton.onClick.AddListener(() =>
                {
                    if (invNet == null) invNet = InventoryNetworkService.Instance;
                    if (invNet == null) return;

                    invNet.RequestSell(capturedUid);
                    SoundFXManager.Instance.PlaySound(myAudioClip);
                    SoundFXManager.Instance.SetVolume(0.5f);
                });
            }

            _spawned.Add(slot);
        }
    }
}