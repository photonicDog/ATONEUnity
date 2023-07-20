using UnityEngine;

namespace Assets.Scripts.Debug
{
    public class DebugData : MonoBehaviour
    {
        private static DebugData _instance;
        public static DebugData Instance { get { return _instance; } }

        public Vector2 CurrentViewAngle;
        public Vector2 CurrentInput;
        public Vector3 CurrentVelocity;
        public float CurrentSpeed;
        public Vector3 Position;

        public bool Grounded;
        public bool JumpPressed;
        public bool JumpedThisFrame;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            } else
            {
                _instance = this;
            }
        }


    }
}