using System.Collections.Generic;
using UnityEngine;

public class PlantVisualSystem : MonoBehaviour
{
    [Header("Refs")]
    public FarmNetwork farmNetwork;

    [Header("Seed Definitions (must include visuals)")]
    public List<SeedDefinition> seedDefs = new();

    [Header("Scene Roots")]
    public Transform farmsRoot;

    [Header("Refresh")]
    [Range(1, 30)] public int refreshPerSecond = 4;

    private Dictionary<string, SeedDefinition> _seedById;
    private readonly Dictionary<(int f, int x, int y), Transform> _anchors = new();
    private readonly Dictionary<(int f, int x, int y), GameObject> _spawnedByPlot = new();
    private readonly Dictionary<(int f, int x, int y), int> _stageCache = new();
    private readonly Dictionary<(int f, int x, int y), float> _lastOccupiedTime = new();
    private readonly List<(int f, int x, int y)> _tmpKeys = new();
    public float emptyGraceSeconds = 0.3f;

    private float _tick;

    private const int FARMS = 8;
    private const int W = 3;
    private const int H = 3;

    private void Awake()
    {
        if (!farmNetwork) farmNetwork = FindFirstObjectByType<FarmNetwork>();

        if (!farmsRoot)
        {
            var go = GameObject.Find("FarmsRoot");
            if (go) farmsRoot = go.transform;
        }

        _seedById = new Dictionary<string, SeedDefinition>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var d in seedDefs)
        {
            if (d == null || string.IsNullOrWhiteSpace(d.seedId)) continue;
            _seedById[d.seedId] = d;
        }

        CacheAnchors();

