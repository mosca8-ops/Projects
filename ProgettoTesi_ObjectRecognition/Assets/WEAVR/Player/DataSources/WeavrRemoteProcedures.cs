using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TXT.WEAVR.Procedure;

using ProcedureEntity = TXT.WEAVR.Communication.Entities.Procedure;
using ProcedureAsset = TXT.WEAVR.Procedure.Procedure;
using TXT.WEAVR.Communication.Entities;
using UnityEngine;
using System.IO;
using TXT.WEAVR.Player.Communication;
using Newtonsoft.Json;
using TXT.WEAVR.Player.Communication.DTO;
using TXT.WEAVR.Communication;
using TXT.WEAVR.Core;

namespace TXT.WEAVR.Player.DataSources
{
    public class WeavrRemoteProcedures : MonoBehaviour, IProcedureDataSource, IProcedureProvider, IDownloadClient, ICacheUser
    {
        #region [  CONSTANTS  ]

        public const string k_ProceduresFolderName = "Procedures";
        public const string k_ScenesFolderName = "Scenes";
        public const string k_HierarchyFilename = "Hierarchy.json";
        public const string k_PreviewFilename = "Preview.json";

        public static string BaseFoldersPath { get; private set; }
        public static string ProceduresFolderPath => Path.Combine(BaseFoldersPath, k_ProceduresFolderName);
        public static string ScenesFolderPath => Path.Combine(BaseFoldersPath, k_ScenesFolderName);
        public static string HierarchyFilePath => Path.Combine(BaseFoldersPath, k_HierarchyFilename);

        #endregion

        #region [  STATIC PART  ]

        private static Dictionary<Guid, ProcedureEntity> s_procedurePreviews = new Dictionary<Guid, ProcedureEntity>();
        private static Dictionary<Guid, Scene> s_scenePreviews = new Dictionary<Guid, Scene>();
        private static HashSet<Guid> s_currentlyLoadingProcedures = new HashSet<Guid>();

        public static string GetProcedurePreviewJson(Guid procedureId)
        {
            var filepath = Path.Combine(ProceduresFolderPath, procedureId.ToString(), k_PreviewFilename);
            return File.Exists(filepath) ? File.ReadAllText(filepath) : null;
        }

        public static string GetProcedureFolderPath(Guid procedureId) => Path.Combine(ProceduresFolderPath, procedureId.ToString());
        public static string GetSceneFolderPath(Guid sceneId) => Path.Combine(ScenesFolderPath, sceneId.ToString());

        public static ProcedureEntity GetProcedurePreview(Guid procedureId)
        {
            if(!s_procedurePreviews.TryGetValue(procedureId, out ProcedureEntity preview))
            {
                var path = Path.Combine(GetProcedureFolderPath(procedureId), k_PreviewFilename);
                preview = File.Exists(path) ? JsonConvert.DeserializeObject<ProcedureEntity>(File.ReadAllText(path)) : null;
                s_procedurePreviews[procedureId] = preview;
            }
            return preview;
        }

        public static Scene GetScenePreview(Guid sceneId)
        {
            if (!s_scenePreviews.TryGetValue(sceneId, out Scene preview))
            {
                var path = Path.Combine(GetSceneFolderPath(sceneId), k_PreviewFilename);
                preview = File.Exists(path) ? JsonConvert.DeserializeObject<Scene>(File.ReadAllText(path)) : null;
                s_scenePreviews[sceneId] = preview;
            }
            return preview;
        }

        public static string GetScenePreviewFilePath(Guid sceneId)
        {
            return Path.Combine(GetSceneFolderPath(sceneId), k_PreviewFilename);
        }

        public static bool IsProcedureVersionPersistent(Guid procedureId, Guid versionId)
        {
            return File.Exists(Path.Combine(GetProcedureFolderPath(procedureId), versionId.ToString()));
        }

        public static bool IsSceneVersionPersistent(Guid sceneId, Guid versionId)
        {
            return File.Exists(Path.Combine(GetSceneFolderPath(sceneId), versionId.ToString()));
        }

        public static string GetSceneVersionFilePath(Guid sceneId, Guid versionId)
        {
            return Path.Combine(GetSceneFolderPath(sceneId), versionId.ToString());
        }

        #endregion

        private Dictionary<Guid, ProcedureProxy> m_procedureProxies = new Dictionary<Guid, ProcedureProxy>();
        private Dictionary<Guid, SceneProxy> m_sceneProxies = new Dictionary<Guid, SceneProxy>();
        private Dictionary<string, ProcedureAsset> m_loadedProcedureAssets = new Dictionary<string, ProcedureAsset>();
        private Dictionary<string, WeavrAssetBundle> m_assetBundles = new Dictionary<string, WeavrAssetBundle>();
        private Dictionary<Guid, Group> m_cachedGroups = new Dictionary<Guid, Group>();
        private Dictionary<Guid, SceneProxy> m_sceneVersionsProxies = new Dictionary<Guid, SceneProxy>();

        public IHierarchyProxy HierarchyProxy { get; private set; }
        public ProcedureHierarchy RemoteHierarchy { get; private set; }
        public ProcedureHierarchy LocalHierarchy { get; private set; }

        public bool IsAvailable => isActiveAndEnabled;// && Application.internetReachability != NetworkReachability.NotReachable;

        public IDownloadManager DownloadManager { get; set; }
        public ICacheManager CacheManager { get; set; }

