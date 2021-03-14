using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TXT.WEAVR.Core;
using TXT.WEAVR.License;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;


namespace TXT.WEAVR.Builder
{

    public class WeavrUnityFilesConverterFromDLL : EditorWindow
    {
        private static readonly Dictionary<string, string> TO_CONVERT = new Dictionary<string, string>()
        {
            { "Procedures", Path.Combine("Assets", "Procedures") },
            { "Scenes", Path.Combine("Assets", "Scenes") },
        };

        private static readonly List<string> EXTENSIONS_TO_SEARCH = new List<string>() { "unity", "asset", "prefab" };

        private static readonly string OUTPUT_FOLDER = "WEAVR_Converted_Unity_Files_From_DLL";

        private static readonly string WEAVR_FOLDER_NAME = "WEAVR";
        private static readonly string WEAVR_PATH = Path.Combine("Assets", WEAVR_FOLDER_NAME);

        private static Dictionary<string, Dictionary<int, (long localId, string guid)>> s_monoGuids = new Dictionary<string, Dictionary<int, (long localId, string guid)>>();

        private static WeavrBuildUtility _weavrBuildUtility;

        [NonSerialized]
        private Dictionary<string, List<FileMeta>> m_filesMeta = null;
        private string m_status;
        private EditorCoroutine m_coroutine;
        private Vector2 m_scrollPos;
        private GUIContent m_elemContent = new GUIContent();
        private DefaultAsset m_addFolder;
        private string m_lastBuildPath;

        [Flags]
        private enum FileStatus
        {
            NotConverted = 0,
            Skipped = 1,
            IsConverting = 2,
            Faulted = 4,
            Converted = 8,
        }

        private class FileMeta
        {
            public bool toBeConverted;
            public string fullPath;
            public string copiedPath;
            public int replacedIds;
            public Exception exception;
            public List<(string line, string error)> faultedLines = new List<(string line, string error)>();
            public FileStatus fileStatus;
            public string destinationFolder;
            public bool expanded;
        }

        [MenuItem("WEAVR/Builder/Convert Unity Files From DLL", priority = 50)]
        public static void ShowWindow()
        {
            //var m_coroutine = EditorCoroutine.StartCoroutine(ConvertObjects());
            GetWindow<WeavrUnityFilesConverterFromDLL>().Show();
        }

        private void OnEnable()
        {
            if (!WeavrLE.IsValid())
            {
                DestroyImmediate(this);
                return;
            }

            minSize = new Vector2(600, 800);
        }

        private void Update()
        {
            Repaint();
        }

