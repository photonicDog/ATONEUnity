using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Gameplay.Input
{
    public class InputManager : MonoBehaviour
    {
        private GameplayControls gameplayControls;
        private static InputManager _instance;
        public static InputManager Instance { get { return _instance; } }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            } else
            {
                _instance = this;
            }
            gameplayControls = new GameplayControls();
        }

        private void OnEnable()
        {
            gameplayControls.Enable();
        }

        private void OnDisable()
        {
            gameplayControls.Disable();
        }

        public Vector2 GetPlayerMovement()
        {
            return gameplayControls.Gameplay.Move.ReadValue<Vector2>();
        }

        public Vector2 GetMouseMovement()
        {
            return gameplayControls.Gameplay.CameraControl.ReadValue<Vector2>();
        }

        public bool StartedJumping()
        {
            return gameplayControls.Gameplay.Jump.triggered;
        }

        public bool StoppedJumping()
        {
            return gameplayControls.Gameplay.Jump.phase == UnityEngine.InputSystem.InputActionPhase.Canceled ||
                gameplayControls.Gameplay.Jump.phase == UnityEngine.InputSystem.InputActionPhase.Waiting;
        }

        public bool GetAltFire()
        {
            return gameplayControls.Gameplay.AltFire.triggered;
        }
    }
}