        private void Awake()
        {
            if (string.IsNullOrEmpty(BaseFoldersPath))
            {
                BaseFoldersPath = Path.Combine(Application.persistentDataPath, "RemoteProcedures");
                if (!Directory.Exists(BaseFoldersPath))
                {
                    Directory.CreateDirectory(BaseFoldersPath);
                }
            }
        }

        private async Task SyncProcedureProxy(ProcedureProxy proxy, Action<float> progressUpdate)
        {
            proxy.Status |= ProcedureFlags.Syncing;
            s_currentlyLoadingProcedures.Add(proxy.Id);

            try
            {
                // Unity handles the Redirect automatically by reusing the headers
                // The problem is that the redirect doesn't need the Auth Token 'Bearer' which is used from first request
                // thus the allowRedirect = false
                var latestVersion = proxy.Entity.GetLastVersionForCurrentPlatform();
                string filepath = null;

                List<Task> downloadTasks = new List<Task>();
                if (!IsProcedureVersionPersistent(proxy.Id, latestVersion.Id))
                {
                    var request = new Request(WeavrPlayer.API.ContentApp.GET_PROCEDURE_FILE);
                    request.AddQueryValue("id", latestVersion.GetForCurrentPlatform().Id.ToString());

                    var response = await new WeavrWebRequest().GET(request, progressUpdate);

                    if (!response.Validate(WeavrPlayer.API.ContentApp.DEBUG_NAME + ":GetProcedureFileURL"))
                    {
                        return;
                    }

                    var files = JsonConvert.DeserializeObject<ProcedureVersionPlatformFile[]>(response.Text);

                    var procedureFile = files[0];

                    filepath = Path.Combine(GetProcedureFolderPath(proxy.Entity.Id), latestVersion.Id.ToString());
                    downloadTasks.Add(DownloadManager.DownloadFileAsync(proxy.Id.ToString(),
                                                            new Request(procedureFile.Src),
                                                            filepath,
                                                            progressUpdate,
                                                            procedureFile.Size));
                }

                // The procedure file is ready, need to load the scene file
                if (!IsSceneVersionPersistent(proxy.Entity.SceneId, latestVersion.SceneVersionId))
                {
                    downloadTasks.Add(SyncSceneVersion(proxy.Id.ToString(),
                                           proxy.Entity.SceneId,
                                           latestVersion.SceneVersionId,
                                           latestVersion.GetForCurrentPlatform().SceneVersionPlatformId,
                                           progressUpdate));
                }

                // And the additive scenes
                var additiveSceneVersionPlatforms = latestVersion.GetForCurrentPlatform().AdditiveSceneVersionPlatforms;
                if (additiveSceneVersionPlatforms?.Any() == true)
                {
                    foreach (var sceneVersionPlatform in additiveSceneVersionPlatforms)
                    {
                        try
                        {
                            if (!IsSceneVersionPersistent(sceneVersionPlatform.SceneId, sceneVersionPlatform.SceneVersionId))
                            {
                                downloadTasks.Add(SyncSceneVersion(proxy.Id.ToString(), sceneVersionPlatform.SceneId,
                                                       sceneVersionPlatform.SceneVersionId,
                                                       sceneVersionPlatform.Id,
                                                       progressUpdate));
                            }
                        }
                        catch (Exception ex)
                        {
                            WeavrDebug.LogException(this, ex);
                        }
                    }
                }

                await Task.WhenAll(downloadTasks);

                if (additiveSceneVersionPlatforms?.Any() == true)
                {
                    List<ISceneProxy> additiveProxies = new List<ISceneProxy>();
                    foreach (var sceneVersionPlatform in additiveSceneVersionPlatforms)
                    {
                        try
                        {
                            var sceneProxy = await GetSceneVersion(sceneVersionPlatform.SceneId, sceneVersionPlatform.SceneVersionId, true);
                            additiveProxies.Add(sceneProxy);
                        }
                        catch(Exception ex)
                        {
                            WeavrDebug.LogException(this, ex);
                        }
                    }
                    proxy.AdditionalScenes = additiveProxies;
                }

                proxy.SceneProxy = await GetSceneVersion(proxy.Entity.SceneId, latestVersion.SceneVersionId, true);
                proxy.Status |= ProcedureFlags.Sync | ProcedureFlags.Ready | ProcedureFlags.CanBeRemoved;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                proxy.Status &= ~ProcedureFlags.Syncing;
                s_currentlyLoadingProcedures.Remove(proxy.Id);
            }
        }

        private async Task ReassignSceneProxies(ProcedureProxy proxy)
        {
            proxy.SceneProxy = await GetSceneVersion(proxy.Entity.SceneId, proxy.Entity.GetLastVersionForCurrentPlatform().SceneVersionId, true);
            var additiveSceneVersionPlatforms = proxy.Entity.GetLastVersionForCurrentPlatform().GetForCurrentPlatform().AdditiveSceneVersionPlatforms;
            if (additiveSceneVersionPlatforms?.Any() == true)
            {
                var additionalScenes = new List<ISceneProxy>();
                foreach (var sceneVersionPlatform in additiveSceneVersionPlatforms)
                {
                    additionalScenes.Add(await GetSceneVersion(sceneVersionPlatform.SceneId, sceneVersionPlatform.SceneVersionId, false));
                }
                proxy.AdditionalScenes = additionalScenes;
            }
        }

