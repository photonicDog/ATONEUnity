﻿using Assets.Scripts.Gameplay.Components;
using Assets.Scripts.Gameplay.Models;
using Assets.Scripts.Gameplay.Models.Configurations;
using UnityEngine;

namespace Assets.Scripts.Gameplay.Controllers
{
    public class PlayerController : MonoBehaviour
    {
        private PlayerConfig _config;
        private CapsuleCollider _collider;
        private InputComponent _input;
        private PhysMoveComponent _physics;

        public CameraController Camera;

        public PlayerModel Data;

        // Start is called before the first frame update
        void Start()
        {
            Data = new PlayerModel();
            _config = new PlayerConfig();
            Cursor.lockState = CursorLockMode.Locked;
            _input = GetComponent<InputComponent>();
            _collider = GetComponent<CapsuleCollider>();
            _physics = new PhysMoveComponent();
            Camera = GameObject.FindGameObjectsWithTag("CameraControl")[0].GetComponent<CameraController>();
            Camera.Player = this;
        }

        void Update()
        {
            _physics.SetPosition(transform.position);
            ///DEBUG TELEPORT
            if (_input.GetAltFire())
            {
                _physics.KillVelocity();
                transform.position = new Vector3(-16.9454803f, 2.74504304f, 101.066254f);
            }

            _physics.ProcessMovement(_collider, _input.GetPlayerMovement(), _config.RunSpeed, Camera.GetLookAtAsVectors(), _input.StartedJumping() || _input.HeldJumping(), _config.JumpHeight, Time.deltaTime);
            transform.position = _physics.GetPosition();
            Data.Speed = _physics.GetSpeed();
        }

        public Vector2 GetInputVector()
        {
            return _input.GetPlayerMovement();
        }
    }
}
