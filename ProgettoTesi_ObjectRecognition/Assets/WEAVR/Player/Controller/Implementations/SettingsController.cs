using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Core;
using TXT.WEAVR.Localization;
using TXT.WEAVR.Player.Model;
using TXT.WEAVR.Player.Views;
using UnityEngine;

namespace TXT.WEAVR.Player.Controller
{

    public class SettingsController : BaseController, ISettingsController
    {
        public ISettingsView SettingsView { get; private set; }
        public ISettingsModel Settings { get; private set; }
        public IUserModel UserModel { get; private set; }

        private string[] m_graphicsQualities;
        public string[] GraphicsQualities
        {
            get
            {
                if(m_graphicsQualities == null)
                {
                    m_graphicsQualities = new[] { Translate("Low"), Translate("Medium"), Translate("High") };
                }
                return m_graphicsQualities;
            }
        }

        public SettingsController(IDataProvider provider) : base(provider)
        {
            SettingsView = provider.GetView<ISettingsView>();
            Settings = provider.GetModel<ISettingsModel>();
            UserModel = provider.GetModel<IUserModel>();

            Initialize();
        }

        private void Initialize()
        {

        }

        public void ShowView()
        {
            PrepareView();
            SettingsView.Show();
        }

        private void PrepareView()
        {
            SettingsView.ClearSettings();
            foreach (var (setting, value) in Settings.GetSettings())
            {
                SettingsView.AddSettingLine(setting, value, v => Settings.Set(setting, v));
            }

            SettingsView.OnChangePassword -= SettingsView_ChangePassword;
            SettingsView.OnChangePassword += SettingsView_ChangePassword;

            SettingsView.OnChangeApplicationLanguage -= SettingsView_ChangeApplicationLanguage;
            SettingsView.OnChangeApplicationLanguage += SettingsView_ChangeApplicationLanguage;

            SettingsView.OnChangeGraphicsQuality -= SettingsView_OnChangeGraphicsQuality;
            SettingsView.OnChangeGraphicsQuality += SettingsView_OnChangeGraphicsQuality;

            SettingsView.OnAutoProceduresUpdates -= SettingsView_AutoProceduresUpdatesChanged;
            SettingsView.OnAutoProceduresUpdates += SettingsView_AutoProceduresUpdatesChanged;

            SettingsView.OnClearCache -= SettingsView_ClearCache;
            SettingsView.OnClearCache += SettingsView_ClearCache;

            SettingsView.OnResetSettings -= SettingsView_ResetSettings;
            SettingsView.OnResetSettings += SettingsView_ResetSettings;

            // User Data
            SettingsView.UserName = UserModel.CurrentUser.FullName;
            SettingsView.UserDepartment = UserModel.CurrentUser.Department;
            SettingsView.UserRole = UserModel.AuthUser.Roles.FirstOrDefault();
            // TODO: Add user icon to the view

            SettingsView.AutoUpdateProcedures = Settings.AutoUpdateProcedures;
            // TODO: Use the UI Localization
            SettingsView.CurrentLanguage = LocalizationManager.Current.CurrentLanguage;

            SettingsView.GraphicsQuality = GraphicsQualities[QualitySettings.GetQualityLevel()];
        }

        private async void SettingsView_OnChangeGraphicsQuality()
        {
            var level = await PopupManager.ShowDropdownAsync("Graphics Quality", 
                                        "Select Quality", 
                                        QualitySettings.GetQualityLevel(), 
                                        GraphicsQualities.Select(l => new PopupOption(l)));
            QualitySettings.SetQualityLevel(level);
            SettingsView.GraphicsQuality = GraphicsQualities[level];
        }

        private void SettingsView_ResetSettings()
        {
            // TODO: Reset Player Settings
        }

        private async void SettingsView_ClearCache()
        {
            if (await PopupManager.ShowConfirmAsync(Translate("Clean up Procedures"), 
                                Translate("Are you sure you want to clean up procedures?")))
            {
                DataProvider.GetController<ILibraryController>()?.CleanUp();
            }
        }

        private void SettingsView_AutoProceduresUpdatesChanged(bool value)
        {
            Settings.AutoUpdateProcedures = value;
        }

        private async void SettingsView_ChangeApplicationLanguage()
        {
            // TODO: Use a separate UI Localization
            var languages = Language.AllLanguages.ToList();
            int languageIndex = await PopupManager.ShowDropdownAsync("Language",
                        "Select Language",
                        0,
                        languages.Select(l => new PopupOption()
                        {
                            text = l.DisplayName,
                            image = l.Icon,
                        }));
            var selectedLanguage = languages[languageIndex];
            SettingsView.CurrentLanguage = selectedLanguage;
        }

        private void SettingsView_ChangePassword()
        {
            var changePasswordView = DataProvider.GetView<IChangePasswordView>();
            if (changePasswordView != null)
            {
                changePasswordView.OnHide -= ChangePasswordView_OnHide;
                changePasswordView.OnHide += ChangePasswordView_OnHide;
                changePasswordView.OnPasswordChange -= ChangePasswordView_OnPasswordChange;
                changePasswordView.OnPasswordChange += ChangePasswordView_OnPasswordChange;

                changePasswordView.Show();
            }
        }

        private async void ChangePasswordView_OnPasswordChange(string oldPassword, string newPassword)
        {
            var view = DataProvider.GetView<IChangePasswordView>();
            view?.StartLoading(Translate("Changing..."));

            try
            {
                var authController = DataProvider.GetController<IAuthenticationController>();
                var success = await authController.ChangePassword(oldPassword, newPassword);

                if (success)
                {
                    view?.Hide();
                }
                else
                {
                    if (view.IsVisible)
                    {
                        view.Error = Translate(WeavrPlayer.Labels.CannotChangePasswordError);
                        view.Refresh();
                    }
                    else
                    {
                        PopupManager.ShowError(Translate(WeavrPlayer.Labels.ChangePasswordError), Translate(WeavrPlayer.Labels.CannotChangePasswordError));
                    }
                }
            }
            finally
            {
                view?.StopLoading();
            }
        }

        private void ChangePasswordView_OnHide(IView view)
        {
            view.OnHide -= ChangePasswordView_OnHide;
            if (view is IChangePasswordView changePasswordView)
            {
                changePasswordView.OnPasswordChange -= ChangePasswordView_OnPasswordChange;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            SettingsView.OnChangePassword -= SettingsView_ChangePassword;
            SettingsView.OnChangeApplicationLanguage -= SettingsView_ChangeApplicationLanguage;
            SettingsView.OnAutoProceduresUpdates -= SettingsView_AutoProceduresUpdatesChanged;
            SettingsView.OnClearCache -= SettingsView_ClearCache;
            SettingsView.OnResetSettings -= SettingsView_ResetSettings;
        }
    }
}
