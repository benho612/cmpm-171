using UnityEngine;

public class EnemyFinisherZone : MonoBehaviour
{
    public EnemyFinisherTarget target;

    private void Reset()
    {
        target = GetComponentInParent<EnemyFinisherTarget>();
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }
}