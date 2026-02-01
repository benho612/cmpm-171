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
    [SerializeField] protected float attackRange = 2f;
    [SerializeField] protected float chargeSpeed = 2.5f;
    [SerializeField] protected float chargeStopDistance = 2f; // Distance to stop before attacking

    [Header("Awareness & Engagement")]
    [SerializeField] protected float awarenessRange = 25f;
    [SerializeField] protected float engagementRange = 15f;

    [Header("Attack Damage")]
    [SerializeField] protected float punchDamage = 10f;
    [SerializeField] protected float chargeDamage = 25f;
    [SerializeField] protected float shoveDamage = 5f;

    [Header("Timing")]
    [SerializeField] protected float stunDuration = 2f;
    [SerializeField] protected float attackCooldown = 1.5f;

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
    protected static readonly int AnimQuadPunch = Animator.StringToHash("QuadPunch");
    protected static readonly int AnimPunchCombo = Animator.StringToHash("PunchCombo");
    protected static readonly int AnimBlock = Animator.StringToHash("Blocking");
    protected static readonly int AnimShove = Animator.StringToHash("Shove");
    protected static readonly int AnimHit = Animator.StringToHash("Hit");
    protected static readonly int AnimDie = Animator.StringToHash("Die");
    protected static readonly int AnimSpeed = Animator.StringToHash("Speed");

    protected virtual void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();

        animator = GetComponentInChildren<Animator>(true);

        if (animator == null)
        {
            Debug.LogError($"{gameObject.name}: No Animator found in children! Check hierarchy.", this);
        }
        else
        {
            Debug.Log($"{gameObject.name}: Animator found on {animator.gameObject.name}", animator.gameObject);
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

        if (isCharging)
        {
            UpdateChargeAttack();
        }

        if (animator != null)
        {
            Vector3 worldVelocity = navAgent.velocity;
            float rawSpeed = worldVelocity.magnitude;

            float normSpeed = rawSpeed / chaseSpeed;

            Vector3 localVelocityDir = transform.InverseTransformDirection(worldVelocity.normalized);

            animator.SetFloat(AnimSpeed, normSpeed);
            animator.SetFloat("VelocityX", localVelocityDir.x);
            animator.SetFloat("VelocityZ", localVelocityDir.z);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: Animator is null in Update!");
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
            Debug.Log($"{gameObject.name}: Player detected! Starting pursuit.");
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
                ChargeAttack();
                hasOpenedWithCharge = true;
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

    public virtual void SimplePunch()
    {
        if (!CanPerformAction()) return;

        isAttacking = true;
        attackCooldownTimer = attackCooldown;
        navAgent.isStopped = true;

        if (Random.value < 0.5f)
        {
            animator?.SetTrigger(AnimQuadPunch);
        }
        else
        {
            animator?.SetTrigger(AnimPunchCombo);
        }
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

    public virtual void ChargeAttack()
    {
        if (!CanPerformAction() || player == null) return;

        isCharging = true;
        attackCooldownTimer = attackCooldown * 1.8f;

        navAgent.isStopped = false;
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

        // Check if we've reached stop distance
        if (distanceToPlayer <= chargeStopDistance)
        {
            // Completely stop movement
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero;
            navAgent.ResetPath();

            isCharging = false;
            isAttacking = true;

            // Face player before attacking
            FacePlayerImmediate();

            // Perform the attack
            animator?.SetTrigger(AnimQuadPunch);
        }
        else
        {
            // Keep updating destination as player moves
            navAgent.SetDestination(player.position);
        }
    }

    /// <summary>
    /// Immediately faces the player without interpolation
    /// </summary>
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

    public virtual void Shove()
    {
        if (!CanPerformAction()) return;

        isAttacking = true;
        attackCooldownTimer = attackCooldown;
        navAgent.isStopped = true;

        animator?.SetTrigger(AnimShove);
    }

    protected virtual bool CanPerformAction()
    {
        return !isAttacking && !isBlocking && !isStunned && !isCharging && attackCooldownTimer <= 0;
    }
    #endregion

    #region Damage & Health
    public virtual void TakeDamage(float damage)
    {
        if (isBlocking)
        {
            return;
        }

        currentHealth -= damage;
        animator?.SetTrigger(AnimHit);

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

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        isAttacking = false;
        isBlocking = false;
        isCharging = false;
        navAgent.isStopped = true;
        navAgent.velocity = Vector3.zero;
        navAgent.enabled = false;

        animator?.SetTrigger(AnimDie);

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        enabled = false;
    }

    public bool IsDead() => currentHealth <= 0;
    public float GetHealthPercentage() => currentHealth / maxHealth;
    #endregion

    #region Animation Events (call from animation clips)
    public void OnAttackEnd()
    {
        isAttacking = false;
        isCharging = false;

        if (!isStunned && navAgent.isOnNavMesh)
            navAgent.speed = chaseSpeed;
    }

    public void OnPunchHit()
    {
        TryDamagePlayer(punchDamage, attackRange);
    }

    public void OnChargeHit()
    {
        TryDamagePlayer(chargeDamage, attackRange * 1.5f);
        isCharging = false;
    }

    public void OnShoveHit()
    {
        if (TryDamagePlayer(shoveDamage, attackRange))
        {
            Debug.Log("Player shoved & stunned!");
        }
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