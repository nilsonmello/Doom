using UnityEngine;

public static class RectIntWorldExtensions
{
    public static void WorldBounds(this RectInt footprint, float cellSize, out Vector2 min, out Vector2 max)
    {
        min = new Vector2(footprint.x * cellSize, footprint.y * cellSize);
        max = new Vector2(
            (footprint.x + footprint.width) * cellSize,
            (footprint.y + footprint.height) * cellSize
        );
    }

    public static Vector3 WorldCenter(this RectInt footprint, float cellSize)
    {
        footprint.WorldBounds(cellSize, out Vector2 min, out Vector2 max);
        return new Vector3((min.x + max.x) / 2f, 0f, (min.y + max.y) / 2f);
    }

    public static bool ContainsWorldPoint(this RectInt footprint, float cellSize, Vector3 worldPos)
    {
        footprint.WorldBounds(cellSize, out Vector2 min, out Vector2 max);
        return worldPos.x >= min.x && worldPos.x <= max.x && worldPos.z >= min.y && worldPos.z <= max.y;
    }
}
