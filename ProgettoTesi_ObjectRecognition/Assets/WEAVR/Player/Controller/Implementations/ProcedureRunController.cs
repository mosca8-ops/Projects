using System;
using System.Linq;
using System.Threading.Tasks;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using TXT.WEAVR.Player.DataSources;
using TXT.WEAVR.Player.Model;
using TXT.WEAVR.Player.Views;
using TXT.WEAVR.Procedure;
using UnityEngine;
using TXT.WEAVR.Communication.Entities;
using ProcedureAsset = TXT.WEAVR.Procedure.Procedure;
using ProcedureEntity = TXT.WEAVR.Communication.Entities.Procedure;
using TXT.WEAVR.Localization;

namespace TXT.WEAVR.Player.Controller
{

    public class ProcedureRunController : BaseController, IProcedureRunController
    {
        public IProcedureRunView View { get; private set; }
        public IProceduresModel Model { get; private set; }

        public IProcedureProxy CurrentProcedure { get; private set; }
        public ProcedureEntity ProcedureEntity { get; private set; }
        public ProcedureAsset ProcedureAsset { get; private set; }
        public ProcedureRunner ProcedureRunner { get; private set; }

        public ExecutionStatistics CurrentExecutionStatistics { get; private set; }
        public Language CurrentLanguage { get; private set; }

        public ProcedureRunController(IDataProvider provider) : base(provider)
        {
            View = provider.GetView<IProcedureRunView>();
            Model = provider.GetModel<IProceduresModel>();
            Initialize();
        }

        private void Initialize()
        {
            View.OnRestart -= Controls_OnRestart;
            View.OnRestart += Controls_OnRestart;


            View.OnExit -= Controls_OnExit;
            View.OnExit += Controls_OnExit;
        }

        public async Task Close()
        {
            DataProvider.GetView<IFadeView>()?.Show();
            View.Hide();
            // Let it all settle down
            await Task.Delay(500);
            await StopProcedure();
            await WeavrSceneManager.UnloadAllScenesAsync();
            if(CurrentProcedure is IDisposableProxy disposableProcedure)
            {
                try
                {
                    disposableProcedure.Dispose();
                }
                catch(Exception ex)
                {
                    WeavrDebug.LogException(this, ex);
                }
            }
            await DataProvider.GetController<ILibraryController>().ShowLibrary();
        }

        private async void Controls_OnRestart(IProcedureRunView view)
        {
            await RestartProcedure();
        }

        private async void Controls_OnExit(IProcedureRunView view)
        {
            await Close();
        }

        public async Task StartProcedure(Guid procedureId, string executionMode, string language) => await StartProcedure(procedureId, executionMode, language, true);

        public async Task StartProcedure(Guid procedureId, string executionMode, string language, bool restartIfNeeded)
        {
            var view = DataProvider.ViewManager.LastShownView;
            view.StartLoading("Starting Procedure");

            try
            {
                if (!string.IsNullOrEmpty(language))
                {
                    var lang = Language.Get(language);
                    if (lang) { CurrentLanguage = lang; }
                }
                DataProvider.GetView<IFadeView>()?.Show();

                CurrentProcedure = Model.GetProxy(procedureId);

                view.StartLoading("Unpacking Procedure...");
                ProcedureEntity = await CurrentProcedure.GetEntity();
                ProcedureAsset = await CurrentProcedure.GetAsset();
                var sceneProxy = await CurrentProcedure.GetSceneProxy();

                view.StartLoading("Unpacking Scenes...");
                var sceneName = await sceneProxy.GetUnityScene();
                var additiveScenes = await GetAdditiveSceneNames(CurrentProcedure);

                var loadProgress = view.StartLoadingWithProgress("Loading Scenes...");
                var scene = await WeavrSceneManager.LoadNextScene(sceneName, 
                                                    restartIfNeeded, 
                                                    loadProgress.SetProgress, 
                                                    additiveScenes);

                // Loading should be completed by now
                // First disable any test canvases
                var procedureTestPanel = Weavr.TryGetInScene<ProcedureTestPanel>(scene);
                if (procedureTestPanel)
                {
                    procedureTestPanel.gameObject.SetActive(false);
                }

                // Then get the runner
                ProcedureRunner = Weavr.GetInScene<ProcedureRunner>(scene);
                ProcedureRunner.StartWhenReady = false;
                ProcedureRunner.CurrentProcedure = null;

                // Let it all set down then remove the fade
                await Task.Delay(500);
                DataProvider.GetView<IFadeView>()?.Hide();

                // Setup Events
                ProcedureRunner.ProcedureStarted -= ProcedureRunner_ProcedureStarted;
                ProcedureRunner.ProcedureStarted += ProcedureRunner_ProcedureStarted;
                ProcedureRunner.ProcedureFinished -= ProcedureRunner_ProcedureFinished;
                ProcedureRunner.ProcedureFinished += ProcedureRunner_ProcedureFinished;
                ProcedureRunner.ProcedureStopped -= ProcedureRunner_ProcedureStopped;
                ProcedureRunner.ProcedureStopped += ProcedureRunner_ProcedureStopped;
                ProcedureRunner.StepStarted -= ProcedureRunner_StepStarted;
                ProcedureRunner.StepStarted += ProcedureRunner_StepStarted;

                // Start the procedure
                var execModeToStart = ProcedureAsset.ExecutionModes.Find(e => e.ModeName.ToLower() == executionMode.ToLower());
                if (execModeToStart)
                {
                    ProcedureRunner.StartProcedure(ProcedureAsset, execModeToStart);
                }
                else
                {
                    WeavrDebug.LogError(this, $"Procedure {ProcedureAsset.ProcedureName} does not have execution mode {executionMode}. Started with default mode instead");
                    ProcedureRunner.StartProcedure(ProcedureAsset);
                }
            }
            finally
            {
                view.StopLoading();
            }
        }

