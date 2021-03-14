using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Maintenance;

namespace TXT.WEAVR.RemoteControl
{
    [AddComponentMenu("WEAVR/Remote Control/Commands/Push Button Controls")]
    public class PushButtonControlCommands : BaseCommandUnit
    {
        [RemoteEvent]
        public event Action<string> OnButtonPressed;
        [RemoteEvent]
        public event Action<string> OnButtonReleased;

        private AbstractPushButton m_lastPushButton;
        public AbstractPushButton LastPushButton
        {
            get => m_lastPushButton;
            set
            {
                if (m_lastPushButton != value)
                {
                    if (m_lastPushButton)
                    {
                        m_lastPushButton.OnDown.RemoveListener(InvokeButtonPressed);
                        m_lastPushButton.OnUp.RemoveListener(InvokeButtonReleased);
                    }
                    m_lastPushButton = value;
                    if (m_lastPushButton)
                    {
                        m_lastPushButton.OnDown.RemoveListener(InvokeButtonPressed);
                        m_lastPushButton.OnDown.AddListener(InvokeButtonPressed);
                        m_lastPushButton.OnUp.RemoveListener(InvokeButtonReleased);
                        m_lastPushButton.OnUp.AddListener(InvokeButtonReleased);
                    }
                }
            }
        }

        [RemotelyCalled]
        public void Press(Guid guid)
        {
            var pushButton = Query.GetComponentByGuid<AbstractPushButton>(guid);
            if (pushButton)
            {
                LastPushButton = pushButton;
                pushButton.CurrentState = AbstractPushButton.State.Down;
            }
        }

        [RemotelyCalled]
        public void Press(string path)
        {
            var pushButton = Query.Find<AbstractPushButton>(QuerySearchType.Scene, path).First();
            if (pushButton)
            {
                LastPushButton = pushButton;
                pushButton.CurrentState = AbstractPushButton.State.Down;
            }
        }

        [RemotelyCalled]
        public void Release(Guid guid)
        {
            var pushButton = Query.GetComponentByGuid<AbstractPushButton>(guid);
            if (pushButton)
            {
                LastPushButton = pushButton;
                pushButton.CurrentState = AbstractPushButton.State.Up;
            }
        }

        [RemotelyCalled]
        public void Release(string path)
        {
            var pushButton = Query.Find<AbstractPushButton>(QuerySearchType.Scene, path).First();
            if (pushButton)
            {
                LastPushButton = pushButton;
                pushButton.CurrentState = AbstractPushButton.State.Up;
            }
        }

        public void InvokeButtonPressed(string path)
        {
            OnButtonPressed?.Invoke(path);
        }

        public void InvokeButtonReleased(string path)
        {
            OnButtonReleased?.Invoke(path);
        }

        public void InvokeButtonPressed(GameObject gameObject)
        {
            if (gameObject == null) return;

            var path = SceneTools.GetGameObjectPath(gameObject);
            InvokeButtonPressed(path);
        }

        public void InvokeButtonReleased(GameObject gameObject)
        {
            if (gameObject == null) return;

            var path = SceneTools.GetGameObjectPath(gameObject);
            InvokeButtonReleased(path);
        }

        private void InvokeButtonPressed()
        {
            InvokeButtonPressed(LastPushButton.GetHierarchyPath());
        }

        private void InvokeButtonReleased()
        {
            InvokeButtonReleased(LastPushButton.GetHierarchyPath());
        }
    }
}
