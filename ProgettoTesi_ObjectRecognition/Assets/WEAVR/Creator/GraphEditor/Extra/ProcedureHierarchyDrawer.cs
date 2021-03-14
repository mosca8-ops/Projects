using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class ProcedureHierarchyDrawer
    {
        private class Styles : BaseStyles
        {
            public GUIStyle link;
            public GUIStyle testing;
            public GUIStyle testingBackground;

            public float testingWidth;

            public GUIContent willPlay;
            public GUIContent playing;
            public GUIContent pause;

            protected override void InitializeStyles(bool isProSkin)
            {
                link = WeavrStyles.EditorSkin2.FindStyle("hierarchy_Link");
                testing = WeavrStyles.EditorSkin2.FindStyle("hierarchy_Testing");
                testingBackground = WeavrStyles.EditorSkin2.FindStyle("hierarchy_TestingBackground");

                willPlay = new GUIContent(" Will play", WeavrStyles.Icons["play_square_grey"]);
                playing = new GUIContent(" Playing", WeavrStyles.Icons["play_square"]);
                pause = new GUIContent(" Paused", WeavrStyles.Icons["pause_square"]);
            }
        }

        private static Styles s_styles;
        private static Styles Style
        {
            get
            {
                if(s_styles == null)
                {
                    s_styles = new Styles();
                    s_styles.Refresh();
                }
                return s_styles;
            }
        }

        private ProcedureRunner m_runner;
        private GameObject m_runnerGO;
        private ReferenceTable m_table;
        private Procedure m_procedure;

        public Procedure Procedure
        {
            get => m_procedure;
            set
            {
                if(m_procedure != value)
                {
                    m_procedure = value;
                    if (m_procedure && m_procedure.Graph)
                    {
                        m_table = m_procedure.Graph.ReferencesTable;
                        Style.willPlay.text = m_procedure.name;
                        Style.playing.text = m_procedure.name;
                        Style.pause.text = m_procedure.name;

                        Style.testingWidth = Style.testing.CalcSize(Style.willPlay).x;
                    }
                    else
                    {
                        m_table = null;
                    }
                }
            }
        }

        public ProcedureRunner ProcedureRunner
        {
            get => m_runner;
            set
            {
                if(m_runner != value)
                {
                    m_runner = value;
                    if (m_runner)
                    {
                        m_runnerGO = m_runner.gameObject;
                    }
                    else
                    {
                        m_runnerGO = null;
                    }
                }
            }
        }

        public void Prepare()
        {
            Style?.Refresh();
        }

        public void DrawProcedureHierarchyElement(int instanceID, Rect rect, ProcedureRunner procedureTester)
        {
            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (!gameObject) { return; }
            if (gameObject == m_runnerGO)
            {
                
                var content = Application.isPlaying ? Style.playing : Style.willPlay;
                var r = new Rect(rect.x + rect.width - Style.testingWidth - Style.testing.margin.right, 
                                    rect.y + Style.testing.margin.top, Style.testingWidth, rect.height);
                if (Selection.activeGameObject == gameObject)
                {
                    EditorGUI.DrawRect(r, WeavrStyles.Colors.selectionOpaque);
                }
                else
                {
                    EditorGUI.DrawRect(r, WeavrStyles.Colors.WindowBackground);
                    if (procedureTester && procedureTester.gameObject == gameObject)
                    {
                        Style.testingBackground.Draw(new Rect(rect.x - s_styles.testingBackground.margin.left,
                                                              rect.y - s_styles.testingBackground.margin.top,
                                                              rect.width + s_styles.testingBackground.margin.horizontal,
                                                              rect.height + s_styles.testingBackground.margin.vertical),
                                                              false, false, false, false);
                    }
                }
                Style.testing.Draw(r, content, false, false, Application.isPlaying, false);
            }
            else if(m_table != null && m_table.HasGameObject(gameObject))
            {
                rect.x = rect.x + rect.width - Style.link.fixedWidth - Style.link.margin.right;
                rect.y += Style.link.margin.top;
                rect.width = Style.link.fixedWidth;
                Style.link.Draw(rect, false, false, gameObject.scene.path != Procedure.ScenePath, false);
            }
        }

        private static void FallbackTestDraw(Rect rect)
        {
            Color color = Color.green;
            color.a = 0.05f;
            EditorGUI.DrawRect(rect, color);
            rect.x += rect.width - 40;
            rect.width = 40;
            GUI.Label(rect, "TEST", WeavrStyles.GreenLeftBoldLabel);
        }
    }
}
