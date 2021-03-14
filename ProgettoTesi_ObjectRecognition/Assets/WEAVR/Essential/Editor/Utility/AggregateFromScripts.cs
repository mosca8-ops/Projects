using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Utility
{

    public class AggregateFromScripts : EditorWindow
    {
#if !WEAVR_DLL && WEAVR_INTERNAL_USE
        [MenuItem("WEAVR/Utilities/Aggregate In Scripts", priority = 5)]
#endif
        static void ShowAggregateWindow()
        {
            GetWindow<AggregateFromScripts>().Show();
        }

        [SerializeField]
        DefaultAsset m_folder;
        [SerializeField]
        string m_regexPattern;
        [SerializeField]
        string m_lineOutput;
        [SerializeField]
        bool m_useRegex;
        string m_fullOutput;
        string m_message;

        Vector2 m_scrollPosition;
        bool m_elaborating;

        private void OnEnable()
        {
            m_message = null;
            titleContent = new GUIContent("Aggregate From Scripts");
        }

        private void OnGUI()
        {
            m_folder = EditorGUILayout.ObjectField("Folder", m_folder, typeof(DefaultAsset), false) as DefaultAsset;
            m_regexPattern = EditorGUILayout.TextField("Regex Pattern", m_regexPattern);
            using (new GUILayout.HorizontalScope())
            {
                m_lineOutput = EditorGUILayout.TextField("Line Output", m_lineOutput);
                m_useRegex = EditorGUILayout.ToggleLeft("Regex", m_useRegex, GUILayout.Width(60));
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label(m_message);
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(!m_folder || m_elaborating))
            {
                if (GUILayout.Button("Execute"))
                {
                    ExecuteAggregation(AssetDatabase.GetAssetPath(m_folder));
                }
            }
            GUILayout.EndHorizontal();
            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition, "Box", GUILayout.ExpandWidth(false));
            GUILayout.Label("Lines", EditorStyles.boldLabel);
            GUILayout.Space(8);
            EditorGUILayout.TextArea(m_fullOutput, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private async void ExecuteAggregation(string path)
        {
            m_message = "Aggregating... Please wait";
            m_elaborating = true;
            try
            {
                bool useRegex = m_useRegex;
                await Task.Run(() =>
                {
                    var scripts = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
                    m_message = $"Aggregating {scripts.Length} files. Please wait..";
                    Regex regex = new Regex(m_regexPattern);
                    StringBuilder sb = new StringBuilder();
                    int successFiles = 0;
                    int linesAggregated = 0;
                    foreach (var script in scripts)
                    {
                        var matches = regex.Matches(File.ReadAllText(script));
                        if (matches?.Count > 0)
                        {
                            successFiles++;
                            foreach (Match match in matches)
                            {
                                if (match.Success)
                                {
                                    linesAggregated++;
                                    string line = m_lineOutput;
                                    for (int i = 0; i < match.Groups.Count; i++)
                                    {
                                        line = line.Replace("$" + i, match.Groups[i].Value);
                                    }
                                    if (useRegex)
                                    {
                                        sb.AppendLine(line.Replace("\\t", "\t").Replace("\\n", "\n"));
                                    }
                                    else
                                    {
                                        sb.AppendLine(line);
                                    }
                                }
                            }
                        }
                    }

                    m_fullOutput = sb.ToString();
                    m_message = $"aggregated {linesAggregated} lines in {successFiles} files";
                });
            }
            finally
            {
                m_elaborating = false;
            }
        }
    }
}