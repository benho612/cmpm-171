using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class RemapAction : MonoBehaviour
{
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private TMP_Text bindingDisplayNameText;
    [SerializeField] private GameObject overlay;
    
    // The index of the binding. For WASD: 
    // 0 is the Composite, 1 is Up, 2 is Down, 3 is Left, 4 is Right.
    [SerializeField] private int bindingIndex; 

    private InputActionRebindingExtensions.RebindingOperation _rebindOperation;

    void Start() => UpdateUI();

    public void StartRebinding()
    {
        moveAction.action.actionMap.Disable();

        if (overlay != null) overlay.SetActive(true);

        _rebindOperation = moveAction.action.PerformInteractiveRebinding()
            // This is the crucial line for WASD/Composite bindings:
            .WithTargetBinding(bindingIndex) 
            .WithControlsExcluding("Mouse")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => FinishRebinding())
            .Start();
    }

    private void FinishRebinding()
    {
        _rebindOperation.Dispose();
        
        if (overlay != null) overlay.SetActive(false);
        
        moveAction.action.actionMap.Enable(); 
        UpdateUI();
    }

    private void UpdateUI()
    {
        // GetDisplayString can take an index to show only that specific key
        bindingDisplayNameText.text = moveAction.action.GetBindingDisplayString(bindingIndex);
    }
}