using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlotAdSpeedupService : MonoBehaviour
{
    [Header("Refs")]
    public FarmNetwork farmNetwork;
    public Button watchAdButton;
    public TMP_Text infoText;

    private int _farmIndex = -1;
    private int _x = -1;
    private int _y = -1;
    private bool _hasTarget;

    private void Start()
    {
        if (!farmNetwork) farmNetwork = FindFirstObjectByType<FarmNetwork>();

        if (watchAdButton)
        {
            watchAdButton.onClick.RemoveAllListeners();
            watchAdButton.onClick.AddListener(OnWatchClicked);
        }

        Hide();
    }

    public void SetTargetPlot(int farmIndex, int x, int y)
    {
        _farmIndex = farmIndex;
        _x = x;
        _y = y;
        _hasTarget = true;
        Refresh();
    }

    public void Hide()
    {
        _hasTarget = false;

        if (watchAdButton) watchAdButton.gameObject.SetActive(false);
        if (infoText) infoText.gameObject.SetActive(false);
    }

    public void Refresh()
    {
        if (!_hasTarget || farmNetwork == null)
        {
            Hide();
            return;
        }

        var ps = farmNetwork.GetPlotState(_farmIndex, _x, _y);
        if (ps == null || !ps.occupied)
        {
            Hide();
            return;
        }

        if (watchAdButton) watchAdButton.gameObject.SetActive(true);
        if (infoText) infoText.gameObject.SetActive(true);

        bool ok = farmNetwork.TryGetRemainingSeconds(_farmIndex, _x, _y, out int rem, out bool ready);

        if (!ok || ready || rem <= 1)
        {
            if (watchAdButton) watchAdButton.interactable = false;
            if (infoText) infoText.text = "This crop is already ready.";
            return;
        }

        int halved = Mathf.CeilToInt(rem * 0.5f);

        if (watchAdButton)
            watchAdButton.interactable = PokiAdsService.Instance != null && !PokiAdsService.Instance.IsAdRunning;

        if (infoText)
            infoText.text = $"Watch ad: reduce remaining time from {rem}s to {halved}s";
    }

    private void OnWatchClicked()
    {
        if (!_hasTarget) return;
        if (farmNetwork == null) return;
        if (PokiAdsService.Instance == null) return;

        if (watchAdButton) watchAdButton.interactable = false;

        PokiAdsService.Instance.ShowRewarded(success =>
        {
            if (success)
            {
                farmNetwork.RequestHalveRemainingGrowTime(_farmIndex, _x, _y);
                Debug.Log($"[AdReward] Plot speed-up granted ({_farmIndex},{_x},{_y})");
            }
            else
            {
                Debug.Log("[AdReward] Plot speed-up ad failed/skipped.");
            }

            Refresh();
        });
    }


    //TEST -- SÝLÝNECEK!!!!
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha8))
        {
            this.gameObject.SetActive(true);
        }
    }
}