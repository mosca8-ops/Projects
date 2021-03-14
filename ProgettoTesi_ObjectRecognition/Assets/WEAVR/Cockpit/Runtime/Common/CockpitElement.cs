namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;

    [Serializable]
    [Obsolete("Use Element which is newer and more customizable")]
    [AddComponentMenu("")]
    public abstract class CockpitElement : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
    {
        protected Animator _animator;
        protected Rigidbody _rigidBody;

        private Coroutine _moveCoroutine;

        [SerializeField]
        private Binding _binding;
        public Binding Binding {
            get {
                if (_binding == null)
                {
                    _binding = ScriptableObject.CreateInstance<Binding>();
                    _binding.id = "Variable";
                }
                return _binding;
            }
        }

        [SerializeField]
        [HideInInspector]
        protected List<ElementState> _states;

        public List<ElementState> EditorStates {
            get {
                if (_states == null)
                {
                    _states = new List<ElementState>();
                }
                return _states;
            }
        }

        public Animator EditorAnimator {
            get {
                if (_animator == null)
                {
                    _animator = GetComponent<Animator>();
                }
                return _animator;
            }
        }

        /// <summary>
        /// Whether to show and edit states
        /// </summary>
        public virtual bool EditorEditableStates {
            get {
                return AutoRegisterStates();
            }
        }

        public virtual Enum EditorEnumState {
            get { return GetDefaultState(); }
            set { SetInitialState(value); }
        }

        public float StateChangeTime {
            get {
                return _stateChangeTime;
            }
            set {
                _stateChangeTime = Mathf.Max(0.1f, value);
            }
        }

        [SerializeField]
        protected float _stateChangeTime = 1f;

        protected virtual void Awake()
        {
            if (_states == null)
            {
                _states = new List<ElementState>();
            }
            _animator = GetComponent<Animator>();
            _rigidBody = GetComponent<Rigidbody>();

            if (!AutoRegisterStates())
            {
                RegisterStates();
            }
        }

        protected virtual void Start()
        {
            if (Binding.Property != null && Binding.mode != BindingMode.Write)
            {
                Binding.Property.ValueChanged += Property_ValueChanged;
            }
        }

        private void Property_ValueChanged(Core.Property property, object oldValue, object newValue)
        {
            string newValString = newValue != null ? newValue.ToString() : "";
            foreach (var state in _states)
            {
                if (state == null || state.Value == null) { continue; }
                if (state.Value.Equals(newValString))
                {
                    ChangeToNewState(state);
                }
            }
        }

        private void Update()
        {
            if (Binding.Property != null && Binding.mode != BindingMode.Write)
            {
                Property_ValueChanged(Binding.Property, null, Binding.Property.Value);
            }
        }

        protected virtual void ChangeToNewState(ElementState newState)
        {
            ApplyChangeState(newState);
        }

        protected abstract void RegisterStates();

        protected abstract bool AutoRegisterStates();

        protected abstract Enum GetDefaultState();

        protected virtual void SetInitialState(Enum state) { }

        public bool RegisterState(ElementState state)
        {
            foreach (var s in _states)
            {
                if (s.state == state.state)
                {
                    return false;
                }
            }
            _states.Add(state);
            return true;
        }

        public bool RegisterState(Enum state, Vector3 position, Quaternion? rotation = null, bool skippable = true)
        {
            return RegisterState(state.ToString(), position, rotation, skippable);
        }

        public bool RegisterState(string name, Vector3 position, Quaternion? rotation = null, bool skippable = true)
        {
            _states.Add(new ElementState()
            {
                state = name,
                position = position,
                rotation = rotation ?? transform.rotation,
                skippable = skippable,
                useAnimator = false
            });
            return true;
        }

        public bool RegisterState(Enum state, string parameterName, float? value = null)
        {
            return RegisterState(state.ToString(), parameterName, value);
        }

        public bool RegisterState(string name, string parameterName, float? value = null)
        {
            if (_animator == null)
            {
                return false;
            }
            foreach (var parameter in _animator.parameters)
            {
                if (parameter.name == parameterName)
                {
                    var state = new ElementState()
                    {
                        state = name,
                        useAnimator = true,
                        skippable = false
                    };
                    if (value.HasValue)
                    {
                        state.parameter = new Common.AnimatorParameter(parameter, value.Value);
                        state.animatorParameterName = state.parameter.name;
                    }
                    _states.Add(state);
                    return true;
                }
            }
            return false;
        }

        protected virtual bool ChangeState(string newState)
        {
            foreach (var state in _states)
            {
                if (state.state.Equals(newState, StringComparison.OrdinalIgnoreCase))
                {
                    return ApplyChangeState(state);
                }
            }
            return false;
        }

        protected virtual bool ChangeState(int stateIndex)
        {
            if (stateIndex < 0 || stateIndex >= _states.Count)
            {
                return false;
            }
            return ApplyChangeState(_states[stateIndex]);
        }

        protected virtual bool ChangeState(Enum newState)
        {
            return ChangeState(newState.ToString());
        }

        private bool ApplyChangeState(ElementState state)
        {
            if (Binding.Property != null && Binding.mode != BindingMode.Read)
            {
                Binding.Property.Value = state.Value;
            }

            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }
            if (state.useAnimator && !string.IsNullOrEmpty(state.animatorParameterName) && state.parameter != null)
            {
                _animator.applyRootMotion = false;
                switch (state.parameter.type)
                {
                    case AnimatorControllerParameterType.Bool:
                        _animator.SetBool(state.parameter.hashId, state.parameter.boolValue);
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        _animator.SetTrigger(state.parameter.hashId);
                        break;
                    case AnimatorControllerParameterType.Float:
                        _animator.SetFloat(state.parameter.hashId, state.parameter.numericValue);
                        break;
                    case AnimatorControllerParameterType.Int:
                        _animator.SetInteger(state.parameter.hashId, (int)(state.parameter.numericValue));
                        break;

                }
                return true;
            }
            if (_animator != null)
            {
                _animator.applyRootMotion = true;
            }
            _moveCoroutine = StartCoroutine(MoveToPose(state.position, state.rotation));
            return true;
        }

        protected virtual IEnumerator MoveToPose(Vector3 position, Quaternion rotation)
        {
            if (_rigidBody == null)
            {
                var waitFixedUpdate = new WaitForFixedUpdate();
                while (transform.position != position)
                {
                    transform.localPosition = Vector3.MoveTowards(transform.localPosition, position, Time.fixedDeltaTime / _stateChangeTime);
                    transform.localRotation = Quaternion.RotateTowards(transform.localRotation, rotation, Time.fixedDeltaTime / _stateChangeTime * 360f);
                    yield return new WaitForFixedUpdate();
                }
            }
            else
            {
                var waitFixedUpdate = new WaitForFixedUpdate();
                while (transform.position != position)
                {
                    position = transform.localToWorldMatrix * position;
                    _rigidBody.position = Vector3.MoveTowards(_rigidBody.position, position, Time.fixedDeltaTime / _stateChangeTime);
                    _rigidBody.rotation = Quaternion.RotateTowards(_rigidBody.rotation, rotation, Time.fixedDeltaTime / _stateChangeTime * 360f);
                    yield return new WaitForFixedUpdate();
                }
            }
        }

        public abstract void OnPointerUp(PointerEventData eventData);

        public virtual void OnPointerDown(PointerEventData eventData)
        {

        }
    }
}