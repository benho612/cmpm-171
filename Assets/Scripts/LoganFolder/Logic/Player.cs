using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public RunData PlayerRunData;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
        CombatHandler combatHandler = GetComponent<CombatHandler>();
        //GameManager.Instance.OpenUpgradeMenu();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update(){
        if (Keyboard.current.uKey.wasPressedThisFrame){
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            GameManager.Instance.OpenUpgradeMenu();
        }
    }
}
