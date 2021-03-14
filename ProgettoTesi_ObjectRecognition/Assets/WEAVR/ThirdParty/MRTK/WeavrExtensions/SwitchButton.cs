#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
namespace TXT.WEAVR.InteractionUI
{
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    [RequireComponent(typeof(Button), typeof(Animator))]
    public class SwitchButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public bool initialStatus = false;
        public UnityEvent whenOn;
        public UnityEvent whenOff;

        [Header("Configuration")]
        public bool longStareEnabled = true;
        public float longStareTime = 1;
        public float defaultTime = 0.5f;

        private Animator _animator;
        private int _animatorStatus = Animator.StringToHash("Status");
        private int _animatorIsChanging = Animator.StringToHash("IsChanging");
        private int _animatorChangeSpeed = Animator.StringToHash("ChangeSpeed");

        private Button _button;
        private bool _isON = true;
        private bool _quickToggle;

        public bool IsON {
            get {
                return _isON;
            }
            set {
                if (_isON != value)
                {
                    _isON = value;
                    if (_isON) whenOn.Invoke();
                    else whenOff.Invoke();
                }
            }
        }


        private void OnValidate()
        {
            if (!longStareEnabled)
            {
                longStareTime = defaultTime;
            }
        }

        private void Start()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            foreach (var stateBehaviour in _animator.GetBehaviours<SwitchButtonChangeStatus>())
            {
                stateBehaviour.switchButton = this;
            }

            _animator.SetBool(_animatorStatus, initialStatus);
            SetTo(initialStatus);

            if (_button == null)
            {
                _button = GetComponent<Button>();
            }
            _button.onClick.AddListener(Toggle);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (longStareEnabled) Toggle(true, longStareTime, false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (longStareEnabled && !_quickToggle) Toggle(false, longStareTime, false);
        }

        public void Toggle(bool enable, float time, bool quickToggle)
        {
            _quickToggle = quickToggle;
            _animator.SetFloat(_animatorChangeSpeed, 1.0f / time);
            _animator.SetBool(_animatorIsChanging, enable);
        }

        public virtual void Toggle()
        {
            Toggle(true, defaultTime, true);
        }

        protected virtual void SetTo(bool to)
        {
            if (_isON != to)
            {
                Toggle();
            }
        }
    }
}
#endif
