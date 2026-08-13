using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;

[RequireComponent(typeof(CityGenerator))]
public class CityBuilder : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Prefab de cubo simples (pivot no centro). Vai ser escalado pra virar cada prédio.")]
    public GameObject buildingPrefab;
    public GameObject groundPrefab;
    public GameObject playerPrefab;

    [Header("Inimigos")]
    public GameObject[] enemyPrefabs;
    public int enemyCount = 15;
    [Tooltip("Distância mínima da praça de spawn onde inimigos podem aparecer.")]
    public float enemyMinDistanceFromSpawn = 20f;

    [Header("Dimensões")]
    public float cellSize = 4f;

    [Header("Seed")]
    public int seed = 0;
    public bool randomSeedOnPlay = true;

    [Header("Navegação")]
    public NavMeshSurface navMeshSurface;

    private CityGenerator generator;
    private List<CityGenerator.BuildingLot> buildings;
    private Transform mapParent;
    private Transform playerInstance;
    private Vector3 spawnWorldPos;

    void Start()
    {
        generator = GetComponent<CityGenerator>();

        if (randomSeedOnPlay) seed = System.Guid.NewGuid().GetHashCode();
        buildings = generator.Generate(seed);

        BuildCity();
        BakeNavMesh();
        SpawnPlayer();
        SpawnEnemies();
    }

    private void BakeNavMesh()
    {
        if (navMeshSurface == null)
        {
            Debug.LogWarning("NavMeshSurface não atribuído");
            return;
        }

        navMeshSurface.BuildNavMesh();
    }

    private void BuildCity()
    {
        if (mapParent != null) Destroy(mapParent.gameObject);
        mapParent = new GameObject("GeneratedCity").transform;
        mapParent.SetParent(transform);

        if (groundPrefab != null)
        {
            var ground = Instantiate(groundPrefab, mapParent);
            ground.transform.position = new Vector3(
                generator.mapWidth * cellSize / 2f,
                0f,
                generator.mapHeight * cellSize / 2f
            );

            ScaleToFit(ground, generator.mapWidth * cellSize, generator.mapHeight * cellSize);
        }

        if (buildingPrefab == null) return;

        foreach (var lot in buildings)
        {
            Vector3 center = FootprintCenter(lot.footprint);
            Vector3 pos = new Vector3(center.x, lot.height / 2f, center.z);

            var building = Instantiate(buildingPrefab, pos, Quaternion.identity, mapParent);
            building.transform.localScale = new Vector3(
                lot.footprint.width * cellSize,
                lot.height,
                lot.footprint.height * cellSize
            );
        }
    }

    /// <summary>
    /// Escala um objeto instanciado (com escala 1,1,1) pra que seu mesh cubra exatamente
    /// targetWidth x targetDepth em unidades de mundo, medindo o tamanho REAL do mesh
    /// em vez de assumir um tamanho fixo (ex: Plane padrão da Unity = 10x10).
    /// Funciona com qualquer prefab de chão: Plane, Quad, Cube, mesh customizado, etc.
    /// </summary>
    private void ScaleToFit(GameObject obj, float targetWidth, float targetDepth)
    {
        var meshFilter = obj.GetComponentInChildren<MeshFilter>();

        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning($"Não encontrei MeshFilter em '{obj.name}' pra calcular a escala do chão automaticamente. Confira se o groundPrefab tem um MeshFilter/MeshRenderer.");
            return;
        }

        Vector3 meshSize = meshFilter.sharedMesh.bounds.size;

        float scaleX = meshSize.x > 0f ? targetWidth / meshSize.x : 1f;
        float scaleZ = meshSize.z > 0f ? targetDepth / meshSize.z : 1f;

        obj.transform.localScale = new Vector3(scaleX, obj.transform.localScale.y, scaleZ);
    }

    private Vector3 FootprintCenter(RectInt footprint)
    {
        return new Vector3(
            (footprint.x + footprint.width / 2f) * cellSize,
            0f,
            (footprint.y + footprint.height / 2f) * cellSize
        );
    }

    private void SpawnPlayer()
    {
        if (playerPrefab == null) return;

        Vector3 pos;

        if (generator.TryGetPlazaFootprint(out RectInt plaza))
        {
            Vector3 c = FootprintCenter(plaza);
            pos = new Vector3(c.x, 1f, c.z);
        }
        else
        {
            // Sem praça válida encontrada (ex: spawnPlazaCount = 0): cai no centro do mapa mesmo.
            pos = new Vector3(generator.mapWidth * cellSize / 2f, 1f, generator.mapHeight * cellSize / 2f);
        }

        spawnWorldPos = pos;

        var player = Instantiate(playerPrefab, pos, Quaternion.identity);
        playerInstance = player.transform;
    }

    private void SpawnEnemies()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;
        if (playerInstance == null) return;

        var enemyRng = new System.Random(seed);
        int spawned = 0;
        int attempts = 0;
        int maxAttempts = enemyCount * 30;

        while (spawned < enemyCount && attempts < maxAttempts)
        {
            attempts++;

            float rx = (float)enemyRng.NextDouble() * generator.mapWidth * cellSize;
            float rz = (float)enemyRng.NextDouble() * generator.mapHeight * cellSize;
            Vector3 candidate = new Vector3(rx, 1f, rz);

            if (Vector3.Distance(candidate, spawnWorldPos) < enemyMinDistanceFromSpawn) continue;
            if (IsInsideAnyBuilding(candidate)) continue;

            var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            var enemyObj = Instantiate(prefab, candidate, Quaternion.identity, mapParent);

            var enemyAI = enemyObj.GetComponent<EnemyAI>();
            if (enemyAI != null)
                enemyAI.player = playerInstance;

            spawned++;
        }
    }

    private bool IsInsideAnyBuilding(Vector3 worldPos)
    {
        foreach (var lot in buildings)
        {
            float minX = lot.footprint.x * cellSize;
            float maxX = (lot.footprint.x + lot.footprint.width) * cellSize;
            float minZ = lot.footprint.y * cellSize;
            float maxZ = (lot.footprint.y + lot.footprint.height) * cellSize;

            if (worldPos.x >= minX && worldPos.x <= maxX && worldPos.z >= minZ && worldPos.z <= maxZ)
                return true;
        }
        return false;
    }
}