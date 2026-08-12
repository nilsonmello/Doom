using UnityEngine;

/// <summary>
/// Mantém este transform sempre alinhado com a câmera (posição + rotação),
/// com um offset à frente — útil pra um shootPoint que precisa ficar sempre
/// centralizado na mira, independente de onde ele esteja na hierarquia.
///
/// Roda em LateUpdate pra garantir que já rodou depois do PlayerCam.Update()
/// (que é onde a rotação da câmera é definida a cada frame) — sem isso,
/// dependendo da ordem de execução dos scripts, o shootPoint ficaria sempre
/// um frame atrasado em relação ao olhar do player.
/// </summary>
public class FollowCameraOffset : MonoBehaviour
{
    public Transform cameraTransform;
    public float forwardOffset = 1.2f;

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        transform.position = cameraTransform.position + cameraTransform.forward * forwardOffset;
        transform.rotation = cameraTransform.rotation;
    }
}
