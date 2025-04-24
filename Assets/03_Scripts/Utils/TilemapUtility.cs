using UnityEngine;
using UnityEngine.Tilemaps;

namespace Utils
{
    /// <summary>
    /// Provides utility methods for working with tilemaps.
    /// </summary>
    public static class TilemapUtility
    {
        /// <summary>
        /// Returns the combined size in world units of all Tilemaps under the given GameObject.
        /// </summary>
        /// <param name="parent">The GameObject whose child Tilemaps will be combined.</param>
        public static Vector2 GetCombinedTilemapSize(GameObject parent)
        {
            var allTilemaps = parent.GetComponentsInChildren<Tilemap>();
            if (allTilemaps == null || allTilemaps.Length == 0)
                return Vector2.zero;

            Vector3Int minCell = new Vector3Int(int.MaxValue, int.MaxValue, 0);
            Vector3Int maxCell = new Vector3Int(int.MinValue, int.MinValue, 0);

            foreach (var tilemap in allTilemaps)
            {
                var bounds = tilemap.cellBounds;
                for (int x = bounds.min.x; x < bounds.max.x; x++)
                {
                    for (int y = bounds.min.y; y < bounds.max.y; y++)
                    {
                        var cellPos = new Vector3Int(x, y, 0);
                        if (tilemap.HasTile(cellPos))
                        {
                            if (x < minCell.x) minCell.x = x;
                            if (y < minCell.y) minCell.y = y;
                            if (x > maxCell.x) maxCell.x = x;
                            if (y > maxCell.y) maxCell.y = y;
                        }
                    }
                }
            }

            if (minCell.x == int.MaxValue)
                return Vector2.zero;

            int widthInTiles = maxCell.x - minCell.x + 1;
            int heightInTiles = maxCell.y - minCell.y + 1;

            var cellSize = allTilemaps[0].cellSize;
            return new Vector2(widthInTiles * cellSize.x, heightInTiles * cellSize.y);
        }
    }
} 