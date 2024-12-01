using UnityEngine;

namespace TinyTanks.CDM
{
    public interface ICdmShape
    {
        Bounds bounds { get; }
        bool DoesIntersect(Ray ray, out float enter);
    }
}