using UnityEngine;
using System.Collections.Generic;
public class CombatHandler : MonoBehaviour{
    public List <string> UnlockedCombos = new List<string>();
    private List<ComboUnlock> _unlockedComboData = new List<ComboUnlock>();
    private ElementType _activeElement;
    
    private RunData _stats;
    private MetaManager _meta;
    [SerializeField] private AnimationBridge _playerAnimator;
    [SerializeField] private CombatSandBox _combatSandBox;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
        _stats = GameManager.Instance.PlayerInstance.PlayerRunData;
        _meta = MetaManager.Instance;
        _playerAnimator.GetComponent<AnimationBridge>();
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

    public bool ExecuteMove(string moveID, bool canInterrupt){
        string[] parts = moveID.Split('_');
        string moveName = parts[0];

        bool attackStarted = false;
        char lastInput = moveName[moveName.Length - 1];
        int step = moveName.Length;
        string animationToPlay = "";

        if(lastInput == 'L'){
            int index = ((step - 1) % 2) + 1;
            animationToPlay = "Light_" + index;
            Debug.Log(animationToPlay);
        }else if(lastInput == 'H'){
            int index = ((step - 1) % 2) + 1;
            animationToPlay = "Heavy_" + index;
        }

        if (lastInput == 'L'){
            attackStarted = _combatSandBox.ExecutePhysicalAttack(
                canInterrupt, 
                _combatSandBox.lightAttackDuration, 
                _combatSandBox.lightHitboxSize, 
                _combatSandBox.lightAttackColor, 
                _stats.LightAttackDamage
            );
        }
        else{
            attackStarted = _combatSandBox.ExecutePhysicalAttack(
                canInterrupt, 
                _combatSandBox.heavyAttackDuration, 
                _combatSandBox.heavyHitboxSize, 
                _combatSandBox.heavyAttackColor, 
                _stats.HeavyAttackDamage
            );
        }
        
        if(attackStarted){
            _playerAnimator.PlayAttack(animationToPlay);

            if(IsFinisher(moveID)){
            _activeElement = (ElementType)System.Enum.Parse(typeof(ElementType), parts[1]);
            //Later implementation of VFX script 
            // VFXManager.Instance.PlayEffect(_activeElement, transform.position);
            }else _activeElement = ElementType.None;
        }
        return attackStarted;
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

    public void ProcessHit(GameObject enemy, float baseDamage){
        //not sure if this is actual enemy script but this should link enemy to the hit
        TempStatusScript enemyData = enemy.GetComponent<TempStatusScript>();
        BaseEnemy enemyScript = enemy.GetComponent<BaseEnemy>();
        if(enemyScript == null) {
            Debug.Log("enemyScript = null");
            return;
        }

        float baseStagger = _stats.StaggerDamage;
        float baseCritChance = _stats.CritChance;
        
        //info from enemy - this is temporary since enemy does not have a status currently
        StatusEffect enemyStatus = enemyData.CurrentStatus;
        bool isStaggered = false;

        float damageMult = _meta.StatIncreaseCheck(StatType.Damage, enemyStatus);
        float staggerMult = _meta.StatIncreaseCheck(StatType.StaggerDamage, enemyStatus);
        float critBonus = _meta.StatIncreaseCheck(StatType.CritChance, enemyStatus);

        float finalDamage = baseDamage * (1.0f + damageMult);

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
        enemyScript.TakeDamage(finalDamage);
        Debug.Log($"Processed hit on {enemy.name} for {finalDamage} total damage!");
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
