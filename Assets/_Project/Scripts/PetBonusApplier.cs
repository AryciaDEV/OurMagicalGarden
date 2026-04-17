using UnityEngine;

public class PetBonusApplier : MonoBehaviour
{
    [Header("Refs")]
    public PlayerMovement movement; // sende olan script
    private float _baseMove;
    private float _baseSprint;

    private void Start()
    {
        if (!movement) movement = GetComponent<PlayerMovement>();
        if (!movement) return;

        _baseMove = movement.moveSpeed;
        _baseSprint = movement.sprintSpeed;

        // local inventory event
        if (PlayerPetInventory.Local != null)
            PlayerPetInventory.Local.OnChanged += Recalc;

        Recalc();
    }

    private void OnDestroy()
    {
        if (PlayerPetInventory.Local != null)
            PlayerPetInventory.Local.OnChanged -= Recalc;
    }

    private void Recalc()
    {
        if (!movement) return;

        // reset
        movement.moveSpeed = _baseMove;
        movement.sprintSpeed = _baseSprint;

        var inv = PlayerPetInventory.Local;
        if (inv == null) return;

        int uid = inv.EquippedUid;
        if (uid <= 0) return;

        var it = inv.GetByUid(uid);
        if (it == null) return;

        var svc = PetNetworkService.Instance;
        if (svc == null) return;

        var (grow, move, sell) = svc.GetBonusesForPet(it.petId);

        float moveMul = 1f + (move / 100f);
        movement.moveSpeed = _baseMove * moveMul;
        movement.sprintSpeed = _baseSprint * moveMul;
    }
}