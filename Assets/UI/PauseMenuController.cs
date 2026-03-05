using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PauseMenuController : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference pauseAction; // UI/Pause (ESC)

    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject keyBindingsPanel;

    [Header("First Selected (optional but recommended)")]
    public GameObject pauseFirstSelected;
    public GameObject keyBindingsFirstSelected;

    [Header("Pause Behavior")]
    public bool pauseTimeScale = true;
    public bool showCursorWhenPaused = true;

    private bool _isPaused;
    private bool _inKeyBindings;

    void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed += OnPausePerformed;
            pauseAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePerformed;
            pauseAction.action.Disable();
        }
    }

    private void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        // ESC behavior:
        // - If in Key Bindings page => go back to Pause panel
        // - Else toggle pause menu
        if (_inKeyBindings)
        {
            OpenPausePanel();
            return;
        }

        if (_isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        _isPaused = true;
        _inKeyBindings = false;

        if (pausePanel) pausePanel.SetActive(true);
        if (keyBindingsPanel) keyBindingsPanel.SetActive(false);

        if (pauseTimeScale) Time.timeScale = 0f;

        ApplyCursorState(true);

        SelectFirst(pauseFirstSelected);
    }

    public void Resume()
    {
        _isPaused = false;
        _inKeyBindings = false;

        if (pausePanel) pausePanel.SetActive(false);
        if (keyBindingsPanel) keyBindingsPanel.SetActive(false);

        if (pauseTimeScale) Time.timeScale = 1f;

        ApplyCursorState(false);

        // Clear selection to avoid weird UI navigation when resuming
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
    }

    public void OpenKeyBindings()
    {
        if (!_isPaused) Pause();

        _inKeyBindings = true;

        if (pausePanel) pausePanel.SetActive(false);
        if (keyBindingsPanel) keyBindingsPanel.SetActive(true);

        SelectFirst(keyBindingsFirstSelected);

        // If your rebind rows display current bindings, refresh them:
        RefreshAllRebindRows();
    }

    public void OpenPausePanel()
    {
        if (!_isPaused) Pause();

        _inKeyBindings = false;

        if (pausePanel) pausePanel.SetActive(true);
        if (keyBindingsPanel) keyBindingsPanel.SetActive(false);

        SelectFirst(pauseFirstSelected);
    }

    private void ApplyCursorState(bool paused)
    {
        if (!showCursorWhenPaused) return;

        Cursor.visible = paused;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void SelectFirst(GameObject first)
    {
        if (first == null || EventSystem.current == null) return;

        // Ensure selectable exists when panel just activated
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(first);
    }

    private void RefreshAllRebindRows()
    {
        if (!keyBindingsPanel) return;
        var rows = keyBindingsPanel.GetComponentsInChildren<RebindActionUI>(true);
        foreach (var row in rows) row.RefreshUI();
    }
}