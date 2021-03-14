using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TXT.WEAVR.Common;
using TXT.WEAVR.Player.Views;
using TXT.WEAVR.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TXT.WEAVR.Player
{
    public class PopupView : MonoBehaviour, IPopup
    {
        [SerializeField]
        [AbsoluteValue]
        private float m_hideTimeout = 1f;
        [SerializeField]
        private bool m_hideOnClick = true;
        [SerializeField]
        private UIText m_title;
        [SerializeField]
        private UIText m_message;
        [SerializeField]
        private MaskableGraphic m_icon;
        [SerializeField]
        private GameObject m_content;
        [SerializeField]
        private LabeledButton[] m_allButtons;

        [Space]
        [SerializeField]
        [ShowIf(nameof(HasAnimator))]
        private OptionalString m_showTrigger;
        [SerializeField]
        [ShowIf(nameof(HasAnimator))]
        private OptionalString m_hideTrigger;

        [Space]
        [SerializeField]
        private UnityEvent m_show;
        [SerializeField]
        private UnityEvent m_hide;

        [NonSerialized]
        private Texture2D m_defaultIcon;
        [NonSerialized]
        private Coroutine m_autoDeactivateCoroutine;

        private Animator m_animator;

        public float HideTimeout
        {
            get => m_hideTimeout;
            set
            {
                if(value < 0)
                {
                    throw new ArgumentException("The value for Hide Timeout is negative");
                }
                m_hideTimeout = value;
            }
        }

        public string Title
        {
            get => m_title.Text;
            set => m_title.Text = value;
        }

        public string Message
        {
            get => m_message.Text;
            set => m_message.Text = value;
        }

        public Texture2D Icon
        {
            get => m_icon is Image image && image.sprite ? image.mainTexture as Texture2D : m_icon is RawImage rawImage ? rawImage.texture as Texture2D : null;
            set
            {
                if(m_icon is Image image)
                {
                    image.sprite = value ? SpriteCache.Instance.Get(value) : null;
                    image.enabled = image.sprite;
                }
                else if(m_icon is RawImage rawImage)
                {
                    rawImage.texture = value;
                    rawImage.enabled = rawImage.texture;
                }
            }
        }

        public IReadOnlyList<LabeledButton> Buttons => m_allButtons;

        private bool HasAnimator() => m_animator;

        private void Reset()
        {
            m_showTrigger = "Show";
            m_showTrigger.enabled = false;
            m_hideTrigger = "Hide";
        }

        private void OnValidate()
        {
            if (!m_animator)
            {
                m_animator = GetComponent<Animator>();
            }
        }

        private void Awake()
        {
            m_defaultIcon = Icon;
            if (!m_animator)
            {
                m_animator = GetComponent<Animator>();
            }
        }

        public virtual void Show()
        {
            StopAutoDeactivationCoroutine();
            gameObject.SetActive(true);
            if (m_animator && m_animator.enabled)
            {
                if (m_hideTrigger.enabled) { m_animator.ResetTrigger(m_hideTrigger); }
                if (m_showTrigger.enabled) { m_animator.SetTrigger(m_showTrigger); }
            }
            if (m_hideOnClick)
            {
                foreach (var button in m_allButtons)
                {
                    if (button)
                    {
                        button.onClick.RemoveListener(Hide);
                        button.onClick.AddListener(Hide);
                    }
                }
            }
            m_show.Invoke();
        }

        public virtual void Hide()
        {
            m_hide.Invoke();
            if (m_animator && m_animator.enabled)
            {
                if (m_showTrigger.enabled) { m_animator.ResetTrigger(m_showTrigger); }
                if (m_hideTrigger.enabled) { m_animator.SetTrigger(m_hideTrigger); }
            }
            StopAutoDeactivationCoroutine();
            StartAutoDeactivationCoroutine();
        }

        protected void StartAutoDeactivationCoroutine()
        {
            m_autoDeactivateCoroutine = StartCoroutine(AutoDeactivate(m_hideTimeout));
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
            gameObject.SetActive(false);
            m_autoDeactivateCoroutine = null;
        }

        public void ResetDefaultIcon()
        {
            Icon = m_defaultIcon;
        }

        public async Task ShowAsync()
        {
            Show();
            while (gameObject.activeInHierarchy)
            {
                await Task.Yield();
            }
        }
    }
}
