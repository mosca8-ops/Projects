using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.UI;


namespace TXT.WEAVR.UI
{

    [AddComponentMenu("WEAVR/Setup/Notification Panel")]
    public class NotificationPanel : MonoBehaviour
    {
        [SerializeField]
        [Draggable]
        private Text m_textComponent;
        [SerializeField]
        [Draggable]
        private Image m_background;
        [SerializeField]
        private bool m_keepAlpha = true;
        [SerializeField]
        private bool m_autoSetup = true;
        [SerializeField]
        private float m_keepVisibleTime = 3;

        [Header("Animation")]
        [SerializeField]
        private bool m_useAnimation;
        [SerializeField]
        [HiddenBy(nameof(m_useAnimation))]
        [Draggable]
        private Animator m_animator;
        [SerializeField]
        [HiddenBy(nameof(m_animator), nameof(m_useAnimation))]
        private string m_showTrigger = "ShowNotification";
        [SerializeField]
        [HiddenBy(nameof(m_animator), nameof(m_useAnimation))]
        private string m_hideTrigger = "HideNotification";

        [Header("Audio")]
        [SerializeField]
        private bool m_useAudio;
        [SerializeField]
        [HiddenBy(nameof(m_useAudio))]
        [Draggable]
        private AudioClip m_showAudio;
        [SerializeField]
        [HiddenBy(nameof(m_useAudio))]
        [Draggable]
        private AudioClip m_hideAudio;

        [Header("Colors")]
        [SerializeField]
        private ColorPair m_infoColors;
        [SerializeField]
        private ColorPair m_warnColors;
        [SerializeField]
        private ColorPair m_errorColors;

        private Queue<Notification> m_notifications;
        private bool m_currentlyShowingNotification;

        private void OnValidate()
        {
            if(m_background == null)
            {
                m_background = GetComponentInChildren<Image>(true);
            }
            if(m_textComponent == null)
            {
                m_textComponent = GetComponentInChildren<Text>(true);
            }
            if(m_useAnimation && m_animator == null)
            {
                m_animator = GetComponentInParent<Animator>();
            }
            //if(m_useAudio && m_showAudio != null && m_hideAudio == null)
            //{
            //    m_hideAudio = m_showAudio;
            //}
        }

        // Use this for initialization
        void Start()
        {
            if (m_keepAlpha && m_background != null)
            {
                m_infoColors.backgroundColor.a = m_background.color.a;
                m_warnColors.backgroundColor.a = m_background.color.a;
                m_errorColors.backgroundColor.a = m_background.color.a;
            }
            m_notifications = new Queue<Notification>();
        }

        private void OnEnable()
        {
            if(m_notifications == null)
            {
                m_notifications = new Queue<Notification>();
            }
            StopAllCoroutines();
            m_currentlyShowingNotification = false;
            m_notifications.Clear();
            if (m_useAnimation && m_animator != null)
            {
                m_animator.ResetTrigger(m_showTrigger);
                m_animator.SetTrigger(m_hideTrigger);
            }
            if (m_autoSetup)
            {
                var notificationManager = NotificationManager.Instance;
                if (notificationManager != null)
                {
                    notificationManager.OnInfo.AddListener(ShowInfo);
                    notificationManager.OnWarning.AddListener(ShowWarn);
                    notificationManager.OnError.AddListener(ShowError);
                }
            }
        }

        private void OnDisable()
        {
            if (m_autoSetup)
            {
                var notificationManager = NotificationManager.Instance;
                if (notificationManager != null)
                {
                    notificationManager.OnInfo.RemoveListener(ShowInfo);
                    notificationManager.OnWarning.RemoveListener(ShowWarn);
                    notificationManager.OnError.RemoveListener(ShowError);
                }
            }
            StopAllCoroutines();
            m_currentlyShowingNotification = false;
            m_notifications.Clear();
            if (m_useAnimation && m_animator != null)
            {
                m_animator.ResetTrigger(m_showTrigger);
                m_animator.SetTrigger(m_hideTrigger);
            }
        }

        private void ShowError(string text)
        {
            Enqueue(new Notification(text, m_errorColors));
        }

        private void ShowInfo(string text)
        {
            Enqueue(new Notification(text, m_infoColors));
        }

        private void ShowWarn(string text)
        {
            Enqueue(new Notification(text, m_warnColors));
        }

        private void Enqueue(Notification notification)
        {
            if (isActiveAndEnabled)
            {
                m_notifications.Enqueue(notification);
                if (!m_currentlyShowingNotification)
                {
                    StartCoroutine(ShowNotification(m_keepVisibleTime));
                }
            }
        }

        IEnumerator ShowNotification(float showTime)
        {
            m_currentlyShowingNotification = true;
            while (m_notifications.Count > 0)
            {
                m_notifications.Dequeue().Apply(m_background, m_textComponent);

                if(m_useAudio && m_showAudio != null)
                {
                    AudioSource.PlayClipAtPoint(m_showAudio, transform.position);
                }
                if (m_useAnimation && m_animator != null)
                {
                    m_animator.ResetTrigger(m_hideTrigger);
                    m_animator.SetTrigger(m_showTrigger);
                    //yield return null;
                    yield return new WaitForSeconds(1);
                }

                yield return new WaitForSeconds(showTime);

                if(m_useAudio && m_hideAudio)
                {
                    AudioSource.PlayClipAtPoint(m_hideAudio, transform.position);
                }
                if (m_useAnimation && m_animator != null)
                {
                    m_animator.ResetTrigger(m_showTrigger);
                    m_animator.SetTrigger(m_hideTrigger);
                    //yield return null;
                    yield return new WaitForSeconds(1);
                }
            }
            m_currentlyShowingNotification = false;
        }

        private class Notification
        {
            public string text;
            public ColorPair colors;

            public Notification(string text, ColorPair colors)
            {
                this.text = text;
                this.colors = colors;
            }

            public void Apply(Image background, Text textComponent)
            {
                textComponent.text = text;
                textComponent.color = colors.foregroundColor;
                if(background != null)
                {
                    background.color = colors.backgroundColor;
                }
            }
        }

        [Serializable]
        private struct ColorPair
        {
            public Color backgroundColor;
            public Color foregroundColor;
        }
    }
}
