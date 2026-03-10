using UnityEngine;
using UnityEngine.InputSystem;

public class CombatCoordinator : MonoBehaviour
{
    [Header("Input References")]
    [Tooltip("Drag the 'Move' InputActionReference here")]
    public InputActionReference moveAction;
    [Tooltip("Drag the 'Light Attack' InputActionReference here")]
    public InputActionReference lightAttackAction;
    [Tooltip("Drag the 'Heavy Attack' InputActionReference here")]
    public InputActionReference heavyAttackAction;
    [Tooltip("Drag the 'Defend' InputActionReference here")]
    public InputActionReference defendAction;

    [SerializeField] private CombatHandler _combatHandler;

    public string RecordedCombo = "";
    public float LastInputTime = 0f;
    public float ComboResetTime = 0.8f;

    //prevents holding the button to dodge
    private float _lastMoveY;

    private void Awake()
    {
        // Subscribe to our attack events using the references
        if (lightAttackAction != null)
        {
            lightAttackAction.action.performed += ctx => RecordInput('L');
        }

        if (heavyAttackAction != null)
        {
            heavyAttackAction.action.performed += ctx => RecordInput('H');
        }
    }

    private void OnEnable()
    {
        moveAction?.action.Enable();
        lightAttackAction?.action.Enable();
        heavyAttackAction?.action.Enable();
        defendAction?.action.Enable();
    }

    private void OnDisable()
    {
        moveAction?.action.Disable();
        lightAttackAction?.action.Disable();
        heavyAttackAction?.action.Disable();
        defendAction?.action.Disable();
    }

    private void Update()
    {
        // Read movement securely
        Vector2 moveInput = Vector2.zero;
        if (moveAction != null)
        {
            moveInput = moveAction.action.ReadValue<Vector2>();
        }

        // Read defense securely
        bool isHoldingDefend = false;
        if (defendAction != null)
        {
            isHoldingDefend = defendAction.action.IsPressed();
        }

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

    public void RecordInput(char input)
    {
        if (_combatHandler.IsBlocking || _combatHandler.IsDodging || _combatHandler.IsDashing) return;
        if (Time.time - LastInputTime > ComboResetTime) RecordedCombo = "";

        string potentialCombo = RecordedCombo + input;
        string bestMatch = "";

        foreach (string combo in _combatHandler.UnlockedCombos)
        {
            if (combo.Split('_')[0] == potentialCombo)
            {
                bestMatch = combo;
                break;
            }
        }

        bool success = false;

        //ifElse to make sure something happens even if there is no combo available 
        if (bestMatch != "")
        {
            success = _combatHandler.ExecuteMove(bestMatch, true); //true makes an interruption in the combo animation possible
        }
        else
        {
            success = _combatHandler.ExecuteMove(input.ToString() + "_None", false); //false is for the non combo moves
            if (success) RecordedCombo = "";
        }

        if (success)
        { //only updates the combo if a move was actually able to be fired
            RecordedCombo = potentialCombo;
            LastInputTime = Time.time;

            if (_combatHandler.IsFinisher(bestMatch) || RecordedCombo.Length > 5) RecordedCombo = "";
        }
    }
}