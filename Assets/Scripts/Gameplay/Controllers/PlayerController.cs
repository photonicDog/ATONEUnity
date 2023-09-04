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
        public TetherController TetherBase;

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
            TetherBase = tetherObject.AddComponent<TetherController>();
            TetherBase.Player = this;
            TetherBase.AddPhysMoveComponent(_physics);

            Camera = GameObject.FindGameObjectsWithTag("CameraControl")[0].GetComponent<CameraController>();
            Camera.Player = this;
        }

        void Update()
        {
            _physics.SetPosition(transform.position);

            if (_input.GetAltFire())
            {
                TetherBase.Fire(transform.position, Camera.GetLookAtAsVectors().forward.normalized);
            }

            _physics.UpdateStates(_collider, _input.StartedJumping() || _input.HeldJumping());
            _physics.ToggleBaseVelocity(false);
            _physics.UpdateVelocity(_collider, _input.GetPlayerMovement(), _config.RunSpeed, Camera.GetLookAtAsVectors(), _config.JumpHeight, Time.deltaTime, false);
            TetherBase.AdjustVelocityToTether();
            _physics.ToggleBaseVelocity(true);
            _physics.UpdatePosition(Time.deltaTime, _collider);
            transform.position = _physics.GetPosition();
            TetherBase.transform.position = transform.position;
            Data.Speed = _physics.GetSpeed();
            Data.OnGround = _physics.GetGroundedStatus();
        }

        public Vector2 GetInputVector()
        {
            return _input.GetPlayerMovement();
        }
    }
}
