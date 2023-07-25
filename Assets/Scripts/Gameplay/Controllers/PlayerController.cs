using Assets.Scripts.Gameplay.Components;
using Assets.Scripts.Gameplay.Interfaces;
using Assets.Scripts.Gameplay.Models.Configurations;
using UnityEngine;

namespace Assets.Scripts.Gameplay.Controllers
{
    public class PlayerController : MonoBehaviour
    {
        private PlayerConfig _config;
        private CapsuleCollider _collider;
        private InputComponent _input;
        private PhysicsComponent _physics;
        private CameraController _camera;

        // Start is called before the first frame update
        void Start()
        {
            _config = new PlayerConfig();
            Cursor.lockState = CursorLockMode.Locked;
            _collider = GetComponent<CapsuleCollider>();
            _input = new InputComponent();
            _physics = new PhysicsComponent();
            _camera = Camera.main.GetComponent<CameraController>();
        }

        void Update()
        {
            ///DEBUG TELEPORT
            if (_input.GetAltFire())
            {
                _physics.KillVelocity();
                transform.position = new Vector3(-16.9454803f, 2.74504304f, 101.066254f);
            }

            _collider.transform.position = _physics.ProcessMovement(_collider, _input.GetPlayerMovement(), _config.RunSpeed, _camera.GetLookAtAsVectors(), _input.StartedJumping() || _input.HeldJumping(), _config.JumpHeight, Time.deltaTime);
        }
    }
}
