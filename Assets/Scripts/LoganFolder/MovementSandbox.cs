using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class MovementSandBox : MonoBehaviour
{
    [Header("Input References")]
    [Tooltip("Drag the 'Move' InputActionReference here")]
    public InputActionReference moveAction;
    [Tooltip("Drag the 'Dash' InputActionReference here")]
    public InputActionReference dashAction;

    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;

    [Header("Dash Settings")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.5f; // Time between dashes
    public float dashIFrames = 0.15f; // How long you are invincible during the dash

    [Tooltip("How fast the character turns while sprinting")]
    public float rotationSpeed = 15f;

    [Tooltip("How fast the character aligns with the camera while strafing")]
    public float strafeTurnSpeed = 20f;

    [Header("Buffering")]
    public float bufferWindow = 0.2f; // How long to remember the dash input
    private float _dashBufferTimer;
    private bool _hasBufferedDash;

    [Header("Physics")]
    public float gravity = -9.81f;
    public float gravityMultiplier = 2.0f;

    // Internal Variables
    private CharacterController _controller;
    private Transform _cameraTransform;

    // References for combat and animation
    [SerializeField] private CombatSandBox _combat;
    [SerializeField] private AnimationBridge _animator;

    private Vector3 _velocity;
    private Vector2 _moveInput;
    private float _smoothSpeed;
    private float _targetSpeed;
    private float _dashCooldownTimer;
    private float _iFrameTimer;

    // Dash Logic
    private bool _isDashing;
    public bool IsDashing => _isDashing;
    private float _dashTimer;
    public bool IsInvincibleViaDash => _iFrameTimer > 0;
    private Vector3 _dashDirection;

    // Anchor Logic
    private Vector3 _defenseAnchorPosition;
    private bool _wasBlocking;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _cameraTransform = Camera.main.transform;

        // Subscribe to input events using the Action References
        if (moveAction != null)
        {
            moveAction.action.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
            moveAction.action.canceled += ctx => _moveInput = Vector2.zero;
        }

        if (dashAction != null)
        {
            // Note: In your original script, you subscribed twice (AttemptDash and OnDashInput).
            // OnDashInput already calls AttemptDash, so we only need to subscribe to OnDashInput here to prevent duplicate calls.
            dashAction.action.performed += ctx => OnDashInput();
        }
    }

    private void OnEnable()
    {
        moveAction?.action.Enable();
        dashAction?.action.Enable();
    }

    private void OnDisable()
    {
        moveAction?.action.Disable();
        dashAction?.action.Disable();
    }

    private void OnDashInput()
    {
        // If we are in active frames, buffer the dash instead of failing
        if (_combat != null && _combat.IsAttacking && _combat.IsInActiveFrames)
        {
            _hasBufferedDash = true;
            _dashBufferTimer = bufferWindow;
            Debug.Log("Dash Buffered!");
        }
        else
        {
            AttemptDash(); // Normal dash attempt
        }
    }

    private void Update()
    {
        if (_dashCooldownTimer > 0) _dashCooldownTimer -= Time.deltaTime;
        if (_iFrameTimer > 0) _iFrameTimer -= Time.deltaTime;

        // Tick down buffer
        if (_hasBufferedDash)
        {
            _dashBufferTimer -= Time.deltaTime;
            if (_dashBufferTimer <= 0) _hasBufferedDash = false;

            // If we are no longer in active frames, execute the buffered dash
            if (_combat != null && !_combat.IsInActiveFrames)
            {
                _hasBufferedDash = false;
                AttemptDash();
            }
        }

        ApplyGravity();

        // Mouse Lock Toggle (Alt key) - Keeping this as a direct hardware poll for debug purposes
        if (Keyboard.current.leftAltKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        if (_combat != null && _combat.IsStunned)
        {
            _smoothSpeed = 0f; // Rapidly decelerate
            return;
        }

        // If Dashing, override everything
        if (_isDashing)
        {
            HandleDash();
            return;
        }

        // If Attacking, stop movement logic so Combat.cs controls rotation
        if (_combat != null && (_combat.IsAttacking || _combat.IsDodging || _combat.IsBlocking))
        {
            _smoothSpeed = 0; // Rapidly decelerate to a stop
            return;
        }

        // Otherwise, handle standard movement
        HandleMovement();

        // AUDIO: Evaluate and play/stop the movement audio loops
        HandleMovementAudio();
    }

    private void HandleMovement()
    {
        if (_moveInput.magnitude < 0.1f) return;

        // Using the new reference to check if the button is held
        bool isSprinting = dashAction != null && dashAction.action.IsPressed();

        // Calculate World Direction relative to Camera
        Vector3 camForward = _cameraTransform.forward;
        Vector3 camRight = _cameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = (camForward * _moveInput.y + camRight * _moveInput.x).normalized;

        if (isSprinting)
        {
            // Sprinting: Face the direction we are moving
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            // Walking: Strafe
            if (camForward != Vector3.zero)
            {
                Quaternion strafeRotation = Quaternion.LookRotation(camForward);
                transform.rotation = Quaternion.Slerp(transform.rotation, strafeRotation, strafeTurnSpeed * Time.deltaTime);
            }
        }

        _targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
        _smoothSpeed = Mathf.Lerp(_smoothSpeed, _targetSpeed, 10f * Time.deltaTime);

        _controller.Move(moveDir * _smoothSpeed * Time.deltaTime);
    }

    private void AttemptDash()
    {
        // Check cooldown and state
        if (_isDashing || _dashCooldownTimer > 0) return;
        if (_combat != null && _combat.IsStunned) return;

        if (_combat != null && _combat.IsAttacking)
        {
            // Double check: if still in active frames, we can't dash yet
            if (_combat.IsInActiveFrames) return;

            // If in recovery or windup, cancel it
            _animator.BackToLocomotion();
            _combat.CancelAttackForDash();
        }

        _hasBufferedDash = false;

        _isDashing = true;
        _dashTimer = dashDuration;
        _iFrameTimer = dashIFrames; // Start I-Frames
        _dashCooldownTimer = dashCooldown; // Start Cooldown

        if (_moveInput.magnitude > 0.1f)
        {
            Vector3 camForward = _cameraTransform.forward;
            Vector3 camRight = _cameraTransform.right;
            camForward.y = 0;
            camRight.y = 0;
            _dashDirection = (camForward * _moveInput.y + camRight * _moveInput.x).normalized;
        }
        else
        {
            _dashDirection = transform.forward;
        }
    }

    private void HandleDash()
    {
        _controller.Move(_dashDirection * dashSpeed * Time.deltaTime);
        //transform.rotation = Quaternion.LookRotation(_dashDirection);

        _dashTimer -= Time.deltaTime;
        if (_dashTimer <= 0)
        {
            _isDashing = false;
        }
    }

    private void ApplyGravity()
    {
        if (_controller.isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f;
        }

        _velocity.y += gravity * gravityMultiplier * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    private void HandleMovementAudio()
    {
        // Determines the player's current state
        bool isMovingInput = _moveInput.magnitude > 0.1f;
        bool isSprintingInput = dashAction != null && dashAction.action.IsPressed();

        // Checks if the player is actually allowed to walk right now
        bool canMove = !_isDashing && (_combat == null || (!_combat.IsAttacking && !_combat.IsDodging && !_combat.IsBlocking && !_combat.IsStunned));

        // Plays the correct loop based on the state (Walking/Running)
        if (isMovingInput && canMove)
        {
            if (isSprintingInput)
            {
                AudioManager.Instance.PlayLoop("Player_Running");
                AudioManager.Instance.Stop("Player_Walking");
            }
            else
            {
                AudioManager.Instance.PlayLoop("Player_Walking");
                AudioManager.Instance.Stop("Player_Running");
            }
        }
        else
        {
            // If we are standing still, stunned, or dashing, silence the footsteps!
            AudioManager.Instance.Stop("Player_Walking");
            AudioManager.Instance.Stop("Player_Running");
        }
    }

}