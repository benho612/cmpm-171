using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages chamber-based enemy spawning. Each chamber has its own spawn points,
/// a trigger to activate it, and an exit wall that is destroyed when all enemies are dead.
/// </summary>
public class ChamberSpawner : MonoBehaviour
{
    [System.Serializable]
    public class Chamber
    {
        [Header("Spawn Point Containers")]
        [Tooltip("Parent GameObject containing basic enemy spawn points for this chamber")]
        public Transform basicSpawnContainer;

        [Tooltip("Parent GameObject containing elite enemy spawn points for this chamber")]
        public Transform eliteSpawnContainer;

        [Header("Chamber References")]
        [Tooltip("The trigger collider the player walks through to start this chamber")]
        public GameObject trigger;

        [Tooltip("The exit wall that gets destroyed when all enemies in this chamber are dead (leave empty for last chamber)")]
        public GameObject exitWall;

        // Runtime state
        [HideInInspector] public bool hasBeenTriggered;
        [HideInInspector] public bool isCleared;
        [HideInInspector] public List<BaseEnemy> spawnedEnemies = new List<BaseEnemy>();
    }

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject basicEnemyPrefab;
    [SerializeField] private GameObject eliteEnemyPrefab;

    [Header("Chambers")]
    [SerializeField] private Chamber[] chambers;

    [Header("Settings")]
    [Tooltip("How often (in seconds) to check if all enemies in the active chamber are dead")]
    [SerializeField] private float clearCheckInterval = 0.5f;

    [Tooltip("Delay before destroying the exit wall after all enemies are killed")]
    [SerializeField] private float wallDestroyDelay = 1f;

    private int activeChamberIndex = -1;

    private void Start()
    {
        // Set up triggers for each chamber
        for (int i = 0; i < chambers.Length; i++)
        {
            if (chambers[i].trigger != null)
            {
                ChamberTrigger triggerScript = chambers[i].trigger.GetComponent<ChamberTrigger>();
                if (triggerScript == null)
                {
                    triggerScript = chambers[i].trigger.AddComponent<ChamberTrigger>();
                }
                triggerScript.Initialize(this, i);
            }
            else
            {
                Debug.LogWarning($"ChamberSpawner: Chamber {i} has no trigger assigned!");
            }
        }
    }

    /// <summary>
    /// Called by ChamberTrigger when the player enters a chamber's trigger zone.
    /// </summary>
    public void OnChamberTriggered(int chamberIndex)
    {
        if (chamberIndex < 0 || chamberIndex >= chambers.Length) return;

        Chamber chamber = chambers[chamberIndex];

        // Only trigger once
        if (chamber.hasBeenTriggered) return;

        chamber.hasBeenTriggered = true;
        activeChamberIndex = chamberIndex;

        Debug.Log($"ChamberSpawner: Chamber {chamberIndex} triggered! Spawning enemies...");

        SpawnChamberEnemies(chamber);

        // Start monitoring for chamber clear
        StartCoroutine(MonitorChamberClear(chamberIndex));
    }

    /// <summary>
    /// Spawns all enemies for a given chamber.
    /// </summary>
    private void SpawnChamberEnemies(Chamber chamber)
    {
        int basicCount = 0;
        int eliteCount = 0;

        // Spawn basic enemies
        if (basicEnemyPrefab != null && chamber.basicSpawnContainer != null)
        {
            foreach (Transform spawnPoint in chamber.basicSpawnContainer)
            {
                BaseEnemy enemy = SpawnEnemy(basicEnemyPrefab, spawnPoint);
                if (enemy != null)
                {
                    chamber.spawnedEnemies.Add(enemy);
                    basicCount++;
                }
            }
        }

        // Spawn elite enemies
        if (eliteEnemyPrefab != null && chamber.eliteSpawnContainer != null)
        {
            foreach (Transform spawnPoint in chamber.eliteSpawnContainer)
            {
                BaseEnemy enemy = SpawnEnemy(eliteEnemyPrefab, spawnPoint);
                if (enemy != null)
                {
                    chamber.spawnedEnemies.Add(enemy);
                    eliteCount++;
                }
            }
        }

        Debug.Log($"ChamberSpawner: Spawned {basicCount} basic and {eliteCount} elite enemies");
    }

