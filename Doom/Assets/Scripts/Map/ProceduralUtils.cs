using UnityEngine;

public static class ProceduralUtils
{
    public static System.Random CreateRng(ref int seed, bool useFixedSeed)
    {
        if (!useFixedSeed)
            seed = System.Guid.NewGuid().GetHashCode();

        return new System.Random(seed);
    }

    public static void ClearChildren(Transform container)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform filho = container.GetChild(i);

            if (Application.isPlaying)
                Object.Destroy(filho.gameObject);
            else
                Object.DestroyImmediate(filho.gameObject);
        }
    }

    public static GameObject SpawnRandom(
        GameObject[] prefabs,
        Vector3 position,
        Quaternion rotation,
        Transform parent,
        System.Random rng,
        out GameObject prefabEscolhido)
    {
        prefabEscolhido = null;

        if (prefabs == null || prefabs.Length == 0 || rng == null)
            return null;

        prefabEscolhido = prefabs[rng.Next(prefabs.Length)];
        return Object.Instantiate(prefabEscolhido, position, rotation, parent);
    }
}
