using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class LeaderboardUI : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    public Transform rowsRoot;
    public LeaderboardRowUI rowPrefab;

    [Header("Refresh")]
    [SerializeField] private float refreshInterval = 1f;

    private readonly List<LeaderboardRowUI> _spawned = new();
    private float _timer;

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= refreshInterval)
        {
            _timer = 0f;
            Refresh();
        }
    }

    public override void OnJoinedRoom()
    {
        Refresh();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Refresh();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Refresh();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps == null) return;

        if (changedProps.ContainsKey("coins"))
            Refresh();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (!PhotonNetwork.InRoom) return;
        if (!rowsRoot || !rowPrefab) return;

        // temizle
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null)
                Destroy(_spawned[i].gameObject);
        }
        _spawned.Clear();

        // ? En saðlam kaynak: CurrentRoom.Players
        var players = PhotonNetwork.CurrentRoom.Players.Values
            .OrderByDescending(GetCoins)
            .ThenBy(p => p.NickName)
            .ToList();

        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            var row = Instantiate(rowPrefab, rowsRoot);
            row.gameObject.SetActive(true);

            row.Bind(
                i + 1,
                string.IsNullOrWhiteSpace(p.NickName) ? $"Player {p.ActorNumber}" : p.NickName,
                GetCoins(p)
            );

            _spawned.Add(row);
        }
    }

    private int GetCoins(Player p)
    {
        if (p != null && p.CustomProperties != null && p.CustomProperties.TryGetValue("coins", out object v))
        {
            if (v is int i) return i;
            if (v is long l) return (int)l;
        }
        return 0;
    }
}