using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ComboUnlock", menuName = "playerCombos/ComboUnlock")]
public class ComboUnlock : UpgradeData{
    public string ComboID;
    public bool IsFinisher;
    public List<string> RequiredMoveParts = new List<string>();

    [HideInInspector]
    public string RunTimeElement = "None";
    
    public override void ApplyUpgrade(GameObject Player){
        Player playerScript = Player.GetComponent<Player>();
        if (playerScript == null){
            Debug.LogWarning("Player script not found on the provided GameObject.");
            return;
        }

        CombatHandler combatHandler = playerScript.GetComponent<CombatHandler>();
        foreach(string part in RequiredMoveParts){
            combatHandler.UnlockCombo(null, part + "_" + RunTimeElement);
        }
        string FinalCombo = ComboID + "_" + RunTimeElement;
        combatHandler.UnlockCombo(this, FinalCombo);
    }
}
