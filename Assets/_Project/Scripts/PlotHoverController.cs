using Photon.Pun;
using UnityEngine;

public class PlotHoverController : MonoBehaviourPun
{
    [Header("Refs")]
    public Camera cam;
    public FarmNetwork farmNetwork;
    public PlotHoverUI ui;

    [Header("Ad Speedup")]
    public PlotAdSpeedupService plotAdSpeedupService;

    [Header("Raycast")]
    public float maxDistance = 25f;
    public LayerMask plotMask = ~0;

    [Header("UI Follow")]
    public Vector3 worldOffset = new Vector3(0, 0.6f, 0);

    private PlotInteract _currentPlot;
    private int _cf, _cx, _cy;
    private bool _initialized = false;

    private void Start()
    {
        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }

        if (!cam) cam = Camera.main;
        if (!farmNetwork) farmNetwork = FindFirstObjectByType<FarmNetwork>();

        ui?.SetActive(false);

        if (!ui)
            ui = FindFirstObjectByType<PlotHoverUI>(FindObjectsInactive.Include);

        if (!plotAdSpeedupService)
            plotAdSpeedupService = FindFirstObjectByType<PlotAdSpeedupService>(FindObjectsInactive.Include);

        _initialized = true;

        Debug.Log("[PlotHoverController] Initialized");
    }

    private void Update()
    {
        if (!_initialized || !cam || !farmNetwork || ui == null) return;

        var hitPlot = RaycastPlot(out RaycastHit hit);

        if (hitPlot == null)
        {
            _currentPlot = null;
            ui.SetActive(false);
            plotAdSpeedupService?.Hide();
            return;
        }

        _currentPlot = hitPlot;
        _cx = hitPlot.x;
        _cy = hitPlot.y;

        // Farm index'i PlotInteract'ten al
        _cf = hitPlot.GetFarmIndex();

        if (_cf < 0)
        {
            Debug.LogError($"[PlotHoverController] Invalid farm index from plot!");
            ui.SetActive(false);
            return;
        }

        // YETKI KONTROLU
        if (!FarmAuth.CanEditFarm(_cf))
        {
            ui.SetActive(false);
            plotAdSpeedupService?.Hide();
            return;
        }

        Vector3 wpos = hitPlot.transform.position + worldOffset;
        Vector3 spos = cam.WorldToScreenPoint(wpos);

        if (spos.z < 0.1f)
        {
            ui.SetActive(false);
            plotAdSpeedupService?.Hide();
            return;
        }

        ui.panel.position = spos;
        ui.SetActive(true);

        var ps = farmNetwork.GetPlotState(_cf, _cx, _cy);

        if (ps == null)
        {
            Debug.LogError($"[PlotHoverController] Null plot state at ({_cf},{_cx},{_cy})!");
            return;
        }

        if (!ps.occupied)
        {
            plotAdSpeedupService?.Hide();

            string seed = PlayerSeedBag.Local != null ? PlayerSeedBag.Local.SelectedSeedId : "carrot";
            ui.titleText.text = "Empty";
            ui.infoText.text = $"Planting: {seed}";

            SetupActionButton("Plant", () =>
            {
                TryInteract(seed);
            });

            if (Input.GetKeyDown(KeyCode.E))
                TryInteract(seed);

            return;
        }

        bool hasTimer = farmNetwork.TryGetRemainingSeconds(_cf, _cx, _cy, out int rem, out bool ready);

        string planted = string.IsNullOrWhiteSpace(ps.seedId) ? "Seed" : ps.seedId;
        ui.titleText.text = $"{planted}";

        if (!hasTimer)
        {
            plotAdSpeedupService?.Hide();
            ui.infoText.text = "Growing...";
            ui.actionButton.gameObject.SetActive(false);
            return;
        }

        if (ready)
        {
            plotAdSpeedupService?.Hide();
            ui.infoText.text = "Ready to Harvest!";
            SetupActionButton("Harvest", () =>
            {
                TryInteract(planted);
            });

            if (Input.GetKeyDown(KeyCode.E))
                TryInteract(planted);
        }
        else
        {
            ui.infoText.text = $"Time: {FormatMMSS(rem)}";
            ui.actionButton.gameObject.SetActive(false);
            plotAdSpeedupService?.SetTargetPlot(_cf, _cx, _cy);
            plotAdSpeedupService?.Refresh();
        }
    }

    private void TryInteract(string seedId)
    {
        if (_currentPlot == null)
        {
            Debug.LogWarning("[PlotHoverController] TryInteract: No current plot!");
            return;
        }

        // TEKRAR YETKI KONTROLU
        if (!FarmAuth.CanEditFarm(_cf))
        {
            Debug.LogWarning($"[PlotHoverController] TryInteract: Cannot edit farm {_cf}!");
            return;
        }

        Debug.Log($"[PlotHoverController] Interacting with farm={_cf}, plot=({_cx},{_cy}), seed={seedId}");
        farmNetwork.InteractPlot(_cf, _cx, _cy, seedId);
    }

    private PlotInteract RaycastPlot(out RaycastHit hit)
    {
        hit = default;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out hit, maxDistance, plotMask, QueryTriggerInteraction.Ignore))
        {
            var plot = hit.collider.GetComponentInParent<PlotInteract>();
            if (plot == null)
            {
                plot = hit.collider.GetComponent<PlotInteract>();
            }
            return plot;
        }

        return null;
    }

    private void SetupActionButton(string text, System.Action onClick)
    {
        if (!ui.actionButton) return;

        ui.actionButton.gameObject.SetActive(true);
        if (ui.actionButtonText) ui.actionButtonText.text = text;

        ui.actionButton.onClick.RemoveAllListeners();
        ui.actionButton.onClick.AddListener(() => onClick?.Invoke());
    }

    private string FormatMMSS(int sec)
    {
        int mm = sec / 60;
        int ss = sec % 60;
        return $"{mm:00}:{ss:00}";
    }
}