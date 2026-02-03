using UnityEngine;

[CreateAssetMenu(fileName = "ComboUnlock", menuName = "playerCombos/ComboUnlock")]
public class ComboUnlock : UpgradeData
{
    public string comboID;
    public override void ApplyUpgrade(GameObject Player)
    {
        Player playerScript = Player.GetComponent<Player>();
        if (playerScript != null)
        {
            // Assuming the Player script has a method to unlock combos
            playerScript.combatHandler.UnlockCombo(comboID);
        }
        else
        {
            Debug.LogWarning("Player script not found on the provided GameObject.");
        }
    }
}
