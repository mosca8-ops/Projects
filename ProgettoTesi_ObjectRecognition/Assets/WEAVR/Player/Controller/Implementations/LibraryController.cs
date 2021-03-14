using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using TXT.WEAVR.Player.Communication;
using TXT.WEAVR.Localization;
using TXT.WEAVR.Player.Views;
using TXT.WEAVR.Player.DataSources;
using TXT.WEAVR.Procedure;

using ProcedureEntity = TXT.WEAVR.Communication.Entities.Procedure;
using ProcedureAsset = TXT.WEAVR.Procedure.Procedure;
using System.Collections.Generic;
using System.Diagnostics;
using TXT.WEAVR.Communication.Entities;
using System.Linq;
using TXT.WEAVR.Player.Model;
using UnityEngine;
using TXT.WEAVR.Common;
using System.Collections;
using System.Text;
using TXT.WEAVR.Communication;
using TXT.WEAVR.Player.Communication.Auth;

namespace TXT.WEAVR.Player.Controller
{

    public class LibraryController : BaseController, ILibraryController, IDownloadClient
    {
        private List<IProcedureDataSource> m_sources = new List<IProcedureDataSource>();
        private Dictionary<Guid, IProxy> m_proxies = new Dictionary<Guid, IProxy>();
        private Dictionary<Guid, IGroupViewModel> m_groupsViewModels = new Dictionary<Guid, IGroupViewModel>();
        private Dictionary<Guid, IProcedureViewModel> m_procedureViewModels = new Dictionary<Guid, IProcedureViewModel>();
        private IEnumerable<ProcedureViewModel> m_viewModels;

        public ILibraryView View { get; private set; }
        public IProceduresModel Model { get; private set; }
        public IReadOnlyList<IProcedureDataSource> ProcedureSources => m_sources;
        public IReadOnlyDictionary<Guid, IProxy> Proxies => m_proxies;

        public IDownloadManager DownloadManager { get; set; }

        private float m_nextUpdateTime;
        private float m_updateRate = 120f; // 2 Minutes
        private AuthUser m_lastUser;

        public LibraryController(ILibraryView view, IDataProvider provider) : base(provider)
        {
            View = view ?? provider.GetView<ILibraryView>();
            Model = provider.GetModel<IProceduresModel>();

            View.OnRefresh -= View_OnRefresh;
            View.OnRefresh += View_OnRefresh;
        }

        private async void View_OnRefresh()
        {
            m_nextUpdateTime = 0;
            await RefreshLibrary();
        }

        public T GetProxy<T>(Guid id) where T : IProxy => m_proxies.TryGetValue(id, out IProxy proxy) && proxy is T tProxy ? tProxy : default;

        public async Task ShowLibrary()
        {
            View.Show();
            await RefreshLibrary();
        }

