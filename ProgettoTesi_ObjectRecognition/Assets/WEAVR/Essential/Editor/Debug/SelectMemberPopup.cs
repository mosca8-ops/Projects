using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;


namespace TXT.WEAVR.Debugging
{

    public class SelectMemberPopup : EditorWindow
    {
        private class Styles : BaseStyles
        {
            public GUIStyle buttonLeft;
            public GUIStyle buttonMid;
            public GUIStyle buttonRight;

            private int fontSize = 11;
            private RectOffset padding = new RectOffset(8, 8, 6, 6);

            protected override void InitializeStyles(bool isProSkin) {
                buttonLeft = new GUIStyle(EditorStyles.miniButtonLeft) {
                    fontSize = fontSize,
                    fontStyle = FontStyle.Bold,
                    padding = padding,
                };
                buttonMid = new GUIStyle(EditorStyles.miniButtonMid) {
                    fontSize = fontSize,
                    fontStyle = FontStyle.Bold,
                    padding = padding,
                };
                buttonRight = new GUIStyle(EditorStyles.miniButtonRight) {
                    fontSize = fontSize,
                    fontStyle = FontStyle.Bold,
                    padding = padding,
                };
            }
        }

        private static Styles s_styles = new Styles();

        private List<MemberInfoWrapper> m_events;
        private List<MemberInfoWrapper> m_fields;
        private List<MemberInfoWrapper> m_properties;
        private List<MemberInfoWrapper> m_methods;

        private object m_target;

        private Action<MemberInfo> m_onAddCallback;
        private Func<MemberInfo, bool> m_filterCallback;
        
        private List<MemberInfoWrapper> m_selectedList;

        private Column[] m_columns;

        private Vector2 m_scrollPosition;

        public static void ShowAsPopup(Rect rect, object target, Func<MemberInfo, bool> filterCallback, Action<MemberInfo> onAddCallback)
        {
            GetWindow<SelectMemberPopup>(true).Initialize(target, filterCallback, onAddCallback).ShowAuxWindow(/*rect, new Vector2(300, 300)*/);
        }

        private SelectMemberPopup Initialize(object target, Func<MemberInfo, bool> filterCallback, Action<MemberInfo> onAddCallback) {
            m_events = new List<MemberInfoWrapper>();
            m_fields = new List<MemberInfoWrapper>();
            m_properties = new List<MemberInfoWrapper>();
            m_methods = new List<MemberInfoWrapper>();

            m_target = target;

            m_onAddCallback = onAddCallback;
            m_filterCallback = filterCallback ?? (m => true);

            FillUpData();

            return this;
        }

        private void FillUpData() {
            foreach(var memberInfo in m_target.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                if (!m_filterCallback(memberInfo) || memberInfo.GetCustomAttribute<ObsoleteAttribute>() != null) { continue; }
                if (memberInfo.IsEvent()) {
                    m_events.Add(new MemberInfoWrapper(memberInfo));
                }
                else if(memberInfo is FieldInfo) {
                    m_fields.Add(new MemberInfoWrapper(memberInfo));
                }
                else if(memberInfo is PropertyInfo) {
                    m_properties.Add(new MemberInfoWrapper(memberInfo));
                }
                else if(IsMethod(memberInfo)) {
                    m_methods.Add(new MemberInfoWrapper(memberInfo));
                }
            }
            
            m_selectedList = m_fields;
            m_columns = new[]
            {
                new Column("Declaring Type", 0.2f),
                new Column("Member Name", 0.3f),
                new Column("Member Type", 0.3f),
                new Column("Is Public", 0.1f),
                new Column("Action", 0.1f)
            };
        }

        private bool IsMethod(MemberInfo info)
        {
            return info is MethodInfo 
                && ((MethodInfo)info).ReturnType != typeof(void) 
                && ((MethodInfo)info).GetParameters().Length == 0 
                && !((MethodInfo)info).IsGenericMethod
                && !info.Name.StartsWith("get_");
        }

        private void OnGUI()
        {
            s_styles.Refresh();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(m_selectedList == m_fields, "Fields", s_styles.buttonLeft))
            {
                m_selectedList = m_fields;
            }
            if (GUILayout.Toggle(m_selectedList == m_properties, "Properties", s_styles.buttonMid))
            {
                m_selectedList = m_properties;
            }
            if (GUILayout.Toggle(m_selectedList == m_methods, "Methods", s_styles.buttonMid))
            {
                m_selectedList = m_methods;
            }
            if (GUILayout.Toggle(m_selectedList == m_events, "Events", s_styles.buttonRight))
            {
                m_selectedList = m_events;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();

            if (Event.current.type == EventType.Layout)
            {
                float width = position.width - 20;
                for (int i = 0; i < m_columns.Length; i++)
                {
                    m_columns[i].UpdateWidth(width);
                }
            }

            DrawHeader(m_columns);
            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
            for(int i = 0; i < m_selectedList.Count; i++)
            {
                DrawElement(m_selectedList[i], m_columns);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader(Column[] columns)
        {
            EditorGUILayout.BeginHorizontal("Box");
            for (int i = 0; i < columns.Length; i++)
            {
                EditorGUILayout.LabelField(columns[i].title, EditorStyles.boldLabel, GUILayout.Width(columns[i].width));
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawElement(MemberInfoWrapper wrapper, Column[] columns)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(wrapper.memberInfo.DeclaringType.Name, GUILayout.Width(columns[0].width));
            EditorGUILayout.LabelField(wrapper.niceName, GUILayout.Width(columns[1].width));
            EditorGUILayout.LabelField(wrapper.memberType.Name, GUILayout.Width(columns[2].width));
            GUILayout.Toggle(wrapper.isPublic, GUIContent.none, GUILayout.Width(columns[3].width));

            var wasColor = GUI.color;
            GUI.color = Color.green;
            if(GUILayout.Button("+ Add"))
            {
                m_onAddCallback(wrapper.memberInfo);
                if (!m_filterCallback(wrapper.memberInfo))
                {
                    m_selectedList.Remove(wrapper);
                    Repaint();
                }
            }
            GUI.color = wasColor;
            EditorGUILayout.EndHorizontal();
        }

        private struct Column
        {
            public string title;
            public float width;
            public float widthRatio;

            public Column(string title, float widthRatio)
            {
                this.title = title;
                this.width = 0;
                this.widthRatio = widthRatio;
            }

            public void UpdateWidth(float tableWidth)
            {
                width = widthRatio * tableWidth;
            }
        }

        private struct MemberInfoWrapper
        {
            public MemberInfo memberInfo;
            public string memberName;
            public string niceName;
            public Type memberType;
            public bool isPublic;

            public MemberInfoWrapper(MemberInfo info) {
                memberInfo = info;
                memberName = info.Name;
                niceName = DebugUtility.NicifyName(info.Name);
                memberType = info.GetMemberType();
                isPublic = info.IsPublic();
            }
        }
    }
}
