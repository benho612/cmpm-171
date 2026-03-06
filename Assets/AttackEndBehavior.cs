using UnityEngine;

public class AttackEndBehaviour : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        BaseEnemy enemy = animator.GetComponent<BaseEnemy>();
        if (enemy == null) return;

        // Only call OnAttackEnd if the enemy is actually in an attack
        if (enemy.IsAttacking())
        {
            enemy.OnAttackEnd();
        }
    }
}