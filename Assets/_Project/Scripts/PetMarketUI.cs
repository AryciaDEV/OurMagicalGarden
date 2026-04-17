using Photon.Pun;
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

public class PetMarketUI : MonoBehaviour
{
    [Header("UI")]
    public Transform rowsRoot;
    public TMP_Text timerText;

    [Header("Services")]
    public PetMarketRotationService rotationService;
    public PetNetworkService petService;

    private PetMarketRowUI[] _rows;

    private void Awake()
    {
        if (!rowsRoot)
        {
            Debug.LogError("[PetMarketUI] rowsRoot missing");
            _rows = Array.Empty<PetMarketRowUI>();
            return;
        }

        _rows = rowsRoot.GetComponentsInChildren<PetMarketRowUI>(true)
            .OrderBy(r => r.transform.GetSiblingIndex())
            .ToArray();
    }

    private void OnEnable()
    {
        if (petService == null) petService = PetNetworkService.Instance;

        if (rotationService != null)
            rotationService.OnMarketPackedUpdated += OnMarketUpdated;

        if (petService != null)
            petService.OnLocalInventoryChanged += RefreshInteractables;

        if (rotationService != null)
        {
            rotationService.ForceRefresh();

            string packed = rotationService.GetCurrentPacked();
            if (!string.IsNullOrWhiteSpace(packed))
            {
                OnMarketUpdated(packed);
            }
        }

        RefreshInteractables();

        StartCoroutine(DelayedRefresh());
    }

    private void OnDisable()
    {
        if (rotationService != null)
            rotationService.OnMarketPackedUpdated -= OnMarketUpdated;

        if (petService != null)
            petService.OnLocalInventoryChanged -= RefreshInteractables;
    }

    private IEnumerator DelayedRefresh()
    {
        yield return new WaitForSeconds(1f);

        if (rotationService != null && _rows != null && _rows.Length > 0)
        {
            if (timerText != null && timerText.text == "--:--")
            {
                Debug.Log("[PetMarketUI] Delayed refresh triggered");
                rotationService.ForceRefresh();

                string packed = rotationService.GetCurrentPacked();
                if (!string.IsNullOrWhiteSpace(packed))
                    OnMarketUpdated(packed);
            }
        }
    }

    private void Update()
    {
        RefreshTimerDisplay();
    }

    private void RefreshTimerDisplay()
    {
        if (!timerText || rotationService == null) return;

        long ts = rotationService.LastMarketTs;

        if (ts <= 0)
        {
            timerText.text = "--:--";

            if (Time.frameCount % 120 == 0 && PhotonNetwork.InRoom)
            {
                rotationService?.ForceRefresh();
            }
            return;
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long remain = rotationService.RotationSeconds - (now - ts);
        if (remain < 0) remain = 0;

        long mm = remain / 60;
        long ss = remain % 60;
        timerText.text = $"{mm:00}:{ss:00}";
    }

    private void OnMarketUpdated(string packed)
    {
        if (petService == null) petService = PetNetworkService.Instance;
        if (petService == null || rotationService == null) return;

        var items = PetMarketRotationService.Unpack(packed);

        for (int i = 0; i < _rows.Length; i++)
        {
            var row = _rows[i];

            if (i >= items.Count)
            {
                SetEmpty(row);
                continue;
            }

            var it = items[i];
            var egg = petService.GetEggDef(it.eggId);

            row.eggId = it.eggId;
            row.qty = it.qty;
            row.price = egg != null ? egg.price : 0;

            if (row.eggNameText) row.eggNameText.text = it.eggId;
            if (row.rarityText) row.rarityText.text = egg != null ? egg.rarity.ToString() : "-";
            if (row.priceText) row.priceText.text = NumberShortener.Format(row.price);
            if (row.qtyText) row.qtyText.text = it.qty.ToString();

            if (row.icon)
            {
                row.icon.enabled = (egg != null && egg.icon != null);
                row.icon.sprite = egg != null ? egg.icon : null;
            }

            bool stockOk = it.qty > 0;
            bool moneyOk = PlayerEconomy.Local == null || PlayerEconomy.Local.CanAfford(row.price);

            row.BindBuy(petService, it.eggId, row.price);
            row.Unlock();

            if (row.buyButton)
                row.buyButton.interactable = stockOk && moneyOk;
        }
    }

    private void RefreshInteractables()
    {
        if (rotationService == null) return;

        string packed = rotationService.GetCurrentPacked();
        if (!string.IsNullOrWhiteSpace(packed))
            OnMarketUpdated(packed);
    }

    private void SetEmpty(PetMarketRowUI row)
    {
        row.eggId = "";
        row.qty = 0;
        row.price = 0;

        if (row.eggNameText) row.eggNameText.text = "-";
        if (row.rarityText) row.rarityText.text = "-";
        if (row.priceText) row.priceText.text = "-";
        if (row.qtyText) row.qtyText.text = "-";

        if (row.icon) { row.icon.enabled = false; row.icon.sprite = null; }

        if (row.buyButton)
        {
            row.buyButton.onClick.RemoveAllListeners();
            row.buyButton.interactable = false;
        }

        row.Unlock();
    }
}