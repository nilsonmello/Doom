using UnityEngine;

public class BillboardSprite : MonoBehaviour
{
    public enum Mode
    {
        Cylindrical,
        FullBillboard,
        ClampedPitch
    }

    [Header("Modo")]
    public Mode mode = Mode.ClampedPitch;

    [Header("Clamped Pitch")]
    [Range(0f, 90f)] public float maxPitchAngle = 30f;

    [Header("Anti-flicker")]
    public float updateInterval = 0f;

    private Transform cam;
    private float nextUpdateTime;

    void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (cam == null) return;

        if (updateInterval > 0f)
        {
            if (Time.time < nextUpdateTime) return;
            nextUpdateTime = Time.time + updateInterval;
        }

        switch (mode)
        {
            case Mode.FullBillboard:
                transform.LookAt(transform.position + cam.forward);
                break;

            case Mode.ClampedPitch:
                ApplyClampedPitch();
                break;

            default:
                ApplyCylindrical();
                break;
        }
    }

    private void ApplyCylindrical()
    {
        Vector3 direction = cam.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(-direction);
    }

    private void ApplyClampedPitch()
    {
        Vector3 toCam = cam.position - transform.position;

        Vector3 flat = toCam;
        flat.y = 0f;

        if (flat.sqrMagnitude < 0.001f)
            flat = transform.forward;

        Quaternion yawRotation = Quaternion.LookRotation(-flat.normalized);

        float horizontalDist = flat.magnitude;
        float verticalDist = toCam.y;
        float pitchAngle = Mathf.Atan2(verticalDist, horizontalDist) * Mathf.Rad2Deg;
        float clampedPitch = Mathf.Clamp(pitchAngle, -maxPitchAngle, maxPitchAngle);

        Quaternion pitchRotation = Quaternion.AngleAxis(-clampedPitch, yawRotation * Vector3.right);

        transform.rotation = pitchRotation * yawRotation;
    }
}