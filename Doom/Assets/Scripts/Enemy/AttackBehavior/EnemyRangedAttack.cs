using UnityEngine;

public class EnemyRangedAttack : EnemyAttackBehavior
{
    [Header("Ataque a Distância")]
    public float attackCooldown = 2f;
    public int damage = 8;
    public LayerMask hitMask = ~0;
    public LayerMask solidMask = ~0;
    public Sprite attackSprite;

    [Header("Projétil")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 25f;
    public Transform muzzlePoint;

    private float lastAttackTime;

    public override void TryAttack(EnemyAI self, Transform target)
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        if (projectilePrefab == null)
        {
            return;
        }

        lastAttackTime = Time.time;

        if (self.spriteRenderer != null && attackSprite != null)
            self.spriteRenderer.sprite = attackSprite;

        Vector3 origin = muzzlePoint != null ? muzzlePoint.position : self.transform.position + Vector3.up * 1f;
        Vector3 direction = (target.position - origin).normalized;

        GameObject obj = Object.Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
        Projectile proj = obj.GetComponent<Projectile>();

        if (proj == null)
        {
            Object.Destroy(obj);
            return;
        }

        proj.damage = damage;
        proj.hitMask = hitMask;
        proj.solidMask = solidMask;
        proj.debugMode = false;

        Collider[] ownColliders = self.GetComponentsInChildren<Collider>();
        Collider projCollider = obj.GetComponent<Collider>();
        if (projCollider != null)
        {
            foreach (var ownCollider in ownColliders)
            {
                if (ownCollider != null)
                    Physics.IgnoreCollision(projCollider, ownCollider);
            }
        }

        proj.Launch(direction, projectileSpeed);
    }
}