using Assets.Scripts.Gameplay.Models.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.Gameplay.Models
{
    public class PhysicsEntity
    {
        public PhysicsEntity(PhysicsConfig config)
        {
            _clipPlanes = new Vector3[config.MaxClipPlanes];
        }

        private Vector3 _velocity;
        private Vector3 _wishVelocity;
        private Vector3 _position;
        private Vector2 _lookDirection;
        private GameObject _groundObject;
        private Vector3 _groundNormal;
        private float _surfaceFriction;
        private Vector3[] _clipPlanes;
        private Collider[] _colliders;
        private Vector3 _slideDirection;
        private float _currentSlideSpeed;

        private bool _isJumping;
        private bool _isGrounded;
        private bool _isSliding;

        public Vector3 Position { get => _position; set => _position = value; }

        public Vector3 Velocity { get => _velocity; set => _velocity = value; }

        public float Speed => _velocity.magnitude;

        public Vector3 WishVelocity { get => _wishVelocity; set => _wishVelocity = value; }

        public Vector3 WishDirection => _wishVelocity.normalized;

        public float WishSpeed => _wishVelocity.magnitude;

        public Vector2 LookDirection { get => _lookDirection; set => _lookDirection = value; }

        public GameObject Ground { get => _groundObject; set => _groundObject = value; }

        public Vector3 GroundNormal { get => _groundNormal; set => _groundNormal = value; }

        public float SurfaceFriction { get => _surfaceFriction; set => _surfaceFriction = value; }

        public bool IsGrounded { get => _isGrounded; set => _isGrounded = value; }
        public bool IsSliding { get => _isSliding; set => _isSliding = value; }
        public bool IsJumping { get => _isJumping; set => _isJumping = value; }

        public Vector3[] ClipPlanes { get => _clipPlanes; set => _clipPlanes = value; }
        public Collider[] Collisions { get => _colliders; set => _colliders = value; }
    
        public Vector3 SlideDirection { get => _slideDirection; set => _slideDirection = value; }
        public float CurrentSlideSpeed { get => _currentSlideSpeed; set => _currentSlideSpeed = value;}
    
    }
}
