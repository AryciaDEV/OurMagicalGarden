using Photon.Pun;
using UnityEngine;

public class PetFollowNetwork : MonoBehaviourPun
{
    public float followDistance = 1.4f;
    public float height = 0.0f;
    public float moveLerp = 10f;
    public float rotLerp = 12f;

    private int _ownerViewId;
    private Transform _owner;

    private void Awake()
    {
        if (photonView.InstantiationData != null && photonView.InstantiationData.Length > 0)
            _ownerViewId = (int)photonView.InstantiationData[0];
    }

    private void Start() => ResolveOwner();

    private void ResolveOwner()
    {
        if (_owner != null) return;
        if (_ownerViewId <= 0) return;

        var pv = PhotonView.Find(_ownerViewId);
        if (pv != null) _owner = pv.transform;
    }

    private void Update()
    {
        if (_owner == null)
        {
            ResolveOwner();
            return;
        }

        Vector3 back = -_owner.forward;
        back.y = 0f;
        if (back.sqrMagnitude < 0.0001f) back = Vector3.back;
        back.Normalize();

        Vector3 target = _owner.position + back * followDistance;
        target.y = _owner.position.y + height;

        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * moveLerp);

        Quaternion rot = Quaternion.LookRotation(_owner.forward, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * rotLerp);
    }
}