using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns enemies at designated spawn points.
/// Supports both basic and elite enemy types for modular level design.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject basicEnemyPrefab;
    [SerializeField] private GameObject eliteEnemyPrefab;

    [Header("Spawn Point Containers")]
    [Tooltip("Parent GameObject containing all basic enemy spawn points as children")]
    [SerializeField] private Transform basicEnemySpawnContainer;

    [Tooltip("Parent GameObject containing all elite enemy spawn points as children")]
    [SerializeField] private Transform eliteEnemySpawnContainer;

    [Header("Spawn Settings")]
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private float spawnDelay = 0f;

    // Track spawned enemies
    private List<BaseEnemy> spawnedEnemies = new List<BaseEnemy>();

    private void Start()
    {
        if (spawnOnStart)
        {
            if (spawnDelay > 0)
            {
                Invoke(nameof(SpawnAllEnemies), spawnDelay);
            }
            else
            {
                SpawnAllEnemies();
            }
        }
    }

    /// <summary>
    /// Spawns all enemies at their designated spawn points
    /// </summary>
    public void SpawnAllEnemies()
    {
        SpawnBasicEnemies();
        SpawnEliteEnemies();
    }

    /// <summary>
    /// Spawns basic enemies at all child transforms of the basic spawn container
    /// </summary>
    public void SpawnBasicEnemies()
    {
        if (basicEnemyPrefab == null)
        {
            if (basicEnemySpawnContainer != null && basicEnemySpawnContainer.childCount > 0)
            {
                Debug.LogWarning("EnemySpawner: Basic enemy prefab not assigned but spawn points exist!");
            }
            return;
        }

        if (basicEnemySpawnContainer == null)
        {
            Debug.LogWarning("EnemySpawner: Basic enemy spawn container not assigned!");
            return;
        }

        int spawnCount = 0;
        foreach (Transform spawnPoint in basicEnemySpawnContainer)
        {
            SpawnEnemy(basicEnemyPrefab, spawnPoint);
            spawnCount++;
        }

        Debug.Log($"EnemySpawner: Spawned {spawnCount} basic enemies");
    }

    /// <summary>
    /// Spawns elite enemies at all child transforms of the elite spawn container
    /// </summary>
    public void SpawnEliteEnemies()
    {
        if (eliteEnemyPrefab == null)
        {
            if (eliteEnemySpawnContainer != null && eliteEnemySpawnContainer.childCount > 0)
            {
                Debug.LogWarning("EnemySpawner: Elite enemy prefab not assigned but spawn points exist!");
            }
            return;
        }

        if (eliteEnemySpawnContainer == null)
        {
            return; // No elite container assigned, skip silently (for Level 1)
        }

        int spawnCount = 0;
        foreach (Transform spawnPoint in eliteEnemySpawnContainer)
        {
            SpawnEnemy(eliteEnemyPrefab, spawnPoint);
            spawnCount++;
        }

        Debug.Log($"EnemySpawner: Spawned {spawnCount} elite enemies");
    }

    /// <summary>
    /// Spawns a single enemy at the specified spawn point
    /// </summary>
    private BaseEnemy SpawnEnemy(GameObject prefab, Transform spawnPoint)
    {
        GameObject enemyObj = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

        BaseEnemy enemy = enemyObj.GetComponent<BaseEnemy>();
        if (enemy != null)
        {
            spawnedEnemies.Add(enemy);
        }

        return enemy;
    }

    /// <summary>
    /// Returns the number of enemies still alive
    /// </summary>
    public int GetAliveEnemyCount()
    {
        spawnedEnemies.RemoveAll(e => e == null || e.IsDead());
        return spawnedEnemies.Count;
    }

    /// <summary>
    /// Returns true if all spawned enemies are dead
    /// </summary>
    public bool AreAllEnemiesDead()
    {
        return GetAliveEnemyCount() == 0;
    }

    /// <summary>
    /// Clears all spawned enemies (kills and removes them)
    /// </summary>
    public void ClearAllEnemies()
    {
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null && !enemy.IsDead())
            {
                Destroy(enemy.gameObject);
            }
        }
        spawnedEnemies.Clear();
    }

    /// <summary>
    /// Respawns all enemies (clears existing and spawns new)
    /// </summary>
    public void RespawnAllEnemies()
    {
        ClearAllEnemies();
        SpawnAllEnemies();
    }

    // Visualize spawn points in editor
    private void OnDrawGizmosSelected()
    {
        // Draw basic enemy spawn points in blue
        if (basicEnemySpawnContainer != null)
        {
            Gizmos.color = Color.blue;
            foreach (Transform spawnPoint in basicEnemySpawnContainer)
            {
                Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
                Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + spawnPoint.forward);
            }
        }

        // Draw elite enemy spawn points in red
        if (eliteEnemySpawnContainer != null)
        {
            Gizmos.color = Color.red;
            foreach (Transform spawnPoint in eliteEnemySpawnContainer)
            {
                Gizmos.DrawWireSphere(spawnPoint.position, 0.75f);
                Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + spawnPoint.forward);
            }
        }
    }
}