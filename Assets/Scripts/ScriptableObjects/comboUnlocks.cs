using UnityEngine;

public abstract class ComboData : ScriptableObject
{
    public string comboName;
    public Sprite icon;

    public abstract void UnlockCombo(GameObject Player);
}
