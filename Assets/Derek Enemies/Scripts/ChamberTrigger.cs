using UnityEngine;

/// <summary>
/// Attach to a trigger collider. When the player enters, it notifies
/// the ChamberSpawner to activate the corresponding chamber.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ChamberTrigger : MonoBehaviour
{
    private ChamberSpawner spawner;
    private int chamberIndex;
    private bool hasTriggered;

    /// <summary>
    /// Called by ChamberSpawner during setup.
    /// </summary>
    public void Initialize(ChamberSpawner spawner, int chamberIndex)
    {
        this.spawner = spawner;
        this.chamberIndex = chamberIndex;

        // Ensure the collider is set to trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"ChamberTrigger: Collider on {gameObject.name} was not set to isTrigger. Fixing automatically.");
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;
            Debug.Log($"ChamberTrigger: Player entered trigger for chamber {chamberIndex}");
            spawner.OnChamberTriggered(chamberIndex);

            // Disable the trigger so it can't fire again
            gameObject.SetActive(false);
        }
    }
}