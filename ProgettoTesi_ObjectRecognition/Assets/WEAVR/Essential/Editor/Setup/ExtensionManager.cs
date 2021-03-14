using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEngine;

using ManifestDictionary = System.Collections.Generic.Dictionary<System.String, TXT.WEAVR.Core.WeavrPackageManager.PackagesManifest>;

namespace TXT.WEAVR.Core
{
    [InitializeOnEditorStart(nameof(OnStartup))]
    public class ExtensionManager : IActiveBuildTargetChanged
    {
        #region CONSTANTS
        private const string k_IntentsKey = "WEAVR_EXTENSIONS_INTENTS";
        private const string k_RepairsKey = "WEAVR_EXTENSIONS_REPAIRS";
        private const string k_RepairWithImportKey = "WEAVR_EXTENSIONS_REPAIRS_WITH_IMPORT";
        private const string k_ReimportsKey = "WEAVR_EXTENSIONS_REIMPORTS";
        private const string k_BuildTargetManifests = "WEAVR_EXTENSIONS_MANIFESTS";

        private const string kSymbols_WeavrVR = "WEAVR_VR";
        private const string kSymbols_WeavrAR = "WEAVR_AR";
        private const string kSymbols_WeavrExtensionsMRTK = "WEAVR_EXTENSIONS_MRTK";
        private const string kSymbols_WeavrExtensionsObi = "WEAVR_EXTENSIONS_OBI";
        private const string kSymbols_WeavrExtensionsSteamVR = "WEAVR_EXTENSIONS_STEAMVR";
        private const string kSymbols_WeavrNetwork = "WEAVR_NETWORK";

        #endregion CONSTANTS

        private static Dictionary<string, Extension> s_extensions;
        private static Dictionary<string, ExtensionWrapper> s_wrappers = new Dictionary<string, ExtensionWrapper>();
        private static Dictionary<string, ExtensionIntent> s_extensionsIntents;

        internal static IEnumerable<ExtensionWrapper> ExtensionWrappers
        {
            get
            {
                if(s_wrappers.Count != AllExtensions.Count)
                {
                    foreach(var extension in AllExtensions)
                    {
                        if (!s_wrappers.ContainsKey(extension.Key))
                        {
                            s_wrappers[extension.Key] = new ExtensionWrapper(extension.Value);
                        }
                    }
                }
                return s_wrappers.Values;
            }
        }
        internal static ExtensionWrapper Extension_VR => GetExtensionWrapper(kSymbols_WeavrVR);
        internal static ExtensionWrapper Extension_AR => GetExtensionWrapper(kSymbols_WeavrAR);
        internal static ExtensionWrapper Extension_Network => GetExtensionWrapper(kSymbols_WeavrNetwork);

        public static string ExtensionsFilepath => Path.Combine(WeavrEditor.EDITOR_RESOURCES_FULLPATH, "Extensions.json");

        public static event Action<ExtensionIntent> ExtensionIntentChanged;

        public static IReadOnlyDictionary<string, Extension> AllExtensions
        {
            get
            {
                if(s_extensions == null)
                {
                    s_extensions = LoadDefaultExtensions().ToDictionary(e => e.name);
                }
                return s_extensions;
            }
        }

        private static Dictionary<string, ExtensionIntent> ExtensionsIntents
        {
            get
            {
                if(s_extensionsIntents == null)
                {
                    try
                    {
                        var currentIntents = PlayerPrefs.GetString(k_IntentsKey, string.Empty);
                        s_extensionsIntents = JsonConvert.DeserializeObject<ExtensionIntent[]>(currentIntents)?
                                                         .ToDictionary(i => i.name)
                                                         ?? new Dictionary<string, ExtensionIntent>();
                    }
                    catch
                    {
                        s_extensionsIntents = new Dictionary<string, ExtensionIntent>();
                    }
                }
                return s_extensionsIntents;
            }
        }

        public int callbackOrder => 1;

        private static IEnumerable<Extension> LoadDefaultExtensions()
        {
            if (File.Exists(ExtensionsFilepath))
            {
                try
                {
                    return JsonConvert.DeserializeObject<Extension[]>(File.ReadAllText(ExtensionsFilepath));
                }
                catch(Exception ex) 
                {
                    WeavrDebug.LogException(typeof(ExtensionManager), ex);
                }
            }
            return new Extension[0];
        }

