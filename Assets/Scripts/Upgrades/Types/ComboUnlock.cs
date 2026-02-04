using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ComboUnlock", menuName = "playerCombos/ComboUnlock")]
public class ComboUnlock : UpgradeData
{
    public string comboID;
    public MetaData metaData;
    public override void ApplyUpgrade(GameObject Player)
    {
        Player playerScript = Player.GetComponent<Player>();
        if (playerScript == null)
        {
            Debug.LogWarning("Player script not found on the provided GameObject.");
            return;
        }
        CombatHandler combatHandler = playerScript.GetComponent<CombatHandler>();
        List<string> unlockedElements = metaData.GetUnlockedElementPool();
        string comboString = comboID;
        if(unlockedElements.Count > 0)
        {
            int randomElement = Random.Range(0, unlockedElements.Count);
            comboString += "_" + unlockedElements[randomElement];
        }
        combatHandler.UnlockCombo(comboString);
    }
}
