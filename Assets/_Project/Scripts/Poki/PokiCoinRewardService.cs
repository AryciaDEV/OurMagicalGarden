using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PokiCoinRewardService : MonoBehaviour
{
    [Header("UI")]
    public GameObject rewardPanel;
    public TMP_Text infoText;
    public TMP_Text cooldownText;
    public Button watchAdButton;

    [Header("Config")]
    public float cooldownSeconds = 300f; // 5 dk
    public int maxRewardCoins = 50000;   // güvenlik/dengeli ekonomi için tavan

    private float _remaining;
    private bool _ready;

    private void Start()
    {
        _remaining = cooldownSeconds;
        _ready = false;

        if (watchAdButton)
        {
            watchAdButton.onClick.RemoveAllListeners();
            watchAdButton.onClick.AddListener(OnWatchAdClicked);
        }

        RefreshUI();
    }

    private void Update()
    {
        if (!_ready)
        {
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                _remaining = 0f;
                _ready = true;
            }
        }

        RefreshUI();

        //TEST ÝÇÝN  -- SÝLÝNECEKKK!!!!!!
        if(Input.GetKeyDown(KeyCode.Alpha9))
        {
            rewardPanel.SetActive(true);
        }
    }

    private int CalculateReward(int currentCoins)
    {
        if (currentCoins <= 0)
            return 250; // tamamen fakirse baþlangýç desteði

        float multiplier;

        if (currentCoins < 1_000)
            multiplier = 5f;
        else if (currentCoins < 10_000)
            multiplier = 3f;
        else if (currentCoins < 100_000)
            multiplier = 1.5f;
        else
            multiplier = 0.5f;

        int reward = Mathf.RoundToInt(currentCoins * multiplier);
        reward = Mathf.Clamp(reward, 0, maxRewardCoins);
        return reward;
    }

    private void RefreshUI()
    {
        int currentCoins = PlayerEconomy.Local != null ? PlayerEconomy.Local.Coins : 0;
        int rewardCoins = CalculateReward(currentCoins);

        if (rewardPanel) rewardPanel.SetActive(true);

        if (infoText)
            infoText.text = _ready
                ? $"Watch ad: get {NumberShortener.Format(rewardCoins)} coins"
                : "Next ad reward is charging...";

        if (cooldownText)
        {
            if (_ready) cooldownText.text = "Ready";
            else
            {
                int sec = Mathf.CeilToInt(_remaining);
                int mm = sec / 60;
                int ss = sec % 60;
                cooldownText.text = $"{mm:00}:{ss:00}";
            }
        }

        if (watchAdButton)
            watchAdButton.interactable = _ready &&
                                         PokiAdsService.Instance != null &&
                                         !PokiAdsService.Instance.IsAdRunning;
    }

    private void OnWatchAdClicked()
    {
        if (!_ready) return;
        if (PlayerEconomy.Local == null) return;
        if (PokiAdsService.Instance == null) return;

        int rewardCoins = CalculateReward(PlayerEconomy.Local.Coins);
        if (rewardCoins <= 0) return;

        if (watchAdButton) watchAdButton.interactable = false;

        PokiAdsService.Instance.ShowRewarded(success =>
        {
            if (success)
            {
                PlayerEconomy.Local.AddCoins(rewardCoins);
                Debug.Log($"[AdReward] Coin reward granted: +{rewardCoins}");

                _ready = false;
                _remaining = cooldownSeconds;
            }
            else
            {
                Debug.Log("[AdReward] Coin ad failed/skipped.");
            }

            RefreshUI();
        });
    }
}