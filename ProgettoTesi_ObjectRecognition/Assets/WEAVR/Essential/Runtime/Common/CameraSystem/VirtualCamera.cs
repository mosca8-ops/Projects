using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Camera System/Virtual Camera")]
    public class VirtualCamera : MonoBehaviour
    {
        private static VirtualCamera s_default;
        public static VirtualCamera Default
        {
            get
            {
                if (!s_default)
                {
                    s_default = new GameObject("DefaultVirtualCamera").AddComponent<VirtualCamera>();
                    s_default.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_default;
            }
        }

        public static bool IsDefaultAvailable => s_default;

        public bool orthographic;
        [Range(1, 179)]
        [DisabledBy("isOrthographic", disableWhenTrue: true)]
        public float fieldOfView = 60;
        [DisabledBy("isOrthographic")]
        public float orthographicSize = 5;

        public float nearClipPlane = 0.1f;
        public float farClipPlane = 1000f;
        public float aspect = 1;

        private bool m_inTransition;
        private Camera m_transitionCamera;
        private VirtualCamera m_transitionTarget;
        private VelocityData m_transitionSpeed;
        private System.Action m_onTransitionFinishedCallback;

        private bool m_shouldUnlockCameraOrbit;

        public void StartTransition(Camera camera, VirtualCamera target, float time, System.Action onFinishedCallback) {
            CameraOrbit.StopAnyCameraTransitions();
            StopCurrentTransition();
            if(time <= 0) {
                target.ApplyTo(camera, true);
                onFinishedCallback?.Invoke();
                return;
            }
            m_transitionCamera = camera;
            m_transitionTarget = target;
            m_onTransitionFinishedCallback = onFinishedCallback;

            m_shouldUnlockCameraOrbit = false;
            if (CameraOrbit.Instance.SourceCamera == camera)
            {
                m_shouldUnlockCameraOrbit = !CameraOrbit.Instance.IsLocked;
                CameraOrbit.Instance.IsLocked = true;
            }

            m_transitionSpeed = ComputeSpeeds(this, target, time);

            m_inTransition = true;
        }

        public void StartTransition(Camera camera, float time, System.Action onFinishedCallback)
        {
            CameraOrbit.StopAnyCameraTransitions();
            StopCurrentTransition();
            if (time <= 0)
            {
                ApplyTo(camera, true);
                onFinishedCallback?.Invoke();
                return;
            }

            m_shouldUnlockCameraOrbit = false;
            if(CameraOrbit.Instance.SourceCamera == camera)
            {
                CameraOrbit.Instance.IsLocked = true;
                m_shouldUnlockCameraOrbit = true;
            }

            m_transitionCamera = camera;
            m_transitionTarget = this;
            m_onTransitionFinishedCallback = onFinishedCallback;

            m_transitionSpeed = ComputeSpeeds(camera, this, time);

            m_inTransition = true;
        }

        public void StopCurrentTransition() {
            m_inTransition = false;
            if (m_shouldUnlockCameraOrbit)
            {
                CameraOrbit.Instance.IsLocked = false;
                m_shouldUnlockCameraOrbit = false;
            }
        }

        private void Update() {
            if (m_inTransition) {
                bool finished = InternalMoveTowards(Time.deltaTime);
                ApplyTo(m_transitionCamera, true);
                if (finished){
                    m_onTransitionFinishedCallback?.Invoke();
                    StopCurrentTransition();
                }
            }
        }

        public void UpdateFrom(Camera camera, bool transformAlso) {
            if (transformAlso) {
                transform.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);
            }
            UpdateFrom(camera);
        }

        public void UpdateFrom(VirtualCamera camera, bool transformAlso)
        {
            if (transformAlso)
            {
                transform.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);
            }
            UpdateFrom(camera);
        }

        public void UpdateFrom(Camera camera) {
            orthographic = camera.orthographic;
            fieldOfView = camera.fieldOfView;
            orthographicSize = camera.orthographicSize;

            nearClipPlane = camera.nearClipPlane;
            farClipPlane = camera.farClipPlane;
            aspect = camera.aspect;
        }

        public void UpdateFrom(VirtualCamera camera)
        {
            orthographic = camera.orthographic;
            fieldOfView = camera.fieldOfView;
            orthographicSize = camera.orthographicSize;

            nearClipPlane = camera.nearClipPlane;
            farClipPlane = camera.farClipPlane;
            aspect = camera.aspect;
        }

        public void ApplyTo(Camera camera, bool transformAlso) {
            if (transformAlso) {
                camera.transform.SetPositionAndRotation(transform.position, transform.rotation);
            }
            ApplyTo(camera);
        }

        public void ApplyTo(Camera camera) {
            camera.orthographic = orthographic;
            camera.fieldOfView = fieldOfView;
            camera.orthographicSize = orthographicSize;

            camera.nearClipPlane = nearClipPlane;
            camera.farClipPlane = farClipPlane;
        }

        public VirtualCamera Clone() {
            return Instantiate(this);
        }

        private bool InternalMoveTowards(float dt) {
            fieldOfView = Mathf.MoveTowards(fieldOfView, m_transitionTarget.fieldOfView, m_transitionSpeed.fovSpeed * dt);
            orthographicSize = Mathf.MoveTowards(orthographicSize, m_transitionTarget.orthographicSize, m_transitionSpeed.orthoSizeSpeed * dt);

            transform.position = Vector3.MoveTowards(transform.position, m_transitionTarget.transform.position, m_transitionSpeed.linearSpeed * dt);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, m_transitionTarget.transform.rotation, m_transitionSpeed.rotationSpeed * dt);

            return fieldOfView == m_transitionTarget.fieldOfView
                && orthographicSize == m_transitionTarget.orthographicSize
                && transform.position == m_transitionTarget.transform.position
                && transform.rotation == m_transitionTarget.transform.rotation;
        }

        public bool MoveTowards(VirtualCamera to, float step, float rotationStep) {
            MoveTowardsNoCheck(to, step, rotationStep);

            return fieldOfView == to.fieldOfView
                && orthographicSize == to.orthographicSize
                && transform.position == to.transform.position
                && transform.rotation == to.transform.rotation;
        }

        public void MoveTowardsNoCheck(VirtualCamera to, float step, float rotationStep) {
            fieldOfView = Mathf.MoveTowards(fieldOfView, to.fieldOfView, step);
            orthographicSize = Mathf.MoveTowards(orthographicSize, to.orthographicSize, step);

            transform.position = Vector3.MoveTowards(transform.position, to.transform.position, step);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, to.transform.rotation, rotationStep);
        }

        public static void MoveTowards(VirtualCamera current, VirtualCamera from, VirtualCamera to, float step, float rotationStep) {
            current.fieldOfView = Mathf.MoveTowards(from.fieldOfView, to.fieldOfView, step);
            current.orthographicSize = Mathf.MoveTowards(from.orthographicSize, to.orthographicSize, step);

            current.transform.position = Vector3.MoveTowards(from.transform.position, to.transform.position, step);
            current.transform.rotation = Quaternion.RotateTowards(from.transform.rotation, to.transform.rotation, rotationStep);
        }

        public static VelocityData ComputeSpeeds(VirtualCamera from, VirtualCamera to, float time) {
            return new VelocityData() {
                linearSpeed = (to.transform.position - from.transform.position).magnitude / time,
                rotationSpeed = Quaternion.Angle(from.transform.rotation, to.transform.rotation) / time,
                fovSpeed = Mathf.Abs(to.fieldOfView - from.fieldOfView) / time,
                orthoSizeSpeed = Mathf.Abs(to.orthographicSize - from.orthographicSize) / time,
            };
        }

        public static VelocityData ComputeSpeeds(Camera camera, VirtualCamera to, float time)
        {
            return new VelocityData()
            {
                linearSpeed = (to.transform.position - camera.transform.position).magnitude / time,
                rotationSpeed = Quaternion.Angle(camera.transform.rotation, to.transform.rotation) / time,
                fovSpeed = Mathf.Abs(to.fieldOfView - camera.fieldOfView) / time,
                orthoSizeSpeed = Mathf.Abs(to.orthographicSize - camera.orthographicSize) / time,
            };
        }

        public struct VelocityData
        {
            // All required speeds
            public float linearSpeed;
            public float rotationSpeed;
            public float fovSpeed;
            public float orthoSizeSpeed;
        }
    }
}