        private async Task RemoveProcedure(ProcedureProxy proxy)
        {
            var folder = GetProcedureFolderPath(proxy.Id);
            // Delete the procedure folder first
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
            var entity = await proxy.GetEntity();
            var lastVersion = entity.GetLastVersionForCurrentPlatform();
            var lastPlatformVersion = lastVersion.GetForCurrentPlatform();
            var sceneVersionPlatforms = new SceneVersionPlatform[] { lastPlatformVersion.SceneVersionPlatform }
                                                        .Concat(lastPlatformVersion.AdditiveSceneVersionPlatforms);
            m_procedureProxies.Remove(proxy.Id);
            // Then check if there are any other procedures which require the same scene
            // and if not delete the scene as well
            IEnumerable<ISceneProxy> allSceneProxies = new ISceneProxy[] { proxy.SceneProxy };
            if (proxy.AdditionalScenes != null)
            {
                allSceneProxies = allSceneProxies.Concat(proxy.AdditionalScenes);
            }
            foreach(var sceneProxy in allSceneProxies)
            {
                var id = sceneProxy.Id;
                if(m_procedureProxies.Values.Any(p => p.SceneProxy?.Id == id 
                                                   || p.AdditionalScenes?.Any(s => s.Id == id) == true))
                {
                    continue;
                }
                // here we have a scene which is not used by any other procedure
                var sceneVersionPlatform = sceneVersionPlatforms.FirstOrDefault(s => s.SceneId == id || s.SceneVersionId == id);
                if(sceneVersionPlatform != null)
                {
                    var scenePath = GetSceneVersionFilePath(sceneVersionPlatform.SceneId, sceneVersionPlatform.SceneVersionId);
                    if (File.Exists(scenePath))
                    {
                        File.Delete(scenePath);
                    }
                    // Delete the folder if it is empty (the preview is always there)
                    if(Directory.GetFiles(GetSceneFolderPath(sceneVersionPlatform.SceneId)).Length <= 1)
                    {
                        Directory.Delete(GetSceneFolderPath(sceneVersionPlatform.SceneId), true);
                    }
                }
            }
        }

