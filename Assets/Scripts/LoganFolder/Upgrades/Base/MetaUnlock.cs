using UnityEngine;

public abstract class MetaUnlock : ScriptableObject{
    [Header("Meta Base Info")]
    public string UpgradeName;
    public string Description;
    public float Cost;
    public bool IsUnlocked = false;
    public Sprite Icon;
    public string ElementCategory; //Fire, Ice, Earth
    
    public MetaUnlock PrerequisiteUnlock;

    public bool CanUnlock(){
        return PrerequisiteUnlock == null || PrerequisiteUnlock.IsUnlocked;
    }

    public abstract void ApplyMetaUnlock();
}
