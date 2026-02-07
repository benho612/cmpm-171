using UnityEngine;

[CreateAssetMenu(fileName = "ElementUnlock", menuName = "Meta/ElementUnlock")]
public class ElementUnlock : MetaUnlock
{
    public string ElementName; //Fire, Ice, Earth


    public override void ApplyMetaUnlock(){
        isUnlocked = true;
    }
}
