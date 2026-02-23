using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.VisualScripting;

public class CombatSandBox : MonoBehaviour
{
    private Coroutine _activeAttackRoutine;
    private GameObject _currentHitBox;

    [Header("Combat Settings")]
    public float lightAttackDamage = 15f;
    public float heavyAttackDamage = 30f;
    public float lightAttackDuration = 0.5f;
    public float heavyAttackDuration = 0.9f;
    public float MinCancelTime = 0.3f;
    private float _attackStartTime;

    [Header("Visual Feedback")]
    public Vector3 hitboxOffset = new Vector3(0, 1.0f, 1.0f);
    public Vector3 lightHitboxSize = new Vector3(1.5f, 1.5f, 1.5f);
    public Vector3 heavyHitboxSize = new Vector3(2.0f, 2.0f, 2.0f);
    public Color lightAttackColor = Color.yellow;
    public Color heavyAttackColor = Color.red;
    

    // References
    private PlayerControls _input;
    private bool _isAttacking;
    [SerializeField] private CombatHandler _combatHandler; 

    // Expose this so Movement.cs can see it
    public bool IsAttacking => _isAttacking;

    private void Awake()
    {
        _input = new PlayerControls();
        //_input.Gameplay.LightAttack.performed += ctx => PerformLightAttack();
        //_input.Gameplay.HeavyAttack.performed += ctx => PerformHeavyAttack();
    }

    //private void OnEnable() => _input.Enable();
    //private void OnDisable() => _input.Disable();

    public bool ExecutePhysicalAttack(bool canInterrupt, float duration, Vector3 size, Color color, float damage){
        if(_isAttacking){
            float timeSinceStart = Time.time - _attackStartTime;
            if(timeSinceStart < MinCancelTime) return false;

            if(!canInterrupt) return false;
            Debug.Log("ExecutePhysicalAttackTest");

            StopCoroutine(_activeAttackRoutine);
            if(_currentHitBox != null) Destroy(_currentHitBox);
        }
        _attackStartTime = Time.time;
        _activeAttackRoutine = StartCoroutine(AttackRoutine(duration, size, color, damage));
        return true;
    }
    
    
    
    /*private void PerformLightAttack()
    {
        if (_isAttacking){
            float timeSinceStart = Time.time - _attackStartTime;
            if(timeSinceStart < MinCancelTime) return;

            StopCoroutine(_activeAttackRoutine);
            if(_currentHitBox != null) Destroy(_currentHitBox);
        }

        RotateToInputDirection();
        _attackStartTime = Time.time;
        _activeAttackRoutine = StartCoroutine(AttackRoutine(lightAttackDuration, lightHitboxSize, lightAttackColor, lightAttackDamage));
    }

    private void PerformHeavyAttack()
    {
        if (_isAttacking) return;

        RotateToInputDirection();
        StartCoroutine(AttackRoutine(heavyAttackDuration, heavyHitboxSize, heavyAttackColor, heavyAttackDamage));
    }*/

    private void RotateToInputDirection()
    {
        Vector2 input = _input.Gameplay.Move.ReadValue<Vector2>();

        if (input.sqrMagnitude > 0.01f)
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 targetDir = (camForward * input.y + camRight * input.x).normalized;
            transform.rotation = Quaternion.LookRotation(targetDir);
        }
    }

    private IEnumerator AttackRoutine(float duration, Vector3 size, Color color, float damage)
    {
        _isAttacking = true;

        // Create visual hitbox
        _currentHitBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _currentHitBox.transform.position = transform.TransformPoint(hitboxOffset);
        _currentHitBox.transform.rotation = transform.rotation;
        _currentHitBox.transform.localScale = size;

        Renderer rend = _currentHitBox.GetComponent<Renderer>();
        if (rend != null) rend.material.color = color;

        // Remove the default collider from the visual cube
        Destroy(_currentHitBox.GetComponent<BoxCollider>());

        // Detect enemies in the hitbox area
        Vector3 hitboxCenter = transform.TransformPoint(hitboxOffset);
        Collider[] hits = Physics.OverlapBox(hitboxCenter, size / 2f, transform.rotation);

        foreach (var hit in hits)
        {
            BaseEnemy enemy = hit.GetComponent<BaseEnemy>();
            if (enemy != null && !enemy.IsDead())
            {
                _combatHandler.ProcessHit(hit.gameObject, damage);
            }
        }

        yield return new WaitForSeconds(duration);

        if(_currentHitBox != null){
            Destroy(_currentHitBox);
        }
        _isAttacking = false;
    }
}