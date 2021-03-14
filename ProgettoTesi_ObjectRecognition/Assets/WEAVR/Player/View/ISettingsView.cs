using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Localization;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Player.Views
{
    public interface ISettingsView : IView
    {
        Texture2D UserImage { get; set; }

        string UserName { get; set; }

        string UserDepartment { get; set; }

        string UserRole { get; set; }

        bool AutoUpdateProcedures { get; set; }
        Language CurrentLanguage { get; set; }
        string GraphicsQuality { get; set; }

        event UnityAction OnChangePassword;

        event UnityAction OnChangeApplicationLanguage;

        event UnityAction OnChangeGraphicsQuality;

        event UnityAction<bool> OnAutoProceduresUpdates;

        event UnityAction OnClearCache;

        event UnityAction OnResetSettings;


        void AddSettingLine(string key, object value, Action<object> onChanged);
        void AddSettingLine(string key, Action onSelected);
        void ClearSettings();

    }



}

