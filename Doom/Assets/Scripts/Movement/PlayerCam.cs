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

    /// <summary>
    /// Tremor de posição (jitter), útil pra impactos/eventos genéricos (ex:
    /// grab, arremesso, hit). Anima a posição local da própria câmera, não a
    /// do camHolder — camHolder.rotation é reescrito todo frame no Update(),
    /// então qualquer tween nela seria anulado no frame seguinte.
    /// "amount" é a força do tremor; ajuste no Inspector do chamador
    /// (ex: grabShakeAmount, throwShakeAmount) até achar o efeito desejado.
    /// </summary>
    public void AddScreenShake(float amount)
    {
        transform.DOShakePosition(0.2f, amount, 10, 90f, false, true);
    }

    /// <summary>
    /// "Chute" de rotação (recoil/kick), útil pra reações direcionais (ex:
    /// agarrar, arremessar). Separado de AddScreenShake (que mexe em
    /// posição) pra não brigarem entre si quando chamados juntos.
    /// "amount" é o ângulo do punch em graus no eixo X; valores pequenos
    /// (ex: 0.05) rendem um kick bem sutil — suba o valor se quiser algo
    /// mais perceptível.
    /// </summary>
    public void AddCameraPunch(float amount)
    {
        transform.DOPunchRotation(new Vector3(amount, 0f, 0f), 0.2f, 10, 1f);
    }
}