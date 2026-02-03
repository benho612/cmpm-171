using UnityEngine;
using System.Collections.Generic;
public class CombatHandler : MonoBehaviour
{
    List <string> unlockedCombos = new List<string>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

//logic to unlock combos
    public void UnlockCombo(string comboID)
    {
        if(!unlockedCombos.Contains(comboID))
        {
            unlockedCombos.Add(comboID);
            Debug.Log("Combo Unlocked: " + comboID);
        }
        else
        {
            Debug.Log("Combo already unlocked: " + comboID);
        }
    }
}
