using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEngine;

using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{

    public class MoveCameraAction : BaseReversibleProgressAction, ITargetingObject
    {
        [Serializable]
        private class ValueProxyVirtualCamera : ValueProxyComponent<VirtualCamera>{ }

        [SerializeField]
        [Tooltip("The camera to move")]
        [Draggable]
        private Camera m_camera;
        [SerializeField]
        [Tooltip("The point from where to start moving the camera. If left null then the camera will start from its current position")]
        [Draggable]
        private ValueProxyVirtualCamera m_from;
        [SerializeField]
        [Tooltip("The destination of the camera")]
        [Draggable]
        private ValueProxyVirtualCamera m_to;
        [SerializeField]
        [Tooltip("The time the camera will take to reach the specified destination")]
        private ValueProxyFloat m_duration;
        [SerializeField]
        [Tooltip("When true, the destination will become the default virtual camera for all free cameras")]
        private bool m_setAsDefault;

        [System.NonSerialized]
        private VirtualCamera m_currentCam;
        [System.NonSerialized]
        private VirtualCamera m_start;

        private bool m_started;
        private bool m_ended;

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

        public Object Target {
            get => Camera;
            set => Camera = value is Camera cam ? 
                cam : value is Component c ? c.GetComponent<Camera>() : 
                value is GameObject go ? go.GetComponent<Camera>() : 
                value == null ? null : Camera;
        }

        public VirtualCamera From
        {
            get => m_from;
            set
            {
                if(m_from != value)
                {
                    BeginChange();
                    m_from.Value = value;
                    PropertyChanged(nameof(From));
                }
            }
        }

        public VirtualCamera To
        {
            get => m_to;
            set
            {
                if (m_to != value)
                {
                    BeginChange();
                    m_to.Value = value;
                    PropertyChanged(nameof(To));
                }
            }
        }

        public float Duration
        {
            get => m_duration;
            set
            {
                if (m_duration != value)
                {
                    BeginChange();
                    m_duration.Value = value;
                    PropertyChanged(nameof(Duration));
                }
            }
        }

        public string TargetFieldName => nameof(m_camera);

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!m_camera) { m_camera = WeavrCamera.CurrentCamera; }
        }

        public override void OnStart(ExecutionFlow flow, ExecutionMode executionMode)
        {
            base.OnStart(flow, executionMode);
            if (!m_camera) { m_camera = Application.isPlaying ? WeavrCamera.CurrentCamera : WeavrCamera.GetCurrentCamera(WeavrCamera.WeavrCameraType.Free); }
            m_started = false;
            m_ended = false;
        }

        public override bool Execute(float dt)
        {
            if (m_setAsDefault)
            {
                VirtualCamera.Default.UpdateFrom(m_to.Value, true);
            }
            if (!m_started)
            {
                m_started = true;
                if (m_currentCam)
                {
                    m_currentCam.UpdateFrom(m_camera, true);
                }
                else
                {
                    CreateCurrentCamera();
                }
                m_currentCam.StartTransition(m_camera, m_to, m_duration, () => m_ended = true);
            }
            Progress += dt / m_duration;
            return m_ended;
        }

        private void CreateCurrentCamera()
        {
            if (m_from.Value)
            {
                m_currentCam = m_from.Value.Clone();
            }
            else
            {
                m_currentCam = m_to.Value.Clone();
                m_currentCam.UpdateFrom(m_camera, true);
            }
            m_currentCam.gameObject.hideFlags = HideFlags.HideAndDontSave;

            if (!m_start)
            {
                m_start = m_currentCam.Clone();
                m_start.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        public override void FastForward()
        {
            base.FastForward();
            var virtualCameraToApply = /*RevertOnExit ? m_start : */m_to.Value;
            if (m_currentCam)
            {
                m_currentCam.StopCurrentTransition();
            }
            virtualCameraToApply.ApplyTo(m_camera, true);
        }

        public override void OnStop()
        {
            base.OnStop();
            var virtualCameraToApply = RevertOnExit ? m_start : m_to.Value;
            virtualCameraToApply.ApplyTo(m_camera, true);
            if (m_currentCam)
            {
                m_currentCam.StopCurrentTransition();
                DestroyTempObjects();
            }
        }

        private void DestroyTempObjects()
        {
            Destroy(m_currentCam.gameObject);
            Destroy(m_start.gameObject);
            m_currentCam = null;
            m_start = null;
        }

        protected override void OnStateChanged(ExecutionState value)
        {
            base.OnStateChanged(value);
            if(!value.HasFlag(ExecutionState.Running) && !RevertOnExit && m_currentCam)
            {
                DestroyTempObjects();
            }
        }

        public override void OnContextExit(ExecutionFlow flow)
        {
            if (m_currentCam && m_start)
            {
                m_currentCam.StartTransition(m_camera, m_start, m_duration, DestroyTempObjects);
            }
        }

        public override string GetDescription()
        {
            return (m_camera ? $"Move camera {m_camera.name} " : "Move camera [ ? ] ")
                + (m_from.Value ? $"from {m_from.Value.name} " : string.Empty)
                + $"to {m_to} "
                + $"in {m_duration:0.00} seconds";
        }
    }
}
