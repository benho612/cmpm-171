using UnityEngine;
using System.Collections.Generic;

public class MetaManager : MonoBehaviour
{
    public List<MetaUnlock> AllMetaUnlocks;

    public bool FeatureLogicCheck(string featureID){
        if(AllMetaUnlocks == null) return false;
        foreach(MetaUnlock unlock in AllMetaUnlocks){
            if(unlock is ElementalEffectUnlocks elementalUnlock && elementalUnlock.FeatureID == featureID && elementalUnlock.IsUnlocked){
                return true;
            }
        }
        return false;
    }

    public void StatIncreaseCheck(string statEffected){

    }
}
