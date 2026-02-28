using UnityEngine;

public class EnemyFinisherTarget : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animator Params")]
    [SerializeField] private string finisherReactTrigger = "FinisherReact";
    [SerializeField] private string dieTrigger = "Die";

    [Header("Anchor")]
    [Tooltip("Player can snap here before finisher (optional).")]
    [SerializeField] private Transform finisherAnchor;

    [Header("Death (optional)")]
    [SerializeField] private bool disableCollidersOnDeath = true;
    [SerializeField] private Collider[] collidersToDisableOnDeath;

    bool isDead;

    public Transform FinisherAnchor => finisherAnchor;

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();

        if (!finisherAnchor)
        {
            var go = new GameObject("FinisherAnchor");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 0f, 0.8f);
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            finisherAnchor = go.transform;
        }

        if (collidersToDisableOnDeath == null || collidersToDisableOnDeath.Length == 0)
            collidersToDisableOnDeath = GetComponentsInChildren<Collider>();
    }

    public void PlayFinisherReact()
    {
        if (isDead) return;
        animator?.SetTrigger(finisherReactTrigger);
        Debug.Log("Enemy PlayFinisherReact() called on: " + gameObject.name);
    }

    public void ResolveFinisherKill()
    {
        if (isDead) return;
        isDead = true;

        animator?.SetTrigger(dieTrigger);

        if (disableCollidersOnDeath && collidersToDisableOnDeath != null)
        {
            foreach (var c in collidersToDisableOnDeath)
                if (c) c.enabled = false;
        }
    }
}