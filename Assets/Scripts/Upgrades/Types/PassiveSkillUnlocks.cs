using UnityEngine;

[CreateAssetMenu(fileName = "PassiveSkillUnlocks", menuName = "Meta/PassiveSkillUnlocks")]
public class PassiveSkillUnlocks : MetaUnlock
{
    public string RequiredStatusEffect;
    public string StatEffected;
    public float EffectIncrease;
    
    public override void ApplyMetaUnlock(){
        isUnlocked = true;
    }
}
