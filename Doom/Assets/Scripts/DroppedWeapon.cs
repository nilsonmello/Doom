using UnityEngine;

public class DroppedWeapon : MonoBehaviour
{
    public WeaponData weaponIndex;
    public SpriteRenderer spriteRenderer;

    public Vector2 worldSize = new Vector2(0.6f, 0.6f);

    public float gravity = -9.8f;
    public LayerMask groundMask;
    public float groundOffset = 0.05f;
    public float collisionSkin = 0.05f;

    public float pickupDelay = 0.5f;

    private Vector3 velocity;
    private bool isFlying;
    private float spawnTime;

    void Start()
    {
        spawnTime = Time.time;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (weaponIndex == null || weaponIndex.idleSprite == null)
            return;

        spriteRenderer.sprite = weaponIndex.idleSprite;

        Vector2 nativeSize = spriteRenderer.sprite.bounds.size;

        float scaleX = nativeSize.x > 0f ? worldSize.x / nativeSize.x : 1f;
        float scaleY = nativeSize.y > 0f ? worldSize.y / nativeSize.y : 1f;

        transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    void Update()
    {
        if (!isFlying) return;

        velocity.y += gravity * Time.deltaTime;

        Vector3 moveDelta = velocity * Time.deltaTime;
        float moveDist = moveDelta.magnitude;

        if (moveDist > 0.0001f)
        {
            Vector3 moveDir = moveDelta.normalized;

            if (Physics.Raycast(transform.position, moveDir, out RaycastHit hit, moveDist + collisionSkin, groundMask))
            {
                transform.position = hit.point + hit.normal * groundOffset;
                Land();
                return;
            }
        }

        transform.position += moveDelta;
    }

    public void Launch(Vector3 initialVelocity)
    {
        velocity = initialVelocity;
        isFlying = true;
    }

    private void Land()
    {
        isFlying = false;
        velocity = Vector3.zero;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time - spawnTime < pickupDelay)
            return;

        if (!other.CompareTag("Player"))
            return;

        WeaponController weaponController = other.GetComponent<WeaponController>();

        if (weaponController == null)
            return;

        weaponController.EquipWeapon(weaponIndex);

        Destroy(gameObject);
    }
}