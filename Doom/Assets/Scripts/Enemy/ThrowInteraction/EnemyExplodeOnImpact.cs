using UnityEngine;

public class EnemyExplodeOnImpact : EnemyImpactBehavior
{
    [Header("Explosão")]
    public float explosionRadius = 4f;
    public int explosionDamage = 40;
    public LayerMask explosionMask = ~0;
    public GameObject explosionVfxPrefab;

    public override void OnImpact(EnemyAI self, Collider hitCollider, RaycastHit hit)
    {
        if (explosionVfxPrefab != null)
            Instantiate(explosionVfxPrefab, hit.point, Quaternion.identity);

        Collider[] hits = Physics.OverlapSphere(self.transform.position, explosionRadius, explosionMask);
        foreach (var col in hits)
        {
            var damageable = col.GetComponent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(explosionDamage);
        }

        self.Kill();
    }
}