        public async Task RefreshLibrary()
        {
            if(Time.time < m_nextUpdateTime && m_lastUser == WeavrPlayer.Authentication.AuthUser)
            {
                RefreshViewModels();
                View.RefreshViews();
                return;
            }
            m_viewModels = null;
            View.StartLoading(Translate("Loading..."));
            try
            {
                m_lastUser = WeavrPlayer.Authentication.AuthUser;
                WeavrDebug.BeginSample(nameof(LibraryController) + ":" + nameof(RefreshLibrary));
                var (groups, proxyList) = await GetHierarchy();

                m_groupsViewModels.Clear();

                // Add default group
                m_groupsViewModels[Guid.Empty] = new GroupViewModel()
                {
                    Id = Guid.Empty,
                    Name = Translate("My Procedures"),
                    Description = Translate("All procedures assigned exclussively to me"),
                };

                foreach (var group in groups)
                {
                    m_groupsViewModels[group.Id] = CreateGroupViewModel(group);
                }

                WeavrDebug.BeginSample("RegisterEntities");
                m_proxies.Clear();
                foreach (var proxy in proxyList)
                {
                    m_proxies[proxy.Id] = proxy;
                    //var sceneProxy = await proxy.GetSceneProxy();
                    //m_proxies[sceneProxy.Id] = sceneProxy;
                }
                WeavrDebug.EndSample();

                if (Model != null)
                {
                    WeavrDebug.BeginSample("AssignEntitiesToModel");
                    Model.Clear();
                    Model.AddProcedures(proxyList);
                    Model.AddGroups(groups.Concat(new Group[] { new Group() { Id = Guid.Empty, Name = Translate("My Procedures") } }));
                    WeavrDebug.EndSample();
                }

                WeavrDebug.BeginSample("UpdateView");
                // Load up the view
                View.Groups = m_groupsViewModels.Values;
                m_viewModels = await ConvertToViewModels(proxyList);
                View.ClearProcedures();
                View.AddProcedures(m_viewModels);
                View.ProcedureSelected -= View_ProcedureSelected;
                View.ProcedureSelected += View_ProcedureSelected;
                WeavrDebug.EndSample();

                RefreshViewModels();
                m_nextUpdateTime = Time.time + m_updateRate;
            }
            catch (Exception ex)
            {
                WeavrDebug.LogException(this, ex);
                await PopupManager.ShowErrorAsync("Error", Translate("A generic error occured"));
            }
            finally
            {
                WeavrDebug.EndSample();
                View.StopLoading();
            }
        }

        private void RefreshViewModels()
        {
            if(m_viewModels == null) { return; }

            foreach(var viewModel in m_viewModels)
            {
                if(m_proxies.TryGetValue(viewModel.Id, out IProxy proxy) && proxy is IProcedureProxy procedureProxy)
                {
                    procedureProxy.Refresh();
                }
            }
        }

        private async Task<IEnumerable<ProcedureViewModel>> ConvertToViewModels(IEnumerable<IProcedureProxy> proxyList)
        {
            List<ProcedureViewModel> viewModels = new List<ProcedureViewModel>();
            foreach(var proxy in proxyList)
            {
                try
                {
                    viewModels.Add(await CreateProcedureViewModel(proxy));
                }
                catch (Exception ex)
                {
                    WeavrDebug.LogException(this, ex);
                }
            }
            return viewModels;
        }

        private void View_ProcedureSelected(IProcedureViewModel viewModel)
        {
            View.Hide();
            DataProvider.GetController<IProcedureController>().ShowView(viewModel);
            DataProvider.GetView<IProcedureView>().OnBack -= ProcedureView_OnBack;
            DataProvider.GetView<IProcedureView>().OnBack += ProcedureView_OnBack;
        }

        private async void ProcedureView_OnBack(IView view)
        {
            DataProvider.GetView<IProcedureView>().OnBack -= ProcedureView_OnBack;
            await ShowLibrary();
        }

        private IGroupViewModel CreateGroupViewModel(Group group)
        {
            return new GroupViewModel()
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
            };
        }

        private ProcedureFlags GetProcedureStatusNew(Guid id)
        {
            return (Model.GetStatistics(id)?.Executions.Count ?? 0) <= 0 ? ProcedureFlags.New : ProcedureFlags.Undefined;
        }

        private ProcedureStatistics GetStatistics(Guid id)
        {
            return Model.GetStatistics(id);
        }

        private async Task<ProcedureViewModel> CreateProcedureViewModel(IProcedureProxy proxy)
        {
            var entity = await proxy.GetEntity();
            var lastVersion = entity.GetLastVersionForCurrentPlatform();
            var numberOfSteps = lastVersion.ProcedureVersionSteps?.Count() ?? 0;
            return new ProcedureViewModel(this, proxy)
            {
                Entity = entity,
                VersionId = lastVersion.Id,
                NumberOfSteps = numberOfSteps,
                Image = await proxy.GetPreviewImage(),
                AssignedGroups = proxy.GetAssignedGroupsIds().Where(g => m_groupsViewModels.ContainsKey(g)).Select(g => m_groupsViewModels[g]),
                DownloadManager = DownloadManager,
                Languages = lastVersion.AvailableLanguages.Select(l => Language.Get(l)),
                DefaultLanguage = Language.Get(lastVersion.DefaultLanguage),
                LastUpdate = lastVersion.UpdatedAt,
                Status = proxy.Status,
            };
        }

