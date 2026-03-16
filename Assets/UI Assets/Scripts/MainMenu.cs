using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [Header("UI Screens")]
    public GameObject menuText;
    public GameObject settingsPanel;

    public GameObject loading;
    private Animation anim;

    void Start()
    {
        anim = loading.GetComponent<Animation>();
    }

    public void OpenSettings() {
        menuText.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings() {
        settingsPanel.SetActive(false);
        menuText.SetActive(true);
    }

    public void play_game() 
    {
        StartCoroutine(play_and_load());
    }

    IEnumerator play_and_load() {
        anim.Play("Loading-Transition");
        yield return new WaitForSeconds(anim["Loading-Transition"].length);
        SceneManager.LoadScene("NewLevel1");
    }

    public void goto_credits() 
    {
        SceneManager.LoadScene("Credits");
    }

    public void quit_game()
    {
        Application.Quit();
    }
}
