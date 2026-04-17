using Photon.Pun;
using UnityEngine;

public class LookAtCamera : MonoBehaviourPun
{
    private Camera _localPlayerCamera;

    private void Start()
    {
        FindLocalPlayerCamera();
    }

    private void FindLocalPlayerCamera()
    {
        // "PlayerCamera" tag'ine sahip tüm objeleri bul
        GameObject[] cameraObjects = GameObject.FindGameObjectsWithTag("PlayerCamera");

        foreach (var camObj in cameraObjects)
        {
            var cam = camObj.GetComponent<Camera>();
            if (cam == null) continue;

            // Bu kameranýn PhotonView'ini bul
            var photonView = camObj.GetComponentInParent<PhotonView>();

            // Eðer PhotonView varsa ve local player'a aitse
            if (photonView != null && photonView.IsMine)
            {
                _localPlayerCamera = cam;
                Debug.Log("[Billboard] Local player camera found!");
                break;
            }
        }

        // Eðer hala bulunamadýysa, direkt tag ile dene (PhotonView yoksa)
        if (_localPlayerCamera == null)
        {
            foreach (var camObj in cameraObjects)
            {
                var cam = camObj.GetComponent<Camera>();
                if (cam != null)
                {
                    _localPlayerCamera = cam;
                    Debug.Log("[Billboard] Camera found by tag only");
                    break;
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (_localPlayerCamera == null)
        {
            // Her frame bulmaya çalýþma, periyodik dene
            if (Time.frameCount % 30 == 0) // Her 30 framede bir dene
                FindLocalPlayerCamera();
            return;
        }

        transform.LookAt(_localPlayerCamera.transform);
        transform.Rotate(0, 180, 0);
    }

    // Eðer camera yok edilirse veya deðiþirse
    private void OnCameraDestroyed()
    {
        _localPlayerCamera = null;
    }
}