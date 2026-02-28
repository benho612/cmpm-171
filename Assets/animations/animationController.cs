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

    [Header("Finisher")]
    public string finisherTrigger = "finisher";
    [Tooltip("MUST match the finisher state's name in Animator exactly.")]
    public string finisherStateName = "Finisher";
    public float finisherCooldown = 0.35f;

    [Header("Finisher Target (Trigger Zone)")]
    public EnemyFinisherTarget currentFinisherTarget;

    [Tooltip("Max seconds allowed for finisher. Prevents permanent lock if transitions are wrong.")]
    public float maxFinisherTime = 3.0f;

    [Header("Cinemachine (optional)")]
    // Cinemachine 3 uses CinemachineCamera. If you're on Cinemachine 2, use CinemachineVirtualCamera instead.
    public Unity.Cinemachine.CinemachineCamera finisherVCam;
    public int finisherCamPriority = 50;

    Vector3 preFinisherPos;
    Quaternion preFinisherRot;
    bool hasPreFinisherTransform;

    CharacterController controller;
    Vector2 moveInput;
    bool shiftHeld;
    bool isBattle;

    Coroutine battleRoutine;

    bool isDodging;
    float lastDodgeTime = -999f;

    bool isFinishing;
    float lastFinisherTime = -999f;

    int cachedFinisherCamPriority;
    bool hasCachedFinisherCamPriority;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
    }

    void OnDisable()
    {
        // Safety: if script gets disabled mid-finisher, always unlock.
        ForceUnlock();
    }

    void SavePreFinisherTransform()
    {
        preFinisherPos = transform.position;
        preFinisherRot = transform.rotation;
        hasPreFinisherTransform = true;
    }

    void RestorePreFinisherTransform()
    {
        if (!hasPreFinisherTransform) return;

        bool ccWasEnabled = controller != null && controller.enabled;
        if (controller) controller.enabled = false;

        transform.position = preFinisherPos;
        transform.rotation = preFinisherRot;

        if (controller && ccWasEnabled) controller.enabled = true;

        hasPreFinisherTransform = false;

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

    // Hook this action to F
    public void OnFinisher(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        TryFinisher();
    }

    void TryFinisher()
    {
        if (isDodging || isFinishing) return;
        if (Time.time < lastFinisherTime + finisherCooldown) return;

        if (currentFinisherTarget == null)
        {
            Debug.Log("No finisher target in range.");
            return;
        }

        StartCoroutine(FinisherRoutine(currentFinisherTarget));
    }

    IEnumerator AlignToAnchor(Transform anchor, float duration)
    {
        if (!anchor) yield break;

        // Temporarily disable CharacterController while snapping (prevents ¡§fight¡¨)
        bool ccWasEnabled = controller != null && controller.enabled;
        if (controller) controller.enabled = false;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 endPos = anchor.position;
        Quaternion endRot = anchor.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(duration, 0.0001f);
            transform.position = Vector3.Lerp(startPos, endPos, t);
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        if (controller && ccWasEnabled) controller.enabled = true;
    }

    IEnumerator FinisherRoutine(EnemyFinisherTarget target)
    {
        isFinishing = true;
        lastFinisherTime = Time.time;

        SavePreFinisherTransform();

        if (target.FinisherAnchor != null)
            yield return AlignToAnchor(target.FinisherAnchor, 0.15f);

        EnableFinisherCam(true);

        target.PlayFinisherReact();

        animator?.ResetTrigger(finisherTrigger);
        animator?.SetTrigger(finisherTrigger);

        // 1) Wait until we ENTER finisher state, but never forever
        float enterTimeout = 1.0f;
        float enterT = 0f;
        while (enterT < enterTimeout)
        {
            if (animator && animator.GetCurrentAnimatorStateInfo(0).IsName(finisherStateName))
                break;

            enterT += Time.deltaTime;
            yield return null;
        }

        // If never entered, unlock safely (state name mismatch or transition failed)
        if (!(animator && animator.GetCurrentAnimatorStateInfo(0).IsName(finisherStateName)))
        {
            ForceUnlock();
            yield break;
        }

        // 2) Wait until we LEAVE finisher state, but also never forever
        float elapsed = 0f;
        while (elapsed < maxFinisherTime)
        {
            if (animator && !animator.GetCurrentAnimatorStateInfo(0).IsName(finisherStateName))
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        ForceUnlock();
    }

    void ForceUnlock()
    {
        // End finisher lock
        isFinishing = false;

        // Restore camera
        EnableFinisherCam(false);

        RestorePreFinisherTransform();

    }

    void EnableFinisherCam(bool on)
    {
        if (!finisherVCam) return;

        if (!hasCachedFinisherCamPriority)
        {
            cachedFinisherCamPriority = finisherVCam.Priority;
            hasCachedFinisherCamPriority = true;
        }

        finisherVCam.Priority = on ? finisherCamPriority : cachedFinisherCamPriority;
    }

    private void OnTriggerEnter(Collider other)
    {
        var zone = other.GetComponent<EnemyFinisherZone>();
        if (zone != null && zone.target != null)
            currentFinisherTarget = zone.target;
    }

    private void OnTriggerExit(Collider other)
    {
        var zone = other.GetComponent<EnemyFinisherZone>();
        if (zone != null && zone.target == currentFinisherTarget)
            currentFinisherTarget = null;
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

        // Wait until we LEAVE the dodge state
        while (animator && animator.GetCurrentAnimatorStateInfo(0).IsName(dodgeStateName))
            yield return null;

        isDodging = false;
    }

    void Update()
    {
        if (isDodging || isFinishing)
        {
            // Freeze blend tree input while dodging/finishing
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

        // Locomotion uses CharacterController. Root motion can still drive dodge/finisher animations.
        Vector3 velocity = moveDirWorld * speed;
        velocity.y = -2f;
        controller.Move(velocity * Time.deltaTime);

        float blendMul = canRun ? 2f : 1f;
        if (animator)
        {
            animator.SetFloat(moveXParam, input.x * blendMul, 0.10f, Time.deltaTime);
            animator.SetFloat(moveYParam, input.y * blendMul, 0.10f, Time.deltaTime);
        }
    }
}