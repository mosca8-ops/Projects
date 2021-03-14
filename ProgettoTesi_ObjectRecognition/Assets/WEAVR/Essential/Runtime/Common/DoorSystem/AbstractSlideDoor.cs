using UnityEngine;

namespace TXT.WEAVR.Common
{
    public abstract class AbstractSlideDoor : AbstractDoor
    {
        [Header("Slide Door Data")]
        [SerializeField]
        [Tooltip("The distance from the closed point (fully open point) in percentage for the door to be considered closed (fully open)")]
        [Range(0.000001f, 0.4999999f)]
        protected float m_closedThreshold = 0.1f;
        [Space]
        [SerializeField]
        [Draggable]
        [CanBeGenerated(Relationship.Sibling)]
        protected Transform m_closingPoint;
        [SerializeField]
        [Draggable]
        [CanBeGenerated(Relationship.Sibling)]
        protected Transform m_fullyOpenedPoint;

        [Space]
        [SerializeField]
        protected bool m_maintainMomemntum = true;
        [SerializeField]
        [HiddenBy(nameof(m_maintainMomemntum))]
        protected float m_momemtumDampenRate = 5.0f;

        protected float m_initialMappingOffset;
        protected int m_numMappingChangeSamples = 5;
        protected float[] m_mappingChangeSamples;
        protected float m_mappingChangeRate;
        protected int m_sampleCount = 0;

        protected bool m_isValid;
        protected float m_closedThresholdDistance;

        protected override void Reset()
        {
            base.Reset();
            InstantiateTransform(ref m_closingPoint, $"{name}_ClosingPoint", true);
            InstantiateTransform(ref m_fullyOpenedPoint, $"{name}_FullyOpenedPoint", true);

            SnapshotClosed();
            SnapshotFullyOpen();

            Controller.DefaultBehaviour = this;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            m_isValid = m_closingPoint && m_fullyOpenedPoint;
            UpdateClosedDistanceThreshold();
        }

        private void UpdateClosedDistanceThreshold()
        {
            if (m_isValid)
            {
                m_closedThresholdDistance = Vector3.Distance(m_closingPoint.position, m_fullyOpenedPoint.position) * m_closedThreshold;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            m_mappingChangeSamples = new float[m_numMappingChangeSamples];
        }

        protected override void AnimateDoorMovement(float progress)
        {
            if (m_isValid)
            {
                Vector3 newPosition = Vector3.MoveTowards(m_closingPoint.position, 
                                                          m_fullyOpenedPoint.position, 
                                                          progress * Vector3.Distance(m_closingPoint.position, m_fullyOpenedPoint.position));
                Quaternion newRotation = Quaternion.Slerp(m_closingPoint.rotation, m_fullyOpenedPoint.rotation, progress);
                if (m_rigidBody && !m_rigidBody.isKinematic)
                {
                    m_rigidBody.MovePosition(newPosition);
                    m_rigidBody.MoveRotation(newRotation);
                }
                else
                {
                    transform.position = newPosition;
                    transform.rotation = newRotation;
                }
            }
        }

        protected override void UpdateState()
        {
            if (m_isValid)
            {
                IsClosed = Vector3.Distance(transform.position, m_closingPoint.position) < m_closedThresholdDistance;
                IsFullyOpened = Vector3.Distance(transform.position, m_fullyOpenedPoint.position) < m_closedThresholdDistance;
                if (!IsClosed && m_maintainMomemntum && m_mappingChangeRate != 0.0f)
                {
                    //Dampen the mapping change rate and apply it to the mapping
                    m_mappingChangeRate = Mathf.Lerp(m_mappingChangeRate, 0.0f, m_momemtumDampenRate * Time.deltaTime);
                    CurrentOpenProgress = Mathf.Clamp01(m_currentOpening + (m_mappingChangeRate * Time.deltaTime));

                    AnimateDoorMovement(m_currentOpening);
                }
            }
        }

        public override void SnapshotClosed()
        {
            base.SnapshotClosed();
            if (m_closingPoint)
            {
                m_closingPoint.position = transform.position;
                m_closingPoint.rotation = transform.rotation;
            }
        }

        public override void SnapshotFullyOpen()
        {
            base.SnapshotFullyOpen();
            if (m_fullyOpenedPoint)
            {
                m_fullyOpenedPoint.position = transform.position;
                m_fullyOpenedPoint.rotation = transform.rotation;
            }
        }

        protected override void Start()
        {
            base.Start();
            m_isValid = m_closingPoint && m_fullyOpenedPoint;

            if (m_isValid)
            {
                UpdateClosedDistanceThreshold();
                CurrentOpenProgress = m_initialMappingOffset = Mathf.Clamp01(Vector3.Distance(transform.position, m_closingPoint.position)
                                                                         / Vector3.Distance(m_fullyOpenedPoint.position, m_closingPoint.position));
                UpdateLinearMapping(transform);
            }
            InteractTrigger = Interaction.BehaviourInteractionTrigger.OnPointerDown;

            OnClosed.AddListener(Controller.StopCurrentInteraction);
            OnFullyOpened.AddListener(Controller.StopCurrentInteraction);
        }

        protected override void OnDestroy()
        {
            DestroyPoint(m_closingPoint);
            DestroyPoint(m_fullyOpenedPoint);

            base.OnDestroy();
        }

        protected void InstantiateTransform(ref Transform point, string name, bool removeGenerator)
        {
            if (removeGenerator && point != null)
            {
                DestroyPoint(point);
            }
            if (point == null)
            {
                point = new GameObject(name).transform;
                var generated = point.gameObject.AddComponent<GeneratedObject>();
                generated.Generator = this;
                //transform.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }

            point.SetParent(transform.parent, false);
            point.SetSiblingIndex(transform.GetSiblingIndex() + 1);
        }

        protected void DestroyPoint(Transform point)
        {
            if (point != null)
            {
                var generated = point.GetComponent<GeneratedObject>();
                if (generated != null && generated.Generator == this)
                {
                    DestroyImmediate(point.gameObject);
                }
            }
        }

        protected void CalculateMappingChangeRate()
        {
            //Compute the mapping change rate
            m_mappingChangeRate = 0.0f;
            int mappingSamplesCount = Mathf.Min(m_sampleCount, m_mappingChangeSamples.Length);
            if (mappingSamplesCount != 0)
            {
                for (int i = 0; i < mappingSamplesCount; ++i)
                {
                    m_mappingChangeRate += m_mappingChangeSamples[i];
                }
                m_mappingChangeRate /= mappingSamplesCount;
            }
        }


        //-------------------------------------------------
        protected void UpdateLinearMapping(Transform tr)
        {
            float prevMapping = m_currentOpening;
            CurrentOpenProgress = Mathf.Clamp01(m_initialMappingOffset + CalculateLinearMapping(tr));

            m_mappingChangeSamples[m_sampleCount % m_mappingChangeSamples.Length] = (1.0f / Time.deltaTime) * (m_currentOpening - prevMapping);
            m_sampleCount++;

            AnimateDoorMovement(m_currentOpening);
        }


        //-------------------------------------------------
        protected float CalculateLinearMapping(Transform tr)
        {
            Vector3 direction = m_fullyOpenedPoint.position - m_closingPoint.position;
            float length = direction.magnitude;
            direction.Normalize();

            Vector3 displacement = tr.position - m_closingPoint.position;

            return Vector3.Dot(displacement, direction) / length;
        }
    }
}
