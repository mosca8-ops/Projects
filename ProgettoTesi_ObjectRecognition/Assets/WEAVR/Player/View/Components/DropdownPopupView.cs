using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TXT.WEAVR.Common;
using TXT.WEAVR.Player.Views;
using TXT.WEAVR.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace TXT.WEAVR.Player
{
    public class DropdownPopupView : MonoBehaviour, IDropdownPopup
    {
        [SerializeField]
        [AbsoluteValue]
        private float m_hideTimeout = 0.5f;
        [SerializeField]
        private UIText m_title;
        [SerializeField]
        private UIText m_description;
        [SerializeField]
        private MaskableGraphic m_icon;
        [SerializeField]
        private ExtendedDropdown m_dropdown;
        [SerializeField]
        private LabeledButton m_cancel;
        [SerializeField]
        private LabeledButton m_apply;

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
        private bool m_valueIsValid;
        private int m_validSelectedIndex;
        private Action<int> m_onSelectedAction;

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

        public string Description
        {
            get => m_description.Text;
            set => m_description.Text = value;
        }

        public Texture2D Icon
        {
            get => m_icon is Image image && image.sprite ? image.mainTexture as Texture2D : m_icon is RawImage rawImage ? rawImage.texture as Texture2D : null;
            set
            {
                if(m_icon is Image image)
                {
                    image.sprite = value ? SpriteCache.Instance.Get(value) : null;
                }
                else if(m_icon is RawImage rawImage)
                {
                    rawImage.texture = value;
                }
            }
        }

        public LabeledButton CancelButton => m_cancel;
        public LabeledButton ApplyButton => m_apply;

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
            m_apply.Button.onClick.AddListener(ApplyClicked);
            m_cancel.Button.onClick.AddListener(CanceClicked);
        }

        private void CanceClicked()
        {
            m_valueIsValid = true;
            Hide();
        }

        private void ApplyClicked()
        {
            m_validSelectedIndex = m_dropdown.value;
            m_valueIsValid = true;
            if(m_onSelectedAction != null)
            {
                m_onSelectedAction(m_validSelectedIndex);
                m_onSelectedAction = null;
            }
            Hide();
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
            StopAutoDeactivationCoroutine();
            gameObject.SetActive(true);
            m_show.Invoke();
            while (gameObject.activeInHierarchy)
            {
                await Task.Yield();
            }
        }

        private void OnDisable()
        {
            m_valueIsValid = true;
        }

        public void Show(string title, string description, int selectedOption, IEnumerable<PopupOption> options, Action<int> onSelection)
        {
            m_valueIsValid = false;
            m_validSelectedIndex = selectedOption;
            Title = title;
            Description = description;
            m_onSelectedAction = onSelection;
            m_dropdown.options = options.Select(o => new TMPro.TMP_Dropdown.OptionData(o.text)).ToList();
            m_dropdown.value = selectedOption;
            Show();
        }

        public async Task<int> ShowAsync(string title, string description, int selectedOption, IEnumerable<PopupOption> options)
        {
            m_valueIsValid = false;
            m_validSelectedIndex = selectedOption;
            Title = title;
            Description = description;
            m_onSelectedAction = null;
            m_dropdown.options = options.Select(o => new TMPro.TMP_Dropdown.OptionData(o.text)).ToList();
            m_dropdown.value = selectedOption;
            Show();

            while (!m_valueIsValid)
            {
                await Task.Yield();
            }

            return m_validSelectedIndex;
        }
    }
}
