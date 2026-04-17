using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkBootstrap : MonoBehaviourPunCallbacks
{
    public static NetworkBootstrap Instance { get; private set; }

    public event Action OnConnectedAndReady;
    public event Action<DisconnectCause> OnLostConnection;

    private bool _connecting;
    private float _heartbeatTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Update()
    {
        // YENÝ: Her 60 saniyede bir heartbeat gönder
        _heartbeatTimer += Time.deltaTime;
        if (_heartbeatTimer >= 10)
        {
            _heartbeatTimer = 0f;
            PlayFabSessionValidator.SendHeartbeat();
        }
    }

    public void Connect()
    {
        if (_connecting) return;

        if (PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.InLobby) { OnConnectedAndReady?.Invoke(); return; }
            PhotonNetwork.JoinLobby();
            return;
        }

        _connecting = true;
        Debug.Log("[Bootstrap] Connecting...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[Bootstrap] Connected to Master.");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[Bootstrap] Joined Lobby.");
        _connecting = false;
        OnConnectedAndReady?.Invoke();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("[Bootstrap] Disconnected: " + cause);
        _connecting = false;

        // YENÝ: Baðlantý koptuðunda offline iþaretle
        PlayFabSessionValidator.MarkOffline();

        OnLostConnection?.Invoke(cause);
    }
}