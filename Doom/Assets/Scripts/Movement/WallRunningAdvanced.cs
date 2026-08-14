using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallRunningAdvanced : MonoBehaviour
{
    [Header("Wallrunning")]
    public LayerMask whatIsWall;
    public LayerMask whatIsGround;
    public float wallRunAcceleration = 40f;
    public float wallRunTurnRate = 8f;
    public float wallRunGravity = -1.5f;
    public float wallJumpUpForce = 9f;
    public float wallJumpSideForce = 9f;
    public float wallClimbSpeed = 4f;
    public float maxWallRunTime = 1.5f;
    private float wallRunTimer;

    [Header("Controle Direcional")]
    [Range(0f, 1f)]
    public float wallRunSteerInfluence = 0.4f;
    public float wallStickForce = 12f;

    [Header("Wall Jump")]
    [Range(0f, 1f)]
    public float wallJumpLookInfluence = 0.5f;

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

    [Header("References")]
    public Transform orientation;
    public PlayerCam cam;
    private PlayerMovementAdvanced pm;

    [Header("UI")]
    public HandUIController handUI;

    private void Start()
    {
        pm = GetComponent<PlayerMovementAdvanced>();
    }

    private void Update()
    {
        CheckForWall();
        StateMachine();

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

        if ((wallLeft || wallRight) && verticalInput > 0 && AboveGround() && !exitingWall)
        {
            if (!pm.wallRunning)
                StartWallRun();
            else
                UpdateHandSide();

            if (wallRunTimer > 0)
                wallRunTimer -= Time.deltaTime;

            if (wallRunTimer <= 0 && pm.wallRunning)
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
        pm.verticalVelocity = 0f;

        pm.RefreshJumps();

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
        Vector3 wallNormal = wallRight ? rightWallhit.normal : leftWallhit.normal;
        lastWallNormal = wallNormal;

        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
        if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
            wallForward = -wallForward;

        Vector3 inputDir = orientation.right * horizontalInput + orientation.forward * verticalInput;
        Vector3 steerTarget = inputDir.sqrMagnitude > 0.01f ? inputDir.normalized : wallForward;
        Vector3 wishDir = Vector3.Lerp(wallForward, steerTarget, wallRunSteerInfluence);

        wishDir = Vector3.ProjectOnPlane(wishDir, wallNormal).normalized;

        float currentSpeed = pm.horizontalVelocity.magnitude;
        Vector3 currentDir = currentSpeed > 0.01f ? pm.horizontalVelocity / currentSpeed : wishDir;

        Vector3 newDir = Vector3.Slerp(currentDir, wishDir, wallRunTurnRate * Time.deltaTime).normalized;

        float newSpeed = currentSpeed < pm.wallRunSpeed
            ? Mathf.MoveTowards(currentSpeed, pm.wallRunSpeed, wallRunAcceleration * Time.deltaTime)
            : currentSpeed;

        pm.horizontalVelocity = newDir * newSpeed;

        float outwardSpeed = Vector3.Dot(pm.horizontalVelocity, wallNormal);
        if (outwardSpeed > 0f)
            pm.horizontalVelocity -= wallNormal * Mathf.Min(outwardSpeed, wallStickForce * Time.deltaTime);

        if (upwardsRunning)
            pm.verticalVelocity = wallClimbSpeed;
        else if (downwardsRunning)
            pm.verticalVelocity = -wallClimbSpeed;
        else
            pm.verticalVelocity += wallRunGravity * Time.deltaTime;
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

        pm.suppressNextJumpInput = true;

        exitingWall = true;
        exitWallTimer = exitWallTime;

        Vector3 wallNormal = wallRight ? rightWallhit.normal : leftWallhit.normal;
        lastWallNormal = wallNormal;

        Vector3 wallNormalFlat = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;
        Vector3 lookFlat = new Vector3(orientation.forward.x, 0f, orientation.forward.z);
        Vector3 kickDir = lookFlat.sqrMagnitude > 0.001f
            ? Vector3.Slerp(wallNormalFlat, lookFlat.normalized, wallJumpLookInfluence).normalized
            : wallNormalFlat;

        pm.AddExternalVelocity(kickDir * wallJumpSideForce);
        pm.verticalVelocity = wallJumpUpForce;
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