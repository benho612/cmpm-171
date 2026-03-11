using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [Tooltip("How much health recovers per second after the delay.")]
    [SerializeField] private float healthRecoveryRate = 1f; // 1 HP per second = REAL slow
    
    [Tooltip("How long to wait after taking damage before health starts recovering.")]
    [SerializeField] private float healthRecoveryDelay = 5.0f; // 5 seconds of safety needed
    private float healthRecoveryTimer = 0f;

    [Header("Defense & Posture")]
    public bool isBlocking;
    [SerializeField] private float maxStunMeter = 100f;
    [SerializeField] private float stunRecoveryRate = 5f;
    [Tooltip("How long to wait after taking damage before posture starts recovering.")]
    [SerializeField] private float stunRecoveryDelay = 2.0f; // Wait 2 seconds
    private float stunRecoveryTimer = 0f; // Tracks the delay
    private float currentStunMeter = 0f;
    public bool isStunned { get; private set; }

    [Header("Invincibility")]
    [SerializeField] private float invincibilityDuration = 0.5f;
    private float invincibilityTimer;
    private bool isInvincible;

    [Header("References")]
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _stunSlider;
    [SerializeField] private CombatSandBox _combatSandBox;
    [SerializeField] private AnimationBridge _animationBridge;
    [SerializeField] private MovementSandBox _movement;

    // Events for UI or other systems to subscribe to
    public event Action<float, float> OnHealthChanged; // currentHealth, maxHealth
    public event Action OnPlayerDeath;

    public bool IsDead => currentHealth <= 0;
    public float HealthPercentage => currentHealth / maxHealth;

    private void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if(_healthSlider != null){
            _healthSlider.maxValue = maxHealth;
            _healthSlider.value = maxHealth;
        }
        if(_stunSlider != null){
            _stunSlider.maxValue = maxStunMeter;
            _stunSlider.value = 0f;
        }
    }

    private void Update()
    {
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0)
            {
                isInvincible = false;
            }
        }
        
        // gradually recover Stun Meter over time if not currently stunned
        if (currentStunMeter > 0 && !isStunned)
        {
            // tick down the delay timer first
            if (stunRecoveryTimer > 0)
            {
                stunRecoveryTimer -= Time.deltaTime;
            }
            // once the delay is over, slowly recover the meter
            else
            {
                currentStunMeter -= stunRecoveryRate * Time.deltaTime;
                currentStunMeter = Mathf.Max(0, currentStunMeter);
                if (_stunSlider != null) _stunSlider.value = currentStunMeter;
            }
        }

        if (currentHealth < maxHealth && !IsDead)
        {
            // tick down the delay timer first
            if (healthRecoveryTimer > 0)
            {
                healthRecoveryTimer -= Time.deltaTime;
            }
            // once the delay is over, slowly recover health
            else
            {
                currentHealth += healthRecoveryRate * Time.deltaTime;
                currentHealth = Mathf.Min(currentHealth, maxHealth); // Don't heal past Max HP
                
                // update ui and trigger events
                if (_healthSlider != null) _healthSlider.value = currentHealth;
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }
        }
    }

    public void TakeDamage(float damage, GameObject attacker = null)
    {
        if (IsDead || isInvincible) return;

        if (_movement != null && _movement.IsInvincibleViaDash)
        {
            Debug.Log("Dodged damage via Dash I-Frames!");
            return;
        }

        if (_combatSandBox.IsParrying)
        { 
            Debug.Log("Player successfully PARRIED the attack!");
            
            // TODO: Play a cool Parry *CLANG* sound and VFX here
            
            return; //Stops the damage AND stops standard hit I-frames
        }else if(_combatSandBox.IsBlocking){
            //_animationBridge.PlayAttack()
            
            currentHealth -= (damage * 0.1f);
            currentStunMeter += damage;
            
            // reset the recovery delay timer everytime you get hit
            stunRecoveryTimer = stunRecoveryDelay;
            healthRecoveryTimer = healthRecoveryDelay;

            // AUDIO LOGIC: Check the attacker's tag for the right block sound
            if (attacker != null && !attacker.CompareTag("BasicEnemy"))
            {
                // If there is an attacker, and they are NOT a BasicEnemy (e.g., Elite or Boss)
                AudioManager.Instance.Play("Player_Blocking_Sword");
            }
            else
            {
                // If it IS a BasicEnemy, or if the attacker is somehow null
                AudioManager.Instance.Play("Player_Blocking_Punch");
            }

            Debug.Log($"Hit! Damage: {damage}. Stun Meter is now: {currentStunMeter} / {maxStunMeter}");
            
            if (_stunSlider != null) _stunSlider.value = currentStunMeter;

            if (currentStunMeter >= maxStunMeter)
            {
                BreakGuard();
            }
            else
            {
                Debug.Log($"Player BLOCKED. Posture: {currentStunMeter}/{maxStunMeter}");
                if (_animationBridge != null) _animationBridge.PlayBlock(0.2f);
            }
            _animationBridge.PlayBlock(0.2f);
        }else {
            currentHealth -= damage;
            healthRecoveryTimer = healthRecoveryDelay;
            stunRecoveryTimer = stunRecoveryDelay;

            // AUDIO: Randomly pick between 0 and 1
            int randomSound = UnityEngine.Random.Range(0, 2);

            // So we don't have the same hit sound so it's not too repetitive.
            if (randomSound == 0)
            {
                AudioManager.Instance.Play("Player_Hit");
            }
            else
            {
                AudioManager.Instance.Play("Player_Hit_1");
            }

            //invoke hit reaction animation here when we have them
        }

        currentHealth = Mathf.Max(currentHealth, 0);
        _healthSlider.value = currentHealth;
        //Debug.Log($"Player took {damage} damage! Health: {currentHealth}/{maxHealth}");
        // Brief invincibility to prevent multiple hits from same attack
        isInvincible = true;
        invincibilityTimer = invincibilityDuration;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void BreakGuard()
    {
        Debug.Log("GUARD BROKEN! Player is stunned.");
        isStunned = true;
        
        // Reset the meter visually
        currentStunMeter = 0;
        if (_stunSlider != null) _stunSlider.value = 0;

        // Tell CombatSandBox to lock inputs and force drop the shield
        float stunDuration = 2.0f; // 2 seconds of stun
        _combatSandBox.TriggerGuardBreak(stunDuration);

        // Tell PlayerHealth to unlock after the duration
        Invoke(nameof(RecoverFromStun), stunDuration);

        // TODO: Play Guard Break / Stun Animation here
        // _animationBridge.PlayStun();
    }

    private void RecoverFromStun()
    {
        Debug.Log("Player recovered from stun.");
        isStunned = false;
    }

    public void Heal(float amount)
    {
        if (IsDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        Debug.Log("Player died!");

        // AUDIO: Play the death sound
        AudioManager.Instance.Play("Player_Death");

        //OnPlayerDeath?.Invoke();
        // TODO: Add death logic (respawn, game over screen, etc.)
        Time.timeScale = 1f;
        //temp reset scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}