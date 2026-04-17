using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class PlayerPetInventory : MonoBehaviourPun
{
    public static PlayerPetInventory Local { get; private set; }

    private readonly Dictionary<int, PetItem> _items = new();
    public event Action OnChanged;

    public int EquippedUid { get; private set; } = 0;

    private void Awake()
    {
        if (photonView.IsMine)
        {
            Local = this;
        }
        else
        {
            enabled = false;
        }
    }

    public void ClearAllFromSave()
    {
        _items.Clear();
        EquippedUid = 0;
        OnChanged?.Invoke();
    }

    public void NotifyChangedFromSave()
    {
        OnChanged?.Invoke();
    }

    public IReadOnlyCollection<PetItem> GetAll() => _items.Values;

    public PetItem GetByUid(int uid) => _items.TryGetValue(uid, out var it) ? it : null;

    public void LocalAdd(PetItem it)
    {
        if (it == null) return;
        _items[it.uid] = it;

        // ilk pet geldiyse otomatik equip istemiyorsan bunu kaldýr
        if (EquippedUid == 0)
            EquippedUid = it.uid;

        OnChanged?.Invoke();
    }

    public void LocalRemove(int uid)
    {
        _items.Remove(uid);
        if (EquippedUid == uid) EquippedUid = 0;
        OnChanged?.Invoke();
    }

    public void LocalSetEquipped(int uid)
    {
        EquippedUid = uid;
        OnChanged?.Invoke();
    }
}