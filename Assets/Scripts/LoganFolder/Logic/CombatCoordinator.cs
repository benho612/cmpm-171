using UnityEngine;

public class CombatCoordinator : MonoBehaviour
{
    [SerializeField] private CombatHandler _combatHandler;

    private PlayerControls _input;
    public string RecordedCombo = "";
    public float LastInputTime = 0f;
    public float ComboResetTime = 0.8f;

    private void Start(){
        _input = new PlayerControls();
        _input.Gameplay.LightAttack.performed += ctx => RecordInput('L');
        _input.Gameplay.HeavyAttack.performed += ctx => RecordInput('H');
    }

    public void RecordInput(char input){
        Debug.Log("test1");
        if(Time.time - LastInputTime > ComboResetTime) RecordedCombo = "";
        
        RecordedCombo += input;
        LastInputTime = Time.time;
        
        string bestMatch = "";

        foreach(string combo in _combatHandler.UnlockedCombos){
            string[] parts = combo.Split('_');
            if(parts[0] == RecordedCombo){
                bestMatch = combo;
                //call combat script 
                break;
            }
        } 
        
        //ifElse to make sure something happens even if there is no combo available  
        if(bestMatch != ""){
            _combatHandler.ExecuteMove(bestMatch);
            if(_combatHandler.IsFinisher(bestMatch)){
                RecordedCombo = "";
            }
        } else{
            _combatHandler.ExecuteMove(input.ToString() + "_None");
            if(RecordedCombo.Length > 5) RecordedCombo = "";
            Debug.Log("test");
        }
    }
}