        public async Task<(IEnumerable<Group> groups, IEnumerable<IProcedureProxy> proxies)> GetHierarchy()
        {
            WeavrDebug.BeginSample(nameof(LibraryController) + ":" + nameof(GetHierarchy));
            Group[] groups = new Group[0];
            if (!WeavrPlayer.Options.Offline)
            {
                try
                {
                    WeavrDebug.BeginSample("GetUserGroups");
                    // Get User groups
                    var response = await new WeavrWebRequest().GET(new Request()
                    {
                        Url = WeavrPlayer.API.IdentityApp.USERS_GROUPS(WeavrPlayer.Authentication.AuthUser.Id),
                    });
                    WeavrDebug.EndSample();

                    if (response.HasError)
                    {
                        WeavrDebug.LogError(WeavrPlayer.API.IdentityApp.DEBUG_NAME + ":USERS_GROUPS", response.FullError);
                        throw new Exception(response.FullError);
                    }
                    else if (!response.WasCancelled)
                    {
                        WeavrDebug.BeginSample("DeserializeUserGroups");
                        groups = JsonConvert.DeserializeObject<Group[]>(response.Text);
                        WeavrDebug.EndSample();
                    }
                }
                catch(Exception ex)
                {
                    WeavrDebug.LogException(this, ex);
                }
            }

            WeavrDebug.BeginSample("GetHierarchiesFromSources");
            var proceduresTasks = new List<Task<IHierarchyProxy>>();
            foreach (var source in ProcedureSources)
            {
                if (source.IsAvailable)
                {
                    try
                    {
                        proceduresTasks.Add(source.GetProceduresHierarchy(WeavrPlayer.Authentication.AuthUser.Id, groups.Select(g => g.Id).ToArray()));
                    }
                    catch(Exception ex)
                    {
                        WeavrDebug.LogException(this, ex);
                    }
                }
            }

            await Task.WhenAll(proceduresTasks);
            WeavrDebug.EndSample();
            foreach(var task in proceduresTasks)
            {
                if (task.IsFaulted)
                {
                    WeavrDebug.LogException(this, task.Exception);
                }
            }

            WeavrDebug.BeginSample("MergeHierarchies");
            var hierarchy = await proceduresTasks[0];
            for (int i = 1; i < proceduresTasks.Count; i++)
            {
                var taskHierarchy = await proceduresTasks[i];
                if (taskHierarchy != null)
                {
                    hierarchy.Merge(taskHierarchy);
                }
            }
            WeavrDebug.EndSample();

            WeavrDebug.EndSample();
            return (groups, hierarchy.GetAllProcedureProxies());
        }

        public void AddSource(IProcedureDataSource source)
        {
            if (source is IProcedureProvider provider)
            {
                ProcedureAsset.RegisterProvider(provider);
            }
            if (!m_sources.Contains(source))
            {
                m_sources.Add(source);
            }
        }

        public void RemoveSource(IProcedureDataSource source)
        {
            if (source is IProcedureProvider provider)
            {
                ProcedureAsset.UnregisterProvider(provider);
            }
            m_sources.Remove(source);
        }

        private async Task SyncProcedure(ProcedureViewModel procedureViewModel, Action<float> setProgress)
        {
            if(m_proxies.TryGetValue(procedureViewModel.Id, out IProxy proxy) && proxy is IProcedureProxy procedureProxy)
            {
                await procedureProxy.Sync(setProgress);
                procedureViewModel.Status = procedureProxy.Status;
            }
        }

