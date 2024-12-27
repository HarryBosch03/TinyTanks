using System;
using TMPro;
using UnityEngine;

namespace TinyTanks.TerrainGeneration
{
    public class NoiseTL : TerrainLayer
    {
        public float amplitude;
        public float frequency = 1f;
        public int octaves = 1;
        public float persistence = 0.5f;
        public float lacunarity = 2f;
        public bool normalize;
        
        public override void Apply(float[,] heights)
        {
            var div = 2f - Mathf.Pow(2f, 1f - octaves);
            var min = float.MaxValue;
            var max = float.MinValue;
            
            for (var x = 0; x < heights.GetLength(0); x++)
            for (var y = 0; y < heights.GetLength(1); y++)
            {
                var val = 0f;
                
                for (var i = 0; i < octaves; i++)
                {
                    var frequency = this.frequency * Mathf.Pow(lacunarity, i);
                    var amplitude = this.amplitude * Mathf.Pow(persistence, i);
                    val += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
                }

                val /= div;
                heights[x, y] += val;
                min = Mathf.Min(min, val);
                max = Mathf.Max(max, val);
            }

            if (normalize)
            {
                for (var x = 0; x < heights.GetLength(0); x++)
                for (var y = 0; y < heights.GetLength(1); y++)
                {
                    heights[x, y] = Mathf.Lerp(0f, amplitude, Mathf.InverseLerp(min, max, heights[x, y]));
                }
            }
        }

        private void OnValidate()
        {
            frequency = Mathf.Max(0f, frequency);
            octaves = Mathf.Max(1, octaves);
            persistence = Mathf.Max(0f, persistence);
            lacunarity = Mathf.Max(0f, lacunarity);
        }
    }
}