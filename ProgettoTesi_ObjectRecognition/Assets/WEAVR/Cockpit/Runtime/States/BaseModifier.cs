using System;
using System.Linq;

namespace TXT.WEAVR.Cockpit
{
    using UnityEngine;
    using UnityEngine.EventSystems;

    public abstract class BaseModifier : BaseModifierState
    {
        public enum ModifierAxis
        {
            X,
            Y,
            Z
        }

        public enum MappingMode
        {
            Factor,
            Interpolation
        };

        [Serializable]
        public struct InterpolationPoint
        {
            public float SimulationValue;
            public float SceneValue;
        }

        [SerializeField]
        protected ModifierAxis m_ModifierAxis = ModifierAxis.Y;
        [SerializeField]
        protected MappingMode m_MappingMode;
        [SerializeField]
        protected InterpolationPoint[] m_InterpolationPoints;


        private float InterpolateFromScene(float iValue)
        {
            if (m_InterpolationPoints.Length >= 2)
            {
                if (iValue <= m_InterpolationPoints[0].SceneValue)
                {
                    return m_InterpolationPoints[0].SimulationValue;
                }

                if (iValue >= m_InterpolationPoints.Last().SceneValue)
                {
                    return m_InterpolationPoints.Last().SimulationValue;
                }

                for (int i = 0; i < m_InterpolationPoints.Length - 1; ++i)
                {
                    if (iValue >= m_InterpolationPoints[i].SceneValue &&
                        iValue < m_InterpolationPoints[i + 1].SceneValue)
                    {
                        float wSimulationDelta = m_InterpolationPoints[i + 1].SimulationValue -
                                                 m_InterpolationPoints[i].SimulationValue;
                        float wSceneDelta = m_InterpolationPoints[i + 1].SceneValue -
                                            m_InterpolationPoints[i].SceneValue;
                        float wSlope = wSimulationDelta / wSceneDelta;
                        return m_InterpolationPoints[i].SimulationValue +
                               wSlope * (iValue - m_InterpolationPoints[i].SceneValue);
                    }
                }
            }
            Debug.LogError("Invalid Interpolation");
            return 0.0f;
        }

        private float InterpolateFromSimulation(float iValue)
        {
            if (m_InterpolationPoints.Length >= 2)
            {
                if (iValue <= m_InterpolationPoints[0].SimulationValue)
                {
                    return m_InterpolationPoints[0].SceneValue;
                }

                if (iValue >= m_InterpolationPoints.Last().SimulationValue)
                {
                    return m_InterpolationPoints.Last().SceneValue;
                }

                for (int i = 0; i < m_InterpolationPoints.Length - 1; ++i)
                {
                    if (iValue >= m_InterpolationPoints[i].SimulationValue &&
                        iValue < m_InterpolationPoints[i + 1].SimulationValue)
                    {
                        float wSimulationDelta = m_InterpolationPoints[i + 1].SimulationValue -
                                                 m_InterpolationPoints[i].SimulationValue;
                        float wSceneDelta = m_InterpolationPoints[i + 1].SceneValue - m_InterpolationPoints[i].SceneValue;
                        float wSlope = wSceneDelta / wSimulationDelta;
                        return m_InterpolationPoints[i].SceneValue +
                               wSlope * (iValue - m_InterpolationPoints[i].SimulationValue);
                    }
                }
            }
            Debug.LogError("Invalid Interpolation");
            return 0.0f;
        }


        private float GetSceneValueFromSimulation(float iValue)
        {
            switch (m_MappingMode)
            {
                case MappingMode.Factor:
                    return iValue * ValueFactor;
                case MappingMode.Interpolation:
                    return InterpolateFromSimulation(iValue);
            }
            return 0.0f;
        }

        private float GetSimulationValueFromScene(float iValue)
        {
            switch (m_MappingMode)
            {
                case MappingMode.Factor:
                    return iValue / ValueFactor;
                case MappingMode.Interpolation:
                    return InterpolateFromScene(iValue);
            }
            return 0.0f;
        }

        protected abstract float Tolerance { get; }

        [SerializeField]
        private bool _hasLimits;
        public bool HasLimits
        {
            get => _hasLimits;
            set
            {
                if (_hasLimits != value)
                {
                    _hasLimits = value;
                    UpdateLimits();
                }
            }
        }

        [SerializeField]
        private float _minLimit;
        public float MinLimit
        {
            get => _minLimit;
            set
            {
                if (Math.Abs(_minLimit - value) > Tolerance)
                {
                    _minLimit = value;
                    UpdateLimits();
                }
            }
        }

        [SerializeField]
        private float _maxLimit;
        public float MaxLimit
        {
            get => _maxLimit;
            set
            {
                if (Math.Abs(_maxLimit - value) > Tolerance)
                {
                    _maxLimit = value;
                    UpdateLimits();
                }
            }
        }

        [SerializeField]
        private float _valueFactor = 1;
        public float ValueFactor
        {
            get => _valueFactor;
            set
            {
                if (Math.Abs(_valueFactor - value) > Tolerance && Math.Abs(value) > Tolerance)
                {
                    _valueFactor = value;
                    UpdateLimits();
                }
            }
        }

        private float _minDegrees;
        private float _maxDegrees;
        protected bool _useDeltaX;

        protected float PreviousModifiedValue => previousValue;

        protected float ModifiedValue
        {
            get => currentValue;
            set
            {
                var wNewValue = value;
                ClampValue(ref wNewValue);
                Value = GetSimulationValueFromScene(wNewValue);
            }
        }

        public override object Value
        {
            get => Convert.ToSingle(base.Value);
            set
            {
                previousValue = currentValue;
                currentValue = GetSceneValueFromSimulation(Convert.ToSingle(value));
                UpdateLimits();
                base.Value = value;
            }
        }

        private float currentValue;
        private float previousValue;

        public override bool UseOwnerEvents => true;

        public override bool OnPointerBeginDrag(PointerEventData eventData)
        {
            _useDeltaX = Mathf.Abs(eventData.delta.x) > Mathf.Abs(eventData.delta.y);
            return true;
        }

        protected void Awake()
        {
            ValueChanged -= ValueChangedHandler;
            ValueChanged += ValueChangedHandler;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_hasLimits)
            {
                UpdateLimits();
            }
        }

        protected virtual void ValueChangedHandler(BaseModifierState iSender, object iLastValue, object iNewValue)
        {
            UpdateScene();
        }



        private void ClampValue(ref float iValue)
        {
            if (!_hasLimits) { return; }
            if (iValue < _minLimit)
            {
                iValue = _minLimit;
            }
            else if (iValue > _maxLimit)
            {
                iValue = _maxLimit;
            }
        }

        protected virtual void UpdateLimits()
        {
            ClampValue(ref currentValue);
        }

        public abstract override bool OnPointerDrag(PointerEventData eventData);
        protected abstract void UpdateScene();

    }
}