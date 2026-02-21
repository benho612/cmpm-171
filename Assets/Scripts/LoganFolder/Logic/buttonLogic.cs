using UnityEngine;
using TMPro;

public class ButtonLogic : MonoBehaviour{
    private UpgradeData assignedData;
    [SerializeField] private TextMeshProUGUI _buttonText;

    public void SetUpButton(UpgradeData data){
        assignedData = data;
        _buttonText.text = assignedData.upgradeName;
        Debug.Log("setup Button");
    }
    public void OnClick(){
        GameObject player = GameObject.FindWithTag("Player");

        Debug.Log("Button clicked for upgrade: " + assignedData.upgradeName);
        assignedData.ApplyUpgrade(player);
        GameManager.Instance.CloseUpgradeMenu();
    }
}
