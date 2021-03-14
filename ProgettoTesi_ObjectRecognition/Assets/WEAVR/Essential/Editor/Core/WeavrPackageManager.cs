using Newtonsoft.Json;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace TXT.WEAVR.Core
{
    [InitializeOnEditorStart(nameof(OnStartup))]
    public class WeavrPackageManager : IActiveBuildTargetChanged
    {
        private const string k_PackagesToInstallKey = "WEAVR_PACKAGES_TO_INSTALL";
        private const string k_PackagesToRemoveKey = "WEAVR_PACKAGES_TO_REMOVE";

        private static string s_CurrentlyInstallingPackages = string.Empty;
        private static string s_CurrentlyRemovingPackages = string.Empty;

        private static void MarkAsInstalling(Package package)
        {
            if (!s_CurrentlyInstallingPackages.Contains(package.name) && !s_CurrentlyInstallingPackages.Contains(package.packageId))
            {
                s_CurrentlyInstallingPackages += package.packageId + ";";
            }
        }

        private static void MarkAsInstalled(Package package)
        {
            s_CurrentlyInstallingPackages = s_CurrentlyInstallingPackages.Replace(package.name + ";", string.Empty)
                                                                         .Replace(package.packageId + ";", string.Empty);
        }

        private static void MarkAsRemoving(Package package)
        {
            if (!s_CurrentlyRemovingPackages.Contains(package.name) && !s_CurrentlyRemovingPackages.Contains(package.packageId))
            {
                s_CurrentlyRemovingPackages += package.packageId + ";";
            }
        }

        private static void MarkAsRemoved(Package package)
        {
            s_CurrentlyRemovingPackages = s_CurrentlyRemovingPackages.Replace(package.name + ";", string.Empty)
                                                                     .Replace(package.packageId + ";", string.Empty);
        }

        [Serializable]
        public class Package
        {
            public string name;
            public string packageId;
            public string minVersion;
            public string maxVersion;

            public async Task<string> GetLatestVersionStringAsync() => (await GetLatestVersionAsync())?.version ?? string.Empty;

            [JsonIgnore]
            public bool IsQueuedForInstallation => AreSimilar(PlayerPrefs.GetString(k_PackagesToInstallKey, string.Empty), packageId);

            [JsonIgnore]
            public bool IsQueuedForRemoval => AreSimilar(PlayerPrefs.GetString(k_PackagesToRemoveKey, string.Empty), packageId);

            [JsonIgnore]
            public bool IsCurrentlyInstalling => s_CurrentlyInstallingPackages.Contains(name) || s_CurrentlyInstallingPackages.Contains(packageId);
            [JsonIgnore]
            public bool IsCurretlyRemoving => s_CurrentlyRemovingPackages.Contains(name) || s_CurrentlyRemovingPackages.Contains(packageId);

            [JsonIgnore]
            public string PackageCompleteName => minVersion == null ? packageId : $"{name}@{minVersion}";

            [JsonIgnore]
            public bool IsCurrentOperation => CurrentOperation != null && (CurrentOperation.Contains(packageId) || packageId.Contains(CurrentOperation));

            [JsonIgnore]
            public string Version { get; private set; }

            private bool AreSimilar(string a, string b) => !string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b) && (a.Contains(b) || b.Contains(a));

            public string GetNameFromId() => packageId.Split('@')[0];

            public async Task<(string name, string version)> AsDependency()
            {
                var status = await GetStatusAsync();
                if(status != PackageStatus.Available)
                {
                    var latestVersion = await GetLatestVersionAsync();
                    return latestVersion != null ? (latestVersion.name, latestVersion.version) : (null, null);
                }
                return (GetNameFromId(), Version);
            }

            internal async Task<PackageInfo> GetLatestVersionAsync()
            {
                try
                {
                    var request = Client.Search(packageId, false);

                    while (request?.IsCompleted == false)
                    {
                        await Task.Yield();
                    }

                    if (request.Error != null)
                    {
                        WeavrDebug.LogError(this, $"[{packageId}]: Error {request.Error.errorCode}: {request.Error.message}");
                        return null;
                    }
                    else
                    {
                        return request.Result.OrderByDescending(p => p.version)
                                                          .FirstOrDefault(p => IsValidVersion(p.version));
                    }
                }
                catch (Exception ex)
                {
                    WeavrDebug.LogException(this, ex);
                    return null;
                }
            }

            private void AddToInstallQueue()
            {
                var queue = PlayerPrefs.GetString(k_PackagesToInstallKey, string.Empty);
                if (!queue.Contains(packageId))
                {
                    PlayerPrefs.SetString(k_PackagesToInstallKey, queue + packageId + ';');
                }
            }

            private void RemoveFromInstallQueue()
            {
                var queue = PlayerPrefs.GetString(k_PackagesToInstallKey, string.Empty);
                if (queue.Contains(packageId))
                {
                    PlayerPrefs.SetString(k_PackagesToInstallKey, queue.Replace(packageId + ';', string.Empty));
                }
            }

            private void AddToRemovalQueue()
            {
                var queue = PlayerPrefs.GetString(k_PackagesToRemoveKey, string.Empty);
                if (!queue.Contains(packageId))
                {
                    PlayerPrefs.SetString(k_PackagesToRemoveKey, queue + packageId + ';');
                }
            }

            private void RemoveFromRemovalQueue()
            {
                var queue = PlayerPrefs.GetString(k_PackagesToRemoveKey, string.Empty);
                if (queue.Contains(packageId))
                {
                    PlayerPrefs.SetString(k_PackagesToRemoveKey, queue.Replace(packageId + ';', string.Empty));
                }
            }

            public async Task<PackageStatus> GetStatusAsync()
            {
                try
                {
                    // TODO: Use Client List instead for the installed packages
                    var request = Client.List(true);

                    while (request?.IsCompleted == false)
                    {
                        await Task.Yield();
                    }

                    if (request.Error != null)
                    {
                        WeavrDebug.LogError(this, $"[{packageId}]: Error {request.Error.errorCode}: {request.Error.message}");
                        return PackageStatus.Error;
                    }
                    else
                    {
                        var latestVersion = request.Result.OrderByDescending(p => p.version)
                                                          .FirstOrDefault(p => p.packageId.StartsWith(packageId) && IsValidVersion(p.version));
                        // It seems there is a problem with how Unity sets status for local packages
                        //return latestVersion?.status ?? PackageStatus.Unavailable;

                        // For now it is enough to know that the version exists
                        name = latestVersion?.name ?? name;
                        packageId = latestVersion?.packageId ?? packageId;
                        Version = latestVersion?.version ?? string.Empty;
                        return latestVersion != null ? PackageStatus.Available : PackageStatus.Unavailable;
                    }
                }
                catch (Exception ex)
                {
                    WeavrDebug.LogException(this, ex);
                    return PackageStatus.Unknown;
                }
            }

            public async Task WaitInProgressOperationAsync()
            {
                var status = await GetStatusAsync();
                if (status == PackageStatus.InProgress || IsQueuedForInstallation)
                {
                    while (status != PackageStatus.InProgress || IsQueuedForInstallation)
                    {
                        await Task.Delay(1000);
                        status = await GetStatusAsync();
                    }
                }
            }

            public async Task<bool> Install()
            {
                try
                {
                    var status = await GetStatusAsync();
                    if (status == PackageStatus.InProgress)
                    {
                        while (status != PackageStatus.InProgress)
                        {
                            await Task.Delay(1000);
                            status = await GetStatusAsync();
                        }
                    }
                    RemoveFromRemovalQueue();
                    if (status == PackageStatus.Available)
                    {
                        RemoveFromInstallQueue();
                        return true;
                    }

                    var latestVersion = await GetLatestVersionAsync();
                    if (latestVersion == null)
                    {
                        RemoveFromInstallQueue();
                        WeavrDebug.LogError(this, $"Error: Unable to find the package [{packageId}]");
                        return false;
                    }

                    // Wait for other intallation to finish
                    while (EditorApplication.isCompiling)
                    {
                        await Task.Yield();
                    }

                    status = await GetStatusAsync();
                    if (status == PackageStatus.Available)
                    {
                        return true;
                    }

                    while (CurrentOperation != null && !IsCurrentOperation)
                    {
                        await Task.Yield();
                    }

                    if (IsQueuedForInstallation)
                    {
                        while (IsQueuedForInstallation && status != PackageStatus.Available)
                        {
                            await Task.Delay(100);
                            status = await GetStatusAsync();
                        }
                    }

                    AddToInstallQueue();
                    MarkAsInstalling(this);
                    WeavrDebug.Log(this, $"Installing [{packageId}] ...");

                    // Install the package
                    CurrentOperation = packageId;
                    var request = Client.Add(latestVersion.packageId);
                    while (request?.IsCompleted == false)
                    {
                        await Task.Yield();
                    }

                    if (request.Error != null)
                    {
                        RemoveFromInstallQueue();
                        WeavrDebug.LogError(this, $"[{packageId}]: Error {request.Error.errorCode}: {request.Error.message}");
                        return false;
                    }

                    if (!IsQueuedForInstallation)
                    {
                        return await Remove();
                    }

                    RemoveFromInstallQueue();
                    return (await GetStatusAsync()) == PackageStatus.Available;
                }
                catch (Exception ex)
                {
                    RemoveFromInstallQueue();
                    WeavrDebug.LogException(this, ex);
                    return false;
                }
                finally
                {
                    if (IsCurrentOperation)
                    {
                        CurrentOperation = null;
                    }
                    MarkAsInstalled(this);
                }
            }

            public async Task<bool> Remove()
            {
                try
                {
                    var status = await GetStatusAsync();
                    RemoveFromInstallQueue();
                    if (status == PackageStatus.Available/* && !IsQueuedForInstallation*/)
                    {
                        // Wait for compilation to end
                        while (EditorApplication.isCompiling)
                        {
                            await Task.Yield();
                        }

                        status = await GetStatusAsync();
                        if (status == PackageStatus.Unavailable)
                        {
                            return true;
                        }

                        // Wait for other intallation to finish
                        while (CurrentOperation != null && !IsCurrentOperation)
                        {
                            await Task.Yield();
                        }

                        if (IsQueuedForRemoval)
                        {
                            while (IsQueuedForRemoval && status == PackageStatus.Available)
                            {
                                await Task.Delay(100);
                                status = await GetStatusAsync();
                            }
                            return true;
                        }

                        AddToRemovalQueue();
                        MarkAsRemoving(this);
                        WeavrDebug.Log(this, $"Removing [{packageId}] ...");
                        CurrentOperation = packageId;
                        var request = Client.Remove(name);
                        await GetStatusAsync();
                        while (request?.IsCompleted == false)
                        {
                            await Task.Yield();
                        }

                        RemoveFromRemovalQueue();
                        if (request.Error != null)
                        {
                            WeavrDebug.LogError(this, $"[{packageId}]: Error {request.Error.errorCode}: {request.Error.message}");
                            return false;
                        }
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    RemoveFromRemovalQueue();
                    WeavrDebug.LogException(this, ex);
                    return false;
                }
                finally
                {
                    if (IsCurrentOperation)
                    {
                        CurrentOperation = null;
                    }
                    MarkAsRemoved(this);
                }
                return true;
            }

            private bool IsValidVersion(string version)
            {
                return (string.IsNullOrEmpty(minVersion) || string.Compare(minVersion, version) <= 0)
                    && (string.IsNullOrEmpty(maxVersion) || string.Compare(version, maxVersion) <= 0);
            }

            public override string ToString()
            {
                if (!string.IsNullOrEmpty(minVersion) && !string.IsNullOrEmpty(maxVersion))
                {
                    return $"{packageId}[{minVersion} - {maxVersion}]";
                }
                else if (!string.IsNullOrEmpty(minVersion))
                {
                    return $"{packageId}[>={minVersion}]";
                }
                else if (!string.IsNullOrEmpty(maxVersion))
                {
                    return $"{packageId}[<={maxVersion}]";
                }
                return packageId;
            }

            public Package(string name, string id, string minVersion = null, string maxVersion = null)
            {
                this.name = name;
                packageId = id;
                this.minVersion = minVersion;
                this.maxVersion = maxVersion;
            }

            public Package(string id)
            {
                packageId = id;
            }

            public Package() { }
        }

        [Serializable]
        public struct RegistryScope
        {
            public string name;
            public string url;
            public List<string> scopes;

            public override int GetHashCode()
            {
                return url?.GetHashCode() ?? 0;
            }

            public override bool Equals(object obj)
            {
                return obj is RegistryScope scope
                    && scope.name == name
                    && scope.url == url
                    && scope.scopes?.Count == scopes?.Count
                    && scope.scopes.Except(scopes).Count() == 0;
            }
        }

        [Serializable]
        public class PackagesManifest
        {
            public Dictionary<string, string> dependencies;
            public string registry = "https://packages.unity.com";
            public List<RegistryScope> scopedRegistries = new List<RegistryScope>();
            public List<string> testables = new List<string>();
            public bool enableLockFile = true;
            public string resolutionStrategy = "lowest";
            public bool useSatSolver = true;
        }

        private static string MandatoryPackagesFilepath => Path.Combine(WeavrEditor.EDITOR_RESOURCES_FULLPATH, "RequiredPackages.json");
        private static string ManifestFilepath => Path.Combine("Packages", "manifest.json");
        private static string CurrentOperation { get; set; }
        public int callbackOrder => 0;

        static WeavrPackageManager()
        {
            EditorApplication.quitting -= EditorApplication_Quitting;
            EditorApplication.quitting += EditorApplication_Quitting;
        }

        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            ClearQueues();
        }

        public static PackagesManifest GetManifest()
        {
            return JsonConvert.DeserializeObject<PackagesManifest>(File.ReadAllText(ManifestFilepath));
        }

        public static void ClearQueues()
        {
            PlayerPrefs.DeleteKey(k_PackagesToInstallKey);
            PlayerPrefs.DeleteKey(k_PackagesToRemoveKey);
        }

        public static void OverwriteManifest(PackagesManifest manifest)
        {
            if (string.IsNullOrEmpty(manifest.resolutionStrategy))
            {
                manifest.resolutionStrategy = "lowest";
            }
            File.WriteAllText(ManifestFilepath, JsonConvert.SerializeObject(manifest));
        }

        public static void RegisterScope(RegistryScope scope, Dictionary<string, string> dependencies = null)
        {
            if (string.IsNullOrEmpty(scope.url)) { return; }
            var manifest = GetManifest();
            if (manifest.scopedRegistries == null)
            {
                manifest.scopedRegistries = new List<RegistryScope>();
            }
            if (!manifest.scopedRegistries.Contains(scope))
            {
                manifest.scopedRegistries.Add(scope);
                if (dependencies != null)
                {
                    foreach (var dependency in dependencies)
                    {
                        manifest.dependencies[dependency.Key] = dependency.Value;
                    }
                }
                OverwriteManifest(manifest);
            }
        }

        public static void UnregisterScope(RegistryScope scope)
        {
            var manifest = GetManifest();
            if (manifest.scopedRegistries?.Contains(scope) == true)
            {
                manifest.scopedRegistries.Remove(scope);
                OverwriteManifest(manifest);
            }
        }

        public static void RemoveProjectDependencies(Dictionary<string, string> dependencies, bool removeRecursive = false)
        {
            var manifest = GetManifest();
            if (manifest.dependencies == null) { return; }

            bool updateFile = false;
            foreach (var dep in dependencies)
            {
                updateFile |= manifest.dependencies.Remove(dep.Key);
            }
            if (updateFile)
            {
                OverwriteManifest(manifest);
            }
        }

        public static void AddProjectDependencies(Dictionary<string, string> dependencies)
        {
            var manifest = GetManifest();
            if (manifest.dependencies == null)
            {
                manifest.dependencies = new Dictionary<string, string>();
            }
            bool updateFile = false;
            foreach (var dep in dependencies)
            {
                if (!dependencies.TryGetValue(dep.Key, out string value) || value != dep.Value)
                {
                    updateFile = true;
                    dependencies[dep.Key] = dep.Value;
                }
            }
            if (updateFile)
            {
                OverwriteManifest(manifest);
            }
        }

        private static void OnStartup()
        {
            ClearQueues();
        }

        private static void EditorApplication_Quitting()
        {
            ClearQueues();
        }

        public static async void ResumeOperations()
        {
            var removeQueue = PlayerPrefs.GetString(k_PackagesToRemoveKey, string.Empty);
            if (removeQueue.Length > 0)
            {
                foreach (var toRemove in removeQueue.Split(';'))
                {
                    var package = new Package(toRemove);
                    await package.Remove();
                }
            }
            var installQueue = PlayerPrefs.GetString(k_PackagesToInstallKey, string.Empty);
            if (installQueue.Length > 0)
            {
                foreach (var toInstall in installQueue.Split(';'))
                {
                    var package = new Package(toInstall);
                    await package.Install();
                }
            }
        }

        public static async void Install(params string[] packagesIds)
        {
            for (int i = 0; i < packagesIds.Length; i++)
            {
                var package = new Package() { packageId = packagesIds[i] };
                await package.Install();
            }
        }

        public static async void Remove(params string[] packagesIds)
        {
            for (int i = 0; i < packagesIds.Length; i++)
            {
                var package = new Package() { packageId = packagesIds[i] };
                await package.Remove();
            }
        }

        public static async void CheckAndInstallRequiredPackages()
        {
            var directory = Path.GetDirectoryName(MandatoryPackagesFilepath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                return;
            }
            if (!File.Exists(MandatoryPackagesFilepath))
            {
                return;
            }
            try
            {
                var packages = JsonConvert.DeserializeObject<Package[]>(File.ReadAllText(MandatoryPackagesFilepath));
                foreach (var package in packages)
                {
                    await package.Install();
                }
            }
            catch (Exception ex)
            {
                WeavrDebug.LogException(nameof(WeavrPackageManager), ex);
            }
        }
    }
}
