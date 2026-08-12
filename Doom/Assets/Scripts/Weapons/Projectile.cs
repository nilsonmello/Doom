using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    [Header("Configurado automatico")]
    public int damage;
    public LayerMask hitMask = ~0;
    public Transform ownerRoot; // raiz do atirador (ex: transform.root do player)

    [Header("Configurado no prefab")]
    public LayerMask solidMask = ~0;

    public float lifeTime = 5f;
    public bool debugMode = true;

    public System.Action onDamageDealt;

    private Rigidbody rb;
    private bool hasHit = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        Destroy(gameObject, lifeTime);
    }

    public void Launch(Vector3 direction, float speed)
    {
        rb.linearVelocity = direction.normalized * speed;
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Ignora qualquer collider que pertença ao próprio atirador,
        // não importa em que filho/pai da hierarquia ele esteja.
        if (ownerRoot != null && other.transform.root == ownerRoot) return;

        // Projétil não deve colidir com outro projétil (ex: pellets de
        // shotgun nascendo sobrepostos no mesmo muzzlePoint).
        if (other.TryGetComponent<Projectile>(out _)) return;

        int layer = other.gameObject.layer;
        bool isDamageable = (hitMask.value & (1 << layer)) != 0;
        bool isSolid = (solidMask.value & (1 << layer)) != 0;

        if (!isDamageable && !isSolid) return;

        hasHit = true;

        if (isDamageable)
        {
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                onDamageDealt?.Invoke();
            }
        }

        if (debugMode)
            Debug.Log($"Projectile hit {other.name} (damageable={isDamageable}, solid={isSolid})", other);

        Destroy(gameObject);
    }
}