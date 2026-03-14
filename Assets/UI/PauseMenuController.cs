using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference pauseAction;

    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject deathScreen;
    public GameObject settingsPanel;       // Replaces the old keyBindingsPanel
    public GameObject generalTabPanel;     // Inside Settings
    public GameObject keyBindingsTabPanel; // Inside Settings

    [Header("First Selected (for Gamepad/Keyboard navigation)")]
    public GameObject pauseFirstSelected;
    public GameObject settingsFirstSelected; // E.g., The "General" tab button

    [Header("Pause Behavior")]
    public bool pauseTimeScale = true;
    public bool showCursorWhenPaused = true;

    private bool _isPaused;
    private bool _inSettings;

    public void onDeath()
    {
        Debug.Log("trigger worked");
        _isPaused = true;

        deathScreen.SetActive(true);

        if (pauseTimeScale) Time.timeScale = 0f;
        ApplyCursorState(true);
        SelectFirst(pauseFirstSelected);
    }

    public void Restart()
    {
        Time.timeScale = 1f;

        // Destroy persistent singletons so the next scene load behaves like a fresh load
        if (GameManager.Instance != null) Destroy(GameManager.Instance.gameObject);
        if (AudioManager.Instance != null) Destroy(AudioManager.Instance.gameObject);
        // clear known static refs as well
        EnemyCombatManager.Instance = null;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

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
        if (_inSettings)
        {
            OpenPausePanel();
            return;
        }

        if (_isPaused) Resume();
        else Pause();
    }

    public void Quit() 
    {
        Time.timeScale = 1f;

        // Destroy persistent singletons so the next scene load behaves like a fresh load
        if (GameManager.Instance != null) Destroy(GameManager.Instance.gameObject);
        if (AudioManager.Instance != null) Destroy(AudioManager.Instance.gameObject);
        // clear known static refs as well
        EnemyCombatManager.Instance = null;
        SceneManager.LoadScene("Main Menu");
    }

    public void Pause()
    {
        _isPaused = true;
        _inSettings = false;

        if (pausePanel) pausePanel.SetActive(true);
        if (settingsPanel) settingsPanel.SetActive(false);

        if (pauseTimeScale) Time.timeScale = 0f;
        ApplyCursorState(true);
        SelectFirst(pauseFirstSelected);
    }

    public void Resume()
    {
        _isPaused = false;
        _inSettings = false;

        if (pausePanel) pausePanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);

        if (pauseTimeScale) Time.timeScale = 1f;
        ApplyCursorState(false);

        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
    }

    public void OpenPausePanel()
    {
        if (!_isPaused) Pause();
        _inSettings = false;

        if (pausePanel) pausePanel.SetActive(true);
        if (settingsPanel) settingsPanel.SetActive(false);

        SelectFirst(pauseFirstSelected);
    }

    // Opens the Settings Panel and defaults to the General Tab
    public void OpenSettings()
    {
        if (!_isPaused) Pause();
        _inSettings = true;

        if (pausePanel) pausePanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(true);

        //ShowGeneralTab();
        //SelectFirst(settingsFirstSelected);
    }

    public void CloseSettings()
    {
        pausePanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    // Tab Button Methods
    public void ShowGeneralTab()
    {
        if (generalTabPanel) generalTabPanel.SetActive(true);
        if (keyBindingsTabPanel) keyBindingsTabPanel.SetActive(false);
    }

    public void ShowKeyBindingsTab()
    {
        if (generalTabPanel) generalTabPanel.SetActive(false);
        if (keyBindingsTabPanel) keyBindingsTabPanel.SetActive(true);

        RefreshAllRebindRows();
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
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(first);
    }

    private void RefreshAllRebindRows()
    {
        if (!keyBindingsTabPanel) return;
        var rows = keyBindingsTabPanel.GetComponentsInChildren<RebindActionUI>(true);
        foreach (var row in rows) row.RefreshUI();
    }
}
