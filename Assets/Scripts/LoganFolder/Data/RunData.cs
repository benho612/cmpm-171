using UnityEngine;

[CreateAssetMenu(fileName = "RunData", menuName = "PlayerRunData/runData")]
public class RunData : ScriptableObject{
    [Header("Combat Data")]
    public float Damage;
    public float AttackSpeed;
    public float Knockback;
    public float StaggerDamage;

    [Header("Advanced Combat Data")]
    public float CritChance;
    public float WallSlamDamage;
    public float ParryStaggerAmount;

    [Header("Health Data")]
    public float CurrentHealth;
    public float MaxHealth;
    public float HealthRegenRate;
    public float LifeSteal;

    public void ResetStats(){
        Damage = 10f;
        AttackSpeed = 5f;
        Knockback = 5f;
        StaggerDamage = 3f;
        CritChance = 1f;
        WallSlamDamage = 20f;
        ParryStaggerAmount = 20;
        CurrentHealth = 100f;
        MaxHealth = 100f;
        HealthRegenRate = 0f;
        LifeSteal = 0;
    }
}
