using Photon.Pun;
using UnityEngine;

public class PanelTrigger : MonoBehaviourPun
{
    [Header("Panel Settings")]
    [SerializeField] private PanelType panelType; // Hangi panel açýlacak
    [SerializeField] private GameObject targetPanel; // Açýlacak panel (opsiyonel)
    [SerializeField] private bool toggleOnExit = true; // Çýkýþta kapansýn mý?

    [Header("Trigger Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool debugMode = true;

    public AudioClip myAudioClipOpen;
    public AudioClip myAudioClipClose;

    private void OnTriggerEnter(Collider other)
    {
        // Sadece player tag'ine sahip objeleri iþle
        if (!other.CompareTag(playerTag)) return;

        // PhotonView ile local player kontrolü
        PhotonView photonView = other.GetComponentInParent<PhotonView>();
        if (photonView != null && photonView.IsMine)
        {
            // Local player trigger'a girdi
            Debug.Log($"[PanelTrigger] Local player entered {panelType} trigger");

            // Paneli aç
            OpenPanel(panelType);
            SoundFXManager.Instance.PlaySound(myAudioClipOpen);

            // Eðer direkt panel referansý varsa onu da aç
            if (targetPanel != null)
                targetPanel.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!toggleOnExit) return;
        if (!other.CompareTag(playerTag)) return;

        PhotonView photonView = other.GetComponentInParent<PhotonView>();
        if (photonView != null && photonView.IsMine)
        {
            Debug.Log($"[PanelTrigger] Local player exited {panelType} trigger");

            // Paneli kapat
            ClosePanel(panelType);
            SoundFXManager.Instance.PlaySound(myAudioClipClose);

            if (targetPanel != null)
                targetPanel.SetActive(false);
        }
    }

    private void OpenPanel(PanelType type)
    {
        // PanelManager üzerinden aç
        PanelManager.Instance?.OpenPanel(type);
    }

    private void ClosePanel(PanelType type)
    {
        PanelManager.Instance?.ClosePanel(type);
    }
}