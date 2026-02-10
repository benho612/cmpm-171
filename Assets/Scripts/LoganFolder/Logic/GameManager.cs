using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour{
    public static GameManager Instance;
    public Player PlayerInstance;
    public MetaData MetaData;


//list and array of all possible upgrades and the buttons that will display them
    public List<UpgradeData> AllUpgrades;
    public ButtonLogic[] UpgradeButtons;
//canvas the buttons are being held on
    public Canvas RunUpgrades;

    void Awake(){
        if (Instance == null){
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
        
    }

    // Update is called once per frame
    void Update(){
        
    }

//this will handle the scene switching when the players dies as well as the meta progression transfers
    public void HandleDeath(){
        
    }
//this will update the UI for gold and health
    public void UpdateUI(){
        
    }

    public void OpenUpgradeMenu(){
        Time.timeScale = 0f;
        GetUpgradesReady();
        RunUpgrades.enabled = true;
    }
//this function will handle the randomized selection of available upgrades to display 
    public void GetUpgradesReady(){
        List<UpgradeData> shuffleList = new List<UpgradeData>();
        for(int i = 0; i < AllUpgrades.Count; i++){
            int randomIndex = Random.Range(0, shuffleList.Count);
            UpgradeData pickedUpgrade = shuffleList[randomIndex];
            
            if(pickedUpgrade is ComboUnlock combo){
                List<string> pool = MetaData.GetUnlockedElementPool();
                combo.RunTimeElement = pool[Random.Range(0, pool.Count)];
            }
            
            UpgradeButtons[i].SetUpButton(pickedUpgrade);
            shuffleList.RemoveAt(randomIndex);
        }
    }
}
