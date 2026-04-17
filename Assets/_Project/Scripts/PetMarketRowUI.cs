using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PetMarketRowUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text eggNameText;
    public TMP_Text rarityText;
    public TMP_Text priceText;
    public TMP_Text qtyText;
    public Button buyButton;

    [HideInInspector] public string eggId;
    [HideInInspector] public int price;
    [HideInInspector] public int qty;

    private bool _locked;

    public void BindBuy(PetNetworkService svc, string eggIdToBuy, int priceToBuy)
    {
        if (!buyButton) return;

        eggId = eggIdToBuy;
        price = priceToBuy;

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() =>
        {
            if (_locked) return;

            // local check
            if (PlayerEconomy.Local != null && !PlayerEconomy.Local.CanAfford(priceToBuy))
                return;

            _locked = true;
            buyButton.interactable = false;

            svc.RequestBuyEgg(eggIdToBuy);
        });
    }

    public void Unlock()
    {
        _locked = false;
    }
}