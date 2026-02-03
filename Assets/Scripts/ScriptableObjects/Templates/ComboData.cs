using UnityEngine;

[CreateAssetMenu(fileName = "comboUnlocks", menuName = "playerCombos/comboUnlocks")]
public abstract class comboUnlocks : ScriptableObject
{
    public string comboName;
    public Sprite icon;

    public abstract void UnlockCombo(GameObject Player);
}
