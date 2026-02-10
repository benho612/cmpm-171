using UnityEngine;
using TMPro;

public class ButtonLogic : MonoBehaviour{
    private UpgradeData assignedData;
    [SerializeField] private TextMeshProUGUI _buttonText;

    public void SetUpButton(UpgradeData data){
        assignedData = data;
        _buttonText.text = assignedData.upgradeName;
    }
    public void OnClick(){
        GameObject player = GameObject.FindWithTag("Player");

        Debug.Log("Button clicked for upgrade: " + assignedData.upgradeName);
        assignedData.ApplyUpgrade(player);
        Time.timeScale = 1f;
        GameManager.Instance.RunUpgrades.enabled = false;
    }
}
