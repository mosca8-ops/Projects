using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Player.Views
{
    public interface ILibraryView : IView
    {
        IGroupViewModel SelectedGroup { get; set; }
        IEnumerable<IGroupViewModel> Groups { get; set; }

        void AddProcedures(IEnumerable<IProcedureViewModel> procedures);
        void ClearProcedures();
        void RefreshViews();

        event Action<string> OnProcedureSearchUpdated;
        event Action<IGroupViewModel> SelectedGroupChanged;
        event Action<IProcedureViewModel> ProcedureSelected;
        event Action OnRefresh;
    }
}
