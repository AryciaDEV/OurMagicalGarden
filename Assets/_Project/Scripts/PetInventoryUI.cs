using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PetInventoryUI : MonoBehaviour
{
    [Header("UI")]
    public Transform rowsRoot;
    public PetInventoryRowUI rowPrefab;

    [Header("Services")]
    public PetNetworkService petService;

    private readonly Dictionary<int, PetInventoryRowUI> _rowsByUid = new();
    private PlayerPetInventory _bound;
    private bool _dirty;

    public AudioClip myAudioClipEquip;
    public AudioClip myAudioClipSell;

    private void OnEnable()
    {
        TryBind();
        MarkDirty();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void Update()
    {
        TryBind();

        if (_dirty)
        {
            _dirty = false;
            RefreshNow();
        }
    }

    private void TryBind()
    {
        var inv = PlayerPetInventory.Local;
        if (inv == null) return;
        if (_bound == inv) return;

        Unbind();
        _bound = inv;
        _bound.OnChanged += MarkDirty;
        MarkDirty();
    }

    private void Unbind()
    {
        if (_bound != null)
            _bound.OnChanged -= MarkDirty;

        _bound = null;
    }

    private void MarkDirty()
    {
        _dirty = true;
    }

    private void RefreshNow()
    {
        if (!rowsRoot || !rowPrefab) return;
        if (petService == null) petService = PetNetworkService.Instance;
        if (petService == null) return;

        var inv = PlayerPetInventory.Local;
        if (inv == null) return;

        var items = inv.GetAll()
            .Where(x => x != null)
            .OrderBy(x => x.uid)
            .ToList();

        // 1) Gerekli satýrlarý oluþtur / güncelle
        foreach (var it in items)
        {
            if (!_rowsByUid.TryGetValue(it.uid, out var row) || row == null)
            {
                row = Instantiate(rowPrefab, rowsRoot);
                row.gameObject.SetActive(true); // KRÝTÝK
                _rowsByUid[it.uid] = row;
            }
            else
            {
                row.gameObject.SetActive(true); // KRÝTÝK
            }

            row.uid = it.uid;
            row.petId = it.petId;
            row.eggId = it.eggId;

            var petDef = petService.GetPetDef(it.petId);
            var eggDef = petService.GetEggDef(it.eggId);

            if (row.petNameText)
                row.petNameText.text = (petDef != null && !string.IsNullOrWhiteSpace(petDef.displayName))
                    ? petDef.displayName
                    : it.petId;

            if (row.bonusText)
                row.bonusText.text = petDef != null ? petDef.GetBonusLineSingleText() : "-";

            if (row.eggRarityText)
                row.eggRarityText.text = eggDef != null ? eggDef.rarity.ToString() : "-";

            if (row.icon)
            {
                row.icon.enabled = (petDef != null && petDef.icon != null);
                row.icon.sprite = petDef != null ? petDef.icon : null;
            }

            if (row.equipButton)
            {
                row.equipButton.onClick.RemoveAllListeners();
                int capturedUid = it.uid;
                row.equipButton.onClick.AddListener(() =>
                {
                    petService.RequestEquip(capturedUid);
                    SoundFXManager.Instance.PlaySound(myAudioClipEquip);
                });
            }

            if (row.sellButton)
            {
                row.sellButton.onClick.RemoveAllListeners();
                int capturedUid = it.uid;
                row.sellButton.onClick.AddListener(() =>
                {
                    petService.RequestSell(capturedUid);
                    SoundFXManager.Instance.PlaySound(myAudioClipSell);
                });
            }
        }

        // 2) Envanterde olmayan satýrlarý sil
        var uidSet = new HashSet<int>(items.Select(x => x.uid));
        var toRemove = _rowsByUid.Keys.Where(uid => !uidSet.Contains(uid)).ToList();

        foreach (var uid in toRemove)
        {
            if (_rowsByUid.TryGetValue(uid, out var row) && row != null)
                Destroy(row.gameObject);

            _rowsByUid.Remove(uid);
        }

        // 3) Sýralama
        int index = 0;
        foreach (var it in items)
        {
            if (_rowsByUid.TryGetValue(it.uid, out var row) && row != null)
                row.transform.SetSiblingIndex(index++);
        }

        /*
        // 4) Selected outline
        int equipped = inv.EquippedUid;
        foreach (var kv in _rowsByUid)
        {
            if (kv.Value == null) continue;
            kv.Value.SetSelected(kv.Key == equipped);
        }
        */

        // 5) Layout rebuild
        var rect = rowsRoot as RectTransform;
        if (rect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
    }
}