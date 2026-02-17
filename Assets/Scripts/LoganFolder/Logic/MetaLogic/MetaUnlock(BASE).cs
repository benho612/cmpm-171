using UnityEngine;
    
    public enum ElementType{None, Fire, Ice, Stone}
    public enum StatusEffect{None, Burning, Chilled, Concussed}
    public enum StatType{Damage, CritChance, StaggerDamage}
public abstract class MetaUnlock : ScriptableObject{
    
    [Header("Meta Base Info")]
    public string UpgradeName;
    public string Description;
    public float Cost;
    public bool IsUnlocked = false;
    public Sprite Icon;
    public ElementType ElementCategory; //Fire, Ice, Stone
    
    public MetaUnlock PrerequisiteUnlock;

    public bool CanUnlock(){
        return PrerequisiteUnlock == null || PrerequisiteUnlock.IsUnlocked;
    }

    public abstract void ApplyMetaUnlock();
}
