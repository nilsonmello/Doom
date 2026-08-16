using System.Collections.Generic;
using UnityEngine;

public class CityGenerator : MonoBehaviour
{
    [Header("Configuração do Mapa")]
    public int mapWidth = 120;
    public int mapHeight = 120;

    [Header("Configuração do BSP")]
    public int minLeafSize = 10;
    public int maxLeafSize = 22;

    [Header("Ruas")]
    public float streetWidth = 3f;
    public int minBuildingFootprint = 4;

    [Header("Altura dos Prédios")]
    public float minBuildingHeight = 6f;
    public float maxBuildingHeight = 40f;

    [Header("Silhueta dos Prédios")]
    [Range(0.1f, 1f)] public float minFootprintRatio = 0.5f;
    [Range(0.1f, 1f)] public float maxFootprintRatio = 0.85f;

    [Header("Muralha de Perímetro")]
    public int perimeterThickness = 14;
    public float perimeterStreetWidth = 1f;
    public float perimeterMinHeight = 70f;
    public float perimeterMaxHeight = 100f;

    [Header("Praça de Spawn")]
    public int spawnPlazaCount = 1;

    public struct BuildingLot
    {
        public RectInt footprint;
        public float height;
        public bool isPerimeter;
    }

    private class Leaf
    {
        public RectInt bounds;
        public Leaf left, right;
        public bool isPlaza;

        public Leaf(RectInt bounds) { this.bounds = bounds; }
        public bool IsLeafNode() => left == null && right == null;
    }

    private List<BuildingLot> buildings = new List<BuildingLot>();
    private RectInt plazaFootprint;
    private bool hasPlaza;
    private System.Random rng;

    public List<BuildingLot> Generate(int seed)
    {
        WarnIfStreetsMightClose();

        rng = new System.Random(seed);
        buildings.Clear();
        hasPlaza = false;

        var root = new Leaf(new RectInt(0, 0, mapWidth, mapHeight));
        SplitLeaves(root);

        var leafNodes = new List<Leaf>();
        CollectLeafNodes(root, leafNodes);

        ChoosePlazas(leafNodes);
        BuildLots(leafNodes);

        return buildings;
    }

    private void SplitLeaves(Leaf leaf)
    {
        if (!leaf.IsLeafNode()) return;

        bool splitH = rng.NextDouble() > 0.5;
        if (leaf.bounds.width > leaf.bounds.height && leaf.bounds.width / (float)leaf.bounds.height >= 1.25f)
            splitH = false;
        else if (leaf.bounds.height > leaf.bounds.width && leaf.bounds.height / (float)leaf.bounds.width >= 1.25f)
            splitH = true;

        int max = (splitH ? leaf.bounds.height : leaf.bounds.width) - minLeafSize;
        if (max <= minLeafSize) return;

        int split = rng.Next(minLeafSize, max);

        if (splitH)
        {
            leaf.left = new Leaf(new RectInt(leaf.bounds.x, leaf.bounds.y, leaf.bounds.width, split));
            leaf.right = new Leaf(new RectInt(leaf.bounds.x, leaf.bounds.y + split, leaf.bounds.width, leaf.bounds.height - split));
        }
        else
        {
            leaf.left = new Leaf(new RectInt(leaf.bounds.x, leaf.bounds.y, split, leaf.bounds.height));
            leaf.right = new Leaf(new RectInt(leaf.bounds.x + split, leaf.bounds.y, leaf.bounds.width - split, leaf.bounds.height));
        }

        if (leaf.left.bounds.width > maxLeafSize || leaf.left.bounds.height > maxLeafSize)
            SplitLeaves(leaf.left);
        if (leaf.right.bounds.width > maxLeafSize || leaf.right.bounds.height > maxLeafSize)
            SplitLeaves(leaf.right);
    }

