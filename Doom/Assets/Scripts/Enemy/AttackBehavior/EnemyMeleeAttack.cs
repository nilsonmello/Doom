using System.Collections;
using UnityEngine;

public class EnemyMeleeAttack : EnemyAttackBehavior
{
    [Header("Ataque Corpo a Corpo")]
    public float attackCooldown = 1.2f;
    public int attackDamage = 10;

    [Header("Animação de Ataque")]
    [Tooltip("Sequência de sprites tocada do início ao fim de cada ataque.")]
    public Sprite[] attackFrames;
    [Tooltip("Tempo em segundos que cada frame fica na tela.")]
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
            // Espera o flash de dano terminar antes de aplicar o frame, em vez de
            // deixar o SetSprite descartar a chamada silenciosamente e perder o frame.
            while (self != null && self.IsHitFlashing)
                yield return null;

            if (self == null) yield break;

            self.SetSprite(frame);
            yield return new WaitForSeconds(attackFrameRate);
        }

        if (self != null)
            self.ReturnToIdleSprite();

        attackAnimCoroutine = null;
    }
}