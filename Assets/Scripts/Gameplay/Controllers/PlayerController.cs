using Assets.Scripts.Gameplay.Components;
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
        public TetherController Tether;

        public PlayerModel Data;

        // Start is called before the first frame update
        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;

            _config = new PlayerConfig();
            _input = GetComponent<InputComponent>();
            _collider = GetComponent<CapsuleCollider>();
            _physics = new PhysMoveComponent();

            Data = new PlayerModel();

            var tetherObject = new GameObject("Tether");
            tetherObject.transform.parent = transform;
            tetherObject.transform.localPosition = Vector3.zero;
            Tether = tetherObject.AddComponent<TetherController>();
            Tether.Player = this;

            Camera = GameObject.FindGameObjectsWithTag("CameraControl")[0].GetComponent<CameraController>();
            Camera.Player = this;
        }

        void Update()
        {
            _physics.SetPosition(transform.position);

            if (_input.GetAltFire())
            {
                Tether.Fire(transform.position, Camera.GetLookAtAsVectors().forward.normalized);
            }

            _physics.ProcessMovement(_collider, _input.GetPlayerMovement(), _config.RunSpeed, Camera.GetLookAtAsVectors(), _input.StartedJumping() || _input.HeldJumping(), _config.JumpHeight, Time.deltaTime);
            transform.position = _physics.GetPosition();
            Tether.transform.position = transform.position;
            Data.Speed = _physics.GetSpeed();
            Data.OnGround = _physics.GetGroundedStatus();
        }

        public Vector2 GetInputVector()
        {
            return _input.GetPlayerMovement();
        }
    }
}