        public void CleanUp()
        {
            foreach (var dataSource in m_sources)
            {
                dataSource?.CleanUp();
            }
        }

        private class ProcedureViewModel : IProcedureViewModel, IDownloadClient, IDisposable
        {
            private ProcedureFlags m_status;

            public ProcedureEntity Entity { get; set; }

            public Guid Id => Entity.Id;

            public Guid VersionId { get; set; }

            public string Name => Entity.Name;

            public string Description => Entity.Description;

            public DateTime AssignedDate => Entity.UpdatedAt;

            public Texture2D Image { get; set; }

            public ProcedureFlags Status
            {
                get => m_status | Controller.GetProcedureStatusNew(Id);
                set
                {
                    if(m_status != value)
                    {
                        m_status = value;
                        ValidateStatusChange();
                    }
                }
            }

            private void ValidateStatusChange()
            {
                StatusChanged?.Invoke(this, Status);
                if(m_status.HasFlag(ProcedureFlags.Syncing))
                {
                    Progress = 0;
                    DownloadManager?.RegisterForProgress(Id.ToString(), SetProgress);
                }
                else
                {
                    DownloadManager?.UnregisterFromProgress(Id.ToString(), SetProgress);
                }
            }

            public string CollaborationName { get; set; }

            public IEnumerable<IGroupViewModel> AssignedGroups { get; set; }

            public IEnumerable<IUserViewModel> Instructors { get; set; }

            public IEnumerable<IUserViewModel> AssignedStudents { get; set; }

            public IEnumerable<IUserViewModel> LiveStudents { get; set; }

            public float Progress { get; private set; }

            public event StatusChangedDelegate StatusChanged;
            public event OnChangeDelegate Changed;

            public float GetSyncProgress() => Progress;

            public Action<float> CurrentProgressCallback { get; private set; }

            internal LibraryController Controller { get; private set; }

            public int NumberOfSteps { get; internal set; }

            // TODO: Get this data from server
            public string AverageTime
            {
                get
                {
                    var statistics = Controller.GetStatistics(Id);
                    var completions = statistics.Executions.Count(e => e.Status == ExecutionStatus.Finished);
                    return completions > 0 ? $"{Mathf.CeilToInt(statistics.GetAverageExecutionTime() / 60)}m" : $"0m";
                }
            }

            public int NumberOfCompletions => Controller.GetStatistics(Id).Executions.Count(e => e.Status == ExecutionStatus.Finished);

            public IDownloadManager DownloadManager { get; set; }
            public IEnumerable<Language> Languages { get; set; }
            public Language DefaultLanguage { get; set; }
            public DateTime LastUpdate { get; set; }
            public IProcedureProxy Proxy { get; set; }

            public ProcedureViewModel(LibraryController controller, IProcedureProxy proxy)
            {
                Controller = controller;
                proxy.StatusChanged += Proxy_StatusChanged;
                Proxy = proxy;
            }

            private void Proxy_StatusChanged(ProcedureFlags value)
            {
                Status = value;
            }

            public void SetProgress(float progress)
            {
                CurrentProgressCallback?.Invoke(progress);
                Progress = progress;
            }

            public async Task Sync(Action<float> progressCallback = null)
            {
                if(!Status.HasFlag(ProcedureFlags.Sync) && !Status.HasFlag(ProcedureFlags.Syncing))
                {
                    CurrentProgressCallback = progressCallback;
                    await Controller.SyncProcedure(this, SetProgress);
                }
            }

            public void Dispose()
            {
                DownloadManager?.UnregisterFromProgress(Id.ToString(), SetProgress);
            }

            public async Task<bool> Delete()
            {
                if(await Proxy.Delete())
                {
                    Proxy.Refresh();
                    return true;
                }
                return false;
            }
        }

        private class GroupViewModel : IGroupViewModel
        {
            public Guid Id { get; set; }

            public string Name { get; set; }

            public string Description { get; set; }

            public event OnChangeDelegate Changed;
        }
    }
}
