using UnityEngine;

public class EliteEnemy : BaseEnemy
{
    [Header("Elite Attack Damage")]
    [SerializeField] private float swordSwingDamage = 35f;
    [SerializeField] private float legSweepDamage = 15f;
    [SerializeField] private float swordSlamDamage = 50f;

    [Header("Elite AI Settings")]
    [SerializeField] private float attackDistance = 3f;
    [SerializeField] private float chargeDistance = 10f;
    [SerializeField][Range(0f, 1f)] private float blockChance = 0.3f;
    [SerializeField][Range(0f, 1f)] private float chargeChance = 0.4f;

    // Animation hashes for elite attacks
    protected static readonly int AnimSwordSwing = Animator.StringToHash("SwordSwing");
    protected static readonly int AnimLegSweep = Animator.StringToHash("LegSweep");
    protected static readonly int AnimSwordSlam = Animator.StringToHash("SwordSlam");

    private float aiDecisionTimer;
    private float aiDecisionInterval = 0.4f;
    private bool legSweepHit; // Track if leg sweep connected for combo

    protected override void Update()
    {
        base.Update();

        // Don't continue if dead, stunned, in hit stun, or dashing back
        if (IsDead() || isStunned || isInHitStun || isDashingBack) return;

        aiDecisionTimer -= Time.deltaTime;
        if (aiDecisionTimer <= 0f)
        {
            aiDecisionTimer = aiDecisionInterval + Random.Range(-0.1f, 0.1f);
            MakeDecision();
        }

        // Default behavior: chase & face unless doing something else
        if (!isAttacking && !isBlocking)
        {
            ChasePlayer();
            FacePlayer();
        }
    }

    private void MakeDecision()
    {
        if (isAttacking || isBlocking || isStunned) return;

        float distance = GetDistanceToPlayer();

        // Close enough to melee
        if (distance <= attackDistance)
        {
            float roll = Random.value;

            if (roll < blockChance)
            {
                StartBlock();
                float blockTime = Random.Range(0.3f, 1f);
                Invoke(nameof(StopBlock), blockTime);
            }
            else if (roll < 0.35f)
            {
                //SimplePunch();
            }
            else if (roll < 0.5f)
            {
                //Shove();
            }
            else if (roll < 0.7f)
            {
                UnblockableSwordSwing();
            }
            else if (roll < 0.85f)
            {
                LegSweep();
            }
            else
            {
                SwordSlam(); // Standalone powerful attack
            }
        }
        // Medium distance ? chance to charge
        else if (distance <= chargeDistance && distance > attackDistance + 1.5f)
        {
            if (Random.value < chargeChance)
            {
                ChargeAttack();
            }
        }
        // Otherwise keep chasing (handled in Update)
    }

    #region Elite Attacks
    /// <summary>
    /// Unblockable sword swing - player must dodge
    /// </summary>
    public void UnblockableSwordSwing()
    {
        if (!CanPerformAction()) return;

        isAttacking = true;
        attackCooldownTimer = attackCooldown * 1.5f;
        navAgent.isStopped = true;

        // Visual/audio warning for player (optional: red flash, wind-up sound)
        animator?.SetTrigger(AnimSwordSwing);
    }

    /// <summary>
    /// Leg sweep that can combo into sword slam if it connects
    /// </summary>
    public void LegSweep()
    {
        if (!CanPerformAction()) return;

        isAttacking = true;
        legSweepHit = false;
        attackCooldownTimer = attackCooldown;
        navAgent.isStopped = true;

        animator?.SetTrigger(AnimLegSweep);
    }

    /// <summary>
    /// Powerful sword slam - used standalone or as combo finisher
    /// </summary>
    public void SwordSlam()
    {
        if (!CanPerformAction()) return;

        isAttacking = true;
        attackCooldownTimer = attackCooldown * 2f;
        navAgent.isStopped = true;

        animator?.SetTrigger(AnimSwordSlam);
    }
    #endregion

    #region Animation Events for Elite Attacks
    /// <summary>
    /// Call during sword swing at impact - unblockable
    /// </summary>
    public void OnSwordSwingHit()
    {
        TryDamagePlayer(swordSwingDamage, attackRange * 1.5f);
        // Note: Make player TakeDamage unblockable for this attack (implement in PlayerHealth)
        Debug.Log("UNBLOCKABLE sword swing hit!");
    }

    /// <summary>
    /// Call at end of sword swing animation
    /// </summary>
    public void OnSwordSwingEnd()
    {
        OnAttackEnd();
    }

    /// <summary>
    /// Call during leg sweep at impact
    /// </summary>
    public void OnLegSweepHit()
    {
        if (TryDamagePlayer(legSweepDamage, attackRange))
        {
            legSweepHit = true;
            // TODO: Knock player down / brief stun / ragdoll
            Debug.Log("Leg sweep connected! Combo incoming...");
        }
    }

    /// <summary>
    /// Call at end of leg sweep - triggers combo if hit
    /// </summary>
    public void OnLegSweepEnd()
    {
        if (legSweepHit)
        {
            // Reset attacking state briefly to allow immediate followup
            isAttacking = false;
            SwordSlamComboFollowup();
        }
        else
        {
            OnAttackEnd();
        }
    }

    private void SwordSlamComboFollowup()
    {
        isAttacking = true;
        attackCooldownTimer = attackCooldown * 2f; // Full cooldown for slam
        animator?.SetTrigger(AnimSwordSlam);
    }

    /// <summary>
    /// Call during sword slam at impact (standalone or combo)
    /// </summary>
    public void OnSwordSlamHit()
    {
        TryDamagePlayer(swordSlamDamage, attackRange * 1.2f);
    }

    /// <summary>
    /// Call at end of sword slam animation
    /// </summary>
    public void OnSwordSlamEnd()
    {
        OnAttackEnd();
    }
    #endregion
}