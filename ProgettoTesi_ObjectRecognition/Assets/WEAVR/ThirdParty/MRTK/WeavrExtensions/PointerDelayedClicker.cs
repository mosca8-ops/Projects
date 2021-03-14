#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
namespace TXT.WEAVR.InteractionUI
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;

    public class PointerDelayedClicker : MonoBehaviour, IPointerHandler
    {

        public PointerManager pointerManager;
        public DelayManager delayManager;

        [Header("Configuration")]
        public bool repeatingClick;

        public GameObject loadingObject;
        public Image loadingImage;
        public Gradient loadingColor;
        public float imageDepth = 0.1f;

        public Animator clickAnimator;

        public Color clickColor;
        public float clickDuration;
        [Tooltip("Makes the long stare longer if pointer is moving")]
        public float dampingMultiplier = 10000;

        protected bool _isLoading;
        protected float _loadingProgress;
        protected bool _canClick;
        protected IInteractiveObject _currentClickable;
        protected StateFinishedBehaviour _clickBehaviour;

        protected int _animatorClick = Animator.StringToHash("Clicked");

        protected Vector3 _lastCursorPosition;

        private void OnValidate()
        {
            if (loadingObject == null)
            {
                loadingObject = gameObject;
            }
            if (clickAnimator == null)
            {
                clickAnimator = loadingObject.GetComponentInChildren<Animator>(true);
            }
        }

        void Start()
        {
            pointerManager.ClickableChanged += PointerManager_ClickableChanged;

            loadingObject.gameObject.SetActive(true);

            if (clickAnimator == null)
            {
                clickAnimator = loadingObject.GetComponentInChildren<Animator>();
            }
            if (clickAnimator != null)
            {
                _clickBehaviour = clickAnimator.GetBehaviour<StateFinishedBehaviour>();
                if (_clickBehaviour != null) _clickBehaviour.actionToPerform = PerformClick;
            }

            loadingObject.transform.position = new Vector3(0, 0, imageDepth);
            loadingObject.transform.SetParent(pointerManager.pointer, false);

            loadingImage.fillAmount = 0;

            _lastCursorPosition = pointerManager.pointer.position;
        }

        private void PointerManager_ClickableChanged(IInteractiveObject previous, IInteractiveObject current)
        {
            if (current != null && !current.Equals(_currentClickable))
            {
                StartDelayedClick(current);
            }
            else
            {
                StopDelayedClick();
            }
        }

        void LateUpdate()
        {
            if (_isLoading)
            {
                if (_loadingProgress < delayManager.DelayAmount)
                {
                    _loadingProgress += Time.deltaTime - Mathf.Min(Time.deltaTime, (pointerManager.pointer.position - _lastCursorPosition).sqrMagnitude * dampingMultiplier);
                    var percentageProgress = _loadingProgress / delayManager.DelayAmount;
                    loadingImage.fillAmount = percentageProgress;
                    loadingImage.color = loadingColor.Evaluate(percentageProgress);
                }
                else if (_canClick)
                {
                    _canClick = false;
                    ClickAnimation();
                }
            }
            _lastCursorPosition = pointerManager.pointer.position;
        }

        public void StartDelayedClick(IInteractiveObject clickable)
        {
            if (!loadingObject.activeInHierarchy)
            {
                loadingObject.SetActive(true);
            }
            if (delayManager.isDelayEnabled)
            {
                _isLoading = true;
                _canClick = true;
                _loadingProgress = 0;
            }
            _currentClickable = clickable;
        }

        public void StopDelayedClick()
        {
            _isLoading = false;
            loadingImage.fillAmount = 0;
        }

        public void PointerAction(InteractionType type, Vector3 pointerPosition)
        {
            PerformPointerAction(type, pointerPosition);
        }

        private void PerformPointerAction(InteractionType type, Vector3? pointerPosition = null)
        {
            if (_currentClickable != null && _currentClickable.CanInteract)
            {
                _currentClickable.Interact(type, pointerPosition);
            }
        }

        protected virtual void PerformClick()
        {
            if (_currentClickable != null)
            {
                PerformPointerAction(InteractionType.PointerLongUp);
                if (repeatingClick)
                {
                    StartDelayedClick(_currentClickable);
                }
            }
        }

        protected virtual void ClickAnimation()
        {
            if (clickAnimator != null)
            {
                StartAnimatorClick();
            }
            else
            {
                StartCoroutine(ClickCoroutine());
            }
        }

        protected virtual void StartAnimatorClick()
        {
            clickAnimator.SetBool(_animatorClick, true);
            if (_clickBehaviour == null)
            {
                _clickBehaviour = clickAnimator.GetBehaviour<StateFinishedBehaviour>();
            }
            if (_clickBehaviour != null) _clickBehaviour.actionToPerform = PerformClick;
        }

        protected virtual IEnumerator ClickCoroutine()
        {
            loadingImage.color = clickColor;
            yield return new WaitForSeconds(0.1f);
            PerformClick();
        }
    }
}
#endif
