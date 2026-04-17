using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject playerPrefab;

    private bool _spawned;
    private FarmSpawnPoints _spawns;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        _spawns = FindFirstObjectByType<FarmSpawnPoints>();
        TrySpawn();
    }

    private void Update()
    {
        if (!_spawned)
            TrySpawn();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _spawns = FindFirstObjectByType<FarmSpawnPoints>();
        TrySpawn();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (targetPlayer != PhotonNetwork.LocalPlayer) return;

        if (changedProps != null && changedProps.ContainsKey(FarmAssignmentService.PROP_FARM))
        {
            Debug.Log("[PlayerSpawner] Farm assignment arrived (callback) -> TrySpawn()");
            TrySpawn();
        }
    }

    private void TrySpawn()
    {
        if (_spawned) return;
        if (!PhotonNetwork.InRoom) return;

        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] Player prefab missing");
            return;
        }

        if (_spawns == null)
        {
            Debug.LogError("[PlayerSpawner] FarmSpawnPoints not found in scene");
            return;
        }

        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(FarmAssignmentService.PROP_FARM))
        {
            return;
        }

        int farmIndex = (int)PhotonNetwork.LocalPlayer.CustomProperties[FarmAssignmentService.PROP_FARM];
        Transform spawn = _spawns.GetSpawn(farmIndex);

        if (spawn == null)
        {
            Debug.LogError("[PlayerSpawner] Spawn not set for farm " + farmIndex);
            return;
        }

        Debug.Log($"[PlayerSpawner] Local Actor={PhotonNetwork.LocalPlayer.ActorNumber} farm={farmIndex} spawning...");

        var go = PhotonNetwork.Instantiate(playerPrefab.name, spawn.position, spawn.rotation);

        var view = go.GetComponent<PhotonView>();
        Debug.Log($"[PlayerSpawner] Spawned local player: {go.name} view={view?.ViewID}");

        _spawned = true;
    }
}