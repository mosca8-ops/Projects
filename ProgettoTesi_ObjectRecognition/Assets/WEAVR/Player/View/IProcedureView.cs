using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Localization;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Player.Views
{
    public interface IProcedureView : IView
    {
        Texture2D ProcedureImage { get; set; }
        string ProcedureName { get; set; }
        string AssignedGroupName { get; set; }
        string ProcedureOverview { get; set; }
        string ProcedureLastUpdate { get; set; }
        string ProcedureEstTime { get; set; }
        int ProcedureStepNumber { get; set; }
        int ProcedureCompletedTimes { get; set; }

        void SetStatus(ProcedureFlags status, Func<float> progressForLoader);

        Language Language { get; set; }
        void SetAvailableLanguages(IEnumerable<Language> languages);

        event Action OnRemove;
        event Action OnStart;
        event Action OnUpdate;
        event Action OnDownload;
    }
}