        private async Task<string[]> GetAdditiveSceneNames(IProcedureProxy procedure)
        {
            var sceneProxies = await procedure.GetAdditiveScenesProxies();
            if(sceneProxies == null) { return new string[0]; }

            var tasks = sceneProxies.Select(s => s.GetUnityScene());
            await Task.WhenAll(tasks);
            return tasks.Select(t => t.Result).ToArray();
        }

        private void ProcedureRunner_StepStarted(IProcedureStep step)
        {
            CurrentExecutionStatistics?.Snapshot();
            if (Model.RunningProcedure.Capabilities.useStepInfo)
            {
                View.StepDescription = step.Description;
                View.StepNumber = step.Number;
                View.StepTitle = step.Title;
            }
            UpdateNavigationButtons(step);
        }

        private void ProcedureRunner_ProcedureStopped(ProcedureRunner runner, ProcedureAsset procedure)
        {
            if (procedure.Capabilities.useAnalytics)
            {
                DataProvider.GetController<IAnalyticsController>()?.End();
            }

            CurrentExecutionStatistics?.Snapshot();
            CurrentExecutionStatistics = null;
            Model.SaveStatistics();
            UnhookFromEvents();
            DataProvider.GetController<IARController>()?.Stop();
            Model.RunningProcedure = null;
        }

        private async void ProcedureRunner_ProcedureFinished(ProcedureRunner runner, ProcedureAsset procedure)
        {
            if (procedure.Capabilities.useAnalytics)
            {
                DataProvider.GetController<IAnalyticsController>()?.End();
            }

            if (CurrentExecutionStatistics != null)
            {
                CurrentExecutionStatistics.Snapshot();
                CurrentExecutionStatistics.Status = ExecutionStatus.Finished;
            }
            CurrentExecutionStatistics = null;
            Model.SaveStatistics();
            UnhookFromEvents();
            DataProvider.GetController<IARController>()?.Stop();
            Model.RunningProcedure = null;
            var response = await PopupManager.ShowAsync(PopupManager.MessageType.Question, 
                        Translate("Completed"), 
                        Translate("Procedure successfully completed!"), 
                        true,
                        Translate("To Library"),
                        Translate("Restart"),
                        Translate("Continue"));

            switch (response)
            {
                case 0:
                    await Close();
                    break;
                case 1:
                    await RestartProcedure();
                    break;
            }
        }

