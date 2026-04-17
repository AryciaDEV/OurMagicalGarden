using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

public class PlayerSeedBag : MonoBehaviourPun
{
    public static PlayerSeedBag Local { get; private set; }
    public AudioClip myAudioClip;

    // seedId -> qty (qty=0 ise dictionary'de tutulmaz)
    private readonly Dictionary<string, int> _seeds = new(StringComparer.OrdinalIgnoreCase);

    public event Action OnChanged;

    public string SelectedSeedId { get; private set; } = "";

    private void Awake()
    {
        if (photonView.IsMine)
            Local = this;
    }

    private void Start()
    {
        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }

        // Baþlangýç seed verme istemiyorsan kapalý býrak:
        // AddSeed("Carrot", 5);

        AutoSelectIfNeeded(force: true);
        OnChanged?.Invoke();
    }

    public void ClearAllFromSave()
    {
        _seeds.Clear();
        SelectedSeedId = "";
        OnChanged?.Invoke();
    }

    public IReadOnlyDictionary<string, int> GetAll()
    {
        return _seeds;
    }

    public int GetCount(string seedId)
        => _seeds.TryGetValue(seedId, out int c) ? c : 0;

    public bool HasAnySeed() => _seeds.Count > 0;

    public void AddSeed(string seedId, int amount)
    {
        if (string.IsNullOrWhiteSpace(seedId) || amount <= 0) return;

        _seeds.TryGetValue(seedId, out int cur);
        _seeds[seedId] = cur + amount;

        // ilk seed geldiyse otomatik seç
        if (string.IsNullOrWhiteSpace(SelectedSeedId))
            SelectedSeedId = seedId;

        OnChanged?.Invoke();
    }

    public bool TrySpendSeed(string seedId, int amount)
    {
        if (string.IsNullOrWhiteSpace(seedId)) return false;
        if (amount <= 0) return true;

        if (!_seeds.TryGetValue(seedId, out int current)) return false;
        if (current < amount) return false;

        current -= amount;

        if (current <= 0)
            _seeds.Remove(seedId);
        else
            _seeds[seedId] = current;

        // selected bittiyse baþka seç
        if (string.Equals(SelectedSeedId, seedId, StringComparison.OrdinalIgnoreCase))
            AutoSelectIfNeeded(force: true);

        OnChanged?.Invoke();
        return true;
    }

    public void SelectSeed(string seedId)
    {
        if (string.IsNullOrWhiteSpace(seedId)) return;
        if (GetCount(seedId) <= 0) return;

        SoundFXManager.Instance.PlaySound(myAudioClip);

        SelectedSeedId = seedId;
        OnChanged?.Invoke();
    }

    private void AutoSelectIfNeeded(bool force)
    {
        if (!force &&
            !string.IsNullOrWhiteSpace(SelectedSeedId) &&
            GetCount(SelectedSeedId) > 0)
            return;

        SelectedSeedId = _seeds.Count > 0 ? _seeds.Keys.First() : "";
    }
}