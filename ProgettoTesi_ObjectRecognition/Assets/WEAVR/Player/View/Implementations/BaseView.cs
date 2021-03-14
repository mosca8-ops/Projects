namespace TXT.WEAVR.Player.Views
{
    using System;
    using System.Collections;
    using TXT.WEAVR.Common;
    using TXT.WEAVR.Localization;
    using TXT.WEAVR.UI;
    using UnityEngine;
    using UnityEngine.Serialization;

    public abstract class BaseView : MonoBehaviour, IView, ILoadingView
    {
        public delegate void OnStateChange(BaseView page);

        // Just a quick shortcut
        protected static string Translate(string value) => LocalizationManager.Translate(value);

        [Tooltip("Canvas page")]
        [Draggable]
        [FormerlySerializedAs("page")]
        public GameObject view;
        [SerializeField]
        [AbsoluteValue]
        private OptionalFloat m_hideTimeout = 1f;
        [Tooltip("Loader object")]
        [Draggable]
        [Type(typeof(IProgressLoader))]
        public Component progressLoader;
        [Draggable]
        [Type(typeof(ILoader))]
        public Component indefiniteLoader;


        private bool m_isVisible;
        public bool IsVisible 
        {
            get => m_isVisible;
            set
            {
                if (m_isVisible != value)
                {
                    if (value)
                    {
                        Show();
                    }
                    else
                    {
                        Hide();
                    }
                }
            }
        }

        public event ViewDelegate OnShow;
        public event ViewDelegate OnHide;
        public event ViewDelegate OnBack;

        [NonSerialized]
        protected Animator m_viewAnimator;
        [NonSerialized]
        private Coroutine m_autoDeactivateCoroutine;

        protected void Back() => OnBack?.Invoke(this);

        protected virtual void Reset()
        {
            view = gameObject;
            progressLoader = GetComponentInChildren<IProgressLoader>(true) as Component;
            indefiniteLoader = GetComponentInChildren<ILoader>(true) as Component;
        }

        // Start is called before the first frame update
        protected virtual void Start()
        {
            if (!view) { view = gameObject; }
            m_isVisible = view.activeInHierarchy;
            m_viewAnimator = view.GetComponent<Animator>();
        }

        public virtual void Show()
        {
            if (view)
            {
                StopAutoDeactivationCoroutine();
                view.SetActive(true);
                if (m_viewAnimator)
                {
                    m_viewAnimator.SetTrigger("Show");
                }
            }
            m_isVisible = true;
            OnShow?.Invoke(this);
        }

        public virtual void Hide()
        {
            m_isVisible = false;
            OnHide?.Invoke(this);
            if (view)
            {
                StopAutoDeactivationCoroutine();
                if (m_viewAnimator)
                {
                    m_viewAnimator.ResetTrigger("Show");
                    m_viewAnimator.SetTrigger("Hide");
                    StartAutoDeactivationCoroutine();
                }
                else if (!m_hideTimeout.enabled)
                {
                    view.SetActive(false);
                }
                else
                {
                    StartAutoDeactivationCoroutine();
                }
            }
        }

        public virtual void StartLoading(string title, Func<float> progressCallback)
        {
            StopLoading();
            if(progressLoader is IProgressLoader loader)
            {
                loader.Show(title, progressCallback);
            }
        }

        public virtual void StartLoading(string title = "")
        {
            StopLoading();
            if(indefiniteLoader is ILoader loader)
            {
                loader.Show(title);
            }
        }

        public virtual void StopLoading()
        {
            if(progressLoader is ILoader pLoader)
            {
                pLoader.Hide();
            }
            if(indefiniteLoader is ILoader iLoader)
            {
                iLoader.Hide();
            }
        }

        protected void StartAutoDeactivationCoroutine()
        {
            if (view && m_hideTimeout.enabled)
            {
                m_autoDeactivateCoroutine = StartCoroutine(AutoDeactivate(m_hideTimeout));
            }
        }

        protected void StopAutoDeactivationCoroutine()
        {
            if (m_autoDeactivateCoroutine != null)
            {
                StopCoroutine(m_autoDeactivateCoroutine);
            }
        }

        private IEnumerator AutoDeactivate(float timeout)
        {
            yield return new WaitForSeconds(timeout);
            view.SetActive(false);
            m_autoDeactivateCoroutine = null;
        }
    }
}