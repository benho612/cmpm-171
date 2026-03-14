using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void play_game() 
    {
        SceneManager.LoadScene("NewLevel1");
    }

    public void quit_game()
    {
        Application.Quit();
    }
}
