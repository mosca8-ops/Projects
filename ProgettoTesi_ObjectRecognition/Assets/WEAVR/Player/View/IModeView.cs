using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Localization;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Player.Views
{
    public interface IModeView : IView
    {
        string ProcedureName { get; set; }

        event ViewDelegate<IModeView> OnStart;
        event ViewDelegate<IModeView> OnCancel;

        Language Language { get; set; }
        void SetAvailableLanguages(IEnumerable<Language> languages);

        IExecutionModeViewModel SelectedMode { get; set; }
        void SetExecutionModes(IEnumerable<IExecutionModeViewModel> executionModes);
    }
}

