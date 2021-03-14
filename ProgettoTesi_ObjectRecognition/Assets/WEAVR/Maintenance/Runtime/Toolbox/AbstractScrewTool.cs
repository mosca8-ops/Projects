namespace TXT.WEAVR.Maintenance
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Common;
    using TXT.WEAVR.Interaction;
    using UnityEngine;

    public abstract class AbstractScrewTool : MaintenanceTool
    {
        public enum RotationDirection { Clockwise = -1, CounterClockwise = 1 }

        private const float k_limitReturnSpeedMultiplier = 2;

        public string toolName = "ScrewTool";
        public string operation = "Screw";
        public float screwStep = 0.2f;

        public RotationDirection currentDirection = RotationDirection.Clockwise;

        public bool animateScrewing = true;
        [DisabledBy("animateScrewing")]
        [Tooltip("Rotation velocity in degrees")]
        public float rotationSpeed = 180;
        [DisabledBy("animateScrewing")]
        [Tooltip("Rotation in degrees to have 1 N/m")]
        public float rotationPerTorque = 12f;
        [DisabledBy("animateScrewing")]
        [Tooltip("Whether to use limits or not. E.g. Dynamometric wrench")]
        public bool useLimits = false;
        [DisabledBy("animateScrewing;useLimits")]
        [Tooltip("The angle in degrees at which the tool will return back")]
        public float angleLimit = 60;

        protected AbstractScrewable _currentScrewable;
        private float _angleToRotate;
        private float _currentAngleRotation;
        private bool _returnMode;

        private float m_lastScrewValue;

        protected AbstractOperable m_operable;
        protected AbstractExecutable m_executable;

        protected AbstractInteractiveBehaviour m_lastConnected;

        private float _targetAutoValue;
        public float AutoValue {
            get {
                return _targetAutoValue;
            }
            set {
                if (_currentScrewable != null)
                {
                    _targetAutoValue = value;
                    if (m_operable != null)
                    {
                        m_operable.AutoValue = value;
                    }
                    else
                    {
                        ValueChangerMenu.Show(transform, true, "Screw Value", _currentScrewable.value,
                                            _currentScrewable.limits.min, _currentScrewable.limits.max,
                                            screwStep, UpdateScrewableValue)
                                        .SetAutomaticTargetValue(value, screwStep * 5);
                    }
                }
            }
        }

        private void Connectable_ConnectionChanged(AbstractConnectable source, AbstractInteractiveBehaviour previous, AbstractInteractiveBehaviour current, AbstractConnectable otherConnectable)
        {
            _angleToRotate = 0;
            _currentAngleRotation = 0;
            if (current == null)
            {
                return;
            }

            _currentScrewable = current.GetComponent<AbstractScrewable>();
            if (_currentScrewable == null)
            {
                return;
            }
            if (_controller == null || !_controller.enabled)
            {
                return;
            }
            m_lastConnected = current;

            m_lastScrewValue = _currentScrewable.value;

            if (CheckIfCanExecute())
            {
                Execute();
            }
        }

        protected abstract void Execute();

        protected virtual bool CanUpdate()
        {
            return true;
        }

        // Update is called once per frame
        void Update()
        {
            if (!CanUpdate())
            {
                return;
            }

            if (animateScrewing && _connectable.IsConnected)
            {
                if (_returnMode)
                {
                    float angleDelta = Mathf.MoveTowards(0, _currentAngleRotation, rotationSpeed * k_limitReturnSpeedMultiplier * Time.deltaTime);
                    transform.RotateAround(_connectable.ConnectionPoint.position, _connectable.ConnectionPoint.forward, -angleDelta);
                    _currentAngleRotation -= angleDelta;

                    _returnMode = _currentAngleRotation != 0;
                }
                else
                {
                    float angleDelta = Mathf.MoveTowards(0, _angleToRotate, rotationSpeed * Time.deltaTime);
                    transform.RotateAround(_connectable.ConnectionPoint.position, _connectable.ConnectionPoint.forward, angleDelta);
                    _angleToRotate -= angleDelta;

                    _currentAngleRotation += angleDelta;
                    if (useLimits && Mathf.Abs(_currentAngleRotation) > angleLimit)
                    {
                        _returnMode = true;
                        _angleToRotate = 0;
                    }
                }
            }
        }

        protected void UpdateScrewableValue(float newValue)
        {
            _angleToRotate += (newValue - _currentScrewable.value) * rotationPerTorque * (int)_currentScrewable.screwDirection * (int)currentDirection;
            float value = newValue - m_lastScrewValue;
            if (currentDirection == RotationDirection.Clockwise && value > 0)
            {
                _currentScrewable.value += value;
            }
            else if (currentDirection == RotationDirection.CounterClockwise && value < 0)
            {
                _currentScrewable.value += value;
            }
            m_lastScrewValue = newValue;
            if (m_operable == null)
            {
                if (_currentScrewable.IsValidValue)
                {
                    ValueChangerMenu.Instance.ValueColor = _currentScrewable.validColor;
                }
                else if (_currentScrewable.IsCriticalValue)
                {
                    ValueChangerMenu.Instance.ValueColor = _currentScrewable.criticalColor;
                }
                else
                {
                    ValueChangerMenu.Instance.ValueColor = Color.clear;
                }
            }
        }

        protected override ObjectClass GetDefaultObjectClass()
        {
            return new ObjectClass() { type = toolName };
        }

        private bool Executable_ConditionToExecute(GameObject arg1, ObjectsBag arg2)
        {
            return CheckIfCanExecute();
        }

        private bool CheckIfCanExecute()
        {
            if (!_connectable.IsConnected || _currentScrewable == null)
            {
                return false;
            }
            foreach (var connectable in _currentScrewable.GetComponents<AbstractConnectable>())
            {
                if (connectable != _connectable.ConnectedObject && connectable.IsConnected)
                {
                    // Update operable values
                    if (m_operable != null)
                    {
                        m_operable.limits.Resize(_currentScrewable.limits);
                        m_operable.valid.Resize(_currentScrewable.valid);
                        m_operable.validColor = _currentScrewable.validColor;
                        m_operable.notValidColor = _currentScrewable.criticalColor;
                        m_operable.Value = _currentScrewable.value;
                    }
                    return true;
                }
            }
            return false;
        }

        protected override Type[] GetRequiredComponentsTypes()
        {
            return null;
        }

        protected void OnGrab()
        {
            //if()
        }

        protected void OnUngrab()
        {
            if (_connectable.IsConnected)
            {
                _connectable.ConnectedObject.Controller.enabled = false;
            }
            else if (m_lastConnected != null)
            {
                m_lastConnected.Controller.enabled = true;
            }
        }

        protected override void SetupEventHandlers()
        {
            if (m_operable == null)
            {
                m_operable = GetComponent<AbstractOperable>();
            }

            m_operable.operationName = operation;
            m_operable.InteractCondition = CheckIfCanExecute;
            m_operable.property = "Screw Value";
            //m_operable.onValueChange.AddListener(UpdateScrewableValue);
            m_operable.valueStep = screwStep;

            m_operable.Filter = f =>
            {
                if (_currentScrewable == null) { return f; }
                UpdateScrewableValue(f);
                return _currentScrewable.value;
            };

            if (m_executable == null)
            {
                m_executable = GetComponent<AbstractExecutable>();
            }

            m_executable.commandName = "Change Direction";
            //m_executable.ConditionToExecute += Executable_ConditionToExecute;
            m_executable.onExecute.AddListener(() => currentDirection = currentDirection == RotationDirection.Clockwise
                                                                      ? RotationDirection.CounterClockwise : RotationDirection.Clockwise);

            _connectable.ConnectionChanged -= Connectable_ConnectionChanged;
            _connectable.ConnectionChanged += Connectable_ConnectionChanged;

            _grabbable.onGrab.AddListener(OnGrab);
            _grabbable.onUngrab.AddListener(OnUngrab);
        }


    }
}