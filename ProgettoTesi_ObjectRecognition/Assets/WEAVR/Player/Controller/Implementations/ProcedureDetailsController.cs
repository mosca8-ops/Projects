using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TXT.WEAVR.Core;
using TXT.WEAVR.Localization;
using TXT.WEAVR.Player.DataSources;
using TXT.WEAVR.Player.Model;
using TXT.WEAVR.Communication.Entities;
using TXT.WEAVR.Player.Views;

namespace TXT.WEAVR.Player.Controller
{

    public class ProcedureDetailsController : BaseController, IProcedureController
    {
        private IProcedureViewModel m_viewModel;

        public IProcedureView View { get; private set; }
        public IProceduresModel Model { get; private set; }

        public Guid CurrentProcedureId { get; private set; }

        public IProcedureViewModel ViewModel
        {
            get => m_viewModel;
            set
            {
                if(m_viewModel != value)
                {
                    if(m_viewModel != null)
                    {
                        m_viewModel.StatusChanged -= ViewModel_StatusChanged;
                    }
                    m_viewModel = value;
                    if(m_viewModel != null)
                    {
                        m_viewModel.StatusChanged -= ViewModel_StatusChanged;
                        m_viewModel.StatusChanged += ViewModel_StatusChanged;
                    }
                }
            }
        }

        public ProcedureDetailsController(IDataProvider provider) : base(provider)
        {
            View = provider.GetView<IProcedureView>();
            Model = provider.GetModel<IProceduresModel>();
        }

        public async void ShowView(Guid procedureId)
        {
            CurrentProcedureId = procedureId;
            var proxy = Model.GetProxy(procedureId);
            var entity = await proxy.GetEntity();

            SetupStaticInfo(proxy);
            SetupEventHooks();

            View.Show();

        }

        private void SetupStaticInfo(IProcedureProxy proxy)
        {
            var entity = proxy.GetEntity().Result;
            View.ProcedureCompletedTimes = 1;
            View.ProcedureEstTime = "10m";
            View.AssignedGroupName = GetGroupNames(proxy);
            View.ProcedureName = entity.Name;
            View.ProcedureOverview = entity.Description;
            View.ProcedureStepNumber = entity.ProcedureSteps?.Count() ?? 0;
            View.ProcedureLastUpdate = entity.UpdatedAt.ToShortDateString();
        }

        private void SetupEventHooks()
        {
            UnregisterFromEvents();
            RegisterToEvents();
        }

        private void RegisterToEvents()
        {
            View.OnHide += View_OnHide;
            View.OnDownload += View_OnDownload;
            View.OnRemove += View_OnRemove;
            View.OnUpdate += View_OnUpdate;
            View.OnStart += View_OnStart;
            View.OnBack += View_OnBack;
        }
        
        private void UnregisterFromEvents()
        {
            View.OnHide -= View_OnHide;
            View.OnHide -= View_OnHide;
            View.OnDownload -= View_OnDownload;
            View.OnRemove -= View_OnRemove;
            View.OnUpdate -= View_OnUpdate;
            View.OnStart -= View_OnStart;
        }

        private void View_OnBack(IView view)
        {
            UnregisterFromEvents();
            view.Hide();
        }

        private async void View_OnStart()
        {
            //DataProvider.GetController<ICoreController>().SetOnBackCallback(() => ShowView(CurrentProcedureId));
            var proxy = Model.GetProxy(CurrentProcedureId);
            var entity = await proxy.GetEntity();

            var modes = entity.GetLastVersion().ExecutionModes;
            if(modes.Count() <= 1)
            {
                StartCurrentProcedure(modes.FirstOrDefault(), View.Language);
                return;
            }

            var modesView = DataProvider.GetView<IModeView>();
            modesView.SetAvailableLanguages(ViewModel.Languages);
            modesView.Language = View.Language;

            modesView.ProcedureName = entity.Name;
            modesView.SetExecutionModes(modes.Select(e => new ModeViewModel() { Name = e }).ToArray());

            modesView.OnCancel -= ModesView_OnCancel;
            modesView.OnCancel += ModesView_OnCancel;
            modesView.OnStart -= ModesView_OnStart;
            modesView.OnStart += ModesView_OnStart;
            modesView.OnHide -= ModesView_OnHide;
            modesView.OnHide += ModesView_OnHide;

            modesView.Show();
        }

        private async void StartCurrentProcedure(string executionMode, Language language)
        {
            try
            {
                await DataProvider.GetController<IProcedureRunController>().StartProcedure(CurrentProcedureId, executionMode, language.TwoLettersISOName);
                View.Hide();
            }
            catch(Exception ex)
            {
                WeavrDebug.LogException(this, ex);
                PopupManager.ShowError("Error Starting Procedure", "Something went wrong when starting procedure");
                try
                {
                    await DataProvider.GetController<IProcedureRunController>().Close();
                }
                catch(Exception e)
                {
                    WeavrDebug.LogException(this, e);
                }
            }
        }

