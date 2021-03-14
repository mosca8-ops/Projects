using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TXT.WEAVR.Common;
using TXT.WEAVR.Player.Views;
using UnityEngine;

namespace TXT.WEAVR.Player
{
    [AddComponentMenu("WEAVR/Player/Popup Manager")]
    public class PopupManager : MonoBehaviour
    {
        [SerializeField]
        private string m_okLabel = "OK";
        [SerializeField]
        private string m_cancelLabel = "Cancel";
        [Header("Icons")]
        [SerializeField]
        private Texture2D m_infoIcon;
        [SerializeField]
        private Texture2D m_warningIcon;
        [SerializeField]
        private Texture2D m_errorIcon;
        [SerializeField]
        private Texture2D m_questionIcon;
        [SerializeField]
        private Texture2D m_criticalIcon;
        [Header("Components")]
        [SerializeField]
        private PopupView m_infoPopup;
        [SerializeField]
        private PopupView m_errorPopup;
        [SerializeField]
        private PopupView m_okCancelPopup;
        [SerializeField]
        private PopupView m_okPopup;
        [SerializeField]
        private PopupView m_customPopup;
        [SerializeField]
        private PopupView m_customPrimaryActionPopup;
        [SerializeField]
        [Type(typeof(IDropdownPopup))]
        private Component m_dropdownPopup;

        private static List<PopupManager> s_managers = new List<PopupManager>();

        public enum MessageType
        {
            Info, Warning, Error, Critical, Question
        }

        private void Awake()
        {
            if (m_errorPopup && m_errorPopup.Buttons.Count > 0)
            {
                m_errorPopup.Buttons[0].onClick.AddListener(() => m_errorPopup.Hide());
            }
            if (m_okPopup && m_okPopup.Buttons.Count > 0)
            {
                m_okPopup.Buttons[0].onClick.AddListener(() => m_okPopup.Hide());
            }
        }

        /// <summary>
        /// The current PopupManager
        /// </summary>
        /// <remarks>May be NULL if not in scene</remarks>
        public static PopupManager Current => s_managers.Count > 0 ? s_managers[s_managers.Count - 1] : null;

        public static void ShowInfo(string title, string message)
        {
            if (Current.m_infoPopup)
            {
                Current.m_infoPopup.Title = title;
                Current.m_infoPopup.Message = message;
                Current.m_infoPopup.Show();
            }
        }

        public static void ShowError(string title, string message)
        {
            if (Current.m_errorPopup)
            {
                Current.m_errorPopup.Title = title;
                Current.m_errorPopup.Message = message;
                Current.m_errorPopup.Show();
            }
        }

        public static async Task ShowInfoAsync(string title, string message)
        {
            if (Current.m_infoPopup)
            {
                Current.m_infoPopup.Title = title;
                Current.m_infoPopup.Message = message;
                await Current.m_infoPopup.ShowAsync();
            }
        }

        public static async Task ShowErrorAsync(string title, string message)
        {
            if (Current.m_errorPopup)
            {
                Current.m_errorPopup.Title = title;
                Current.m_errorPopup.Message = message;
                await Current.m_errorPopup.ShowAsync();
            }
        }

        public static async Task<bool> ShowConfirmAsync(string title, string message)
        {
            if (Current.m_okCancelPopup)
            {
                bool okClicked = false;
                Current.m_okCancelPopup.Title = title;
                Current.m_okCancelPopup.Message = message;
                Current.m_okCancelPopup.Buttons[0].onClick.RemoveAllListeners();
                Current.m_okCancelPopup.Buttons[0].onClick.AddListener(() => okClicked = true);
                await Current.m_okCancelPopup.ShowAsync();
                Current.m_okCancelPopup.Buttons[0].onClick.RemoveAllListeners();
                return okClicked;
            }
            return false;
        }

        public static void Show(string title, string message, Action okCallback)
        {
            var popup = Current.m_okPopup ? Current.m_okPopup : Current.m_okCancelPopup ? Current.m_okCancelPopup : Current.m_customPopup;
            SetupPopup(popup, title, message, (Current.m_okLabel, okCallback));
            popup.Show();
        }

        public static void Show(string title, string message, Action okCallback, Action cancelCallback)
        {
            var popup = Current.m_okCancelPopup ? Current.m_okCancelPopup : Current.m_customPopup;
            SetupPopup(popup, title, message, (Current.m_okLabel, okCallback), (Current.m_cancelLabel, cancelCallback));
            popup.Show();
        }

        public static void Show(MessageType type, string title, string message, Action okCallback)
        {
            var popup = Current.m_okPopup ? Current.m_okPopup : Current.m_okCancelPopup ? Current.m_okCancelPopup : Current.m_customPopup;
            SetupPopup(popup, title, message, (Current.m_okLabel, okCallback));
            popup.Icon = Current.GetIcon(type);
            popup.Show();
        }

        public static void Show(MessageType type, string title, string message, Action okCallback, Action cancelCallback)
        {
            var popup = Current.m_okCancelPopup ? Current.m_okCancelPopup : Current.m_customPopup;
            SetupPopup(popup, title, message, (Current.m_okLabel, okCallback), (Current.m_cancelLabel, cancelCallback));
            popup.Icon = Current.GetIcon(type);
            popup.Show();
        }