        private async void ProcedureRunner_ProcedureStarted(ProcedureRunner runner, ProcedureAsset procedure, ExecutionMode mode)
        {
            DataProvider.GetController<IAnalyticsController>()?.Begin(CurrentProcedure);

            if (CurrentLanguage)
            {
                LocalizationManager.Current.CurrentLanguage = CurrentLanguage;
            }
            CurrentLanguage = LocalizationManager.Current.CurrentLanguage;

            Model.RunningProcedure = procedure;
            CurrentExecutionStatistics = Model.GetStatistics(ProcedureEntity.Id)?.NewExecution();
            if(CurrentExecutionStatistics != null)
            {
                CurrentExecutionStatistics.StartTime = DateTime.Now;
                CurrentExecutionStatistics.Status = ExecutionStatus.Started;
            }

            View.ResetAllButtons();

            View.EnableNavigationButtons = mode.UsesStepPrevNext;
            View.ProcedureTitle = procedure.ProcedureName;
            View.StepTitle = string.Empty;
            View.StepNumber = string.Empty;
            View.StepDescription = string.Empty;

            View.OnNext -= View_OnNext;
            View.OnPrev -= View_OnPrev;
            runner.CanMoveNextStepChanged -= Runner_CanMoveNextStepChanged;
            runner.RequiresNextToContinue -= Runner_CanMoveNextStepChanged;

            if (mode.UsesStepPrevNext)
            {
                View.OnNext += View_OnNext;
                View.OnPrev += View_OnPrev;
                UpdateNavigationButtons(null);
                runner.CanMoveNextStepChanged += Runner_CanMoveNextStepChanged;
                runner.RequiresNextToContinue += Runner_CanMoveNextStepChanged;
            }

            View.ProcedureProgress = 0;
            View.ShowAllButtons = !procedure.Capabilities.useStepInfo;

            SetupStandardButtons(procedure, mode, View.GetStandardButtons());
            SetupOPSFeatures(procedure, mode);
            SetupVTFeatures(procedure, mode);

            // TODO: Add the buttons here...
            if (procedure.Capabilities.usesAR)
            {
                var arController = DataProvider.GetController<IARController>();
                if (arController != null)
                {
                    await arController.Start();
                    arController.OnAREnabled -= ArController_OnAREnabled;
                    arController.OnAREnabled += ArController_OnAREnabled;
                }
            }
            else
            {
                DataProvider.GetView<IARControlsView>()?.Hide();
            }


            // Show the view
            await Task.Delay(300);
            View.Show();
        }

        private void Runner_CanMoveNextStepChanged(object source, bool newValue)
        {
            View?.SetNextEnabled(newValue || ProcedureRunner.CanMoveNext);
        }

        private void ArController_OnAREnabled(bool value)
        {
            if (!value)
            {
                Clicked_ResetCameraOrbit();
            }
        }

        private void SetupVTFeatures(ProcedureAsset procedure, ExecutionMode mode)
        {
            if (procedure.Configuration.ShortName.ToLower() != "vt") { return; }

        }

        private void CleanupVTFeatures()
        {
            // Nothing for now, RESERVED FOR FUTURE
        }

        private void SetupOPSFeatures(ProcedureAsset procedure, ExecutionMode mode)
        {
            if(procedure.Configuration.ShortName.ToLower() != "ops") { return; }
            // Setup Camera Orbit
            DataProvider.GetView<IGesturesView>()?.Show();
            CameraOrbit.Instance.GesturesPanel = DataProvider.GetView<IGesturesView>()?.GetPanel();
            CameraOrbit.Instance.LockStatusChanged -= CameraOrbit_LockChanged;
            CameraOrbit.Instance.LockStatusChanged += CameraOrbit_LockChanged;
            CameraOrbit.TargetChanged -= CameraOrbit_TargetChanged;
            CameraOrbit.TargetChanged += CameraOrbit_TargetChanged;
        }
        
        private void CleanupOPSFeatures()
        {
            // Cleanup Camera Orbit
            DataProvider.GetView<IGesturesView>()?.Hide();
            if (CameraOrbit.Instance.GesturesPanel == DataProvider.GetView<IGesturesView>()?.GetPanel())
            {
                CameraOrbit.Instance.GesturesPanel = null;
            }
            CameraOrbit.TargetChanged -= CameraOrbit_TargetChanged;
            CameraOrbit.Instance.LockStatusChanged -= CameraOrbit_LockChanged;
        }

        private void CameraOrbit_LockChanged(bool isLockde)
        {
            if (View.GetStandardButtons().LockCameraOrbit != null)
            {
                View.GetStandardButtons().LockCameraOrbit.IsOn = isLockde;
            }
        }

        private void CameraOrbit_TargetChanged(GameObject target)
        {
            if(View?.GetStandardButtons()?.ResetCameraOrbit != null)
            {
                View.GetStandardButtons().ResetCameraOrbit.Enabled = CameraOrbit.DefaultTarget;
            }
        }


