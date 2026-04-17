using Photon.Pun;
using TMPro;
using UnityEngine;

public class PlayerNameTag : MonoBehaviourPun
{
    [Header("Refs")]
    public TMP_Text nameText;
    public Transform lookTarget; // boþsa Camera.main'e bakar

    private Camera _cam;

    private void Start()
    {
        _cam = Camera.main;

        if (nameText != null)
        {
            string nick = photonView != null && photonView.Owner != null
                ? photonView.Owner.NickName
                : "Player";

            nameText.text = nick;
        }
    }

    private void LateUpdate()
    {
        if (nameText == null) return;

        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        // kamera yönüne baksýn
        transform.forward = _cam.transform.forward;
    }
}