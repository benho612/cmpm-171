using UnityEngine;
using System.Collections.Generic;
public class CombatHandler : MonoBehaviour{
    public List <string> UnlockedCombos = new List<string>();
    private List<ComboUnlock> _unlockedComboData = new List<ComboUnlock>();
    private string _activeElement;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
        
    }

//logic to unlock combos
    public void UnlockCombo(ComboUnlock comboSO, string comboID){
        if(!UnlockedCombos.Contains(comboID)){
            UnlockedCombos.Add(comboID);
            _unlockedComboData.Add(comboSO);
            Debug.Log("Combo Unlocked: " + comboID);
        }
        else{
            Debug.Log("Combo already unlocked: " + comboID);
        }
    }

    public void ExecuteMove(string moveID){
        string[] parts = moveID.Split('_');
        string moveName = parts[0];
        if(IsFinisher(moveID)) _activeElement = parts[1];
        else _activeElement = "None";

        //Later implementation of VFX script
        // VFXManager.Instance.PlayEffect(_activeElement, transform.position);

        //For animation
        // playerAnimator.SetTrigger(moveName);
        // playerAnimator.SetInteger("ElementID", GetElementID(_activeElement));

        //the actual attack
        //CombatScript.PerformAttack(moveName);
    }

//helper function to see if the combo is the finishing part of the combo and not mid combo
    public bool IsFinisher(string moveID){
        foreach(var data in _unlockedComboData){
            string checkID = data.ComboID + "_" + data.RunTimeElement;
            if(checkID == moveID) return data.IsFinisher;
        }
        return false;
    }

    public void ProcessHit(GameObject enemy){
        float finalDamage = GameManager.Instance.PlayerInstance.playerRunData.damage;
        string enemyStatus = "None";

        float multiplier = MetaManager.Instance.StatIncreaseCheck("Damage", enemyStatus);
        finalDamage *= (1.0f + multiplier);
    }
}
