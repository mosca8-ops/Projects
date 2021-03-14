namespace TXT.WEAVR.Common
{
    using UnityEngine;

    [AddComponentMenu("WEAVR/Components/Float Event Forwarder")]
    public class FloatEventForwarder : MonoBehaviour
    {

        public float scaleToApply = 1;
        public UnityEventFloat forwardEvent;

        private float m_originalValue;
        private float m_lastValue;
        private float m_max;

        private void OnValidate()
        {
            scaleToApply = Mathf.Max(scaleToApply, 0.00001f);
        }

        private void Start()
        {
            m_max = 1f / scaleToApply;
        }

        public void ResetTo(float value)
        {
            m_originalValue = value;
        }

        public void Clamp01_NoForwarding(float value)
        {
            m_originalValue = Mathf.Clamp01(value);
        }

        public void ClampScaled_NoForwarding(float value)
        {
            m_originalValue = Mathf.Clamp(value, 0, m_max);
        }

        public void Clamp01_NoForwarding()
        {
            m_originalValue = Mathf.Clamp01(m_originalValue);
        }

        public void ClampScaled_NoForwarding()
        {
            m_originalValue = Mathf.Clamp(m_originalValue, 0, m_max);
        }

        public void ResetSavedValue()
        {
            m_originalValue = m_lastValue = 0;
        }

        public void Forward(float value)
        {
            m_originalValue = value;
            forwardEvent.Invoke(value * scaleToApply);
        }

        public void Clamp01(float value)
        {
            m_originalValue = value;
            forwardEvent.Invoke(Mathf.Clamp01(m_originalValue * scaleToApply));
        }

        public void ClampScaled(float value)
        {
            m_originalValue = Mathf.Clamp(value, 0, m_max);
            forwardEvent.Invoke(Mathf.Clamp01(m_originalValue * scaleToApply));
        }

        public void InverseClampScaled(float value)
        {
            m_originalValue = Mathf.Clamp(m_max - value, 0, m_max);
            forwardEvent.Invoke(Mathf.Clamp01(m_originalValue * scaleToApply));
        }

        public void InverseClamp01(float value)
        {
            m_originalValue = value;
            forwardEvent.Invoke(Mathf.Clamp01(1 - m_originalValue * scaleToApply));
        }

        public void Add(float value)
        {
            value = m_originalValue + value;
            forwardEvent.Invoke(value * scaleToApply);
            m_originalValue = value;
        }

        public void AddClamped01(float value)
        {
            value = m_originalValue + value;
            forwardEvent.Invoke(Mathf.Clamp01(value * scaleToApply));
            m_originalValue = Mathf.Clamp01(value);
        }

        public void AddClampedScaled(float value)
        {
            value = Mathf.Clamp(m_originalValue + value, 0, m_max);
            forwardEvent.Invoke(value * scaleToApply);
            m_originalValue = value;
        }

        public void AddDeltaClampedScaled(float value)
        {
            float newValue = Mathf.Clamp(m_originalValue + value - m_lastValue, 0, m_max);
            m_lastValue = value;
            forwardEvent.Invoke(newValue * scaleToApply);
            m_originalValue = newValue;
        }

        public void Substract(float value)
        {
            value = m_originalValue - value;
            forwardEvent.Invoke(value * scaleToApply);
            m_originalValue = value;
        }

        public void SubstractClamped01(float value)
        {
            value = m_originalValue - value;
            forwardEvent.Invoke(Mathf.Clamp01(value * scaleToApply)); 
            m_originalValue = Mathf.Clamp01(value);
        }

        public void SubstractClampedScaled(float value)
        {
            value = Mathf.Clamp(m_originalValue - value, 0, m_max);
            forwardEvent.Invoke(value * scaleToApply);
            m_originalValue = value;
        }

        public void SubstractDeltaClampedScaled(float value)
        {
            float newValue = Mathf.Clamp(m_originalValue - (value - m_lastValue), 0, m_max);
            m_lastValue = value;
            forwardEvent.Invoke(newValue * scaleToApply);
            m_originalValue = newValue;
        }
    }
}