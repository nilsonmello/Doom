using UnityEngine;

public class EnemyDamageOnImpact : EnemyImpactBehavior
{
    [Header("Dano de Impacto")]
    public int damageDealt = 15;
    public int damageTaken = 10;

    public override void OnImpact(EnemyAI self, Collider hitCollider, RaycastHit hit)
    {
        var damageable = hitCollider.GetComponent<IDamageable>();
        if (damageable != null)
            damageable.TakeDamage(damageDealt);

        self.TakeDamage(damageTaken);
    }
}