using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour, IDamageable
{
    private enum State { Idle, Chase, Attack, Dead }

    [Header("Detecção")]
    public float sightRange = 15f;
    public LayerMask obstacleMask;

    [Header("Combate")]
    public int maxHealth = 30;

    [Header("Movimento")]
    public float chaseSpeed = 3.5f;

    [Header("Knockback")]
    public float knockbackDrag = 8f;
    public float knockbackGravity = -35f;
    public float knockbackSettleSpeed = 0.2f;
    public LayerMask groundMask;
    public float groundCheckDistance = 5f;
    public LayerMask wallMask;
    public float wallCheckRadius = 0.4f;

    [Header("Impacto")]
    public LayerMask impactMask;

    [Header("Morte")]
    public Sprite deathSprite;
    public float deathDelay = 2f;

    [Header("Dano")]
    [Tooltip("Sprite exibido rapidamente quando o inimigo toma dano.")]
    public Sprite hitSprite;
    [Tooltip("Duração em segundos que o sprite de dano fica visível antes de voltar ao normal.")]
    public float hitFlashDuration = 0.1f;

    [Header("Captura")]
    public Sprite heldUISprite;

    [Header("Referências")]
    public Transform player;
    public SpriteRenderer spriteRenderer;
    public Sprite idleSprite;
    public Sprite[] walkSprites;

    private State currentState = State.Idle;
    public NavMeshAgent agent;
    private int currentHealth;
    private float walkAnimTimer;
    private int walkFrameIndex;

    private bool isKnockedBack;
    private Vector3 knockbackVelocity;
    private bool hasHitImpactTarget;
    private bool isThrownByPlayer;

    private bool isHeld;
    private EnemyImpactBehavior impactBehavior;
    private EnemyAttackBehavior attackBehavior;
    private EnemyCatchBehavior catchBehavior;

    private Collider ownCollider;
    private Collider playerCollider;

    private Vector3 originalScale;

    private Coroutine hitFlashCoroutine;
    private bool isHitFlashing;

    public bool CanBeGrabbed => isKnockedBack && !isHeld && currentState != State.Dead;
    public bool IsHeld => isHeld;
    public bool CanBlockDamage => catchBehavior != null && catchBehavior.BlocksDamage;

    /// <summary>
    /// True enquanto o flash de dano estiver ativo. Sistemas externos (como animações
    /// de ataque) devem checar isso antes de escrever sprites, pra não perder frames
    /// silenciosamente via SetSprite.
    /// </summary>
    public bool IsHitFlashing => isHitFlashing;

    public event System.Action<Collider, RaycastHit> OnThrowImpact;

    private float AttackRange => attackBehavior != null ? attackBehavior.attackRange : 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = chaseSpeed;
        currentHealth = maxHealth;

        impactBehavior = GetComponent<EnemyImpactBehavior>();
        attackBehavior = GetComponent<EnemyAttackBehavior>();
        catchBehavior = GetComponent<EnemyCatchBehavior>();
        ownCollider = GetComponent<Collider>();

        originalScale = transform.localScale;

        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        if (player != null)
            playerCollider = player.GetComponent<Collider>();

        SetState(State.Idle);
    }

    void Update()
    {
        if (currentState == State.Dead || player == null) return;

        if (isHeld) return;

        if (isKnockedBack)
        {
            UpdateKnockback();
            return;
        }

        float distToPlayer = Vector3.Distance(transform.position, player.position);
        float attackRange = AttackRange;

        switch (currentState)
        {
            case State.Idle:
                if (distToPlayer <= sightRange && HasLineOfSight())
                    SetState(State.Chase);
                break;

            case State.Chase:
                if (distToPlayer <= attackRange)
                {
                    SetState(State.Attack);
                }
                else if (distToPlayer > sightRange * 1.5f)
                {
                    SetState(State.Idle);
                }
                else if (agent.isOnNavMesh)
                {
                    agent.SetDestination(player.position);
                    AnimateWalk();
                }
                break;

            case State.Attack:
                if (distToPlayer > attackRange)
                {
                    SetState(State.Chase);
                }
                else
                {
                    agent.ResetPath();
                    attackBehavior?.TryAttack(this, player);
                }
                break;
        }
    }

    public void ApplyKnockback(Vector3 force, bool fromPlayerThrow = false)
    {
        if (currentState == State.Dead) return;

        attackBehavior?.CancelAttackAnimation();
        isKnockedBack = true;
        agent.enabled = false;
        knockbackVelocity = force;
        hasHitImpactTarget = false;
        isThrownByPlayer = fromPlayerThrow;

        if (ownCollider != null && playerCollider != null)
            Physics.IgnoreCollision(ownCollider, playerCollider, true);
    }

    public void Grab(Transform holdPoint)
    {
        if (!CanBeGrabbed) return;

        isHeld = true;
        isKnockedBack = false;
        knockbackVelocity = Vector3.zero;

        if (ownCollider != null) ownCollider.enabled = false;

        if (spriteRenderer != null) spriteRenderer.enabled = false;

        transform.SetParent(holdPoint, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = originalScale;

        catchBehavior?.OnGrab(this);
    }

    public void Throw(Vector3 force)
    {
        if (!isHeld) return;

        isHeld = false;

        Vector3 worldPos = transform.position;
        Quaternion worldRot = transform.rotation;

        transform.SetParent(null, false);

        transform.position = worldPos;
        transform.rotation = worldRot;
        transform.localScale = originalScale;

        if (ownCollider != null) ownCollider.enabled = true;

        if (spriteRenderer != null) spriteRenderer.enabled = true;

        catchBehavior?.OnThrow(this);

        ApplyKnockback(force, fromPlayerThrow: true);
    }

    private void UpdateKnockback()
    {
        knockbackVelocity.y += knockbackGravity * Time.deltaTime;

        Vector3 horizontal = new Vector3(knockbackVelocity.x, 0f, knockbackVelocity.z);
        horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, knockbackDrag * Time.deltaTime);
        knockbackVelocity.x = horizontal.x;
        knockbackVelocity.z = horizontal.z;

        Vector3 delta = knockbackVelocity * Time.deltaTime;

        Vector3 horizontalDelta = new Vector3(delta.x, 0f, delta.z);
        float horizontalDist = horizontalDelta.magnitude;

        if (horizontalDist > 0.0001f)
        {
            Vector3 dir = horizontalDelta.normalized;
            LayerMask combinedMask = wallMask | impactMask;

            if (Physics.SphereCast(transform.position, wallCheckRadius, dir, out RaycastHit hitInfo, horizontalDist, combinedMask))
            {
                bool isImpactTarget = (impactMask.value & (1 << hitInfo.collider.gameObject.layer)) != 0;

                if (isImpactTarget && isThrownByPlayer && !hasHitImpactTarget)
                {
                    hasHitImpactTarget = true;
                    impactBehavior?.OnImpact(this, hitInfo.collider, hitInfo);
                    OnThrowImpact?.Invoke(hitInfo.collider, hitInfo);
                }

                if (currentState != State.Dead)
                {
                    float safeDist = Mathf.Max(0f, hitInfo.distance - 0.05f);
                    horizontalDelta = dir * safeDist;

                    knockbackVelocity.x = 0f;
                    knockbackVelocity.z = 0f;
                }
            }
        }

        if (currentState == State.Dead) return;

        Vector3 nextPos = transform.position + new Vector3(horizontalDelta.x, delta.y, horizontalDelta.z);

        bool falling = knockbackVelocity.y <= 0f;
        bool hitGround = false;

        if (falling)
        {
            float castDistance = Mathf.Max(groundCheckDistance, Mathf.Abs(delta.y) + 0.2f);
            Vector3 castOrigin = transform.position + Vector3.up * 0.05f;

            if (Physics.Raycast(castOrigin, Vector3.down, out RaycastHit groundHit, castDistance, groundMask))
            {

                float feetOffset = ownCollider != null
                    ? transform.position.y - ownCollider.bounds.min.y
                    : 0f;

                float groundLevel = groundHit.point.y + feetOffset;

                if (nextPos.y <= groundLevel)
                {
                    nextPos.y = groundLevel;
                    knockbackVelocity.y = 0f;
                    hitGround = true;

                    bool isImpactTarget = (impactMask.value & (1 << groundHit.collider.gameObject.layer)) != 0;

                    if (isImpactTarget && isThrownByPlayer && !hasHitImpactTarget)
                    {
                        hasHitImpactTarget = true;
                        impactBehavior?.OnImpact(this, groundHit.collider, groundHit);
                        OnThrowImpact?.Invoke(groundHit.collider, groundHit);
                    }
                }
            }
        }

        if (currentState == State.Dead) return;

        transform.position = nextPos;

        bool settled = hitGround && horizontal.magnitude < knockbackSettleSpeed;
        if (settled) EndKnockback();
    }

    private void EndKnockback()
    {
        isKnockedBack = false;
        agent.enabled = true;

        if (ownCollider != null && playerCollider != null)
            Physics.IgnoreCollision(ownCollider, playerCollider, false);

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
        {
            transform.position = navHit.position;
            agent.Warp(navHit.position);
        }
        else
        {
            agent.Warp(transform.position);
        }
    }

    private bool HasLineOfSight()
    {
        Vector3 dir = player.position - transform.position;
        if (Physics.Raycast(transform.position, dir.normalized, out RaycastHit hit, sightRange, obstacleMask))
        {
            return false;
        }
        return true;
    }

    private void AnimateWalk()
    {
        if (spriteRenderer == null || walkSprites == null || walkSprites.Length == 0) return;

        walkAnimTimer += Time.deltaTime;
        if (walkAnimTimer >= 0.2f)
        {
            walkAnimTimer = 0f;
            walkFrameIndex = (walkFrameIndex + 1) % walkSprites.Length;
            SetSprite(walkSprites[walkFrameIndex]);
        }
    }

    private void SetState(State newState)
    {
        currentState = newState;

        if (newState == State.Idle && idleSprite != null)
            SetSprite(idleSprite);
    }

    /// <summary>
    /// Ponto único de escrita no spriteRenderer. Sistemas externos (como o ataque)
    /// devem chamar isso em vez de escrever direto no spriteRenderer, pra respeitar
    /// a prioridade do flash de dano.
    /// </summary>
    public void SetSprite(Sprite sprite)
    {
        if (spriteRenderer == null || sprite == null) return;
        if (isHitFlashing) return;

        spriteRenderer.sprite = sprite;
    }

    /// <summary>
    /// Volta o sprite pro idle. Útil pra ser chamado por comportamentos externos
    /// (como o de ataque) quando terminam sua própria animação.
    /// </summary>
    public void ReturnToIdleSprite()
    {
        if (idleSprite != null)
            SetSprite(idleSprite);
    }

    public void TakeDamage(int amount)
    {
        if (currentState == State.Dead) return;

        attackBehavior?.CancelAttackAnimation();
        currentHealth -= amount;

        if (currentState == State.Idle)
            SetState(State.Chase);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        PlayHitFlash();
    }

    private void PlayHitFlash()
    {
        if (spriteRenderer == null || hitSprite == null) return;

        if (hitFlashCoroutine != null)
            StopCoroutine(hitFlashCoroutine);

        hitFlashCoroutine = StartCoroutine(RotinaHitFlash());
    }

    private IEnumerator RotinaHitFlash()
    {
        isHitFlashing = true;
        spriteRenderer.sprite = hitSprite;

        yield return new WaitForSeconds(hitFlashDuration);

        isHitFlashing = false;
        hitFlashCoroutine = null;

        ReturnToIdleSprite();
    }

    public void Kill()
    {
        if (currentState == State.Dead) return;
        Die();
    }

    private void Die()
    {
        SetState(State.Dead);

        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = null;
        }
        isHitFlashing = false;

        attackBehavior?.CancelAttackAnimation();

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.ResetPath();

        agent.enabled = false;

        if (isHeld)
        {
            isHeld = false;

            Vector3 worldPos = transform.position;
            Quaternion worldRot = transform.rotation;

            transform.SetParent(null, false);

            transform.position = worldPos;
            transform.rotation = worldRot;
            transform.localScale = originalScale;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            if (deathSprite != null) spriteRenderer.sprite = deathSprite;
        }

        if (ownCollider != null)
        {
            if (playerCollider != null)
                Physics.IgnoreCollision(ownCollider, playerCollider, false);

            ownCollider.enabled = false;
        }

        Destroy(gameObject, deathDelay);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);
    }
}