        static void OnStartup()
        {
            // Delete any unprocessed intents (most probably still there due to previous Unity crash)
            ResetExtensionsIntents();
        }

        static void OnPlatformChanged()
        {
            // Refresh the extensions based on symbols
            //Debug.Log("Platform Changed");
            RepairExtensions(false, false);
        }

        public static void SetExtensionIntent(string name, bool enable)
        {
            if(!ExtensionsIntents.TryGetValue(name, out ExtensionIntent intent))
            {
                intent = new ExtensionIntent()
                {
                    name = name,
                    enable = enable,
                };
            }
            else
            {
                intent.enable = enable;
            }

            //Debug.Log("ADD INTENT: " + name);
            ExtensionsIntents[name] = intent;
            PlayerPrefs.SetString(k_IntentsKey, JsonConvert.SerializeObject(ExtensionsIntents.Values.ToArray()));
        }

        public static bool? GetExtensionIntent(string name) => ExtensionsIntents.TryGetValue(name, out ExtensionIntent intent) ? intent.enable : default;

        public static void ClearExtensionIntent(string name)
        {
            if (ExtensionsIntents.Remove(name))
            {
                //Debug.Log("REMOVE INTENT: " + name);
                if (ExtensionsIntents.Count > 0)
                {
                    PlayerPrefs.SetString(k_IntentsKey, JsonConvert.SerializeObject(ExtensionsIntents.Values.ToArray()));
                }
                else
                {
                    PlayerPrefs.DeleteKey(k_IntentsKey);
                }
            }
        }

        private static void RegisterExtensionForRepair(string name)
        {
            var reimportString = PlayerPrefs.GetString(k_RepairsKey, string.Empty);
            if (!reimportString.Contains(name))
            {
                //Debug.Log("ADD REPAIR: " + name);
                PlayerPrefs.SetString(k_RepairsKey, reimportString + name + ";");
            }
        }

        private static void UnregisterExtensionFromRepair(string name)
        {
            var reimportString = PlayerPrefs.GetString(k_RepairsKey, string.Empty);
            if (reimportString.Contains(name + ";"))
            {
                //Debug.Log("REMOVE REPAIR: " + name);
                reimportString = reimportString.Replace(name + ";", string.Empty);
                if (reimportString.Length > 0)
                {
                    PlayerPrefs.SetString(k_RepairsKey, reimportString);
                }
                else
                {
                    PlayerPrefs.DeleteKey(k_RepairsKey);
                    PlayerPrefs.DeleteKey(k_RepairWithImportKey);
                }
            }
        }

        private static bool IsRepairRegistered(string name) => PlayerPrefs.GetString(k_RepairsKey, string.Empty).Contains(name);

        private static void RegisterReimportPath(string path)
        {
            var reimportString = PlayerPrefs.GetString(k_ReimportsKey, string.Empty);
            if (!reimportString.Contains(path))
            {
                PlayerPrefs.SetString(k_ReimportsKey, reimportString + path + ";");
            }
        }

        private static void UnregisterReimportPath(string path)
        {
            var reimportString = PlayerPrefs.GetString(k_ReimportsKey, string.Empty);
            if (reimportString.Contains(path + ";"))
            {
                reimportString.Replace(path + ";", string.Empty);
                if (reimportString.Length > 0)
                {
                    PlayerPrefs.SetString(k_ReimportsKey, reimportString);
                }
                else
                {
                    PlayerPrefs.DeleteKey(k_ReimportsKey);
                }
            }
        }

        private static bool IsReimportPathRegistered(string path) => PlayerPrefs.GetString(k_ReimportsKey, string.Empty).Contains(path);

        internal static ExtensionWrapper GetExtensionWrapper(Extension extension)
        {
            if(!s_wrappers.TryGetValue(extension.name, out ExtensionWrapper wrapper) || wrapper.extension != extension)
            {
                wrapper = new ExtensionWrapper(extension);
                s_wrappers[extension.name] = wrapper;
            }
            return wrapper;
        }

        internal static ExtensionWrapper GetExtensionWrapper(string name)
        {
            if(AllExtensions.TryGetValue(name, out Extension extension))
            {
                return GetExtensionWrapper(extension);
            }
            extension = AllExtensions.Values.FirstOrDefault(e => e.preprocessors.Any(p => p == name));
            return extension != null ? GetExtensionWrapper(extension) : null;
        }

