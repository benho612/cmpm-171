using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MetaData", menuName = "PlayerMetaData/metaData")]

public class MetaData : ScriptableObject
{
    [Header("Elements Unlocked")]
    public bool fireUnlocked;
    public bool iceUnlocked;
    public bool earthUnlocked;

    public List<string> GetUnlockedElementPool()
    {
        List<string> elementPool = new List<string>();
        if (fireUnlocked) elementPool.Add("Fire");
        if (iceUnlocked) elementPool.Add("Ice");
        if (earthUnlocked) elementPool.Add("Earth");
        return elementPool;
    }
}