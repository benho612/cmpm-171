using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.VisualScripting;

public class CombatSandBox : MonoBehaviour
{
    private Coroutine _activeAttackRoutine;
    private GameObject _currentHitBox;

    [Header("Attack Magnetism")]
    public float magnetizeRadius = 6.0f; // How far the player can detect an enemy to magnetize
    [Range(0, 360)]
    public float magnetizeAngle = 120f;  // The cone angle in front of the camera/player to detect enemies
    public float magnetizeRotationSpeed = 25f; // How fast the player rotates to the target

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
    
    [Header("Combat Settings")]
    public float lightAttackDuration = 0.5f;
    public float heavyAttackDuration = 0.9f;
    public float MinCancelTime = 0.3f;
    private float _attackStartTime;
    [SerializeField] private float _maxAttackDistance = 2.0f;
    [SerializeField] private float _stoppingDistance = 1.2f;

    [Header("Visual Feedback")]
    public Vector3 hitboxOffset = new Vector3(0, 1.0f, 1.0f);
    public Vector3 lightHitboxSize = new Vector3(1.5f, 1.5f, 1.5f);
    public Vector3 heavyHitboxSize = new Vector3(2.0f, 2.0f, 2.0f);
    public Color lightAttackColor = Color.yellow;
    public Color heavyAttackColor = Color.red;

    [Header("AnimationEvent Testing Vars")]
    private float _duration;
    private Vector3 _size;
    private Color _color;
    private float _damage;

    // References
    private PlayerControls _input;
    [SerializeField] private CombatHandler _combatHandler; 
    [SerializeField] private CharacterController _controller;
    [SerializeField] private PlayerHealth _playerHealth;
    
    // States
    private bool _isAttacking;
    private bool _canCancel = false;
    private bool _isBlocking;
    private bool _isParrying;
    private bool _isDodgingHigh;
    private bool _isDodgingLow;
    
    private GameObject _blockVisual;
    private Coroutine _parryCoroutine;
    private Coroutine _dodgeCoroutine;

    public bool IsAttacking => _isAttacking;
    public bool IsBlocking => _isBlocking;
    public bool IsParrying => _isParrying;
    public bool IsDodging => _isDodgingHigh || _isDodgingLow;
    public bool IsInActiveFrames => _currentHitBox != null;

    public int punishWindowFrames = 1;

    private void Awake()
    {
        _input = new PlayerControls();
        CreateBlockVisual();

        // Auto-grab the health component
        if (_playerHealth == null) 
        {
            _playerHealth = GetComponent<PlayerHealth>();
        }
    }

    private void OnEnable()
    {
        _input?.Enable();
    }

    private void OnDisable()
    {
        _input?.Disable();
    }
    

    public bool ExecutePhysicalAttack(bool canInterrupt, float duration, Vector3 size, Color color, float damage){
        if(_isAttacking){
            if(!_canCancel) return false;
            /*float timeSinceStart = Time.time - _attackStartTime;
            if(timeSinceStart < MinCancelTime) return false;*/

            if(!canInterrupt) return false;
            Debug.Log("ExecutePhysicalAttackTest");
            StopCoroutine(_activeAttackRoutine);
            if(_currentHitBox != null) Destroy(_currentHitBox);
        }
        //assign variables for attackImpact animation event
        _duration = duration;
        _size = size;
        _color = color;
        _damage = damage;
        _attackStartTime = Time.time;
        _isAttacking = true;
        _canCancel = false;
        //_activeAttackRoutine = StartCoroutine(AttackRoutine(duration, size, color, damage));
        return true;
    }

    public void ExecuteAttackImpact(){

        if (!_isAttacking) return;
        _canCancel = true;
        MagnetizeToTarget();
        _activeAttackRoutine = StartCoroutine(AttackRoutine(_duration, _size, _color, _damage));

    }

    public void StartDefense()
    {
        _isBlocking = true;

        if (_playerHealth != null) _playerHealth.isBlocking = true;

        _blockVisual.SetActive(true);
        _blockVisual.transform.localPosition = new Vector3(0, 1f, 0); 
        
        if (_parryCoroutine != null) StopCoroutine(_parryCoroutine);
        //_parryCoroutine = StartCoroutine(ParryRoutine());
    }

