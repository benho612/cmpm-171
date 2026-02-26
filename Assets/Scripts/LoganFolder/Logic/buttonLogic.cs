using UnityEngine;
using TMPro;

public class ButtonLogic : MonoBehaviour{
    private UpgradeData _assignedData;
    [SerializeField] private TextMeshProUGUI _buttonText;

    public void SetUpButton(UpgradeData data){
        _assignedData = data;
        _buttonText.text = _assignedData.upgradeName;
        Debug.Log("setup Button");
    }
    public void OnClick(){
        GameObject player = GameObject.FindWithTag("Player");

        Debug.Log("Button clicked for upgrade: " + _assignedData.upgradeName);
        _assignedData.ApplyUpgrade(player);
        GameManager.Instance.AllUpgrades.Remove(_assignedData);
        GameManager.Instance.CloseUpgradeMenu();
    }
}
