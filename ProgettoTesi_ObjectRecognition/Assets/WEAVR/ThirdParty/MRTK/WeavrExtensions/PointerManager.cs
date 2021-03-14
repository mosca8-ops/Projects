#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
namespace TXT.WEAVR.InteractionUI
{
    using System.Collections.Generic;
    using TXT.WEAVR.Common;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    public delegate void OnDataChanged<T>(T previous, T current);

    public class PointerManager : MonoBehaviour
    {
        public static PointerManager Instance { get; private set; }


        public PointerRaycaster pointerRaycaster;
        public Transform pointer;

        public event OnDataChanged<GameObject> PointingObjectChanged;
        public event OnDataChanged<IInteractiveObject> ClickableChanged;

        protected List<ClickWrapper> _clickWrappers;
        protected GameObject _currentObject;
        protected ClickWrapper _currentClickable;

        private void OnValidate()
        {
            if (pointerRaycaster == null)
            {
                pointerRaycaster = GetComponent<PointerRaycaster>();
                if (pointerRaycaster == null)
                {
                    Debug.LogWarning("Pointer Raycaster should be set in order to allow this manager to work correctly");
                }
            }
            else if (pointerRaycaster.pointer != null)
            {
                pointer = pointerRaycaster.pointer;
            }
            else if (pointer != null)
            {
                pointerRaycaster.pointer = pointer;
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        // Use this for initialization
        void Start()
        {
            RegisterClickWrapper(new ButtonClicker());
            RegisterClickWrapper(new PointerClickWrapper(PointerEventData.InputButton.Left));
            if (pointerRaycaster != null)
            {
                pointerRaycaster.PointedObject += PointerRaycaster_PointedObject;
            }
        }

        private void PointerRaycaster_PointedObject(PointerRaycaster.PointedDataAll allPointedData)
        {
            ClickWrapper clickable = null;
            GameObject pointedObject = allPointedData.pointedObject;

            if (pointedObject != _currentObject)
            {
                if (PointingObjectChanged != null) PointingObjectChanged(_currentObject, pointedObject);
                _currentObject = pointedObject;

                if (ClickableChanged != null && (_currentClickable == null || !_currentClickable.IsAlreadyWrapped(_currentObject)))
                {
                    clickable = GetAsClickable(pointedObject);
                    if (_currentClickable != clickable)
                    {
                        clickable = clickable != null ? clickable.Clone() : null;
                        ClickableChanged(_currentClickable, clickable);
                        _currentClickable = clickable;
                    }
                }
            }
        }

        public virtual ClickWrapper GetAsClickable(GameObject gameObject)
        {
            if (gameObject != null)
            {
                foreach (var wrapper in _clickWrappers)
                {
                    if (wrapper.Wrap(gameObject))
                    {
                        return wrapper;
                    }
                }
            }
            return null;
        }

        public virtual void RegisterClickWrapper(ClickWrapper clickWrapper)
        {
            if (_clickWrappers == null)
            {
                _clickWrappers = new List<ClickWrapper>();
            }
            _clickWrappers.Add(clickWrapper);
        }

        public abstract class ClickWrapper : IInteractiveObject
        {
            public abstract bool CanInteract {
                get;
            }

            public abstract void Interact(InteractionType type, Vector3? interactionPoint = null);

            public abstract bool Wrap(GameObject gameObject);

            public abstract bool IsAlreadyWrapped(GameObject gameObject);

            public abstract ClickWrapper Clone();
        }

        public class ButtonClicker : ClickWrapper
        {
            private Button _component;
            private GameObject _gameObject;

            public override bool CanInteract {
                get {
                    return _component != null && _component.enabled && _component.interactable;
                }
            }

            public override bool Wrap(GameObject gameObject)
            {
                _gameObject = gameObject;
                _component = gameObject.GetComponent(typeof(Button)) as Button;
                return _component != null;
            }

            public override void Interact(InteractionType type, Vector3? interactionPoint = null)
            {
                _component.onClick.Invoke();
            }

            public override ClickWrapper Clone()
            {
                return new ButtonClicker()
                {
                    _component = _component,
                    _gameObject = _gameObject,
                };
            }

            public override bool IsAlreadyWrapped(GameObject gameObject)
            {
                return _gameObject == gameObject;
            }
        }

        public class PointerClickWrapper : ClickWrapper
        {
            private PointerEventData _eventData;
            private IPointerClickHandler _componentClick;
            private IPointerDownHandler _componentDown;
            private IPointerUpHandler _componentUp;
            private GameObject _gameObject;

            public override bool CanInteract {
                get {
                    return _componentUp != null || _componentDown != null && _componentClick != null;
                }
            }

            public PointerClickWrapper(PointerEventData.InputButton inputButton)
            {
                _eventData = new PointerEventData(EventSystem.current);
                _eventData.button = inputButton;
            }

            private PointerClickWrapper(PointerClickWrapper other)
            {
                _eventData = other._eventData;
                _componentClick = other._componentClick;
                _componentUp = other._componentUp;
                _componentDown = other._componentDown;
                _gameObject = other._gameObject;
            }

            public override bool Wrap(GameObject gameObject)
            {
                _gameObject = gameObject;
                _componentDown = gameObject.GetComponent(typeof(IPointerDownHandler)) as IPointerDownHandler;
                if (_componentDown != null) { return true; }
                _componentUp = gameObject.GetComponent(typeof(IPointerUpHandler)) as IPointerUpHandler;
                if (_componentUp != null) { return true; }
                _componentClick = gameObject.GetComponent(typeof(IPointerClickHandler)) as IPointerClickHandler;
                return _componentClick != null;
            }

            public override void Interact(InteractionType type, Vector3? interactionPoint = null)
            {
                _eventData = new PointerEventData(EventSystem.current);
                _eventData.position = interactionPoint ?? _eventData.position;
                if (_componentDown != null && type == InteractionType.PointerDown)
                {
                    _componentDown.OnPointerDown(_eventData);
                    return;
                }
                if (_componentUp != null && type.ContainsFlag(InteractionType.PointerUp))
                {
                    _componentUp.OnPointerUp(_eventData);
                    return;
                }
                if (_componentClick != null)
                {
                    _componentClick.OnPointerClick(_eventData);
                }
            }

            public override ClickWrapper Clone()
            {
                return new PointerClickWrapper(this);
            }

            public override bool IsAlreadyWrapped(GameObject gameObject)
            {
                return _gameObject == gameObject;
            }
        }
    }
}
#endif
