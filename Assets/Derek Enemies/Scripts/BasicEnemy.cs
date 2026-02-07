using UnityEngine;

public class BasicEnemy : BaseEnemy
{
    [Header("AI Settings")]
    [SerializeField] private float attackDistance = 2.5f;
    [SerializeField] private float chargeDistance = 8f;
    [SerializeField][Range(0f, 1f)] private float blockChance = 0.25f;
    [SerializeField][Range(0f, 1f)] private float heavyAttackChance = 0.3f;
    [SerializeField][Range(0f, 1f)] private float chargeChance = 0.35f;

    private float aiDecisionTimer;
    private float aiDecisionInterval = 0.4f;

    protected override void Update()
    {
        base.Update();

        if (IsDead() || isStunned) return;

        // Don't do anything until aware of player
        if (!isAware) return;

        // Don't do anything until engaged
        if (!isEngaged) return;

        // Wait for opening charge to complete before making decisions
        if (!hasOpenedWithCharge || isCharging || isAttacking) return;

        aiDecisionTimer -= Time.deltaTime;
        if (aiDecisionTimer <= 0f)
        {
            aiDecisionTimer = aiDecisionInterval + Random.Range(-0.1f, 0.1f);
            MakeDecision();
        }

        // Removed: ChasePlayer() and FacePlayer() calls
        // These are now handled by ContinueCombat() in BaseEnemy
    }

    private void MakeDecision()
    {
        if (isAttacking || isBlocking || isStunned || isCharging) return;

        float distance = GetDistanceToPlayer();

        // Close enough to melee
        if (distance <= attackDistance)
        {
            float roll = Random.value;

            if (roll < blockChance)
            {
                StartBlock();
                float blockTime = Random.Range(0.6f, 1.8f);
                Invoke(nameof(StopBlock), blockTime);
            }
            else if (roll < blockChance + heavyAttackChance)
            {
                HeavyAttack();
            }
            else
            {
                // Normal attack pattern - light attack with random animation
                LightAttack();
            }
        }
        // Medium distance → chance to charge
        else if (distance <= chargeDistance && distance > attackDistance + 1.5f)
        {
            if (Random.value < chargeChance)
            {
                ChargeAttack();
            }
        }
    }
}