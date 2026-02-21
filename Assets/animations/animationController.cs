using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerBlendTreeController_WithRootDodge : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 2.5f;
    public float runForwardSpeed = 6.0f;
    public float runStrafeSpeed = 5.0f;
    public float battleWalkSpeed = 1.6f;

    [Header("Speed Multipliers")]
    [Range(0.5f, 1f)] public float diagonalSpeedMultiplier = 0.85f;
    [Range(0.5f, 1f)] public float backwardSpeedMultiplier = 0.80f;

    [Header("Rotation")]
    public float rotationSpeed = 12f;

    [Header("References")]
    public Transform cameraTransform;
    public Animator animator;

    [Header("Blend Tree Params")]
    public string moveXParam = "MoveX";
    public string moveYParam = "MoveY";

    [Header("Battle Mode")]
    public float battleDuration = 5f;
    public string battleBool = "isBattle";

    [Header("Punch")]
    public string punchTrigger = "right_punch";

    [Header("Dodge")]
    public bool dodgeOnlyInBattle = true;
    public string dodgeTrigger = "dodge";
    public string dodgeStateName = "Dodge Backward"; 
    public float dodgeCooldown = 0.35f;

    [Header("Block")]
    public bool blockOnlyInBattle = true;
    public string blockTrigger = "block";
    public string blockStateName = "Standing Block";
    public float blockCooldown = 0.35f;

    CharacterController controller;
    Vector2 moveInput;
    bool shiftHeld;
    bool isBattle;

    Coroutine battleRoutine;

    bool isDodging;
    float lastDodgeTime = -999f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;

        // IMPORTANT: do NOT override applyRootMotion here since you want it enabled already.
        // animator.applyRootMotion = true/false;  <-- removed on purpose
    }

    // PlayerInput events (New Input System)
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

        SetBattle(true);
        if (battleRoutine != null) StopCoroutine(battleRoutine);
        battleRoutine = StartCoroutine(BattleTimeout());

        animator?.SetTrigger(punchTrigger);
    }

    public void OnDodge(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        TryDodge();
    }

    void SetBattle(bool value)
    {
        isBattle = value;
        animator?.SetBool(battleBool, value);
    }

    void TryDodge()
    {
        if (isDodging) return;
        if (Time.time < lastDodgeTime + dodgeCooldown) return;
        if (dodgeOnlyInBattle && !isBattle) return;

        StartCoroutine(DodgeRoutine());
    }

    IEnumerator DodgeRoutine()
    {
        isDodging = true;
        lastDodgeTime = Time.time;

        animator?.SetTrigger(dodgeTrigger);

        // Wait until we ENTER the dodge state (with a timeout so we never get stuck)
        float t = 0f;
        while (t < 1.0f)
        {
            if (animator && animator.GetCurrentAnimatorStateInfo(0).IsName(dodgeStateName))
                break;

            t += Time.deltaTime;
            yield return null;
        }

        // If we never entered dodge state, don't lock player forever
        if (!(animator && animator.GetCurrentAnimatorStateInfo(0).IsName(dodgeStateName)))
        {
            isDodging = false;
            yield break;
        }

        // Wait until we LEAVE the dodge state (this is the key fix)
        while (animator && animator.GetCurrentAnimatorStateInfo(0).IsName(dodgeStateName))
            yield return null;

        isDodging = false;
    }

    void Update()
    {
        if (isDodging)
        {
            // Freeze blend tree input while dodging
            if (animator)
            {
                animator.SetFloat(moveXParam, 0f, 0.08f, Time.deltaTime);
                animator.SetFloat(moveYParam, 0f, 0.08f, Time.deltaTime);
            }
            return;
        }

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

        // Run only in normal mode, no backward run
        bool canRun = !isBattle && shiftHeld && (input.y >= 0f) && isMoving;

        float speed;
        if (isBattle) speed = battleWalkSpeed;
        else if (canRun) speed = (Mathf.Abs(input.x) > Mathf.Abs(input.y)) ? runStrafeSpeed : runForwardSpeed;
        else speed = walkSpeed;

        if (isBackward) speed *= backwardSpeedMultiplier;
        if (isDiagonal) speed *= diagonalSpeedMultiplier;

        // Camera-relative movement direction
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

        // If you're using root motion for dodge only, it's fine to still move via CC for locomotion.
        // Stick to ground:
        Vector3 velocity = moveDirWorld * speed;
        velocity.y = -2f;
        controller.Move(velocity * Time.deltaTime);

        // Blend tree params (run region uses กำ2 and 0,2 if you set it up that way)
        float blendMul = canRun ? 2f : 1f;
        if (animator)
        {
            animator.SetFloat(moveXParam, input.x * blendMul, 0.10f, Time.deltaTime);
            animator.SetFloat(moveYParam, input.y * blendMul, 0.10f, Time.deltaTime);
        }
    }
}
