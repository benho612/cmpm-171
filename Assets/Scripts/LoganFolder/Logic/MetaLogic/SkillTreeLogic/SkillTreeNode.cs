using UnityEngine;
using UnityEngine.UI;
using TMPro;

//this files controls each specific node in the Skill tree and communicates with the manager
//when something has changed so each node knows when it can be bought or not
public class SkillTreeNode : MonoBehaviour
{
    public MetaUnlock MetaUnlock;
    [SerializeField] private Button _nodeButton;
    [SerializeField] private Image _nodeImage;
    [SerializeField] private TextMeshProUGUI _buttonText;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
        
    }

    public void OnClick(){
        if(!MetaUnlock.CanUnlock()){
            Debug.Log("Prerequisite not met");
            return;
        }
        if(MetaUnlock.cost <= GameManager.Instance.MetaData.MetaCurrency){
            GameManager.Instance.MetaData.MetaCurrency -= (int)MetaUnlock.Cost;
            MetaUnlock.IsUnlocked = true;
            MetaUnlock.ApplyMetaUnlock();
            SkillTreeManager.Instance.RefreshAllNodes();
        } 
    }

    public void RefreshNode(){
        if(MetaUnlock.IsUnlocked){
            _nodeButton.interactable = false;
            _nodeImage.color = Color.green;
        }else if(!MetaUnlock.IsUnlocked && MetaUnlock.CanUnlock()){
            _buttonText.text = MetaUnlock.UpgradeName + "\nCost: " + MetaUnlock.Cost;
            _nodeButton.interactable = true;
            _nodeImage.color = Color.white;
        }else{
            _nodeButton.interactable = false;
            _nodeImage.color = new Color(1, 1, 1, 0.4f);
        }
    }
}
