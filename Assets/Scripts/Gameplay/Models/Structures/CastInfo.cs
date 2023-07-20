using UnityEngine;

namespace Assets.Scripts.Gameplay.Structures
{
    public struct CastInfo
    {
        public float fraction;
        public Collider collider;
        public Vector3 collisionPoint;
        public Vector3 normal;
        public float distance;
    }
}
