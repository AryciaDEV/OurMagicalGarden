using Photon.Pun;
using UnityEngine;

public class PlayerAnimDriver : MonoBehaviourPun
{
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController cc;
    private PlayerAnimatorSync sync;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int GroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int SprintHash = Animator.StringToHash("IsSprinting");

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!cc) cc = GetComponent<CharacterController>();
        if (!sync) sync = GetComponent<PlayerAnimatorSync>();
    }

    private void Start()
    {
        if (!photonView.IsMine)
        {
            enabled = false; // sadece local hesaplar
            return;
        }
    }

    private void Update()
    {
        if (!animator || !cc) return;

        // CharacterController.velocity güvenilir
        Vector3 v = cc.velocity;
        v.y = 0f;

        float speed = v.magnitude;
        animator.SetFloat(SpeedHash, speed);

        bool sprinting = Input.GetKey(KeyCode.LeftShift) && speed > 0.2f;
        animator.SetBool(SprintHash, sprinting);

        animator.SetBool(GroundedHash, cc.isGrounded);

        // jump input’u burada yakalayalým (movement zaten zýplatýyor, anim de tetiklensin)
        if (cc.isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            animator.SetTrigger(JumpHash);
            if (sync != null) sync.QueueJump();
        }
    }
}