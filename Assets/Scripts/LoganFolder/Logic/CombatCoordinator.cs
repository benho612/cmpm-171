using UnityEngine;

public class CombatCoordinator : MonoBehaviour
{
    [SerializeField] private CombatHandler _combatHandler;

    private PlayerControls _input;
    public string RecordedCombo = "";
    public float LastInputTime = 0f;
    public float ComboResetTime = 0.8f;
    //prevents holding the button to dodge
    private float _lastMoveY;

    private void Start(){
        _input = new PlayerControls();
        _input.Enable();
        _input.Gameplay.LightAttack.performed += ctx => RecordInput('L');
        _input.Gameplay.HeavyAttack.performed += ctx => RecordInput('H');
    }

    private void Update(){
        Vector2 moveInput = _input.Gameplay.Move.ReadValue<Vector2>();

        bool isHoldingDefend = _input.Gameplay.Defend.IsPressed();
        _combatHandler.ProcessDefenceInput(isHoldingDefend);
        
        // Only allow a stationary dodge if blocking, not already dodging, and not dashing
        if (_combatHandler.IsBlocking && !_combatHandler.IsDodging && !_combatHandler.IsDashing)
        {
            if (moveInput.y > 0.5f && _lastMoveY <= 0.5f) 
            {
                _combatHandler.AttemptStationaryDodge(true);
            }
            else if (moveInput.y < -0.5f && _lastMoveY >= -0.5f)
            {
                _combatHandler.AttemptStationaryDodge(false);
            }
        }

        // Store this frame's Y input so we can compare it next frame
        _lastMoveY = moveInput.y;
    }

    public void RecordInput(char input){
        if(_combatHandler.IsBlocking || _combatHandler.IsDodging || _combatHandler.IsDashing) return;
        if(Time.time - LastInputTime > ComboResetTime) RecordedCombo = "";
        
        string potentialCombo = RecordedCombo + input;
        string bestMatch = "";
        
        foreach(string combo in _combatHandler.UnlockedCombos){
            if(combo.Split('_')[0] == potentialCombo){
                bestMatch = combo;
                break;
            }
        } 
        
        bool success = false;
        //ifElse to make sure something happens even if there is no combo available 
        if(bestMatch != ""){
            success = _combatHandler.ExecuteMove(bestMatch, true);//true makes a interruption in the combo animation possible
        } else{
            success = _combatHandler.ExecuteMove(input.ToString() + "_None", false);//false is for the non combo moves
            if(success) RecordedCombo = "";
        }

        if(success){ //only updates the combo if a move was actually able to be fired
            RecordedCombo = potentialCombo;
            LastInputTime = Time.time;

            if(_combatHandler.IsFinisher(bestMatch) || RecordedCombo.Length > 5) RecordedCombo = "";
        }
    }
}