    private void CollectLeafNodes(Leaf leaf, List<Leaf> result)
    {
        if (leaf.IsLeafNode())
        {
            result.Add(leaf);
            return;
        }
        CollectLeafNodes(leaf.left, result);
        CollectLeafNodes(leaf.right, result);
    }

    private bool IsPerimeterLeaf(Leaf leaf)
    {
        return leaf.bounds.x <= perimeterThickness
            || leaf.bounds.y <= perimeterThickness
            || (leaf.bounds.x + leaf.bounds.width) >= (mapWidth - perimeterThickness)
            || (leaf.bounds.y + leaf.bounds.height) >= (mapHeight - perimeterThickness);
    }

    private void ChoosePlazas(List<Leaf> leafNodes)
    {
        if (spawnPlazaCount <= 0) return;

        Vector2 center = new Vector2(mapWidth / 2f, mapHeight / 2f);

        var candidates = new List<Leaf>(leafNodes);
        candidates.Sort((a, b) =>
            Vector2.Distance(LeafCenter(a), center).CompareTo(Vector2.Distance(LeafCenter(b), center)));

        int chosen = 0;
        foreach (var leaf in candidates)
        {
            if (chosen >= spawnPlazaCount) break;

            if (IsPerimeterLeaf(leaf)) continue;

            leaf.isPlaza = true;

            if (!hasPlaza)
            {
                plazaFootprint = leaf.bounds;
                hasPlaza = true;
            }

            chosen++;
        }
    }

    private Vector2 LeafCenter(Leaf leaf) =>
        new Vector2(leaf.bounds.x + leaf.bounds.width / 2f, leaf.bounds.y + leaf.bounds.height / 2f);

    private void BuildLots(List<Leaf> leafNodes)
    {
        foreach (var leaf in leafNodes)
        {
            if (leaf.isPlaza) continue;

            bool isPerimeter = IsPerimeterLeaf(leaf);

            float street = isPerimeter ? perimeterStreetWidth : streetWidth;
            int padX = Mathf.Max(0, Mathf.RoundToInt(street / 2f));
            int padY = padX;

            int availableW = Mathf.Max(minBuildingFootprint, leaf.bounds.width - padX * 2);
            int availableH = Mathf.Max(minBuildingFootprint, leaf.bounds.height - padY * 2);
            availableW = Mathf.Min(availableW, leaf.bounds.width);
            availableH = Mathf.Min(availableH, leaf.bounds.height);

            float ratioX = (float)(rng.NextDouble() * (maxFootprintRatio - minFootprintRatio) + minFootprintRatio);
            float ratioZ = (float)(rng.NextDouble() * (maxFootprintRatio - minFootprintRatio) + minFootprintRatio);

            int footprintW = Mathf.Clamp(Mathf.RoundToInt(availableW * ratioX), minBuildingFootprint, availableW);
            int footprintH = Mathf.Clamp(Mathf.RoundToInt(availableH * ratioZ), minBuildingFootprint, availableH);

            int fx = leaf.bounds.x + (leaf.bounds.width - footprintW) / 2;
            int fy = leaf.bounds.y + (leaf.bounds.height - footprintH) / 2;

            float height = isPerimeter
                ? (float)(rng.NextDouble() * (perimeterMaxHeight - perimeterMinHeight) + perimeterMinHeight)
                : (float)(rng.NextDouble() * (maxBuildingHeight - minBuildingHeight) + minBuildingHeight);

            buildings.Add(new BuildingLot
            {
                footprint = new RectInt(fx, fy, footprintW, footprintH),
                height = height,
                isPerimeter = isPerimeter
            });
        }
    }

    public bool TryGetPlazaFootprint(out RectInt footprint)
    {
        footprint = plazaFootprint;
        return hasPlaza;
    }

    public List<BuildingLot> GetBuildings() => buildings;

    private void WarnIfStreetsMightClose()
    {
        int recommendedMinLeaf = minBuildingFootprint + Mathf.CeilToInt(streetWidth) + 2;
    }
}
