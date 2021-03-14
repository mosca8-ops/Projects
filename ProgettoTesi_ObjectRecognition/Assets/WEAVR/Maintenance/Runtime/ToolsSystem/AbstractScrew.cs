using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Interaction;
using UnityEngine;


namespace TXT.WEAVR.MaintenanceTools
{

    public abstract class AbstractScrew : AbstractInteractiveBehaviour
    {
        public enum ScrewState
        {
            Free,
            Unscrewed,
            Screwed
        }

        [SerializeField]
        [Range(0.01f, 0.99f)]
        private float m_screwThreshold = 0.05f;
        private ScrewState m_state;

        private float m_screwValue; // 0 -> unsrewed, 1 -> fully screwd
        private AbstractScrewTool m_currentTool;

        public ScrewState State
        {
            get => m_state;
            set
            {
                if(m_state != value)
                {
                    TransitionToState(value);
                }
            }
        }

        public bool IsFullyScrewed => State == ScrewState.Screwed;
        public bool IsFree => State == ScrewState.Free;
        
        public virtual float ScrewValue
        {
            get => m_screwValue;
            set
            {
                if(m_screwValue != value)
                {
                    m_screwValue = Mathf.Clamp01(value);
                    if(m_screwValue < m_screwThreshold)
                    {
                        TransitionToState(ScrewState.Screwed);
                    }

                }
            }
        }

        private void TransitionToState(ScrewState value)
        {
            throw new NotImplementedException();
        }

        public override string GetInteractionName(ObjectsBag currentBag)
        {
            throw new System.NotImplementedException();
        }

        public override void Interact(ObjectsBag currentBag)
        {
            throw new System.NotImplementedException();
        }

        private IEnumerator ScrewUpdateCoroutine()
        {
            if (m_currentTool && m_currentTool.RotationTip)
            {
                var tip = m_currentTool.RotationTip;
                var currentRotation = tip.rotation;
                var lastRight = tip.right;
                float lastTime = Time.time;

                Vector3 screwHeadForward = Vector3.zero;

                float deltaTime;
                while (true)
                {
                    yield return null;
                    deltaTime = Time.time - lastTime;
                    lastTime = Time.time;
                    Quaternion.FromToRotation(lastRight, tip.right).ToAngleAxis(out float angle, out Vector3 axis);
                    var dotProduct = Vector3.Dot(axis, screwHeadForward);
                    if (dotProduct > 0)
                    {
                        // Apply delta angle
                        angle = angle * dotProduct;
                    }
                }
            }
        }
    }
}
