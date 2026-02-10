using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Combat : MonoBehaviour
{
    [Header("Combat Settings")]
    public float lightAttackDuration = 0.2f;
    public float heavyAttackDuration = 0.5f;

    [Header("Visual Feedback")]
    public Vector3 hitboxOffset = new Vector3(0, 1.0f, 1.0f);
    public Vector3 lightHitboxSize = new Vector3(0.5f, 0.5f, 0.5f);
    public Vector3 heavyHitboxSize = new Vector3(1.0f, 1.0f, 1.0f);
    public Color lightAttackColor = Color.yellow;
    public Color heavyAttackColor = Color.red;

    // References
    private PlayerControls _input;
    private bool _isAttacking;

    // Expose this so Movement.cs can see it
    public bool IsAttacking => _isAttacking;

    private void Awake()
    {
        _input = new PlayerControls();
        _input.Gameplay.LightAttack.performed += ctx => PerformLightAttack();
        _input.Gameplay.HeavyAttack.performed += ctx => PerformHeavyAttack();
    }

    private void OnEnable() => _input.Enable();
    private void OnDisable() => _input.Disable();

    private void PerformLightAttack()
    {
        if (_isAttacking) return;
        
        RotateToInputDirection(); // Snap rotation before attacking
        StartCoroutine(AttackRoutine(lightAttackDuration, lightHitboxSize, lightAttackColor));
    }

    private void PerformHeavyAttack()
    {
        if (_isAttacking) return;

        RotateToInputDirection(); // Snap rotation before attacking
        StartCoroutine(AttackRoutine(heavyAttackDuration, heavyHitboxSize, heavyAttackColor));
    }

    // Snaps character to face the input direction (if any)
    private void RotateToInputDirection()
    {
        Vector2 input = _input.Gameplay.Move.ReadValue<Vector2>();

        // Only rotate if there is actual input
        if (input.sqrMagnitude > 0.01f)
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            // Calculate target direction
            Vector3 targetDir = (camForward * input.y + camRight * input.x).normalized;

            // Snap immediately
            transform.rotation = Quaternion.LookRotation(targetDir);
        }
    }

    private IEnumerator AttackRoutine(float duration, Vector3 size, Color color)
    {
        _isAttacking = true;

        GameObject hitbox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        
        // Position relative to the NEW rotation we just set
        hitbox.transform.position = transform.TransformPoint(hitboxOffset); 
        hitbox.transform.rotation = transform.rotation;
        hitbox.transform.localScale = size;

        Renderer rend = hitbox.GetComponent<Renderer>();
        if (rend != null) rend.material.color = color;
        
        Destroy(hitbox.GetComponent<BoxCollider>());

        yield return new WaitForSeconds(duration);

        Destroy(hitbox);
        _isAttacking = false;
    }
}