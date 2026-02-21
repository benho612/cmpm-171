using UnityEngine;

public class TempStatusScript : MonoBehaviour
{
    public StatusEffect CurrentStatus = StatusEffect.None;
    
    public void ApplyStatus(StatusEffect newStatus){
        CurrentStatus = newStatus;
        Debug.Log($"{gameObject.name} is now {newStatus}!");
    }
}
