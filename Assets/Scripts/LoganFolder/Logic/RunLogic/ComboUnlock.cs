using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ComboUnlock", menuName = "playerCombos/ComboUnlock")]
public class ComboUnlock : UpgradeData{
    public string ComboID;
    public bool IsFinisher;

    [HideInInspector]
    public string RunTimeElement = "None";
    
    public override void ApplyUpgrade(GameObject Player){
        Player playerScript = Player.GetComponent<Player>();
        if (playerScript == null){
            Debug.LogWarning("Player script not found on the provided GameObject.");
            return;
        }

        CombatHandler combatHandler = playerScript.GetComponent<CombatHandler>();
        string FinalCombo = ComboID + "_" + RunTimeElement;
        combatHandler.UnlockCombo(this, FinalCombo);
    }
}
