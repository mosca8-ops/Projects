#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
namespace TXT.WEAVR.InteractionUI
{
    using UnityEngine;

    [RequireComponent(typeof(Animator))]
    public class DelayManager : MonoBehaviour
    {
        public bool isDelayEnabled;
        public SwitchButton switchButton;
        public ProgressiveValueSetter delaySetter;

        public float maxDelayAmount = 5;

        private float _delayAmount = 1;

        /// <summary>
        /// The delay of this manager
        /// </summary>
        public float DelayAmount {
            get {
                return _delayAmount;
            }
            set {
                if (_delayAmount != value)
                {
                    _delayAmount = value;
                    if (switchButton != null)
                    {
                        switchButton.longStareTime = value;
                    }
                }
            }
        }

        private Animator _animator;
        private int _animatorDelayEnabledId = Animator.StringToHash("DelayEnabled");

        void Start()
        {
            _animator = GetComponent<Animator>();
            if (delaySetter != null)
            {
                delaySetter.Title = "Click Delay";
                delaySetter.maxValue = maxDelayAmount;
                DelayAmount = delaySetter.value;
                delaySetter.ValueChanged += DelaySetter_DelayChanged;
            }
        }

        private void DelaySetter_DelayChanged(ProgressiveValueSetter sender, float newData)
        {
            DelayAmount = newData;
        }

        /// <summary>
        /// Toggles the delay values
        /// </summary>
        /// <param name="enable"></param>
        public void ToggleDelay(bool enable)
        {
            _animator.SetBool(_animatorDelayEnabledId, enable);
            if (switchButton != null && delaySetter != null)
            {
                switchButton.longStareTime = delaySetter.value;
            }
            isDelayEnabled = enable;
        }
    }
}
#endif
