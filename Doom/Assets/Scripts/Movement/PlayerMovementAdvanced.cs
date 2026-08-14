using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementAdvanced : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;
    public PlayerCam cam;
    public Transform orientation;

    [Header("Movement Speeds")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 9f;
    public float crouchSpeed = 3f;
    public float wallRunSpeed = 8f;
    public float slideSpeed = 12f;

    [Header("Ground / Air Acceleration")]
    public float groundAcceleration = 45f;
    public float groundDeceleration = 55f;
    public float groundStopDeceleration = 40f;
    public float groundTurnRate = 25f;
    public float airAcceleration = 20f;
    public float airTurnRate = 6f;

    [Header("Gravity & Jump")]
    public float gravity = -25f;
    public float jumpForce = 9f;
    public float jumpCooldown = 0.2f;
    private bool readyToJump = true;

    [Header("Double Jump")]
    public int maxJumps = 2;
    private int jumpsRemaining;

    [Header("Crouching")]
    public KeyCode crouchKey = KeyCode.LeftControl;
    [Range(0.1f, 1f)] public float crouchHeightScale = 0.5f;
    public float crouchTransitionSpeed = 10f;
    public LayerMask ceilingMask;
    [HideInInspector] public bool crouching;
    private float standingHeight;

    [Header("Slope Handling")]
    public float maxSlopeAngle = 40f;
    private RaycastHit slopeHit;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;

    public enum MovementState { walking, sprinting, crouching, sliding, wallRunning, air }
    public MovementState state;

    [HideInInspector] public bool sliding;
    [HideInInspector] public bool wallRunning;

    [HideInInspector] public bool suppressNextJumpInput;

    [Header("Momentum Externo")]
    public float externalVelocityDrag = 6f;
    public float maxExternalSpeed = 20f;
    private Vector3 externalVelocity;

    [HideInInspector] public Vector3 horizontalVelocity;

    [HideInInspector] public float verticalVelocity;

    public bool grounded;
    private bool groundedLastFrame;

    public event System.Action OnJumped;

    private float horizontalInput;
    private float verticalInput;
    private float desiredSpeed;

    private float requestedHeight = -1f;

    private void Start()
    {
        if (controller == null) controller = GetComponent<CharacterController>();

        standingHeight = controller.height;
        jumpsRemaining = maxJumps;
    }

    private void Update()
    {
        grounded = controller.isGrounded;
        bool justLanded = grounded && !groundedLastFrame;
        groundedLastFrame = grounded;

        if (justLanded)
            jumpsRemaining = maxJumps;

        MyInput();
        StateHandler();

        if (!sliding && !wallRunning)
            UpdateHorizontalVelocity();

        UpdateVerticalVelocity();
    }

    private void LateUpdate()
    {
        externalVelocity = Vector3.MoveTowards(externalVelocity, Vector3.zero, externalVelocityDrag * Time.deltaTime);

        ApplyHeight();

        Vector3 finalMove = horizontalVelocity + externalVelocity + Vector3.up * verticalVelocity;
        controller.Move(finalMove * Time.deltaTime);

        suppressNextJumpInput = false;
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(jumpKey) && readyToJump && jumpsRemaining > 0 && !wallRunning && !suppressNextJumpInput)
        {
            readyToJump = false;
            jumpsRemaining--;

            verticalVelocity = jumpForce;
            OnJumped?.Invoke();

            Invoke(nameof(ResetJump), jumpCooldown);
        }

        if (Input.GetKeyDown(crouchKey) && !sliding)
            crouching = true;

        if (Input.GetKeyUp(crouchKey))
            crouching = false;
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void StateHandler()
    {
        if (wallRunning)
        {
            state = MovementState.wallRunning;
            desiredSpeed = wallRunSpeed;
        }
        else if (sliding)
        {
            state = MovementState.sliding;
            desiredSpeed = slideSpeed;
        }
        else if (crouching)
        {
            state = MovementState.crouching;
            desiredSpeed = crouchSpeed;
        }
        else if (grounded && Input.GetKey(sprintKey))
        {
            cam.DoFov(90f);
            state = MovementState.sprinting;
            desiredSpeed = sprintSpeed;
        }
        else if (grounded)
        {
            cam.DoFov(80f);
            state = MovementState.walking;
            desiredSpeed = walkSpeed;
        }
        else
        {
            state = MovementState.air;
        }
    }

    private void UpdateHorizontalVelocity()
    {
        Vector3 inputDir = (orientation.right * horizontalInput + orientation.forward * verticalInput).normalized;

        if (grounded && OnSlope())
            inputDir = GetSlopeMoveDirection(inputDir);

        if (!grounded)
        {
            UpdateAirVelocity(inputDir);
            return;
        }

        if (inputDir.sqrMagnitude < 0.0001f)
        {
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, groundStopDeceleration * Time.deltaTime);
            return;
        }

        float currentSpeed = horizontalVelocity.magnitude;
        Vector3 currentDir = currentSpeed > 0.01f ? horizontalVelocity / currentSpeed : inputDir;

        Vector3 newDir = Vector3.Slerp(currentDir, inputDir, groundTurnRate * Time.deltaTime).normalized;

        float rate = desiredSpeed > currentSpeed ? groundAcceleration : groundDeceleration;
        float newSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, rate * Time.deltaTime);

        horizontalVelocity = newDir * newSpeed;
    }

    private void UpdateAirVelocity(Vector3 inputDir)
    {
        if (inputDir.sqrMagnitude <= 0.0001f)
            return;

        float currentSpeed = horizontalVelocity.magnitude;
        Vector3 currentDir = currentSpeed > 0.01f ? horizontalVelocity / currentSpeed : inputDir;

        Vector3 newDir = Vector3.Slerp(currentDir, inputDir, airTurnRate * Time.deltaTime).normalized;

        float newSpeed = currentSpeed < desiredSpeed
            ? Mathf.MoveTowards(currentSpeed, desiredSpeed, airAcceleration * Time.deltaTime)
            : currentSpeed;

        horizontalVelocity = newDir * newSpeed;
    }

    private void UpdateVerticalVelocity()
    {
        if (wallRunning) return;

        if (grounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity += gravity * Time.deltaTime;
    }

    private void ApplyHeight()
    {
        float crouchHeight = standingHeight * crouchHeightScale;
        float targetHeight = requestedHeight >= 0f ? requestedHeight : (crouching ? crouchHeight : standingHeight);

        if (targetHeight > controller.height && !CanStandUp(targetHeight))
            targetHeight = controller.height;

        float newHeight = Mathf.MoveTowards(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        float diff = newHeight - controller.height;

        controller.height = newHeight;
        controller.center += Vector3.up * (diff * 0.5f);

        requestedHeight = -1f;
    }

    private bool CanStandUp(float targetHeight)
    {
        float diff = targetHeight - controller.height;
        if (diff <= 0.01f) return true;

        Vector3 origin = transform.position + controller.center + Vector3.up * (controller.height * 0.5f);
        return !Physics.Raycast(origin, Vector3.up, diff + 0.05f, ceilingMask);
    }

    public void RequestHeight(float height)
    {
        requestedHeight = height;
    }

    public void RefreshJumps()
    {
        jumpsRemaining = maxJumps;
    }

    public void AddExternalVelocity(Vector3 v)
    {
        if (v.sqrMagnitude > 0.0001f)
        {
            Vector3 dir = v.normalized;
            float oppositeComponent = Vector3.Dot(externalVelocity, dir);

            if (oppositeComponent < 0f)
                externalVelocity -= dir * oppositeComponent;
        }

        externalVelocity += v;

        if (externalVelocity.magnitude > maxExternalSpeed)
            externalVelocity = externalVelocity.normalized * maxExternalSpeed;
    }

    public void DampExternalVelocityAlongNormal(Vector3 normal, float maxDelta = Mathf.Infinity)
    {
        float outward = Vector3.Dot(externalVelocity, normal);
        if (outward > 0f)
            externalVelocity -= normal * Mathf.Min(outward, maxDelta);
    }

    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, controller.height * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0f;
        }

        return false;
    }

    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }
}