        public static void ShowWithPrimaryAction(MessageType type, string title, string message, params (string label, Action callback)[] actions)
        {
            SetupPopup(Current.m_customPrimaryActionPopup, Current.GetIcon(type), title, message, actions);
            Current.m_customPrimaryActionPopup.Show();
        }

        public static void ShowWithoutPrimaryAction(MessageType type, string title, string message, params (string label, Action callback)[] actions)
        {
            SetupPopup(Current.m_customPopup, Current.GetIcon(type), title, message, actions);
            Current.m_customPopup.Show();
        }

        public static void Close()
        {
            Current.m_infoPopup.Hide();
            Current.m_okPopup.Hide();
            Current.m_okCancelPopup.Hide();
            Current.m_errorPopup.Hide();
            Current.m_customPrimaryActionPopup.Hide();
            Current.m_customPopup.Hide();
        }

        public static async Task<int> ShowAsync(MessageType type, string title, string message, params string[] buttonLabels) => await ShowAsync(type, title, message, false, CancellationToken.None, buttonLabels);
        public static async Task<int> ShowAsync(MessageType type, string title, string message, bool withPrimary, params string[] buttonLabels) => await ShowAsync(type, title, message, withPrimary, CancellationToken.None, buttonLabels);

        public static async Task<int> ShowAsync(MessageType type, string title, string message, bool withPrimary, CancellationToken cancellationToken, params string[] buttonLabels)
        {
            PopupView popup = Current.m_customPopup;

            if (buttonLabels.Length == 0 && Current.m_okPopup)
            {
                popup = Current.m_okPopup;
            }
            else if (withPrimary)
            {
                popup = Current.m_customPrimaryActionPopup;
            }

            var listener = new ButtonListener();

            popup.ResetDefaultIcon();
            popup.Title = title;
            popup.Message = message;

            int minButtons = Mathf.Min(popup.Buttons.Count, buttonLabels.Length);
            for (int i = 0; i < minButtons; i++)
            {
                var popupButton = popup.Buttons[i];
                popupButton.gameObject.SetActive(true);
                popupButton.Label = buttonLabels[i];
                popupButton.onClick.RemoveAllListeners();
                int buttonIndex = i;
                popupButton.onClick.AddListener(() => listener.selectedButton = buttonIndex);
            }
            for (int i = minButtons; i < popup.Buttons.Count; i++)
            {
                popup.Buttons[i].gameObject.SetActive(false);
            }

            popup.Icon = Current.GetIcon(type);

            popup.Show();

            while (listener.selectedButton < 0 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Yield();
            }

            return listener.selectedButton;
        }

        public void Show(string title, string description, int selectedOption, IEnumerable<PopupOption> options, Action<int> onSelection)
        {
            if (Current.m_dropdownPopup is IDropdownPopup popup)
            {
                popup.Show(title, description, selectedOption, options, onSelection);
            }
        }

        public static async Task<int> ShowDropdownAsync(string title, string description, int selectedOption, IEnumerable<PopupOption> options)
        {
            if(Current.m_dropdownPopup is IDropdownPopup popup)
            {
                return await popup.ShowAsync(title, description, selectedOption, options);
            }
            return selectedOption;
        }

        private static void SetupPopup(PopupView popup, params (string label, Action callback)[] buttons)
        {
            popup.ResetDefaultIcon();
            int minButtons = Mathf.Min(popup.Buttons.Count, buttons.Length);
            for (int i = 0; i < minButtons; i++)
            {
                var (label, callback) = buttons[i];

                var popupButton = popup.Buttons[i];
                popupButton.gameObject.SetActive(true);
                popupButton.Label = label;
                popupButton.onClick.RemoveAllListeners();
                popupButton.onClick.AddListener(() =>
                {
                    callback?.Invoke();
                    if (popup != null)
                    {
                        popup.Hide();
                    }
                });
            }
            for (int i = minButtons; i < popup.Buttons.Count; i++)
            {
                popup.Buttons[i].gameObject.SetActive(false);
            }
        }

        private static void SetupPopup(PopupView popup, string message, params (string label, Action callback)[] buttons)
        {
            SetupPopup(popup, buttons);
            popup.Message = message;
        }

        private static void SetupPopup(PopupView popup, string title, string message, params (string label, Action callback)[] buttons)
        {
            SetupPopup(popup, buttons);
            popup.Title = title;
            popup.Message = message;
        }

        private static void SetupPopup(PopupView popup, Texture2D icon, string title, string message, params (string label, Action callback)[] buttons)
        {
            SetupPopup(popup, title, message, buttons);
            popup.Icon = icon;
        }



        public string OkLabel {
            get => m_okLabel;
            set => m_okLabel = value;
        }

        public string CancelLabel {
            get => m_cancelLabel;
            set => m_cancelLabel = value;
        }

        protected void OnEnable()
        {
            s_managers.Remove(this);
            s_managers.Insert(0, this);
        }

        protected void OnDisable()
        {
            s_managers.Remove(this);
        }

        private Texture2D GetIcon(MessageType type)
        {
            switch (type)
            {
                case MessageType.Info: return m_infoIcon;
                case MessageType.Warning: return m_warningIcon;
                case MessageType.Error: return m_errorIcon;
                case MessageType.Question: return m_questionIcon;
                case MessageType.Critical: return m_criticalIcon;
                default: return null;
            };
        }

        private class ButtonListener
        {
            public int selectedButton = -1;
        }
    }
}
