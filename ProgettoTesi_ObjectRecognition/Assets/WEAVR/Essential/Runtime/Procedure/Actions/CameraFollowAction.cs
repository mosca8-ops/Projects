using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class CameraFollowAction : BaseReversibleProgressAction, ITargetingObject
    {
        [SerializeField]
        [Tooltip("The object to be followed by the camera")]
        [Draggable]
        private ValueProxyTransform m_target;
        [SerializeField]
        [Tooltip("The camera which should follow the target")]
        [Draggable]
        private Camera m_camera;
        [SerializeField]
        [Tooltip("If true, the camera will keep its current position and will only 'observe' the target from that position")]
        private bool m_fixedPosition;
        [SerializeField]
        [Tooltip("The speed in [m/s] of the camera to follow its target")]
        [HiddenBy(nameof(m_fixedPosition), hiddenWhenTrue: true)]
        private float m_followSpeed = 1;
        [SerializeField]
        [Range(1, 20)]
        [Tooltip("The rotation speed of the camera, lower the value for a smoother rotation")]
        private float m_rotationSpeed = 10;
        [SerializeField]
        [Reversible]
        [Tooltip("The field of view to apply to the camera")]
        private OptionalAnimatedFloat m_fieldOfView;
        [SerializeField]
        [AbsoluteValue]
        [Tooltip("The time in [s] to follow the target")]
        private OptionalProxyFloat m_duration;

        [System.NonSerialized]
        private Transform m_lookAtTarget;
        [System.NonSerialized]
        private float m_remainingTime;
        [System.NonSerialized]
        private Vector3 m_cameraToTarget;
        [System.NonSerialized]
        private Vector3 m_cameraToTargetNormalized;
        [System.NonSerialized]
        private float m_distanceToTarget;

        [System.NonSerialized]
        private Vector3 m_prevPosition;
        [System.NonSerialized]
        private Quaternion m_prevRotation;
        [System.NonSerialized]
        private float m_prevFov;
        
        [System.NonSerialized]
        private Transform m_cameraTransform;
        
        public Object Target {
            get => m_target;
            set => this.To(value, m_target);
        }

        public Camera Camera
        {
            get => m_camera;
            set
            {
                if(m_camera != value)
                {
                    BeginChange();
                    m_camera = value ? value : WeavrCamera.GetCurrentCamera(WeavrCamera.WeavrCameraType.Free);
                    PropertyChanged(nameof(Camera));
                }
            }
        }

        public float Duration
        {
            get => m_duration;
            set
            {
                if(m_duration != value)
                {
                    BeginChange();
                    m_duration.value = value < 0.1f ? 1f : value;
                    m_duration.enabled = true;
                    PropertyChanged(nameof(Duration));
                }
            }
        }

        public bool FixedPosition
        {
            get => m_fixedPosition;
            set
            {
                if(m_fixedPosition != value)
                {
                    BeginChange();
                    m_fixedPosition = value;
                    PropertyChanged(nameof(FixedPosition));
                }
            }
        }

        public string TargetFieldName => nameof(m_target);

        private Transform LookAtTarget
        {
            get
            {
                if (!m_lookAtTarget)
                {
                    m_lookAtTarget = new GameObject("__LookAt__Dummy").transform;
                    m_lookAtTarget.gameObject.hideFlags = HideFlags.HideAndDontSave;
                    m_lookAtTarget.position = m_camera.transform.position;
                }
                return m_lookAtTarget;
            }
        }

        public override void OnValidate()
        {
            base.OnValidate();
            m_followSpeed = m_followSpeed <= 0.001f ? 1 : m_followSpeed;
            m_fieldOfView.value.CurrentTargetValue = Mathf.Clamp(m_fieldOfView.value.TargetValue, 10, 170);
            if(AsyncThread == 0)
            {
                m_duration.enabled = true;
                m_duration.value = m_duration.value.Value <= 0.1f ? 1f : m_duration.value.Value;
            }
        }

        public override void OnStart(ExecutionFlow flow, ExecutionMode executionMode)
        {
            base.OnStart(flow, executionMode);
            if (!m_camera)
            {
                m_camera = WeavrCamera.CurrentCamera;
            }
            m_cameraTransform = m_camera.transform;
            m_remainingTime = m_duration.enabled ? m_duration.value.Value : float.MaxValue;
            m_cameraToTarget = m_target.Value.position - m_cameraTransform.position;
            m_distanceToTarget = m_cameraToTarget.magnitude;
            m_cameraToTargetNormalized = m_cameraToTarget.normalized;
            m_followSpeed = m_followSpeed <= 0.001f ? 1 : m_followSpeed;

            m_prevPosition = m_cameraTransform.position;
            m_prevRotation = m_cameraTransform.rotation;
            m_prevFov = m_camera.fieldOfView;

            if (m_fieldOfView.enabled)
            {
                m_fieldOfView.value.Start(m_prevFov);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!m_camera)
            {
                m_camera = Application.isPlaying ? WeavrCamera.CurrentCamera : WeavrCamera.GetCurrentCamera(WeavrCamera.WeavrCameraType.Free);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (m_lookAtTarget)
            {
                DestroyLookAtTarget();
            }
        }

        private void DestroyLookAtTarget()
        {
            if (Application.isPlaying)
            {
                Destroy(m_lookAtTarget.gameObject);
            }
            else
            {
                DestroyImmediate(m_lookAtTarget.gameObject);
            }
            m_lookAtTarget = null;
        }

        public override bool Execute(float dt)
        {
            if (!m_fixedPosition)
            {
                LookAtTarget.position = m_target.Value.position - m_cameraToTarget;
                m_cameraTransform.position = Vector3.Lerp(m_cameraTransform.position, m_lookAtTarget.position, m_followSpeed * dt);
            }

            LookAtTarget.LookAt(m_target);
            m_cameraTransform.rotation = Quaternion.Slerp(m_cameraTransform.rotation, m_lookAtTarget.rotation, m_rotationSpeed * dt);

            if (m_fieldOfView.enabled)
            {
                m_camera.fieldOfView = m_fieldOfView.value.Next(dt);
            }

            Progress = 1;
            if (m_duration.enabled && m_duration.value != 0)
            {
                m_remainingTime -= dt;
                Progress = 1 - (m_remainingTime / m_duration.value);
            }

            if(m_remainingTime <= 0 && m_lookAtTarget)
            {
                DestroyLookAtTarget();
                Progress = 1;
            }
            return m_remainingTime <= 0;
        }
        

        public override void FastForward()
        {
            base.FastForward();
            m_cameraTransform.LookAt(m_target);
            if (m_lookAtTarget)
            {
                DestroyLookAtTarget();
            }
        }

        public override void OnStop()
        {
            base.OnStop();
            if (m_lookAtTarget)
            {
                DestroyLookAtTarget();
            }
        }

        public override void OnContextExit(ExecutionFlow flow)
        {
            if (m_fieldOfView.enabled)
            {
                m_fieldOfView.value.AutoAnimate(m_prevFov, v => m_camera.fieldOfView = v);
            }
            if (AsyncThread == 0)
            {
                flow.EnqueueCoroutine(AnimateBackwards(), true);
            }
            else
            {
                flow.StartCoroutine(AnimateBackwards());
            }
        }

        private IEnumerator AnimateBackwards()
        {
            float timeout = m_fixedPosition ? (m_fieldOfView.enabled ? Mathf.Max(m_fieldOfView.value.Duration, 1f) : 1f) 
                : Vector3.Distance(m_cameraTransform.position, m_prevPosition) / m_followSpeed;
            while(timeout > 0 || m_cameraTransform.position != m_prevPosition)
            {
                timeout -= Time.deltaTime;
                m_cameraTransform.position = Vector3.MoveTowards(m_cameraTransform.position, m_prevPosition, m_followSpeed * Time.deltaTime);
                m_cameraTransform.rotation = Quaternion.RotateTowards(m_cameraTransform.rotation, m_prevRotation, m_followSpeed * Time.deltaTime);

                yield return null;
            }
        }

        public override string GetDescription()
        {
            return (m_camera ? $"Camera {m_camera.name} " : "Camera [ ? ] ")
                + $"follow {m_target} "
                + (m_duration.enabled ? $"for {m_duration.value} " : string.Empty);
        }
    }
}
