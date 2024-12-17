using UnityEngine;

namespace TinyTanks.Utility
{
    public static class Extensions
    {
        public static float NextFloat(this System.Random rng) => rng.NextFloat(0f, 1f);

        public static float NextFloat(this System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);

        public static T IndexWrapped<T>(this T[] array, int i)
        {
            var c = array.Length;
            return array[(i % c + c) % c];
        }
    }
}