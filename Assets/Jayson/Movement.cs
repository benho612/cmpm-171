using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Movement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;
    
    [Header("Dash Settings (Mobility)")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    
    [Tooltip("How fast the character turns while sprinting/walking")]
    public float rotationSpeed = 15f; 

    [Header("Physics")]
    public float gravity = -9.81f;
    public float gravityMultiplier = 2.0f;

    // Internal Variables
    private CharacterController _controller;
    private PlayerControls _input;
    private Transform _cameraTransform;
    private Combat _combat; 
    
    private Vector3 _velocity;
    private Vector2 _moveInput;
    private float _smoothSpeed;
    
    // Dash States
    private bool _isDashing;
    private float _dashTimer;
    private Vector3 _dashDirection;

    public bool IsDashing => _isDashing;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _combat = GetComponent<Combat>(); 
        _cameraTransform = Camera.main.transform;
        
        _input = new PlayerControls();
        
        _input.Gameplay.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _input.Gameplay.Move.canceled += ctx => _moveInput = Vector2.zero;
        
        // Listen for Dash action
        _input.Gameplay.Dash.performed += ctx => AttemptDash();
    }

    private void OnEnable() => _input.Enable();
    private void OnDisable() => _input.Disable();

    private void Update()
    {
        ApplyGravity();

        if (Keyboard.current.leftAltKey.wasPressedThisFrame)
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = Cursor.lockState == CursorLockMode.None;
        }

        if (_isDashing)
        {
            HandleDash();
            return;
        }

        // Halt movement if attacking or actively holding block/dodging
        if (_combat != null && (_combat.IsAttacking || _combat.IsBlocking))
        {
            _smoothSpeed = 0; 
            return;
        }

        //Standard Movement
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (_moveInput.magnitude < 0.1f) return;

        bool isSprinting = false;

        Vector3 camForward = _cameraTransform.forward;
        Vector3 camRight = _cameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = (camForward * _moveInput.y + camRight * _moveInput.x).normalized;

        if (moveDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
        _smoothSpeed = Mathf.Lerp(_smoothSpeed, targetSpeed, 10f * Time.deltaTime);
        
        _controller.Move(moveDir * _smoothSpeed * Time.deltaTime);
    }

    private void AttemptDash()
    {
        if (_isDashing || (_combat != null && (_combat.IsAttacking || _combat.IsBlocking))) return;

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