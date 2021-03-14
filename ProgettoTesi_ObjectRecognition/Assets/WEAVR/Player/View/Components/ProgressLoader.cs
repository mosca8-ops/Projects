using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.UI;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Player.Views
{

    public class ProgressLoader : MonoBehaviour, IProgressLoader, IProgressElement
    {
        public delegate void ShowLoaderDelegate(ProgressLoader loader, bool isIndefinite);
        public delegate void HideLoaderDelegate(ProgressLoader loader);
        public delegate void ProgressUpdateDelegate(ProgressLoader loader, float progress);

        [SerializeField]
        private UIText m_info;
        [SerializeField]
        [AbsoluteValue]
        private OptionalFloat m_hideTimeout = 0.5f;
        [SerializeField]
        [EnableIfComponentExists(typeof(Animator))]
        private OptionalString m_showTrigger;
        [SerializeField]
        [EnableIfComponentExists(typeof(Animator))]
        private OptionalString m_hideTrigger;

        [Header("Events")]
        [SerializeField]
        private UnityEvent m_onShowIndefinite;
        [SerializeField]
        private UnityEvent m_onShowWithProgress;
        [SerializeField]
        private UnityEvent m_onHide;
        [SerializeField]
        private UnityEventFloat m_onProgressUpdate;

        public event ShowLoaderDelegate OnShow;
        public event HideLoaderDelegate OnHide;
        public event ProgressUpdateDelegate OnProgressUpdate;

        private Coroutine m_updateCoroutine;
        private Animator m_animator;

        #region [  DELAYED HIDE LOGIC  ]

        private Coroutine m_hideCoroutine;

        public float HideTimeout
        {
            get => m_hideTimeout;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("The value for Hide Timeout is negative");
                }
                m_hideTimeout = value;
            }
        }

        protected virtual void Reset()
        {
            m_showTrigger = new OptionalString()
            {
                value = "Show",
                enabled = false
            };
            m_hideTrigger = new OptionalString()
            {
                value = "Hide",
                enabled = false
            };
        }

        private void Start()
        {
            if (!m_animator)
            {
                m_animator = GetComponent<Animator>();
            }
        }

        protected void StartAutoDeactivationCoroutine()
        {
            m_hideCoroutine = StartCoroutine(AutoDeactivate(m_hideTimeout));
        }

        protected void StopAutoDeactivationCoroutine()
        {
            if (m_hideCoroutine != null)
            {
                StopCoroutine(m_hideCoroutine);
                m_hideCoroutine = null;
            }
        }

        private IEnumerator AutoDeactivate(float timeout)
        {
            yield return new WaitForSeconds(timeout);
            gameObject.SetActive(false);
            m_hideCoroutine = null;
        }

        #endregion

        private float m_progress;

        public string Text
        {
            get => m_info.Text;
            set => m_info.Text = value;
        }

        public Func<float> ProgressFunctor { get; set; }

        public float Progress
        {
            get => m_progress;
            set
            {
                m_progress = Mathf.Clamp01(value);
                m_onProgressUpdate.Invoke(m_progress);
                OnProgressUpdate?.Invoke(this, m_progress);
            }
        }

        public void Hide()
        {
            if (!gameObject.activeSelf) { return; }

            if (gameObject.activeInHierarchy)
            {
                StopAutoDeactivationCoroutine();
                HideAnimator();
                if (m_hideTimeout.enabled)
                {
                    StartAutoDeactivationCoroutine();
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
            else
            {
                gameObject.SetActive(false);
            }
            OnHide?.Invoke(this);
            m_onHide.Invoke();
        }

        public void Show(string caption, Func<float> progressUpdate)
        {
            StopAutoDeactivationCoroutine();
            StopProgressUpdateCoroutine();
            Text = caption;
            gameObject.SetActive(true);
            ShowAnimator();
            ProgressFunctor = progressUpdate;
            OnShow?.Invoke(this, false);
            m_onShowWithProgress.Invoke();
            StartProgressUpdateCoroutine();
        }

        public void Show(string caption)
        {
            StopAutoDeactivationCoroutine();
            StopProgressUpdateCoroutine();
            Text = caption;
            gameObject.SetActive(true);
            ShowAnimator();
            OnShow?.Invoke(this, true);
            m_onShowIndefinite.Invoke();
        }

        protected void ShowAnimator()
        {
            if(m_animator && m_showTrigger.enabled)
            {
                if (m_hideTrigger.enabled) { m_animator.ResetTrigger(m_hideTrigger.value); }
                m_animator.SetTrigger(m_showTrigger.value);
            }
        }

        protected void HideAnimator()
        {
            if (m_animator && m_hideTrigger.enabled)
            {
                if (m_showTrigger.enabled) { m_animator.ResetTrigger(m_showTrigger.value); }
                m_animator.SetTrigger(m_hideTrigger.value);
            }
        }

        protected void StopProgressUpdateCoroutine()
        {
            if(m_updateCoroutine != null)
            {
                StopCoroutine(m_updateCoroutine);
                m_updateCoroutine = null;
            }
        }

        protected async void StartProgressUpdateCoroutine()
        {
            while(gameObject.activeSelf && !gameObject.activeInHierarchy)
            {
                await System.Threading.Tasks.Task.Yield();
            }
            if (gameObject.activeSelf)
            {
                m_updateCoroutine = StartCoroutine(ProgressUpdateCoroutine());
            }
        }

        protected virtual void UpdateProgress(float progress) 
        {
            m_progress = progress;
            m_onProgressUpdate.Invoke(progress);
            OnProgressUpdate?.Invoke(this, progress);
        }

        private IEnumerator ProgressUpdateCoroutine()
        {
            while(ProgressFunctor != null)
            {
                UpdateProgress(Mathf.Clamp01(ProgressFunctor()));
                yield return null;
            }
            m_updateCoroutine = null;
        }

        public void ResetProgress()
        {
            Progress = 0;
        }
    }
}
