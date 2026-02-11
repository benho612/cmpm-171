using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MetaData", menuName = "PlayerMetaData/metaData")]

public class MetaData : ScriptableObject{
    public int MetaCurrency;
    [Header("Available Elements")]
    [SerializeField] private List<string> _elementPool = new List<string>();

    public List<string> GetUnlockedElementPool(){
        return _elementPool;
    }
}