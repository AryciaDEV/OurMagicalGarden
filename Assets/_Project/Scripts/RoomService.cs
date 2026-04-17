using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class RoomService : MonoBehaviourPunCallbacks
{
    private const string PROP_PRIVATE = "priv";
    private const string PROP_PWD = "pwd";

    private string _pendingPasswordHash = "";

    private void Awake()
    {
        PhotonNetwork.KeepAliveInBackground = 60f;
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public bool CanMatchmake() =>
        PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InLobby;

    public void CreateRoom(string roomName, bool isPrivate, string password)
    {
        if (!CanMatchmake())
        {
            Debug.LogWarning("[RoomService] Not ready for CreateRoom.");
            return;
        }

        var options = new RoomOptions
        {
            MaxPlayers = 8,
            IsVisible = !isPrivate,
            IsOpen = true,
            CleanupCacheOnLeave = true,
            PlayerTtl = 0,
            EmptyRoomTtl = 0
        };

        var props = new Hashtable
        {
            { PROP_PRIVATE, isPrivate },
            { PROP_PWD, isPrivate ? SimpleHash(password) : "" }
        };

        options.CustomRoomProperties = props;
        options.CustomRoomPropertiesForLobby = new[] { PROP_PRIVATE };

        Debug.Log($"[RoomService] Creating room '{roomName}' private={isPrivate}");
        PhotonNetwork.CreateRoom(roomName, options, TypedLobby.Default);
    }

    public void JoinPublicRandom()
    {
        if (!CanMatchmake())
        {
            Debug.LogWarning("[RoomService] Not ready for JoinRandom.");
            return;
        }

        Debug.Log("[RoomService] JoinRandomRoom...");
        PhotonNetwork.JoinRandomRoom();
    }

    public void JoinPrivate(string roomName, string password)
    {
        if (!CanMatchmake())
        {
            Debug.LogWarning("[RoomService] Not ready for JoinRoom.");
            return;
        }

        _pendingPasswordHash = SimpleHash(password);
        Debug.Log($"[RoomService] JoinRoom '{roomName}'...");
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("[RoomService] Room created: " + PhotonNetwork.CurrentRoom.Name);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[RoomService] Joined room: " + PhotonNetwork.CurrentRoom.Name);

        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        bool isPrivate = props.ContainsKey(PROP_PRIVATE) && (bool)props[PROP_PRIVATE];

        if (isPrivate)
        {
            string stored = props.ContainsKey(PROP_PWD) ? (string)props[PROP_PWD] : "";
            if (stored != _pendingPasswordHash)
            {
                Debug.LogWarning("[RoomService] Wrong password. Leaving.");
                _pendingPasswordHash = "";
                PhotonNetwork.LeaveRoom();
                return;
            }
        }

        _pendingPasswordHash = "";

        if (!string.IsNullOrWhiteSpace(AuthSession.PlayFabId))
            FarmAssignmentService.RegisterLocalPlayFabId(AuthSession.PlayFabId);

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[RoomService] Master loading Game scene...");
            PhotonNetwork.LoadLevel("Game");
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[RoomService] CreateRoom failed: {returnCode} {message}");
        _pendingPasswordHash = "";
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[RoomService] JoinRandom failed, creating fallback room...");
        string fallback = "Public-" + Random.Range(1000, 9999);
        CreateRoom(fallback, false, "");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[RoomService] JoinRoom failed: {returnCode} {message}");
        _pendingPasswordHash = "";
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[RoomService] Left room.");
        _pendingPasswordHash = "";

        // YENÝ: Odadan ayrýlýnca offline iþaretle
        PlayFabSessionValidator.MarkOffline();
    }

    private static string SimpleHash(string s)
    {
        unchecked
        {
            int hash = 23;
            foreach (char c in s) hash = hash * 31 + c;
            return hash.ToString();
        }
    }
}