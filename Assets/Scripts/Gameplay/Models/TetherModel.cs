using Assets.Scripts.Gameplay.Models.Enums;
using UnityEngine;

namespace Assets.Scripts.Gameplay.Models
{
    public class TetherModel
    {
        private TetherStatus state;

        private Transform _tetherHook;
        private LineRenderer _tetherRenderer;

        private Vector3 _hookTarget;
        private Vector3 _hookPosition;
        private float _tetherRadius;
        private Collider _tetheredCollider;

        private float _cooldown;

        public TetherModel(Transform tetherHook, LineRenderer tetherRenderer)
        {
            _tetherHook = tetherHook;
            _tetherRenderer = tetherRenderer;
        }

        public TetherStatus State { get { return state; } set { state = value; } }
        public Transform TetherHook { get { return _tetherHook; } }
        public LineRenderer TetherRenderer { get { return _tetherRenderer; } }
        public Vector3 HookTarget {  get { return _hookTarget; } set { _hookTarget = value; } }
        public Vector3 NextHookPosition { get { return _hookPosition; } set { _hookPosition = value; } }
        public Collider TetheredCollider { get { return _tetheredCollider; } set { _tetheredCollider = value; } }
        public float Cooldown { get {  return _cooldown; } set {  _cooldown = value; } }
        public float TetherRadius { get { return _tetherRadius; } set { _tetherRadius = value; } }

        public bool CheckState(TetherStatus tetherStatus)
        {
            return State == tetherStatus;
        }

        public void SetState(TetherStatus tetherStatus)
        {
            State = tetherStatus;
        }
    }
}
