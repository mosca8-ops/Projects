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

    public class WeavrUnityFilesConverterToDLL : MonoBehaviour
    {
        private static readonly Dictionary<string, string> TO_CONVERT = new Dictionary<string, string>()
        {
            { "Procedures", Path.Combine("Assets", "Procedures") },
            { "Scenes", Path.Combine("Assets", "Scenes") },
        };

        private static readonly List<string> EXTENSIONS_TO_SEARCH = new List<string>() { "unity", "asset", "prefab" };

        private static readonly string OUTPUT_FOLDER = "WEAVR_Converted_Unity_Files";

        private static readonly string WEAVR_FOLDER_NAME = "WEAVR";
        private static readonly string WEAVR_PATH = Path.Combine("Assets", WEAVR_FOLDER_NAME);

        private static Dictionary<string, string> filePathByGuid;
        private static Dictionary<string, string> asmDefByPath;

        private static WeavrBuildUtility _weavrBuildUtility;

        [MenuItem("WEAVR/Builder/Convert Unity Files To DLL", priority = 50)]
        public static void ShowWindow()
        {
            var m_coroutine = EditorCoroutine.StartCoroutine(ConvertScenes());
        }

        private void OnEnable()
        {
            if (!WeavrLE.IsValid())
            {
                DestroyImmediate(this);
                return;
            }
        }

        private static IEnumerator ConvertScenes()
        {
            filePathByGuid = new Dictionary<string, string>();
            asmDefByPath = new Dictionary<string, string>();

            _weavrBuildUtility = new WeavrBuildUtility();

            Debug.Log("START CONVERTING UNITY FILES TO DLL");
            yield return null;

            // Get all meta
            {
                var filesMeta = Directory.GetFiles(Path.Combine(WEAVR_PATH), "*.meta", SearchOption.AllDirectories);
                foreach (var fileMeta in filesMeta)
                {
                    if (fileMeta.Contains("WEAVR\\ThirdParty") || fileMeta.Contains("WEAVR/ThirdParty"))
                    {
                        continue;
                    }

                    using (StreamReader file = File.OpenText(fileMeta))
                    {
                        string filePath = null;

                        string line;
                        while ((line = file.ReadLine()) != null)
                        {
                            if (filePath == null && line.Contains("guid: "))
                            {
                                var match = Regex.Match(line, @"[0-9a-f]{8}[-]?[0-9a-f]{4}[-]?[0-9a-f]{4}[-]?[0-9a-f]{4}[-]?[0-9a-f]{12}");
                                if (match.Success)
                                {
                                    filePath = fileMeta.Replace("\\", "/").Replace(".meta", "");
                                    filePathByGuid.Add(match.Value, filePath);
                                }
                            }
                        }
                    }
                }
            }
            yield return null;

            // Get all asmdef
            {
                var filesAsmDef = Directory.GetFiles(Path.Combine(WEAVR_PATH), "*.asmdef", SearchOption.AllDirectories);
                foreach (var fileAsmDef in filesAsmDef)
                {
                    if (fileAsmDef.Contains("WEAVR\\ThirdParty") || fileAsmDef.Contains("WEAVR/ThirdParty"))
                    {
                        continue;
                    }

                    var name = _weavrBuildUtility.GetNameFromAsmDef(fileAsmDef);

                    var assembly = CompilationPipeline.GetAssemblies().FirstOrDefault(x => x.name == name);

                    if (assembly == null)
                    {
                        assembly = CompilationPipeline.GetAssemblies(AssembliesType.Editor).FirstOrDefault(x => x.name == name);
                    }

                    if (assembly == null)
                    {
                        assembly = CompilationPipeline.GetAssemblies(AssembliesType.Player).FirstOrDefault(x => x.name == name);
                    }

                    if (assembly != null)
                    {
                        foreach (var path in assembly.sourceFiles)
                        {
                            asmDefByPath.Add(path, name);
                        }
                    }
                }
            }
            yield return null;


            // Create directory for build
            string dateTimeOfRelease = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

            string buildPath = Path.Combine(OUTPUT_FOLDER, dateTimeOfRelease);

            foreach (var toConvert in TO_CONVERT)
            {
                string buildPathInternal = Path.Combine(buildPath, toConvert.Key);

                Directory.CreateDirectory(buildPathInternal);

                // Copy .meta of toConvert folder
                File.Copy(Path.Combine(WEAVR_PATH, "..", $"{toConvert.Key}.meta"), Path.Combine(buildPathInternal, "..", $"{toConvert.Key}.meta"));

                Debug.Log($"Create converted {toConvert.Key} folder in {buildPathInternal}");
                yield return null;

                List<string> unityFiles = new List<string>();
                foreach (var extension in EXTENSIONS_TO_SEARCH)
                {
                    unityFiles.AddRange(Directory.GetFiles(toConvert.Value, $"*.{extension}", SearchOption.AllDirectories));
                }

                // For each Value
                foreach (var unityFile in unityFiles)
                {
                    var fileName = Path.GetFileName(unityFile);

                    // Copy the meta of the scene
                    File.Copy($"{unityFile}.meta", Path.Combine(buildPathInternal, $"{fileName}.meta"));

                    ManageUnityFile(unityFile, fileName, buildPathInternal);
                }
                yield return null;
            }

            Debug.Log("END CONVERTING UNITY FILES TO DLL");

            yield return 1;
        }

        private static void ManageUnityFile(string fileString, string relativePath, string buildPath)
        {

            string line;

            using (StreamReader file = File.OpenText(fileString))
            using (StreamWriter sw = new StreamWriter(Path.Combine(buildPath, relativePath)))
            {
                while ((line = file.ReadLine()) != null)
                {
                    if (line.Contains("{fileID: "))
                    {
                        // Match with a guid without dash
                        var match = Regex.Match(line, @"[0-9a-f]{8}[-]?[0-9a-f]{4}[-]?[0-9a-f]{4}[-]?[0-9a-f]{4}[-]?[0-9a-f]{12}");
                        if (match.Success)
                        {
                            string filePath;
                            // if the guid is found in some meta
                            if (filePathByGuid.TryGetValue(match.Value, out filePath))
                            {
                                // Check if the new script is inside some dll
                                string nameAsmDef;
                                if (asmDefByPath.TryGetValue(filePath, out nameAsmDef))
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
                                            Debug.LogWarning($"Scene [{relativePath}]: File path [{filePath}] found in [{nameAsmDef}] but monoscript.GetClass() not found.");
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogWarning($"Scene [{relativePath}]: File path [{filePath}] found in [{nameAsmDef}] but monoscript not found.");
                                    }
                                }
                            }
                        }
                    }

                    sw.WriteLine(line);
                }
            }
        }
    }
}