using UnityEngine;

[ExecuteAlways]
public class AutoTileTexture : MonoBehaviour
{
    public float textureWorldSize = 1f;

    void OnEnable() => UpdateTiling();

    public void UpdateTiling()
    {
        Renderer rend = GetComponent<Renderer>();
        Vector3 scale = transform.localScale;

        Vector2 tiling = new Vector2(scale.x / textureWorldSize, scale.z / textureWorldSize);
        rend.sharedMaterial.mainTextureScale = tiling;
    }
}