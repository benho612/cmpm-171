using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SifuMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float rotationSpeed = 15f; // High value for snappy "Sifu" turns

    [Header("Physics")]
    public float gravity = -9.81f;
    public float gravityMultiplier = 2.0f; // Games often need higher gravity to feel grounded

    // Internal Variables
    private CharacterController _controller;
    private PlayerControls _input;
    private Transform _cameraTransform;
    private Vector3 _velocity;
    private Vector2 _moveInput;
    
    // Dash Logic
    private bool _isDashing;
    private float _dashTimer;
    private Vector3 _dashDirection;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _cameraTransform = Camera.main.transform;
        
        // Initialize Input System
        _input = new PlayerControls();
        
        // Subscribe to input events
        _input.Gameplay.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _input.Gameplay.Move.canceled += ctx => _moveInput = Vector2.zero;
        
        _input.Gameplay.Dash.performed += ctx => AttemptDash();
    }

    private void OnEnable() => _input.Enable();
    private void OnDisable() => _input.Disable();

    private void Update()
    {
        ApplyGravity();
        
        if (_isDashing)
        {
            HandleDash();
        }
        else
        {
            HandleMovement();
        }
    }

    private void HandleMovement()
    {
        // 1. If no input, stop moving
        if (_moveInput.magnitude < 0.1f) return;

        // 2. Calculate direction relative to Camera
        // We project the camera vectors onto the flat ground (y=0) so looking up/down doesn't slow us
        Vector3 camForward = _cameraTransform.forward;
        Vector3 camRight = _cameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = (camForward * _moveInput.y + camRight * _moveInput.x).normalized;

        // 3. Rotate Character (Sifu Style: Snappy turns)
        Quaternion targetRotation = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // 4. Move Character
        _controller.Move(moveDir * walkSpeed * Time.deltaTime);
    }

    private void AttemptDash()
    {
        if (_isDashing) return; // Prevent spamming

        _isDashing = true;
        _dashTimer = dashDuration;

        // Dash towards input direction, or forward if standing still
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
        // Move rapidly in the dash direction
        _controller.Move(_dashDirection * dashSpeed * Time.deltaTime);
        
        // Keep character facing the dash direction
        transform.rotation = Quaternion.LookRotation(_dashDirection);

        _dashTimer -= Time.deltaTime;
        if (_dashTimer <= 0)
        {
            _isDashing = false;
            // Optional: slight cooldown could go here
        }
    }

    private void ApplyGravity()
    {
        // Reset gravity accumulation when grounded
        if (_controller.isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f; // Small stick-to-ground force
        }

        _velocity.y += gravity * gravityMultiplier * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }
}