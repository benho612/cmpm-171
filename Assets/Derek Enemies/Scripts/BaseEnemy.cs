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

    [Header("References")]
    [SerializeField] protected Transform player;
    [SerializeField] protected Animator animator;

    [Header("Stun Meter")]
    [SerializeField] protected float maxStunMeter = 100f;
    [SerializeField] protected float stunDecayRate = 10f;
    protected float currentStunMeter;

    [Header("Recovery")]
    [SerializeField] protected float hitRecoveryTime = 0.5f;
    protected bool isRecovering;

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
    protected static readonly int AnimHit = Animator.StringToHash("Hit");
    protected static readonly int AnimDie = Animator.StringToHash("Die");
    protected static readonly int AnimSpeed = Animator.StringToHash("Speed");
    protected static readonly int AnimBlockHit = Animator.StringToHash("BlockHit");
    protected static readonly int AnimStunned = Animator.StringToHash("Stunned");

    protected virtual void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError($"{gameObject.name}: No Animator found! Check that Animator is on this GameObject.", this);
        }

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
            Debug.LogWarning($"{name}: Player not found!");
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        navAgent.speed = chaseSpeed;
        navAgent.isStopped = true;
        isAware = false;
        isEngaged = false;
        hasOpenedWithCharge = false;

        // Ensure NavMeshAgent controls movement by default, not root motion
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        // Register with combat manager
        if (EnemyCombatManager.Instance != null)
        {
            EnemyCombatManager.Instance.RegisterEnemy(this);
        }
    }

    protected virtual void OnDestroy()
    {
        // Unregister from combat manager
        if (EnemyCombatManager.Instance != null)
        {
            EnemyCombatManager.Instance.UnregisterEnemy(this);
        }
    }

    protected virtual void Update()
    {
        if (currentHealth <= 0) return;

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
            // Check if we should wait or attack
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

        // Always face the player when aware and not stunned
        if (isAware && !isStunned)
        {
            FacePlayer();
        }

        UpdateAnimatorParameters();
    }

    /// <summary>
    /// Check if this enemy should wait for their turn to attack
    /// </summary>
    protected bool ShouldWaitForTurn()
    {
        if (EnemyCombatManager.Instance == null) return false;
        return EnemyCombatManager.Instance.ShouldWait(this);
    }

    /// <summary>
    /// Circle around the player while waiting
    /// </summary>
    protected virtual void CircleAroundPlayer()
    {
        if (isAttacking || isBlocking || isStunned || isCharging) return;
        if (EnemyCombatManager.Instance == null) return;

        Vector3 targetPos = EnemyCombatManager.Instance.GetCirclePosition(this);

        navAgent.speed = circleSpeed;
        navAgent.isStopped = false;
        navAgent.SetDestination(targetPos);
    }

    /// <summary>
    /// Check if this enemy is blocking or stunned (used by combat manager)
    /// </summary>
    public bool IsBlockingOrStunned()
    {
        return isBlocking || isStunned;
    }

    protected void UpdateAnimatorParameters()
    {
        if (animator == null)
        {
            Debug.LogWarning($"{gameObject.name}: Animator is null in Update!");
            return;
        }

        Vector3 worldVelocity = navAgent.velocity;
        float rawSpeed = worldVelocity.magnitude;

        Vector3 localVelocityDir = transform.InverseTransformDirection(worldVelocity.normalized);

        animator.SetFloat(AnimSpeed, rawSpeed);
        animator.SetFloat("VelocityX", localVelocityDir.x);
        animator.SetFloat("VelocityZ", localVelocityDir.z);
    }

    /// <summary>
    /// Handles root motion - applies animation movement to the GameObject during attacks.
    /// Only applies position, not rotation, so enemy always faces player.
    /// </summary>
    private void OnAnimatorMove()
    {
        if (animator == null) return;

        if (isAttacking)
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

    /// <summary>
    /// Handles ongoing combat behavior after the enemy is engaged.
    /// Override in subclasses for custom attack patterns.
    /// </summary>
    protected virtual void ContinueCombat()
    {
        // Skip if busy with an action
        if (isAttacking || isBlocking || isCharging || isStunned) return;

        float distance = GetDistanceToPlayer();

        if (distance <= attackRange)
        {
            // Request permission to attack
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

            // Only do opening charge if we have attack permission
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
                    // Can't attack, just mark opener as done and circle
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

    #region Movement
    protected virtual void ChasePlayer()
    {
        if (isAttacking || isBlocking || isStunned || isCharging || player == null) return;

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

    /// <summary>
    /// Performs a light attack with random animation selection (0, 1, or 2)
    /// </summary>
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

    /// <summary>
    /// Performs a heavy attack
    /// </summary>
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
        if (isAttacking || isStunned || isCharging) return;

        isBlocking = true;
        navAgent.isStopped = true;
        animator?.SetBool(AnimBlock, true);
    }

    public virtual void StopBlock()
    {
        isBlocking = false;
        animator?.SetBool(AnimBlock, false);
    }

    /// <summary>
    /// Initiates a charge toward the player, triggers ChargeAttack when in range
    /// </summary>
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
        return !isAttacking && !isBlocking && !isStunned && !isCharging && attackCooldownTimer <= 0;
    }
    #endregion

    #region Damage & Health


    /// <summary>
    /// Called when player attacks this enemy. Handles blocking, stun buildup, and damage.
    /// </summary>
    public virtual void TakeDamage(float damage)
    {
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
            // Add stun damage when blocking
            currentStunMeter += damage * 0.5f;

            // Check if guard is broken
            if (currentStunMeter >= maxStunMeter)
            {
                BreakGuard();
                return;
            }

            // Play block hit reaction
            animator?.SetTrigger(AnimBlockHit);
            Debug.Log($"{gameObject.name} blocked! Stun meter: {currentStunMeter}/{maxStunMeter}");
            return;
        }

        // Not blocking - take full damage
        currentHealth -= damage;

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

        // Play hit animation and start recovery
        animator?.SetTrigger(AnimHit);
        StartCoroutine(HitRecoveryRoutine());
    }

    /// <summary>
    /// Break enemy's guard and stun them
    /// </summary>
    protected virtual void BreakGuard()
    {
        isBlocking = false;
        animator?.SetBool(AnimBlock, false);
        currentStunMeter = 0f;

        Debug.Log($"{gameObject.name}'s guard was broken!");
        ApplyStun(stunDuration);
    }

    private IEnumerator HitRecoveryRoutine()
    {
        isRecovering = true;
        yield return new WaitForSeconds(hitRecoveryTime);
        isRecovering = false;

        if (!isStunned && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = false;
        }
    }

    protected virtual void Die()
    {
        isAttacking = false;
        isBlocking = false;
        isCharging = false;
        isRecovering = false;
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
    #endregion

    #region Animation Events (call from animation clips)
    public void OnAttackEnd()
    {
        isAttacking = false;
        isCharging = false;

        navAgent.updatePosition = true;
        navAgent.nextPosition = transform.position;

        // Release attack permission when done attacking
        if (EnemyCombatManager.Instance != null)
        {
            EnemyCombatManager.Instance.ReleaseAttackPermission(this);
        }

        FacePlayerImmediate();

        if (!isStunned && navAgent.isOnNavMesh)
        {
            navAgent.speed = chaseSpeed;
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
        if (distance <= effectiveRange)
        {
            Debug.Log($"Player hit for {damage} damage!");
            return true;
        }
        return false;
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

        navAgent.isStopped = true;
        navAgent.velocity = Vector3.zero;

        yield return new WaitForSeconds(duration);

        isStunned = false;
    }
    #endregion
}