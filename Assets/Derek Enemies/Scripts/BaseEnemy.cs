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

    [Header("Hit Stun")]
    [SerializeField] protected float maxHitStunDuration = 5f;
    [SerializeField] protected float hitStunResetTime = 1f;
    protected float hitStunTimer;
    protected float timeSinceLastHit;
    protected bool isInHitStun;

    [Header("Hit Immunity")]
    [SerializeField] protected float hitImmunityDuration = 5f; // Can't be staggered for this long after hit stun ends
    protected bool isHitImmune;
    protected float hitImmuneTimer;

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
    protected static readonly int AnimStunned = Animator.StringToHash("Stunned");

    private PlayerHealth playerHealth;

    protected virtual void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();

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
        isHitImmune = false;
        hitImmuneTimer = 0f;

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

        // Update hit immunity timer
        if (isHitImmune)
        {
            hitImmuneTimer -= Time.deltaTime;
            if (hitImmuneTimer <= 0f)
            {
                isHitImmune = false;
                Debug.Log($"{gameObject.name}: Hit immunity expired");
            }
        }

        // Update hit stun timer
        if (isInHitStun)
        {
            hitStunTimer += Time.deltaTime;
            timeSinceLastHit += Time.deltaTime;

            if (hitStunTimer >= maxHitStunDuration)
            {
                ExitHitStunWithImmunity();
            }
            else if (timeSinceLastHit >= hitStunResetTime)
            {
                ExitHitStunWithImmunity();
            }

            return;
        }

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (!isAware)
        {
            CheckAwareness();
        }
        else if (!isEngaged || !hasOpenedWithCharge)
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
        if (isAttacking || isBlocking || isStunned || isCharging || isInHitStun) return;
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

        if (isAttacking || isInHitStun)
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
        if (isAttacking || isBlocking || isCharging || isStunned || isInHitStun) return;

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

            // Keep retrying the opener charge until it actually fires
            if (!hasOpenedWithCharge)
            {
                if (CanPerformAction())
                {
                    if (EnemyCombatManager.Instance == null ||
                        EnemyCombatManager.Instance.RequestAttackPermission(this))
                    {
                        Debug.Log($"{gameObject.name}: OPENER CHARGE FIRED! Distance: {distance:F2}");
                        ChargeAttack();
                        hasOpenedWithCharge = true;
                    }
                    else
                    {
                        Debug.Log($"{gameObject.name}: Opener charge DENIED by CombatManager");
                    }
                }
                else
                {
                    Debug.Log($"{gameObject.name}: Can't perform action yet - cooldown: {attackCooldownTimer:F2}, attacking: {isAttacking}, charging: {isCharging}");
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
    public bool IsAttacking() => isAttacking;

    #region Movement
    protected virtual void ChasePlayer()
    {
        if (isAttacking || isBlocking || isStunned || isCharging || isInHitStun || player == null) return;

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

        float randomIndex = Random.Range(0, 2);// Produces 0, 0.5, or 1
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
        if (isAttacking || isStunned || isCharging || isInHitStun) return;

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
        Debug.Log($"{gameObject.name}: Attempting to start charge attack");
        if (!CanPerformAction() || player == null)
        {
            Debug.Log($"{gameObject.name}: Cannot start charge attack - CanPerformAction: {CanPerformAction()}, Player null: {player == null}");
            return;
        }
        Debug.Log($"{gameObject.name}: Starting charge attack towards player at {player.position}");
        isCharging = true;
        attackCooldownTimer = attackCooldown * 1.8f;

        navAgent.isStopped = false;
        navAgent.updatePosition = true;
        navAgent.speed = chargeSpeed;
        navAgent.SetDestination(player.position);
    }

    protected virtual void UpdateChargeAttack()
    {
        Debug.Log($"{gameObject.name}: Updating charge attack");
        if (player == null)
        {
            EndCharge();
            Debug.LogWarning($"{gameObject.name}: Player lost during charge - ending charge");
            return;
        }

        float distanceToPlayer = GetDistanceToPlayer();

        if (distanceToPlayer <= chargeStopDistance)
        {
            Debug.Log($"{gameObject.name}: Charge reached player - stopping and triggering attack! Distance: {distanceToPlayer:F2}");
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero;
            navAgent.ResetPath();

            isCharging = false;
            isAttacking = true;

            FacePlayerImmediate();
            Debug.Log($"{gameObject.name}: Charge reached player, triggering attack! Distance: {distanceToPlayer:F2}");
            animator?.SetTrigger(AnimChargeAttack);
        }
        else
        {
            Debug.Log($"{gameObject.name}: Charging towards player - Distance: {distanceToPlayer:F2}");
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
        Debug.Log($"{gameObject.name}: Ending charge - player lost or invalid");
        isCharging = false;
        navAgent.velocity = Vector3.zero;
        navAgent.speed = chaseSpeed;
    }

    protected virtual bool CanPerformAction()
    {
        return !isAttacking && !isBlocking && !isStunned && !isCharging && !isInHitStun && attackCooldownTimer <= 0;
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

        // Take damage (always take damage, even if immune to stagger)
        currentHealth -= damage;
        Debug.Log($"{gameObject.name}: Health now {currentHealth}/{maxHealth}");

        // Show health bar when damaged
        var healthBar = GetComponentInChildren<EnemyHealthBar>();
        healthBar?.ShowHealthBar();

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // If immune to hit stun, just take damage but don't stagger
        if (isHitImmune)
        {
            Debug.Log($"{gameObject.name}: Hit immune - took damage but no stagger ({hitImmuneTimer:F1}s remaining)");
            return;
        }

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

        // Enter hit stun and play hit reaction
        Debug.Log($"{gameObject.name}: Hit! Entering hit stun");
        animator?.SetTrigger(AnimHitReaction);
        EnterHitStun();
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
        // Stop all coroutines to prevent any ongoing routines
        StopAllCoroutines();

        isAttacking = false;
        isBlocking = false;
        isCharging = false;
        isRecovering = false;
        isInHitStun = false;
        isHitImmune = false;
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

        // Hide health bar
        var healthBar = GetComponentInChildren<EnemyHealthBar>();
        if (healthBar != null) healthBar.gameObject.SetActive(false);

        // Start dissolve effect
        var dissolve = GetComponent<DissolveExample.DissolveChilds>();
        if (dissolve != null)
        {
            dissolve.StartDissolve();
        }
        else
        {
            Destroy(gameObject, 3f);
        }

        enabled = false;
    }

    public bool IsDead() => currentHealth <= 0;
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public float GetStunPercentage() => currentStunMeter / maxStunMeter;
    public bool IsRecovering() => isRecovering;
    public bool IsHitImmune() => isHitImmune;

    #region Animation Events
    public void OnAttackEnd()
    {
        Debug.Log($"{gameObject.name}: OnAttackEnd called! isCharging was: {isCharging}, isAttacking was: {isAttacking}");

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

    public void OnHitReactionEnd()
    {
        Debug.Log($"{gameObject.name}: Hit reaction animation ended");
    }
    #endregion

    #region Stun
    public virtual void ApplyStun(float duration)
    {
        if (isStunned || isHitImmune) return;
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

        yield return new WaitForSeconds(duration);

        isStunned = false;

        if (navAgent.isOnNavMesh)
        {
            navAgent.isStopped = false;
        }
    }
    #endregion

    protected virtual void EnterHitStun()
    {
        if (!isInHitStun)
        {
            isInHitStun = true;
            hitStunTimer = 0f;

            // Grant hit immunity immediately so they can't be staggered again
            isHitImmune = true;
            hitImmuneTimer = hitImmunityDuration;
            Debug.Log($"{gameObject.name}: Entered hit stun, now immune to stagger for {hitImmunityDuration}s!");
        }

        timeSinceLastHit = 0f;
    }

    /// <summary>
    /// Exit hit stun and resume normal combat (immunity already granted on enter)
    /// </summary>
    protected virtual void ExitHitStunWithImmunity()
    {
        isInHitStun = false;
        hitStunTimer = 0f;
        timeSinceLastHit = 0f;

        navAgent.updatePosition = true;
        navAgent.nextPosition = transform.position;

        if (!isStunned && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = false;
        }

        Debug.Log($"{gameObject.name}: Exited hit stun, resuming combat");
    }
}