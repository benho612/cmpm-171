using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Movement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    
    [Tooltip("How fast the character turns while sprinting")]
    public float rotationSpeed = 15f; 
    
    [Tooltip("How fast the character aligns with the camera while strafing")]
    public float strafeTurnSpeed = 20f; 

    [Header("Physics")]
    public float gravity = -9.81f;
    public float gravityMultiplier = 2.0f;

    // Internal Variables
    private CharacterController _controller;
    private PlayerControls _input;
    private Transform _cameraTransform;
    private Combat _combat; // Reference to Combat script
    
    private Vector3 _velocity;
    private Vector2 _moveInput;
    private float _smoothSpeed;
    
    // Dash Logic
    private bool _isDashing;
    private float _dashTimer;
    private Vector3 _dashDirection;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _combat = GetComponent<Combat>(); // Get the Combat component
        _cameraTransform = Camera.main.transform;
        
        _input = new PlayerControls();
        
        _input.Gameplay.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _input.Gameplay.Move.canceled += ctx => _moveInput = Vector2.zero;
        
        _input.Gameplay.Dash.performed += ctx => AttemptDash();
    }

    private void OnEnable() => _input.Enable();
    private void OnDisable() => _input.Disable();

    private void Update()
    {
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
        
        // 1. If Dashing, override everything
        if (_isDashing)
        {
            HandleDash();
            return;
        }

        // 2. If Attacking, stop movement logic so Combat.cs controls rotation
        if (_combat != null && _combat.IsAttacking)
        {
            _smoothSpeed = 0; // Rapidly decelerate
            return;
        }

        // 3. Otherwise, handle standard movement
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (_moveInput.magnitude < 0.1f) return;

        bool isSprinting = _input.Gameplay.Dash.IsPressed();

        // --- Calculate World Direction relative to Camera ---
        Vector3 camForward = _cameraTransform.forward;
        Vector3 camRight = _cameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = (camForward * _moveInput.y + camRight * _moveInput.x).normalized;

        // --- Rotation Logic Changes ---
        if (isSprinting)
        {
            // Sprinting: Face the direction we are moving (Classic logic)
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            // Walking: Strafe (Face the Camera's forward)
            // This allows "Backstepping" when pressing Down
            if (camForward != Vector3.zero)
            {
                Quaternion strafeRotation = Quaternion.LookRotation(camForward);
                transform.rotation = Quaternion.Slerp(transform.rotation, strafeRotation, strafeTurnSpeed * Time.deltaTime);
            }
        }

        // --- Movement Application ---
        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
        _smoothSpeed = Mathf.Lerp(_smoothSpeed, targetSpeed, 10f * Time.deltaTime);
        
        _controller.Move(moveDir * _smoothSpeed * Time.deltaTime);
    }

    private void AttemptDash()
    {
        if (_isDashing) return;
        if (_combat != null && _combat.IsAttacking) return; // Can't dash mid-attack (optional)

        _isDashing = true;
        _dashTimer = dashDuration;

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