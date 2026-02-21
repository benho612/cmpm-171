using UnityEngine;

[CreateAssetMenu(fileName = "statUpgrade", menuName = "CombatUpgrades/statUpgrade")]
public class CombatStatUpgrade : UpgradeData
{
    public string UpgradeDescription;

    [Header("Combat Stat Increases")]
    public float DamageIncrease;
    public float AttackSpeedIncrease;
    public float KnockbackIncrease;
    public float WallSlamDamageIncrease;
    public float StaggerDamageIncrease;

    [Header("Health Increases")]
    public float MaxHealthIncrease;
    public float HealthIncrease;
    public float LifeStealIncrease;

    public override void ApplyUpgrade(GameObject Player)
    {
        Player playerScript = Player.GetComponent<Player>();
        if(playerScript != null)
        {
            playerScript.PlayerRunData.Damage += DamageIncrease;
            playerScript.PlayerRunData.AttackSpeed += AttackSpeedIncrease;
            playerScript.PlayerRunData.Knockback += KnockbackIncrease;
            playerScript.PlayerRunData.WallSlamDamage += WallSlamDamageIncrease;
            playerScript.PlayerRunData.StaggerDamage += StaggerDamageIncrease;
            playerScript.PlayerRunData.MaxHealth += MaxHealthIncrease;
            playerScript.PlayerRunData.CurrentHealth += HealthIncrease;
            playerScript.PlayerRunData.LifeSteal += LifeStealIncrease;
        } else
        {
            Debug.LogWarning("Player script not found on the provided GameObject.");
        }  
    }
}
