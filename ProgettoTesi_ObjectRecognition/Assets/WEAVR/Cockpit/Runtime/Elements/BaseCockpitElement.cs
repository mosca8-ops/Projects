namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Core;
    using UnityEngine;
    using UnityEngine.EventSystems;

    [Serializable]
    public abstract class BaseCockpitElement : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler/*, IPointerEnterHandler, IPointerExitHandler*/
    {

        #region [  GENERAL  ]

        public Vector3 defaultLocalPosition;
        public Quaternion defaultLocalRotation;

        [SerializeField]
        private string _elementType;
        [DoNotExpose]
        public virtual string ElementType {
            get {
                return _elementType;
            }
            set {
                _elementType = value;
            }
        }
        
        [SerializeField]
        private bool _isCustomizable = true;
        [DoNotExpose]
        public virtual bool IsCustomizable {
            get {
                return _isCustomizable;
            }
            protected set {
                _isCustomizable = value;
            }
        }

        public Rigidbody RigidBody { get; private set; }

        #endregion

        #region [  BINDINGS  ]

        [SerializeField]
        private List<Binding> _bindings;
        [DoNotExpose]
        public List<Binding> Bindings {
            get {
                if(_bindings == null || _bindings.Count == 0) {
                    _bindings = new List<Binding>();
                    var firstBinding = ScriptableObject.CreateInstance<Binding>();
                    firstBinding.id = "Variable";
                    _bindings.Add(firstBinding);
                }
                return _bindings;
            }
        }

        public virtual void AddBinding(string id) {
            var binding = ScriptableObject.CreateInstance<Binding>();
            binding.id = id;
            binding.mode = Bindings[0].mode;
            binding.dataSource = Bindings[0].dataSource;
            Bindings.Add(binding);
        }

        protected virtual void AddBinding(Binding binding) {
            Bindings.Add(binding);
        }

        public virtual void RemoveBinding(Binding binding) {
            if (Bindings.Count == 1) {
                // Do not remove
                return;
            }
            if (Bindings.Remove(binding)) {
                // Remove also from states
                foreach (var state in States) {
                    if (state.Binding == binding) {
                        state.Binding = Bindings[0];
                    }
                }
            }
        }

        public virtual void RemoveBinding(int index) {
            if (0 <= index && index < Bindings.Count) {
                RemoveBinding(Bindings[index]);
            }
        }

        #endregion

        #region [  STATES  ]
        [SerializeField]
        private BaseDiscreteState _startState;
        [DoNotExpose]
        public virtual BaseDiscreteState StartState {
            get {
                return _startState;
            }
            set {
                _startState = value;
            }
        }

        [SerializeField]
        private BaseDiscreteState _currentState;
        [HideInternals]
        [DynamicEnum("States", false)]
        public virtual BaseDiscreteState CurrentState {
            get {
                return _currentState;
            }
            set {
                if(_currentState != value && value != null && value.CanEnterState(_currentState)) {
                    if(_currentState != null) {
                        _currentState.OnStateExit(value);
                    }
                    var previousState = _currentState;
                    _currentState = value;
                    _currentState.OnStateEnter(previousState);
                }
            }
        }

        [SerializeField]
        private List<BaseDiscreteState> _states;
        [DoNotExpose]
        public List<BaseDiscreteState> States {
            get {
                if(_states == null) {
                    _states = new List<BaseDiscreteState>();
                    if (IsCustomizable) {
                        AddState(ScriptableObject.CreateInstance<IdleState>());
                    }
                }
                return _states;
            }
        }

        public virtual void RemoveState(BaseDiscreteState state) {
            if (States.Remove(state)) {
                state.OnRemove();
            }
        }

        public virtual void RemoveState(int index) {
            if (0 <= index && index < States.Count) {
                States.Remove(States[index]);
            }
        }

        public virtual void AddState(BaseDiscreteState state) {
            if (state == null) { return; }
            if (!States.Contains(state)) {
                _states.Add(state);
            }
            if (state.Owner != this && state.Owner != null) {
                state.Owner.RemoveState(state);
            }
            state.Owner = this;
        }

        #endregion

        #region [  MODIFIERS  ]

        [SerializeField]
        private List<BaseModifierState> _modifiers;
        [DoNotExpose]
        public List<BaseModifierState> Modifiers {
            get {
                if (_modifiers == null) {
                    _modifiers = new List<BaseModifierState>();
                }
                return _modifiers;
            }
        }

        public virtual void RemoveModifier(BaseModifierState modifier) {
            if (Modifiers.Remove(modifier)) {
                modifier.OnRemove();
            }
        }
        
        public virtual void RemoveModifier(int index) {
            if (0 <= index && index < Modifiers.Count) {
                Modifiers.Remove(Modifiers[index]);
            }
        }

        public virtual void AddModifier(BaseModifierState modifier) {
            if (modifier == null) { return; }
            if (!Modifiers.Contains(modifier)) {
                _modifiers.Add(modifier);
            }
            if (modifier.Owner != this && modifier.Owner != null) {
                modifier.Owner.RemoveModifier(modifier);
            }
            modifier.Owner = this;
        }

        #endregion

        #region [  ANIMATOR  ]

        public Animator animator;

        [SerializeField]
        private bool _useAnimator = false;
        [DoNotExpose]
        public virtual bool UseAnimator {
            get {
                return _useAnimator;
            }
            set {
                _useAnimator = value;
            }
        }

        #region Unused Code
        //[SerializeField]
        //private List<AnimatorControllerParameter> _animatorParameters;
        //[DoNotExpose]
        //public List<AnimatorControllerParameter> AnimatorParameters {
        //    get {
        //        if(_animatorParameters == null) {
        //            _animatorParameters = new List<AnimatorControllerParameter>();
        //        }
        //        return _animatorParameters;
        //    }
        //}

        //public virtual void UpdateAnimatorParameters(params AnimatorControllerParameter[] parameters) {
        //    AnimatorParameters.Clear();
        //    AnimatorParameters.AddRange(parameters);
        //}
        #endregion

        #endregion

        #region [  INTERACTION  ]

        protected List<BaseState> _interactiveStates;

        public virtual void OnPointerDown(PointerEventData eventData) {
            if (eventData.used) { return; }
            foreach(var state in _interactiveStates) {
                if (state.OnPointerDown(eventData)) {
                    eventData.Use();
                    return;
                }
            }

            // Here we need to cast a ray
            var hits = Physics.RaycastAll(eventData.enterEventCamera.ScreenPointToRay(eventData.pointerCurrentRaycast.screenPosition));

            foreach (var hit in hits) {
                if (hit.collider.gameObject == gameObject) { continue; }
                var handler = hit.collider.GetComponent<IPointerDownHandler>();
                if (handler == null) { continue; }
                handler.OnPointerDown(eventData);
                if (eventData.used) { return; }
            }
        }

        public virtual void OnPointerUp(PointerEventData eventData) {
            if (eventData.used) { return; }
            foreach (var state in _interactiveStates) {
                if (state.OnPointerUp(eventData)) {
                    eventData.Use();
                    return;
                }
            }

            // Here we need to cast a ray
            var eventCamera = eventData.enterEventCamera ?? eventData.pressEventCamera;
            if(eventCamera == null) { return; }
            var hits = Physics.RaycastAll(eventCamera.ScreenPointToRay(eventData.pointerCurrentRaycast.screenPosition));

            foreach (var hit in hits) { 
                if(hit.collider.gameObject == gameObject) { continue; }
                var handler = hit.collider.GetComponent<IPointerUpHandler>();
                if (handler == null) { continue; }
                handler.OnPointerUp(eventData);
                if (eventData.used) { return; }
            }
        }

        public void OnDrag(PointerEventData eventData) {
            if (eventData.used) { return; }
            foreach (var state in _interactiveStates) {
                if (state.OnPointerDrag(eventData)) {
                    eventData.Use();
                    return;
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData) {
            if (eventData.used) { return; }
            foreach (var state in _interactiveStates) {
                if (state.OnPointerBeginDrag(eventData)) {
                    eventData.Use();
                    return;
                }
            }
        }

        #region [--UNUSED FOR NOW--]
        //public virtual void OnPointerExit(PointerEventData eventData) {
        //    if (eventData.used) { return; }
        //    foreach (var state in _interactiveStates) {
        //        if (state.OnPointerExit(eventData)) {
        //            eventData.Use();
        //            return;
        //        }
        //    }
        //}

        //public virtual void OnPointerEnter(PointerEventData eventData) {
        //    if (eventData.used) { return; }
        //    foreach (var state in _interactiveStates) {
        //        if (state.OnPointerEnter(eventData)) {
        //            eventData.Use();
        //            return;
        //        }
        //    }
        //}
        #endregion

        #endregion

        #region [  COROUTINES  ]

        protected Coroutine _positionCoroutine;
        protected Coroutine _rotateCoroutine;

        public virtual void StopRotationCoroutine(BaseState requestingState) {
            if(_rotateCoroutine != null) {
                StopCoroutine(_rotateCoroutine);
                _rotateCoroutine = null;
            }
        }

        public virtual void StopMovementCoroutine(BaseState requestingState) {
            if (_positionCoroutine != null) {
                StopCoroutine(_positionCoroutine);
                _positionCoroutine = null;
            }
        }

        public virtual void MoveTo(BaseState requestingStep, Vector3 localPosition, float moveTime) {
            StopMovementCoroutine(requestingStep);
            _positionCoroutine = StartCoroutine(MoveTo(localPosition, moveTime, null));
        }

        public virtual void MoveTo(BaseState requestingStep, Vector3 localPosition, float moveTime, Action onFinish) {
            StopMovementCoroutine(requestingStep);
            _positionCoroutine = StartCoroutine(MoveTo(localPosition, moveTime, onFinish));
        }

        public virtual void RotateTo(BaseState requestingStep, Quaternion localRotation, float moveTime) {
            StopRotationCoroutine(requestingStep);
            _rotateCoroutine = StartCoroutine(RotateTo(localRotation, moveTime, null));
        }

        public virtual void RotateTo(BaseState requestingStep, Quaternion localRotation, float moveTime, Action onFinish) {
            StopRotationCoroutine(requestingStep);
            _rotateCoroutine = StartCoroutine(RotateTo(localRotation, moveTime, onFinish));
        }

        protected virtual IEnumerator MoveTo(Vector3 localPosition, float moveTime, Action finishCallback) {
            var rigidBody = RigidBody;
            var thisTransform = transform;
            if (rigidBody == null) {
                if (moveTime <= Mathf.Epsilon) {
                    thisTransform.localPosition = localPosition;
                }
                else {
                    float positionSpeed = (thisTransform.localPosition - localPosition).magnitude / moveTime;

                    while (thisTransform.localPosition != localPosition) {
                        thisTransform.localPosition = Vector3.MoveTowards(thisTransform.localPosition, localPosition, Time.fixedDeltaTime * positionSpeed);
                        yield return new WaitForFixedUpdate();
                    }
                }
            }
            else {
                localPosition = thisTransform.localToWorldMatrix * localPosition;
                if (moveTime <= Mathf.Epsilon) {
                    rigidBody.MovePosition(localPosition);
                }
                else {
                    float positionSpeed = (thisTransform.localPosition - localPosition).magnitude / moveTime;

                    while (thisTransform.localPosition != localPosition) {
                        rigidBody.MovePosition(Vector3.MoveTowards(rigidBody.position, localPosition, Time.fixedDeltaTime * positionSpeed));
                        yield return new WaitForFixedUpdate();
                    }
                }
            }

            if(finishCallback != null) {
                finishCallback();
            }
        }

        protected virtual IEnumerator RotateTo(Quaternion localRotation, float moveTime, Action finishCallback) {
            var rigidBody = RigidBody;
            var thisTransform = transform;
            if (rigidBody == null) {
                if (moveTime <= Mathf.Epsilon) {
                    thisTransform.localRotation = localRotation;
                }
                else {
                    float rotationSpeed = Quaternion.Angle(thisTransform.localRotation, localRotation) / moveTime;

                    while (thisTransform.localRotation != localRotation) {
                        thisTransform.localRotation = Quaternion.RotateTowards(thisTransform.localRotation, localRotation, Time.fixedDeltaTime * rotationSpeed);
                        yield return new WaitForFixedUpdate();
                    }
                }
            }
            else {
                // CAUTION: RigidBody rotation is not tested and can behave strangely, because we apply a local rotation to a global rotation
                // Fix it
                if (moveTime == 0) {
                    rigidBody.MoveRotation(localRotation);
                }
                else {
                    float rotationSpeed = Quaternion.Angle(thisTransform.localRotation, localRotation) / moveTime;

                    while (thisTransform.localRotation != localRotation) {
                        rigidBody.MoveRotation(Quaternion.RotateTowards(rigidBody.rotation, localRotation, Time.fixedDeltaTime * rotationSpeed));
                        yield return new WaitForFixedUpdate();
                    }
                }
            }

            if(finishCallback != null) {
                finishCallback();
            }
        }

        #endregion

        #region [  GENERAL METHODS  ]

        protected virtual void Reset() {
            defaultLocalPosition = transform.localPosition;
            defaultLocalRotation = transform.localRotation;
        }

        private void Start() {
            RigidBody = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
            UseAnimator &= animator != null;

            _interactiveStates = new List<BaseState>();


            // Initialize bindings
            foreach (var binding in Bindings) {
                if(binding.Property != null) {
                    binding.Property.ValueChanged += Property_ValueChanged;
                }
            }
            foreach(var state in States) {
                if(state.OverrideBinding != null && state.OverrideBinding.Property != null) {
                    state.OverrideBinding.Property.ValueChanged += Property_ValueChanged;
                }
                state.Initialize();

                if (state.UseOwnerEvents) { _interactiveStates.Add(state); }
            }
            foreach (var modifier in Modifiers) {
                if (modifier.OverrideBinding != null && modifier.OverrideBinding.Property != null) {
                    modifier.OverrideBinding.Property.ValueChanged += Property_ValueChanged;
                }
                modifier.Initialize();

                if (modifier.UseOwnerEvents) { _interactiveStates.Add(modifier); }
            }

            if(StartState != null) {
                _currentState = StartState;
                _currentState.OnStateEnter(_currentState);
            }

            Initialize();
        }

        protected virtual void Update() {
            if(CurrentState != null) {
                CurrentState.Update();
            }
        }

        protected virtual void Property_ValueChanged(Property property, object oldValue, object newValue) {
            foreach(var state in States) {
                if(state.EffectiveBinding.Property == property) {
                    if (state.CanEnterState(newValue)) {
                        CurrentState = state;
                        return;
                    }
                }
            }
            foreach (var wModifier in Modifiers)
            {
                if (wModifier.EffectiveBinding.Property == property)
                {
                    wModifier.Value = newValue;
                }
            }
        }

        protected abstract void Initialize();


        protected virtual void OnEnable() {
            foreach (var state in States) {
                if (state.triggerZone != null) {
                    state.triggerZone.gameObject.SetActive(true);
                }
            }
        }


        protected virtual void OnDisable() {
            foreach (var state in States) {
                if (state.triggerZone != null) {
                    state.triggerZone.gameObject.SetActive(false);
                }
            }
        }

        #endregion

    }
}