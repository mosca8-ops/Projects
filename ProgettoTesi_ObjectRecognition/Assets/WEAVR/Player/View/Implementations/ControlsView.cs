
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TXT.WEAVR.Player.Views
{

    public class ControlsView : MonoBehaviour, IControlsView
    {
        [SerializeField]
        [Draggable]
        private Button m_quit;
        [SerializeField]
        [Draggable]
        private Button m_back;
        [SerializeField]
        [Draggable]
        private Button m_logout;
        [SerializeField]
        [Draggable]
        private Button m_settings;
        [SerializeField]
        [Draggable]
        private Button m_library;
        [SerializeField]
        [Draggable]
        private Button m_home;
        [SerializeField]
        [Draggable]
        private Button m_notifications;
        [SerializeField]
        [Draggable]
        private Button m_collaboration;
        [Tooltip("Button for muting audio")]
        [Draggable]
        public Button m_mutedAudio;
        [Tooltip("Button for unmuting audio")]
        [Draggable]
        public Button m_unmutedAudio;

        public Button Quit => m_quit;
        public Button Back => m_back;
        public Button Logout => m_logout;
        public Button Settings => m_settings;
        public Button Library => m_library;
        public Button Home => m_home;
        public Button Notifications => m_notifications;
        public Button Collaboration => m_collaboration;
        public Button MutedAudio => m_mutedAudio;
        public Button UnmutedAudio => m_unmutedAudio;

        public event Action OnQuit;
        public event Action OnLogout;
        public event Action OnSettings;
        public event Action OnHome;
        public event Action OnNotifications;
        public event Action OnCollaboration;
        public event Action OnMuteAudio;
        public event Action OnUnmuteAudio;
        public event OnUserActionDelegate OnUserAction;

        public event ViewDelegate OnBack;

        private void OnEnable()
        {
            UnregisterEvents();
            RegisterEvents();
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }

        private void RegisterEvents()
        {
            RegisterButtonToEvent(Logout, InvokeLogout);
            RegisterButtonToEvent(Settings, InvokeSettings);
            RegisterButtonToEvent(Home, InvokeHome);
            RegisterButtonToEvent(Notifications, InvokeNotifications);
            RegisterButtonToEvent(Collaboration, InvokeCollaboration);
            RegisterButtonToEvent(Quit, InvokeQuit);
            RegisterButtonToEvent(Back, InvokeBack);
            RegisterButtonToEvent(UnmutedAudio, InvokeMuteAudio);
            RegisterButtonToEvent(MutedAudio, InvokeUnmuteAudio);
        }

        private void UnregisterEvents()
        {
            UnregisterButtonFromEvent(Logout, InvokeLogout);
            UnregisterButtonFromEvent(Settings, InvokeSettings);
            UnregisterButtonFromEvent(Home, InvokeHome);
            UnregisterButtonFromEvent(Notifications, InvokeNotifications);
            UnregisterButtonFromEvent(Collaboration, InvokeCollaboration);
            UnregisterButtonFromEvent(Quit, InvokeQuit);
            UnregisterButtonFromEvent(Back, InvokeBack);
            UnregisterButtonFromEvent(UnmutedAudio, InvokeMuteAudio);
            UnregisterButtonFromEvent(MutedAudio, InvokeUnmuteAudio);
        }

        private void RegisterButtonToEvent(Button button, UnityAction action)
        {
            if (button)
            {
                button.onClick.AddListener(action);
            }
            else
            {
                WeavrDebug.LogError(this, $"Button was not set");
            }
        }

        private void UnregisterButtonFromEvent(Button button, UnityAction action)
        {
            if (button)
            {
                button.onClick.RemoveListener(action);
            }
        }

        private void InvokeLogout() => OnLogout?.Invoke();
        public void InvokeSettings() => OnSettings?.Invoke();
        public void InvokeHome() => OnHome?.Invoke();
        public void InvokeNotifications() => OnNotifications?.Invoke();
        public void InvokeCollaboration() => OnCollaboration?.Invoke();
        public void InvokeBack() => OnBack?.Invoke(this);

        public void InvokeQuit()
        {
            PopupManager.Show(PopupManager.MessageType.Question, "Application Quit", "Do you want to quit the application?", () => OnQuit?.Invoke(), null);
        }

        public void InvokeMuteAudio()
        {
            MutedAudio.gameObject.SetActive(true);
            UnmutedAudio.gameObject.SetActive(false);

            OnMuteAudio?.Invoke();
        }

        public void InvokeUnmuteAudio()
        {
            MutedAudio.gameObject.SetActive(false);
            UnmutedAudio.gameObject.SetActive(true);

            OnUnmuteAudio?.Invoke();
        }

        #region [  UNUSED VIEW LOGIC  ]
        public bool IsVisible {
            get; set;
        }
        public event ViewDelegate OnShow;
        public event ViewDelegate OnHide;

        public void Hide()
        {

        }

        public void Show()
        {

        }
        #endregion
    }
}
