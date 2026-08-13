using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identificação")]
    public string weaponName = "Pistol";

    [Header("Disparo")]
    public bool isAutomatic = false;
    public int pelletsPerShot = 1;

    [Header("Spread")]
    public float spreadAngle = 0f;

    [Header("Combate (Hitscan)")]
    public int damage = 10;
    public float range = 50f;
    public float fireRate = 0.25f;
    [Tooltip("Deve incluir todas as layers que o tiro pode atingir: inimigos E ambiente/paredes. Não inclua a layer do Player.")]
    public LayerMask hitMask = ~0;

    [Header("Munição")]
    public int magazineSize = 12;
    public int maxReserveAmmo = 60;
    public float reloadTime = 1.2f;
    public bool infiniteReserve = false;

    [Header("Sprites")]
    public Sprite idleSprite;
    public Sprite[] shootFrames;
    public Sprite[] reloadFrames;
    public float shootFrameRate = 0.05f;

    [Header("Sprite Dropado")]
    public Sprite droppedSprite;

    [Header("Cápsula Ejetada")]
    public Sprite shellSprite;

    [Header("Áudio (opcional)")]
    public AudioClip shootSfx;
    public AudioClip reloadSfx;
    public AudioClip emptySfx;

    [Header("Feel — Recoil")]
    public float recoilKick = 2f;
    public float recoilHorizontalRange = 0.4f;

    [Header("Feel — Screen Shake")]
    public float shakeOnShoot = 0.05f;
    public float shakeOnHit = 0.12f;

    [Header("Feel — Hit-Stop")]
    public float hitStopDuration = 0.04f;
}