        public static void RegisterExtension(Extension extension)
        {
            if (s_extensions == null)
            {
                s_extensions = LoadDefaultExtensions().ToDictionary(e => e.name);
            }
            s_extensions[extension.name] = extension;
        }

        public static void UnregisterExtension(Extension extension)
        {
            if (s_extensions != null 
                && s_extensions.TryGetValue(extension.name, out Extension ex)
                && ex == extension)
            {
                s_extensions.Remove(ex.name);
            }
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        [InitializeOnLoadMethod]
        private static void ResumeExtensions()
        {
            EditorApplication.quitting -= EditorApplication_Quitting;
            EditorApplication.quitting += EditorApplication_Quitting;

            //EditorApplication.delayCall += DelayedApplyExtensionsSetup;
            DelayedApplyExtensionsSetup();
        }

        private static void DelayedApplyExtensionsSetup()
        {
            EditorApplication.delayCall -= DelayedApplyExtensionsSetup;
            RepairExtensionsInternal(PlayerPrefs.GetInt(k_RepairWithImportKey, 0) > 0, false);
            ApplyExtensionSetupInternal(true);
        }

        private static void EditorApplication_Quitting()
        {
            ResetExtensionsIntents();
        }

        public static void ApplyExtensionSetup(bool showProgress = true, bool applyToAllGroups = false)
        {
            foreach (var wrapper in ExtensionWrappers)
            {
                if (wrapper.ShouldPerformAction)
                {
                    SetExtensionIntent(wrapper.extension.name, wrapper.IsEnabled);
                }
            }
            ApplyExtensionSetupInternal(showProgress, applyToAllGroups);
        }

        private static async void ApplyExtensionSetupInternal(bool showProgress, bool applyToAllGroups = false)
        {
            foreach (var wrapper in ExtensionWrappers)
            {
                if (wrapper.ShouldPerformAction)
                {
                    try
                    {
                        if (await wrapper.PerformAction(showProgress, applyToAllGroups))
                        {
                            ClearExtensionIntent(wrapper.extension.name);
                            UnregisterExtensionFromRepair(wrapper.extension.name);
                        }
                    }
                    catch (Exception ex)
                    {
                        WeavrDebug.LogException(wrapper.extension.name, ex);
                    }
                    finally
                    {
                        EditorUtility.ClearProgressBar();
                    }
                }
            }
        }

        public static void RepairExtensions(bool reimportFiles = true, bool showProgress = true)
        {
            foreach(var wrapper in ExtensionWrappers)
            {
                RegisterExtensionForRepair(wrapper.extension.name);
            }
            PlayerPrefs.SetInt(k_RepairWithImportKey, reimportFiles ? 1 : 0);
            WeavrPackageManager.ClearQueues();
            RepairExtensionsInternal(reimportFiles, showProgress);
            if (!EditorApplication.isCompiling)
            {
                CompilationPipeline.RequestScriptCompilation();
            }
        }

        private static async void RepairExtensionsInternal(bool reimportFiles, bool showProgress)
        {
            foreach (var wrapper in ExtensionWrappers.ToList())
            {
                try
                {
                    //Debug.Log($"IS REPAIR REGISTERED: {wrapper.extension.name} = {IsRepairRegistered(wrapper.extension.name)}");
                    if (IsRepairRegistered(wrapper.extension.name) && await wrapper.Repair(reimportFiles, showProgress))
                    {
                        UnregisterExtensionFromRepair(wrapper.extension.name);
                    }
                }
                catch (Exception ex)
                {
                    WeavrDebug.LogException(wrapper.extension.name, ex);
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        [Serializable]
        internal class ExtensionWrapper
        {
            public readonly Extension extension;
            private bool m_inProgress;
            private bool m_isEnabled;

            public bool IsEnabled {
                //get => ExtensionsIntents.TryGetValue(extension.name, out ExtensionIntent intent) ? intent.enable : IsExtensionFullyEnabled();
                //set => SetExtensionIntent(extension.name, value);
                get => m_isEnabled;
                set => m_isEnabled = value;
            }
            public string Description { get; private set; }
            public bool CanBeEdited => !m_inProgress;
            public bool ShouldPerformAction => !m_inProgress && (ExtensionsIntents.TryGetValue(extension.name, out ExtensionIntent intent) || m_isEnabled != IsExtensionFullyEnabled());

            public virtual bool IsExtensionFullyEnabled()
            {
                foreach(var symbol in extension.preprocessors)
                {
                    if (!SymbolsHelper.HasSymbol(symbol))
                    {
                        return false;
                    }
                }
                return true;
            }

            public virtual bool IsValid()
            {
                for (int i = 0; i < extension.requiredPaths.Length; i++)
                {
                    if (!File.Exists(extension.requiredPaths[i]) && !Directory.Exists(extension.requiredPaths[i]))
                    {
                        return false;
                    }
                }
                if(extension.validPlatforms?.Length > 0)
                {
                    var platform = EditorUserBuildSettings.activeBuildTarget.ToString().ToLower();
                    if(!extension.validPlatforms.Any(p => p.ToLower() == platform))
                    {
                        return false;
                    }
                }
                if (extension.invalidPlatforms?.Length > 0)
                {
                    var platform = EditorUserBuildSettings.activeBuildTarget.ToString().ToLower();
                    return !extension.invalidPlatforms.Any(p => p.ToLower() == platform);
                }
                return true;
            }

            public virtual async Task<bool> PerformAction(bool showProgress, bool applyToAllGroups)
            {
                try
                {
                    m_inProgress = true;
                    if (applyToAllGroups)
                    {
                        var currentGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
                        ActivateSymbolsForGroups(IsEnabled, GetValidBuildTargets()
                                                            .Select(b => BuildPipeline.GetBuildTargetGroup(b))
                                                            .Where(g => g != currentGroup));
                    }
                    if (IsValid())
                    {
                        if (IsEnabled)
                        {
                            return await EnableExtension(showProgress);
                        }
                        else
                        {
                            return await DisableExtension(showProgress);
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
                finally
                {
                    m_inProgress = false;
                }
            }

            public IEnumerable<BuildTarget> GetValidBuildTargets()
            {
                IEnumerable<string> allTargets = Enum.GetNames(typeof(BuildTarget));
                if(extension.validPlatforms?.Length > 0)
                {
                    allTargets = allTargets.Intersect(extension.validPlatforms);
                }
                if(extension.invalidPlatforms?.Length > 0)
                {
                    allTargets = allTargets.Except(extension.invalidPlatforms);
                }
                return allTargets.Select(t => (BuildTarget)Enum.Parse(typeof(BuildTarget), t))
                                 .Where(t => t.GetType().GetAttribute<ObsoleteAttribute>() == null);
            }

            private void ActivateSymbolsForGroups(bool enable, IEnumerable<BuildTargetGroup> groups)
            {
                groups = groups.Distinct();
                if (enable)
                {
                    foreach(var group in groups)
                    {
                        foreach(var symbol in extension.preprocessors)
                        {
                            SymbolsHelper.SetSymbol(symbol, group);
                        }
                    }
                }
                else
                {
                    foreach (var group in groups)
                    {
                        foreach (var symbol in extension.preprocessors)
                        {
                            SymbolsHelper.RemoveSymbol(symbol, group);
                        }
                    }
                }
            }

            private async Task<bool> EnableExtension(bool showProgress)
            {
                // First copy the files
                int counter = 1;
                foreach (var file in extension.filesToCopy)
                {
                    try
                    {
                        if (showProgress)
                        {
                            EditorUtility.DisplayProgressBar($"Enabling {extension.name}...",
                                                             $"Copying files [{counter++}/{extension.filesToCopy.Length}]",
                                                             counter / (float)extension.filesToCopy.Length * 0.45f);
                        }
                        CopyFile(file);
                    }
                    catch (Exception ex)
                    {
                        WeavrDebug.LogException(extension.name, ex);
                        return false;
                    }
                }

                // Then install the packages
                counter = 1;
                foreach (var package in extension.packages)
                {
                    try
                    {
                        if (showProgress)
                        {
                            EditorUtility.DisplayProgressBar($"Enabling {extension.name}...",
                                                             $"Installing packages [{counter++}/{extension.packages.Length}]",
                                                             0.45f + counter / (float)extension.packages.Length * 0.45f);
                        }
                        await InstallPackage(package);
                    }
                    catch (Exception ex)
                    {
                        WeavrDebug.LogException(package?.package?.name, ex);
                        return false;
                    }
                }

                // Last checkup before setting the preprocessor
                foreach (var package in extension.packages)
                {
                    try
                    {
                        if (package.package != null)
                        {
                            var status = await package.package.GetStatusAsync();
                            if (status == PackageStatus.Error
                                || status == PackageStatus.Unavailable
                                || status == PackageStatus.Unknown)
                            {
                                return false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        WeavrDebug.LogException(package?.package?.name, ex);
                        return false;
                    }
                }

                // Apply symbols
                counter = 1;
                foreach (var p in extension.preprocessors)
                {
                    if (showProgress)
                    {
                        EditorUtility.DisplayProgressBar($"Enabling {extension.name}...",
                                                         $"Activating Preprocessor {p} [{counter++}/{extension.preprocessors.Length}]",
                                                         0.9f + counter / (float)extension.preprocessors.Length * 0.1f);
                    }
                    SymbolsHelper.SetSymbol(p);
                }

                // Apply reimports if any
                ReimportFiles();
                return true;
            }

            private void ReimportFiles()
            {
                if (extension.pathsToReimport?.Length > 0)
                {
                    foreach (var path in extension.pathsToReimport)
                    {
                        try
                        {
                            if (File.Exists(path) && !IsReimportPathRegistered(path))
                            {
                                RegisterReimportPath(path);
                                AssetDatabase.ImportAsset(path, ImportAssetOptions.ImportRecursive);
                            }
                        }
                        catch (Exception ex)
                        {
                            WeavrDebug.LogException(extension.name, ex);
                        }
                    }
                    foreach (var path in extension.pathsToReimport)
                    {
                        try
                        {
                            UnregisterReimportPath(path);
                        }
                        catch (Exception ex)
                        {
                            WeavrDebug.LogException(extension.name, ex);
                        }
                    }
                }
            }

            private static async Task InstallPackage(ExtensionPackage package)
            {
                foreach (var scope in package.scopes)
                {
                    WeavrPackageManager.RegisterScope(scope.scope, scope.dependencies);
                }
                if (package.package != null)
                {
                    await package.package.Install();
                }
            }

            private static void CopyFile(ExtensionCopyFile file)
            {
                if (!File.Exists(file.destination) || file.overwrite)
                {
                    if (!Directory.Exists(Path.GetDirectoryName(file.destination)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(file.destination));
                    }
                    File.Copy(file.source, file.destination);
                }
            }

            private async Task<bool> DisableExtension(bool showProgress)
            {
                // First copy the files
                int counter = 1;

                // Reset the symbols
                foreach (var p in extension.preprocessors)
                {
                    if (showProgress)
                    {
                        EditorUtility.DisplayProgressBar($"Disabling {extension.name}...",
                                                         $"Deactivating Preprocessor {p} [{counter++}/{extension.preprocessors.Length}]",
                                                         counter / (float)extension.preprocessors.Length * 0.1f);
                    }
                    SymbolsHelper.RemoveSymbol(p);
                }

                var filesToDelete = extension.filesToCopy.Reverse().Where(f => f.deleteOnDisable 
                                                                            && !string.IsNullOrEmpty(f.destination) 
                                                                            && File.Exists(f.destination)).ToArray();
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        if (showProgress)
                        {
                            EditorUtility.DisplayProgressBar($"Disabling {extension.name}...",
                                                             $"Deleting files [{counter++}/{filesToDelete.Length}]",
                                                             0.1f + counter / (float)filesToDelete.Length * 0.45f);
                        }
                        DeleteFile(file);
                    }
                    catch (Exception ex)
                    {
                        WeavrDebug.LogException(extension.name, ex);
                        return false;
                    }
                }

                // Then install the packages
                counter = 1;
                var packagesToRemoveCount = extension.packages.Count(p => p.removeOnDisable && p.package != null);
                List<(string name, string version)> dependencies = new List<(string name, string version)>();
                foreach (var package in extension.packages.Reverse())
                {
                    try
                    {
                        if (showProgress && package.removeOnDisable && package.package != null)
                        {
                            EditorUtility.DisplayProgressBar($"Disabling {extension.name}...",
                                                                 $"Removing packages [{counter++}/{packagesToRemoveCount}]",
                                                                 0.55f + counter / (float)packagesToRemoveCount * 0.45f);
                        }
                        if (package.removeOnDisable && package.package != null)
                        {
                            dependencies.Add(await package.package.AsDependency());
                        }
                        foreach (var scope in package.scopes)
                        {
                            if (scope?.unregisterOnDisable == true)
                            {
                                WeavrPackageManager.UnregisterScope(scope.scope);
                            }
                            if (scope?.removeDependenciesOnDisable == true)
                            {
                                foreach(var pair in scope.dependencies)
                                {
                                    dependencies.Add((pair.Key, pair.Value));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        WeavrDebug.LogException(package?.package?.name, ex);
                        return false;
                    }
                }

                if (dependencies.Count > 0) {
                    Dictionary<string, string> depsToRemove = new Dictionary<string, string>();
                    foreach (var (key, value) in dependencies)
                    {
                        depsToRemove[key] = value;
                    }
                    WeavrPackageManager.RemoveProjectDependencies(depsToRemove);
                }
                return true;
            }

            private static async Task RemovePackage(ExtensionPackage package)
            {
                if (package.removeOnDisable && package.package != null)
                {
                    await package.package.Remove();
                }
                foreach (var scope in package.scopes)
                {
                    if (scope?.unregisterOnDisable == true)
                    {
                        WeavrPackageManager.UnregisterScope(scope.scope);
                    }
                    if (scope?.removeDependenciesOnDisable == true)
                    {
                        WeavrPackageManager.RemoveProjectDependencies(scope.dependencies);
                    }
                }
            }

            private static void DeleteFile(ExtensionCopyFile file)
            {
                if (file.deleteOnDisable)
                {
                    File.Delete(file.destination);
                }
            }

            public async Task<bool> Repair(bool reimportFiles, bool showProgress = false)
            {
                bool shouldBeEnabled = IsExtensionFullyEnabled();
                RegisterExtensionForRepair(extension.name);
                // First copy the files
                int counter = 1;
                foreach (var file in extension.filesToCopy)
                {
                    try
                    {
                        if (showProgress)
                        {
                            EditorUtility.DisplayProgressBar($"Fixing {extension.name}...",
                                                             $"Working on files [{counter++}/{extension.filesToCopy.Length}]",
                                                             counter / (float)extension.filesToCopy.Length * 0.5f);
                        }
                        if (shouldBeEnabled)
                        {
                            CopyFile(file);
                        }
                        else
                        {
                            DeleteFile(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        WeavrDebug.LogException(extension.name, ex);
                        return false;
                    }
                }

                // Then install the packages
                counter = 1;
                foreach (var package in extension.packages)
                {
                    try
                    {
                        if (showProgress)
                        {
                            EditorUtility.DisplayProgressBar($"Fixing {extension.name}...",
                                                             $"Working on packages [{counter++}/{extension.packages.Length}]",
                                                             0.5f + counter / (float)extension.packages.Length * 0.5f);
                        }
                        if (shouldBeEnabled)
                        {
                            await InstallPackage(package);
                        }
                        else
                        {
                            await RemovePackage(package);
                        }
                    }
                    catch (Exception ex)
                    {
                        WeavrDebug.LogException(package?.package?.name, ex);
                        return false;
                    }
                }
                if (reimportFiles)
                {
                    ReimportFiles();
                }

                UnregisterExtensionFromRepair(extension.name);
                return true;
            }

            public ExtensionWrapper(Extension extension)
            {
                this.extension = extension;
                Description = extension.description;
                m_inProgress = false;
                IsEnabled = ExtensionsIntents.TryGetValue(extension.name, out ExtensionIntent intent) ? intent.enable : IsExtensionFullyEnabled();
            }
        }

        internal static void ResetExtensions(bool removeIntents = false)
        {
            s_wrappers?.Clear();
            s_extensions = null;
            if (removeIntents)
            {
                ResetExtensionsIntents();
            }
        }

        public static void ResetExtensionsIntents()
        {
            s_extensionsIntents = null;
            PlayerPrefs.DeleteKey(k_IntentsKey);
            PlayerPrefs.DeleteKey(k_RepairsKey);
            PlayerPrefs.DeleteKey(k_RepairWithImportKey);
            PlayerPrefs.DeleteKey(k_ReimportsKey);
        }

        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            WeavrDebug.Log(nameof(ExtensionManager), $"Switching extensions from [{previousTarget}] to [{newTarget}]...");
            var manifests = JsonConvert.DeserializeObject<ManifestDictionary>(PlayerPrefs.GetString(k_BuildTargetManifests, string.Empty)) ?? new ManifestDictionary();
            var current = WeavrPackageManager.GetManifest();
            try
            {
                Dictionary<string, string> newlyAddedDependencies = null;
                if (manifests.TryGetValue(previousTarget.ToString(), out WeavrPackageManager.PackagesManifest previous))
                {
                    newlyAddedDependencies = Except(Except(current.dependencies, previous.dependencies), GetAllExtensionsDependencies());
                }
                manifests[previousTarget.ToString()] = current;
                if (manifests.TryGetValue(newTarget.ToString(), out WeavrPackageManager.PackagesManifest manifest))
                {
                    if(newlyAddedDependencies != null)
                    {
                        manifest.dependencies = Merge(manifest.dependencies, newlyAddedDependencies);
                    }
                    WeavrPackageManager.OverwriteManifest(manifest);
                }
            }
            finally
            {
                PlayerPrefs.SetString(k_BuildTargetManifests, JsonConvert.SerializeObject(manifests));
            }
            RepairExtensions(false, false);
        }

        private static Dictionary<T, S> ToDictionary<T, S>(IEnumerable<KeyValuePair<T, S>> pairs)
        {
            Dictionary<T, S> dictionary = new Dictionary<T, S>();
            foreach(var pair in pairs)
            {
                dictionary[pair.Key] = pair.Value;
            }
            return dictionary;
        }

        private static Dictionary<T, S> Except<T, S>(Dictionary<T, S> a, Dictionary<T, S> b)
        {
            Dictionary<T, S> r = new Dictionary<T, S>();
            foreach(var pair in a)
            {
                if (!b.ContainsKey(pair.Key))
                {
                    r[pair.Key] = pair.Value;
                }
            }
            return r;
        }

        private static Dictionary<T, S> Merge<T, S>(Dictionary<T, S> a, Dictionary<T, S> b)
        {
            Dictionary<T, S> r = new Dictionary<T, S>(a);
            foreach (var pair in b)
            {
                r[pair.Key] = pair.Value;
            }
            return r;
        }

        private static Dictionary<string, string> GetAllExtensionsDependencies()
        {
            Dictionary<string, string> deps = new Dictionary<string, string>();
            foreach(var extension in AllExtensions)
            {
                if(extension.Value.packages?.Length > 0)
                {
                    foreach(var package in extension.Value.packages)
                    {
                        if(package.package != null)
                        {
                            deps[package.package.GetNameFromId()] = package.package.Version;
                        }
                        if(package.scopes?.Length > 0)
                        {
                            foreach(var scope in package.scopes)
                            {
                                if(scope.dependencies?.Count > 0)
                                {
                                    foreach(var dep in scope.dependencies)
                                    {
                                        deps[dep.Key] = dep.Value;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return deps;
        }

        private struct BuildTargetManifest
        {
            public string buildTarget;
            public WeavrPackageManager.PackagesManifest manifest;
        }

        [Serializable]
        public struct ExtensionIntent
        {
            public string name;
            public bool enable;
        }

        [Serializable]
        public class Extension
        {
            public string name;
            public string description;
            public string[] preprocessors;
            public string[] requiredPaths;
            public string[] validPlatforms;
            public string[] invalidPlatforms;
            public string[] pathsToReimport;
            public ExtensionPackage[] packages;
            public ExtensionCopyFile[] filesToCopy;
        }

        [Serializable]
        public class ExtensionPackage
        {
            public WeavrPackageManager.Package package;
            public ExtensionRegistryScope[] scopes;
            public bool removeOnDisable;
        }

        [Serializable]
        public class ExtensionRegistryScope
        {
            public WeavrPackageManager.RegistryScope scope;
            public Dictionary<string, string> dependencies;
            public bool unregisterOnDisable;
            public bool removeDependenciesOnDisable;
        }

        [Serializable]
        public class ExtensionCopyFile
        {
            public string source;
            public string destination;
            public bool deleteOnDisable;
            public bool overwrite;
        }
    }
}