        private async Task SyncSceneProxy(RemoteSceneProxy proxy, Action<float> progressUpdate)
        {
            proxy.Status |= ProcedureFlags.Syncing;
            try
            {
                var filepath = await SyncSceneVersion(null, proxy.Scene.Id, proxy.Scene.GetLastVersion().Id, proxy.Scene.GetLastVersionPlatform().Id, progressUpdate);
                if (!string.IsNullOrEmpty(filepath))
                {
                    proxy.FilePath = Path.Combine(GetSceneFolderPath(proxy.Scene.Id), proxy.Scene.GetLastVersion().Id.ToString());
                    proxy.Status |= ProcedureFlags.Sync | ProcedureFlags.Ready;
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                proxy.Status &= ~ProcedureFlags.Syncing;
            }
        }

        private async Task<string> SyncSceneVersion(string downloadId, Guid sceneId, Guid sceneVersionId, Guid sceneVersionPlatformId, Action<float> progressUpdate)
        {
            var request = new Request(WeavrPlayer.API.ContentApp.GET_SCENE_FILE);

            // Unity handles the Redirect automatically by reusing the headers
            // The problem is that the redirect doesn't need the Auth Token 'Bearer' which is used from first request
            // thus the allowRedirect = false
            request.AddQueryValue("id", sceneVersionPlatformId.ToString());
            request.AddQueryValue("allowRedirect", false);

            var response = await new WeavrWebRequest().GET(request, progressUpdate);

            if (response.Validate(WeavrPlayer.API.ContentApp.DEBUG_NAME + ":GetSceneFileURL"))
            {
                var filepath = Path.Combine(GetSceneFolderPath(sceneId), sceneVersionId.ToString());
                var remoteFiles = JsonConvert.DeserializeObject<SceneVersionPlatformFile[]>(response.Text);
                var sceneFileInfo = remoteFiles[0];
                await DownloadManager.DownloadFileAsync(downloadId ?? sceneId.ToString(), 
                                                        new Request(sceneFileInfo.Src), 
                                                        filepath, 
                                                        progressUpdate,
                                                        sceneFileInfo.Size);
                return filepath;
            }
            return null;
        }

        private async Task<ProcedureHierarchy> GetHierarchy(Guid userId, params Guid[] groupsIds)
        {
            var request = new Request()
            {
                Url = WeavrPlayer.API.ContentApp.HIERARCHY(userId),
                Body = new GroupIdsDTO() { groupIds = groupsIds },
                ContentType = MIME.JSON,
            }
            .AddQueryValue("playerVersion", WeavrPlayer.VERSION)
            .AddQueryValue("provider", WeavrPlayer.INPUT_PROVIDER)
            .AddQueryValue("platform", WeavrPlayer.PLATFORM);

            var response = await new WeavrWebRequest(timeout: 30).POST(request);

            if (response.HasError)
            {
                WeavrDebug.LogError(WeavrPlayer.API.ContentApp.DEBUG_NAME + ":GetHierarchy", response.FullError);
            }
            else if (!response.WasCancelled)
            {
                WeavrDebug.Log(this, $"Received Hierarchy: [{response.Bytes.Length / 1024f} KB]");
                WeavrDebug.BeginSample("DeserializeHierarchy");
                var hierarchy = await WeavrJson.DeserializeAsync<ProcedureHierarchy>(response.Text);
                WeavrDebug.EndSample();
                return hierarchy;
            }

            return null;
        }

        private async Task<ProcedureEntity> GetRemoteProcedure(Guid procedureId)
        {
            var response = await new WeavrWebRequest().GET(new Request()
            {
                Url = WeavrPlayer.API.ContentApp.PROCEDURE(procedureId),
                ContentType = MIME.JSON,
            });

            if (response.HasError)
            {
                WeavrDebug.LogError(WeavrPlayer.API.ContentApp.DEBUG_NAME + ":GetProcedure", response.FullError);
            }
            else if (!response.WasCancelled)
            {
                return await WeavrJson.DeserializeAsync<ProcedureEntity>(response.Text);
            }
            return null;
        }

        private async Task<Scene> GetRemoteScene(Guid sceneId)
        {
            var response = await new WeavrWebRequest().GET(new Request()
            {
                Url = WeavrPlayer.API.ContentApp.SCENE(sceneId),
                ContentType = MIME.JSON,
            });

            if (response.HasError)
            {
                WeavrDebug.LogError(WeavrPlayer.API.ContentApp.DEBUG_NAME + ":GetScene", response.FullError);
            }
            else if (!response.WasCancelled)
            {
                return await WeavrJson.DeserializeAsync<Scene>(response.Text);
            }
            return null;
        }

        private ProcedureFlags ComputeStatus(ProcedureEntity entity)
        {
            if(entity == null)
            {
                return ProcedureFlags.Undefined;
            }

            var status = ProcedureFlags.Undefined;
            var preview = GetProcedurePreview(entity.Id);
            if (preview != null)
            {
                status |= ProcedureFlags.Preview;
                // Check if there is any download
                if(DownloadManager.IsDownloadInProgress(entity.Id.ToString()))
                {
                    status |= ProcedureFlags.Syncing;
                    s_currentlyLoadingProcedures.Remove(entity.Id);
                }

                // Check if scene is ready
                if(entity.Scene == null)
                {
                    return status;
                }

                var latestVersion = entity.GetLastVersionForCurrentPlatform();
                if (!status.HasFlag(ProcedureFlags.Syncing) && s_currentlyLoadingProcedures.Contains(entity.Id))
                {
                    if(latestVersion != null && !IsProcedureVersionPersistent(entity.Id, latestVersion.Id))
                    {
                        return status | ProcedureFlags.Syncing;
                    }
                    else
                    {
                        s_currentlyLoadingProcedures.Remove(entity.Id);
                    }
                }

                // Check if the latest version is present
                if (latestVersion == null 
                    || !IsSceneVersionPersistent(entity.SceneId, latestVersion.SceneVersionId)
                    || latestVersion.GetForCurrentPlatform().AdditiveSceneVersionPlatforms?
                                    .Any(s => !IsSceneVersionPersistent(s.SceneId, s.SceneVersionId)) == true)
                {
                    return status;
                }
                if (latestVersion.Id == preview.GetLastVersionForCurrentPlatform()?.Id 
                    && IsProcedureVersionPersistent(entity.Id, latestVersion.Id))
                {
                    status |= ProcedureFlags.Sync | ProcedureFlags.Ready | ProcedureFlags.CanBeRemoved;
                }
                // Check if any version is present
                else if(preview.ProcedureVersions.Any(v => IsProcedureVersionPersistent(entity.Id, v.Id)))
                {
                    status |= ProcedureFlags.Ready | ProcedureFlags.CanBeRemoved;
                }
            }

            return status;
        }

        private ProcedureFlags ComputeStatus(Scene entity)
        {
            if (entity == null)
            {
                return ProcedureFlags.Undefined;
            }

            var status = ProcedureFlags.Undefined;
            var preview = GetScenePreview(entity.Id);
            if (preview != null)
            {
                status |= ProcedureFlags.Preview;
                // Check if the latest version is present
                var latestVersion = entity.GetLastVersionForCurrentPlatform();
                if (latestVersion.Id == preview.GetLastVersionForCurrentPlatform().Id
                    && IsSceneVersionPersistent(entity.Id, latestVersion.Id))
                {
                    status |= ProcedureFlags.Sync | ProcedureFlags.Ready;
                }
                // Check if any version is present
                else if (preview.SceneVersions.Any(v => IsSceneVersionPersistent(entity.Id, v.Id)))
                {
                    status |= ProcedureFlags.Ready;
                }
            }

            return status;
        }

        private ProcedureFlags ComputeStatus(SceneVersion entity) 
            => ComputeStatus(entity?.SceneId ?? Guid.Empty, entity?.Id ?? Guid.Empty);

        private ProcedureFlags ComputeStatus(Guid sceneId, Guid sceneVersionId)
        {
            if (sceneId == Guid.Empty || sceneVersionId == Guid.Empty)
            {
                return ProcedureFlags.Undefined;
            }

            var status = ProcedureFlags.Undefined;
            var preview = GetScenePreview(sceneId);
            if (preview != null)
            {
                status |= ProcedureFlags.Preview;

                // Check if the latest version is present
                if (IsSceneVersionPersistent(sceneId, sceneVersionId))
                {
                    status |= ProcedureFlags.Sync | ProcedureFlags.Ready;
                }
            }

            return status;
        }

        private async Task<ProcedureAsset> LoadProcedureAsset(Guid procedureId)
        {
            var preview = GetProcedurePreview(procedureId);
            var assetPath = Path.Combine(GetProcedureFolderPath(preview.Id), preview.GetLastVersion().Id.ToString());
            
            if ((!m_loadedProcedureAssets.TryGetValue(assetPath, out ProcedureAsset asset) || !asset) && File.Exists(assetPath))
            {
                var assetBundle = await WeavrAssetBundle.LoadFromFileAsync(assetPath);
                var assets = await assetBundle.LoadAllAssetsAsync<ProcedureAsset>();
                var assetBundleIsValid = false;
                for (int i = 0; i < assets.Length; i++)
                {
                    if(assets[i].Guid == preview.UnityId.ToString())
                    {
                        asset = assets[i];
                        m_loadedProcedureAssets[assetPath] = asset;
                        m_loadedProcedureAssets[asset.Guid] = asset;
                        m_assetBundles[asset.Guid] = assetBundle;
                        assetBundleIsValid = true;
                        break;
                    }
                }
                if (!assetBundleIsValid)
                {
                    assetBundle.Unload(true);
                }
            }

            return asset;
        }

        private void UnloadProcedureAsset(Guid procedureId)
        {
            var preview = GetProcedurePreview(procedureId);
            var assetPath = Path.Combine(GetProcedureFolderPath(preview.Id), preview.GetLastVersion().Id.ToString());
            if (m_loadedProcedureAssets.TryGetValue(assetPath, out ProcedureAsset asset) && asset)
            {
                m_loadedProcedureAssets.Remove(assetPath);
                m_loadedProcedureAssets.Remove(asset.Guid);
                if(m_assetBundles.TryGetValue(asset.Guid, out WeavrAssetBundle assetBundle))
                {
                    assetBundle?.Unload(true);
                    m_assetBundles.Remove(asset.Guid);
                }
                if(m_procedureProxies.TryGetValue(procedureId, out ProcedureProxy proxy))
                {
                    if(proxy.AdditionalScenes != null)
                    {
                        foreach(var scene in proxy.AdditionalScenes)
                        {
                            if(scene is IDisposableProxy sceneProxy)
                            {
                                sceneProxy.Dispose();
                            }
                        }
                    }
                    if(proxy.SceneProxy is IDisposableProxy disposableProxy)
                    {
                        disposableProxy.Dispose();
                    }
                }
            }
        }

        private async Task<ISceneProxy> GetSceneVersion(Guid sceneId, Guid sceneVersionId, bool update = false)
        {
            Scene scene = null;
            if (!m_sceneVersionsProxies.TryGetValue(sceneVersionId, out SceneProxy proxy))
            {
                if(!m_sceneProxies.TryGetValue(sceneId, out SceneProxy sceneProxy))
                {
                    sceneProxy = await GetScene(sceneId) as SceneProxy;
                }
                scene = await sceneProxy.GetSceneEntity();
                proxy = new RemoteSceneProxy(this, scene, GetSceneVersionFilePath(sceneId, sceneVersionId))
                {
                    Status = ComputeStatus(scene.GetVersion(sceneVersionId)),
                    SceneVersionId = sceneVersionId
                };

                m_sceneVersionsProxies[sceneVersionId] = proxy;
            }
            else if (update)
            {
                if(scene == null && !m_sceneProxies.TryGetValue(sceneId, out SceneProxy sceneProxy))
                {
                    sceneProxy = await GetScene(sceneId) as SceneProxy;
                    scene = await sceneProxy.GetSceneEntity();
                }
                var version = scene?.GetVersion(sceneVersionId);
                proxy.Status = version != null ? ComputeStatus(version) : ComputeStatus(sceneId, sceneVersionId);
            }
            return proxy;
        }

        #region [  IProcedureDataSource Implementation  ]

        public async Task<IProcedureProxy> GetProcedureById(Guid procedureId)
        {
            if (WeavrPlayer.Options.Offline) { return null; }

            var entity = await GetRemoteProcedure(procedureId);

            if(entity != null 
                && m_procedureProxies.TryGetValue(entity.Id, out ProcedureProxy proxy) 
                && proxy.Entity.GetLastVersionForCurrentPlatform().Id == entity.GetLastVersionForCurrentPlatform().Id)
            {
                return proxy;
            }

            proxy = new ProcedureProxy(this)
            {
                Id = entity.Id,
                Entity = entity,
                Status = ComputeStatus(entity)
            };

            m_procedureProxies[procedureId] = proxy;

            return proxy;
        }

        public async Task<IHierarchyProxy> GetProceduresHierarchy(Guid userId, params Guid[] groupsIds)
        {
            if (WeavrPlayer.Options.Offline) { return null; }

            WeavrDebug.BeginSample(nameof(WeavrRemoteProcedures) + ":" + nameof(GetProceduresHierarchy));
            if (LocalHierarchy == null && File.Exists(HierarchyFilePath))
            {
                try
                {
                    LocalHierarchy = await WeavrJson.DeserializeFileAsync<ProcedureHierarchy>(HierarchyFilePath);
                }
                catch(Exception ex)
                {
                    WeavrDebug.LogException(this, ex);
                }
            }

            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                WeavrDebug.BeginSample("GetHierarchy");
                RemoteHierarchy = await GetHierarchy(userId, groupsIds);
                WeavrDebug.EndSample();
                foreach (var procedure in RemoteHierarchy.GetAllProcedures())
                {
                    s_procedurePreviews[procedure.Id] = procedure;
                    s_scenePreviews[procedure.Scene.Id] = procedure.Scene;
                }
            }
            else
            {
                RemoteHierarchy = LocalHierarchy;
            }

            WeavrDebug.BeginSample("CreateHierarchyProxy");
            HierarchyProxy = new HierarchyProxy(this,
                                                    RemoteHierarchy,
                                                    p => new ProcedureProxy(this)
                                                    {
                                                        Id = p.Id,
                                                        Entity = p,
                                                        Status = ComputeStatus(p),
                                                        GroupsIds = new HashSet<Guid>(),
                                                    },
                                                    GetGroupAsync);
            WeavrDebug.EndSample();


            WeavrDebug.BeginSample("SyncHierarchies");
            await Task.Run(SyncHierarchies);
            WeavrDebug.EndSample();

            // Save the hierarchy
            PersistRemoteHierarchy();

            WeavrDebug.EndSample();
            return HierarchyProxy;
        }