        private void OnGUI()
        {
            if (m_filesMeta == null)
            {
                m_filesMeta = new Dictionary<string, List<FileMeta>>();
                foreach (var toConvert in TO_CONVERT)
                {
                    AddFilesFromFolder(toConvert.Key, toConvert.Value);
                }
            }

            // Header
            EditorGUILayout.BeginHorizontal("GroupBox");
            GUILayout.FlexibleSpace();
            GUILayout.Label("CONVERT FILES FROM DLL", EditorStyles.largeLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            m_addFolder = EditorGUILayout.ObjectField("Add Folder", m_addFolder, typeof(DefaultAsset), false) as DefaultAsset;
            if (m_addFolder)
            {
                // Get Folder
                var localDirectory = AssetDatabase.GetAssetPath(m_addFolder);
                var directory = Path.GetDirectoryName(Path.Combine(Application.dataPath.Replace("Assets", string.Empty), localDirectory));
                if (Directory.Exists(directory))
                {
                    var relative = (localDirectory.Replace("Assets/", string.Empty), localDirectory);
                    AddFilesFromFolder(relative.Item1, relative.Item2);
                }
                m_addFolder = null;
            }

            // Central part
            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos, "GroupBox");
            foreach (var group in m_filesMeta)
            {
                DrawGroup(group.Key, group.Value);
            }

            EditorGUILayout.EndScrollView();

            // Footer
            EditorGUILayout.BeginHorizontal("GroupBox");
            if (!string.IsNullOrEmpty(m_status))
            {
                GUILayout.Label("STATUS:");
                GUILayout.FlexibleSpace();
                GUILayout.Label(m_status, EditorStyles.boldLabel);
            }
            GUILayout.FlexibleSpace();
            if (m_coroutine != null)
            {
                if (GUILayout.Button("Cancel"))
                {
                    EditorCoroutine.StopCoroutine(m_coroutine);
                    m_coroutine = null;
                }
            }
            else
            {
                if (GUILayout.Button("CONVERT"))
                {
                    m_lastBuildPath = null;
                    foreach (var pair in m_filesMeta)
                    {
                        foreach (var meta in pair.Value)
                        {
                            meta.exception = null;
                            meta.faultedLines.Clear();
                            meta.fileStatus = FileStatus.NotConverted;
                            meta.copiedPath = null;
                            meta.expanded = false;
                            meta.replacedIds = 0;
                        }
                    }
                    m_coroutine = EditorCoroutine.StartCoroutine(ConvertObjects());
                }

                if (GUILayout.Button("CLEAR"))
                {
                    m_lastBuildPath = null;
                    m_filesMeta = null;
                }
            }
            if(!string.IsNullOrEmpty(m_lastBuildPath) && GUILayout.Button("OPEN FOLDER"))
            {
                EditorUtility.RevealInFinder(m_lastBuildPath);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void AddFilesFromFolder(string key, string value)
        {
            var metaList = new List<FileMeta>();
            m_filesMeta[key] = metaList;
            foreach (var extension in EXTENSIONS_TO_SEARCH)
            {
                metaList.AddRange(Directory.GetFiles(value, $"*.{extension}", SearchOption.AllDirectories).Select(f => new FileMeta()
                {
                    toBeConverted = true,
                    fullPath = f,
                    copiedPath = string.Empty,
                    replacedIds = 0,
                    exception = null,
                    fileStatus = FileStatus.NotConverted,
                    destinationFolder = f.Replace(Application.dataPath, string.Empty)
                }));
            }
        }

        private void DrawGroup(string group, List<FileMeta> metas)
        {
            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label(group, EditorStyles.boldLabel);
            foreach (var meta in metas)
            {
                DrawElement(meta);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawElement(FileMeta meta)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            meta.toBeConverted = GUILayout.Toggle(meta.toBeConverted, GUIContent.none, GUILayout.Width(20));
            if (meta.toBeConverted)
            {
                var color = GUI.contentColor;
                switch (meta.fileStatus)
                {
                    case FileStatus.Converted:
                        GUI.contentColor = Color.green;
                        break;
                    case FileStatus.IsConverting:
                        GUI.contentColor = Color.blue;
                        break;
                    case FileStatus.Faulted:
                        GUI.contentColor = Color.red;
                        break;
                }

                m_elemContent.text = meta.fullPath;
                m_elemContent.tooltip = string.IsNullOrEmpty(meta.copiedPath) ? null : "Copied to " + meta.copiedPath;

                if (meta.faultedLines.Count > 0)
                {
                    meta.expanded = EditorGUILayout.Foldout(meta.expanded, m_elemContent, true);
                }
                else
                {
                    GUILayout.Label(m_elemContent);
                }
                GUILayout.FlexibleSpace();
                if (meta.fileStatus == FileStatus.Faulted || meta.fileStatus == FileStatus.Converted)
                {
                    GUILayout.Label($"{meta.replacedIds} lines converted", EditorStyles.centeredGreyMiniLabel);
                }
                if (meta.exception != null)
                {
                    m_elemContent.text = "ERROR";
                    m_elemContent.tooltip = meta.exception.Message;
                    GUILayout.Label(m_elemContent, EditorStyles.boldLabel);
                }
                GUI.contentColor = color;
            }
            else
            {
                GUI.enabled = false;
                GUILayout.Label(meta.fullPath);
                GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();
            if (meta.expanded && meta.faultedLines.Count > 0)
            {
                var contentColor = GUI.contentColor;
                EditorGUILayout.BeginVertical("Box");
                foreach (var line in meta.faultedLines)
                {
                    EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                    GUILayout.Label(line.line);
                    GUI.contentColor = Color.red;
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(line.error);
                    GUI.contentColor = contentColor;
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
        }

        private IEnumerator ConvertObjects()
        {
            s_monoGuids = new Dictionary<string, Dictionary<int, (long localId, string guid)>>();

            _weavrBuildUtility = new WeavrBuildUtility();

            WeavrDebug.Log("ConversionFromDLL", (m_status = "START CONVERTING UNITY FILES FROM DLL"));
            yield return null;

            // Get all meta
            {
                var filesCSharp = Directory.GetFiles(Path.Combine(WEAVR_PATH), "*.cs", SearchOption.AllDirectories);
                foreach (var fileCSharp in filesCSharp)
                {
                    if (fileCSharp.Contains("WEAVR\\ThirdParty") || fileCSharp.Contains("WEAVR/ThirdParty"))
                    {
                        continue;
                    }

                    var localPath = fileCSharp.StartsWith("Assets") ? fileCSharp : FileUtil.GetProjectRelativePath(fileCSharp);
                    var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(localPath);

                    if (monoScript && monoScript.GetClass() != null)
                    {
                        var asmGuid = _weavrBuildUtility.GenerateGuidFromString(monoScript.GetClass().Assembly.GetName().Name).ToString().Replace("-", "");
                        if (!s_monoGuids.TryGetValue(asmGuid, out Dictionary<int, (long, string)> guids))
                        {
                            guids = new Dictionary<int, (long, string)>();
                            s_monoGuids[asmGuid] = guids;
                        }
                        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(monoScript, out string guid, out long localId))
                        {
                            int monoId = FileIDUtil.Compute(monoScript.GetClass());
                            guids[monoId] = (localId, guid);
                        }
                    }
                }
            }
            yield return null;

            // Create directory for build
            string dateTimeOfRelease = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

            string buildPath = Path.Combine(OUTPUT_FOLDER, dateTimeOfRelease);

            foreach (var pair in m_filesMeta)
            {
                string buildPathInternal = Path.Combine(buildPath, pair.Key);

                foreach (var metaFile in pair.Value)
                {
                    if (!metaFile.toBeConverted)
                    {
                        metaFile.fileStatus = FileStatus.Skipped;
                        continue;
                    }

                    var fileName = Path.GetFileName(metaFile.fullPath);

                    var buildPathFile = Path.GetDirectoryName(Path.Combine(buildPath, metaFile.destinationFolder));

                    if (!Directory.Exists(buildPathFile))
                    {
                        try
                        {
                            Directory.CreateDirectory(buildPathFile);
                            var fileToCopy = Path.GetDirectoryName(metaFile.fullPath) + ".meta";
                            var fileDestination = buildPathFile + ".meta";
                            while (File.Exists(fileToCopy) && !File.Exists(fileDestination))
                            {
                                File.Copy(fileToCopy, fileDestination);
                                fileToCopy = Directory.GetParent(Path.GetDirectoryName(fileToCopy)).FullName + ".meta";
                                fileDestination = Directory.GetParent(Path.GetDirectoryName(fileDestination)).FullName + ".meta";
                            }
                        }
                        catch (Exception ex)
                        {
                            WeavrDebug.LogException("ConversionFromDLL", ex);
                        }
                    }

                    try
                    {
                        // Copy the meta of the scene
                        File.Copy($"{metaFile.fullPath}.meta", Path.Combine(buildPathFile, $"{fileName}.meta"));

                        ManageUnityFile(metaFile, fileName, buildPathFile);
                    }
                    catch (Exception e)
                    {
                        WeavrDebug.Log("ConversionFromDLL", $"{metaFile.fullPath} unable to process: Cause: {e.Message}");
                        metaFile.fileStatus = FileStatus.Faulted;
                        metaFile.exception = e;
                    }
                    yield return null;
                }
                yield return null;
            }

            m_lastBuildPath = buildPath;
            WeavrDebug.Log("ConversionFromDLL", (m_status = "END CONVERTING UNITY FILES FROM DLL"));

            yield return 1;

            m_coroutine = null;
        }

        private static void ManageUnityFile(FileMeta fileMeta, string relativePath, string buildPath)
        {
            string fileString = fileMeta.fullPath;
            string line;
            int lineNumber = 0;

            using (StreamReader file = File.OpenText(fileString))
            using (StreamWriter sw = new StreamWriter(Path.Combine(buildPath, relativePath)))
            {
                while ((line = file.ReadLine()) != null)
                {
                    lineNumber++;
                    if (line.Contains("m_Script: {fileID: ") && !line.Contains(@"m_Script: {fileID: 11500000"))
                    {
                        // Match with a guid without dash
                        // Example Line: m_Script: {fileID: -2073950428, guid: 7e36c52d89b800215139e5013a9b64e4, type: 3}
                        var match = Regex.Match(line, @"m_Script: *{ *fileID: *(-?\d*) *, *guid: *([\d\w-]*) *,");
                        if (match.Success)
                        {
                            string filePath = fileString;
                            // if the guid is found in some meta
                            if (s_monoGuids.TryGetValue(match.Groups[2].Value, out Dictionary<int, (long, string)> guids))
                            {
                                if (int.TryParse(match.Groups[1].Value, out int id))
                                {
                                    if (guids.TryGetValue(id, out (long localId, string guid) mono))
                                    {
                                        line = Regex.Replace(line, @"fileID: (-?\d*) *,", $"fileID: {mono.localId},");
                                        line = Regex.Replace(line, @"guid: [\w\d]* *,", $"guid: {mono.guid},");
                                        fileMeta.replacedIds++;
                                    }
                                    else
                                    {
                                        WeavrDebug.LogWarning("ConversionFromDLL", $"Asset [{relativePath}]: Unable to get the file specified by id {id}");
                                        fileMeta.fileStatus = FileStatus.Faulted;
                                        fileMeta.faultedLines.Add(($"{lineNumber}: {line}", $"Unable to get the file specified by id {id}"));
                                    }
                                }
                                else
                                {
                                    WeavrDebug.LogWarning("ConversionFromDLL", $"Asset [{relativePath}]: Unable to parse local file id {match.Groups[1].Value}.");
                                    fileMeta.fileStatus = FileStatus.Faulted;
                                    fileMeta.faultedLines.Add(($"{lineNumber}: {line}", $"Unable to parse local file id {match.Groups[1].Value}"));
                                }
                            }
                            else
                            {
                                WeavrDebug.LogWarning("ConversionFromDLL", $"Asset [{relativePath}]: Unable to find asmdef with guid: {match.Groups[2].Value}");
                                fileMeta.fileStatus = FileStatus.Faulted;
                                fileMeta.faultedLines.Add(($"{lineNumber}: {line}", $"Unable to find asmdef with guid: {match.Groups[2].Value}"));
                            }
                        }
                    }

                    sw.WriteLine(line);

                    if (fileMeta.fileStatus != FileStatus.Faulted)
                    {
                        fileMeta.fileStatus = FileStatus.Converted;
                    }

                    fileMeta.copiedPath = Path.Combine(buildPath, relativePath);
                }
            }
        }
    }
}