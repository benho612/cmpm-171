using UnityEngine;
using UnityEngine.InputSystem;

public class InputBootstrap : MonoBehaviour
{
    public InputActionAsset actions;

    void Awake()
    {
        RebindSaveLoad.LoadAllBindings(actions);
    }
}