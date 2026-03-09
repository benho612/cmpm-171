using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RebindActionUI : MonoBehaviour
{
    [Header("References")]
    public InputActionReference actionReference;
    public int bindingIndex = 0;
    public TextMeshProUGUI actionNameText;
    public TextMeshProUGUI bindingText;
    public Button rebindButton;
    public Button resetButton;

    private InputActionRebindingExtensions.RebindingOperation _rebindOp;

    void OnEnable()
    {
        if (rebindButton) rebindButton.onClick.AddListener(StartRebind);
        if (resetButton) resetButton.onClick.AddListener(ResetBinding);

        RefreshUI();
    }

    void OnDisable()
    {
        if (rebindButton) rebindButton.onClick.RemoveListener(StartRebind);
        if (resetButton) resetButton.onClick.RemoveListener(ResetBinding);

        CancelRebindIfActive();
    }

    public void RefreshUI()
    {
        var action = actionReference.action;
        var binding = action.bindings[bindingIndex];

        /*if (actionNameText)
        {
            if (binding.isPartOfComposite && !string.IsNullOrEmpty(binding.name))
                actionNameText.text = $"{action.name} {binding.name}";
            else
                actionNameText.text = action.name;
        }*/

        if (bindingText)
        {
            bindingText.text = action.GetBindingDisplayString(bindingIndex);
        }
    }

    public void StartRebind()
    {
        CancelRebindIfActive();

        var action = actionReference.action;
        action.Disable();

        if (bindingText)
        {
            bindingText.text = "Press any key...";
        }

        _rebindOp = action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .OnMatchWaitForAnother(0.1f)
            .OnCancel(op =>
            {
                CleanupAfterRebind(action);
                RefreshUI(); // Reverts back to the original key text
            })
            .OnComplete(op =>
            {
                // --- DUPLICATE CHECK LOGIC ---
                if (IsDuplicateBinding(action, bindingIndex))
                {
                    action.RemoveBindingOverride(bindingIndex); // Throw away the duplicate input
                    CleanupAfterRebind(action);

                    if (bindingText)
                    {
                        // Show a red warning directly on the button!
                        bindingText.text = "<color=red>Already Used</color>";
                    }
                    return;
                }

                // If it's a unique key, save it and update the UI normally
                CleanupAfterRebind(action);
                RebindSaveLoad.SaveAllBindings(action.actionMap.asset);
                RefreshUI();
            });

        _rebindOp.Start();
    }

    public void ResetBinding()
    {
        var action = actionReference.action;
        action.RemoveBindingOverride(bindingIndex);

        RebindSaveLoad.SaveAllBindings(action.actionMap.asset);
        RefreshUI();
    }

    private void CleanupAfterRebind(InputAction action)
    {
        _rebindOp?.Dispose();
        _rebindOp = null;
        action.Enable();
    }

    private void CancelRebindIfActive()
    {
        if (_rebindOp != null)
        {
            _rebindOp.Cancel();
            _rebindOp.Dispose();
            _rebindOp = null;
        }
    }

    // --- NEW METHOD ---
    private bool IsDuplicateBinding(InputAction action, int mappedBindingIndex)
    {
        InputBinding newBinding = action.bindings[mappedBindingIndex];

        // Look through every single binding in the entire Action Map
        foreach (InputBinding existingBinding in action.actionMap.bindings)
        {
            // Don't compare the binding against itself
            if (existingBinding.action == newBinding.action && existingBinding.id == newBinding.id)
                continue;

            // If the literal hardware path (e.g., "<Keyboard>/space") matches another action's path
            if (existingBinding.effectivePath == newBinding.effectivePath)
            {
                return true;
            }
        }
        return false;
    }
}