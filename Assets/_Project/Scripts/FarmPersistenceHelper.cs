using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections.Generic;
using ExitGames.Client.Photon;

public class FarmPersistenceHelper : MonoBehaviourPunCallbacks
{
    public static FarmPersistenceHelper Instance { get; private set; }

    private const string FARM_MAPPINGS_KEY = "farmMappings_v2";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log("[FarmPersistenceHelper] New master, restoring farm mappings...");

        var room = PhotonNetwork.CurrentRoom;
        if (room?.CustomProperties == null) return;

        if (room.CustomProperties.TryGetValue(FARM_MAPPINGS_KEY, out object mappingsObj))
        {
            string json = mappingsObj as string;
            if (!string.IsNullOrEmpty(json))
            {
                var mappings = JsonUtility.FromJson<FarmMappingWrapper>(json);
                if (mappings?.mappings != null)
                {
                    var farmNetwork = FindFirstObjectByType<FarmNetwork>();
                    if (farmNetwork != null)
                    {
                        foreach (var mapping in mappings.mappings)
                        {
                            if (!string.IsNullOrEmpty(mapping.playFabId))
                            {
                                var field = farmNetwork.GetType().GetField("_playFabIdToFarm",
                                    System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);

                                if (field != null)
                                {
                                    var dict = field.GetValue(farmNetwork) as Dictionary<string, int>;

                                    // DÜZELTÝLMÝÞ: Null kontrolü sonra atama
                                    if (dict != null)
                                    {
                                        dict[mapping.playFabId] = mapping.farmIndex;
                                    }
                                }
                            }
                        }
                    }
                    Debug.Log($"[FarmPersistenceHelper] Restored {mappings.mappings.Count} farm mappings");
                }
            }
        }
    }

    public void SaveFarmMappings(Dictionary<string, int> mappings)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (mappings == null || mappings.Count == 0) return;

        var wrapper = new FarmMappingWrapper();
        foreach (var kvp in mappings)
        {
            wrapper.mappings.Add(new FarmMapping
            {
                playFabId = kvp.Key,
                farmIndex = kvp.Value
            });
        }

        string json = JsonUtility.ToJson(wrapper);

        var room = PhotonNetwork.CurrentRoom;
        if (room != null)
        {
            room.SetCustomProperties(new Hashtable
            {
                { FARM_MAPPINGS_KEY, json }
            });
        }
    }

    [System.Serializable]
    private class FarmMappingWrapper
    {
        public List<FarmMapping> mappings = new List<FarmMapping>();
    }

    [System.Serializable]
    private class FarmMapping
    {
        public string playFabId;
        public int farmIndex;
    }
}