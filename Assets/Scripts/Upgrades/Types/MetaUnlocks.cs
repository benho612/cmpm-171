using UnityEngine;

public abstract class MetaUnlock : ScriptableObject
{
    public string upgradeName;
    public bool isUnlocked = false;
    
    public MetaUnlock prerequisiteUnlock;
    public bool CanUnlock()
    {
        return prerequisiteUnlock == null || prerequisiteUnlock.isUnlocked;
    }
    public abstract void ApplyMetaUnlock(GameObject player);
}
