using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages group combat behavior - ensures only one enemy attacks at a time
/// while others circle around waiting for opportunities.
/// </summary>
public class EnemyCombatManager : MonoBehaviour
{
    public static EnemyCombatManager Instance { get; set; }

    [Header("Combat Settings")]
    [SerializeField] private float engageChance = 0.5f;
    [SerializeField] private float opportunityCheckInterval = 2f; // Changed to 2 seconds
    [SerializeField] private float circleRadius = 4f;
    [SerializeField] private float maxAttackDuration = 5f; // Force release after this time

    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // Track all enemies and who's currently attacking
    private List<BaseEnemy> registeredEnemies = new List<BaseEnemy>();
    private BaseEnemy currentAttacker;
    private float opportunityCheckTimer;
    private float attackDurationTimer;

    private void Awake()
    {
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

        // Validate current attacker
        ValidateCurrentAttacker();

        // Track how long current attacker has been attacking
        if (currentAttacker != null)
        {
            attackDurationTimer += Time.deltaTime;

            // Force release if attacking too long
            if (attackDurationTimer >= maxAttackDuration)
            {
                if (showDebugLogs)
                    Debug.Log($"[CombatManager] Forcing {currentAttacker.name} to release - max duration reached");

                ReleaseAttackPermission(currentAttacker);
            }
        }

        // Periodically check for engagement opportunities
        opportunityCheckTimer -= Time.deltaTime;
        if (opportunityCheckTimer <= 0f)
        {
            opportunityCheckTimer = opportunityCheckInterval;
            CheckEngagementOpportunities();
        }
    }

    public void RegisterEnemy(BaseEnemy enemy)
    {
        if (!registeredEnemies.Contains(enemy))
        {
            registeredEnemies.Add(enemy);
            //if (showDebugLogs)
            //    Debug.Log($"[CombatManager] Registered: {enemy.name} (Total: {registeredEnemies.Count})");
        }
    }

    public void UnregisterEnemy(BaseEnemy enemy)
    {
        registeredEnemies.Remove(enemy);
        if (currentAttacker == enemy)
        {
            currentAttacker = null;
            attackDurationTimer = 0f;
        }
        if (showDebugLogs)
            Debug.Log($"[CombatManager] Unregistered: {enemy.name} (Total: {registeredEnemies.Count})");
    }

    public bool RequestAttackPermission(BaseEnemy enemy)
    {
        // If no one is attacking, grant permission
        if (currentAttacker == null)
        {
            currentAttacker = enemy;
            attackDurationTimer = 0f;
            if (showDebugLogs)
                Debug.Log($"[CombatManager] GRANTED attack permission to: {enemy.name}");
            return true;
        }

        // If this enemy is already the attacker, allow
        if (currentAttacker == enemy)
        {
            return true;
        }

        // Otherwise, deny
        if (showDebugLogs)
            Debug.Log($"[CombatManager] DENIED attack permission to: {enemy.name} (Current: {currentAttacker.name})");
        return false;
    }

    public void ReleaseAttackPermission(BaseEnemy enemy)
    {
        if (currentAttacker == enemy)
        {
            if (showDebugLogs)
                Debug.Log($"[CombatManager] Released attack permission from: {enemy.name}");
            currentAttacker = null;
            attackDurationTimer = 0f;
        }
    }

    public bool IsCurrentAttacker(BaseEnemy enemy)
    {
        return currentAttacker == enemy;
    }

    public bool ShouldWait(BaseEnemy enemy)
    {
        // Must wait if someone else is attacking
        if (currentAttacker != null && currentAttacker != enemy)
        {
            return true;
        }
        return false;
    }

    public Vector3 GetCirclePosition(BaseEnemy enemy)
    {
        if (player == null) return enemy.transform.position;

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

        float angleStep = 360f / totalWaiting;
        float angle = (angleStep * waitingIndex) + (Time.time * 15f);
        float radians = angle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * circleRadius;
        return player.position + offset;
    }

    private void ValidateCurrentAttacker()
    {
        if (currentAttacker == null) return;

        // Release if dead
        if (currentAttacker.IsDead())
        {
            if (showDebugLogs)
                Debug.Log($"[CombatManager] Attacker died: {currentAttacker.name}");
            currentAttacker = null;
            attackDurationTimer = 0f;
            return;
        }

        // Release if in hit stun (got interrupted by player)
        if (currentAttacker.IsInHitStun())
        {
            if (showDebugLogs)
                Debug.Log($"[CombatManager] Attacker interrupted: {currentAttacker.name}");
            currentAttacker = null;
            attackDurationTimer = 0f;
        }
    }

    private void CheckEngagementOpportunities()
    {
        if (showDebugLogs)
            Debug.Log($"[CombatManager] Checking opportunities... Current attacker: {(currentAttacker != null ? currentAttacker.name : "NONE")}");

        // If no current attacker, find one
        if (currentAttacker == null)
        {
            AssignNewAttacker();
            return;
        }

        // Check if current attacker is vulnerable
        if (IsCurrentAttackerVulnerable())
        {
            if (Random.value <= engageChance)
            {
                BaseEnemy newAttacker = FindBestWaitingEnemy();
                if (newAttacker != null)
                {
                    if (showDebugLogs)
                        Debug.Log($"[CombatManager] Swapping attacker: {currentAttacker.name} -> {newAttacker.name}");
                    currentAttacker = newAttacker;
                    attackDurationTimer = 0f;
                }
            }
        }
    }

    private bool IsCurrentAttackerVulnerable()
    {
        if (currentAttacker == null) return false;
        return currentAttacker.IsBlockingOrStunned() || currentAttacker.IsInHitStun();
    }

    private void AssignNewAttacker()
    {
        BaseEnemy closest = FindClosestEngagedEnemy();
        if (closest != null)
        {
            currentAttacker = closest;
            attackDurationTimer = 0f;
            if (showDebugLogs)
                Debug.Log($"[CombatManager] Assigned new attacker: {closest.name}");
        }
    }

    private BaseEnemy FindClosestEngagedEnemy()
    {
        if (player == null) return null;

        BaseEnemy closest = null;
        float closestDist = float.MaxValue;

        foreach (var enemy in registeredEnemies)
        {
            if (enemy == null || enemy.IsDead()) continue;
            if (!enemy.IsEngaged()) continue;
            if (enemy.IsInHitStun()) continue; // Don't pick stunned enemies

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
            if (enemy.IsInHitStun()) continue; // Don't pick stunned enemies

            float dist = Vector3.Distance(enemy.transform.position, player.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                best = enemy;
            }
        }

        return best;
    }

    private void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Gizmos.color = Color.yellow;
        DrawCircle(player.position, circleRadius, 32);

        if (currentAttacker != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(player.position, currentAttacker.transform.position);
            Gizmos.DrawWireSphere(currentAttacker.transform.position, 1f);
        }

        // Draw waiting enemies in blue
        Gizmos.color = Color.blue;
        foreach (var enemy in registeredEnemies)
        {
            if (enemy == null || enemy == currentAttacker) continue;
            Gizmos.DrawWireSphere(enemy.transform.position, 0.5f);
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