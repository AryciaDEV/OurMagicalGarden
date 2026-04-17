using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SeedBarUI : MonoBehaviour
{
    [Header("UI")]
    public RectTransform slotsRoot;       // Content
    public SeedBarSlotUI slotPrefab;      // SeedBarSlotPrefab

    [Header("Seed Definitions (market order + icons)")]
    public List<SeedDefinition> seedDefs = new();

    private Dictionary<string, SeedDefinition> _seedById;
    private Dictionary<string, int> _orderIndex;

    // seedId -> slot instance
    private readonly Dictionary<string, SeedBarSlotUI> _slots =
        new(System.StringComparer.OrdinalIgnoreCase);

    private PlayerSeedBag _boundBag;
    private bool _dirty;

    private void Awake()
    {
        _seedById = new Dictionary<string, SeedDefinition>(System.StringComparer.OrdinalIgnoreCase);
        _orderIndex = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < seedDefs.Count; i++)
        {
            var d = seedDefs[i];
            if (d == null || string.IsNullOrWhiteSpace(d.seedId)) continue;

            _seedById[d.seedId] = d;
            _orderIndex[d.seedId] = i;
        }
    }

    private void OnEnable()
    {
        TryRebind();
        MarkDirty();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void Update()
    {
        TryRebind();
    }

    private void LateUpdate()
    {
        if (!_dirty) return;
        _dirty = false;
        RefreshNow();
    }

    private void TryRebind()
    {
        var bag = PlayerSeedBag.Local;
        if (bag == null) return;
        if (_boundBag == bag) return;

        Unbind();
        _boundBag = bag;
        _boundBag.OnChanged += MarkDirty;

        MarkDirty();
    }

    private void Unbind()
    {
        if (_boundBag != null)
            _boundBag.OnChanged -= MarkDirty;
        _boundBag = null;
    }

    private void MarkDirty()
    {
        _dirty = true;
    }

    private void RefreshNow()
    {
        if (!slotsRoot || !slotPrefab) return;
        var bag = PlayerSeedBag.Local;
        if (bag == null) return;

        // sadece qty>0 olanlar
        var wanted = bag.GetAll()
            .Where(kv => kv.Value > 0)
            .Select(kv => (seedId: kv.Key, qty: kv.Value))
            .ToList();

        // 1) create/update
        foreach (var w in wanted)
        {
            if (!_slots.TryGetValue(w.seedId, out var slot) || slot == null)
            {
                slot = Instantiate(slotPrefab, slotsRoot);
                slot.seedId = w.seedId;

                if (slot.selectButton)
                {
                    string captured = w.seedId;
                    slot.selectButton.onClick.RemoveAllListeners();
                    slot.selectButton.onClick.AddListener(() =>
                    {
                        var b = PlayerSeedBag.Local;
                        if (b == null) return;
                        b.SelectSeed(captured);
                    });
                }

                _slots[w.seedId] = slot;
            }

            if (slot.nameText) slot.nameText.text = w.seedId;
            if (slot.qtyText) slot.qtyText.text = w.qty.ToString();

            if (slot.iconImage)
            {
                if (_seedById.TryGetValue(w.seedId, out var def) && def != null && def.icon != null)
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
        }

        // 2) remove 0 olanlar
        var wantedSet = new HashSet<string>(wanted.Select(x => x.seedId), System.StringComparer.OrdinalIgnoreCase);
        var removeKeys = _slots.Keys.Where(k => !wantedSet.Contains(k)).ToList();

        foreach (var k in removeKeys)
        {
            if (_slots.TryGetValue(k, out var s) && s != null)
                Destroy(s.gameObject);
            _slots.Remove(k);
        }

        // 3) order
        var ordered = _slots.Values
            .Where(s => s != null)
            .OrderBy(s => _orderIndex.TryGetValue(s.seedId, out int idx) ? idx : 9999)
            .ToList();

        for (int i = 0; i < ordered.Count; i++)
            ordered[i].transform.SetSiblingIndex(i);

        // 4) selected outline
        string selected = bag.SelectedSeedId;
        foreach (var s in _slots.Values)
        {
            if (!s) continue;
            bool isSel = !string.IsNullOrWhiteSpace(selected) &&
                         string.Equals(selected, s.seedId, System.StringComparison.OrdinalIgnoreCase);
            s.SetSelected(isSel);
        }

        // 5) layout rebuild (invisible fix)
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(slotsRoot);
    }
}