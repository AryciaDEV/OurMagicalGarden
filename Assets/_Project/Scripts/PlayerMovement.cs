using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviourPun
{
    [Header("Move")]
    public float moveSpeed = 4.5f;
    public float sprintSpeed = 7.0f;
    public float gravity = -20f;
    public float jumpHeight = 1.2f;

    [Header("Look")]
    public RobloxCameraController camController;
    public float turnSmooth = 18f;

    private CharacterController _cc;
    private float _yVel;
    private bool _jumpPressed;
    private bool _isMobileControlEnabled = false;

    // Mobile Controller referanslarý - otomatik bulunacak
    private FixedJoystick _movementJoystick;
    private Button _jumpButton;
    private MobileControllerToggle _mobileToggle;

    private bool _pokiGameplayStarted;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    private void Start()
    {
        if (!camController) camController = GetComponent<RobloxCameraController>();

        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }

        // ===== MOBÝL KONTROLLERÝ OTOMATÝK BUL =====
        FindMobileControllers();

        // Event'i dinle
        if (_mobileToggle != null)
        {
            MobileControllerToggle.OnMobileControlToggled += OnMobileControlToggled;
            _isMobileControlEnabled = _mobileToggle.IsMobileControlEnabled();

            // Buton event'ini baðla
            if (_jumpButton != null)
            {
                _jumpButton.onClick.RemoveAllListeners();
                _jumpButton.onClick.AddListener(OnJumpButtonPressed);
            }

            // UI'ý baþlangýçta ayarla
            SetMobileControlsActive(_isMobileControlEnabled);
        }

        // PC modu için imleç ayarlarý
        if (!_isMobileControlEnabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Debug.Log($"[PlayerMovement] Start - Mobile control: {_isMobileControlEnabled}, Joystick: {(_movementJoystick != null)}, JumpButton: {(_jumpButton != null)}");
    }

    private void FindMobileControllers()
    {
        // MobileControllerToggle'ý bul
        _mobileToggle = FindObjectOfType<MobileControllerToggle>();

        if (_mobileToggle != null)
        {
            // Joystick'i bul
            _movementJoystick = FindObjectOfType<FixedJoystick>();

            // Jump butonunu bul (JumpButton script'i olan GameObject'teki Button)
            var jumpButtonObj = FindObjectOfType<JumpButton>();
            if (jumpButtonObj != null)
            {
                _jumpButton = jumpButtonObj.GetComponent<Button>();
            }

            // Eðer hala bulunamadýysa, ismi "JumpButton" olan butonu dene
            if (_jumpButton == null)
            {
                GameObject jumpBtnGO = GameObject.Find("JumpButton");
                if (jumpBtnGO != null)
                    _jumpButton = jumpBtnGO.GetComponent<Button>();
            }

            Debug.Log($"[PlayerMovement] Found - Joystick: {(_movementJoystick != null)}, JumpButton: {(_jumpButton != null)}");
        }
        else
        {
            Debug.Log("[PlayerMovement] MobileControllerToggle not found!");
        }
    }

    private void OnDestroy()
    {
        if (_mobileToggle != null)
        {
            MobileControllerToggle.OnMobileControlToggled -= OnMobileControlToggled;
        }
    }

    private void OnMobileControlToggled(bool enabled)
    {
        _isMobileControlEnabled = enabled;
        SetMobileControlsActive(enabled);

        // Ýmleç ayarlarý
        if (enabled)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Debug.Log($"[PlayerMovement] Mobile control toggled: {enabled}");
    }

    private void SetMobileControlsActive(bool active)
    {
        if (_movementJoystick != null)
            _movementJoystick.gameObject.SetActive(active);

        if (_jumpButton != null)
            _jumpButton.gameObject.SetActive(active);
    }

    private void Update()
    {
        // Input al
        Vector2 input = GetInput();
        float h = input.x;
        float v = input.y;

        if (!_pokiGameplayStarted)
        {
            _pokiGameplayStarted = true;
            if (PokiAdsService.Instance != null)
                PokiAdsService.Instance.GameplayStart();
        }

        float yaw = camController != null ? camController.GetYaw() : transform.eulerAngles.y;
        Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 moveDir = (yawRot * new Vector3(h, 0f, v)).normalized;

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSmooth);
        }

        // Sprint - Mobilde her zaman hýzlý koþ
        bool sprint = GetSprintState();

        float baseSpeed = sprint ? sprintSpeed : moveSpeed;
        float speed = baseSpeed * GetPetMoveSpeedMultiplier();

        // Gravity
        if (_cc.isGrounded && _yVel < 0) _yVel = -2f;
        _yVel += gravity * Time.deltaTime;

        // Jump
        if (GetJumpState())
        {
            _yVel = Mathf.Sqrt(jumpHeight * -2f * gravity);
            _jumpPressed = false;
            Debug.Log("[PlayerMovement] Jump executed!");
        }

        Vector3 velocity = moveDir * speed;
        velocity.y = _yVel;

        _cc.Move(velocity * Time.deltaTime);
    }

    // ===== GÝRÝÞ KONTROLÜ =====
    private Vector2 GetInput()
    {
        if (_isMobileControlEnabled && _movementJoystick != null)
        {
            // Mobil kontrol açýk: Joystick'ten al
            Vector2 joystickInput = new Vector2(_movementJoystick.Horizontal, _movementJoystick.Vertical);

            // Joystick deðeri varsa logla (performans için sadece debug modunda)
            if (joystickInput.sqrMagnitude > 0.01f)
            {
                // Debug.Log($"[PlayerMovement] Joystick: {joystickInput}");
            }

            return joystickInput;
        }
        else
        {
            // PC modu: Klavyeden al
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }
    }

    private bool GetSprintState()
    {
        if (_isMobileControlEnabled)
        {
            // Mobilde her zaman sprint hýzýnda koþ
            return true;
        }
        else
        {
            // PC'de Shift tuþu
            return Input.GetKey(KeyCode.LeftShift);
        }
    }

    private bool GetJumpState()
    {
        if (_isMobileControlEnabled)
        {
            // Mobilde buton basýldý mý?
            return _cc.isGrounded && _jumpPressed;
        }
        else
        {
            // PC'de Space tuþu
            return _cc.isGrounded && Input.GetKeyDown(KeyCode.Space);
        }
    }

    public void OnJumpButtonPressed()
    {
        Debug.Log("[PlayerMovement] Jump button pressed!");
        if (_cc.isGrounded)
        {
            _jumpPressed = true;
            Debug.Log("[PlayerMovement] Jump button - grounded, will jump next frame!");
        }
        else
        {
            Debug.Log("[PlayerMovement] Jump button - NOT grounded!");
        }
    }

    // ===== PET BONUS =====
    private float GetPetMoveSpeedMultiplier()
    {
        float mul = 1f;
        // Pet bonus kodun burada...
        return mul;
    }
}