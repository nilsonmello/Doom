using UnityEngine;

public class EnemyMeleeAttack : EnemyAttackBehavior
{
    [Header("Ataque Corpo a Corpo")]
    public float attackCooldown = 1.2f;
    public int attackDamage = 10;
    public Sprite attackSprite;

    private float lastAttackTime;

    public override void TryAttack(EnemyAI self, Transform target)
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;

        if (self.spriteRenderer != null && attackSprite != null)
            self.spriteRenderer.sprite = attackSprite;

        var health = target.GetComponent<PlayerHealth>();
        if (health != null)
            health.TakeDamage(attackDamage, self.transform.position);
    }
}