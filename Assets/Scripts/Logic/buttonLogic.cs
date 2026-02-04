using UnityEngine;
using TMPro;

public class ButtonLogic : MonoBehaviour
{
    private UpgradeData assignedData;
    public Player playerInstance;
    [SerializeField] private TextMeshProUGUI buttonText;

    public void SetUpButton(UpgradeData data)
    {
        assignedData = data;
        buttonText.text = assignedData.upgradeName;
    }
    public void OnClick()
    {
        Debug.Log("Button clicked for upgrade: " + assignedData.upgradeName);
        assignedData.ApplyUpgrade(playerInstance.gameObject);
        Time.timeScale = 1f;
        GameManager.instance.runUpgrades.enabled = false;
    }
}
