using UnityEngine;

public class SkillTreeManager : MonoBehaviour
{
    public SkillTreeManager Instance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake(){
        if(Instance == null){
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }else Destroy(gameObject);
    }
        
}
