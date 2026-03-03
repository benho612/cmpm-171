using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class MovementSandBox : MonoBehaviour
{
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
    private PlayerControls _input;
    private Transform _cameraTransform;
    [SerializeField] private CombatSandBox _combat;
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
        
        _input = new PlayerControls();
        
        _input.Gameplay.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _input.Gameplay.Move.canceled += ctx => _moveInput = Vector2.zero;
        
        _input.Gameplay.Dash.performed += ctx => AttemptDash();
        _input.Gameplay.Dash.performed += ctx => OnDashInput();
    }

    private void OnEnable() => _input.Enable();
    private void OnDisable() => _input.Disable();

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

        // Mouse Lock Toggle (Alt key)
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
        
        // If Dashing, override everything
        if (_isDashing)
        {
            HandleDash();
            return;
        }

        // bool isBlocking = _combat != null && _combat.IsBlocking;

        // // ANCHOR SYSTEM - so that player doesnt get pushed around when blocking./
        // if (isBlocking)
        // {
        //     // The exact frame block started, save foot position
        //     if (!_wasBlocking)
        //     {
        //         _defenseAnchorPosition = transform.position;
        //     }
            
        //     // Force our X and Z position to stay exactly where we planted our feet.
        //     transform.position = new Vector3(_defenseAnchorPosition.x, transform.position.y, _defenseAnchorPosition.z);
            
        //     // Sync with Unity's physics engine so it doesn't get confused
        //     Physics.SyncTransforms(); 
        // }
        
        // // Remember state for the next frame
        // _wasBlocking = isBlocking;

        // If Attacking, stop movement logic so Combat.cs controls rotation
        if (_combat != null && (_combat.IsAttacking || _combat.IsDodging || _combat.IsBlocking))
        {

            _smoothSpeed = 0; // Rapidly decelerate to a stop
            return;
        }

        // Otherwise, handle standard movement
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (_moveInput.magnitude < 0.1f) return;

        bool isSprinting = _input.Gameplay.Dash.IsPressed();

        //Calculate World Direction relative to Camera
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
        if (_combat != null && _combat.IsAttacking)
        {
            // Double check: if still in active frames, we can't dash yet
            if (_combat.IsInActiveFrames) return; 
            
            // If in recovery or windup, cancel it
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
        transform.rotation = Quaternion.LookRotation(_dashDirection);

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
}