        private async void PersistRemoteHierarchy()
        {
            List<Task> persistTasks = new List<Task>
            {
                Task.Run(() => File.WriteAllText(HierarchyFilePath, JsonConvert.SerializeObject(RemoteHierarchy)))
            };
            foreach (var procedure in RemoteHierarchy.GetAllProcedures())
            {
                persistTasks.Add(Task.Run(() => File.WriteAllText(Path.Combine(GetProcedureFolderPath(procedure.Id), k_PreviewFilename), JsonConvert.SerializeObject(procedure))));
            }
            await Task.WhenAll(persistTasks);
        }

        private void SyncHierarchies()
        {
            foreach (var procedure in RemoteHierarchy.GetAllProcedures())
            {
                WeavrDebug.BeginSample("InnerSyncProcedureProxy");
                var folderPath = GetProcedureFolderPath(procedure.Id);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                //File.WriteAllText(Path.Combine(folderPath, k_PreviewFilename), JsonConvert.SerializeObject(procedure));
                //s_procedurePreviews[procedure.Id] = procedure;
                var proxy = HierarchyProxy.GetProxy(procedure.Id);

                WeavrDebug.EndSample();

                if (proxy == null)
                {
                    WeavrDebug.LogError(this, $"Unable to get proxy for procedure {procedure.Name} [{procedure.Id}]");
                    continue;
                }

                var latestProcedureVersion = procedure.GetLastVersionForCurrentPlatform();
                if(latestProcedureVersion == null)
                {
                    WeavrDebug.LogError(this, $"Unable to get a valid version for the procedure {procedure.Name} [{procedure.Id}]");
                    continue;
                }

                m_procedureProxies[proxy.Id] = proxy as ProcedureProxy;

                WeavrDebug.BeginSample("InnerSyncSceneProxy");

                // Update the scene part
                var sceneFolder = GetSceneFolderPath(procedure.SceneId);
                if (!Directory.Exists(sceneFolder))
                {
                    Directory.CreateDirectory(sceneFolder);
                }

                var scene = procedure.Scene;

                File.WriteAllText(Path.Combine(sceneFolder, k_PreviewFilename), JsonConvert.SerializeObject(scene));
                // Need to get the version here
                if (!m_sceneProxies.ContainsKey(scene.Id))
                {
                    m_sceneProxies[scene.Id] = new SceneProxy(this, scene);
                }

                var latestSceneVersion = scene.GetLastVersionForCurrentPlatform();
                var sceneVersionId = latestProcedureVersion.SceneVersionId;
                var procSceneVersion = scene.SceneVersions.FirstOrDefault(v => v.Id == sceneVersionId) ?? latestSceneVersion;
                if (!m_sceneVersionsProxies.TryGetValue(sceneVersionId, out SceneProxy versionProxy))
                {
                    versionProxy = GetSceneVersion(scene.Id, sceneVersionId).Result as SceneProxy;
                }

                versionProxy.Status = ComputeStatus(procSceneVersion);
                if (proxy is ProcedureProxy pproxy)
                {
                    pproxy.SceneProxy = versionProxy;
                }

                if (latestSceneVersion?.Id == sceneVersionId)
                {
                    m_sceneProxies[scene.Id] = versionProxy;
                }
                
                WeavrDebug.EndSample();
            }
        }