    public void StopDefense()
    {
        _parryCoroutine = StartCoroutine(ParryRoutine());
    }

    private void FinishDefense()
    {
        _isBlocking = false;
        _isParrying = false;
        if (_playerHealth != null) _playerHealth.isBlocking = false;
        
        _blockVisual.SetActive(false);
    }

    public void ExecuteStationaryDodge(bool isHigh){
        if(_dodgeCoroutine != null) StopCoroutine(_dodgeCoroutine);
        _dodgeCoroutine = StartCoroutine(StationaryDodgeRoutine(isHigh));
    }

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
        // Create visual hitbox
        _currentHitBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _currentHitBox.transform.SetParent(this.transform);

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

    private IEnumerator ParryRoutine()
    {
        for (int i = 0; i < punishWindowFrames; i++)
        {
            yield return null; 
        }
        _isParrying = true;
        Renderer rend = _blockVisual.GetComponent<Renderer>();
        rend.material.color = parryColor;
        
        yield return new WaitForSeconds(parryWindow);
        
        _isParrying = false;
        if (!IsDodging) rend.material.color = blockColor;
        FinishDefense(); 
    }

//temp visual for blocking
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
    
    private void MagnetizeToTarget()
    {
        // Determine the Intended Attack Direction
        Vector2 moveInput = _input.Gameplay.Move.ReadValue<Vector2>();
        Vector3 intendedDir = transform.forward; // Default to where the player is currently facing

        // If the player is pressing a direction, calculate that direction relative to the camera
        if (moveInput.sqrMagnitude > 0.01f) 
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            intendedDir = (camForward * moveInput.y + camRight * moveInput.x).normalized;
        }

        // Search for enemies in the intended direction
        Collider[] hits = Physics.OverlapSphere(transform.position, magnetizeRadius);
        Transform bestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (var hit in hits)
        {
            BaseEnemy enemy = hit.GetComponent<BaseEnemy>();
            if (enemy != null && !enemy.IsDead())
            {
                Vector3 dirToEnemy = hit.transform.position - transform.position;
                dirToEnemy.y = 0; 
                
                float distance = dirToEnemy.sqrMagnitude;
                
                // Check the angle between the enemy and intended direction
                float angle = Vector3.Angle(intendedDir, dirToEnemy.normalized); 

                if (angle <= magnetizeAngle / 2f && distance < closestDistance)
                {
                    closestDistance = distance;
                    bestTarget = hit.transform;
                }
            }
        }

        // Rotate the player
        if (bestTarget != null && _controller != null)
        {
            // If an enemy is found in that direction, snap directly to them
            Vector3 targetDir = bestTarget.position - transform.position;
            targetDir.y = 0;
            _controller.transform.rotation = Quaternion.LookRotation(targetDir.normalized);

            float actualDistance = targetDir.magnitude;
            float distanceToMove = Mathf.Clamp(actualDistance - _stoppingDistance, 0, _maxAttackDistance);

            if(distanceToMove > 0){
                _controller.Move(targetDir.normalized * distanceToMove); //Slightly move the character towards the enemy
            }
        }
        else
        {
            // If no enemy is found, still rotate to face the intended input direction
            _controller.transform.rotation = Quaternion.LookRotation(intendedDir);
        }
    }

    public void CancelAttackForDash()
    {
        // Stop the actual coroutine that moves/checks hits
        if (_activeAttackRoutine != null)
        {
            StopCoroutine(_activeAttackRoutine);
            _activeAttackRoutine = null;
        }
        
        // Immediately nuke the physical cube
        if (_currentHitBox != null) 
        {
            Destroy(_currentHitBox);
        }

        // Reset internal state so ExecuteAttackImpact() fails if it tries to run
        _isAttacking = false;
        _canCancel = false;

        GetComponent<Animator>().Play("Idle");
        
        // Reset the "queued" attack data to prevent late triggers
        _duration = 0;
        _damage = 0;
        _size = Vector3.zero;


        Debug.Log("Combat System: Attack fully purged.");
    }  
}
