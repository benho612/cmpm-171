using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerBlendTreeStrafeRun : MonoBehaviour
{
    [Header("Speeds")]
    public float walkSpeed = 2.5f;
    public float runForwardSpeed = 6.0f;
    public float runStrafeSpeed = 5.0f;
    public float battleWalkSpeed = 1.6f;

    [Header("Speed Multipliers")]
    [Range(0.5f, 1f)]
    public float diagonalSpeedMultiplier = 0.85f;     // W+A / W+D / S+A / S+D slower
    [Range(0.5f, 1f)]
    public float backwardSpeedMultiplier = 0.80f;     // S slower than W

    [Header("Rotation")]
    public float rotationSpeed = 12f;

    [Header("References")]
    public Transform cameraTransform;
    public Animator animator;

    [Header("Blend Tree Params")]
    public string moveXParam = "MoveX";
    public string moveYParam = "MoveY";

    [Header("Gravity")]
    public float gravity = -20f;
    public float groundedStickForce = -2f;

    [Header("Battle Mode")]
    public float battleDuration = 5f;
    public string battleBool = "isBattle";

    [Header("Punch")]
    public string punchTrigger = "right_punch";

    CharacterController controller;
    Vector2 moveInput;
    bool shiftHeld;
    bool isBattle;

    float yVelocity;
    Coroutine battleRoutine;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
    }

    // PlayerInput (Invoke Unity Events)
    public void OnMove(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    public void OnRun(InputAction.CallbackContext ctx) => shiftHeld = !ctx.canceled;

    public void OnBattleToggle(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        SetBattle(true);

        if (battleRoutine != null) StopCoroutine(battleRoutine);
        battleRoutine = StartCoroutine(BattleTimeout());
    }

    IEnumerator BattleTimeout()
    {
        yield return new WaitForSeconds(battleDuration);
        SetBattle(false);
        battleRoutine = null;
    }

    public void OnPunch(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        // Optional: punching forces battle mode + refresh timer
        SetBattle(true);
        if (battleRoutine != null) StopCoroutine(battleRoutine);
        battleRoutine = StartCoroutine(BattleTimeout());

        animator?.SetTrigger(punchTrigger);
    }

    void SetBattle(bool value)
    {
        isBattle = value;
        animator?.SetBool(battleBool, value);
    }

    void Update()
    {
        Vector2 input = Vector2.ClampMagnitude(moveInput, 1f);

        bool isMoving = input.sqrMagnitude > 0.001f;
        if (!isMoving) input = Vector2.zero;

        bool isDiagonal = Mathf.Abs(input.x) > 0.1f && Mathf.Abs(input.y) > 0.1f;
        bool isBackward = input.y < -0.1f;

        // Always face camera forward (strafe mode)
        if (cameraTransform)
        {
            Vector3 faceDir = cameraTransform.forward;
            faceDir.y = 0f;
            if (faceDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(faceDir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }

        // Run conditions:
        // - only normal mode
        // - Shift held
        // - only forward/strafe (NO backward run)
        bool canRun = !isBattle && shiftHeld && (input.y >= 0f) && isMoving;

        // Base speed
        float speed;
        if (isBattle)
        {
            speed = battleWalkSpeed;
        }
        else if (canRun)
        {
            // If mostly strafing, use strafe run speed; if mostly forward, use forward run speed
            speed = (Mathf.Abs(input.x) > Mathf.Abs(input.y)) ? runStrafeSpeed : runForwardSpeed;
        }
        else
        {
            speed = walkSpeed;
        }

        // Apply backward + diagonal slowdowns
        if (isBackward) speed *= backwardSpeedMultiplier;
        if (isDiagonal) speed *= diagonalSpeedMultiplier;

        // Camera-relative move direction
        Vector3 camForward = Vector3.forward;
        Vector3 camRight = Vector3.right;

        if (cameraTransform)
        {
            camForward = cameraTransform.forward;
            camRight = cameraTransform.right;
            camForward.y = 0f; camRight.y = 0f;
            camForward.Normalize(); camRight.Normalize();
        }

        Vector3 moveDirWorld = camRight * input.x + camForward * input.y;

        // Gravity / grounding
        if (controller.isGrounded && yVelocity < 0f) yVelocity = groundedStickForce;
        yVelocity += gravity * Time.deltaTime;

        Vector3 velocity = moveDirWorld * speed;
        velocity.y = yVelocity;
        controller.Move(velocity * Time.deltaTime);

        // Blend tree values:
        // Walk region: -1..1
        // Run region:  -2..2 (hits run clips placed at (กำ2,0) and (0,2))
        float blendMul = canRun ? 2f : 1f;

        if (animator)
        {
            animator.SetFloat(moveXParam, input.x * blendMul, 0.10f, Time.deltaTime);
            animator.SetFloat(moveYParam, input.y * blendMul, 0.10f, Time.deltaTime);
        }
    }
}
