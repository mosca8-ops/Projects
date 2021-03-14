using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.ImpactSystem
{

    [AddComponentMenu("WEAVR/Impact System/Hitting Object")]
    public class HittingObject : MonoBehaviour
    {
        [SerializeField]
        [CanBeGenerated("Hit_Direction", Relationship.Child)]
        [Draggable]
        private Transform m_hitDirection;
        [SerializeField]
        [Tooltip("Min force to be considered a hit")]
        private float m_minForceForHit = 500;
        [SerializeField]
        [Draggable]
        private AbstractObjectMaterial[] m_hittingParts;

        [Space]
        [SerializeField]
        private UnityEventFloat m_onHit;

        private float m_lastCheckTime;
        private float m_lastHitTime;
        private float m_lastHitForce;
        private float m_totalForce;

        private HashSet<AbstractObjectMaterial> m_processedCollisions = new HashSet<AbstractObjectMaterial>();

        private float m_nextValidCollision;
        private bool m_potentialHit;
        private Vector3 m_frameForce;

        public float LastHitTime => m_lastHitTime;
        public float TimeSinceLastHit => Time.time - m_lastHitTime;
        public float TotalHitsForce
        {
            get => m_totalForce;
            set
            {
                if(value == 0)
                {
                    m_totalForce = 0;
                }
            }
        }

        public float LastHitForce => m_lastHitForce;

        public IEnumerable<AbstractObjectMaterial> HittingParts => m_hittingParts;

        private void Reset()
        {
            m_hittingParts = GetComponentsInChildren<AbstractObjectMaterial>(true);
        }

        private void OnValidate()
        {
            if(m_hittingParts == null || m_hittingParts.Length == 0)
            {
                m_hittingParts = GetComponentsInChildren<AbstractObjectMaterial>(true);
            }
        }
        
        void OnEnable()
        {
            for (int i = 0; i < m_hittingParts.Length; i++)
            {
                if (m_hittingParts[i])
                {
                    m_hittingParts[i].OnCollision -= HittingObject_OnCollision;
                    m_hittingParts[i].OnCollision += HittingObject_OnCollision;
                }
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < m_hittingParts.Length; i++)
            {
                if (m_hittingParts[i])
                {
                    m_hittingParts[i].OnCollision -= HittingObject_OnCollision;
                }
            }
        }

        protected bool TryPerformHit(Collision collision, ContactPoint point, Vector3 impulseOnTime)
        {
            float force = 0;
            if (m_hitDirection)
            {
                force = -Vector3.Dot(impulseOnTime, m_hitDirection.forward);
            }
            else
            {
                force = Mathf.Abs((impulseOnTime).magnitude);
            }

            if(force >= m_minForceForHit)
            {
                m_totalForce += force;
                m_lastHitForce += force;
                m_lastHitTime = Time.time;
                point.otherCollider.GetComponentInParent<IHitReceiver>()?.AbsorbHit(this, force, point);
                m_onHit.Invoke(force);
                return true;
            }
            return false;
        }

        private bool HittingObject_OnCollision(AbstractObjectMaterial caller, Collision collision, ContactPoint point, Vector3 force)
        {
            if(Time.time > m_lastCheckTime)
            {
                m_lastCheckTime = Time.time;
                m_processedCollisions.Clear();
                m_lastHitForce = 0;
            }
            m_potentialHit = true;
            if (!m_processedCollisions.Contains(caller))
            {
                m_processedCollisions.Add(caller);
                return TryPerformHit(collision, point, force);
            }
            return false;
        }
    }
}
