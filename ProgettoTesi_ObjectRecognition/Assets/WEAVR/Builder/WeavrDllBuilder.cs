using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TXT.WEAVR.Core;
using TXT.WEAVR.License;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace TXT.WEAVR.Builder
{

    internal class WeavrDllBuilder : MonoBehaviour
    {
        #region Utility Class

        private class BuildPlatform
        {
            public string Platform { get; set; }
            public BuildPair BuildPair { get; set; }

            public BuildPlatform(string platform, BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
            {
                this.Platform = platform;
                this.BuildPair = new BuildPair(buildTargetGroup, buildTarget);
            }
            public BuildPlatform(string platform, BuildPair buildPair)
            {
                this.Platform = platform;
                this.BuildPair = buildPair;
            }
        }

        private class BuildPair
        {
            public BuildTargetGroup BuildTargetGroup { get; set; }
            public BuildTarget BuildTarget { get; set; }

            public BuildPair(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
            {
                this.BuildTargetGroup = buildTargetGroup;
                this.BuildTarget = buildTarget;
            }
        }

        #endregion

        private static readonly string OUTPUT_FOLDER = "WEAVR_Build";
        private static readonly string WEAVR_FOLDER_NAME = "WEAVR";

        private static readonly string WEAVR_PATH = Path.Combine("Assets", WEAVR_FOLDER_NAME);

        private static readonly string SCRIPT_ASSEMBLIES = Path.Combine("Library", "ScriptAssemblies");

        private static readonly List<(string folder, string wildcard)> FOLDERS_TO_COPY = new List<(string folder, string wildcard)>()
        {
            ( Path.Combine("ThirdParty", "CurvedUI"), "*.*" ),
            ( Path.Combine("ThirdParty", "MRTK"), "*.*" ),
            ( Path.Combine("ThirdParty", "JsonDotNet"), "*.*" ),
            ( Path.Combine("ThirdParty", "Licensing"), "*.*" ),
            //( Path.Combine("ThirdParty", "Manus"), "*.*" ),
            //( Path.Combine("ThirdParty", "Obi"), "*.*" ),
            ( Path.Combine("ThirdParty", "Photon"), "*.*" ),
            ( Path.Combine("ThirdParty", "SimulationHub"), "*.*" ),
            ( Path.Combine("ThirdParty", "VIVE"), "*.*" ),
            //( Path.Combine("ThirdParty", "WebRTC"), "*.*" ),
            ( "Plugins", "*.*" ),
            ( Path.Combine("Essential", "Rendering"), "*.*"),
            ( Path.Combine("Essential", "Extensions"), "*.*"),
            //( "Player", "*.*" ),
        };

        private static readonly List<string> EXTENSIONS_TO_SKIP = new List<string>() { ".asmdef", ".asmdef.meta", ".prefab", ".tmp" };
        private static readonly List<string> FILES_TO_SKIP = new List<string>() { ".git", ".gitignore" };
        private static readonly List<string> FOLDERS_TO_SKIP = new List<string>()
        {
            Path.Combine("Essential", "Tests"),
            Path.Combine("Player", "Tests"),
        };

        private static readonly Dictionary<string, List<string>> MODULES = new Dictionary<string, List<string>>
        {
            { "Cockpit", new List<string> { "Editor", "Runtime" } },
            { "Creator", new List<string>{ } },
            { "Demo", new List<string>{ } },
            { "Essential", new List<string>{ "Editor", "Runtime"/*, "Rendering/URP"*/ } },
            { "Maintenance", new List<string>{ "Editor", "Runtime" } },
            //{ "Monitoring", new List<string>{ } },
            { "Networking", new List<string>{ "WeavrMultiplayer" } },
            { "Packaging", new List<string>{} },
            //{ "Player", new List<string>{ } },
            //{ "RemoteControl", new List<string>{ "Editor", "Runtime" } },
            { "Simulation", new List<string>{ "Editor", "Runtime" } },
        };

        private static readonly Dictionary<string, string[]> MODULE_SYMBOLS = new Dictionary<string, string[]>
        {
            { "WEAVR.Essential.Runtime.ReflectionAOT", new string[] { "ENABLE_IL2CPP" } },
            { "WEAVR.Essential.Runtime.Reflection", new string[] { "ENABLE_MONO" } },
        };

        private static readonly Dictionary<string, string[]> MODULE_REFERENCES_EXCLUDES = new Dictionary<string, string[]>
        {
            { "WEAVR.Essential.Editor", new string[] { "WEAVR.Creator" } },
        };

        private static readonly Dictionary<string, List<string>> DIFFERENTS = new Dictionary<string, List<string>>
        {
            { "WEAVR.Essential.Runtime.Reflection", new List<string>{ "WEAVR.Essential.Runtime.Reflection", "WEAVR.Essential.Runtime.ReflectionAOT"/*, "WEAVR.Essential.Runtime.ReflectionUWP"*/ } },
            { "WEAVR.Extensions", new List<string>{ "WEAVR.Extensions.Common", "WEAVR.Extensions.UWP" } },
        };

        private static readonly List<string> cIncompatibleDlls = new List<string>() { "WEAVR.Essential.Runtime.Reflection*.dll", "WEAVR.Extensions.Common*.dll" };



        #region platforms

        private static readonly List<BuildPlatform> SUPPORT_PLATFORMS = new List<BuildPlatform>
        {
            new BuildPlatform("WindowsStandalone64", new BuildPair (BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64)),
            new BuildPlatform("WindowsStandalone32", new BuildPair (BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows)),
            new BuildPlatform("Android", new BuildPair (BuildTargetGroup.Android, BuildTarget.Android)),
            new BuildPlatform("iOS", new BuildPair (BuildTargetGroup.iOS, BuildTarget.iOS)),
            new BuildPlatform("WSA", new BuildPair (BuildTargetGroup.WSA, BuildTarget.WSAPlayer)),
            new BuildPlatform("Editor", new BuildPair (BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64)),
        };

        private static readonly List<string> PLATFORMS = new List<string>
        {
            "Android",
            "Editor",
            "iOS",
            "LinuxStandalone64",
            "macOSStandalone",
            "Nintendo3DS",
            "PS4",
            "PSVita",
            "Switch",
            "tvOS",
            "WSA",
            "WebGL",
            "WindowsStandalone32",
            "WindowsStandalone64",
            "XboxOne"
        };

        #endregion

        private static Dictionary<string, string> mFilePathByGuid;
        private static Dictionary<string, string> mScriptOrderByFilePath;
        private static Dictionary<string, string> mAsmDefByPath;
        private static Dictionary<string, (Assembly assembly, string[] scripts)> mAsmDefScripts;

        private static WeavrBuildUtility _weavrBuildUtility;
        private static string sBuildPath;

        [MenuItem("WEAVR/Builder/Build Weavr dll", priority = 50)]
        public static void ShowWindow()
        {
            var m_coroutine = EditorCoroutine.StartCoroutine(CreateBuild());
        }

        private void OnEnable()
        {
            if (!WeavrLE.IsValid())
            {
                DestroyImmediate(this);
                return;
            }
        }

        private static void UpdateMetas()
        {
            var wFilesMeta = Directory.GetFiles(Path.Combine(WEAVR_PATH), "*.meta", SearchOption.AllDirectories);
            foreach (var wFileMeta in wFilesMeta)
            {
                using (StreamReader wFile = File.OpenText(wFileMeta))
                {
                    string wFilePath = null;

                    string wLine;
                    while ((wLine = wFile.ReadLine()) != null)
                    {
                        if (wFilePath == null && wLine.Contains("guid: "))
                        {
                            var match = Regex.Match(wLine, @"[0-9a-f]{8}[-]?[0-9a-f]{4}[-]?[0-9a-f]{4}[-]?[0-9a-f]{4}[-]?[0-9a-f]{12}");
                            if (match.Success)
                            {
                                wFilePath = wFileMeta.Replace("\\", "/").Replace(".meta", "");
                                mFilePathByGuid.Add(match.Value, wFilePath);
                            }
                        }

                        if (wFilePath != null && wLine.Contains("executionOrder: "))
                        {
                            var wExecutionOrder = wLine.Replace("executionOrder: ", "").Trim();
                            if (!string.IsNullOrEmpty(wExecutionOrder) && wExecutionOrder != "{}" && wExecutionOrder != "0" && wExecutionOrder != "-0")
                            {
                                mScriptOrderByFilePath.Add(wFilePath, wExecutionOrder);
                            }
                        }
                    }
                }
            }
        }

        private static string[] GetFilesFromAsmdef(string fileAsmdef)
        {
            var directory = Path.GetDirectoryName(fileAsmdef);
            var files = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);

            return files.ToArray();
        }

        private static string[] GetAsms()
        {
            List<string> wAsms = new List<string>();
            foreach (var kvp in MODULES)
            {
                if (kvp.Value.Count > 0)
                {
                    foreach (var wFolder in kvp.Value)
                    {
                        if (!Directory.Exists(Path.Combine(WEAVR_PATH, kvp.Key)))
                        {
                            Debug.LogError($"Path [{Path.Combine(WEAVR_PATH, kvp.Key)}] not found.");
                            continue;
                        }
                        if (!Directory.Exists(Path.Combine(WEAVR_PATH, kvp.Key, wFolder)))
                        {
                            Debug.LogError($"Path [{Path.Combine(WEAVR_PATH, kvp.Key, wFolder)}] not found.");
                            continue;
                        }

                        wAsms.AddRange(Directory.GetFiles(Path.Combine(WEAVR_PATH, kvp.Key, wFolder), "*.asmdef", SearchOption.AllDirectories));
                    }
                }
                else
                {
                    if (!Directory.Exists(Path.Combine(WEAVR_PATH, kvp.Key)))
                    {
                        Debug.LogError($"Path [{Path.Combine(WEAVR_PATH, kvp.Key)}] not found.");
                        continue;
                    }

                    wAsms.AddRange(Directory.GetFiles(Path.Combine(WEAVR_PATH, kvp.Key), "*.asmdef", SearchOption.AllDirectories));
                }
            }
            return wAsms.ToArray();
        }

        private static void UpdateAsmDefs()
        {
            foreach (var wCurrentAsmDef in GetAsms())
            {
                var name = _weavrBuildUtility.GetNameFromAsmDef(wCurrentAsmDef);

                var assembly = CompilationPipeline.GetAssemblies().FirstOrDefault(x => x.name == name);

                if (assembly == null)
                {
                    assembly = CompilationPipeline.GetAssemblies(AssembliesType.Editor).FirstOrDefault(x => x.name == name);
                }

                if (assembly == null)
                {
                    assembly = CompilationPipeline.GetAssemblies(AssembliesType.Player).FirstOrDefault(x => x.name == name);
                }

                string[] wSourceFiles = null;
                if (assembly == null)
                {
                    wSourceFiles = GetFilesFromAsmdef(wCurrentAsmDef);
                }
                else
                {
                    wSourceFiles = assembly.sourceFiles;
                }

                if (wSourceFiles != null && wSourceFiles.Length > 0)
                {
                    mAsmDefScripts.Add(wCurrentAsmDef, (assembly, wSourceFiles));
                    foreach (var path in wSourceFiles)
                    {
                        mAsmDefByPath.Add(path, name);
                    }
                }
            }
        }

        private static bool HasReferences(string iFile)
        {
            string iName = Path.GetFileNameWithoutExtension(iFile);
            string iExtension = Path.GetExtension(iFile);
            return (iExtension == ".prefab" || iExtension == ".asset" || iExtension == ".unity")
                        && iName.ToLower() != "lightingdata";
        }

        private static void CopyResources(string iBuildPath)
        {
            // Copy of tech guides
            var pdfList = Directory.EnumerateFiles(WEAVR_PATH)
                                .Where(file => file.ToLower().EndsWith("pdf") || file.ToLower().EndsWith("md"))
                                .ToList();

            foreach (var pdf in pdfList)
            {
                string fileName = Path.GetFileName(pdf);

                File.Copy(Path.Combine(WEAVR_PATH, fileName), Path.Combine(iBuildPath, fileName));
                File.Copy(Path.Combine(WEAVR_PATH, $"{fileName}.meta"), Path.Combine(iBuildPath, $"{fileName}.meta"));
            }

            File.Copy(Path.Combine(WEAVR_PATH, "ThirdParty.meta"), Path.Combine(iBuildPath, "ThirdParty.meta"));

            foreach (var (folder, wildcard) in FOLDERS_TO_COPY)
            {
                var path = Path.Combine(WEAVR_PATH, folder);

                if (!Directory.Exists(path))
                {
                    Debug.LogError($"Path [{path}] not found.");
                    continue;
                }

                // Create Folder
                Directory.CreateDirectory(Path.Combine(iBuildPath, folder));

                // Copy .meta
                File.Copy($"{path}.meta", Path.Combine(iBuildPath, $"{folder}.meta"));

                // Get all the folders and copy
                {
                    var foldersString = Directory.GetDirectories(path, wildcard, SearchOption.AllDirectories);
                    foreach (var folderString in foldersString)
                    {
                        var relativePath = folderString.Replace(WEAVR_PATH + Path.DirectorySeparatorChar, "");

                        Directory.CreateDirectory(Path.Combine(iBuildPath, relativePath));
                    }
                }

                // Get all the files and copy
                {
                    var filesString = Directory.GetFiles(path, wildcard, SearchOption.AllDirectories);
                    foreach (var fileString in filesString)
                    {
                        var relativePath = fileString.Replace(WEAVR_PATH + Path.DirectorySeparatorChar, "");

                        File.Copy(fileString, Path.Combine(iBuildPath, relativePath), true);
                    }
                }
            }

            foreach (var keyValuePair in MODULES)
            {
                var path = Path.Combine(WEAVR_PATH, keyValuePair.Key);

                if (!Directory.Exists(path))
                {
                    Debug.LogError($"Path [{path}] not found.");
                    continue;
                }

                // Copy the meta of the module
                File.Copy($"{path}.meta", Path.Combine(iBuildPath, $"{keyValuePair.Key}.meta"));

                // Get all the folders in the modules and copy them (only folder, no files)
                {
                    var foldersString = Directory.GetDirectories(path, "*.*", SearchOption.AllDirectories);
                    foreach (var folderString in foldersString)
                    {
                        var relativePath = folderString.Replace(WEAVR_PATH + Path.DirectorySeparatorChar, "");
                        Directory.CreateDirectory(Path.Combine(iBuildPath, relativePath));
                    }
                }

                // Get all the files in the modules and copy
                {
                    var filesString = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                    foreach (var fileString in filesString)
                    {
                        var relativePath = fileString.Replace(WEAVR_PATH + Path.DirectorySeparatorChar, "");

                        // Copy the files not in module children
                        if (!IsToSkip(relativePath, keyValuePair.Key))
                        {
                            File.Copy(fileString, Path.Combine(iBuildPath, relativePath), true);
                        }

                        // The prefab needs the change of guid inside
                        if (HasReferences(fileString))
                        {
                            UpdateScriptReferences(fileString, relativePath, iBuildPath);
                        }
                    }
                }
            }
        }

        private static string GetRelativePath(string iProjectPath)
        {
            return iProjectPath.Replace('/', '\\').Replace(WEAVR_PATH + Path.DirectorySeparatorChar, "");
        }

        private static string ProjectPathToBuild(string iProjectPath)
        {
            return Path.Combine(sBuildPath, GetRelativePath(iProjectPath));
        }

        private static bool IsDirectoryEmpty(string iDir)
        {
            return !Directory.EnumerateFileSystemEntries(iDir).Any();
        }

        private static string GetCorrespondingMeta(string iFile)
        {
            return iFile + ".meta";
        }

        private static void RemoveEmptyDirs(string iDir)
        {
            if (IsDirectoryEmpty(iDir))
            {
                Directory.Delete(iDir);
                string wMeta = GetCorrespondingMeta(iDir);
                if (File.Exists(wMeta))
                {
                    File.Delete(wMeta);
                }
            }
            else
            {
                foreach (var wSubDir in Directory.GetDirectories(iDir))
                {
                    RemoveEmptyDirs(wSubDir);
                }
                if (IsDirectoryEmpty(iDir))
                {
                    Directory.Delete(iDir);
                    string wMeta = GetCorrespondingMeta(iDir);
                    if (File.Exists(wMeta))
                    {
                        File.Delete(wMeta);
                    }
                }
            }
        }


        private static void CleanBuild()
        {
            RemoveEmptyDirs(sBuildPath);
        }

        private static void LogReferences(Assembly assembly, int depth)
        {
            if (assembly == null)
            {
                return;
            }

            Debug.Log("------");
            Debug.Log(assembly.name);
            Debug.Log("allReferences");
            foreach (var a in assembly.allReferences)
            {
                Debug.Log(a);
            }
            Debug.Log("compiledAssemblyReferences");
            foreach (var a in assembly.compiledAssemblyReferences)
            {
                Debug.Log(a);
            }
            depth--;
            if (depth > 0)
            {
                foreach (var a in assembly.assemblyReferences)
                {
                    LogReferences(a, depth);
                }
            }
            Debug.Log("------");
        }

        private static void CompileAsm(string iBuildPath)
        {
            foreach (var wAsmKeyValue in mAsmDefScripts)
            {
                if (!string.IsNullOrEmpty(wAsmKeyValue.Key))
                {
                    Debug.Log("Building ASM " + wAsmKeyValue.Key);
                    //LogReferences(wAsmKeyValue.Value.assembly, 1);

                    var name = _weavrBuildUtility.GetNameFromAsmDef(wAsmKeyValue.Key);
                    var wScriptAssembliesDllPath = Path.Combine(SCRIPT_ASSEMBLIES, $"{name}.dll");
                    var wAsmPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(name).Replace("\\", "/");
                    var dllPath = Path.Combine(Directory.GetParent(Path.GetDirectoryName(ProjectPathToBuild(wAsmPath))).ToString(), $"{name}.dll");

                    if (name.ToUpper().StartsWith("WEAVR.ESSENTIAL.RUNTIME.REFLECTION"))
                    {
                        wScriptAssembliesDllPath = Path.Combine(SCRIPT_ASSEMBLIES, "WEAVR.Essential.Runtime.Reflection.dll");
                    }
                    else if (name.StartsWith("WEAVR.EXTENSIONS"))
                    {
                        wScriptAssembliesDllPath = Path.Combine(SCRIPT_ASSEMBLIES, "WEAVR.Extensions.Common.dll");
                    }

                    if (File.Exists(wScriptAssembliesDllPath))
                    {
                        try
                        {
                            Debug.Log("Change name of " + wScriptAssembliesDllPath + " to " + $"{wScriptAssembliesDllPath}.old");
                            File.Move(wScriptAssembliesDllPath, $"{wScriptAssembliesDllPath}.old");

                            BuildDllAndMeta(name, dllPath, wAsmKeyValue.Key, wAsmKeyValue.Value.assembly, wAsmKeyValue.Value.scripts);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                        finally
                        {
                            Debug.Log("Change name of " + $"{wScriptAssembliesDllPath}.old" + " to " + wScriptAssembliesDllPath);
                            File.Move($"{wScriptAssembliesDllPath}.old", wScriptAssembliesDllPath);
                        }
                    }
                }
            }
        }

        private static void RemoveIncompatibleDlls()
        {
            foreach (var wIncompatibleDll in cIncompatibleDlls)
            {
                foreach (var wMatch in Directory.GetFiles(SCRIPT_ASSEMBLIES, wIncompatibleDll, SearchOption.TopDirectoryOnly))
                {
                    Debug.Log("Removing incompatible DLL " + wMatch);
                    File.Delete(wMatch);
                }
            }
        }

        private static string GetBuildPath()
        {
            //function to retrieve build path for future config files etc
            //for now just return a stub value
            return "C:\\WEAVRBuilds\\WEAVR";
        }

        public static IEnumerator CreateBuild()
        {
            mFilePathByGuid = new Dictionary<string, string>();
            mScriptOrderByFilePath = new Dictionary<string, string>();
            mAsmDefByPath = new Dictionary<string, string>();
            mAsmDefScripts = new Dictionary<string, (Assembly, string[])>();

            _weavrBuildUtility = new WeavrBuildUtility();

            Debug.Log("START BUILD");
            yield return null;

            // Create directory for build
            string dateTimeOfRelease = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            sBuildPath = GetBuildPath();
            if (Directory.Exists(sBuildPath))
            {
                Directory.Delete(sBuildPath, true);
                Debug.Log($"Deleted folder in {sBuildPath}");
            }
            Directory.CreateDirectory(sBuildPath);

            Debug.Log($"Create build folder in {sBuildPath}");
            yield return null;
            using (File.Create(Path.Combine(sBuildPath, $"RELEASED_ ON_{dateTimeOfRelease}"))) { }

            // Copy .meta of WEAVR folder
            File.Copy(Path.Combine(WEAVR_PATH, "..", $"{WEAVR_FOLDER_NAME}.meta"), Path.Combine(sBuildPath, "..", $"{WEAVR_FOLDER_NAME}.meta"), true);
            //RemoveIncompatibleDlls();
            UpdateMetas();
            UpdateAsmDefs();
            CopyResources(sBuildPath);
            CompileAsm(sBuildPath);
            Obfuscate(sBuildPath);
            CleanBuild();
            Debug.Log("END BUILD");

            yield return 1;
        }

        private static string GetAutomaticBuildPath()
        {
            //function to retrieve build path for future config files etc
            //for now just return a stub value
            return Path.Combine(Application.dataPath, "..", OUTPUT_FOLDER);
        }

        public static void CreateBuildSync(string buildPath)
        {
            mFilePathByGuid = new Dictionary<string, string>();
            mScriptOrderByFilePath = new Dictionary<string, string>();
            mAsmDefByPath = new Dictionary<string, string>();
            mAsmDefScripts = new Dictionary<string, (Assembly, string[])>();

            _weavrBuildUtility = new WeavrBuildUtility();

            Debug.Log("START BUILD");

            // Create directory for build
            string dateTimeOfRelease = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            if (buildPath == null)
            {
                sBuildPath = GetAutomaticBuildPath();
            }
            else
            {
                sBuildPath = buildPath;
            }
            if (Directory.Exists(sBuildPath))
            {
                Directory.Delete(sBuildPath, true);
                Debug.Log($"Deleted folder in {sBuildPath}");
            }
            Directory.CreateDirectory(sBuildPath);

            Debug.Log($"Create build folder in {sBuildPath}");
            using (File.Create(Path.Combine(sBuildPath, $"RELEASED_ ON_{dateTimeOfRelease}"))) { }
            using (var sw = File.CreateText(Path.Combine(sBuildPath, $"RELEASED_ ON_{dateTimeOfRelease}.meta")))
            {
                sw.Write(RELEASED_ON_META);
            }

            // Copy .meta of WEAVR folder
            File.Copy(Path.Combine(WEAVR_PATH, "..", $"{WEAVR_FOLDER_NAME}.meta"), Path.Combine(sBuildPath, "..", $"{WEAVR_FOLDER_NAME}.meta"), true);

            //RemoveIncompatibleDlls();
            UpdateMetas();
            UpdateAsmDefs();
            CopyResources(sBuildPath);
            CompileAsm(sBuildPath);
            Obfuscate(sBuildPath);
            CleanBuild();

            Debug.Log("END BUILD");
        }

        private static string ReactorFile = Path.Combine("Eziriz", ".NET Reactor", "dotNET_Reactor.exe");
        private static void Obfuscate(string sBuildPath)
        {
            var programPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), ReactorFile);
            if (!File.Exists(programPath))
            {
                programPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), ReactorFile);
                if (!File.Exists(programPath))
                {
                    Debug.LogWarning("No Obfuscation");
                    return;
                }
            }

            Directory.CreateDirectory(Path.Combine(sBuildPath, "..", "Secured"));
            var processInfo = new System.Diagnostics.ProcessStartInfo(Path.Combine(WEAVR_PATH, "NetReactor", "Obfuscate.bat"),
                "\"" + Path.GetDirectoryName(EditorApplication.applicationPath) + "\""// Path of editor
                + " \"" + sBuildPath.Remove(sBuildPath.LastIndexOf('\\')) + "\"" // Path of WEAVR
                + " \"" + programPath + "\"" // .Net Reactor
                );

            processInfo.CreateNoWindow = false;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;

            var process = System.Diagnostics.Process.Start(processInfo);

            process.OutputDataReceived += (object sender, System.Diagnostics.DataReceivedEventArgs e) => Debug.Log("output>>" + e.Data);
            process.BeginOutputReadLine();

            process.ErrorDataReceived += (object sender, System.Diagnostics.DataReceivedEventArgs e) => Debug.LogError("error>>" + e.Data);
            process.BeginErrorReadLine();

            process.WaitForExit();

            Debug.Log($"ExitCode: {process.ExitCode}");
            process.Close();
        }

        private static void UpdateScriptReferences(string fileString, string relativePath, string buildPath)
        {

            string line;

            using (StreamReader file = File.OpenText(fileString))
            using (StreamWriter sw = new StreamWriter(Path.Combine(buildPath, relativePath)))
            {
                while ((line = file.ReadLine()) != null)
                {
                    if (line.Contains("m_Script: {fileID: "))
                    {
                        // Match with a guid without dash
                        var match = Regex.Match(line, @"[0-9a-f]{8}[-]?[0-9a-f]{4}[-]?[0-9a-f]{4}[-]?[0-9a-f]{4}[-]?[0-9a-f]{12}");
                        if (match.Success)
                        {
                            string filePath;
                            // if the guid is found in some meta
                            if (mFilePathByGuid.TryGetValue(match.Value, out filePath))
                            {
                                // Check if the new script is inside some dll
                                string nameAsmDef;
                                if (mAsmDefByPath.TryGetValue(filePath, out nameAsmDef))
                                {
                                    line = line.Replace(match.Value, _weavrBuildUtility.GenerateGuidFromString(nameAsmDef).ToString("N"));

                                    var monoscript = AssetDatabase.LoadAssetAtPath<MonoScript>(filePath.Replace("\\", "/"));
                                    if (monoscript != null)
                                    {
                                        if (monoscript.GetClass() != null)
                                        {
                                            int startIndex = line.IndexOf("{fileID: ");
                                            int endIndex = line.IndexOf(", guid: ", startIndex);
                                            var fileId = line.Substring(startIndex, endIndex - startIndex);

                                            int newFileId = FileIDUtil.Compute(monoscript.GetClass());

                                            line = line.Replace(fileId, "{fileID: " + newFileId);
                                        }
                                        else
                                        {
                                            Debug.LogWarning($"Prefab [{relativePath}]: File path [{filePath}] found in [{nameAsmDef}] but monoscript.GetClass() not found.");
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogWarning($"Prefab [{relativePath}]: File path [{filePath}] found in [{nameAsmDef}] but monoscript not found.");
                                    }
                                }
                            }
                        }
                    }

                    sw.WriteLine(line);
                }
            }
        }

        private static void BuildDllAndMeta(string name, string dllPath, string fileAsmdef, Assembly iAssembly, string[] iScripts)
        {

            List<string> includePlatforms;
            List<string> excludePlatforms;
            List<string> defineConstraints;

            using (StreamReader file = File.OpenText(fileAsmdef))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                JObject o = (JObject)JToken.ReadFrom(reader);

                includePlatforms = o["includePlatforms"]?.Select(x => (string)x).ToList();
                excludePlatforms = o["excludePlatforms"]?.Select(x => (string)x).ToList();
                defineConstraints = o["defineConstraints"]?.Select(x => (string)x).ToList();
            }

            AssemblyBuilder assemblyBuilder = new AssemblyBuilder(dllPath, iScripts);
            var useEngineModules = false;
            if (iAssembly != null)
            {
                var assemblyDefines = defineConstraints != null ? iAssembly.defines?.Concat(defineConstraints).ToArray() : iAssembly.defines;

                if (MODULE_SYMBOLS.TryGetValue(iAssembly.name, out string[] symbols))
                {
                    assemblyDefines = assemblyDefines.Concat(symbols).ToArray();
                    if (defineConstraints != null)
                    {
                        defineConstraints.AddRange(symbols);
                    }
                    else
                    {
                        defineConstraints = new List<string>(symbols);
                    }
                }

                assemblyBuilder.additionalDefines = assemblyDefines.Concat(new string[] { "WEAVR_DLL" }).ToArray();

                foreach (var a in iAssembly.compiledAssemblyReferences)
                {
                    if (a.EndsWith("UnityEngine.dll"))
                    {
                        useEngineModules = true;
                        break;
                    }
                }
            }

            if (MODULE_REFERENCES_EXCLUDES.TryGetValue(name, out string[] excludedRefs) && excludedRefs?.Length > 0)
            {
                var referenceToExclude = new HashSet<string>();
                foreach (var excludedRef in excludedRefs)
                {
                    var founds = assemblyBuilder.defaultReferences.Where(x => x.ToUpper().Contains(excludedRef.ToUpper())).ToList();
                    foreach (var f in founds)
                    {
                        if (!string.IsNullOrWhiteSpace(f))
                        {
                            referenceToExclude.Add(f);
                        }
                    }
                }
                assemblyBuilder.excludeReferences = assemblyBuilder.excludeReferences?.Concat(referenceToExclude.ToList()).ToArray() ?? referenceToExclude.ToArray();
            }

            //if(iAssembly.assemblyReferences?.Any(x => x.name == "WeavrEssential.Runtime.Reflection") == true)
            //{
            //    assemblyBuilder.additionalReferences = assemblyBuilder.additionalReferences?.Concat(new string[] { "WeavrEssential.Runtime.ReflectionAOT" }).ToArray() ?? new string[] { "WeavrEssential.Runtime.ReflectionAOT" };
            //}

            if (name.ToUpper().Contains("EDITOR") || name.ToUpper().Contains("CREATOR")
                || name.ToUpper().Contains("PACKAGING"))
            {
                assemblyBuilder.flags = AssemblyBuilderFlags.EditorAssembly;
            }
            else
            {
                assemblyBuilder.flags = AssemblyBuilderFlags.None;
            }


            if (useEngineModules)
            {
                assemblyBuilder.referencesOptions = ReferencesOptions.UseEngineModules;
            }

            if (includePlatforms.Count() > 0)
            {
                var found = false;
                foreach (var platform in includePlatforms)
                {
                    foreach (var supportedPlatform in SUPPORT_PLATFORMS)
                    {
                        if (platform != supportedPlatform.Platform)
                        {
                            continue;
                        }

                        assemblyBuilder.buildTargetGroup = supportedPlatform.BuildPair.BuildTargetGroup;
                        assemblyBuilder.buildTarget = supportedPlatform.BuildPair.BuildTarget;

                        found = true;
                        break;
                    }

                    if (found)
                    {
                        break;
                    }
                }

                // TODO check for found
            }
            else if (excludePlatforms.Count() > 0)
            {
                var found = false;
                foreach (var platform in excludePlatforms)
                {
                    foreach (var supportedPlatform in SUPPORT_PLATFORMS)
                    {
                        if (platform == supportedPlatform.Platform)
                        {
                            continue;
                        }

                        assemblyBuilder.buildTargetGroup = supportedPlatform.BuildPair.BuildTargetGroup;
                        assemblyBuilder.buildTarget = supportedPlatform.BuildPair.BuildTarget;

                        found = true;
                        break;
                    }

                    if (found)
                    {
                        break;
                    }
                }
            }
            // Use the first
            else
            {
                assemblyBuilder.buildTargetGroup = SUPPORT_PLATFORMS[0].BuildPair.BuildTargetGroup;
                assemblyBuilder.buildTarget = SUPPORT_PLATFORMS[0].BuildPair.BuildTarget;
            }

            // Called on main thread
            //assemblyBuilder.buildStarted += delegate (string assemblyPath)
            //{
            //    Debug.Log($"Assembly build started for {assemblyPath}");
            //};

            // Called on main thread
            assemblyBuilder.buildFinished += delegate (string assemblyPath, CompilerMessage[] compilerMessages)
            {
                var errorCount = compilerMessages.Count(m => m.type == CompilerMessageType.Error);
                var warningCount = compilerMessages.Count(m => m.type == CompilerMessageType.Warning);

                File.Delete($"{assemblyPath}.mdb");
                EditorUtility.UnloadUnusedAssetsImmediate();
                Debug.Log($"Assembly build finished for {assemblyPath} with Warnings: {warningCount} - Errors: {errorCount}");

                foreach (var message in compilerMessages)
                {
                    if (message.type == CompilerMessageType.Warning)
                    {
                        Debug.LogWarning($"CompilerMessage for {assemblyPath}: type {message.type} message {message.message}");
                    }
                    else
                    {
                        Debug.LogError($"CompilerMessage for {assemblyPath}: type {message.type} message {message.message}");
                    }
                }

                if (errorCount > 0)
                {
                    throw new Exception($"Assembly build finished for {assemblyPath} with Warnings: {warningCount} - Errors: {errorCount}");
                }
            };

            if (!assemblyBuilder.Build())
            {
                Debug.LogError($"Failed to start build of assembly {assemblyBuilder.assemblyPath}!");
                throw new Exception($"Failed to start build of assembly {assemblyBuilder.assemblyPath}!");
            }


            while (assemblyBuilder.status != AssemblyBuilderStatus.Finished)
                System.Threading.Thread.Sleep(1000);

            string wPdbFile = dllPath.Substring(0, dllPath.Length - "dll".Length) + "pdb";
            if (File.Exists(wPdbFile))
            {
                File.Delete(wPdbFile);
            }
            using (StreamWriter sw = new StreamWriter($"{dllPath}.meta"))
            {
                sw.Write(GenerateParametricDllMeta(name, includePlatforms, excludePlatforms, defineConstraints));
            }

        }

        //private static void UpdateDllMeta(string name, string dllPath, Dictionary<string, string> scriptOrderByFilePathInternal)
        //{

        //    using (StreamWriter sw = new StreamWriter($"{dllPath}.meta"))
        //    {
        //        if (name.ToLower().Contains(".editor"))
        //        {
        //            sw.Write(DLL_META_EDITOR
        //                .Replace("${GUID}", _weavrBuildUtility.GenerateGuidFromString(name).ToString("N"))
        //                .Replace("${EXECUTION_ORDER}", GenerateExecutionOrder(scriptOrderByFilePathInternal)));
        //        }
        //        else
        //        {
        //            sw.Write(DLL_META_ANY
        //                .Replace("${GUID}", _weavrBuildUtility.GenerateGuidFromString(name).ToString("N"))
        //                .Replace("${EXECUTION_ORDER}", GenerateExecutionOrder(scriptOrderByFilePathInternal)));
        //        }
        //    }
        //}

        private static string GenerateExecutionOrder(Dictionary<string, string> scriptOrderByFilePathInternal)
        {
            if (scriptOrderByFilePathInternal == null || scriptOrderByFilePathInternal.Keys.Count == 0)
            {
                return "{}";
            }

            StringBuilder sb = new StringBuilder("");
            foreach (var entry in scriptOrderByFilePathInternal)
            {
                var monoscript = AssetDatabase.LoadAssetAtPath<MonoScript>(entry.Key.Replace("\\", "/"));
                if (monoscript != null)
                {
                    if (monoscript.GetClass() != null)
                    {
                        sb.Append(@"
    ");
                        sb.Append(monoscript.GetClass().Namespace);
                        sb.Append(".");
                        sb.Append(monoscript.GetClass().Name);
                        sb.Append(": ");
                        sb.Append(entry.Value);
                    }
                    else
                    {
                        Debug.LogWarning($"GenerateExecutionOrder [{entry.Key}]: File path [{entry.Key}] found in [{entry.Key}] but monoscript.GetClass() not found.");
                    }
                }
            }

            if (string.IsNullOrEmpty(sb.ToString()))
            {
                sb.Append("{}");
            }

            return sb.ToString();
        }

        private static string GetDefineConstraints(List<string> defineConstraints)
        {
            if (defineConstraints == null || defineConstraints.Count == 0)
            {
                return " []";
            }

            StringBuilder sb = new StringBuilder("");

            foreach (var c in defineConstraints)
            {
                sb.Append(@"
  - ");
                sb.Append(c);
            }

            return sb.ToString();
        }

        private static string GenerateParametricDllMeta(string name, List<string> includePlatforms, List<string> excludePlatforms, List<string> defineConstraints)
        {
            string returnValue = "";

            returnValue = DLL_META_PARAMETRIC
                .Replace("${GUID}", _weavrBuildUtility.GenerateGuidFromString(name).ToString("N"))
                .Replace("${EXECUTION_ORDER}", GenerateExecutionOrder(null))
                .Replace("${DEFINE_CONSTRAINTS}", GetDefineConstraints(defineConstraints));

            if (includePlatforms.Count > 0)
            {
                foreach (var platform in PLATFORMS)
                {
                    if (includePlatforms.Contains(platform))
                    {
                        returnValue = returnValue.Replace("${EXCLUDE_" + platform + "}", "0");
                        returnValue = returnValue.Replace("${ENABLED_" + platform + "}", "1");
                    }
                    else
                    {
                        returnValue = returnValue.Replace("${EXCLUDE_" + platform + "}", "1");
                        returnValue = returnValue.Replace("${ENABLED_" + platform + "}", "0");
                    }

                }
                returnValue = returnValue.Replace("${ENABLED_ANY}", "0");
            }
            else if (excludePlatforms.Count > 0)
            {
                foreach (var platform in PLATFORMS)
                {
                    if (excludePlatforms.Contains(platform))
                    {
                        returnValue = returnValue.Replace("${EXCLUDE_" + platform + "}", "1");
                        returnValue = returnValue.Replace("${ENABLED_" + platform + "}", "0");
                    }
                    else
                    {
                        returnValue = returnValue.Replace("${EXCLUDE_" + platform + "}", "0");
                        returnValue = returnValue.Replace("${ENABLED_" + platform + "}", "1");
                    }

                }
                returnValue = returnValue.Replace("${ENABLED_ANY}", "1");
            }
            else
            {
                // Exclude only OSXIntel and OSXIntel64
                foreach (var platform in PLATFORMS)
                {
                    returnValue = returnValue.Replace("${EXCLUDE_" + platform + "}", "0");
                    returnValue = returnValue.Replace("${ENABLED_" + platform + "}", "1");
                }

                returnValue = returnValue.Replace("${ENABLED_ANY}", "0");
            }

            return returnValue;
        }

        private static bool IsToSkip(string relativePath, string value)
        {
            foreach (var skip in FOLDERS_TO_SKIP)
            {
                if (relativePath.StartsWith(skip))
                {
                    return true;
                }
            }

            foreach (var skip in EXTENSIONS_TO_SKIP)
            {
                if (relativePath.EndsWith(skip))
                {
                    return true;
                }
            }

            foreach (var skip in FILES_TO_SKIP)
            {
                if (relativePath.Equals(skip))
                {
                    return true;
                }
            }

            foreach (var wAsmKeyValue in mAsmDefScripts)
            {
                if (Path.GetExtension(relativePath) == ".cs" || relativePath.EndsWith(".cs.meta"))
                {
                    if (relativePath.StartsWith(GetRelativePath(Path.GetDirectoryName(wAsmKeyValue.Key))))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // -------------- TEMPLATE FOR .META FILES

        private static readonly string RELEASED_ON_META = @"fileFormatVersion: 2
guid: 2c9ec8f2ea0527b4298b3730f9677762
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
";

        private static readonly string DLL_META_PARAMETRIC = @"fileFormatVersion: 2
guid: ${GUID}
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: ${EXECUTION_ORDER}
  defineConstraints:${DEFINE_CONSTRAINTS}
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
  - first:
      '': Any
    second:
      enabled: 0
      settings:
        Exclude Android: ${EXCLUDE_Android}
        Exclude Editor: ${EXCLUDE_Editor}
        Exclude Linux64: ${EXCLUDE_LinuxStandalone64}
        Exclude OSXIntel: ${EXCLUDE_macOSStandalone}
        Exclude OSXIntel64: ${EXCLUDE_macOSStandalone}
        Exclude OSXUniversal: ${EXCLUDE_macOSStandalone}
        Exclude Win: ${EXCLUDE_WindowsStandalone32}
        Exclude Win64: ${EXCLUDE_WindowsStandalone64}
        Exclude WindowsStoreApps: ${EXCLUDE_WSA}
        Exclude iOS: ${EXCLUDE_iOS}
  - first:
      Android: Android
    second:
      enabled: ${ENABLED_Android}
      settings:
        CPU: ARMv7
  - first:
      Any: 
    second:
      enabled: ${ENABLED_ANY}
      settings: {}
  - first:
      Editor: Editor
    second:
      enabled: ${ENABLED_Editor}
      settings:
        CPU: AnyCPU
        DefaultValueInitialized: true
        OS: AnyOS
  - first:
      Facebook: Win
    second:
      enabled: 0
      settings:
        CPU: AnyCPU
  - first:
      Facebook: Win64
    second:
      enabled: 0
      settings:
        CPU: AnyCPU
  - first:
      Standalone: Linux64
    second:
      enabled: ${ENABLED_LinuxStandalone64}
      settings:
        CPU: AnyCPU
  - first:
      Standalone: OSXIntel
    second:
      enabled: ${ENABLED_macOSStandalone}
      settings:
        CPU: AnyCPU
  - first:
      Standalone: OSXIntel64
    second:
      enabled: ${ENABLED_macOSStandalone}
      settings:
        CPU: AnyCPU
  - first:
      Standalone: OSXUniversal
    second:
      enabled: ${ENABLED_macOSStandalone}
      settings:
        CPU: AnyCPU
  - first:
      Standalone: Win
    second:
      enabled: ${ENABLED_WindowsStandalone32}
      settings:
        CPU: AnyCPU
  - first:
      Standalone: Win64
    second:
      enabled: ${ENABLED_WindowsStandalone64}
      settings:
        CPU: AnyCPU
  - first:
      Windows Store Apps: WindowsStoreApps
    second:
      enabled: ${ENABLED_WSA}
      settings:
        CPU: AnyCPU
        DontProcess: false
        PlaceholderPath: 
        SDK: AnySDK
        ScriptingBackend: AnyScriptingBackend
  - first:
      iPhone: iOS
    second:
      enabled: ${ENABLED_iOS}
      settings:
        AddToEmbeddedBinaries: false
        CPU: AnyCPU
        CompileFlags: 
        FrameworkDependencies: 
  userData: 
  assetBundleName: 
  assetBundleVariant: 
";

    }

}