    /// <summary>
    /// Spawns a single enemy at the specified spawn point.
    /// </summary>
    private BaseEnemy SpawnEnemy(GameObject prefab, Transform spawnPoint)
    {
        GameObject enemyObj = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        BaseEnemy enemy = enemyObj.GetComponent<BaseEnemy>();

        if (enemy == null)
        {
            Debug.LogWarning($"ChamberSpawner: Prefab {prefab.name} has no BaseEnemy component!");
        }

        return enemy;
    }

    /// <summary>
    /// Coroutine that periodically checks if all enemies in a chamber are dead.
    /// When cleared, destroys the exit wall.
    /// </summary>
    private IEnumerator MonitorChamberClear(int chamberIndex)
    {
        Chamber chamber = chambers[chamberIndex];

        // Wait until all enemies are dead
        while (true)
        {
            yield return new WaitForSeconds(clearCheckInterval);

            // Remove null/dead enemies from the list
            chamber.spawnedEnemies.RemoveAll(e => e == null || e.IsDead());

            if (chamber.spawnedEnemies.Count == 0)
            {
                break;
            }
        }

        // Chamber cleared!
        chamber.isCleared = true;
        Debug.Log($"ChamberSpawner: Chamber {chamberIndex} CLEARED!");

        // Destroy exit wall after delay (if one exists)
        if (chamber.exitWall != null)
        {
            if (wallDestroyDelay > 0f)
            {
                yield return new WaitForSeconds(wallDestroyDelay);
            }

            Debug.Log($"ChamberSpawner: Destroying exit wall for chamber {chamberIndex}");
            Destroy(chamber.exitWall);
        }
    }

    /// <summary>
    /// Returns the number of alive enemies in a specific chamber.
    /// </summary>
    public int GetAliveEnemyCount(int chamberIndex)
    {
        if (chamberIndex < 0 || chamberIndex >= chambers.Length) return 0;

        Chamber chamber = chambers[chamberIndex];
        chamber.spawnedEnemies.RemoveAll(e => e == null || e.IsDead());
        return chamber.spawnedEnemies.Count;
    }

    /// <summary>
    /// Returns true if a specific chamber has been cleared.
    /// </summary>
    public bool IsChamberCleared(int chamberIndex)
    {
        if (chamberIndex < 0 || chamberIndex >= chambers.Length) return false;
        return chambers[chamberIndex].isCleared;
    }

    /// <summary>
    /// Returns the currently active chamber index (-1 if none).
    /// </summary>
    public int GetActiveChamberIndex() => activeChamberIndex;

    // Visualize spawn points in editor
    private void OnDrawGizmosSelected()
    {
        if (chambers == null) return;

        for (int i = 0; i < chambers.Length; i++)
        {
            Chamber chamber = chambers[i];

            // Draw basic enemy spawn points in blue
            if (chamber.basicSpawnContainer != null)
            {
                Gizmos.color = Color.blue;
                foreach (Transform spawnPoint in chamber.basicSpawnContainer)
                {
                    Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
                    Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + spawnPoint.forward);
                }
            }

            // Draw elite enemy spawn points in red
            if (chamber.eliteSpawnContainer != null)
            {
                Gizmos.color = Color.red;
                foreach (Transform spawnPoint in chamber.eliteSpawnContainer)
                {
                    Gizmos.DrawWireSphere(spawnPoint.position, 0.75f);
                    Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + spawnPoint.forward);
                }
            }

            // Draw trigger in green
            if (chamber.trigger != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(chamber.trigger.transform.position, chamber.trigger.transform.localScale);
            }

            // Draw exit wall in yellow
            if (chamber.exitWall != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(chamber.exitWall.transform.position, chamber.exitWall.transform.localScale);
            }
        }
    }
}