using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [Serializable]
    public class ValueProxyCamera : ValueProxyComponent<Camera> { }

    public class ObjectIsVisibleByCameraCondition : BaseCondition, ITargetingObject
    {
        [SerializeField]
        [Tooltip("The target object to check")]
        [Draggable]
        public ValueProxyTransform m_target;
        [SerializeField]
        [Tooltip("The camera to check against. If left empty, then the check will be done against all cameras")]
        [Draggable]
        public ValueProxyCamera m_camera;
        [SerializeField]
        [Tooltip("Whether it is visible or not")]
        public ValueProxyBool m_visible;
        
        private Renderer m_renderer;
        private Camera m_cam;

        public UnityEngine.Object Target {
            get => m_target;
            set => m_target.Value = value is Component c ? c.transform : value is GameObject go ? go.transform : m_target.Value;
        }

        public string TargetFieldName => nameof(m_target);

        public override void PrepareForEvaluation(ExecutionFlow flow, ExecutionMode mode)
        {
            base.PrepareForEvaluation(flow, mode);
            m_renderer = m_target.Value.GetComponentInChildren<Renderer>();
            m_cam = m_camera.Value ? m_camera.Value : Camera.allCameras.FirstOrDefault(c => c.enabled);
        }

        protected override bool EvaluateCondition()
        {
            if (!m_cam && m_renderer)
            {
                return m_renderer.isVisible == m_visible || (!m_renderer.gameObject.activeInHierarchy && !m_visible);
            }
            return IsVisibleOnScreen(m_target.Value.position, m_cam) == m_visible;
        }

        private bool IsVisibleOnScreen(Vector3 point, Camera cam)
        {
            var projected = cam.WorldToScreenPoint(point);
            return projected.z >= 0
                && projected.x >= 0 && projected.x <= cam.scaledPixelWidth
                && projected.y >= 0 && projected.y <= cam.scaledPixelHeight
                && Vector3.Distance(point, cam.transform.position) < cam.farClipPlane;
        }

        public override string GetDescription()
        {
            string target = m_target.ToString();
            string camera = $" by {m_camera}";
            string visible = m_visible ? " is visible" : " is not visible";
            return target + visible + camera;
        }

        public override string ToFullString()
        {
            return $"[{GetDescription()}]";
        }
    }
}
