using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class BulletShellUI : MonoBehaviour
{
    [Header("Física (em pixels/segundo)")]
    public float riseGravity = 900f;
    public float fallGravity = 2200f;

    [Header("Peso / Arrasto")]
    [Range(0f, 1f)]
    public float horizontalDrag = 0.9f;
    public float maxFallSpeed = 1800f;

    [Header("Rotação")]
    public float minRotationSpeed = -360f;
    public float maxRotationSpeed = 360f;

    [Header("Vida útil")]
    public float lifeTime = 1.5f;
    public float fadeOutDuration = 0.4f;

    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private Vector2 velocity;
    private float rotationSpeed;
    private float timer;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Launch(float upwardForce, float horizontalRange, float forceVariance = 0.2f)
    {
        float randomUp = upwardForce * Random.Range(1f - forceVariance, 1f + forceVariance);
        float randomSide = Random.Range(-horizontalRange, horizontalRange);

        velocity = new Vector2(randomSide, randomUp);
        rotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);
        rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        timer = 0f;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        float currentGravity = velocity.y > 0f ? riseGravity : fallGravity;
        velocity.y -= currentGravity * dt;

        if (velocity.y < -maxFallSpeed)
            velocity.y = -maxFallSpeed;

        velocity.x = Mathf.Lerp(velocity.x, 0f, horizontalDrag * dt * 5f);

        rect.anchoredPosition += velocity * dt;
        rect.Rotate(0f, 0f, rotationSpeed * dt);

        timer += dt;

        float fadeStart = lifeTime - fadeOutDuration;
        if (timer >= fadeStart && fadeOutDuration > 0f)
        {
            float t = Mathf.InverseLerp(fadeStart, lifeTime, timer);
            canvasGroup.alpha = 1f - t;
        }

        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}