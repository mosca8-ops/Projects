using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Localization;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class NotifyMessageAction : BaseAction
    {
        public enum MessageType
        {
            Info,
            Warning,
            Error,
            Custom,
        }

        [SerializeField]
        [Tooltip("Type of the notification")]
        private MessageType m_type = MessageType.Info;
        [SerializeField]
        [Tooltip("Time to show the notification, if not set, then the default one is used")]
        private OptionalProxyFloat m_duration;
        [SerializeField]
        [Tooltip("The image to use for the custom notification")]
        [ShowOnEnum(nameof(m_type), (int)MessageType.Custom)]
        [Draggable]
        private Sprite m_image;
        [SerializeField]
        [Tooltip("The text of the notification")]
        private ValueProxyLocalizedString m_message;
        private ExecutionFlow m_currentFlow;
        
        public override bool Execute(float dt)
        {
            NotificationManager.ForcedNotifications = true;
            switch (m_type)
            {
                case MessageType.Info:
                    if (m_duration.enabled) { NotificationManager.NotificationInfo(m_message, m_duration); }
                    else { NotificationManager.NotificationInfo(m_message); }
                    break;
                case MessageType.Error:
                    if (m_duration.enabled)
                    {
                        NotificationManager.NotificationError(m_message, m_duration);
                    }
                    else
                    {
                        NotificationManager.NotificationError(m_message);
                    }
                    break;
                case MessageType.Warning:
                    if (m_duration.enabled)
                    {
                        NotificationManager.NotificationWarning(m_message, m_duration);
                    }
                    else
                    {
                        NotificationManager.NotificationWarning(m_message);
                    }
                    break;
                case MessageType.Custom:
                    if (m_image)
                    {
                        if (m_duration.enabled)
                        {
                            NotificationManager.ShowGeneric(m_image, m_message, m_duration);
                        }
                        else { NotificationManager.ShowGeneric(m_image, m_message); }
                    }
                    else
                    {
                        if (m_duration.enabled)
                        {
                            NotificationManager.ShowGeneric(m_message, m_duration);
                        }
                        else { NotificationManager.ShowGeneric(m_message); }
                    }
                    break;
            }
            NotificationManager.ForcedNotifications = false;
            return true;
        }
        
        public override string GetDescription()
        {
            string type = m_type != MessageType.Custom ? m_type.ToString() : m_image ? $"with {m_image.name}" : string.Empty;
            return $"Notify {type}: {m_message}" + (m_duration.enabled ? $" for {m_duration.value} seconds" : string.Empty);
        }

    }
}
