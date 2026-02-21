using UnityEngine;

public class Player : MonoBehaviour
{
    public RunData PlayerRunData;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
        CombatHandler combatHandler = GetComponent<CombatHandler>();
        GameManager.Instance.OpenUpgradeMenu();
    }

    // Update is called once per frame
    void Update(){
        
    }
}
