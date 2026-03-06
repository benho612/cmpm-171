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
    [SerializeField][Range(0f, 1f)] private float chargeChance = 0.4f;

    [Header("Sword Hitbox")]
    [SerializeField] private SwordHitbox swordHitbox;

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

        // Don't continue if dead, stunned, or in hit stun
        if (IsDead() || isStunned || isInHitStun) return;

        // Don't make decisions until engaged and opener is done
        if (!isEngaged || !hasOpenedWithCharge || isCharging || isAttacking) return;

        aiDecisionTimer -= Time.deltaTime;
        if (aiDecisionTimer <= 0f)
        {
            aiDecisionTimer = aiDecisionInterval + Random.Range(-0.1f, 0.1f);
            MakeDecision();
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

            // BLOCKING DISABLED FOR TESTING
            // if (roll < blockChance)
            // {
            //     StartBlock();
            //     float blockTime = Random.Range(0.3f, 1f);
            //     Invoke(nameof(StopBlock), blockTime);
            // }
            // else if (roll < 0.35f)
            // {
            //     //SimplePunch();
            // }
            // else if (roll < 0.5f)
            // {
            //     //Shove();
            // }
            // else if (roll < 0.7f)

            if (roll < 0.4f)
            {
                UnblockableSwordSwing();
            }
            else if (roll < 0.7f)
            {
                LegSweep();
            }
            else
            {
                SwordSlam();
            }
        }
        // Medium distance - chance to charge
        else if (distance <= chargeDistance && distance > attackDistance + 1.5f)
        {
            if (Random.value < chargeChance)
            {
                ChargeAttack();
            }
        }
    }

    /// <summary>
    /// Override charge to use the Elite's sword slam as the charge finisher
    /// </summary>
    protected override void UpdateChargeAttack()
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

            // Elite uses SwordSlam as the charge finisher
            Debug.Log($"{gameObject.name}: Charge reached player, triggering SwordSlam!");
            animator?.SetTrigger(AnimChargeAttack);
        }
        else
        {
            navAgent.speed = chargeSpeed;
            navAgent.isStopped = false;
            navAgent.SetDestination(player.position);
        }
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

    #region Animation Events — Sword Hitbox Enable/Disable
    // Call these from animation events to toggle the sword collider.
    // Use EnableSwordSwingHitbox / EnableSwordSlamHitbox / EnableLegSweepHitbox
    // at the start of the active frames, and DisableSwordHitbox at the end.

    public void EnableSwordSwingHitbox()
    {
        swordHitbox?.EnableHitbox(swordSwingDamage);
    }

    public void EnableSwordSlamHitbox()
    {
        swordHitbox?.EnableHitbox(swordSlamDamage);
    }

    public void EnableLegSweepHitbox()
    {
        swordHitbox?.EnableHitbox(legSweepDamage);
    }

    public void DisableSwordHitbox()
    {
        swordHitbox?.DisableHitbox();
    }
    #endregion

    #region Animation Events — Attack End
    /// <summary>
    /// Call at end of sword swing animation
    /// </summary>
    public void OnSwordSwingEnd()
    {
        DisableSwordHitbox();
        OnAttackEnd();
    }

    /// <summary>
    /// Call during leg sweep at impact
    /// </summary>
    public void OnLegSweepHit()
    {
        // Leg sweep still uses range check since it's a foot, not the sword
        if (TryDamagePlayer(legSweepDamage, attackRange))
        {
            legSweepHit = true;
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
    /// Call at end of sword slam animation
    /// </summary>
    public void OnSwordSlamEnd()
    {
        DisableSwordHitbox();
        OnAttackEnd();
    }
    #endregion
}