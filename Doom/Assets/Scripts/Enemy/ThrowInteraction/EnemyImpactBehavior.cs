using UnityEngine;

public abstract class EnemyImpactBehavior : MonoBehaviour
{
    public abstract void OnImpact(EnemyAI self, Collider hitCollider, RaycastHit hit);
}