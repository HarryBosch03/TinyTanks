using UnityEngine;

namespace TinyTanks.TerrainGeneration
{
    [RequireComponent(typeof(Terrain))]
    public class TerrainGenerator : MonoBehaviour
    {
        public Terrain terrain;

        private float[,] heights = new float[0,0];

        private void Awake()
        {
            terrain = GetComponent<Terrain>();
        }

        public void Generate()
        {
            var resolution = terrain.terrainData.heightmapResolution;
            
            heights = new float[resolution, resolution];
            var layers = GetComponentsInChildren<TerrainLayer>();
            foreach (var layer in layers)
            {
                if (layer.isActiveAndEnabled)
                    layer.Apply(heights);
            }

            for (var x = 0; x < heights.GetLength(0); x++)
            for  (var y = 0; y < heights.GetLength(1); y++)
            {
                heights[x, y] /= terrain.terrainData.size.y;
            }

            terrain.terrainData.SetHeightsDelayLOD(0, 0, heights);
            terrain.terrainData.SyncHeightmap();
        }

        private void OnValidate()
        {
            terrain = GetComponent<Terrain>();
        }

        private void OnDrawGizmosSelected()
        {
            //var size = terrain.terrainData.size;
            //var width = heights.GetLength(0);
            //var height = heights.GetLength(1);
            //var cellSize = new Vector2(size.x / width, size.z / height);
            //
            //for (var x = 0; x < width; x++)
            //for (var y = 0; y < height; y++)
            //{
            //    var value = heights[x, y];
            //    if (!float.IsFinite(value)) return;
            //    
            //    var point = Vector3.Scale(new Vector3(x / (float)width, value, y / (float)height), size);
            //    Gizmos.DrawCube(new Vector3(point.x, point.y / 2f, point.z), new Vector3(cellSize.x, point.y, cellSize.y));
            //}
        }
    }
}
