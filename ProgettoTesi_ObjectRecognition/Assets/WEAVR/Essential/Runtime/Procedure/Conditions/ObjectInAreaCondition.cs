using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class ObjectInAreaCondition : BaseCondition, ITargetingObject
    {
        [Serializable]
        private class ValueProxyCollider : ValueProxyComponent<Collider> { }

        private const int k_MaxRenderers = 6;

        public enum TriggerType
        {
            [Tooltip("Triggered when the target enters the area")]
            EntersArea,
            [Tooltip("Triggered when the target exits the area")]
            LeavesArea,
        }

        public enum ValidationType
        {
            [Tooltip("Make the check with the target pivot point 'touches' the area")]
            Pivot,
            [Tooltip("Make the check with the target bounding box 'touches' the area")]
            BoundingBox,
            [Tooltip("Make the check when target's full body is either outside or inside the area")]
            FullBody,
        }

        [SerializeField]
        [Tooltip("The target object")]
        private ValueProxyTransform m_target;
        [SerializeField]
        [Tooltip("The area to check against")]
        [Draggable]
        private ValueProxyCollider m_area;
        [SerializeField]
        [Tooltip("When to trigger the check")]
        public TriggerType m_when;
        [SerializeField]
        [Tooltip("What exactly to check")]
        public ValidationType m_how;

        private Bounds m_bounds;
        private Collider m_collider;
        private SphereCollider m_sphereArea;
        private Renderer[] m_renderers;
        private Func<Bounds> m_boundsProvider;

        public UnityEngine.Object Target {
            get => m_target;
            set => m_target.Value = value is Component c ? c.transform : value is GameObject go ? go.transform : m_target.Value;
        }

        public string TargetFieldName => nameof(m_target);

        public Collider Area => m_area.Value;

        public override void PrepareForEvaluation(ExecutionFlow flow, ExecutionMode mode)
        {
            base.PrepareForEvaluation(flow, mode);
            m_boundsProvider = null;
            m_sphereArea = m_area.Value as SphereCollider;
            if (m_how != ValidationType.Pivot)
            {
                m_collider = m_target.Value.GetComponent<Collider>();
                if (m_collider)
                {
                    m_boundsProvider = GetBoundsFromCollider;
                }
                else
                {
                    var allRenderers = m_target.Value.GetComponentsInChildren<Renderer>(true);
                    if (allRenderers.Length > 0)
                    {
                        m_renderers = new Renderer[Mathf.Min(allRenderers.Length, k_MaxRenderers)];
                        for (int i = 0; i < m_renderers.Length; i++)
                        {
                            m_renderers[i] = allRenderers[i];
                        }
                        m_boundsProvider = GetBoundsFromRenderers;
                    }
                    else
                    {
                        m_how = ValidationType.Pivot;
                    }
                }
            }
        }

        protected override bool EvaluateCondition()
        {
            switch (m_how)
            {
                case ValidationType.Pivot:
                    return m_when != TriggerType.EntersArea ^ AreaContainsPoint();
                case ValidationType.BoundingBox:
                    return AreaIntersectsBounds();
                case ValidationType.FullBody:
                    return m_when == TriggerType.EntersArea ? BoundsIsInsideArea() : !AreaIntersectsBounds();
            }
            return false;
        }

        private bool BoundsIsInsideArea()
        {
            if (m_sphereArea)
            {
                var sphereCenter = m_sphereArea.center + m_sphereArea.transform.position;
                var sqrRadius = m_sphereArea.radius * m_sphereArea.radius;
                var bounds = m_boundsProvider();
                return (bounds.min - sphereCenter).sqrMagnitude <= sqrRadius
                    && (bounds.max - sphereCenter).sqrMagnitude <= sqrRadius;
            }
            return IsInside(m_area.Value.bounds, m_boundsProvider());
        }

        private bool AreaIntersectsBounds()
        {
            if (m_sphereArea)
            {
                var sphereCenter = m_sphereArea.center + m_sphereArea.transform.position;
                var sqrRadius = m_sphereArea.radius * m_sphereArea.radius;
                var closestPoint = m_boundsProvider().ClosestPoint(sphereCenter);
                return (closestPoint - sphereCenter).sqrMagnitude <= sqrRadius;
            }
            return m_area.Value.bounds.Intersects(m_boundsProvider());
        }

        private bool AreaContainsPoint()
        {
            return m_sphereArea ? 
                Vector3.Distance(m_target.Value.position, m_sphereArea.center + m_sphereArea.transform.position) <= m_sphereArea.radius : 
                m_area.Value.bounds.Contains(m_target.Value.position);
        }

        public override void OnValidate()
        {
            base.OnValidate();
            var target = m_target.Value;
            if (target)
            {
                if (m_how != ValidationType.Pivot && target.GetComponent<Collider>() == null && target.GetComponentInChildren<Renderer>() == null)
                {
                    m_how = ValidationType.Pivot;
                }
            }
        }

        private bool IsInside(Bounds a, Bounds b)
        {
            return IsInside(a.min, b.min, a.max, b.max);
        }

        private bool IsInside(Vector3 minA, Vector3 minB, Vector3 maxA, Vector3 maxB)
        {
            return minA.x <= minB.x && minA.y <= minB.y && minA.z <= minB.z
                && maxA.x >= maxB.x && maxA.y >= maxB.y && maxA.z >= maxB.z;
        }

        private Bounds GetBoundsFromCollider()
        {
            return m_collider.bounds;
        }

        private Bounds GetBoundsFromRenderers()
        {
            Bounds bounds = m_renderers[0].bounds;
            for (int i = 1; i < m_renderers.Length; i++)
            {
                bounds.Encapsulate(m_renderers[i].bounds);
            }
            return bounds;
        }

        public override void ForceEvaluation()
        {
            base.ForceEvaluation();
            try
            {
                switch (m_when)
                {
                    case TriggerType.EntersArea:
                        m_target.Value.position = m_area.Value.bounds.center;
                        break;
                    case TriggerType.LeavesArea:
                        m_target.Value.position = m_area.Value.bounds.max;
                        break;
                }
            }
            catch (Exception e)
            {
                WeavrDebug.LogException(this, e);
            }
        }

        public override string GetDescription()
        {
            string target = m_target.ToString();
            string area = m_area.ToString();
            string when = m_when == TriggerType.EntersArea ? " enters in " : " leaves ";
            return target + when + area;
        }

        public override string ToFullString()
        {
            string target = m_target.ToString();
            string area = m_area.ToString();
            return $"[{target} in {area}]";
        }
    }
}