        public async Task<IEnumerable<IProcedureProxy>> GetProcedures(Guid userId, params Guid[] groupsIds)
        {
            LocalHierarchy = LocalHierarchy ?? JsonConvert.DeserializeObject<ProcedureHierarchy>(HierarchyFilePath);

            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                RemoteHierarchy = await GetHierarchy(userId, groupsIds);
            }
            else
            {
                RemoteHierarchy = LocalHierarchy;
            }

            List<IProcedureProxy> proxies = new List<IProcedureProxy>();

            foreach(var (procedure, groups) in RemoteHierarchy.GetAllProceduresWithGroups())
            {
                var folderPath = GetProcedureFolderPath(procedure.Id);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                File.WriteAllText(Path.Combine(folderPath, k_PreviewFilename), JsonConvert.SerializeObject(procedure));
                if(m_procedureProxies.TryGetValue(procedure.Id, out ProcedureProxy proxy))
                {
                    proxy.Entity = procedure;
                    proxy.Asset = null;
                    proxy.Status = ComputeStatus(procedure);
                    proxy.GroupsIds = new HashSet<Guid>(groups.Select(g => g.Id).ToList());
                }
                else
                {
                    proxy = new ProcedureProxy(this)
                    {
                        Id = procedure.Id,
                        Entity = procedure,
                        Status = ComputeStatus(procedure),
                        GroupsIds = new HashSet<Guid>(groups.Select(g => g.Id)),
                    };
                    m_procedureProxies[procedure.Id] = proxy;
                }

                proxies.Add(proxy);

                // Update the scene part
                var sceneFolder = GetSceneFolderPath(procedure.SceneId);
                if (!Directory.Exists(sceneFolder))
                {
                    Directory.CreateDirectory(sceneFolder);
                }

                var scene = procedure.Scene;

                File.WriteAllText(Path.Combine(sceneFolder, k_PreviewFilename), JsonConvert.SerializeObject(scene));
                // Need to get the version here
                if (!m_sceneProxies.ContainsKey(scene.Id))
                {
                    m_sceneProxies[scene.Id] = new SceneProxy(this, scene);
                }

                var latestSceneVersion = scene.GetLastVersionForCurrentPlatform();
                var sceneVersionId = procedure.GetLastVersionForCurrentPlatform().SceneVersionId;
                var procSceneVersion = scene.SceneVersions.FirstOrDefault(v => v.Id == sceneVersionId) ?? latestSceneVersion;
                if(!m_sceneVersionsProxies.TryGetValue(sceneVersionId, out SceneProxy versionProxy))
                {
                    versionProxy = await GetSceneVersion(scene.Id, sceneVersionId) as SceneProxy;
                }

                versionProxy.Status = ComputeStatus(procSceneVersion);
                proxy.SceneProxy = versionProxy;

                if(latestSceneVersion.Id == sceneVersionId)
                {
                    m_sceneProxies[scene.Id] = versionProxy;
                }
            }

