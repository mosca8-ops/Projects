using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TXT.WEAVR.Localization;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Player.Views
{
    public delegate void StatusChangedDelegate(IProcedureViewModel viewModel, ProcedureFlags newStatus);

    public interface IProcedureViewModel : IViewModel
    {
        Guid Id { get; }
        Texture2D Image { get; }
        string Name { get; }
        string Description { get; }
        string AverageTime { get; }
        int NumberOfSteps { get; }
        int NumberOfCompletions { get; }
        DateTime AssignedDate { get; }
        DateTime LastUpdate { get; }
        ProcedureFlags Status { get; }
        string CollaborationName { get; }
        IEnumerable<IGroupViewModel> AssignedGroups { get; }
        IEnumerable<IUserViewModel> Instructors { get; }
        IEnumerable<IUserViewModel> AssignedStudents { get; }
        IEnumerable<IUserViewModel> LiveStudents { get; }
        IEnumerable<Language> Languages { get; }
        Language DefaultLanguage { get; }

        float GetSyncProgress();

        event StatusChangedDelegate StatusChanged;
        Task Sync(Action<float> progressCallback = null);
        Task<bool> Delete();
    }
}
