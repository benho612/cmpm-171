using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages group combat behavior - ensures only one enemy attacks at a time
/// while others circle around waiting for opportunities.
/// </summary>
public class EnemyCombatManager : MonoBehaviour
{
    public static EnemyCombatManager Instance { get; private set; }

    [Header("Combat Settings")]
    [SerializeField] private float engageChance = 0.5f; // 50% chance to engage when opportunity arises
    [SerializeField] private float opportunityCheckInterval = 0.5f; // How often to check for engagement opportunities
    [SerializeField] private float circleRadius = 4f; // Distance waiting enemies keep from player
    [SerializeField] private float circleSpeed = 2f; // Speed enemies circle at

    [Header("References")]
    [SerializeField] private Transform player;

    // Track all enemies and who's currently attacking
    private List<BaseEnemy> registeredEnemies = new List<BaseEnemy>();
    private BaseEnemy currentAttacker;
    private float opportunityCheckTimer;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
    }

    private void Update()
    {
        if (player == null) return;

        // Clean up dead enemies
        registeredEnemies.RemoveAll(e => e == null || e.IsDead());

        // Check if current attacker is still valid
        ValidateCurrentAttacker();

        // Periodically check for engagement opportunities
        opportunityCheckTimer -= Time.deltaTime;
        if (opportunityCheckTimer <= 0f)
        {
            opportunityCheckTimer = opportunityCheckInterval;
            CheckEngagementOpportunities();
        }

        // Update waiting enemies to circle around
        UpdateWaitingEnemies();
    }

    /// <summary>
    /// Register an enemy with the combat manager
    /// </summary>
    public void RegisterEnemy(BaseEnemy enemy)
    {
        if (!registeredEnemies.Contains(enemy))
        {
            registeredEnemies.Add(enemy);
        }
    }

    /// <summary>
    /// Unregister an enemy (call when enemy dies or is destroyed)
    /// </summary>
    public void UnregisterEnemy(BaseEnemy enemy)
    {
        registeredEnemies.Remove(enemy);
        if (currentAttacker == enemy)
        {
            currentAttacker = null;
        }
    }

    /// <summary>
    /// Request permission to attack the player
    /// </summary>
    public bool RequestAttackPermission(BaseEnemy enemy)
    {
        // If no one is attacking, grant permission
        if (currentAttacker == null)
        {
            currentAttacker = enemy;
            return true;
        }

        // If this enemy is already the attacker, allow
        if (currentAttacker == enemy)
        {
            return true;
        }

        // Otherwise, deny - someone else is attacking
        return false;
    }

    /// <summary>
    /// Release attack permission (call when enemy stops attacking)
    /// </summary>
    public void ReleaseAttackPermission(BaseEnemy enemy)
    {
        if (currentAttacker == enemy)
        {
            currentAttacker = null;
        }
    }

    /// <summary>
    /// Check if this enemy is the current attacker
    /// </summary>
    public bool IsCurrentAttacker(BaseEnemy enemy)
    {
        return currentAttacker == enemy;
    }

    /// <summary>
    /// Check if an enemy should be waiting (circling) instead of attacking
    /// </summary>
    public bool ShouldWait(BaseEnemy enemy)
    {
        return currentAttacker != null && currentAttacker != enemy;
    }

    /// <summary>
    /// Get a position for a waiting enemy to circle to
    /// </summary>
    public Vector3 GetCirclePosition(BaseEnemy enemy)
    {
        if (player == null) return enemy.transform.position;

        // Find this enemy's index among waiting enemies
        int waitingIndex = 0;
        int totalWaiting = 0;

        foreach (var e in registeredEnemies)
        {
            if (e == null || e.IsDead() || e == currentAttacker) continue;

            if (e == enemy)
            {
                waitingIndex = totalWaiting;
            }
            totalWaiting++;
        }

        if (totalWaiting == 0) return enemy.transform.position;

        // Distribute enemies evenly around the player
        float angleStep = 360f / totalWaiting;
        float angle = (angleStep * waitingIndex) + (Time.time * 20f); // Slow rotation over time
        float radians = angle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * circleRadius;
        return player.position + offset;
    }

    private void ValidateCurrentAttacker()
    {
        if (currentAttacker == null) return;

        // If current attacker is dead, release them
        if (currentAttacker.IsDead())
        {
            currentAttacker = null;
        }
    }

    private void CheckEngagementOpportunities()
    {
        // If no current attacker, find one
        if (currentAttacker == null)
        {
            AssignNewAttacker();
            return;
        }

        // Check if current attacker is blocking or stunned - opportunity for another enemy
        if (IsCurrentAttackerVulnerable())
        {
            // 50% chance for another enemy to engage
            if (Random.value <= engageChance)
            {
                // Find a waiting enemy to take over
                BaseEnemy newAttacker = FindBestWaitingEnemy();
                if (newAttacker != null)
                {
                    currentAttacker = newAttacker;
                }
            }
        }
    }

    private bool IsCurrentAttackerVulnerable()
    {
        if (currentAttacker == null) return false;

        // Use reflection or add public methods to check state
        // For now, we'll add helper methods to BaseEnemy
        return currentAttacker.IsBlockingOrStunned();
    }

    private void AssignNewAttacker()
    {
        BaseEnemy closest = FindClosestEngagedEnemy();
        if (closest != null)
        {
            currentAttacker = closest;
        }
    }

    private BaseEnemy FindClosestEngagedEnemy()
    {
        if (player == null) return null;

        BaseEnemy closest = null;
        float closestDist = float.MaxValue;

        foreach (var enemy in registeredEnemies)
        {
            if (enemy == null || enemy.IsDead() || !enemy.IsEngaged()) continue;

            float dist = Vector3.Distance(enemy.transform.position, player.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemy;
            }
        }

        return closest;
    }

    private BaseEnemy FindBestWaitingEnemy()
    {
        if (player == null) return null;

        BaseEnemy best = null;
        float closestDist = float.MaxValue;

        foreach (var enemy in registeredEnemies)
        {
            if (enemy == null || enemy.IsDead() || enemy == currentAttacker) continue;
            if (!enemy.IsEngaged()) continue;

            float dist = Vector3.Distance(enemy.transform.position, player.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                best = enemy;
            }
        }

        return best;
    }

    private void UpdateWaitingEnemies()
    {
        foreach (var enemy in registeredEnemies)
        {
            if (enemy == null || enemy.IsDead()) continue;
            if (enemy == currentAttacker) continue;
            if (!enemy.IsEngaged()) continue;

            // This enemy should be circling, not attacking
            // The enemy's Update will handle this via ShouldWait()
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (player == null) return;

        // Draw circle radius
        Gizmos.color = Color.yellow;
        DrawCircle(player.position, circleRadius, 32);

        // Draw current attacker
        if (currentAttacker != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(player.position, currentAttacker.transform.position);
            Gizmos.DrawWireSphere(currentAttacker.transform.position, 1f);
        }
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}