using System.Collections;
using UnityEngine;
using Cinemachine;
using Assets.Scripts.Gameplay.Structures;
using UnityEngine.Windows;
using Assets.Scripts.Gameplay.Models.Configurations;

namespace Assets.Scripts.Gameplay.Controllers
{
    [RequireComponent(typeof(CinemachineVirtualCameraBase))]
    public class CameraController : MonoBehaviour
    {
        private CameraConfig _config;
        public Camera mainCamera;
        private CinemachineVirtualCamera virtualCamera;
        public Transform lookAt;

        private float _currentX, _currentY;

        // Use this for initialization
        void Start()
        {
            _config = new CameraConfig();
            virtualCamera = GetComponent<CinemachineVirtualCamera>();

            lookAt = new GameObject("LookAtTarget").transform;
            lookAt.position = virtualCamera.transform.position;
            lookAt.position += virtualCamera.transform.forward * _config.FocalDistance;

            virtualCamera.LookAt = lookAt;
        }

        // Update is called once per frame
        void Update()
        {
            float x = virtualCamera.GetInputAxisProvider().GetAxisValue(0);
            float y = virtualCamera.GetInputAxisProvider().GetAxisValue(1);
            //Debug.Log($"{x},{y}");

            _currentX = Mathf.Clamp(_currentX + y * _config.Speed, _config.PitchMin, _config.PitchMax);
            _currentY += x * _config.Speed;

            Vector3 dir = Vector3.forward * _config.FocalDistance;

            Vector3 input = new Vector3(-_currentX, _currentY);
            Quaternion r = Quaternion.Euler(input);

            Vector3 freeLook = r * dir;
            Vector3 pos = virtualCamera.transform.position + freeLook;
            lookAt.position = pos;
            //Debug.Log(pos);
            ///why is this even here? - freya
            //lookAt.Rotate(0,0, player.GetInputVector().x * _config.FocalDistance);

            //Debug.Log($"LA: {lookAt.eulerAngles.z}, C: {virtualCamera.transform.eulerAngles.z}");
            //Debug.DrawLine(lookAt.position, transform.position, Color.green);
        }
        public AngleVectors GetLookAtAsVectors()
        {
            /// yeah turns out unity's direction vectors were right! but also i liked
            /// the maths for this and it was difficult so ill keep it just in case - freya
            // 1. convert quarternions into euler angles (yaw, pitch, roll)
            // 2. convert euler angles into vectors
            // note: unity is weird, so Z is pitch Y is yaw and X is roll
            AngleVectors result;
            //var roll = Mathf.Deg2Rad * RelativeAngle(transform.eulerAngles.x);
            //var yaw = Mathf.Deg2Rad * RelativeAngle(transform.eulerAngles.y);
            //var pitch = Mathf.Deg2Rad * RelativeAngle(transform.eulerAngles.z);

            //var sp = Mathf.Sin(pitch);
            //var cp = Mathf.Cos(pitch);
            //var sy = Mathf.Sin(yaw);
            //var cy = Mathf.Cos(yaw);
            //var sr = Mathf.Sin(roll);
            //var cr = Mathf.Cos(roll);

            //lookat vector also needs to be adapted for this
            //result.forward = new Vector3(cp * cy, -sp, cp * sy);
            //result.right = new Vector3(-1 * sr * sp * cy + (-1 * cr * -sy), -1 * sr * cp, (-1 * sr * sp * sy) + (-1 * cr * cy));
            //result.up = new Vector3(cr * sp * cy + (-sr * -sy), cr * cp, (cr * sp * sy) + (-sr * cy));
            result.forward = mainCamera.transform.forward;
            result.right = mainCamera.transform.right;
            result.up = mainCamera.transform.up;

            //Debug.Log($"X: {transform.eulerAngles.x}, Y: {transform.eulerAngles.y}, Z: {transform.eulerAngles.z}"); 

            return result;
        }
    }
}