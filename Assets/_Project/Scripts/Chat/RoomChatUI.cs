using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomChatUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField messageInput;
    public Button sendButton;
    public ScrollRect scrollRect;
    public Transform messagesRoot;
    public ChatMessageRowUI messageRowPrefab;
    public GameObject uiController;

    [Header("Settings")]
    public int maxVisibleMessages = 50;
    public bool loadHistoryOnOpen = true;

    private readonly Queue<ChatMessageRowUI> _rows = new();

    private void OnEnable()
    {
        if (sendButton)
        {
            sendButton.onClick.RemoveAllListeners();
            sendButton.onClick.AddListener(SendCurrentMessage);
        }

        if (RoomChatService.Instance != null)
        {
            RoomChatService.Instance.OnMessageReceived += OnMessageReceived;

            // YENÝ: Panel açýldýðýnda geçmiþ mesajlarý yükle
            if (loadHistoryOnOpen)
                LoadMessageHistory();
        }

        if (uiController)
            uiController.SetActive(false);
    }

    private void OnDisable()
    {
        if (sendButton)
            sendButton.onClick.RemoveListener(SendCurrentMessage);

        if (RoomChatService.Instance != null)
            RoomChatService.Instance.OnMessageReceived -= OnMessageReceived;

        if (uiController)
            uiController.SetActive(true);
    }

    private void Update()
    {
        if (messageInput == null) return;

        if (messageInput.isFocused && Input.GetKeyDown(KeyCode.Return))
        {
            SendCurrentMessage();
        }
    }

    // YENÝ: Geçmiþ mesajlarý UI'a yükle
    private void LoadMessageHistory()
    {
        if (!messagesRoot || !messageRowPrefab) return;
        if (RoomChatService.Instance == null) return;

        // Önce mevcut satýrlarý temizle (eðer varsa)
        ClearAllMessages();

        var history = RoomChatService.Instance.GetMessageHistory();

        foreach (var msg in history)
        {
            CreateMessageRow(msg.Sender, msg.Content);
        }

        // Scroll'u en alta çek
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }

    private void ClearAllMessages()
    {
        while (_rows.Count > 0)
        {
            var old = _rows.Dequeue();
            if (old != null)
                Destroy(old.gameObject);
        }
    }

    private void SendCurrentMessage()
    {
        if (messageInput == null) return;
        if (RoomChatService.Instance == null) return;

        string msg = messageInput.text;
        if (string.IsNullOrWhiteSpace(msg)) return;

        RoomChatService.Instance.SendChatMessage(msg);

        messageInput.text = "";
        messageInput.ActivateInputField();
    }

    private void OnMessageReceived(string senderNick, string message)
    {
        CreateMessageRow(senderNick, message);
    }

    private void CreateMessageRow(string sender, string message)
    {
        if (!messagesRoot || !messageRowPrefab) return;

        string full = $"[{sender}] : {message}";

        var row = Instantiate(messageRowPrefab, messagesRoot);
        row.gameObject.SetActive(true);
        row.Bind(full);

        _rows.Enqueue(row);

        // Limit kontrolü
        while (_rows.Count > maxVisibleMessages)
        {
            var old = _rows.Dequeue();
            if (old != null)
                Destroy(old.gameObject);
        }

        Canvas.ForceUpdateCanvases();

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }
}