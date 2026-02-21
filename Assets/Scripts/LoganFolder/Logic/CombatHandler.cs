using UnityEngine;
using System.Collections.Generic;
public class CombatHandler : MonoBehaviour{
    public List <string> UnlockedCombos = new List<string>();
    private List<ComboUnlock> _unlockedComboData = new List<ComboUnlock>();
    private ElementType _activeElement;
    
    private RunData _stats;
    private MetaManager _meta;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
        _stats = GameManager.Instance.PlayerInstance.PlayerRunData;
        _meta = MetaManager.Instance;
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
        if(IsFinisher(moveID)) _activeElement = (ElementType)System.Enum.Parse(typeof(ElementType), parts[1]);
        else _activeElement = ElementType.None;

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
        //not sure if this is actual enemy script but this should link enemy to the hit
        TempStatusScript enemyData = enemy.GetComponent<TempStatusScript>();

        float baseDamage = _stats.Damage;
        float baseStagger = _stats.StaggerDamage;
        float baseCritChance = _stats.CritChance;
        
        //info from enemy
        StatusEffect enemyStatus = enemyData.CurrentStatus;
        bool isStaggered = false;

        float damageMult = _meta.StatIncreaseCheck(StatType.Damage, enemyStatus);
        float staggerMult = _meta.StatIncreaseCheck(StatType.StaggerDamage, enemyStatus);
        float critBonus = _meta.StatIncreaseCheck(StatType.CritChance, enemyStatus);

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

        //Status Application
        if(_activeElement != ElementType.None){
            StatusEffect newStatus = GetStatusFromElement(_activeElement);
            enemyData.CurrentStatus = newStatus;
        }
        //apply damage
        //enemy.TakeDamage(finalDamage);
        //enemy.TakeStagger(finalStagger);

        //handling feature Logic
        if(_activeElement == ElementType.Ice && _meta.FeatureLogicCheck("FlashFreeze")){
            //spawn Cold Zone
        }
        if(_activeElement == ElementType.Stone && _meta.FeatureLogicCheck("StoneArmor")){
            //apply Stone Armor
        }
    }

    private StatusEffect GetStatusFromElement(ElementType element){
        return element switch{
            ElementType.Fire => StatusEffect.Burning,
            ElementType.Ice => StatusEffect.Chilled,
            ElementType.Stone => StatusEffect.Concussed,
            _ => StatusEffect.None
        };
    }
}