            // Save the hierarchy
            File.WriteAllText(BaseFoldersPath, JsonConvert.SerializeObject(RemoteHierarchy));

            return proxies;
        }

        public async Task<ISceneProxy> GetScene(Guid sceneId)
        {
            if (WeavrPlayer.Options.Offline) { return null; }

            var entity = GetScenePreview(sceneId);
            if (entity == null || (DateTime.Now - File.GetLastWriteTime(GetScenePreviewFilePath(sceneId))).TotalMinutes > 1)
            {
                entity = await GetRemoteScene(sceneId);
            }

            if (entity != null
                && m_sceneProxies.TryGetValue(entity.Id, out SceneProxy proxy))
            {
                var sceneEntity = await proxy.GetSceneEntity();
                if (sceneEntity.GetLastVersionForCurrentPlatform().Id == entity.GetLastVersionForCurrentPlatform().Id)
                {
                    return proxy;
                }
            }

            var latestVersion = entity.GetLastVersionForCurrentPlatform();

            if(m_sceneProxies.TryGetValue(sceneId, out SceneProxy existingProxy) 
                && (existingProxy as RemoteSceneProxy)?.SceneVersionId == latestVersion.Id)
            {
                existingProxy.Status = ComputeStatus(latestVersion);
                return existingProxy;
            }

            proxy = new RemoteSceneProxy(this, entity, GetSceneVersionFilePath(entity.Id, latestVersion.Id))
            {
                Status = ComputeStatus(entity),
                SceneVersionId = latestVersion.Id,
            };

            m_sceneProxies[sceneId] = proxy;

            return proxy;
        }

        private async Task<Group> GetGroupAsync(Guid id)
        {
            if (!m_cachedGroups.TryGetValue(id, out Group group)) 
            {
                var response = await new WeavrWebRequest().GET(new Request()
                {
                    Url = WeavrPlayer.API.IdentityApp.GROUPS(id),
                });

                if (response.HasError)
                {
                    WeavrDebug.LogError($"{WeavrPlayer.API.IdentityApp.DEBUG_NAME}:GROUPS[{id}]", response.FullError);
                }
                else
                {
                    group = JsonConvert.DeserializeObject<Group>(response.Text);
                    if(group != null)
                    {
                        m_cachedGroups[id] = group;
                    }
                }
            }
            return group;
        }

        public void Clear()
        {
            m_procedureProxies.Clear();
            m_sceneProxies.Clear();
        }

        public void CleanUp()
        {
            LocalHierarchy = LocalHierarchy ?? JsonConvert.DeserializeObject<ProcedureHierarchy>(HierarchyFilePath);
            var procedures = LocalHierarchy.GetAllProcedures();
            foreach (var directory in Directory.GetDirectories(ProceduresFolderPath).ToArray())
            {
                var procedure = procedures.FirstOrDefault(p => directory.EndsWith(p.Id.ToString()));
                if (procedure == null)
                {
                    Directory.Delete(directory, true);
                }
                else
                {
                    foreach (var file in Directory.GetFiles(directory).ToArray())
                    {
                        if (!procedure.ProcedureVersions.Any(v => file.EndsWith(v.Id.ToString())))
                        {
                            File.Delete(file);
                        }
                    }
                }
            }

            foreach (var directory in Directory.GetDirectories(ScenesFolderPath).ToArray())
            {
                var scene = procedures.Select(p => p.Scene).FirstOrDefault(s => directory.EndsWith(s.Id.ToString()));
                if (scene == null)
                {
                    Directory.Delete(directory, true);
                }
                else
                {
                    foreach (var file in Directory.GetFiles(directory).ToArray())
                    {
                        if (!scene.SceneVersions.Any(v => file.EndsWith(v.Id.ToString())))
                        {
                            File.Delete(file);
                        }
                    }
                }
            }
        }

