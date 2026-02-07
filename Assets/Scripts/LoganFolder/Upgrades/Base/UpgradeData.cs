using UnityEngine;

public abstract class UpgradeData : ScriptableObject
{
    public string upgradeName;
    public Sprite icon;
    public abstract void ApplyUpgrade(GameObject player);
}
