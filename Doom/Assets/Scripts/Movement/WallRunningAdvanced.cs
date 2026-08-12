using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallRunningAdvanced : MonoBehaviour
{
    [Header("Wallrunning")]
    public LayerMask whatIsWall;
    public LayerMask whatIsGround;
    public float wallRunForce;
    public float wallJumpUpForce;
    public float wallJumpSideForce;
    public float wallClimbSpeed;
    public float maxWallRunTime;
    private float wallRunTimer;

    public float maxWallJumpSpeed = 25f;

    [Header("Câmera")]
    public float wallRunCameraOffset = 0.15f;

    [Header("Input")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode upwardsRunKey = KeyCode.LeftShift;
    public KeyCode downwardsRunKey = KeyCode.LeftControl;
    private bool upwardsRunning;
    private bool downwardsRunning;
    private float horizontalInput;
    private float verticalInput;

    [Header("Detection")]
    public float wallCheckDistance;
    public float minJumpHeight;
    private RaycastHit leftWallhit;
    private RaycastHit rightWallhit;
    private bool wallLeft;
    private bool wallRight;

    [Header("Exiting")]
    private bool exitingWall;
    public float exitWallTime;
    private float exitWallTimer;

    [Header("Wall Switching")]
    public float wallSwitchDotThreshold = 0.7f;
    private Vector3 lastWallNormal;

    [Header("Gravity")]
    public bool useGravity;
    public float gravityCounterForce;

    [Header("References")]
    public Transform orientation;
    public PlayerCam cam;
    private PlayerMovementAdvanced pm;
    private Rigidbody rb;

    [Header("UI")]
    public HandUIController handUI;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovementAdvanced>();
    }

    private void Update()
    {
        CheckForWall();
        StateMachine();
    }

    private void FixedUpdate()
    {
        if (pm.wallRunning)
            WallRunningMovement();
    }

    private void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallhit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallhit, wallCheckDistance, whatIsWall);
    }

    private bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround);
    }

    private void StateMachine()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        upwardsRunning = Input.GetKey(upwardsRunKey);
        downwardsRunning = Input.GetKey(downwardsRunKey);

        if (exitingWall && (wallLeft || wallRight))
        {
            Vector3 currentWallNormal = wallRight ? rightWallhit.normal : leftWallhit.normal;

            if (Vector3.Dot(currentWallNormal, lastWallNormal) < wallSwitchDotThreshold)
            {
                exitingWall = false;
                exitWallTimer = 0f;
            }
        }

        if((wallLeft || wallRight) && verticalInput > 0 && AboveGround() && !exitingWall)
        {
            if (!pm.wallRunning)
                StartWallRun();
            else
                UpdateHandSide();

            if (wallRunTimer > 0)
                wallRunTimer -= Time.deltaTime;

            if(wallRunTimer <= 0 && pm.wallRunning)
            {
                exitingWall = true;
                exitWallTimer = exitWallTime;
            }

            if (Input.GetKeyDown(jumpKey)) WallJump();
        }

        else if (exitingWall)
        {
            if (pm.wallRunning)
                StopWallRun();

            if (exitWallTimer > 0)
                exitWallTimer -= Time.deltaTime;

            if (exitWallTimer <= 0)
                exitingWall = false;
        }

        else
        {
            if (pm.wallRunning)
                StopWallRun();
        }
    }

    private void StartWallRun()
    {
        pm.wallRunning = true;

        wallRunTimer = maxWallRunTime;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        UpdateHandSide();

        cam.DoFov(90f);
        if (wallLeft)
        {
            cam.DoTilt(-5f);
            cam.DoWallRunOffset(leftWallhit.normal, wallRunCameraOffset);
        }
        if (wallRight)
        {
            cam.DoTilt(5f);
            cam.DoWallRunOffset(rightWallhit.normal, wallRunCameraOffset);
        }
    }

    private void WallRunningMovement()
    {
        rb.useGravity = useGravity;

        Vector3 wallNormal = wallRight ? rightWallhit.normal : leftWallhit.normal;
        lastWallNormal = wallNormal;

        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
            wallForward = -wallForward;

        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

        if (upwardsRunning)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, wallClimbSpeed, rb.linearVelocity.z);
        if (downwardsRunning)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, -wallClimbSpeed, rb.linearVelocity.z);

        if (!(wallLeft && horizontalInput > 0) && !(wallRight && horizontalInput < 0))
            rb.AddForce(-wallNormal * 100, ForceMode.Force);

        if (useGravity)
            rb.AddForce(transform.up * gravityCounterForce, ForceMode.Force);
    }

    private void StopWallRun()
    {
        pm.wallRunning = false;

        if (handUI != null)
            handUI.SetWallrun(HandUIController.WallSide.None);

        cam.DoFov(80f);
        cam.DoTilt(0f);
        cam.ResetWallRunOffset();
    }

    private void WallJump()
    {
        StopWallRun();

        exitingWall = true;
        exitWallTimer = exitWallTime;

        pm.wallRunning = false;

        Vector3 wallNormal = wallRight ? rightWallhit.normal : leftWallhit.normal;
        lastWallNormal = wallNormal;

        Vector3 forceToApply = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);

        Vector3 flatVelBeforeJump = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 flatSideForce = new Vector3(wallNormal.x, 0f, wallNormal.z) * wallJumpSideForce;
        Vector3 expectedFlatVelAfterJump = flatVelBeforeJump + flatSideForce / rb.mass;

        float cappedMomentum = Mathf.Min(expectedFlatVelAfterJump.magnitude, maxWallJumpSpeed);

        pm.MarkWallJumping(exitWallTime, cappedMomentum);

        if (handUI != null)
            handUI.SetWallrun(HandUIController.WallSide.None);
    }

    private void UpdateHandSide()
    {
        if (handUI == null) return;

        if (wallRight)
            handUI.SetWallrun(HandUIController.WallSide.Right);
        else if (wallLeft)
            handUI.SetWallrun(HandUIController.WallSide.Left);
    }
}