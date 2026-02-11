using UnityEngine;
using System.Collections.Generic;

//this Manager script is used as a way to communicate between all the nodes 
//in the skill tree and update them when something has changed
public class SkillTreeManager : MonoBehaviour
{
    public SkillTreeManager Instance;
    public List<SkillTreeNode> AllNodes = new List<SkillTreeNode>();
    
    void Awake(){
        if(Instance == null){
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }else Destroy(gameObject);

        AllNodes.AddRange(GetComponentsInChildren<SkillTreeNode>());
        RefreshAllNodes();
    }

    public void RefreshAllNodes(){
        foreach(SkillTreeNode node in AllNodes){
            node.RefreshNode();
        }
    }
}
