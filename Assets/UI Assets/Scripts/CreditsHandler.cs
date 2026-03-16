using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CreditsHandler : MonoBehaviour
{
    public void CloseCredits() {
        SceneManager.LoadScene("Main Menu");
    }
}
