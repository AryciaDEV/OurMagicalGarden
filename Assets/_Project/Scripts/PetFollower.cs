using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PetFollower : MonoBehaviourPunCallbacks
{
    [Header("Follow")]
    public Vector3 localOffset = new Vector3(0f, 0f, -1.2f);
    public float followLerp = 10f;
    public float rotateLerp = 10f;

    [Header("Optional anchor")]
    public Transform followAnchor; // boþsa transform kullanýr

    private GameObject _spawned;
    private string _currentPetId = "";

    private void Start()
    {
        if (!followAnchor) followAnchor = transform;

        // local için baþlangýç
        if (photonView.IsMine)
            RefreshFromLocalInventory();
        else
            RefreshFromPlayerProps(photonView.Owner);
    }

    private void Update()
    {
        if (_spawned == null) return;

        Transform a = followAnchor ? followAnchor : transform;
        Vector3 targetPos = a.TransformPoint(localOffset);

        _spawned.transform.position = Vector3.Lerp(_spawned.transform.position, targetPos, Time.deltaTime * followLerp);

        // owner'ýn forward yönüne göre dönsün
        Quaternion targetRot = Quaternion.LookRotation(a.forward, Vector3.up);
        _spawned.transform.rotation = Quaternion.Slerp(_spawned.transform.rotation, targetRot, Time.deltaTime * rotateLerp);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (targetPlayer == null) return;
        if (photonView.Owner != targetPlayer) return;

        if (changedProps != null && changedProps.ContainsKey("petEquippedId"))
            RefreshFromPlayerProps(targetPlayer);
    }

    // Local inventory equip deðiþince çaðýr
    public void RefreshFromLocalInventory()
    {
        if (!photonView.IsMine) return;
        if (PlayerPetInventory.Local == null) { SetPet(""); return; }

        int uid = PlayerPetInventory.Local.EquippedUid;
        if (uid <= 0) { SetPet(""); return; }

        var it = PlayerPetInventory.Local.GetByUid(uid);
        string petId = it != null ? (it.petId ?? "") : "";
        SetPet(petId);
    }

    private void RefreshFromPlayerProps(Player owner)
    {
        string petId = PetNetworkService.GetEquippedPetIdFromPlayer(owner);
        SetPet(petId);
    }

    private void SetPet(string petId)
    {
        petId = (petId ?? "").Trim();
        if (string.Equals(_currentPetId, petId, System.StringComparison.OrdinalIgnoreCase))
            return;

        _currentPetId = petId;

        if (_spawned != null)
        {
            Destroy(_spawned);
            _spawned = null;
        }

        if (string.IsNullOrWhiteSpace(petId)) return;

        var svc = PetNetworkService.Instance;
        if (svc == null) return;

        var def = svc.GetPetDef(petId);
        if (def == null || def.prefab == null) return;

        _spawned = Instantiate(def.prefab);
        _spawned.name = $"PetFollower_{petId}_{(photonView.Owner != null ? photonView.Owner.ActorNumber : 0)}";

        // anýnda doðru pozisyona koy
        Transform a = followAnchor ? followAnchor : transform;
        _spawned.transform.position = a.TransformPoint(localOffset);
        _spawned.transform.rotation = Quaternion.LookRotation(a.forward, Vector3.up);
    }
}