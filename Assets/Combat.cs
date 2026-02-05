using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Combat : MonoBehaviour
{
    [Header("Combat Settings")]
    [Tooltip("Time the light attack hitbox stays active")]
    public float lightAttackDuration = 0.2f;
    [Tooltip("Time the heavy attack hitbox stays active")]
    public float heavyAttackDuration = 0.5f;

    [Header("Visual Feedback")]
    public Vector3 hitboxOffset = new Vector3(0, 1.0f, 1.0f); // In front of player
    public Vector3 lightHitboxSize = new Vector3(0.5f, 0.5f, 0.5f);
    public Vector3 heavyHitboxSize = new Vector3(1.0f, 1.0f, 1.0f);
    public Color lightAttackColor = Color.yellow;
    public Color heavyAttackColor = Color.red;

    // References
    private PlayerControls _input;
    private bool _isAttacking;

    private void Awake()
    {
        // Initialize the same Input Actions class used in Movement script
        _input = new PlayerControls();

        _input.Gameplay.LightAttack.performed += ctx => PerformLightAttack();
        _input.Gameplay.HeavyAttack.performed += ctx => PerformHeavyAttack();
    }

    private void OnEnable() => _input.Enable();
    private void OnDisable() => _input.Disable();

    private void PerformLightAttack()
    {
        if (_isAttacking) return; // Prevent spamming while already attacking
        StartCoroutine(AttackRoutine(lightAttackDuration, lightHitboxSize, lightAttackColor));
    }

    private void PerformHeavyAttack()
    {
        if (_isAttacking) return;
        StartCoroutine(AttackRoutine(heavyAttackDuration, heavyHitboxSize, heavyAttackColor));
    }

    private IEnumerator AttackRoutine(float duration, Vector3 size, Color color)
    {
        _isAttacking = true;

        // Create a primitive cube to represent the hitbox
        GameObject hitbox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        
        // Position it in front of the player
        // TransformPoint converts local offset to world position respecting rotation
        hitbox.transform.position = transform.TransformPoint(hitboxOffset); 
        hitbox.transform.rotation = transform.rotation;
        hitbox.transform.localScale = size;

        // Color it for visual clarity
        Renderer rend = hitbox.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = color; 
        }

        // Remove the collider so it doesn't push the player
        Destroy(hitbox.GetComponent<BoxCollider>());

        // Wait for the animation duration
        yield return new WaitForSeconds(duration);

        // Cleanup
        Destroy(hitbox);
        _isAttacking = false;
    }
}