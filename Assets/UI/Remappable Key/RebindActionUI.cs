using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class RebindActionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputActionReference actionReference;
    [SerializeField] private int bindingIndex;
    [SerializeField] private TextMeshProUGUI actionNameText; // This text is now preserved
    [SerializeField] private TextMeshProUGUI bindingText;
    [SerializeField] private Button rebindButton;

    [Header("UX Overlay")]
    [SerializeField] private GameObject listeningOverlay;

    private InputActionRebindingExtensions.RebindingOperation _rebindOp;

    private void OnEnable()
    {
        if (rebindButton != null)
            rebindButton.onClick.AddListener(StartRebind);

        RefreshUI();
    }

    private void OnDisable()
    {
        if (rebindButton != null)
            rebindButton.onClick.RemoveListener(StartRebind);

        Cleanup();
    }

    public void RefreshUI()
    {
        if (actionReference == null || actionReference.action == null) return;

        var action = actionReference.action;

        // We are no longer setting actionNameText.text.
        // This allows you to keep the custom labels you wrote in the Inspector.

        if (bindingText != null)
        {
            bindingText.text = action.GetBindingDisplayString(bindingIndex);
        }
    }

    public void StartRebind()
    {
        var action = actionReference.action;
        action.Disable();

        if (listeningOverlay != null) listeningOverlay.SetActive(true);

        _rebindOp = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(op => FinishRebind())
            .Start();
    }

    private void FinishRebind()
    {
        Cleanup();
        RefreshUI();
    }

    private void Cleanup()
    {
        _rebindOp?.Dispose();
        _rebindOp = null;

        if (listeningOverlay != null) listeningOverlay.SetActive(false);

        if (actionReference != null)
            actionReference.action.Enable();
    }
}