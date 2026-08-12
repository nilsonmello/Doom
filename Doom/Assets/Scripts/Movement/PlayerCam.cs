using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerCam : MonoBehaviour
{
    public float sensX;
    public float sensY;

    public Transform orientation;
    public Transform camHolder;

    float xRotation;
    float yRotation;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        yRotation += mouseX;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        camHolder.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }

    public void DoFov(float endValue)
    {
        GetComponent<Camera>().DOFieldOfView(endValue, 0.25f);
    }

    public void DoTilt(float zTilt)
    {
        transform.DOLocalRotate(new Vector3(0, 0, zTilt), 0.25f);
    }

    public void AddScreenShake(float amount)
    {
        transform.DOShakePosition(0.2f, amount, 10, 90f, false, true);
    }

    public void AddCameraPunch(float amount)
    {
        transform.DOPunchRotation(new Vector3(amount, 0f, 0f), 0.2f, 10, 1f);
    }

    public void DoWallRunOffset(Vector3 wallNormalWorld, float offsetAmount)
    {
        Vector3 localOffset = camHolder.InverseTransformDirection(wallNormalWorld) * offsetAmount;
        transform.DOLocalMove(localOffset, 0.25f);
    }

    public void ResetWallRunOffset()
    {
        transform.DOLocalMove(Vector3.zero, 0.25f);
    }
}