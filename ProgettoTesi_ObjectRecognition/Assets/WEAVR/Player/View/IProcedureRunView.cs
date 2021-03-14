using System;

namespace TXT.WEAVR.Player.Views
{
    public interface IStandardButtonsSet
    {
        IClickItem ResetCameraOrbit { get; }
        ISwitchItem LockCameraOrbit { get; } 
    }

    public interface IProcedureRunView : IView
    {
        string ProcedureTitle { get; set; }
        string StepTitle { get; set; }
        string StepNumber { get; set; }
        string StepDescription { get; set; }

        bool EnableNavigationButtons { get; set; }
        string ExitButtonLabel { get; set; }

        float ProcedureProgress { get; set; }
        bool ShowAllButtons { get; set; }

        void StartProgress(int id, string label, Func<float> progressFunctor);
        void StopProgress(int id);

        event ViewDelegate<IProcedureRunView> OnNext;
        event ViewDelegate<IProcedureRunView> OnPrev;
        event ViewDelegate<IProcedureRunView> OnExit;

        event ViewDelegate<IProcedureRunView> OnRestart;

        IStandardButtonsSet GetStandardButtons();

        void AddButton(IButtonViewModel button);
        void RemoveButton(IButtonViewModel button);
        void AddSpecialButton(IClickItem button);
        void RemoveSpecialButton(IClickItem button);
        void ClearButtons();

        void AddMapItem(IItemViewModel item);
        void RemoveMapItem(IItemViewModel item);
        void ClearMapItems();
        void ResetAllButtons();
        void SetNextEnabled(bool enable);
        void SetPrevEnabled(bool enable);
    }
}