        #endregion

        #region [  IProcedureProvider Implementation  ]

        public bool TryGetProcedure(string procedureGuid, out ProcedureAsset procedure) 
            => m_loadedProcedureAssets.TryGetValue(procedureGuid, out procedure);

        #endregion

        private class RemoteSceneProxy : SceneProxy
        {
            public RemoteSceneProxy(IProcedureDataSource dataSource, Scene scene) : base(dataSource, scene)
            {
            }

            public RemoteSceneProxy(IProcedureDataSource dataSource, Scene scene, string sceneFilePath) : base(dataSource, scene, sceneFilePath)
            {

            }

            public RemoteSceneProxy(IProcedureDataSource dataSource, Scene scene, byte[] sceneBytes) : base(dataSource, scene, sceneBytes)
            {

            }

            public WeavrRemoteProcedures DataSource { get; set; }

            public string FilePath { get => m_sceneFullPath; set => m_sceneFullPath = value; }

            public Guid SceneVersionId { get; set; }

            public override async Task Sync(Action<float> progressUpdate = null)
            {
                if (!Status.HasFlag(ProcedureFlags.Sync) && !Status.HasFlag(ProcedureFlags.Syncing))
                {
                    await DataSource.SyncSceneProxy(this, progressUpdate);
                }
            }
        }
        
        private class ProcedureProxy : IProcedureProxy, IDisposableProxy
        {
            ProcedureFlags m_status = ProcedureFlags.Undefined;
            bool m_alreadyRefreshing = false;

            public event OnValueChanged<ProcedureFlags> StatusChanged;

            public Guid Id { get; set; }

            public IEnumerable<ISceneProxy> AdditionalScenes { get; set; }

            public IProcedureDataSource Source => DataSource;

            public ProcedureFlags Status
            {
                get => m_status;
                set
                {
                    if(m_status != value)
                    {
                        m_status = value;
                        StatusChanged?.Invoke(m_status);
                    }
                }
            }

            public ISceneProxy SceneProxy { get; set; }

            public ProcedureEntity Entity { get; set; }

            public ProcedureAsset Asset { get; set; }

            public HashSet<Guid> GroupsIds { get; set; }

            public WeavrRemoteProcedures DataSource { get; set; }

            public ProcedureProxy(WeavrRemoteProcedures source)
            {
                DataSource = source;
            }

            public async Task<ProcedureAsset> GetAsset()
            {
                if (!Asset)
                {
                    // Load the asset here
                    Asset = await DataSource.LoadProcedureAsset(Id);
                    if (Asset)
                    {
                        Status = DataSource.ComputeStatus(Entity);
                    }
                }
                return Asset;
            }
            
            public Task<ProcedureEntity> GetEntity() => Task.FromResult(Entity);

            public async Task<ISceneProxy> GetSceneProxy()
            {
                if(SceneProxy == null)
                {
                    var entity = await GetEntity();
                    SceneProxy = await DataSource.GetSceneVersion(entity.SceneId, entity.GetLastVersionForCurrentPlatform().SceneVersionId);
                }
                return SceneProxy;
            }

            public async Task Sync(Action<float> progressUpdate = null)
            {
                if (!Status.HasFlag(ProcedureFlags.Sync) && !Status.HasFlag(ProcedureFlags.Syncing))
                {
                    await DataSource.SyncProcedureProxy(this, progressUpdate);
                }
            }

            public IEnumerable<Guid> GetAssignedGroupsIds() => GroupsIds;

            public void AssignGroup(Guid groupId)
            {
                if(GroupsIds == null)
                {
                    GroupsIds = new HashSet<Guid>();
                }
                GroupsIds.Add(groupId);
            }

            public async Task<Texture2D> GetPreviewImage()
            {
                // TODO: Implement Get image from other Medias
                if (Entity.ProcedurePreview == null)
                {
                    return null;
                }
                try
                {
                    return await DataSource.CacheManager.GetTexture(Entity.ProcedurePreview.Src);
                }
                catch(Exception ex)
                {
                    WeavrDebug.LogException(this, ex);
                }
                return null;
            }

            public async void Refresh()
            {
                Status = DataSource.ComputeStatus(Entity);
                if (Status.HasFlag(ProcedureFlags.Syncing) && !m_alreadyRefreshing)
                {
                    try
                    {
                        m_alreadyRefreshing = true;
                        await DataSource.DownloadManager.FinishDownloadAsync(Id.ToString());
                        await DataSource.ReassignSceneProxies(this);
                        Status = DataSource.ComputeStatus(Entity);
                    }
                    finally
                    {
                        m_alreadyRefreshing = false;
                    }
                }
            }
            
            public Task<IEnumerable<ISceneProxy>> GetAdditiveScenesProxies()
            {
                return Task.FromResult(AdditionalScenes);
            }

            public async Task<bool> Delete()
            {
                if (!Status.HasFlag(ProcedureFlags.CanBeRemoved))
                {
                    return false;
                }
                try
                {
                    await DataSource.RemoveProcedure(this);
                    return true;
                }
                catch(Exception ex)
                {
                    WeavrDebug.LogException(this, ex);
                    return false;
                }
            }

            public void Dispose()
            {
                DataSource.UnloadProcedureAsset(Id);
            }
        }
    }
}
