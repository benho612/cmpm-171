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
            if(data == null) continue;

            string checkID = data.ComboID + "_" + data.RunTimeElement;
            if(checkID == moveID) return data.IsFinisher;
        }
        return false;
    }

    public void ProcessHit(GameObject enemy){
        float baseDamage = GameManager.Instance.PlayerInstance.PlayerRunData.Damage;
        float baseStagger = GameManager.Instance.PlayerInstance.PlayerRunData.StaggerDamage;
        float baseCritChance = GameManager.Instance.PlayerInstance.PlayerRunData.CritChance;
        
        //info from enemy
        string enemyStatus = "None";
        bool isStaggered = false;

        float damageMult = MetaManager.Instance.StatIncreaseCheck("Damage", enemyStatus);
        float staggerMult = MetaManager.Instance.StatIncreaseCheck("Stagger", enemyStatus);
        float critBonus = MetaManager.Instance.StatIncreaseCheck("CritChance", enemyStatus);

        float finalDamage = baseDamage *= (1.0f + damageMult);

        //check for stagger
        if(isStaggered) finalDamage *= 1.5f;
        
        //check for crit
        float totalCritChance = baseCritChance * (1.0f + critBonus);
        bool isCrit = Random.Range(0f, 100f) <= totalCritChance;
        if(isCrit){
            finalDamage *= 2.0f;
        }
        
        float finalStagger = baseStagger * (1.0f + staggerMult);

        //apply damage
        //enemy.TakeDamage(finalDamage);
        //enemy.TakeStagger(finalStagger);

        //handling feature Logic
        if(_activeElement == "Ice" && MetaManager.Instance.FeatureLogicCheck("FlashFreeze")){
            //spawn Cold Zone
        }
        if(_activeElement == "Stone" && MetaManager.Instance.FeatureLogicCheck("StoneArmor")){
            //apply Stone Armor
        }
    }
}
