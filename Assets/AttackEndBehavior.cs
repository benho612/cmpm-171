using UnityEngine;

public class AttackEndBehaviour : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Get the BaseEnemy component and call OnAttackEnd when the state exits
        BaseEnemy enemy = animator.GetComponent<BaseEnemy>();
        if (enemy != null)
        {
            enemy.OnAttackEnd();
        }
    }
}