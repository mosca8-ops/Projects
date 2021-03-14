using System;
using TXT.WEAVR.Core;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Editor
{
    public class UniqueIDFinderWindow : EditorWindow
    {
#if !WEAVR_DLL && WEAVR_INTERNAL_USE
        [MenuItem("WEAVR/Utilities/Find UniqueID", priority = 5)]
#endif
        public static void ShowWindow()
        {
            UniqueIDFinderWindow wnd = GetWindow<UniqueIDFinderWindow>();
            wnd.titleContent = new GUIContent("Find UniqueID");
        }

        [NonSerialized]
        private GUIStyle m_noResultStyle;

        private string m_idToFind;
        private bool m_noResult;

        private void OnGUI()
        {
            InitializeIfNeeded();

            GUILayout.Space(15f);

            EditorGUILayout.LabelField("UniqueID to find :");
            m_idToFind = EditorGUILayout.TextField(m_idToFind);

            EditorGUILayout.Space();
            if (GUILayout.Button("Find"))
            {
                m_noResult = false;
                Find(m_idToFind);
            }
            EditorGUILayout.Space();
            if (m_noResult)
            {
                EditorGUILayout.LabelField("No UniqueID Found", m_noResultStyle);
            }
            EditorGUILayout.Space();
        }

        private void Find(string id)
        {
            var idToFind = id.Replace(" ", "");
            var goWithID = IDBookkeeper.Get(idToFind);
            if (goWithID != null)
            {
                Selection.activeGameObject = goWithID;
                return;
            }
            m_noResult = true;
        }

        private void InitializeIfNeeded()
        {
            if (m_noResultStyle == null)
            {
                m_noResultStyle = new GUIStyle();
                m_noResultStyle.alignment = TextAnchor.MiddleCenter;
                m_noResultStyle.fontSize = 13;
                m_noResultStyle.normal.textColor = Color.red;
            }
        }
    }
}
