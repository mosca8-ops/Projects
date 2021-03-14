using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.ImpactSystem
{
    /// <summary>
    /// TODO: Consider using this logic without rigidbody... IN VR The rigidbody is kinematic which doesn't trigger physics events
    /// </summary>
    [AddComponentMenu("WEAVR/Impact System/Hit Absorber")]
    public class HitAbsorber : MonoBehaviour, IHitReceiver
    {
        [SerializeField]
        [Tooltip("Min force to be considered a hit")]
        private float m_minForceForHit = 500;
        [SerializeField]
        [Tooltip("Total force to receive to raise the event")]
        private OptionalFloat m_health;

        [SerializeField]
        private TagArray m_receiveHitsFrom;

        [Space]
        [SerializeField]
        private UnityEventFloat m_onAnyHit;
        [SerializeField]
        private UnityEvent m_onEmptyHealth;

        [Space]
        [SerializeField]
        private HitEvent[] m_events;

        private int m_hitCount;
        private float m_lastCheckTime;
        private float m_lastHitTime;
        private float m_lastHitForce;
        private float m_totalForce;

        private float m_nextValidCollision;
        private bool m_potentialHit;
        private Vector3 m_frameForce;
        [SerializeField]
        [ShowAsReadOnly]
        private float m_remainingHealth;

        public float RemainingHealth
        {
            get => m_remainingHealth;
            set
            {
                if(m_remainingHealth != value)
                {
                    if (value <= 0 && m_remainingHealth > 0)
                    {
                        m_remainingHealth = value;
                        m_onEmptyHealth.Invoke();
                    }
                    else
                    {
                        m_remainingHealth = value;
                    }
                }
            }
        }

        public float MinHitForce => m_minForceForHit;

        public int HitCount
        {
            get => m_hitCount;
            protected set
            {
                if(m_hitCount != value)
                {
                    m_hitCount = value;
                }
            }
        }

        public float LastHitTime => m_lastHitTime;
        public float TimeSinceLastHit => Time.time - m_lastHitTime;
        public float TotalHitsForce
        {
            get => m_totalForce;
            protected set
            {
                if(m_totalForce != value)
                {
                    m_totalForce = value;
                }
            }
        }

        public float LastHitForce {
            get => m_lastHitForce;
            protected set
            {
                if(m_lastHitForce != value)
                {
                    m_lastHitForce = value;
                    if(value > 0)
                    {
                        HitCount++;
                        m_lastHitTime = Time.time;
                        m_totalForce += value;
                    }
                }
            }
        }

        public void ResetAll()
        {
            for (int i = 0; i < m_events.Length; i++)
            {
                m_events[i].Counter = 0;
            }
            HitCount = 0;
            TotalHitsForce = 0;
            m_lastHitForce = 0;
        }

        public void AbsorbHit(object source, float force, ContactPoint point)
        {
            if (source is Component c && m_receiveHitsFrom.Contains(c.tag))
            {
                if (force >= m_minForceForHit)
                {
                    LastHitForce = force;
                    RemainingHealth -= force;
                    m_onAnyHit.Invoke(force);
                }

                for (int i = 0; i < m_events.Length; i++)
                {
                    m_events[i].TryApplyForce(force);
                }
            }
        }

        protected virtual void OnEnable()
        {
            RemainingHealth = m_health;
        }

        [Serializable]
        public struct TagArray
        {
            public string tags;

            public bool Contains(string tag) => tags.Contains(tag);
        }

        [Serializable]
        private class HitEvent
        {
            public int minHits;
            public float minForce;
            public bool repeat;
            public UnityEvent onEnoughHits;

            public void TryApplyForce(float force)
            {
                if(force >= minForce)
                {
                    Counter++;
                }
            }

            private bool m_canRaise = true;
            [SerializeField]
            [ShowAsReadOnly]
            private int m_counter;
            public int Counter {
                get => m_counter;
                set
                {
                    if(m_counter != value)
                    {
                        //m_canRaise = m_canRaise || minForce > value;
                        m_counter = value;
                        if(m_canRaise && m_counter >= minHits)
                        {
                            m_canRaise = false;
                            onEnoughHits.Invoke();
                            if (repeat)
                            {
                                m_canRaise = true;
                                m_counter = 0;
                            }
                        }
                    }
                }
            }
        }
    }
}