        private void ModesView_OnHide(IView view)
        {
            if (view is IModeView modesView)
            {
                modesView.OnCancel -= ModesView_OnCancel;
                modesView.OnStart -= ModesView_OnStart;
            }
            view.OnHide -= ModesView_OnHide;
        }

        private void ModesView_OnStart(IModeView view)
        {
            view.Hide();
            StartCurrentProcedure(view.SelectedMode.Name, view.Language);
        }

        private void ModesView_OnCancel(IModeView view)
        {
            view.Hide();
        }

        private ProcedureFlags GetProcedureStatusNew(Guid id)
        {
            return (Model.GetStatistics(id)?.Executions.Count ?? 0) <= 0 ? ProcedureFlags.New : ProcedureFlags.Undefined;
        }

        private async void View_OnUpdate()
        {
            var proxy = Model.GetProxy(CurrentProcedureId);
            try
            {
                if (ViewModel?.Id == proxy.Id)
                {
                    await ViewModel.Sync();
                }
                else
                {
                    var progress = View.StartLoadingWithProgress(Translate("Syncing..."));
                    var entity = await proxy.GetEntity();
                    await proxy.Sync(progress.SetProgress);
                    View.SetStatus(proxy.Status | GetProcedureStatusNew(entity.Id), null);
                }
            }
            catch(Exception ex)
            {
                WeavrDebug.LogException(this, ex);
                await PopupManager.ShowErrorAsync(Translate("Sync Error"), Translate("Error synchronizing the procedure"));
            }
            finally
            {
                View.StopLoading();
            }
        }

        private async void View_OnRemove()
        {
            if (await PopupManager.ShowConfirmAsync(Translate("Delete Procedure"),
                                                    Translate("Are you sure you want to delete this procedure?")))
            {
                try
                {
                    View.StartLoading();
                    if (ViewModel != null && (await ViewModel.Delete()) == true)
                    {
                        // Notify somehow
                    }
                }
                catch (Exception ex)
                {
                    WeavrDebug.LogException(this, ex);
                }
                finally
                {
                    View.StopLoading();
                }
            }
        }


        private void View_OnDownload()
        {
            View_OnUpdate();
        }

        private void View_OnHide(IView view)
        {
            UnregisterFromEvents();
        }

        private string GetGroupNames(IProcedureProxy proxy)
        {
            StringBuilder sb = new StringBuilder();
            var groups = proxy.GetAssignedGroupsIds().Select(g => Model.GetGroup(g));
            if (groups.Any(g => g != null))
            {
                foreach(var group in groups)
                {
                    if (group != null)
                    {
                        sb.Append(group.Name).Append(',').Append(' ');
                    }
                }
                sb.Length -= 2;
            }
            else
            {
                sb.Append("No Group Assigned");
            }
            return sb.ToString();
        }

        public void ShowView(IProcedureViewModel viewModel)
        {
            ViewModel = viewModel;
            CurrentProcedureId = viewModel.Id;

            View.ProcedureCompletedTimes = viewModel.NumberOfCompletions;
            View.ProcedureEstTime = viewModel.AverageTime;
            View.AssignedGroupName = GetGroupNames(viewModel);
            View.ProcedureName = viewModel.Name;
            View.ProcedureOverview = viewModel.Description;
            View.ProcedureStepNumber = viewModel.NumberOfSteps;
            View.ProcedureImage = viewModel.Image;
            View.ProcedureLastUpdate = viewModel.LastUpdate.ToShortDateString();
            View.SetStatus(viewModel.Status, viewModel.GetSyncProgress);

            // TODO: Register languages from procedure instead of globally getting them
            View.SetAvailableLanguages(viewModel.Languages);
            View.Language = viewModel.DefaultLanguage;

            SetupEventHooks();
            View.Show();
        }

        private void ViewModel_StatusChanged(IProcedureViewModel viewModel, ProcedureFlags newStatus)
        {
            View?.SetStatus(newStatus, viewModel.GetSyncProgress);
        }

        private string GetGroupNames(IProcedureViewModel viewModel)
        {
            StringBuilder sb = new StringBuilder();
            var groups = viewModel.AssignedGroups;
            if (groups.Any())
            {
                foreach (var group in groups)
                {
                    sb.Append(group.Name).Append(',').Append(' ');
                }
                sb.Length -= 2;
            }
            else
            {
                sb.Append("No Group Assigned");
            }
            return sb.ToString();
        }

        private class ModeViewModel : IExecutionModeViewModel
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public event OnChangeDelegate Changed;
        }
    }
}
