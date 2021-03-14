using System;
using TXT.WEAVR.Common;
using UnityEngine;

#if WEAVR_VR
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Interaction
{

    [AddComponentMenu("WEAVR/VR/Manipulators/Slider")]
    public class VR_Slider : VR_Manipulator
    {

        public Span defaultLimits = new Span(0, 1);

        //public LinearMapping linearMapping;
        [Tooltip("Reposition the game object according to value")]
        public bool repositionGameObject = true;
        [CanBeGenerated]
        [HiddenBy(nameof(repositionGameObject))]
        [Draggable]
        public Transform manipulate;
        [CanBeGenerated(createAs: Relationship.Sibling)]
        [HiddenBy(nameof(repositionGameObject))]
        [Draggable]
        public Transform startPosition;
        [CanBeGenerated(createAs: Relationship.Sibling)]
        [HiddenBy(nameof(repositionGameObject))]
        [Draggable]
        public Transform endPosition;

        public bool maintainMomemntum = true;
        public float momemtumDampenRate = 5.0f;

        private float m_initialValue;
        private float m_value;
        private int m_numMappingChangeSamples = 5;
        private float[] m_mappingChangeSamples;
        private float m_prevMapping = 0.0f;
        private float m_mappingChangeRate;
        private int m_sampleCount = 0;

        private Span currentLimits;

#if WEAVR_VR

        public override bool CanHandleData(object value)
        {
            return value is float || (value != null && float.TryParse(value.ToString(), out m_value));
        }

        public override void UpdateValue(float value)
        {
            m_value = value;
        }

        //-------------------------------------------------
        void Awake()
        {
            m_mappingChangeSamples = new float[m_numMappingChangeSamples];
        }

        //-------------------------------------------------
        void Start()
        {
            m_initialValue = defaultLimits.min;
            currentLimits = defaultLimits;
            if (manipulate == null)
            {
                manipulate = transform;
            }

            if (repositionGameObject)
            {
                UpdateLinearMapping(manipulate.transform);
            }
        }

        //-------------------------------------------------
        private void CalculateMappingChangeRate()
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
        private void UpdateLinearMapping(Transform tr)
        {
            m_prevMapping = m_value;
            m_value = Mathf.Clamp01(m_initialValue + CalculateLinearMapping(tr));

            m_mappingChangeSamples[m_sampleCount % m_mappingChangeSamples.Length] = (1.0f / Time.deltaTime) * (m_value - m_prevMapping);
            m_sampleCount++;

            if (repositionGameObject)
            {
                manipulate.transform.position = Vector3.Lerp(startPosition.position, endPosition.position, m_value);
            }
        }


        //-------------------------------------------------
        private float CalculateLinearMapping(Transform tr)
        {
            Vector3 direction = endPosition.position - startPosition.position;
            float length = direction.magnitude;
            direction.Normalize();

            Vector3 displacement = tr.position - startPosition.position;

            return Vector3.Dot(displacement, direction) / length;
        }


        //-------------------------------------------------
        void Update()
        {
            if (!m_isActive) { return; }
            if (maintainMomemntum && m_mappingChangeRate != 0.0f)
            {
                //Dampen the mapping change rate and apply it to the mapping
                m_mappingChangeRate = Mathf.Lerp(m_mappingChangeRate, 0.0f, momemtumDampenRate * Time.deltaTime);
                m_value = Mathf.Clamp01(m_value + (m_mappingChangeRate * Time.deltaTime));

                if (repositionGameObject)
                {
                    manipulate.transform.position = Vector3.Lerp(startPosition.position, endPosition.position, m_value);
                }
            }

            m_floatSetter?.Invoke(currentLimits.Denormalize(m_value));
        }

        public override void StartManipulating(Hand hand, Interactable interactable, bool iIsKeepPressedLogic, Func<float> getter, Action<float> setter, Span? span = null)
        {
            base.StartManipulating(hand, interactable, iIsKeepPressedLogic, getter, setter, span);

            currentLimits = span ?? defaultLimits;

            if (m_floatGetter != null)
            {
                m_value = currentLimits.Normalize(m_floatGetter());
                if (repositionGameObject)
                {
                    var direction = endPosition.position - startPosition.position;
                    manipulate.transform.position = startPosition.position + direction * m_value;
                }
            }

            m_initialValue = m_value - CalculateLinearMapping(hand.transform);
            m_sampleCount = 0;
            m_mappingChangeRate = 0.0f;

        }

        protected override void UpdateManipulator(Hand hand, Interactable interactable)
        {
            UpdateLinearMapping(hand.transform);
        }

        public override void StopManipulating(Hand hand, Interactable interactable)
        {
            base.StopManipulating(hand, interactable);
            CalculateMappingChangeRate();
        }

#endif

    }
}

