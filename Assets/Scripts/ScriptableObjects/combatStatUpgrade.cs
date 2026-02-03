using UnityEngine;

[CreateAssetMenu(fileName = "statUpgrade", menuName = "CombatUpgrades/statUpgrade")]
public class statUpgrade : UpgradeData
{
    public float damageIncrease;
    public float attackSpeedIncrease;
    public float knockbackIncrease;

    public override void ApplyUpgrade(GameObject Player)
    {
        Player playerScript = Player.GetComponent<Player>();
        if(playerScript != null)
        {
            playerScript.playerRunData.damage += damageIncrease;
            playerScript.playerRunData.attackSpeed += attackSpeedIncrease;
            playerScript.playerRunData.knockback += knockbackIncrease;
        } else
        {
            Debug.LogWarning("Player script not found on the provided GameObject.");
        }  
    }
}
