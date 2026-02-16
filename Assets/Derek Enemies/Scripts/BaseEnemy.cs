using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class BaseEnemy : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] protected float maxHealth = 100f;
    protected float currentHealth;

    [Header("Movement")]
    [SerializeField] protected float chaseSpeed = 4f;
    [SerializeField] protected float circleSpeed = 2f;
    [SerializeField] protected float attackRange = 2f;
    [SerializeField] protected float chargeSpeed = 5f;
    [SerializeField] protected float chargeStopDistance = 2f;
    [SerializeField] protected float dashBackDistance = 3f;
    [SerializeField] protected float dashBackSpeed = 8f;

    [Header("Awareness & Engagement")]
    [SerializeField] protected float awarenessRange = 25f;
    [SerializeField] protected float engagementRange = 15f;

    [Header("Attack Damage")]
    [SerializeField] protected float lightAttackDamage = 10f;
    [SerializeField] protected float heavyAttackDamage = 25f;
    [SerializeField] protected float chargeAttackDamage = 20f;

    [Header("Timing")]
    [SerializeField] protected float stunDuration = 2f;
    [SerializeField] protected float attackCooldown = 1.5f;

    [Header("Hit Stun")]
    [SerializeField] protected float maxHitStunDuration = 5f; // Max time in hit stun before dash back
    [SerializeField] protected float hitStunResetTime = 1f; // Time without being hit to reset hit stun
    protected float hitStunTimer;
    protected float timeSinceLastHit;
    protected bool isInHitStun;
    protected bool isDashingBack;

    [Header("Stun Meter")]
    [SerializeField] protected float maxStunMeter = 100f;
    [SerializeField] protected float stunDecayRate = 10f;
    protected float currentStunMeter;

    [Header("Recovery")]
    [SerializeField] protected float hitRecoveryTime = 0.5f;
    protected bool isRecovering;

    [Header("References")]
    [SerializeField] protected Transform player;
    [SerializeField] protected Animator animator;

    // Components
    protected NavMeshAgent navAgent;

    // State
    protected bool isBlocking;
    protected bool isAttacking;
    protected bool isCharging;
    protected bool isStunned;
    protected bool isAware;
    protected bool isEngaged;
    protected bool hasOpenedWithCharge;
    protected float attackCooldownTimer;

    // Animation parameter hashes
    protected static readonly int AnimLightAttack = Animator.StringToHash("LightAttack");
    protected static readonly int AnimHeavyAttack = Animator.StringToHash("HeavyAttack");
    protected static readonly int AnimChargeAttack = Animator.StringToHash("ChargeAttack");
    protected static readonly int AnimLightRandom = Animator.StringToHash("LightRandom");
    protected static readonly int AnimBlock = Animator.StringToHash("Block");
    protected static readonly int AnimHitReaction = Animator.StringToHash("HitReaction");
    protected static readonly int AnimDie = Animator.StringToHash("Die");
    protected static readonly int AnimSpeed = Animator.StringToHash("Speed");
    protected static readonly int AnimBlockHit = Animator.StringToHash("BlockHit");
    // TODO: Add "Stunned" bool parameter to Animator Controller when animation is ready
    protected static readonly int AnimStunned = Animator.StringToHash("Stunned");
    // TODO: Add "DashBack" trigger parameter to Animator Controller when animation is ready
    protected static readonly int AnimDashBack = Animator.StringToHash("DashBack");

    private PlayerHealth playerHealth;

    protected virtual void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();

        // Only get Animator if not already assigned in Inspector
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        if (animator == null)
        {
            Debug.LogError($"{gameObject.name}: No Animator found! Check that Animator is on this GameObject or a child.", this);
        }

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                Debug.LogWarning($"{name}: Player found but has no PlayerHealth component!");
            }
        }
        else
        {
            Debug.LogWarning($"{name}: Player not found!");
        }
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        navAgent.speed = chaseSpeed;
        navAgent.isStopped = true;
        isAware = false;
        isEngaged = false;
        hasOpenedWithCharge = false;

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        if (EnemyCombatManager.Instance != null)
        {
            EnemyCombatManager.Instance.RegisterEnemy(this);
        }
    }

    protected virtual void OnDestroy()
    {
        if (EnemyCombatManager.Instance != null)
        {
            EnemyCombatManager.Instance.UnregisterEnemy(this);
        }
    }

    protected virtual void Update()
    {
        if (currentHealth <= 0) return;

        // Update hit stun timer
        if (isInHitStun)
        {
            hitStunTimer += Time.deltaTime;
            timeSinceLastHit += Time.deltaTime;

            // If hit stun duration exceeded, dash back
            if (hitStunTimer >= maxHitStunDuration)
            {
                StartCoroutine(DashBackRoutine());
            }
            // If not hit for a while, reset hit stun
            else if (timeSinceLastHit >= hitStunResetTime)
            {
                ExitHitStun();
            }

            return; // Don't do normal behavior while in hit stun
        }

        // Don't do anything while dashing back
        if (isDashingBack) return;

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (!isAware)
        {
            CheckAwareness();
        }
        else if (!isEngaged)
        {
            CheckEngagement();
        }
        else
        {
            if (ShouldWaitForTurn())
            {
                CircleAroundPlayer();
            }
            else
            {
                ContinueCombat();
            }
        }

        if (isCharging)
        {
            UpdateChargeAttack();
        }

        if (isAware && !isStunned)
        {
            FacePlayer();
        }

        UpdateAnimatorParameters();
    }

    protected bool ShouldWaitForTurn()
    {
        if (EnemyCombatManager.Instance == null) return false;
        return EnemyCombatManager.Instance.ShouldWait(this);
    }

    protected virtual void CircleAroundPlayer()
    {
        if (isAttacking || isBlocking || isStunned || isCharging || isInHitStun || isDashingBack) return;
        if (EnemyCombatManager.Instance == null) return;

        Vector3 targetPos = EnemyCombatManager.Instance.GetCirclePosition(this);

        navAgent.speed = circleSpeed;
        navAgent.isStopped = false;
        navAgent.SetDestination(targetPos);
    }

    public bool IsBlockingOrStunned()
    {
        return isBlocking || isStunned || isInHitStun;
    }

    protected void UpdateAnimatorParameters()
    {
        if (animator == null) return;

        Vector3 worldVelocity = navAgent.velocity;
        float rawSpeed = worldVelocity.magnitude;

        Vector3 localVelocityDir = transform.InverseTransformDirection(worldVelocity.normalized);

        animator.SetFloat(AnimSpeed, rawSpeed);
        animator.SetFloat("VelocityX", localVelocityDir.x);
        animator.SetFloat("VelocityZ", localVelocityDir.z);
    }

    private void OnAnimatorMove()
    {
        if (animator == null) return;

        if (isAttacking || isDashingBack || isInHitStun)
        {
            navAgent.updatePosition = false;
            transform.position += animator.deltaPosition;
            navAgent.nextPosition = transform.position;
        }
        else
        {
            navAgent.updatePosition = true;
        }
    }

    protected virtual void ContinueCombat()
    {
        if (isAttacking || isBlocking || isCharging || isStunned || isInHitStun || isDashingBack) return;

        float distance = GetDistanceToPlayer();

        if (distance <= attackRange)
        {
            if (EnemyCombatManager.Instance == null ||
                EnemyCombatManager.Instance.RequestAttackPermission(this))
            {
                FacePlayer();
                LightAttack();
            }
        }
        else
        {
            ChasePlayer();
            FacePlayer();
        }
    }

    protected virtual void CheckAwareness()
    {
        if (player == null) return;

        float distance = GetDistanceToPlayer();

        if (distance <= awarenessRange)
        {
            isAware = true;
            navAgent.isStopped = false;
        }
    }

    protected virtual void CheckEngagement()
    {
        if (player == null) return;

        float distance = GetDistanceToPlayer();

        if (distance <= engagementRange)
        {
            isEngaged = true;

            if (CanPerformAction())
            {
                if (EnemyCombatManager.Instance == null ||
                    EnemyCombatManager.Instance.RequestAttackPermission(this))
                {
                    ChargeAttack();
                    hasOpenedWithCharge = true;
                }
                else
                {
                    hasOpenedWithCharge = true;
                }
            }
        }
        else if (isAware && !isAttacking && !isBlocking && !isCharging)
        {
            ChasePlayer();
            FacePlayer();
        }
    }

    public bool HasCompletedOpener()
    {
        return isEngaged && hasOpenedWithCharge && !isCharging && !isAttacking;
    }

    public bool IsAware() => isAware;
    public bool IsEngaged() => isEngaged;
    public bool IsInHitStun() => isInHitStun;

    #region Movement
    protected virtual void ChasePlayer()
    {
        if (isAttacking || isBlocking || isStunned || isCharging || isInHitStun || isDashingBack || player == null) return;

        navAgent.isStopped = false;
        navAgent.speed = chaseSpeed;
        navAgent.SetDestination(player.position);
    }

    protected float GetDistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Vector3.Distance(transform.position, player.position);
    }

    protected void FacePlayer()
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }
    #endregion

    #region Combat Actions
    public virtual void LightAttack()
    {
        if (!CanPerformAction()) return;

        isAttacking = true;
        attackCooldownTimer = attackCooldown;
        navAgent.isStopped = true;
        navAgent.velocity = Vector3.zero;

        int randomIndex = Random.Range(0, 3);
        animator?.SetFloat(AnimLightRandom, randomIndex);
        animator?.SetTrigger(AnimLightAttack);
    }

    public virtual void HeavyAttack()
    {
        if (!CanPerformAction()) return;

        isAttacking = true;
        attackCooldownTimer = attackCooldown * 1.5f;
        navAgent.isStopped = true;
        navAgent.velocity = Vector3.zero;

        animator?.SetTrigger(AnimHeavyAttack);
    }

    public virtual void StartBlock()
    {
        if (isAttacking || isStunned || isCharging || isInHitStun || isDashingBack) return;

        isBlocking = true;
        navAgent.isStopped = true;
        animator?.SetBool(AnimBlock, true);
    }

    public virtual void StopBlock()
    {
        isBlocking = false;
        animator?.SetBool(AnimBlock, false);
    }

    public virtual void ChargeAttack()
    {
        if (!CanPerformAction() || player == null) return;

        isCharging = true;
        attackCooldownTimer = attackCooldown * 1.8f;

        navAgent.isStopped = false;
        navAgent.updatePosition = true;
        navAgent.speed = chargeSpeed;
        navAgent.SetDestination(player.position);
    }

    protected virtual void UpdateChargeAttack()
    {
        if (player == null)
        {
            EndCharge();
            return;
        }

        float distanceToPlayer = GetDistanceToPlayer();

        if (distanceToPlayer <= chargeStopDistance)
        {
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero;
            navAgent.ResetPath();

            isCharging = false;
            isAttacking = true;

            FacePlayerImmediate();
            animator?.SetTrigger(AnimChargeAttack);
        }
        else
        {
            navAgent.speed = chargeSpeed;
            navAgent.isStopped = false;
            navAgent.SetDestination(player.position);
        }
    }

    protected void FacePlayerImmediate()
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    protected void EndCharge()
    {
        isCharging = false;
        navAgent.velocity = Vector3.zero;
        navAgent.speed = chaseSpeed;
    }

    protected virtual bool CanPerformAction()
    {
        return !isAttacking && !isBlocking && !isStunned && !isCharging && !isInHitStun && !isDashingBack && attackCooldownTimer <= 0;
    }
    #endregion

    #region Damage & Health
    public virtual void TakeDamage(float damage)
    {
        Debug.Log($"=== {gameObject.name} TakeDamage({damage}) CALLED ===");

        if (IsDead()) return;

        // Become aware and engaged when attacked
        if (!isAware)
        {
            isAware = true;
            navAgent.isStopped = false;
        }
        if (!isEngaged)
        {
            isEngaged = true;
            hasOpenedWithCharge = true;
        }

        // Check if blocking
        if (isBlocking)
        {
            currentStunMeter += damage * 0.5f;

            if (currentStunMeter >= maxStunMeter)
            {
                BreakGuard();
                return;
            }

            animator?.SetTrigger(AnimBlockHit);
            Debug.Log($"{gameObject.name} blocked! Stun meter: {currentStunMeter}/{maxStunMeter}");
            return;
        }

        // Take damage
        currentHealth -= damage;
        Debug.Log($"{gameObject.name}: Health now {currentHealth}/{maxHealth}");

        // Show health bar when damaged
        var healthBar = GetComponentInChildren<EnemyHealthBar>();
        healthBar?.ShowHealthBar();

        // Interrupt current actions
        isAttacking = false;
        isCharging = false;
        navAgent.isStopped = true;
        navAgent.velocity = Vector3.zero;

        // Release attack permission
        if (EnemyCombatManager.Instance != null)
        {
            EnemyCombatManager.Instance.ReleaseAttackPermission(this);
        }

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // Enter or continue hit stun
        EnterHitStun();

        // Play hit reaction animation
        Debug.Log($"{gameObject.name}: Triggering HitReaction animation");
        animator?.SetTrigger(AnimHitReaction);
    }
    #endregion

    public virtual void BreakGuard()
    {
        isBlocking = false;
        animator?.SetBool(AnimBlock, false);
        currentStunMeter = 0f;

        Debug.Log($"{gameObject.name}'s guard was broken!");
        ApplyStun(stunDuration);
    }

    protected virtual void Die()
    {
        isAttacking = false;
        isBlocking = false;
        isCharging = false;
        isRecovering = false;
        isInHitStun = false;
        isDashingBack = false;
        navAgent.isStopped = true;
        navAgent.velocity = Vector3.zero;
        navAgent.enabled = false;

        if (EnemyCombatManager.Instance != null)
        {
            EnemyCombatManager.Instance.ReleaseAttackPermission(this);
        }

        animator?.SetTrigger(AnimDie);

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        enabled = false;
    }

    public bool IsDead() => currentHealth <= 0;
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public float GetStunPercentage() => currentStunMeter / maxStunMeter;
    public bool IsRecovering() => isRecovering;

    #region Animation Events
    public void OnAttackEnd()
    {
        isAttacking = false;
        isCharging = false;

        navAgent.updatePosition = true;
        navAgent.nextPosition = transform.position;

        if (EnemyCombatManager.Instance != null)
        {
            EnemyCombatManager.Instance.ReleaseAttackPermission(this);
        }

        FacePlayerImmediate();

        if (!isStunned && !isInHitStun && navAgent.isOnNavMesh)
        {
            navAgent.speed = chaseSpeed;
            navAgent.isStopped = false;
        }
    }

    public void OnDashBackEnd()
    {
        isDashingBack = false;
        isInHitStun = false;
        navAgent.updatePosition = true;
        navAgent.nextPosition = transform.position;

        if (!isStunned && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = false;
        }
    }

    public void OnLightAttackHit()
    {
        TryDamagePlayer(lightAttackDamage, attackRange);
    }

    public void OnHeavyAttackHit()
    {
        TryDamagePlayer(heavyAttackDamage, attackRange * 1.2f);
    }

    public void OnChargeAttackHit()
    {
        TryDamagePlayer(chargeAttackDamage, attackRange * 1.5f);
    }

    protected bool TryDamagePlayer(float damage, float effectiveRange)
    {
        if (player == null) return false;

        float distance = Vector3.Distance(transform.position, player.position);
        
        Debug.Log($"{gameObject.name}: Attempting to hit player - Distance: {distance:F2}, EffectiveRange: {effectiveRange:F2}");
        
        if (distance <= effectiveRange)
        {
            if (playerHealth != null)
            {
                float healthBefore = playerHealth.HealthPercentage * 100f;
                playerHealth.TakeDamage(damage);
                float healthAfter = playerHealth.HealthPercentage * 100f;
                
                Debug.Log($"=== PLAYER HIT ===");
                Debug.Log($"  Attacker: {gameObject.name}");
                Debug.Log($"  Damage: {damage}");
                Debug.Log($"  Distance: {distance:F2}");
                Debug.Log($"  Player Health: {healthBefore:F1}% -> {healthAfter:F1}%");
                
                if (playerHealth.IsDead)
                {
                    Debug.Log($"  >>> PLAYER KILLED BY {gameObject.name}! <<<");
                }
                
                return true;
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: Cannot damage player - PlayerHealth component not found!");
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Call this from the HitReaction animation event at the end of the clip
    /// </summary>
    public void OnHitReactionEnd()
    {
        // Don't auto-exit hit stun here - let the timer handle it
        // This just ensures the animator state is ready for the next hit
        Debug.Log($"{gameObject.name}: Hit reaction animation ended");
    }
    #endregion

    #region Stun
    public virtual void ApplyStun(float duration)
    {
        if (isStunned) return;
        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        isAttacking = false;
        isBlocking = false;
        isCharging = false;
        isInHitStun = false;

        navAgent.isStopped = true;
        navAgent.velocity = Vector3.zero;

        // TODO: Uncomment when Stunned animation is ready
        // animator?.SetBool(AnimStunned, true);

        yield return new WaitForSeconds(duration);

        // TODO: Uncomment when Stunned animation is ready
        // animator?.SetBool(AnimStunned, false);
        isStunned = false;

        if (navAgent.isOnNavMesh)
        {
            navAgent.isStopped = false;
        }
    }
    #endregion

    /// <summary>
    /// Enter hit stun state - can be hit multiple times
    /// </summary>
    protected virtual void EnterHitStun()
    {
        if (!isInHitStun)
        {
            isInHitStun = true;
            hitStunTimer = 0f;
            Debug.Log($"{gameObject.name} entered hit stun!");
        }

        // Reset time since last hit (allows combo to continue)
        timeSinceLastHit = 0f;
    }

    /// <summary>
    /// Exit hit stun and return to normal behavior
    /// </summary>
    protected virtual void ExitHitStun()
    {
        isInHitStun = false;
        hitStunTimer = 0f;
        timeSinceLastHit = 0f;

        if (!isStunned && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = false;
        }

        Debug.Log($"{gameObject.name} exited hit stun!");
    }

    protected IEnumerator DashBackRoutine()
    {
        // Prevent multiple dash backs from starting
        if (isDashingBack) yield break;

        isDashingBack = true;
        isInHitStun = false;
        navAgent.isStopped = true;
        navAgent.velocity = Vector3.zero;

        // TODO: Uncomment when DashBack animation is ready
        // animator?.SetTrigger(AnimDashBack);

        Vector3 dashDirection = player != null
            ? -(player.position - transform.position).normalized
            : -transform.forward;
        dashDirection.y = 0f;

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + dashDirection * dashBackDistance;

        float dashDuration = dashBackDistance / dashBackSpeed;
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dashDuration);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;

        isDashingBack = false;
        hitStunTimer = 0f;
        timeSinceLastHit = 0f;
        navAgent.updatePosition = true;
        navAgent.nextPosition = transform.position;

        if (!isStunned && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = false;
        }

        Debug.Log($"{gameObject.name} completed dash back!");
    }
}