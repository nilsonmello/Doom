using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementAdvanced : MonoBehaviour
{
    [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float slideSpeed;
    public float wallRunSpeed;

    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;

    public float speedIncreaseMultiplier;
    public float slopeIncreaseMultiplier;

    public float groundDrag;

    public float groundDragRampSpeed = 15f;

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    [Header("Double Jump")]
    public int maxJumps = 2;
    private int jumpsRemaining;

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    public bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    public PlayerCam cam;

    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;

    public MovementState state;
    public enum MovementState
    {
        walking,
        sprinting,
        wallRunning,
        crouching,
        sliding,
        air
    }

    public bool sliding;
    public bool wallRunning;

    [HideInInspector] public bool wallJumping;
    private float wallJumpSpeedCap;

    [Header("Wall Jump Landing Decay")]
    public float wallJumpLandingDecayTime = 0.25f;
    private Coroutine wallJumpLandingDecayCoroutine;
    private bool groundedLastFrame;
    private Coroutine moveSpeedLerpCoroutine;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        readyToJump = true;

        jumpsRemaining = maxJumps;

        startYScale = transform.localScale.y;
    }

    private void Update()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        bool justLanded = grounded && !groundedLastFrame;
        groundedLastFrame = grounded;

        // O reset de jumpsRemaining precisa acontecer só no frame exato da
        // transição pro chão (justLanded), não em todo frame com grounded==true.
        // Um AddForce(Impulse) de pulo só move o Rigidbody de fato no próximo
        // passo de física, então por 1+ frame(s) logo após apertar pulo o
        // raycast de grounded ainda pode continuar batendo no chão — resetando
        // o jumpsRemaining que você acabou de gastar antes mesmo de sair do
        // chão, e dando um pulo "extra" fantasma.
        if (justLanded)
        {
            jumpsRemaining = maxJumps;

            if (wallJumping)
                StartWallJumpLandingDecay();
        }

        MyInput();

        StateHandler();
        SpeedControl();

        float targetDrag = grounded ? groundDrag : 0f;
        rb.linearDamping = Mathf.MoveTowards(rb.linearDamping, targetDrag, groundDragRampSpeed * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(jumpKey) && readyToJump && jumpsRemaining > 0 && !wallRunning)
        {
            readyToJump = false;
            jumpsRemaining--;

            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }

        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }
    }

    private void StateHandler()
    {
        if (wallRunning)
        {
            state = MovementState.wallRunning;
            desiredMoveSpeed = wallRunSpeed;
        }

        else if (sliding)
        {
            state = MovementState.sliding;

            if (OnSlope() && rb.linearVelocity.y < 0.1f)
                desiredMoveSpeed = slideSpeed;

            else
                desiredMoveSpeed = sprintSpeed;
        }

        else if (Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            desiredMoveSpeed = crouchSpeed;
        }

        else if(grounded && !wallRunning && Input.GetKey(sprintKey))
        {
            cam.DoFov(90f);
            state = MovementState.sprinting;
            desiredMoveSpeed = sprintSpeed;
        }

        else if (grounded)
        {
            cam.DoFov(80f);
            state = MovementState.walking;
            desiredMoveSpeed = walkSpeed;
        }

        else
        {
            state = MovementState.air;
        }

        if(Mathf.Abs(desiredMoveSpeed - lastDesiredMoveSpeed) > 4f && moveSpeed != 0)
        {
            if (moveSpeedLerpCoroutine != null)
                StopCoroutine(moveSpeedLerpCoroutine);

            moveSpeedLerpCoroutine = StartCoroutine(SmoothlyLerpMoveSpeed());
        }
        else
        {
            moveSpeed = desiredMoveSpeed;
        }

        lastDesiredMoveSpeed = desiredMoveSpeed;
    }

    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;

        while (time < difference)
        {
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);

            if (OnSlope())
            {
                float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f);

                time += Time.deltaTime * speedIncreaseMultiplier * slopeIncreaseMultiplier * slopeAngleIncrease;
            }
            else
                time += Time.deltaTime * speedIncreaseMultiplier;

            yield return null;
        }

        moveSpeed = desiredMoveSpeed;
    }

    private void MovePlayer()
    {
        if (wallRunning)
            return;

        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * moveSpeed * 20f, ForceMode.Force);

            if (rb.linearVelocity.y > 0)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }

        else if(grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);

        else if(!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        rb.useGravity = !OnSlope();
    }

    private void SpeedControl()
    {
        if (OnSlope() && !exitingSlope)
        {
            if (rb.linearVelocity.magnitude > moveSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * moveSpeed;
        }
        else
        {
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            float speedCap = wallJumping ? Mathf.Max(moveSpeed, wallJumpSpeedCap) : moveSpeed;

            if (flatVel.magnitude > speedCap)
            {
                Vector3 limitedVel = flatVel.normalized * speedCap;
                rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
            }
        }
    }

    private void Jump()
    {
        exitingSlope = true;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    private void ResetJump()
    {
        readyToJump = true;

        exitingSlope = false;
    }

    /// <summary>
    /// Restaura os pulos disponíveis (incluindo o pulo duplo) como se o player
    /// tivesse tocado o chão. Chamado pelo WallRunningAdvanced ao entrar em um
    /// wall run, já que pousar numa parede pra correr nela deve "resetar" o
    /// aéreo do mesmo jeito que pousar no chão reseta.
    /// </summary>
    public void RefreshJumps()
    {
        jumpsRemaining = maxJumps;
    }

    public void MarkWallJumping(float momentumCap)
    {
        if (wallJumpLandingDecayCoroutine != null)
        {
            StopCoroutine(wallJumpLandingDecayCoroutine);
            wallJumpLandingDecayCoroutine = null;
        }

        wallJumping = true;
        wallJumpSpeedCap = momentumCap;
    }

    private void StartWallJumpLandingDecay()
    {
        if (wallJumpLandingDecayCoroutine != null)
            StopCoroutine(wallJumpLandingDecayCoroutine);

        wallJumpLandingDecayCoroutine = StartCoroutine(WallJumpLandingDecay());
    }

    private IEnumerator WallJumpLandingDecay()
    {
        float startCap = wallJumpSpeedCap;
        float time = 0f;

        while (time < wallJumpLandingDecayTime)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / wallJumpLandingDecayTime);

            wallJumpSpeedCap = Mathf.Lerp(startCap, moveSpeed, t);

            yield return null;
        }

        wallJumping = false;
        wallJumpLandingDecayCoroutine = null;
    }

    public bool OnSlope()
    {
        if(Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }
}