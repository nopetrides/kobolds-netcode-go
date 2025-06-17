using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.BossRoom.CameraUtils
{
    public class CameraController : MonoBehaviour
    {
        private CinemachineFreeLook _mMainCamera;

        void Start()
        {
            AttachCamera();
        }

        private void AttachCamera()
        {
            _mMainCamera = GameObject.FindObjectOfType<CinemachineFreeLook>();
            Assert.IsNotNull(_mMainCamera, "CameraController.AttachCamera: Couldn't find gameplay freelook camera");

            if (_mMainCamera)
            {
                // camera body / aim
                _mMainCamera.Follow = transform;
                _mMainCamera.LookAt = transform;
                // default rotation / zoom
                _mMainCamera.m_Heading.m_Bias = 40f;
                _mMainCamera.m_YAxis.Value = 0.5f;
            }
        }
    }
}
