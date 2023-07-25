using Assets.Scripts.Debug;
using Assets.Scripts.Gameplay.Components;
using Assets.Scripts.Gameplay.Structures;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class DebugView : MonoBehaviour
{
    public bool RecordChains;
    public bool RecordAngleSpeedIncreases;

    private TextMeshPro _textMesh;
    private Transform _pCamera;
    private DebugData _debug;

    private Vector2 CurrentViewAngle;
    private Vector2 ViewAngleAtLastJump;
    private Vector2 CurrentInput;
    private Vector2 InputAtLastJump;
    private Vector3 CurrentVelocity;
    private Vector3 VelocityAtLastJump;
    private float CurrentSpeed;
    private float SpeedAtLastJump;
    private Vector3 AngleChangeSinceLastJump;
    private Vector2 ViewAngleChangeBetweenJumps;
    private Vector3 VelocityChangeBetweenJumps;
    private float SpeedChangeBetweenJumps;

    private List<Vector2> ViewAnglesOfJumpChain;
    private List<Vector2> InputsOfJumpChain;
    private List<Vector3> VelocitiesOfJumpChain;
    private List<float> SpeedsOfJumpChain;
    private uint _jumpChain;

    private MovementData _movementChange;
    private List<MovementData> _angleVelocityChanges;

    private bool firstFrame;
    private uint _totalJumps;

    // Start is called before the first frame update
    void Start()
    {
        _textMesh = GetComponent<TextMeshPro>();
        //_pInput = InputManager.Instance;
        _debug = DebugData.Instance;

        ViewAnglesOfJumpChain = new List<Vector2>();
        InputsOfJumpChain = new List<Vector2>();
        VelocitiesOfJumpChain = new List<Vector3>();
        SpeedsOfJumpChain = new List<float>();

        _angleVelocityChanges = new List<MovementData>();

        CurrentViewAngle = Vector3.zero;
        CurrentInput = Vector2.zero;
        CurrentVelocity = Vector3.zero;
        CurrentSpeed = 0f;

        _jumpChain = 0;
        firstFrame = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (firstFrame)
        {
            firstFrame = false;
            _pCamera = Camera.main.transform;
            ViewAngleChangeBetweenJumps = Vector3.zero;
        }

        UpdateValues();

        _textMesh.text = $"CVelocity: {CurrentVelocity}\nCSpeed: {CurrentSpeed.ToString("F2")}\nCViewAngle: {CurrentViewAngle.ToString("F2")}\nIsGrounded: {_debug.Grounded}\n\n" +
                        $"LJVelocity: {VelocityAtLastJump}\nLJSpeed: {SpeedAtLastJump.ToString("F2")}\nLJViewAngle: {ViewAngleAtLastJump.ToString("F2")}\n\n" +
                        $"View Angle Change Between Jumps: {_movementChange.ViewAngle.y.ToString("F2")}\nVelocity Change Between Jumps: {_movementChange.Velocity}\nSpeed Change Between Jumps: {_movementChange.Speed.ToString("F2")}\n\n";

    }

    private void OnApplicationQuit()
    {
        if (RecordAngleSpeedIncreases)
        {

        }
    }

    void UpdateValues()
    {
        if (_debug.JumpedThisFrame)
        {
            if (RecordAngleSpeedIncreases)
            {
                _movementChange.ViewAngle = RelativeAngle(CurrentViewAngle - ViewAngleAtLastJump);
                _movementChange.Velocity = CurrentVelocity - VelocityAtLastJump;
                _movementChange.Speed = CurrentSpeed - SpeedAtLastJump;

                _angleVelocityChanges.Add(_movementChange);
            }

            // last frame's values
            ViewAngleAtLastJump = RelativeAngle(CurrentViewAngle);
            InputAtLastJump = CurrentInput;
            VelocityAtLastJump = CurrentVelocity;
            SpeedAtLastJump = CurrentSpeed;

            _debug.JumpedThisFrame = false;
        }

        CurrentViewAngle = _debug.CurrentViewAngle;
        CurrentInput = _debug.CurrentInput;
        CurrentVelocity = _debug.CurrentVelocity;
        CurrentSpeed = _debug.CurrentSpeed;
        AngleChangeSinceLastJump = RelativeAngle(CurrentViewAngle - ViewAngleAtLastJump);
    }

    public float RelativeAngle(float angle)
    {
        return Mathf.Abs(angle) > 180 ? (angle - 360) % 360 : angle % 360;
    }

    public Vector2 RelativeAngle(Vector2 angles)
    {
        return new Vector2(RelativeAngle(angles.x), RelativeAngle(angles.y));
    }
}
