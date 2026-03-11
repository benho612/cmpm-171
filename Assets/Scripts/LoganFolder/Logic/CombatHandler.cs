using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class CombatHandler : MonoBehaviour{
    public List <ComboUnlock> _allCombos = new List<ComboUnlock>();
    public List <string> UnlockedCombos = new List<string>();
    private List<ComboUnlock> _unlockedComboData = new List<ComboUnlock>();
    private ElementType _activeElement;

    // To remember the attack type for the impact sound
    private bool _isCurrentAttackHeavy;

    private RunData _stats;
    private MetaManager _meta;
    [SerializeField] private AnimationBridge _playerAnimator;
    [SerializeField] private CombatSandBox _combatSandBox;
    [SerializeField] private MovementSandBox _movement;

    //exposing these for the CombatCoordinator so it doesn't read inputs while these are happening
    public bool IsBlocking => _combatSandBox.IsBlocking;
    public bool IsDodging => _combatSandBox.IsDodging;
    public bool IsDashing => _movement.IsDashing;

    [Header("VFX Things")]
    [SerializeField] private GameObject _hitEffectPrefab;

    void Start(){
        _stats = GameManager.Instance.PlayerInstance.PlayerRunData;
        _meta = MetaManager.Instance;
        _stats.ResetStats();
        
        foreach(var combo in _allCombos){
            foreach(string part in combo.RequiredMoveParts){
                UnlockCombo(null, part + "_None");
            }
            UnlockCombo(combo, combo.ComboID + "_None");
        }
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

            AudioManager.Instance.Play("Swoosh");

            // Remembers if this is a heavy attack for when the hit connects
            _isCurrentAttackHeavy = (lastInput == 'H');

            if (IsFinisher(moveID)){
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

//check call for parry/block
    public void ProcessDefenceInput(bool isHoldingDefend)
    {
        if (isHoldingDefend)
        {
            // If we are doing something else, we can't start blocking yet
            if (_combatSandBox.IsAttacking || _movement.IsDashing) return;

            // If we are free, and NOT currently blocking, put the shield up!
            if (!_combatSandBox.IsBlocking)
            {
                _combatSandBox.StartDefense();
            }
        }
        else
        {
            // If we released the button, and we WERE blocking, put the shield down
            if (_combatSandBox.IsBlocking)
            {
                _playerAnimator.StopBlock();
                _combatSandBox.StopDefense();
            }
        }
    }

    public void AttemptStationaryDodge(bool isHigh){
        if(_combatSandBox.IsAttacking || _movement.IsDashing) return;

        if(_combatSandBox.IsBlocking && !_combatSandBox.IsDodging){
            _combatSandBox.ExecuteStationaryDodge(isHigh);
        }
    }

//brings all the damage logic together and sends the damage to the enemy hit
//NOTE - Enemey has no status variable yet so majority of this doesn't work, but the structure is there for when it does
    public void ProcessHit(GameObject enemy, float baseDamage, Vector3 hitboxCenter){
        //not sure if this is actual enemy script but this should link enemy to the hit
        TempStatusScript enemyData = enemy.GetComponent<TempStatusScript>();
        BaseEnemy enemyScript = enemy.GetComponent<BaseEnemy>();
        Animator enemyAnim = enemy.GetComponent<Animator>();
        if(enemyScript == null) {
            Debug.Log("enemyScript = null");
            return;
        }

        float finalDamage = baseDamage; //temp variable creation 
        float baseStagger = _stats.StaggerDamage;
        float baseCritChance = _stats.CritChance;
        
        //info from enemy - this is temporary since enemy does not have a status currently
        //StatusEffect enemyStatus = enemyData.CurrentStatus;
        bool isStaggered = false;

        //float damageMult = _meta.StatIncreaseCheck(StatType.Damage, enemyStatus);
        //float staggerMult = _meta.StatIncreaseCheck(StatType.StaggerDamage, enemyStatus);
        //float critBonus = _meta.StatIncreaseCheck(StatType.CritChance, enemyStatus);

        //float finalDamage = baseDamage * (1.0f + damageMult);

        //check for stagger
        if(isStaggered) finalDamage *= 1.5f;
        
        //check for crit
        //float totalCritChance = baseCritChance * (1.0f + critBonus);
        /*bool isCrit = Random.Range(0f, 100f) <= totalCritChance;
        if(isCrit){
            finalDamage *= 2.0f;
        }
        */
        //float finalStagger = baseStagger * (1.0f + staggerMult);

        //Status Application
        /*if(_activeElement != ElementType.None){
            StatusEffect newStatus = GetStatusFromElement(_activeElement);
            enemyData.CurrentStatus = newStatus;
        }*/
        //apply damage
        if(enemyScript != null && enemyAnim != null){
            Instantiate(_hitEffectPrefab, hitboxCenter, Quaternion.identity);
            enemyScript.TakeDamage(finalDamage);


            // AUDIO LOGIC
            if (enemy.CompareTag("EliteEnemy"))
            {
                // Elite enemy: always play metal sound, regardless of attack type
                AudioManager.Instance.Play("Player_Punching_Metal");
            }
            else
            {
                // Basic enemy: check if it was a heavy or light attack
                if (_isCurrentAttackHeavy)
                {
                    AudioManager.Instance.Play("Player_Heavy_Punch");
                }
                else
                {
                    AudioManager.Instance.Play("Player_Punch");
                }
            }

            StartCoroutine(HitStopRoutine(0.07f, enemyAnim));
        }

        Debug.Log($"Processed hit on {enemy.name} for {finalDamage} total damage!");
        //enemy.TakeStagger(finalStagger);

        //handling feature Logic
        /*
        if(_activeElement == ElementType.Ice && _meta.FeatureLogicCheck("FlashFreeze")){
            //spawn Cold Zone
        }
        if(_activeElement == ElementType.Stone && _meta.FeatureLogicCheck("StoneArmor")){
            //apply Stone Armor
        }
        */
    }

    private IEnumerator HitStopRoutine(float duration, Animator enemyAnim){
        float eSpeed = enemyAnim.speed;
        enemyAnim.speed = 0f;
        _playerAnimator.HitStopAnim();
        yield return new WaitForSeconds(duration);
        enemyAnim.speed = eSpeed;
        _playerAnimator.ResumeAnim();
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
