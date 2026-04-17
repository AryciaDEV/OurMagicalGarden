using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DisconnectPanelController : MonoBehaviourPunCallbacks
{
    public static DisconnectPanelController Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;

    [Header("Scene")]
    [SerializeField] private int firstSceneBuildIndex = 0;

    private bool _disconnectShown;

    private void Awake()
    {
        Instance = this;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError("[DisconnectPanel] Disconnected: " + cause);

        ShowDisconnectPanel();
    }

    public void ShowDisconnectPanel()
    {
        if (_disconnectShown) return;
        _disconnectShown = true;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnClickReconnect()
    {
        Time.timeScale = 1f;

        // Güvenli olsun diye Photon baðlantýsýný kapat
        if (PhotonNetwork.IsConnected)
            PhotonNetwork.Disconnect();

        SceneManager.LoadScene(firstSceneBuildIndex);
    }
}