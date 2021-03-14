using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Common {

    [AddComponentMenu("WEAVR/Setup/Notification Manager")]
    public class NotificationManager : MonoBehaviour, IWeavrSingleton {

        [Serializable]
        public class UnityEventString : UnityEvent<string> { } 

        #region [  STATIC PART  ]
        private static NotificationManager s_instance;

        public static NotificationManager Instance
        {
            get
            {
                if (!s_instance)
                {
                    s_instance = Weavr.TryGetInAnyScene<NotificationManager>();
                    if(s_instance) s_instance.Awake();
                }
                return s_instance;
            }
        }

        [Serializable]
        protected class NotificationConfig
        {
            public bool showInfo;
            public bool showWarning;
            public bool showError;
        }

        private static NotificationConfig s_config;

        protected static NotificationConfig Config
        {
            get
            {
                if(s_config == null)
                {
                    if(!Weavr.TryGetConfig("notifications.json", out s_config))
                    {
                        s_config = new NotificationConfig()
                        {
                            showInfo = true,
                            showWarning = true,
                            showError = true,
                        };
                        Weavr.WriteToConfigFile("notifications.json", JsonUtility.ToJson(s_config));
                    }
                }
                return s_config;
            }
        }

        public static bool ForcedNotifications { get; internal set; }

        public static void NotificationInfo(string text)
        {
            if(Instance) Instance.NotifyInfo(text);
        }

        public static void NotificationWarning(string text)
        {
            if(Instance) Instance.NotifyWarning(text);
        }

        public static void NotificationError(string text)
        {
            if(Instance) Instance.NotifyError(text);
        }

        public static void ShowGeneric(string text)
        {
            if(Instance) Instance.Show(text);
        }

        public static void ShowGeneric(Sprite image, string text)
        {
            if(Instance) Instance.Show(image, text);
        }

        public static void NotificationInfo(string text, float showUpTime)
        {
            if(Instance) Instance.NotifyInfo(text, showUpTime);
        }

        public static void NotificationWarning(string text, float showUpTime)
        {
            if(Instance) Instance.NotifyWarning(text, showUpTime);
        }

        public static void NotificationError(string text, float showUpTime)
        {
            if(Instance) Instance.NotifyError(text, showUpTime);
        }

        public static void ShowGeneric(string text, float showUpTime)
        {
            if(Instance) Instance.Show(text, showUpTime);
        }

        public static void ShowGeneric(Sprite image, string text, float showUpTime)
        {
            if(Instance) Instance.Show(text, image, showUpTime);
        }

        public static void ShowGeneric(Sprite image)
        {
            if(Instance) Instance.Show(image);
        }

        public Notification GetNotificationObject()
        {
            return Instance ? Instance.GetNotification() : null;
        }

        #endregion

        [SerializeField]
        [Tooltip("If slave, then this manager will subscribe to other managers in order to sync notifications, " +
            "otherwise it will be destroyed if at least one manager already exists")]
        private bool m_isSlave;
        [SerializeField]
        [Tooltip("Whether to mute notification sounds or not")]
        private bool m_muteSounds;
        [SerializeField]
        private bool m_linkToDebug;
        [SerializeField]
        private bool m_showImmediately = true;
        [SerializeField]
        [Draggable]
        private RectTransform m_container;

        [Header("Samples")]
        [SerializeField]
        [Draggable]
        private Notification m_infoSample;
        [SerializeField]
        [Draggable]
        private Notification m_errorSample;
        [SerializeField]
        [Draggable]
        private Notification m_warningSample;

        [SerializeField]
        [Draggable]
        private Notification m_genericNotification;

        [Space]
        public UnityEventString OnInfo;
        public UnityEventString OnError;
        public UnityEventString OnWarning;
        public UnityEventString OnGeneric;
        
        private event Action<Sprite> m_onGenericWithSpriteOnly;
        private event Action<Sprite, string> m_onGenericWithSpriteAndText;

        private List<Notification> m_infoSamples;
        private List<Notification> m_errorSamples;
        private List<Notification> m_warningSamples;

        private List<Notification> m_samples;

        private List<Notification> m_queue;

        private event Action<string, float> m_OnInfoWithTime;
        private event Action<string, float> m_OnErrorWithTime;
        private event Action<string, float> m_OnWarningWithTime;

        private event Action<string, float> m_OnGenericWithTextAndTime;
        private event Action<Sprite, float> m_OnGenericWithSpriteAndTime;
        private event Action<string, Sprite, float> m_OnGenericWithTextSpriteAndTime;

        private void Reset()
        {
            m_container = GetComponentInChildren<RectTransform>();
        }

        private void Awake()
        {
            if (!s_instance)
            {
                if (m_isSlave)
                {
                    s_instance = SceneTools.GetComponentsInAllScenes<NotificationManager>()
                                           .OrderBy(n => n.m_isSlave)
                                           .FirstOrDefault(n => n != this && n.gameObject.activeInHierarchy && n.enabled);
                    if (!s_instance)
                    {
                        s_instance = this;
                    }
                    else
                    {
                        s_instance.Awake();
                    }
                }
                else
                {
                    s_instance = this;
                }
            }
            if(s_instance != this)
            {
                if (m_isSlave)
                {
                    UnregisterFromEvents(s_instance);
                    RegisterToEvents(s_instance);
                }
                else if (s_instance.m_isSlave)
                {
                    s_instance = this;
                }
                else
                {
                    Destroy(gameObject);
                    return;
                }
            }

            if(m_infoSamples == null)
            {
                m_infoSamples = new List<Notification>();
            }

            if (m_errorSamples == null)
            {
                m_errorSamples = new List<Notification>();
            }

            if (m_warningSamples == null)
            {
                m_warningSamples = new List<Notification>();
            }

            if(m_samples == null)
            {
                m_samples = new List<Notification>();
            }

            if(m_queue == null)
            {
                m_queue = new List<Notification>();
            }
        }

        private void OnDisable()
        {
            if (m_isSlave && s_instance)
            {
                UnregisterFromEvents(s_instance);
            }
        }

        private void OnDestroy()
        {
            if(s_instance == this)
            {
                s_instance = null;
            }
        }

        private void RegisterToEvents(NotificationManager master)
        {
            master.OnInfo.AddListener(NotifyInfo);
            master.OnError.AddListener(NotifyError);
            master.OnWarning.AddListener(NotifyWarning);
            master.OnGeneric.AddListener(Show);

            master.m_onGenericWithSpriteOnly += Show;
            master.m_onGenericWithSpriteAndText += Show;

            master.m_OnErrorWithTime += NotifyError;
            master.m_OnInfoWithTime += NotifyInfo;
            master.m_OnWarningWithTime += NotifyWarning;

            master.m_OnGenericWithTextAndTime += Show;
            master.m_OnGenericWithSpriteAndTime += Show;
            master.m_OnGenericWithTextSpriteAndTime += Show;
        }

        private void UnregisterFromEvents(NotificationManager master)
        {
            master.OnInfo.RemoveListener(NotifyInfo);
            master.OnError.RemoveListener(NotifyError);
            master.OnWarning.RemoveListener(NotifyWarning);
            master.OnGeneric.RemoveListener(Show);

            master.m_onGenericWithSpriteOnly -= Show;
            master.m_onGenericWithSpriteAndText -= Show;

            master.m_OnErrorWithTime -= NotifyError;
            master.m_OnInfoWithTime -= NotifyInfo;
            master.m_OnWarningWithTime -= NotifyWarning;

            master.m_OnGenericWithTextAndTime -= Show;
            master.m_OnGenericWithSpriteAndTime -= Show;
            master.m_OnGenericWithTextSpriteAndTime -= Show;
        }

        public void Show(string text)
        {
            var notification = GetGenericNotification();
            notification.Text = text;
            if (m_linkToDebug) { WeavrDebug.Log(this, text); }
            Enqueue(notification);
            OnGeneric.Invoke(text);
        }

        public void Show(Sprite image, string text)
        {
            var notification = GetGenericNotification();
            notification.Text = text;
            notification.Image = image;
            if (m_linkToDebug) { WeavrDebug.Log(this, text); }
            Enqueue(notification);
            OnGeneric.Invoke(text);
        }

        public void Show(Sprite image)
        {
            var notification = GetGenericNotification();
            notification.Image = image;
            Enqueue(notification);
            OnGeneric.Invoke("");
        }

        public void Show(string text, float duration)
        {
            var notification = GetGenericNotification();
            notification.Text = text;
            notification.Duration = duration;
            if (m_linkToDebug) { WeavrDebug.Log(this, text); }
            Enqueue(notification);
            OnGeneric.Invoke(text);
        }

        public void Show(string text, Sprite image, float duration)
        {
            var notification = GetGenericNotification();
            notification.Text = text;
            notification.Image = image;
            notification.Duration = duration;
            if (m_linkToDebug) { WeavrDebug.Log(this, text); }
            Enqueue(notification);
            OnGeneric.Invoke(text);
        }

        public void Show(Sprite image, float duration)
        {
            var notification = GetGenericNotification();
            notification.Image = image;
            notification.Duration = duration;
            Enqueue(notification);
            OnGeneric.Invoke("");
        }

        public Notification GetNotification()
        {
            return GetNotification(m_genericNotification, m_samples, Notification_Hidden);
        }

        private Notification GetGenericNotification()
        {
            return GetNotification(m_genericNotification, m_samples, Notification_Hidden);
        }

        private Notification GetInfoNotification()
        {
            return GetNotification(m_infoSample, m_infoSamples, NotificationInfo_Hidden);
        }

        private Notification GetWarningNotification()
        {
            return GetNotification(m_warningSample, m_warningSamples, NotificationWarning_Hidden);
        }

        private Notification GetErrorNotification()
        {
            return GetNotification(m_errorSample, m_errorSamples, NotificationError_Hidden);
        }

        private Notification GetNotification(Notification sample, List<Notification> list, Action<Notification> onHideEvent)
        {
            if (list.Count > 0)
            {
                var returnValue = list[0];
                list.RemoveAt(0);
                returnValue.IsMuted = m_muteSounds;
                //returnValue.gameObject.SetActive(true);
                return returnValue;
            }

            var notification = Instantiate(sample);
            notification.Hidden -= onHideEvent;
            notification.Hidden += onHideEvent;
            notification.transform.SetParent(m_container ? m_container : sample.transform.parent, false);
            notification.IsMuted = m_muteSounds;
            return notification;
        }

        private Notification Enqueue(Notification notification)
        {
            if (!gameObject.activeInHierarchy)
            {
                notification.gameObject.SetActive(true);
                return notification;
            }
            notification.gameObject.SetActive(false);
            m_queue.Add(notification);
            if(m_queue.Count == 1)
            {
                StartCoroutine(HandleNotificationLoop());
            }
            return notification;
        }

        private IEnumerator HandleNotificationLoop()
        {
            while (m_queue.Count > 0)
            {
                var notification = m_queue[0];
                if (notification.transform.IsChildOf(transform))
                {
                    notification.transform.SetAsLastSibling();
                }
                notification.gameObject.SetActive(true);
                if (!m_showImmediately)
                {
                    yield return new WaitForSeconds(notification.FullDuration);
                }
                m_queue.RemoveAt(0);
            }
        }

        private void Notification_Hidden(Notification notification)
        {
            ReclaimNotification(notification, m_genericNotification, m_samples);
        }

        private void NotificationInfo_Hidden(Notification notification)
        {
            ReclaimNotification(notification, m_infoSample, m_infoSamples);
        }

        private void NotificationWarning_Hidden(Notification notification)
        {
            ReclaimNotification(notification, m_warningSample, m_warningSamples);
        }

        private void NotificationError_Hidden(Notification notification)
        {
            ReclaimNotification(notification, m_errorSample, m_errorSamples);
        }

        private void ReclaimNotification(Notification notification, Notification sample, List<Notification> list)
        {
            if (!list.Contains(notification) && sample)
            {
                notification.UpdateFrom(sample);
                notification.gameObject.SetActive(false);
                list.Add(notification);
            }
        }

        public void NotifyInfo(string text)
        {
            if (!ForcedNotifications && !Config.showInfo) { return; }
            var notification = GetInfoNotification();
            notification.Text = text;
            if (m_linkToDebug) { WeavrDebug.Log(this, $"[Info]: {text}"); }
            Enqueue(notification);
            OnInfo.Invoke(text);
        }

        public void NotifyWarning(string text)
        {
            if (!ForcedNotifications && !Config.showWarning) { return; }
            var notification = GetWarningNotification();
            notification.Text = text;
            if (m_linkToDebug) { WeavrDebug.Log(this, $"[Warning]: {text}"); }
            Enqueue(notification);
            OnWarning.Invoke(text);
        }

        public void NotifyError(string text)
        {
            if (!ForcedNotifications && !Config.showError) { return; }
            var notification = GetErrorNotification();
            notification.Text = text;
            if (m_linkToDebug) { WeavrDebug.Log(this, $"[Error]: {text}"); }
            Enqueue(notification);
            OnError.Invoke(text);
        }

        public void NotifyInfo(string text, float duration)
        {
            if (!ForcedNotifications && !Config.showInfo) { return; }
            var notification = GetInfoNotification();
            notification.Text = text;
            notification.Duration = duration;
            if (m_linkToDebug) { WeavrDebug.Log(this, $"[Info {duration} seconds]: {text}"); }
            Enqueue(notification);
            if (m_OnInfoWithTime != null)
            {
                m_OnInfoWithTime(text, duration);
            }
            else
            {
                OnInfo.Invoke(text);
            }
        }

        public void NotifyWarning(string text, float duration)
        {
            if (!ForcedNotifications && !Config.showWarning) { return; }
            var notification = GetWarningNotification();
            notification.Text = text;
            notification.Duration = duration;
            if (m_linkToDebug) { WeavrDebug.Log(this, $"[Warning {duration} seconds]: {text}"); }
            Enqueue(notification);
            if (m_OnWarningWithTime != null)
            {
                m_OnWarningWithTime(text, duration);
            }
            else
            {
                OnWarning.Invoke(text);
            }
        }

        public void NotifyError(string text, float duration)
        {
            if (!ForcedNotifications && !Config.showError) { return; }
            var notification = GetErrorNotification();
            notification.Text = text;
            notification.Duration = duration;
            if (m_linkToDebug) { WeavrDebug.Log(this, $"[Error {duration} seconds]: {text}"); }
            Enqueue(notification);
            if (m_OnErrorWithTime != null)
            {
                m_OnErrorWithTime(text, duration);
            }
            else
            {
                OnError.Invoke(text);
            }
        }
    }
}
