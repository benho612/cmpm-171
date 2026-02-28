using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Combat : MonoBehaviour
{
    [Header("Combat Settings")]
    public float lightAttackDuration = 0.2f;
    public float heavyAttackDuration = 0.5f;

    [Header("Defense & Parry Settings")]
    public float parryWindow = 0.2f;
    public Color blockColor = new Color(0f, 0f, 1f, 0.4f); 
    public Color parryColor = new Color(0.6f, 0.8f, 1f, 0.8f); 
    public Vector3 blockVisualSize = new Vector3(1.2f, 2.0f, 1.2f);
    public Material baseBlockMaterial; 

    [Header("Stationary Dodge Settings")]
    public float dodgeDuration = 0.35f;
    public Color dodgeHighColor = new Color(0f, 1f, 0f, 0.5f); 
    public Color dodgeLowColor = new Color(0.5f, 0f, 0.5f, 0.5f); 

    [Header("Visual Feedback")]
    public Vector3 hitboxOffset = new Vector3(0, 1.0f, 1.0f);
    public Vector3 lightHitboxSize = new Vector3(0.5f, 0.5f, 0.5f);
    public Vector3 heavyHitboxSize = new Vector3(1.0f, 1.0f, 1.0f);
    public Color lightAttackColor = Color.yellow;
    public Color heavyAttackColor = Color.red;

    // References
    private PlayerControls _input;
    private Movement _movement;
    
    // States
    private bool _isAttacking;
    private bool _isBlocking;
    private bool _isParrying;
    private bool _isDodgingHigh;
    private bool _isDodgingLow;
    
    //prevents holding the button to dodge
    private float _lastMoveY;
    
    private GameObject _blockVisual;
    private Coroutine _parryCoroutine;
    private Coroutine _dodgeCoroutine;

    public bool IsAttacking => _isAttacking;
    public bool IsBlocking => _isBlocking;
    public bool IsParrying => _isParrying;
    public bool IsDodging => _isDodgingHigh || _isDodgingLow;

    private void Awake()
    {
        _movement = GetComponent<Movement>();
        _input = new PlayerControls();
        
        _input.Gameplay.LightAttack.performed += ctx => PerformLightAttack();
        _input.Gameplay.HeavyAttack.performed += ctx => PerformHeavyAttack();
        
        _input.Gameplay.Defend.started += ctx => StartDefense();
        _input.Gameplay.Defend.canceled += ctx => StopDefense();
        
        CreateBlockVisual();
    }

    private void OnEnable() => _input.Enable();
    private void OnDisable() => _input.Disable();

    private void Update()
    {
        Vector2 moveInput = _input.Gameplay.Move.ReadValue<Vector2>();

        // Only allow a stationary dodge if blocking, not already dodging, and not dashing
        if (_isBlocking && !IsDodging && !_movement.IsDashing)
        {
            if (moveInput.y > 0.5f && _lastMoveY <= 0.5f) 
            {
                if (_dodgeCoroutine != null) StopCoroutine(_dodgeCoroutine);
                _dodgeCoroutine = StartCoroutine(StationaryDodgeRoutine(true));
            }
            else if (moveInput.y < -0.5f && _lastMoveY >= -0.5f)
            {
                if (_dodgeCoroutine != null) StopCoroutine(_dodgeCoroutine);
                _dodgeCoroutine = StartCoroutine(StationaryDodgeRoutine(false));
            }
        }

        // Store this frame's Y input so we can compare it next frame
        _lastMoveY = moveInput.y;
    }

    private void StartDefense()
    {
        if (_isAttacking || _movement.IsDashing) return;

        _isBlocking = true;
        _blockVisual.SetActive(true);
        _blockVisual.transform.localPosition = new Vector3(0, 1f, 0); 
        
        if (_parryCoroutine != null) StopCoroutine(_parryCoroutine);
        _parryCoroutine = StartCoroutine(ParryRoutine());
    }

    private void StopDefense()
    {
        _isBlocking = false;
        _isParrying = false;
        _blockVisual.SetActive(false);
        
        if (_parryCoroutine != null) StopCoroutine(_parryCoroutine);
    }

    private IEnumerator ParryRoutine()
    {
        _isParrying = true;
        Renderer rend = _blockVisual.GetComponent<Renderer>();
        rend.material.color = parryColor;
        
        yield return new WaitForSeconds(parryWindow);
        
        _isParrying = false;
        if (!IsDodging) rend.material.color = blockColor; 
    }

    private IEnumerator StationaryDodgeRoutine(bool isHigh)
    {
        if (isHigh) _isDodgingHigh = true;
        else _isDodgingLow = true;

        Renderer rend = _blockVisual.GetComponent<Renderer>();
        rend.material.color = isHigh ? dodgeHighColor : dodgeLowColor;
        _blockVisual.transform.localPosition = new Vector3(0, isHigh ? 1.5f : 0.5f, 0);

        yield return new WaitForSeconds(dodgeDuration);

        _isDodgingHigh = false;
        _isDodgingLow = false;

        // Revert visuals back to standard block/parry if they are still holding the defend button
        if (_isBlocking)
        {
            rend.material.color = _isParrying ? parryColor : blockColor;
            _blockVisual.transform.localPosition = new Vector3(0, 1f, 0);
        }
    }

    private void CreateBlockVisual()
    {
        _blockVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(_blockVisual.GetComponent<BoxCollider>()); 
        
        _blockVisual.transform.SetParent(transform);
        _blockVisual.transform.localPosition = new Vector3(0, 1f, 0); 
        _blockVisual.transform.localScale = blockVisualSize;
        
        Renderer rend = _blockVisual.GetComponent<Renderer>();

        if (baseBlockMaterial != null)
        {
            rend.material = new Material(baseBlockMaterial); 
        }
        else
        {
            Debug.LogWarning("Combat: Please assign a transparent Material to 'Base Block Material' in the Inspector!");
        }
        
        rend.material.color = blockColor;
        _blockVisual.SetActive(false); 
    }


    private void PerformLightAttack()
    {
        if (_isAttacking || _isBlocking || _movement.IsDashing) return;
        RotateToInputDirection(); 
        StartCoroutine(AttackRoutine(lightAttackDuration, lightHitboxSize, lightAttackColor));
    }

    private void PerformHeavyAttack()
    {
        if (_isAttacking || _isBlocking || _movement.IsDashing) return;
        RotateToInputDirection(); 
        StartCoroutine(AttackRoutine(heavyAttackDuration, heavyHitboxSize, heavyAttackColor));
    }

    private void RotateToInputDirection()
    {
        Vector2 input = _input.Gameplay.Move.ReadValue<Vector2>();
        if (input.sqrMagnitude > 0.01f)
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            camForward.y = 0; camRight.y = 0;
            camForward.Normalize(); camRight.Normalize();

            Vector3 targetDir = (camForward * input.y + camRight * input.x).normalized;
            transform.rotation = Quaternion.LookRotation(targetDir);
        }
    }

    private IEnumerator AttackRoutine(float duration, Vector3 size, Color color)
    {
        _isAttacking = true;

        GameObject hitbox = GameObject.CreatePrimitive(PrimitiveType.Cube);
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