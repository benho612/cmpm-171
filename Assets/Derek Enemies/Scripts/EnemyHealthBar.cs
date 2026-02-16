using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Canvas canvas;

    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private bool hideWhenFull = true;
    [SerializeField] private float hideDelay = 2f;
    [SerializeField] private bool alwaysFaceCamera = true;

    [Header("Colors")]
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color damagedColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;
    [SerializeField] private float criticalThreshold = 0.25f;
    [SerializeField] private float damagedThreshold = 0.5f;

    [Header("Animation")]
    [SerializeField] private float smoothSpeed = 5f;

    private BaseEnemy enemy;
    private Transform cameraTransform;
    private float targetFillAmount;
    private float hideTimer;
    private bool isVisible;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        enemy = GetComponentInParent<BaseEnemy>();

        if (canvas == null)
            canvas = GetComponent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (enemy == null)
        {
            Debug.LogError("EnemyHealthBar: No BaseEnemy found in parent!");
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        cameraTransform = Camera.main?.transform;

        if (cameraTransform == null)
        {
            Debug.LogWarning("EnemyHealthBar: Main camera not found!");
        }

        // Initialize
        targetFillAmount = 1f;
        if (healthFillImage != null)
            healthFillImage.fillAmount = 1f;

        // Hide if full health at start
        if (hideWhenFull)
        {
            SetVisibility(false);
        }
    }

    private void LateUpdate()
    {
        if (enemy == null || enemy.IsDead())
        {
            SetVisibility(false);
            return;
        }

        // Update position
        transform.position = enemy.transform.position + offset;

        // Billboard - face camera
        if (alwaysFaceCamera && cameraTransform != null)
        {
            transform.forward = cameraTransform.forward;
        }

        // Update health display
        float healthPercent = enemy.GetHealthPercentage();
        targetFillAmount = healthPercent;

        // Smooth fill animation
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = Mathf.Lerp(
                healthFillImage.fillAmount,
                targetFillAmount,
                smoothSpeed * Time.deltaTime
            );

            // Update color based on health
            healthFillImage.color = GetHealthColor(healthPercent);
        }

        // Handle visibility
        if (hideWhenFull)
        {
            if (healthPercent < 1f)
            {
                SetVisibility(true);
                hideTimer = hideDelay;
            }
            else if (isVisible)
            {
                hideTimer -= Time.deltaTime;
                if (hideTimer <= 0f)
                {
                    SetVisibility(false);
                }
            }
        }
    }

    private Color GetHealthColor(float healthPercent)
    {
        if (healthPercent <= criticalThreshold)
            return criticalColor;
        else if (healthPercent <= damagedThreshold)
            return damagedColor;
        else
            return healthyColor;
    }

    private void SetVisibility(bool visible)
    {
        isVisible = visible;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
        }
        else if (canvas != null)
        {
            canvas.enabled = visible;
        }
    }

    /// <summary>
    /// Force show the health bar (e.g., when enemy takes damage)
    /// </summary>
    public void ShowHealthBar()
    {
        SetVisibility(true);
        hideTimer = hideDelay;
    }
}