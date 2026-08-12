using System.Collections;
using UnityEngine;

public class EnemyMeleeAttack : EnemyAttackBehavior
{
    [Header("Ataque Corpo a Corpo")]
    public float attackCooldown = 1.2f;
    public int attackDamage = 10;

    [Header("Animação de Ataque")]
    public Sprite[] attackFrames;
    public float attackFrameRate = 0.08f;

    private float lastAttackTime;
    private Coroutine attackAnimCoroutine;

    public override void TryAttack(EnemyAI self, Transform target)
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;

        PlayAttackAnimation(self);

        var health = target.GetComponent<PlayerHealth>();
        if (health != null)
            health.TakeDamage(attackDamage, self.transform.position);
    }

    private void PlayAttackAnimation(EnemyAI self)
    {
        if (self == null) return;

        if (attackAnimCoroutine != null)
        {
            StopCoroutine(attackAnimCoroutine);
            attackAnimCoroutine = null;
        }

        if (attackFrames == null || attackFrames.Length == 0)
            return;

        attackAnimCoroutine = StartCoroutine(RotinaAnimacaoAtaque(self));
    }

    public override void CancelAttackAnimation()
    {
        if (attackAnimCoroutine != null)
        {
            StopCoroutine(attackAnimCoroutine);
            attackAnimCoroutine = null;
        }
    }

    private IEnumerator RotinaAnimacaoAtaque(EnemyAI self)
    {
        foreach (var frame in attackFrames)
        {
            if (self != null)
                self.SetSprite(frame);

            yield return new WaitForSeconds(attackFrameRate);
        }

        if (self != null)
            self.ReturnToIdleSprite();

        attackAnimCoroutine = null;
    }
}