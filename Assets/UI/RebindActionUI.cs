using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RebindActionUI : MonoBehaviour
{
    [Header("References")]
    public InputActionReference actionReference; // drag the action here
    public int bindingIndex = 0;                 // which binding to rebind (0,1,2...)
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

        // binding info
        var binding = action.bindings[bindingIndex];

        if (actionNameText)
        {
            if (binding.isPartOfComposite && !string.IsNullOrEmpty(binding.name))
                actionNameText.text = $"{action.name} {binding.name}";   // "Move Up"
            else
                actionNameText.text = action.name;                        // "Jump"
        }

        if (bindingText)
        {
            // This safely grabs the visual string for the current key (e.g., "Space", "W", "LMB")
            bindingText.text = action.GetBindingDisplayString(bindingIndex);
        }
    }

    public void StartRebind()
    {
        CancelRebindIfActive();

        var action = actionReference.action;

        // Disable action while rebinding to avoid triggering gameplay
        action.Disable();

        // Change the button text directly to show it is listening
        if (bindingText)
        {
            bindingText.text = "Press any key...";
        }

        // Start rebinding only this specific binding index
        _rebindOp = action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            // Optional: avoid binding mouse movement, etc.
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .OnMatchWaitForAnother(0.1f)
            .OnCancel(op =>
            {
                CleanupAfterRebind(action);
            })
            .OnComplete(op =>
            {
                CleanupAfterRebind(action);

                // Save after a successful rebind
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
        RefreshUI(); // This will automatically change the text from "Press any key..." to the newly bound key!
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
}