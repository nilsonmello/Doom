using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerGrabController : MonoBehaviour
{
    [Header("Referências")]
    public Camera playerCamera;
    public Transform holdPoint;
    public PlayerCam cam;
    public HandUIController handUI;
    public PlayerMovementAdvanced playerMovement;

    [Header("Imagem do Holdpoint")]
    [Tooltip("Image que recebe o sprite do inimigo agarrado. Fica ativa só enquanto um inimigo está sendo segurado; desativa sozinha ao soltar/arremessar ou se nenhum inimigo estiver agarrado.")]
    public Image holdPointImage;

    [Header("Slide Grab")]
    public bool autoGrabOnSlide = true;
    public float slideGrabSlowdown = 0.5f;

    [Header("Captura")]
    public float grabRange = 4f;
    public LayerMask enemyMask = ~0;

    [Header("Arremesso Carregável")]
    public float minThrowForce = 5f;
    public float maxThrowForce = 35f;
    public float maxChargeTime = 2f;
    public float throwUpwardBoost = 2f;
    public AnimationCurve chargeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Agarrar")]
    public float grabShakeAmount = 0.06f;
    public float grabCameraPunch = -0.05f;

    [Header("Arremesso")]
    public float throwShakeAmount = 0.08f;
    public float throwCameraPunch = 0.08f;
    public float throwImpactShakeAmount = 0.22f;
    public float throwImpactHitStop = 0.06f;

    [Header("Feedback Visual de Carga")]
    public bool showChargeFeedback = true;
    public float chargeCameraZoom = -0.1f;
    public float chargeShakeIntensity = 0.02f;

    [Header("Debug")]
    public bool debugMode = true;

    public float shieldProtectionAngle = 100f;

    private EnemyAI heldEnemy;
    private bool isChargingThrow;
    private float chargeStartTime;
    private float currentChargePercent;
    
    private Vector3 originalCameraLocalPos;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (cam == null)
        {
            cam = GetComponent<PlayerCam>();
            if (cam == null) cam = GetComponentInParent<PlayerCam>();
        }

        if (holdPoint == null)
        {
            GameObject go = new GameObject("HoldPoint");
            go.transform.SetParent(playerCamera.transform);
            go.transform.localPosition = new Vector3(0f, 0f, 1.5f);
            holdPoint = go.transform;
        }

        if (handUI == null)
        {
            handUI = GetComponent<HandUIController>();
            if (handUI == null) handUI = GetComponentInParent<HandUIController>();
            if (handUI == null) handUI = FindFirstObjectByType<HandUIController>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovementAdvanced>();
            if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovementAdvanced>();
        }

        if (playerCamera != null)
            originalCameraLocalPos = playerCamera.transform.localPosition;
    }

    void Update()
    {
        if (heldEnemy == null) return;
        
        if (Input.GetMouseButtonDown(1))
        {
            StartChargeThrow();
        }
        
        if (Input.GetMouseButton(1) && isChargingThrow)
        {
            UpdateChargeThrow();
        }
        
        if (Input.GetMouseButtonUp(1) && isChargingThrow)
        {
            ReleaseChargedThrow();
        }
    }

    /// <summary>
    /// O collider do inimigo é Trigger, então a detecção de toque acontece
    /// aqui. Só tenta agarrar se autoGrabOnSlide estiver ligado, o player
    /// estiver deslizando (quando playerMovement está atribuído), nenhum
    /// inimigo já estiver sendo segurado, e o objeto colidido estiver dentro
    /// de enemyMask.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        TryAutoGrab(other);
    }

    private void TryAutoGrab(Collider other)
    {
        if (!autoGrabOnSlide) return;
        if (heldEnemy != null) return;

        if (playerMovement != null && !playerMovement.sliding) return;

        // respeita a layer mask configurada no Inspector
        if ((enemyMask.value & (1 << other.gameObject.layer)) == 0) return;

        EnemyAI enemy = other.GetComponentInParent<EnemyAI>();
        if (enemy == null) return;

        GrabEnemyOnSlide(enemy);
    }

    private void StartChargeThrow()
    {
        isChargingThrow = true;
        chargeStartTime = Time.time;
        currentChargePercent = 0f;
        
    }

    private void UpdateChargeThrow()
    {
        float elapsedTime = Time.time - chargeStartTime;
        currentChargePercent = Mathf.Clamp01(elapsedTime / maxChargeTime);
        
        float curvedPercent = chargeCurve.Evaluate(currentChargePercent);
        
        if (showChargeFeedback && playerCamera != null)
        {
            float zoomOffset = chargeCameraZoom * currentChargePercent;
            Vector3 targetPos = originalCameraLocalPos + new Vector3(0f, 0f, zoomOffset);
            playerCamera.transform.localPosition = Vector3.Lerp(
                playerCamera.transform.localPosition, 
                targetPos, 
                Time.deltaTime * 10f
            );
            
            if (currentChargePercent > 0.8f)
            {
                float shakeIntensity = chargeShakeIntensity * ((currentChargePercent - 0.8f) / 0.2f);
                float shakeX = Random.Range(-shakeIntensity, shakeIntensity);
                float shakeY = Random.Range(-shakeIntensity, shakeIntensity);
                playerCamera.transform.localPosition += new Vector3(shakeX, shakeY, 0f);
            }
        }
        
        handUI?.UpdateChargePercent(currentChargePercent);
        
        if (debugMode && currentChargePercent >= 1f)
        {
            Debug.Log("Carga máxima atingida");
        }
    }

    private void ReleaseChargedThrow()
    {
        isChargingThrow = false;
        
        float throwPower = Mathf.Lerp(minThrowForce, maxThrowForce, currentChargePercent);
        
        ThrowHeldEnemy(throwPower);
        
        StopAllCoroutines();
        StartCoroutine(ResetCameraPosition());
    }

    private IEnumerator ResetCameraPosition()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 startPos = playerCamera.transform.localPosition;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            playerCamera.transform.localPosition = Vector3.Lerp(startPos, originalCameraLocalPos, t);
            yield return null;
        }
        
        playerCamera.transform.localPosition = originalCameraLocalPos;
    }

    public void GrabEnemyOnSlide(EnemyAI enemy)
    {
        if (enemy == null || heldEnemy != null) return;
        
        Vector3 knockbackDir = transform.forward;
        knockbackDir.y = 0f;
        knockbackDir.Normalize();
        
        Vector3 minimalKnockback = knockbackDir * 2f + Vector3.up * 1f;
        enemy.ApplyKnockback(minimalKnockback);
        
        if (enemy.CanBeGrabbed)
        {
            enemy.Grab(holdPoint);
            heldEnemy = enemy;

            handUI?.ShowHeldEnemy(enemy.heldUISprite);
            SetHoldPointImage(enemy.heldUISprite);

            cam?.AddScreenShake(grabShakeAmount);
            cam?.AddCameraPunch(grabCameraPunch);
            
        }
        else
        {
            StartCoroutine(GrabAfterKnockbackDelay(enemy));
        }
    }

    private IEnumerator GrabAfterKnockbackDelay(EnemyAI enemy)
    {
        yield return null;
        yield return null;
        
        if (enemy == null || heldEnemy != null) yield break;
        
        if (!enemy.CanBeGrabbed)
        {
            yield break;
        }
        
        enemy.Grab(holdPoint);
        heldEnemy = enemy;

        handUI?.ShowHeldEnemy(enemy.heldUISprite);
        SetHoldPointImage(enemy.heldUISprite);

        cam?.AddScreenShake(grabShakeAmount);
        cam?.AddCameraPunch(grabCameraPunch);
    }

    public EnemyAI GetHeldEnemy()
    {
        return heldEnemy;
    }

    public float GetSlideGrabSlowdown()
    {
        return slideGrabSlowdown;
    }

    private void ThrowHeldEnemy(float throwForce)
    {
        if (heldEnemy == null)
        {
            handUI?.ClearHeldEnemy();
            SetHoldPointImage(null);
            return;
        }

        EnemyAI enemyBeingThrown = heldEnemy;

        void OnImpact(Collider other, RaycastHit hit)
        {
            float impactShakeMultiplier = 1f + (currentChargePercent * 1.5f);
            cam?.AddScreenShake(throwImpactShakeAmount * impactShakeMultiplier);
            HitStopManager.Request(throwImpactHitStop * (1f + currentChargePercent));
            enemyBeingThrown.OnThrowImpact -= OnImpact;
        }
        enemyBeingThrown.OnThrowImpact += OnImpact;

        Vector3 direction = playerCamera.transform.forward;
        
        float upwardMultiplier = 1f + (currentChargePercent * 0.5f);
        Vector3 force = direction * throwForce + Vector3.up * (throwUpwardBoost * upwardMultiplier);

        float shakeMultiplier = 1f + (currentChargePercent * 2f);
        cam?.AddScreenShake(throwShakeAmount * shakeMultiplier);
        cam?.AddCameraPunch(throwCameraPunch * shakeMultiplier);

        enemyBeingThrown.Throw(force);
        heldEnemy = null;
        currentChargePercent = 0f;

        handUI?.ClearHeldEnemy();
        handUI?.UpdateChargePercent(0f);
        SetHoldPointImage(null);
    }

    public bool TryAbsorbDamage(int amount, Vector3? sourcePosition = null)
    {
        if (heldEnemy == null) return false;
        if (!heldEnemy.CanBlockDamage) return false;

        if (sourcePosition.HasValue && !IsAttackFromFront(sourcePosition.Value))
        {
            return false;
        }

        heldEnemy.TakeDamage(amount);

        if (!heldEnemy.IsHeld)
        {
            heldEnemy = null;
            handUI?.ClearHeldEnemy();
            SetHoldPointImage(null);
            
            if (isChargingThrow)
            {
                isChargingThrow = false;
                currentChargePercent = 0f;
                handUI?.UpdateChargePercent(0f);
            }
        }

        return true;
    }

    private bool IsAttackFromFront(Vector3 sourcePosition)
    {
        Vector3 toSource = sourcePosition - transform.position;
        toSource.y = 0f;

        if (toSource.sqrMagnitude < 0.01f) return true;

        float angle = Vector3.Angle(transform.forward, toSource.normalized);
        return angle <= shieldProtectionAngle / 2f;
    }

    /// <summary>
    /// Atualiza a imagem do holdpoint com o sprite do inimigo agarrado.
    /// Passar null desativa a imagem (usado ao soltar/arremessar ou quando
    /// não há nenhum inimigo agarrado).
    /// </summary>
    private void SetHoldPointImage(Sprite sprite)
    {
        if (holdPointImage == null) return;

        holdPointImage.sprite = sprite;
        holdPointImage.enabled = sprite != null;
    }
}