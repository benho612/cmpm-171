using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Defense & Posture")]
    public bool isBlocking;
    // [SerializeField] private float maxStunMeter = 100f;
    // private float currentStunMeter = 0f;

    [Header("Invincibility")]
    [SerializeField] private float invincibilityDuration = 0.5f;
    private float invincibilityTimer;
    private bool isInvincible;

    [Header("References")]
    [SerializeField] private Slider _healthSlider;
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
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || isInvincible) return;

        /*if (isBlocking)
        {
            Debug.Log($"Player BLOCKED {damage} damage!");
            
            // FUTURE STUN LOGIC GOES HERE:
            // currentStunMeter += damage;
            // if (currentStunMeter >= maxStunMeter) {
            //     BreakGuard();
            // } 
            
            return; // 100% damage mitigation for now
        }*/
        if (_movement != null && _movement.IsInvincibleViaDash)
        {
            Debug.Log("Dodged damage via Dash I-Frames!");
            return;
        }

        if(_combatSandBox.IsParrying){ //check for parry first negate all damage
            Debug.Log("Player successfully PARRIED the attack!");
            //input more parry logic here when stagger is in
        }else if(_combatSandBox.IsBlocking){
            //_animationBridge.PlayAttack()
            currentHealth -= (damage * 0.1f);
            Debug.Log($"Player BLOCKED the attack but still took {damage * 0.1f} damage!");
            //play block animation here when we have them
            _animationBridge.PlayBlock(0.2f);
        }else {
            currentHealth -= damage;
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
        //OnPlayerDeath?.Invoke();
        // TODO: Add death logic (respawn, game over screen, etc.)
        Time.timeScale = 1f;
        //temp reset scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}