        Debug.Log($"[PlantVisualSystem] seedDefs count={seedDefs?.Count ?? 0} dictCount={_seedById?.Count ?? 0}");
    }

    private void OnEnable()
    {
        if (farmNetwork != null)
            farmNetwork.OnPlotStateChanged += OnPlotChanged;
    }

    private void OnDisable()
    {
        if (farmNetwork != null)
            farmNetwork.OnPlotStateChanged -= OnPlotChanged;
    }

    private void Start()
    {
        RefreshAll();
    }

    private void Update()
    {
        if (farmNetwork == null) return;

        _tick += Time.deltaTime;
        float interval = 1f / Mathf.Max(1, refreshPerSecond);
        if (_tick < interval) return;
        _tick = 0f;

        _tmpKeys.Clear();
        foreach (var kv in _spawnedByPlot)
            _tmpKeys.Add(kv.Key);

        for (int i = 0; i < _tmpKeys.Count; i++)
        {
            var k = _tmpKeys[i];
            RefreshPlot(k.f, k.x, k.y);
        }
    }

    private void CacheAnchors()
    {
        _anchors.Clear();

        if (!farmsRoot)
        {
            Debug.LogWarning("[PlantVisualSystem] FarmsRoot not found. Visuals won't spawn.");
            return;
        }

        for (int f = 0; f < FARMS; f++)
        {
            var farm = farmsRoot.Find($"Farm_{f}");
            if (!farm) continue;

            var plots = farm.Find("Plots");
            if (!plots) continue;

            for (int x = 0; x < W; x++)
                for (int y = 0; y < H; y++)
                {
                    var plot = plots.Find($"Plot_{x}_{y}");
                    if (!plot) continue;

                    var anchor = plot.Find("PlantAnchor");
                    if (!anchor)
                    {
                        var a = new GameObject("PlantAnchor");
                        a.transform.SetParent(plot, false);
                        a.transform.localPosition = Vector3.zero;
                        a.transform.localRotation = Quaternion.identity;
                        a.transform.localScale = Vector3.one;
                        anchor = a.transform;
                    }

                    _anchors[(f, x, y)] = anchor;
                }
        }
    }

    private void OnPlotChanged(int f, int x, int y)
    {
        RefreshPlot(f, x, y);
    }

    private void RefreshAll()
    {
        for (int f = 0; f < FARMS; f++)
            for (int x = 0; x < W; x++)
                for (int y = 0; y < H; y++)
                    RefreshPlot(f, x, y);
    }

    private void RefreshPlot(int f, int x, int y)
    {
        if (!_anchors.TryGetValue((f, x, y), out var anchor) || anchor == null) return;

        var ps = farmNetwork?.GetPlotState(f, x, y);
        if (ps == null) return;

        if (!ps.occupied || string.IsNullOrWhiteSpace(ps.seedId))
        {
            if (_lastOccupiedTime.TryGetValue((f, x, y), out float lastTime))
            {
                if (Time.time - lastTime < emptyGraceSeconds)
                {
                    return;
                }
            }

            ClearPlot(f, x, y);
            return;
        }

        _lastOccupiedTime[(f, x, y)] = Time.time;

        _seedById.TryGetValue(ps.seedId, out var def);

        float total = ps.growSeconds;
        if (total <= 0f && def != null) total = def.growSeconds;
        if (total <= 0f) total = 30f;

        long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        float elapsed = Mathf.Max(0f, (float)(now - ps.plantUnix));
        float t = Mathf.Clamp01(elapsed / total);

        int stage = GetStageIndex(t);

        if (_stageCache.TryGetValue((f, x, y), out int cachedStage) && cachedStage == stage)
        {
            ApplyOffsetOnly(anchor, def);
            ApplyGrowthScale(f, x, y, def, ps, t);
            ApplyGrowthLift(anchor, def, t);
            return;
        }

        _stageCache[(f, x, y)] = stage;

        GameObject prefab = ChooseStagePrefab(def, stage);
        if (!prefab)
        {
            ClearSpawnOnly(f, x, y);
            ApplyOffsetOnly(anchor, def);
            return;
        }

        ClearSpawnOnly(f, x, y);

        var go = Instantiate(prefab, anchor);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = Vector3.one;

        _spawnedByPlot[(f, x, y)] = go;

        ApplyOffsetOnly(anchor, def);
        ApplyGrowthScale(f, x, y, def, ps, t);
    }

    private void ApplyTransform(Transform anchor, SeedDefinition def)
    {
        if (!anchor) return;

        if (def != null)
        {
            anchor.localPosition = def.localOffset;
            anchor.localScale = def.localScale;
        }
        else
        {
            anchor.localPosition = Vector3.zero;
            anchor.localScale = Vector3.one;
        }
    }

    private int GetStageIndex(float t)
    {
        if (t < 0.33f) return 0;
        if (t < 0.66f) return 1;
        return 2;
    }

    private GameObject ChooseStagePrefab(SeedDefinition def, int stage)
    {
        if (def == null) return null;

        GameObject p0 = def.stage0Prefab;
        GameObject p1 = def.stage1Prefab ? def.stage1Prefab : p0;
        GameObject p2 = def.stage2Prefab ? def.stage2Prefab : (p1 ? p1 : p0);

        return stage switch
        {
            0 => p0,
            1 => p1,
            _ => p2
        };
    }

    private void ApplyOffsetOnly(Transform anchor, SeedDefinition def)
    {
        if (!anchor) return;
        anchor.localPosition = def != null ? def.localOffset : Vector3.zero;
        anchor.localScale = Vector3.one;
    }

    private Transform GetVisualRoot(GameObject plantInstance)
    {
        if (!plantInstance) return null;

        var vr = plantInstance.transform.Find("VisualRoot");
        return vr ? vr : plantInstance.transform;
    }

    private void ApplyGrowthLift(Transform anchor, SeedDefinition def, float t)
    {
        if (!anchor) return;

        float lift = 0.3f;
        if (def != null && def.maxLift > 0f)
            lift = def.maxLift;

        Vector3 pos = anchor.localPosition;
        pos.y = Mathf.Lerp(0f, lift, t);
        anchor.localPosition = pos;
    }

    private void ApplyGrowthScale(int f, int x, int y, SeedDefinition def, PlotState ps, float t)
    {
        if (!_spawnedByPlot.TryGetValue((f, x, y), out var go) || go == null)
            return;

        Transform vr = GetVisualRoot(go);

        float final = 15f;

        if (def != null)
        {
            float minW = def.minWeight;
            float maxW = def.maxWeight;

            if (maxW < minW)
                (minW, maxW) = (maxW, minW);

            float wn = (maxW > minW)
                ? Mathf.InverseLerp(minW, maxW, ps.weight)
                : 0.5f;

            final = Mathf.Lerp(10f, 15f, wn);
        }

        Vector3 targetFinal = new Vector3(final, final, final);
        Vector3 s = Vector3.Lerp(Vector3.one, targetFinal, t);

        Vector3 parentLossy = vr.parent ? vr.parent.lossyScale : Vector3.one;
        parentLossy.x = Mathf.Max(0.0001f, parentLossy.x);
        parentLossy.y = Mathf.Max(0.0001f, parentLossy.y);
        parentLossy.z = Mathf.Max(0.0001f, parentLossy.z);

        Vector3 desiredWorld = s;

        vr.localScale = new Vector3(
            desiredWorld.x / parentLossy.x,
            desiredWorld.y / parentLossy.y,
            desiredWorld.z / parentLossy.z
        );
    }

    private void ClearPlot(int f, int x, int y)
    {
        ClearSpawnOnly(f, x, y);
        _stageCache.Remove((f, x, y));
    }

    private void ClearSpawnOnly(int f, int x, int y)
    {
        if (_spawnedByPlot.TryGetValue((f, x, y), out var old) && old != null)
            Destroy(old);

        _spawnedByPlot.Remove((f, x, y));
    }
}