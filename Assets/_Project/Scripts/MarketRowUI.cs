using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MarketRowUI : MonoBehaviour
{
    public TMP_Text seedText;
    public TMP_Text priceText;
    public TMP_Text qtyText;
    public Image iconImage;
    public Button buyButton;

    [HideInInspector] public string seedId;
    [HideInInspector] public int price;
    [HideInInspector] public int qty;

    private MarketPurchaseService _purchase;
    private string _seedToBuy;
    private int _priceToBuy;

    private bool _busy;
    private float _busyUntil;

    private void Awake()
    {      
        // ÖNEMLÝ: Inspector'dan eklenmiþ listener'larý da override ederiz
        if (buyButton)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
        }
    }

    public void BindBuy(MarketPurchaseService purchase, string seedId, int price)
    {
        _purchase = purchase;
        _seedToBuy = seedId;
        _priceToBuy = price;

        // her bind'de tekrar ekleme yok (Awake'de 1 kere baðlandý)
        _busy = false;
    }

    private void OnBuyClicked()
    {
        if (_purchase == null) return;
        if (string.IsNullOrWhiteSpace(_seedToBuy)) return;

        // Local spam lock (çok hýzlý double click/press)
        if (_busy && Time.time < _busyUntil) return;
        _busy = true;
        _busyUntil = Time.time + 0.25f;

        // UI-side money check (master zaten kontrol ediyor)
        if (PlayerEconomy.Local != null && !PlayerEconomy.Local.CanAfford(_priceToBuy))
        {
            _busy = false;
            return;
        }

        _purchase.RequestBuy(_seedToBuy);

        // 250ms sonra tekrar aç
        // (istersen OnBuyResultLocal event'i ile daha düzgün açarýz)
        Invoke(nameof(Unlock), 0.25f);
    }

    private void Unlock()
    {
        _busy = false;
    }
}