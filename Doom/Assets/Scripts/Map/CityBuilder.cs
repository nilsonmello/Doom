using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

[RequireComponent(typeof(CityGenerator))]
public class CityBuilder : MonoBehaviour
{
    [Header("Referências")]
    public GameObject buildingPrefab;
    public GameObject groundPrefab;
    public GameObject playerPrefab;

    [Header("Inimigos")]
    public GameObject[] enemyPrefabs;
    public int enemyCount = 15;
    public float enemyMinDistanceFromSpawn = 20f;
    public float enemySpawnHeightOffset = 0.1f;

    [Header("Dimensões")]
    public float cellSize = 4f;

    [Header("Seed")]
    [SerializeField] private int seed = 0;
    [SerializeField] private bool useFixedSeed = false;

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

        System.Random rng = ProceduralUtils.CreateRng(ref seed, useFixedSeed);

        buildings = generator.Generate(seed);

        BuildCity();
        BakeNavMesh();
        SpawnPlayer();
        SpawnEnemies(rng);
    }

    private void BakeNavMesh()
    {
        if (navMeshSurface == null)
        {
            return;
        }

        navMeshSurface.BuildNavMesh();
    }

    private void BuildCity()
    {
        if (mapParent == null)
        {
            mapParent = new GameObject("GeneratedCity").transform;
            mapParent.SetParent(transform);
        }
        else
        {

            ProceduralUtils.ClearChildren(mapParent);
        }

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
            Vector3 center = lot.footprint.WorldCenter(cellSize);
            Vector3 pos = new Vector3(center.x, lot.height / 2f, center.z);

            var building = Instantiate(buildingPrefab, pos, Quaternion.identity, mapParent);
            building.transform.localScale = new Vector3(
                lot.footprint.width * cellSize,
                lot.height,
                lot.footprint.height * cellSize
            );
        }
    }

    private void ScaleToFit(GameObject obj, float targetWidth, float targetDepth)
    {
        var meshFilter = obj.GetComponentInChildren<MeshFilter>();

        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return;
        }

        Vector3 meshSize = meshFilter.sharedMesh.bounds.size;

        float scaleX = meshSize.x > 0f ? targetWidth / meshSize.x : 1f;
        float scaleZ = meshSize.z > 0f ? targetDepth / meshSize.z : 1f;

        obj.transform.localScale = new Vector3(scaleX, obj.transform.localScale.y, scaleZ);
    }

    private void SpawnPlayer()
    {
        if (playerPrefab == null) return;

        Vector3 pos;

        if (generator.TryGetPlazaFootprint(out RectInt plaza))
        {
            Vector3 c = plaza.WorldCenter(cellSize);
            pos = new Vector3(c.x, 1f, c.z);
        }
        else
        {
            pos = new Vector3(generator.mapWidth * cellSize / 2f, 1f, generator.mapHeight * cellSize / 2f);
        }

        spawnWorldPos = pos;

        var player = Instantiate(playerPrefab, pos, Quaternion.identity);
        playerInstance = player.transform;
    }

    private void SpawnEnemies(System.Random rng)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            return;
        }
        if (playerInstance == null)
        {
            return;
        }

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = enemyCount * 30;

        int rejeitadosPorDistancia = 0;
        int rejeitadosPorPredio = 0;
        int rejeitadosPorNavMesh = 0;

        while (spawned < enemyCount && attempts < maxAttempts)
        {
            attempts++;

            float rx = (float)rng.NextDouble() * generator.mapWidth * cellSize;
            float rz = (float)rng.NextDouble() * generator.mapHeight * cellSize;
            Vector3 candidate = new Vector3(rx, 1f, rz);

            if (Vector3.Distance(candidate, spawnWorldPos) < enemyMinDistanceFromSpawn)
            {
                rejeitadosPorDistancia++;
                continue;
            }
            if (IsInsideAnyBuilding(candidate))
            {
                rejeitadosPorPredio++;
                continue;
            }

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 1f, NavMesh.AllAreas))
            {
                rejeitadosPorNavMesh++;
                continue;
            }

            GameObject enemyObj = ProceduralUtils.SpawnRandom(
                enemyPrefabs, navHit.position, Quaternion.identity, mapParent, rng,
                out _);

            if (enemyObj == null) continue;

            NavMeshAgent enemyAgent = enemyObj.GetComponentInChildren<NavMeshAgent>();
            if (enemyAgent != null)
            {
                enemyAgent.baseOffset = enemySpawnHeightOffset;
            }

            var enemyAI = enemyObj.GetComponentInChildren<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.player = playerInstance;
            }

            spawned++;
        }
    }

    private bool IsInsideAnyBuilding(Vector3 worldPos)
    {
        foreach (var lot in buildings)
        {
            if (lot.footprint.ContainsWorldPoint(cellSize, worldPos))
                return true;
        }
        return false;
    }
}