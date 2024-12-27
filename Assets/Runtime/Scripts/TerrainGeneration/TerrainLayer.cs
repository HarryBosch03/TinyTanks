using UnityEngine;

namespace TinyTanks.TerrainGeneration
{
    public abstract class TerrainLayer : MonoBehaviour
    {
        public abstract void Apply(float[,] heights);
    }
}