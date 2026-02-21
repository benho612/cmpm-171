using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class GameManager : MonoBehaviour{
    public static GameManager Instance;
    public Player PlayerInstance;
    public MetaData MetaData;
    public TextMeshProUGUI TextObject;


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
        TextObject.text = MetaData.MetaCurrency.ToString();
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
        RunUpgrades.gameObject.SetActive(true);
    }

    public void CloseUpgradeMenu(){
        Time.timeScale = 1f;
        RunUpgrades.gameObject.SetActive(false);
    }
//this function will handle the randomized selection of available upgrades to display 
    public void GetUpgradesReady(){
        List<UpgradeData> shuffleList = new List<UpgradeData>(AllUpgrades);
        Debug.Log("allUpgrades count = " + AllUpgrades.Count);
        for(int i = 0; i < UpgradeButtons.Length; i++){
            Debug.Log("Check1");
            int randomIndex = Random.Range(0, shuffleList.Count);
            Debug.Log("Check2" + shuffleList.Count);
            UpgradeData pickedUpgrade = shuffleList[randomIndex];
            Debug.Log("Check3");
            if(pickedUpgrade is ComboUnlock combo){
                List<string> pool = MetaData.GetUnlockedElementPool();
                combo.RunTimeElement = pool[Random.Range(0, pool.Count)];
            }
            
            UpgradeButtons[i].SetUpButton(pickedUpgrade);
            shuffleList.RemoveAt(randomIndex);
        }
    }
}
