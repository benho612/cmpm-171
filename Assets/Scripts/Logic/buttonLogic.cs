using UnityEngine;
using TMPro;

public class buttonLogic : MonoBehaviour
{
    private UpgradeData assignedData;
    public Player playerInstance;
    [SerializeField] private TextMeshProUGUI buttonText;

    public void setUpButton(UpgradeData data)
    {
        assignedData = data;
        buttonText.text = assignedData.upgradeName;
    }
    public void onClick()
    {
        Debug.Log("Button clicked for upgrade: " + assignedData.upgradeName);
        assignedData.ApplyUpgrade(playerInstance.gameObject);
        Time.timeScale = 1f;
        GameManager.instance.runUpgrades.enabled = false;
    }
}
