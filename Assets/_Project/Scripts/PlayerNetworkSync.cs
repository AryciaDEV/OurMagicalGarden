using Photon.Pun;
using UnityEngine;

public class PlayerNetworkSync : MonoBehaviourPun, IPunObservable
{
    [Header("Smoothing")]
    public float posLerp = 12f;
    public float rotLerp = 12f;

    private Vector3 _netPos;
    private Quaternion _netRot;

    private void Start()
    {
        _netPos = transform.position;
        _netRot = transform.rotation;

        if (!photonView.IsMine)
        {
            var cc = GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
        }
    }

    private void Update()
    {
        if (photonView.IsMine) return;

        // Remote oyuncuyu yumuþat
        transform.position = Vector3.Lerp(transform.position, _netPos, Time.deltaTime * posLerp);
        transform.rotation = Quaternion.Slerp(transform.rotation, _netRot, Time.deltaTime * rotLerp);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Local -> network
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // Network -> remote
            _netPos = (Vector3)stream.ReceiveNext();
            _netRot = (Quaternion)stream.ReceiveNext();
        }
    }
}