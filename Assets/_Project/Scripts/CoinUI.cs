using TMPro;
using UnityEngine;

public class CoinUI : MonoBehaviour
{
    public static CoinUI Instance;

    [SerializeField] private TMP_Text coinText;

    private void Awake()
    {
        Instance = this;
        if (!coinText) coinText = GetComponentInChildren<TMP_Text>(true);
    }

    public void SetCoins(int amount)
    {
        if (coinText != null)
            coinText.text = NumberShortener.Format(amount);
    }
}