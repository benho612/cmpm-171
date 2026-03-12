using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void play_game() 
    {
        SceneManager.LoadScene("NewLevel1");
    }
}
