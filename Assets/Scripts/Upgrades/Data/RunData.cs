using UnityEngine;

[CreateAssetMenu(fileName = "RunData", menuName = "PlayerRunData/runData")]
public class RunData : ScriptableObject
{
    [Header("Combat Data")]
    public float damage;
    public float attackSpeed;
    public float knockback;
    public float staggerDamage;
    [Header("Health Data")]
    public float currentHealth;
    public float maxHealth;
    public float healthRegenRate;

    public void ResetStats()
    {
        damage = 10f;
        attackSpeed = 1f;
        knockback = 5f;
        staggerDamage = 3f;
        currentHealth = 100f;
        maxHealth = 100f;
        healthRegenRate = 0f;
    }
}
