using UnityEngine;

public abstract class EnemyAttackBehavior : MonoBehaviour
{
    //attack range base
    public float attackRange = 2f;

    //attack method
    public abstract void TryAttack(EnemyAI self, Transform target);
}