        private void SetupStandardButtons(ProcedureAsset procedure, ExecutionMode mode, IStandardButtonsSet standardButtonsSet)
        {
            var resetCameraOrbit = standardButtonsSet.ResetCameraOrbit;
            if (resetCameraOrbit != null)
            {
                resetCameraOrbit.Clear();
                resetCameraOrbit.OnClick -= Clicked_ResetCameraOrbit;
                resetCameraOrbit.OnClick += Clicked_ResetCameraOrbit;
                resetCameraOrbit.IsVisible = !mode.UsesWorldNavigation;
                resetCameraOrbit.Enabled = CameraOrbit.DefaultTarget;
            }

            var lockCameraOrbit = standardButtonsSet.LockCameraOrbit;
            if (lockCameraOrbit != null)
            {
                lockCameraOrbit.Clear();
                lockCameraOrbit.StateChanged -= s => CameraOrbit.Instance.IsLocked = s;
                lockCameraOrbit.StateChanged += s => CameraOrbit.Instance.IsLocked = s;
                lockCameraOrbit.IsVisible = !mode.UsesWorldNavigation;
            }
        }

        private void CleanupStandardButtons(IStandardButtonsSet standardButtonsSet)
        {

        }


        private void Clicked_ResetCameraOrbit()
        {
            if (VirtualCamera.IsDefaultAvailable && CameraOrbit.Instance.SourceCamera)
            {
                var clone = VirtualCamera.Default.Clone();
                clone.UpdateFrom(CameraOrbit.Instance.SourceCamera, true);
                clone.gameObject.hideFlags = HideFlags.HideAndDontSave;
                CameraOrbit.Instance.IsLocked = true;
                clone.StartTransition(CameraOrbit.Instance.SourceCamera, VirtualCamera.Default, 0.5f,
                    () =>
                    {
                        CameraOrbit.Instance.ResetToDefault();
                        CameraOrbit.Instance.IsLocked = false;
                        UnityEngine.Object.Destroy(clone.gameObject, 0.1f);
                    });
            }
            else
            {
                CameraOrbit.Instance.Target = CameraOrbit.DefaultTarget;
            }
        }

        private void View_OnPrev(IProcedureRunView view)
        {
            if (ProcedureRunner)
            {
                ProcedureRunner.MovePreviousStep();
            }
        }

        private void View_OnNext(IProcedureRunView view)
        {
            if (ProcedureRunner)
            {
                ProcedureRunner.MoveNextStep();
            }
        }


        private void UpdateNavigationButtons(IProcedureStep step)
        {
            if (!ProcedureRunner || View == null) { return; }
            View.SetNextEnabled(ProcedureRunner.CanMoveNext || ProcedureRunner.MoveNextOverride != null);
            View.SetPrevEnabled(ProcedureRunner.CanRedo || !ProcedureRunner.IsStartingStep(step));
        }

        private void UnhookFromEvents()
        {
            if (ProcedureRunner)
            {
                // Setup Events
                ProcedureRunner.ProcedureStarted -= ProcedureRunner_ProcedureStarted;
                ProcedureRunner.ProcedureFinished -= ProcedureRunner_ProcedureFinished;
                ProcedureRunner.ProcedureStopped -= ProcedureRunner_ProcedureStopped;
                ProcedureRunner.StepStarted -= ProcedureRunner_StepStarted;
            }
        }


        public async Task StopProcedure()
        {
            if (Model.RunningProcedure)
            {
                if (Model.RunningProcedure.Capabilities.useAnalytics)
                {
                    DataProvider.GetController<IAnalyticsController>()?.End();
                }
                Model.RunningProcedure = null;
            }
            CleanupStandardButtons(View.GetStandardButtons());
            CleanupOPSFeatures();
            CleanupVTFeatures();
            var arController = DataProvider.GetController<IARController>();
            if(arController != null)
            {
                arController.OnAREnabled -= ArController_OnAREnabled;
            }
            if (ProcedureRunner && ProcedureRunner.RunningProcedure)
            {
                ProcedureRunner.StopCurrentProcedure();
                while (Model.RunningProcedure)
                {
                    await Task.Yield();
                }
            }
        }

        public async Task RestartProcedure()
        {
            if (ProcedureRunner)
            {
                var executionMode = ProcedureRunner.ExecutionMode;
                var procedure = ProcedureRunner.CurrentProcedure;

                await StopProcedure();
                await WeavrSceneManager.RestartCurrentScenesAsync();

                await StartProcedure(CurrentProcedure.Id, executionMode.ModeName, CurrentLanguage ? CurrentLanguage.TwoLettersISOName : null, false);
            }
        }
    }
}
