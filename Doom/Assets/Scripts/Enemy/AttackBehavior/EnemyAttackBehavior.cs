using UnityEngine;

public abstract class EnemyAttackBehavior : MonoBehaviour
{
    public float attackRange = 2f;

    public abstract void TryAttack(EnemyAI self, Transform target);

    public abstract void CancelAttackAnimation();
}