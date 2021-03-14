using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Linq;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Setup/Notification")]
    public class Notification : MonoBehaviour
    {
        [SerializeField]
        [Draggable]
        private Image m_image;
        [SerializeField]
        [Draggable]
        private Text m_text;
        [SerializeField]
        private float m_duration;
        [SerializeField]
        [EnableIfComponentExists(typeof(Animator))]
        private string m_animatorHideTrigger;
        [SerializeField]
        [EnableIfComponentExists(typeof(Animator))]
        private float m_hideDuration = 1;

        [Header("Audio")]
        [SerializeField]
        [Draggable]
        private AudioClip m_audioClip;
        private AudioSource m_audioSource;
        private Transform m_transformToPlayAudio;

        private bool m_muted;

        private float m_showDeadline;
        private float m_hideDeadline;

        private bool m_animatorHideTriggered;

        private Action m_onHide;
        private Action OnHide {
            get => m_onHide;
            set
            {
                if(m_onHide != value)
                {
                    m_onHide = value;
                    if(m_onHide != null && !gameObject.activeInHierarchy)
                    {
                        m_onHide();
                        m_onHide = null;
                    }
                }
            }
        }

        private Animator m_animator;

        public bool IsMuted
        {
            get => m_muted;
            set
            {
                if(m_muted != value)
                {
                    m_muted = value;
                    if(m_muted && enabled && m_audioSource && m_audioSource.isPlaying && m_audioClip)
                    {
                        m_audioSource.Stop();
                    }
                }
            }
        }

        public float Duration
        {
            get => m_duration;
            set
            {
                if(m_duration != value)
                {
                    m_duration = value;
                    if (enabled)
                    {
                        m_showDeadline = Time.time + m_duration;
                        m_hideDeadline = m_showDeadline + m_hideDuration;
                    }
                }
            }
        }

        public float FullDuration => m_duration + m_hideDuration;

        public event Action<Notification> Hidden;

        public string Text
        {
            get => m_text ? m_text.text : "";
            set
            {
                if(m_text && m_text.text != value)
                {
                    m_text.text = value;
                    Relayout();
                }
            }
        }

        public Sprite Image
        {
            get => m_image ? m_image.sprite : null;
            set
            {
                if(m_image && m_image.sprite != value)
                {
                    m_image.sprite = value;
                    m_image.enabled = value;
                    Relayout();
                }
            }
        }

        private Transform AudioPoint
        {
            get
            {
                if (!m_transformToPlayAudio)
                {
                    m_transformToPlayAudio = Camera.allCameras.FirstOrDefault()?.transform ?? transform;
                }
                return m_transformToPlayAudio;
            }
        }

        public AudioClip Audio
        {
            get => m_audioClip;
            set
            {
                if(m_audioClip != value)
                {
                    m_audioClip = value;
                    if (enabled)
                    {
                        PlayNotificationSound();
                    }
                }
            }
        }

        private void PlayNotificationSound()
        {
            if (m_muted || !m_audioClip) { return; }

            if (m_audioSource)
            {
                m_audioSource.clip = m_audioClip;
                m_audioSource.loop = false;
                m_audioSource.Play();
            }
            else
            {
                AudioSource.PlayClipAtPoint(m_audioClip, AudioPoint.position);
            }
        }

        public void UpdateFrom(Notification notification)
        {
            Text = notification.Text;
            Image = notification.Image;
            Duration = notification.Duration;
            if(m_image && notification.m_image) { m_image.enabled = notification.m_image.enabled; }
        }

        private void OnValidate()
        {
            m_hideDuration = Mathf.Max(0, m_hideDuration);
        }

        private void Awake()
        {
            if (!m_animator)
            {
                m_animator = GetComponent<Animator>();
            }
            if (!m_text) { m_text = GetComponent<Text>(); }
            if (!m_image) { m_image = GetComponent<Image>(); }
            if (!m_audioSource) { m_audioSource = GetComponentInChildren<AudioSource>(); }
        }

        void OnEnable()
        {
            if (m_animator)
            {
                m_animator.enabled = true;
                m_animatorHideTriggered = false;
                //Relayout();
            }
            m_showDeadline = Time.time + m_duration;
            m_hideDeadline = m_showDeadline + m_hideDuration;
            PlayNotificationSound();
        }

        private void OnDisable()
        {
            if(m_audioSource && m_audioClip && m_audioSource.isPlaying && m_audioSource.clip == m_audioClip)
            {
                m_audioSource.Stop();
            }
        }

        private void Relayout()
        {
            if (transform is RectTransform rectT)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectT);
            }
        }

        void Update()
        {
            if(m_showDeadline > Time.time)
            {
                return;
            }
            else if(m_hideDeadline > Time.time)
            {
                if(!m_animatorHideTriggered && m_animator && m_animator.enabled)
                {
                    m_animator.SetTrigger(m_animatorHideTrigger);
                    m_animatorHideTriggered = true;
                }
                m_hideDeadline -= Time.deltaTime;
            }
            else
            {
                if (m_animator)
                {
                    m_animator.ResetTrigger(m_animatorHideTrigger);
                    m_animator.enabled = false;
                }
                if(OnHide != null)
                {
                    OnHide();
                    OnHide = null;
                }
                Hidden?.Invoke(this);
                gameObject.SetActive(false);
            }
        }
    }
}