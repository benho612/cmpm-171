using UnityEngine;
using System.Collections.Generic;

public class MetaManager : MonoBehaviour
{
    public List<MetaUnlock> AllMetaUnlocks;
    public MetaManager Instance;
    public void Awake(){
        if(Instance == null){
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }else Destroy(gameObject);
    }
    
    public bool FeatureLogicCheck(string featureID){
        if(AllMetaUnlocks == null) return false;
        foreach(MetaUnlock unlock in AllMetaUnlocks){
            if(unlock is ElementalEffectUnlocks eUnlock && eUnlock.FeatureID == featureID && eUnlock.IsUnlocked){
                return true;
            }
        }
        return false;
    }

    public float StatIncreaseCheck(string statEffected, string requiredStatusEffect){
        if(AllMetaUnlocks == null) return 0f;
        float totalIncrease = 0f;
        foreach(MetaUnlock unlock in AllMetaUnlocks){
            if(unlock is ElementalEffectUnlocks eUnlock && unlock.IsUnlocked){
                if(eUnlock.StatEffected == statEffected && eUnlock.RequiredStatusEffect == requiredStatusEffect){
                    totalIncrease += eUnlock.EffectIncrease;
                }
            }
        }
        return totalIncrease;
    }
    [ContextMenu("Reset All Unlocks")] // This adds a button in the Inspector!
    public void ResetAllUnlocks()
    {
        foreach (var unlock in AllMetaUnlocks)
        {
            unlock.IsUnlocked = false;
            // If you added 'CurrentLevel', reset that too
        }
        Debug.Log("Meta-Progression has been wiped for testing.");
    }
}
