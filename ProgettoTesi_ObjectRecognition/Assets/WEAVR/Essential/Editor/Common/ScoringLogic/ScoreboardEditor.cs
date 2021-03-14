using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [CustomEditor(typeof(Scoreboard))]
    public class ScoreboardEditor : UnityEditor.Editor
    {
        private Vector2 m_scrollPosition;
        private Scoreboard m_scoreboard;
        private Action m_preAction;

        private string m_newName;
        private float m_newScore;

        private void OnEnable()
        {
            m_scoreboard = target as Scoreboard;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if(m_preAction != null)
            {
                m_preAction();
                m_preAction = null;
            }

            if (m_scoreboard.Scores != null && m_scoreboard.Scores.scores != null && m_scoreboard.Scores.scores.Length > 0)
            {
                GUILayout.Space(10);
                m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition, "Box");

                for (int i = 0; i < m_scoreboard.Scores.scores.Length; i++)
                {
                    DrawItem(i, m_scoreboard.Scores.scores[i]);
                }

                EditorGUILayout.EndScrollView();
            }

            GUILayout.Space(10);

            EditorGUILayout.BeginVertical("Box");
            m_newName = EditorGUILayout.TextField("Player Name", m_newName);
            m_newScore = EditorGUILayout.FloatField("Score", m_newScore);
            var guiEnabled = GUI.enabled;
            GUI.enabled = !string.IsNullOrEmpty(m_newName);
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Append score", EditorStyles.miniButton))
            {
                var list = m_scoreboard.Scores.scores.ToList();
                list.Add(new Scoreboard.ScoreItemJSON()
                {
                    name = m_newName,
                    score = m_newScore,
                    Date = DateTime.Now,
                });
                m_scoreboard.Scores.scores = list.ToArray();
                m_newScore = 0;
                m_newName = string.Empty;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUI.enabled = guiEnabled;
            EditorGUILayout.EndVertical();

            GUILayout.Space(20);

            EditorGUILayout.BeginHorizontal("Box");
            if(GUILayout.Button("Load from file"))
            {
                m_scoreboard.ReloadFromFile();
            }
            if(GUILayout.Button("Save to file"))
            {
                m_scoreboard.Save();
            }
            EditorGUILayout.EndHorizontal();
        }

        public void DrawItem(int index, Scoreboard.ScoreItemJSON item)
        {
            EditorGUILayout.BeginHorizontal("Box");
            GUILayout.Label((index + 1).ToString(), GUILayout.Width(40));
            GUILayout.Space(10);
            GUILayout.Label(item.name);
            GUILayout.FlexibleSpace();
            GUILayout.Label(item.score.ToString(), GUILayout.Width(40));
            GUILayout.Space(10);
            GUILayout.Label(item.Date.ToString(), GUILayout.Width(140));
            GUILayout.Space(10);
            if(GUILayout.Button("X", EditorStyles.miniButton))
            {
                m_preAction = () => DeleteItem(index);
            }
            EditorGUILayout.EndHorizontal();
        }

        public void DeleteItem(int index)
        {
            if (index < 0 && index >= m_scoreboard.Scores.scores.Length) { return; }
            var list = m_scoreboard.Scores.scores.ToList();
            list.RemoveAt(index);
            m_scoreboard.Scores.scores = list.ToArray();
            if (Application.isPlaying)
            {
                m_scoreboard.Refresh();
            }
        }
    }
}
