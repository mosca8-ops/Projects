using NSubstitute;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Editor
{

    class AssignScriptIcons : EditorWindow
    {
        private const string k_IconPattern = @"icon: *{.*}";
        private const string k_IconFormat = @"icon: {{fileID: {0}, guid: {1}, type: 3}}";

#if !WEAVR_DLL && WEAVR_INTERNAL_USE
        [MenuItem("WEAVR/Utilities/Assign Script Icons", priority = 6)]
#endif
        static void ShowWindow()
        {
            GetWindow<AssignScriptIcons>("Assign Script Icons");
        }

        private class IconGroup
        {
            public Guid id;
            public Texture2D icon;
            public DefaultAsset directory;
            public string directoryFullPath;
            public string directoryRelativePath;
            public string[] totalFiles;
            public List<ScriptFile> files = new List<ScriptFile>();
        }

        private struct ScriptFile
        {
            public GUIContent content;
            public string relativePath;
            public string fullPath;
            public Type type;
            public bool isMonoBehaviour;
            public bool isScriptableObject;

            public ScriptFile(string fullPath, string directoryFullPath)
            {
                this.fullPath = fullPath;
                relativePath = fullPath.Replace(directoryFullPath + Path.DirectorySeparatorChar, "");
                MonoScript monoscript = AssetDatabase.LoadAssetAtPath<MonoScript>(fullPath.Replace(Path.DirectorySeparatorChar, '/').Replace(Application.dataPath, "Assets"));
                if (monoscript)
                {
                    type = monoscript.GetClass();
                    isMonoBehaviour = typeof(MonoBehaviour).IsAssignableFrom(type);
                    isScriptableObject = typeof(ScriptableObject).IsAssignableFrom(type);
                    content = new GUIContent(relativePath, EditorGUIUtility.ObjectContent(monoscript, type)?.image);
                }
                else
                {
                    type = null;
                    content = new GUIContent(relativePath);
                    isMonoBehaviour = isScriptableObject = false;
                }
            }
        }

        private bool m_recurseSearch = true;
        private bool m_onlyUnityObjects = true;
        private Vector2 m_scrollPosition;
        private Color m_darkenColor = new Color(0, 0, 0, 0.2f);
        private DefaultAsset m_addFolder;
        private Dictionary<Guid, IconGroup> m_groups = new Dictionary<Guid, IconGroup>();
        private List<Guid> m_keysToRemove = new List<Guid>();

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            m_addFolder = EditorGUILayout.ObjectField("Add Folder", m_addFolder, typeof(DefaultAsset), false) as DefaultAsset;
            m_recurseSearch = EditorGUILayout.ToggleLeft("Recurse", m_recurseSearch, GUILayout.MaxWidth(80));
            m_onlyUnityObjects = EditorGUILayout.ToggleLeft("Unity Only", m_onlyUnityObjects, GUILayout.MaxWidth(80));
            EditorGUILayout.EndHorizontal();
            if (m_addFolder)
            {
                // Get Folder
                var localDirectory = AssetDatabase.GetAssetPath(m_addFolder);
                var directory = Path.Combine(Application.dataPath.Replace("Assets", string.Empty), localDirectory);
                if (Directory.Exists(directory))
                {
                    var relative = (localDirectory, directory);
                    AddIconGroupFromFolder(m_addFolder, relative.Item1, relative.Item2);
                }
                m_addFolder = null;
            }

            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

            foreach(var pair in m_groups)
            {
                DrawGroup(pair.Value);
            }

            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();

            foreach(var keyToRemove in m_keysToRemove)
            {
                m_groups.Remove(keyToRemove);
            }
            m_keysToRemove.Clear();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear"))
            {
                m_groups.Clear();
            }

            if (GUILayout.Button("Apply"))
            {
                ApplyIcons();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ApplyIcons()
        {
            bool requiresRecompile = false;
            var regex = new Regex(k_IconPattern);
            EditorApplication.LockReloadAssemblies();
            try
            {
                foreach (var pair in m_groups)
                {
                    if (pair.Value.icon && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(pair.Value.icon, out string guid, out long localId))
                    {
                        string replaceString = string.Format(k_IconFormat, localId, guid);
                        foreach (var file in pair.Value.files)
                        {
                            var metaFilepath = file.fullPath + ".meta";
                            if (File.Exists(metaFilepath))
                            {
                                var fileText = File.ReadAllText(metaFilepath);
                                fileText = regex.Replace(fileText, replaceString);
                                File.WriteAllText(metaFilepath, fileText);
                                requiresRecompile = true;
                            }
                        }
                    }
                }
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
            }

            if (requiresRecompile)
            {
                AssetDatabase.Refresh();
            }
        }

        private void DrawGroup(IconGroup group)
        {
            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUILayout.BeginHorizontal("Box");
            group.icon = EditorGUILayout.ObjectField(group.icon, typeof(Texture2D), false, GUILayout.Height(50), GUILayout.Width(50)) as Texture2D;
            EditorGUILayout.BeginVertical();
            GUILayout.Label(group.directoryRelativePath, EditorStyles.boldLabel);
            GUILayout.Label($"Total Files: {group.totalFiles.Length}", EditorStyles.miniLabel);
            GUILayout.Label($"Selected Files: {group.files.Count}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X"))
            {
                m_keysToRemove.Add(group.id);
            }
            EditorGUILayout.EndHorizontal();
            for (int i = 0; i < group.files.Count; i++)
            {
                if(i % 2 == 1)
                {
                    var rect = GUILayoutUtility.GetLastRect();
                    rect.y += rect.height;
                    EditorGUI.DrawRect(rect, m_darkenColor);
                }
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label((i + 1).ToString(), GUILayout.Width(24));
                GUILayout.Label(group.files[i].content, GUILayout.Height(16));
                GUILayout.FlexibleSpace();
                if (group.files[i].isMonoBehaviour)
                {
                    GUILayout.Label("MonoBehaviour", EditorStyles.centeredGreyMiniLabel, GUILayout.MaxWidth(80));
                }
                else if (group.files[i].isScriptableObject)
                {
                    GUILayout.Label("ScriptableObject", EditorStyles.centeredGreyMiniLabel, GUILayout.MaxWidth(100));
                }
                if (GUILayout.Button("-", EditorStyles.miniButton))
                {
                    group.files.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void AddIconGroupFromFolder(DefaultAsset folder, string relativePath, string fullPath)
        {
            //if(m_groups.TryGetValue(folder, out IconGroup group))
            //{
            //    group.totalFiles = Directory.GetFiles(fullPath,
            //                            "*.cs",
            //                            m_recurseSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            //        .Where(f => f.EndsWith(".cs")).ToArray();
            //}
            //else
            //{
                var group = new IconGroup()
                {
                    id = Guid.NewGuid(),
                    directory = folder,
                    directoryFullPath = fullPath,
                    directoryRelativePath = relativePath,
                    totalFiles = Directory.GetFiles(fullPath,
                                        "*.cs",
                                        m_recurseSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                    .Where(f => f.EndsWith(".cs")).ToArray()
                };
                m_groups[group.id] = group;
            //}

            group.files = group.totalFiles.Select(f => new ScriptFile(f, fullPath)).ToList();

            if (m_onlyUnityObjects)
            {
                group.files = group.files.Where(f => f.isMonoBehaviour || f.isScriptableObject).ToList();
            }
        }
    }
}