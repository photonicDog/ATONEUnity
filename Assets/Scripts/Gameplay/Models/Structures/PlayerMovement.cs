using Assets.Scripts.Gameplay.Interfaces;
using UnityEngine;

namespace Assets.Scripts.Gameplay.Structures
{
    internal struct PlayerMovement : IMovement
    {
        private Vector3 _velocity;
        private Vector3 _wishVelocity;
        private bool _grounded;
        private bool _jumpFinished;
        public Vector3 Velocity { get => _velocity; set => _velocity = value; }

        public float Speed => _velocity.magnitude;

        public Vector3 WishVelocity { get => _wishVelocity; set => _wishVelocity = value; }

        public Vector3 WishDirection => _wishVelocity.normalized;

        public float WishSpeed => _wishVelocity.magnitude;

        public bool IsGrounded { get => _grounded; set => _grounded = value; }
    }
}
