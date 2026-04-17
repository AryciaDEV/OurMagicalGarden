using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public enum PanelType
{
    PetMarket,
    PetInventory,
    SeedMarket,
    SeedInventory,
    Leaderboard,
    Reward
}

public class PanelManager : MonoBehaviourPunCallbacks
{
    public static PanelManager Instance { get; private set; }

    [System.Serializable]
    public struct PanelEntry
    {
        public PanelType type;
        public GameObject panel;
        public bool startActive;
    }

    [Header("Panels")]
    [SerializeField] private List<PanelEntry> panels = new List<PanelEntry>();

    // Hangi paneller açýk takibi
    private Dictionary<PanelType, bool> _panelStates = new Dictionary<PanelType, bool>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Panel durumlarýný baþlat
        foreach (var entry in panels)
        {
            if (entry.panel != null)
            {
                entry.panel.SetActive(entry.startActive);
                _panelStates[entry.type] = entry.startActive;
            }
        }
    }

    public void OpenPanel(PanelType type)
    {
        // Sadece local'de aç - RPC gerekmez
        SetPanelState(type, true);
    }

    public void ClosePanel(PanelType type)
    {
        SetPanelState(type, false);
    }

    public void TogglePanel(PanelType type)
    {
        bool currentState = GetPanelState(type);
        SetPanelState(type, !currentState);
    }

    private void SetPanelState(PanelType type, bool active)
    {
        foreach (var entry in panels)
        {
            if (entry.type == type && entry.panel != null)
            {
                entry.panel.SetActive(active);
                _panelStates[type] = active;

                Debug.Log($"[PanelManager] Panel {type} {(active ? "opened" : "closed")}");
                break;
            }
        }
    }

    private bool GetPanelState(PanelType type)
    {
        return _panelStates.ContainsKey(type) && _panelStates[type];
    }

    // Tüm panelleri kapat
    public void CloseAllPanels()
    {
        foreach (var entry in panels)
        {
            if (entry.panel != null)
            {
                entry.panel.SetActive(false);
                _panelStates[entry.type] = false;
            }
        }
    }
}