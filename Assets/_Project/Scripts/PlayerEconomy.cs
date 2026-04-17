using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PlayerEconomy : MonoBehaviourPunCallbacks
{
    public static PlayerEconomy Local;

    private const string P_COINS = "coins";

    [SerializeField] private int startingCoins = 100;
    [SerializeField] private int coins = 100;

    private bool _leaderboardSubmitQueued;
    private float _lastLeaderboardSubmitTime = -999f;
    [SerializeField] private float leaderboardSubmitMinInterval = 3f;

    private bool _initialized;

    private void Awake()
    {
        if (photonView.IsMine)
            Local = this;
    }
    private void Update()
    {
        if (!photonView.IsMine) return;

        if (_leaderboardSubmitQueued && Time.time - _lastLeaderboardSubmitTime >= leaderboardSubmitMinInterval)
        {
            _leaderboardSubmitQueued = false;
            _lastLeaderboardSubmitTime = Time.time;
            PlayFabLeaderboardService.SubmitCoins(coins);
        }
    }
    private void Start()
    {
        if (!photonView.IsMine) return;

        InitializeCoinsSafely();
        RefreshUI();
    }

    public int Coins => coins;
    public bool CanAfford(int price) => coins >= price;

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        coins += amount;

        if (photonView.IsMine)
            PushCoinsToNetwork();

        RefreshUI();

        if (photonView.IsMine)
            _leaderboardSubmitQueued = true;

        var writer = FindFirstObjectByType<PlayerSaveWriter>();
        if (writer != null)
            writer.QueueSave();
    }

    public void SpendCoins(int amount)
    {
        if (amount <= 0) return;

        coins = Mathf.Max(0, coins - amount);

        if (photonView.IsMine)
            PushCoinsToNetwork();

        RefreshUI();

        if (photonView.IsMine)
            _leaderboardSubmitQueued = true;

        var writer = FindFirstObjectByType<PlayerSaveWriter>();
        if (writer != null)
            writer.QueueSave();
    }

    private void InitializeCoinsSafely()
    {
        if (_initialized) return;
        _initialized = true;

        int resolvedCoins = startingCoins;

        if (PhotonNetwork.LocalPlayer != null &&
            PhotonNetwork.LocalPlayer.CustomProperties != null &&
            PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(P_COINS))
        {
            object v = PhotonNetwork.LocalPlayer.CustomProperties[P_COINS];

            if (v is int i)
                resolvedCoins = i;
            else if (v is long l)
                resolvedCoins = (int)l;
        }
        else
        {
            resolvedCoins = Mathf.Max(0, startingCoins);
        }

        coins = Mathf.Max(0, resolvedCoins);

        if (PhotonNetwork.InRoom)
            PushCoinsToNetwork();

        Debug.Log($"[PlayerEconomy] InitializeCoinsSafely -> {coins}");
    }

    private void RefreshUI()
    {
        if (CoinUI.Instance != null)
            CoinUI.Instance.SetCoins(coins);
    }

    private void PushCoinsToNetwork()
    {
        if (!PhotonNetwork.InRoom) return;

        PhotonNetwork.LocalPlayer.SetCustomProperties(
            new Hashtable { { P_COINS, coins } }
        );
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!photonView.IsMine) return;
        if (targetPlayer != PhotonNetwork.LocalPlayer) return;

        if (changedProps.ContainsKey(P_COINS))
        {
            object v = changedProps[P_COINS];

            if (v is int i) coins = i;
            else if (v is long l) coins = (int)l;

            RefreshUI();
        }
    }

    public override void OnJoinedRoom()
    {
        if (!photonView.IsMine) return;

        // Rejoin sonrasý room property boþsa coin yine ezilmesin
        InitializeCoinsSafely();
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (Local == this)
            Local = null;
    }

    public void SetCoins(int amount, bool submitToLeaderboard = true)
    {
        coins = Mathf.Max(0, amount);

        if (photonView.IsMine)
            PushCoinsToNetwork();

        RefreshUI();

        if (photonView.IsMine && submitToLeaderboard)
            PlayFabLeaderboardService.SubmitCoins(coins);
    }
}