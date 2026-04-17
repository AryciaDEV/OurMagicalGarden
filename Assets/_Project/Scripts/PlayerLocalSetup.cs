using Photon.Pun;
using UnityEngine;

public class PlayerLocalSetup : MonoBehaviourPun
{
    [Header("Refs")]
    public Camera playerCamera;
    public AudioListener audioListener;

    private void Reset()
    {
        // Prefab üstünden otomatik bulmaya çalýþýr
        playerCamera = GetComponentInChildren<Camera>(true);
        audioListener = GetComponentInChildren<AudioListener>(true);
    }

    private void Start()
    {
        bool isMine = photonView.IsMine;

        if (playerCamera) playerCamera.gameObject.SetActive(isMine);
        if (audioListener) audioListener.enabled = isMine;

        // Ýstersen burada remote player input scriptlerini kapatýrsýn (ileride)
        Debug.Log($"[PlayerLocalSetup] Actor={PhotonNetwork.LocalPlayer.ActorNumber} thisView={photonView.ViewID} IsMine={isMine}");
    }
}