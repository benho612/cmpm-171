using UnityEngine;
using Unity.Cinemachine;

public class CameraSmartRig : MonoBehaviour
{
    [Header("References")]
    public CinemachineCamera activeCamera;
    public Movement movementScript;
    public Combat combatScript;

    [Header("Orbital Settings")]
    public float defaultRadius = 2.5f;     // Standard distance
    public float sprintRadius = 5.0f;      // Pull back when running
    public float combatRadius = 6.0f;      // Pull back furthest for fighting
    public float zoomDamping = 2.0f;       // How fast it zooms
    public float combatCooldown = 5.0f;

    // Internal State
    private float _currentRadius;
    private float _lastCombatTime;
    private bool _isInCombatMode;

    private CinemachineOrbitalFollow _orbitalFollow; 

    private void Start()
    {
        if (activeCamera != null)
        {
            _orbitalFollow = activeCamera.GetComponent<CinemachineOrbitalFollow>();
            
            // Safety Check
            if (_orbitalFollow == null)
            {
                Debug.LogError("CameraSmartRig: No 'CinemachineOrbitalFollow' found! Make sure your Camera Body is set to Orbital.");
                enabled = false;
                return;
            }
        }
        
        _currentRadius = defaultRadius;
    }

    private void Update()
    {
        if (_orbitalFollow == null) return;

        HandleCombatState();
        HandleZoom();
    }

    private void HandleCombatState()
    {
        if (combatScript != null && combatScript.IsAttacking)
        {
            _lastCombatTime = Time.time;
            _isInCombatMode = true;
        }

        if (_isInCombatMode && (Time.time - _lastCombatTime > combatCooldown))
        {
            _isInCombatMode = false;
        }
    }

    private void HandleZoom()
    {
        float targetRadius = defaultRadius;

        // Logic: Combat > Sprint > Default
        if (_isInCombatMode)
        {
            targetRadius = combatRadius;
        }
        else if (IsSprinting())
        {
            targetRadius = sprintRadius;
        }

        // Smoothly interpolate the RADIUS
        _currentRadius = Mathf.Lerp(_currentRadius, targetRadius, Time.deltaTime * zoomDamping);
        _orbitalFollow.Radius = _currentRadius;
    }

    private bool IsSprinting()
    {
        return UnityEngine.InputSystem.Keyboard.current.leftShiftKey.isPressed;
    }
}