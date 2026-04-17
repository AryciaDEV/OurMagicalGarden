using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;

public class RoomChatService : MonoBehaviourPunCallbacks
{
    public static RoomChatService Instance { get; private set; }

    public event System.Action<string, string> OnMessageReceived;

    [Header("Config")]
    [SerializeField] private int maxMessageLength = 200;
    [SerializeField] private float messageCooldown = 1f;
    [SerializeField] private int maxMessageHistory = 100; // Son X mesajý tut

    private float _lastSendTime;

    // Mesaj geçmiþi - tüm oyuncular için ortak
    private readonly List<ChatMessage> _messageHistory = new();

    // Mesaj yapýsý
    public readonly struct ChatMessage
    {
        public readonly string Sender;
        public readonly string Content;
        public readonly float Timestamp;

        public ChatMessage(string sender, string content)
        {
            Sender = sender;
            Content = content;
            Timestamp = Time.time;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Sahne deðiþse bile kalýcý
    }

    public void SendChatMessage(string rawMessage)
    {
        if (!PhotonNetwork.InRoom) return;
        if (string.IsNullOrWhiteSpace(rawMessage)) return;

        if (Time.time - _lastSendTime < messageCooldown)
            return;

        _lastSendTime = Time.time;

        string msg = rawMessage.Trim();
        if (msg.Length > maxMessageLength)
            msg = msg.Substring(0, maxMessageLength);

        string senderNick = PhotonNetwork.NickName;
        if (string.IsNullOrWhiteSpace(senderNick))
            senderNick = "Player";

        // Önce kendi geçmiþine ekle (local)
        AddMessageToHistory(senderNick, msg);

        // Sonra diðerlerine gönder
        photonView.RPC(nameof(RPC_ReceiveChatMessage), RpcTarget.Others, senderNick, msg);
    }

    private void AddMessageToHistory(string sender, string message)
    {
        var chatMsg = new ChatMessage(sender, message);
        _messageHistory.Add(chatMsg);

        // Limit kontrolü
        if (_messageHistory.Count > maxMessageHistory)
            _messageHistory.RemoveAt(0);

        // Event'i tetikle
        OnMessageReceived?.Invoke(sender, message);
    }

    // UI açýldýðýnda çaðrýlacak - geçmiþ mesajlarý al
    public IReadOnlyList<ChatMessage> GetMessageHistory()
    {
        return _messageHistory;
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        string systemMsg = $"{newPlayer.NickName} joined the room.";
        AddMessageToHistory("System", systemMsg);
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        string systemMsg = $"{otherPlayer.NickName} left the room.";
        AddMessageToHistory("System", systemMsg);
    }

    [PunRPC]
    private void RPC_ReceiveChatMessage(string senderNick, string message)
    {
        AddMessageToHistory(senderNick, message);
    }

    // Oda deðiþtiðinde geçmiþi temizle (isteðe baðlý)
    public override void OnLeftRoom()
    {
        _messageHistory.Clear();
    }
}