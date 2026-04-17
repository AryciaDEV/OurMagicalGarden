using Photon.Pun;
using UnityEngine;

public class PlayerAnimatorSync : MonoBehaviourPun, IPunObservable
{
    [SerializeField] private Animator animator;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int GroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int SprintHash = Animator.StringToHash("IsSprinting");

    private float _netSpeed;
    private bool _netGrounded;
    private bool _netJumpPulse;
    private bool _jumpQueued;
    private bool _netSprinting;

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
    }
    public void QueueJump()
    {
        if (!photonView.IsMine) return;
        _jumpQueued = true;
    }

    private void Update()
    {
        if (photonView.IsMine) return;
        if (!animator) return;

        // Remote taraf: gelen speed’i uygula
        //animator.SetFloat(SpeedHash, _netSpeed);
        animator.SetFloat(SpeedHash, _netSpeed);
        animator.SetBool(GroundedHash, _netGrounded);
        animator.SetBool(SprintHash, _netSprinting);

        if (_netJumpPulse)
        {
            animator.SetTrigger(JumpHash);
            _netJumpPulse = false;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (!animator) return;

        if (stream.IsWriting)
        {
            float localSpeed = animator.GetFloat(SpeedHash);
            bool grounded = animator.GetBool(GroundedHash);
            bool jumpPulse = _jumpQueued;
            bool sprinting = animator.GetBool(SprintHash);

            stream.SendNext(localSpeed);
            stream.SendNext(grounded);
            stream.SendNext(jumpPulse);
            if (_jumpQueued) _jumpQueued = false;
            stream.SendNext(sprinting);
        }
        else
        {
            _netSpeed = (float)stream.ReceiveNext();
            _netGrounded = (bool)stream.ReceiveNext();
            _netJumpPulse = (bool)stream.ReceiveNext();
            _netSprinting = (bool)stream.ReceiveNext();
        }
    }
}