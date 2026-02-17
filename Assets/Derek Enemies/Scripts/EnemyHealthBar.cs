using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private Canvas canvas;

    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private bool hideWhenFull = true;
    [SerializeField] private float hideDelay = 2f;
    [SerializeField] private bool alwaysFaceCamera = true;
    [SerializeField] private bool debugMode = true; // Enable to see debug info

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
    private float targetValue;
    private float hideTimer;
    private bool isVisible;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        enemy = GetComponentInParent<BaseEnemy>();

        if (canvas == null)
            canvas = GetComponent<Canvas>();

        // Ensure canvas is set to World Space
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (enemy == null)
        {
            Debug.LogError($"EnemyHealthBar on {gameObject.name}: No BaseEnemy found in parent!");
            enabled = false;
            return;
        }

        if (debugMode)
        {
            Debug.Log($"EnemyHealthBar: Found enemy {enemy.name}");
            Debug.Log($"EnemyHealthBar: Canvas assigned: {canvas != null}");
            Debug.Log($"EnemyHealthBar: Slider assigned: {healthSlider != null}");
            Debug.Log($"EnemyHealthBar: Fill Image assigned: {fillImage != null}");
        }
    }

    private void Start()
    {
        cameraTransform = Camera.main?.transform;

        if (cameraTransform == null)
        {
            Debug.LogWarning("EnemyHealthBar: Main camera not found!");
        }

        // Initialize slider
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;
        }

        targetValue = 1f;

        // Set initial position
        transform.localPosition = offset;

        // Hide if full health at start
        if (hideWhenFull)
        {
            SetVisibility(false);
        }
        else
        {
            SetVisibility(true);
        }

        if (debugMode)
        {
            Debug.Log($"EnemyHealthBar: Started. HideWhenFull: {hideWhenFull}, Visible: {isVisible}");
        }
    }

    private void LateUpdate()
    {
        if (enemy == null || enemy.IsDead())
        {
            SetVisibility(false);
            return;
        }

        // Update local position (stays relative to parent enemy)
        transform.localPosition = offset;

        // Billboard - face camera
        if (alwaysFaceCamera && cameraTransform != null)
        {
            Vector3 dirToCamera = cameraTransform.position - transform.position;
            dirToCamera.y = 0f;
            if (dirToCamera.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(-dirToCamera);
            }
        }

        // Update health display
        float healthPercent = enemy.GetHealthPercentage();
        targetValue = healthPercent;

        // Smooth slider animation
        if (healthSlider != null)
        {
            healthSlider.value = Mathf.Lerp(
                healthSlider.value,
                targetValue,
                smoothSpeed * Time.deltaTime
            );
        }

        // Update fill color based on health
        if (fillImage != null)
        {
            fillImage.color = GetHealthColor(healthPercent);
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

        if (debugMode && visible)
        {
            Debug.Log($"EnemyHealthBar: Now visible for {enemy?.name}");
        }
    }

    /// <summary>
    /// Force show the health bar (e.g., when enemy takes damage)
    /// </summary>
    public void ShowHealthBar()
    {
        if (debugMode)
        {
            Debug.Log($"EnemyHealthBar: ShowHealthBar called for {enemy?.name}");
        }

        SetVisibility(true);
        hideTimer = hideDelay;
    }

    // Draw gizmo to visualize health bar position in Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 pos = transform.parent != null
            ? transform.parent.position + offset
            : transform.position;
        Gizmos.DrawWireCube(pos, new Vector3(1f, 0.2f, 0.1f));
    }
}