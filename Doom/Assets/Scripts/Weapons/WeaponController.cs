using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Referências")]
    public Camera playerCamera;
    public AudioSource audioSource;
    public Transform muzzlePoint;
    public PlayerMovementAdvanced playerMovement;
    public HandUIController handUI;

    [Header("Arma Atual")]
    public WeaponData weapon;
    public DroppedWeapon weaponToDrop;
    public GameObject weaponPrefab;

    [Header("Arremesso ao Dropar")]
    public float throwSpawnForwardOffset = 1.2f;
    public float dropHeightOffset = 0f;
    public float throwForwardForce = 6f;
    public float throwUpwardForce = 4f;

    [Header("Debug")]
    public bool debugMode = true;

    private enum WeaponState { Idle, Shooting, Reloading }
    private WeaponState state = WeaponState.Idle;

    private int currentAmmo;
    private int reserveAmmo;
    private float nextFireTime;

    void Awake()
    {
        weaponToDrop = weaponPrefab.GetComponent<DroppedWeapon>();
        weaponToDrop.weaponIndex = weapon;
    }

    void Start()
    {
        AutoResolveReferences();

        currentAmmo = weapon.magazineSize;
        reserveAmmo = weapon.maxReserveAmmo;

        SetIdle();
    }

    private void AutoResolveReferences()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null) playerCamera = Camera.main;
        }

        if (handUI == null)
        {
            handUI = GetComponent<HandUIController>();
            if (handUI == null) handUI = GetComponentInParent<HandUIController>();
            if (handUI == null) handUI = FindObjectOfType<HandUIController>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (muzzlePoint == null)
        {
            muzzlePoint = playerCamera != null ? playerCamera.transform : transform;
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovementAdvanced>();
            if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovementAdvanced>();
        }
    }

    void Update()
    {
        if (state == WeaponState.Reloading) return;
        if (weapon == null) return;

        bool wantsToShoot = weapon.isAutomatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);

        if (wantsToShoot && Time.time >= nextFireTime)
        {
            TryShoot();
        }

        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < weapon.magazineSize)
        {
            StartReload();
        }

        DropWeapon();
    }

    public void EquipWeapon(WeaponData newWeapon)
    {
        weapon = newWeapon;

        currentAmmo = weapon.magazineSize;
        reserveAmmo = weapon.maxReserveAmmo;

        SetIdle();
    }

    private void DropWeapon()
    {
        if (weapon == null) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Transform origin = playerCamera != null ? playerCamera.transform : transform;

            Vector3 forwardFlat = origin.forward;
            forwardFlat.y = 0f;

            if (forwardFlat.sqrMagnitude < 0.001f)
                forwardFlat = transform.forward;

            forwardFlat.Normalize();

            Vector3 spawnPos = transform.position + forwardFlat * throwSpawnForwardOffset + Vector3.up * dropHeightOffset;

            GameObject dropped = Instantiate(weaponPrefab, spawnPos, Quaternion.identity);

            DroppedWeapon droppedWeapon = dropped.GetComponent<DroppedWeapon>();
            droppedWeapon.weaponIndex = weapon;

            Vector3 launchVelocity = forwardFlat * throwForwardForce + Vector3.up * throwUpwardForce;
            droppedWeapon.Launch(launchVelocity);

            weapon = null;

            SetIdle();
        }
    }

    private void TryShoot()
    {
        if (currentAmmo <= 0)
        {
            if (weapon.emptySfx != null) audioSource.PlayOneShot(weapon.emptySfx);
            return;
        }

        nextFireTime = Time.time + weapon.fireRate;
        currentAmmo--;

        int pellets = Mathf.Max(1, weapon.pelletsPerShot);

        for (int i = 0; i < pellets; i++)
        {
            FireProjectile();
        }

        if (weapon.shootSfx != null) audioSource.PlayOneShot(weapon.shootSfx);

        handUI?.EjectShell(weapon.shellSprite);

        //playerMovement?.AddRecoil(weapon.recoilKick, weapon.recoilHorizontalRange);
        //playerMovement?.AddScreenShake(weapon.shakeOnShoot);

        state = WeaponState.Shooting;
        handUI?.PlayWeaponFrames(weapon.shootFrames, weapon.shootFrameRate, SetIdle);
    }

    private Vector3 ApplySpread(Vector3 direction, float spreadAngle)
    {
        if (spreadAngle <= 0f) return direction;

        float angle = Mathf.Sqrt(Random.Range(0f, 1f)) * spreadAngle * Mathf.Deg2Rad;
        float rotationAroundAxis = Random.Range(0f, 360f) * Mathf.Deg2Rad;

        Vector3 spreadDir = new Vector3(
            Mathf.Sin(angle) * Mathf.Cos(rotationAroundAxis),
            Mathf.Sin(angle) * Mathf.Sin(rotationAroundAxis),
            Mathf.Cos(angle)
        );

        return Quaternion.LookRotation(direction) * spreadDir;
    }

    private void FireProjectile()
    {
        if (weapon.projectilePrefab == null)
        {
            return;
        }

        Vector3 shootDirection = ApplySpread(playerCamera.transform.forward, weapon.spreadAngle);

        GameObject obj = Instantiate(weapon.projectilePrefab, muzzlePoint.position, Quaternion.LookRotation(shootDirection));
        Projectile proj = obj.GetComponent<Projectile>();

        if (proj != null)
        {
            proj.damage = weapon.damage;
            proj.hitMask = weapon.hitMask;
            proj.debugMode = debugMode;
            proj.ownerRoot = transform.root;

            float capturedShakeOnHit = weapon.shakeOnHit;
            float capturedHitStop = weapon.hitStopDuration;
            PlayerMovementAdvanced capturedMovement = playerMovement;

            proj.onDamageDealt = () =>
            {
                //capturedMovement?.AddScreenShake(capturedShakeOnHit);
                //HitStopManager.Request(capturedHitStop);
            };

            proj.Launch(shootDirection, weapon.projectileSpeed);
        }
    }

    private void StartReload()
    {
        if (reserveAmmo <= 0 && !weapon.infiniteReserve)
        {
            return;
        }

        state = WeaponState.Reloading;

        if (weapon.reloadSfx != null) audioSource.PlayOneShot(weapon.reloadSfx);

        handUI?.PlayWeaponFramesOverTime(weapon.reloadFrames, weapon.reloadTime, FinishReload);
    }

    private void FinishReload()
    {
        int needed = weapon.magazineSize - currentAmmo;
        int toLoad = weapon.infiniteReserve ? needed : Mathf.Min(needed, reserveAmmo);

        currentAmmo += toLoad;
        if (!weapon.infiniteReserve) reserveAmmo -= toLoad;

        SetIdle();
    }

    private void SetIdle()
    {
        state = WeaponState.Idle;

        if (weapon != null && weapon.idleSprite != null)
        {
            handUI?.SetWeaponSprite(weapon.idleSprite);
        }

        if (weapon == null)
        {
            handUI?.SetWeaponEmpty();
        }
    }

    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
}