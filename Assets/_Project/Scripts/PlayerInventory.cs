using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Local { get; private set; }

    private List<InventoryItem> _items = new List<InventoryItem>();

    public event System.Action OnChanged;

    private void Awake()
    {
        Local = this;
    }

    public void ClearAllLocal()
    {
        _items.Clear();
        OnChanged?.Invoke();
    }

    // BU METOD EKLENDI
    public void ClearAllFromSave()
    {
        _items.Clear();
        OnChanged?.Invoke();
    }

    public void LocalAdd(InventoryItem item)
    {
        if (item == null) return;
        _items.Add(item);
        OnChanged?.Invoke();
    }

    public void LocalRemove(int uid)
    {
        _items.RemoveAll(x => x.uid == uid);
        OnChanged?.Invoke();
    }

    public void NotifyChangedFromSave()
    {
        OnChanged?.Invoke();
    }

    public List<InventoryItem> GetAll()
    {
        return new List<InventoryItem>(_items);
    }
}