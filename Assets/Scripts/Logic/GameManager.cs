using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public Player playerInstance;


//list and array of all possible upgrades and the buttons that will display them
    public List<UpgradeData> allUpgrades;
    public buttonLogic[] upgradeButtons;
//canvas the buttons are being held on
    public Canvas runUpgrades;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

//this will handle the scene switching when the players dies as well as the meta progression transfers
    public void handleDeath()
    {
        
    }
//this will update the UI for gold and health
    public void updateUI()
    {
        
    }
//this function will make the upgrade menu appear
    public void openUpgradeMenu()
    {
        Time.timeScale = 0f;
        getUpgradesReady();
        runUpgrades.enabled = true;
    }
//this function will handle the randomized selection of available upgrades to display 
    public void getUpgradesReady()
    {
        List<UpgradeData> shuffleList = new List<UpgradeData>();
        for(int i = 0; i < allUpgrades.Count; i++)
        {
            int randomIndex = Random.Range(0, shuffleList.Count);
            upgradeButtons[i].setUpButton(shuffleList[randomIndex]);
            shuffleList.RemoveAt(randomIndex);
        }
    }
}
