using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Gameplay.Components
{
    public class InputComponent : MonoBehaviour
    {
        private GameplayControls _gameplayControls;

        public InputComponent(GameplayControls controls)
        {
        }

        private void Awake()
        {
            _gameplayControls = new GameplayControls();
        }

        private void Update()
        {
        }

        private void OnEnable()
        {
            _gameplayControls.Enable();
        }

        private void OnDisable()
        {
            _gameplayControls.Disable();
        }

        public Vector2 GetPlayerMovement()
        {
            var input = _gameplayControls.Gameplay.Move.ReadValue<Vector2>();
            return new Vector2(input.y, input.x);
        }

        ///NOTE: currently this isn't used for moving the camera in gameplay, because cinemachine uses its 
        ///      own mouse axis check. this is still here for UI and general mouse movement checks - freya
        public Vector2 GetMouseMovement()
        {
            return _gameplayControls.Gameplay.Look.ReadValue<Vector2>();
        }

        public bool StartedJumping()
        {
            return _gameplayControls.Gameplay.Jump.triggered;
        }

        public bool HeldJumping()
        {
            return _gameplayControls.Gameplay.Jump.IsPressed();
        }

        private bool StoppedJumping()
        {
            return _gameplayControls.Gameplay.Jump.phase == InputActionPhase.Canceled ||
                _gameplayControls.Gameplay.Jump.phase == InputActionPhase.Waiting;
        }

        public bool GetFire()
        {
            return _gameplayControls.Gameplay.Fire.triggered;
        }

        public bool GetRelease()
        {
            return _gameplayControls.Gameplay.Fire.phase == InputActionPhase.Canceled ||
                _gameplayControls.Gameplay.Fire.phase == InputActionPhase.Waiting;
        }

        public bool GetAltFire()
        {
            return _gameplayControls.Gameplay.AltFire.triggered;
        }

        public bool GetAltRelease()
        {
            return _gameplayControls.Gameplay.AltFire.phase == UnityEngine.InputSystem.InputActionPhase.Canceled ||
                _gameplayControls.Gameplay.AltFire.phase == UnityEngine.InputSystem.InputActionPhase.Waiting;
        }

        public bool GetPause()
        {
            return _gameplayControls.Gameplay.Pause.triggered;
        }

        public string GetBindingPath(string actionName, int bindingIndex = 0)
        {
            return _gameplayControls.FindAction(actionName).bindings[bindingIndex].effectivePath;
        }

        public void SetBinding(string actionName, string newPath, int bindingIndex = 0)
        {
            _gameplayControls.FindAction(actionName).ApplyBindingOverride(bindingIndex, newPath);
        }

        public void SetDefaultBinding(string actionName, int bindingIndex = 0)
        {
            _gameplayControls.FindAction(actionName).ApplyBindingOverride(bindingIndex, "");
        }
    }
}
