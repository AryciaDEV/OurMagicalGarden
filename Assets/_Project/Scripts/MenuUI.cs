using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuUI : MonoBehaviourPunCallbacks
{
    [Header("Panels")]
    public GameObject authPanel;
    public GameObject roomPanel;

    [Header("Inputs")]
    public TMP_InputField roomNameInput;
    public TMP_InputField passwordInput;
    public Toggle privateToggle;

    [Header("Buttons")]
    public Button createButton;
    public Button joinPublicButton;
    public Button joinPrivateButton;

    [Header("Status UI")]
    public TMP_Text statusText;

    [Header("Service")]
    public RoomService roomService;

    private void Start()
    {
        SetButtons(false);
        UpdateStatus();

        if (authPanel) authPanel.SetActive(true);
        if (roomPanel) roomPanel.SetActive(false);

        if (PhotonNetwork.InLobby && PhotonNetwork.IsConnectedAndReady)
        {
            if (authPanel) authPanel.SetActive(false);
            if (roomPanel) roomPanel.SetActive(true);
            SetButtons(true);
        }
        privateToggle.isOn = false;
    }

    private void UpdateStatus()
    {
        if (!statusText) return;

        statusText.text =
            $"ConnectedAndReady: {PhotonNetwork.IsConnectedAndReady}\n" +
            $"InLobby: {PhotonNetwork.InLobby}\n" +
            $"State: {PhotonNetwork.NetworkClientState}\n" +
            $"Server: {PhotonNetwork.Server}\n" +
            $"Nick: {PhotonNetwork.NickName}";
    }

    private void SetButtons(bool on)
    {
        if (createButton) createButton.interactable = on;
        if (joinPublicButton) joinPublicButton.interactable = on;
        if (joinPrivateButton) joinPrivateButton.interactable = on;
    }

    public void OnCreateRoomClicked()
    {
        SetButtons(false);
        UpdateStatus();

        string roomName = roomNameInput != null ? roomNameInput.text.Trim() : "";

        // Kullanici girisini kontrol et
        if (!string.IsNullOrWhiteSpace(roomName))
        {
            // Sadece sayýlardan oluþuyor mu kontrol et
            if (System.Text.RegularExpressions.Regex.IsMatch(roomName, @"^\d+$"))
            {
                // Sayý ise ve maksimum 4 karakter ise
                if (roomName.Length <= 4)
                {
                    roomName = "Room-" + roomName;
                }
                else
                {
                    // 4 karakterden uzunsa uyarý ver
                    Debug.LogError("Oda adý maksimum 4 haneli sayý olabilir!");
                    SetButtons(true);
                    return;
                }
            }
            else
            {
                // Kelime veya özel karakter girilmiþse uyarý ver
                Debug.LogError("Oda adý sadece sayýlardan oluþmalýdýr! Kelime giriþi yapýlamaz.");
                SetButtons(true);
                return;
            }
        }
        else
        {
            // Boþ ise varsayýlan oda adý oluþtur
            roomName = "Room-" + Random.Range(1000, 9999);
        }

        bool isPrivate = privateToggle && privateToggle.isOn;
        string pwd = passwordInput ? passwordInput.text : "";

        roomService.CreateRoom(roomName, isPrivate, pwd);
    }

    public void OnJoinPublicClicked()
    {
        SetButtons(false);
        UpdateStatus();
        roomService.JoinPublicRandom();
    }

    public void OnPlayClicked()
    {
        SetButtons(false);
        UpdateStatus();

        string roomName = roomNameInput != null ? roomNameInput.text.Trim() : "";

        // Kullanici girisini kontrol et
        if (!string.IsNullOrWhiteSpace(roomName))
        {
            // Sadece sayýlardan oluþuyor mu kontrol et
            if (System.Text.RegularExpressions.Regex.IsMatch(roomName, @"^\d+$"))
            {
                // Sayý ise ve maksimum 4 karakter ise
                if (roomName.Length <= 4)
                {
                    roomName = "Room-" + roomName;
                }
                else
                {
                    // 4 karakterden uzunsa uyarý ver
                    Debug.LogError("Oda adý maksimum 4 haneli sayý olabilir!");
                    SetButtons(true);
                    return;
                }
            }
            else
            {
                // Kelime veya özel karakter girilmiþse uyarý ver
                Debug.LogError("Oda adý sadece sayýlardan oluþmalýdýr! Kelime giriþi yapýlamaz.");
                SetButtons(true);
                return;
            }
        }
        else
        {
            // Boþ ise varsayýlan oda adý oluþtur
            roomName = "Room-" + Random.Range(1000, 9999);
        }

        bool isPrivate = privateToggle && privateToggle.isOn;
        string pwd = passwordInput ? passwordInput.text : "";

        roomService.CreateRoom(roomName, isPrivate, pwd);
    }

    public void OnJoinPrivateClicked()
    {
        SetButtons(false);
        UpdateStatus();

        string roomName = roomNameInput != null ? roomNameInput.text.Trim() : "";
        if (string.IsNullOrWhiteSpace(roomName))
        {
            Debug.LogWarning("[MenuUI] Room name boþ olamaz (private join).");
            if (roomService.CanMatchmake()) SetButtons(true);
            return;
        }

        string pwd = passwordInput ? passwordInput.text : "";
        roomService.JoinPrivate(roomName, pwd);
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[MenuUI] OnJoinedLobby -> buttons enabled");
        UpdateStatus();
        SetButtons(true);

        if (authPanel) authPanel.SetActive(false);
        if (roomPanel) roomPanel.SetActive(true);
    }

    public override void OnLeftLobby()
    {
        Debug.Log("[MenuUI] OnLeftLobby -> buttons disabled");
        UpdateStatus();
        SetButtons(false);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[MenuUI] OnCreateRoomFailed {returnCode} {message}");
        UpdateStatus();

        if (roomService.CanMatchmake()) SetButtons(true);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[MenuUI] OnJoinRoomFailed {returnCode} {message}");
        UpdateStatus();

        if (roomService.CanMatchmake()) SetButtons(true);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[MenuUI] OnJoinRandomFailed {returnCode} {message}");
        UpdateStatus();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[MenuUI] OnJoinedRoom -> buttons disabled");
        UpdateStatus();
        SetButtons(false);
    }
}