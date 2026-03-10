using UnityEngine;

/// <summary>
/// Attach this to the sword GameObject that has a CapsuleCollider (set as Trigger).
/// The collider starts disabled and is toggled via animation events on the enemy.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SwordHitbox : MonoBehaviour
{
    private Collider hitboxCollider;
    private BaseEnemy ownerEnemy;
    private float pendingDamage;

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.isTrigger = true;
        hitboxCollider.enabled = false;

        // Walk up the hierarchy to find the owning enemy
        ownerEnemy = GetComponentInParent<BaseEnemy>();
        if (ownerEnemy == null)
        {
            Debug.LogError($"SwordHitbox on {gameObject.name}: No BaseEnemy found in parent hierarchy!");
        }
    }

    /// <summary>
    /// Call from animation event to activate the hitbox with a specific damage value.
    /// </summary>
    public void EnableHitbox(float damage)
    {
        pendingDamage = damage;
        hitboxCollider.enabled = true;
    }

    /// <summary>
    /// Call from animation event to deactivate the hitbox.
    /// </summary>
    public void DisableHitbox()
    {
        hitboxCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hitboxCollider.enabled) return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        playerHealth.TakeDamage(pendingDamage);
        Debug.Log($"{ownerEnemy?.gameObject.name}: Sword collider hit player for {pendingDamage} damage!");

        // Disable after hitting so we don't multi-hit in one swing
        hitboxCollider.enabled = false;
    }
}