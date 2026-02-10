using UnityEngine;

[CreateAssetMenu(fileName = "ElementalEffectUnlocks", menuName = "Meta/ElementalEffect")]
public class ElementalEffectUnlocks : MetaUnlock
{
    [Header("Feature Logic")]
    public string FeatureID; //Unique identifier for the passive skill feature
    public string FeatureDescription;
    [Header("Stat increase logic")]
    public string RequiredStatusEffect;
    public string StatEffected;
    public float EffectIncrease;
    
    public override void ApplyMetaUnlock(){
        IsUnlocked = true;
        Debug.Log($"Passive Skill Unlocked: {UpgradeName}");
    }
}
