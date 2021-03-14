using TXT.WEAVR.Interaction;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Maintenance
{

    [CustomEditor(typeof(AbstractSwitch), editorForChildClasses: true)]
    [CanEditMultipleObjects]
    public class AbstractSwitchEditor : InteractiveBehaviourEditor
    {

        protected override void DrawInspector(SerializedProperty currentProperty)
        {
            while (currentProperty.NextVisible(false) && currentProperty.name != "m_defaultLocalPosition")
            {
                EditorGUILayout.PropertyField(currentProperty, true);
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal("Box");

            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(120));
            GUILayout.Label("Default Pose", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save"))
            {
                foreach (AbstractSwitch @switch in targets)
                {
                    @switch.SaveDefaults();
                }
            }
            if (GUILayout.Button("Restore"))
            {
                foreach (AbstractSwitch @switch in targets)
                {
                    @switch.RestoreDefaults();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("Box");
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 60;
            bool wasEnabled = GUI.enabled;
            GUI.enabled = false;
            GUIContent content = new GUIContent("Position");
            EditorGUILayout.PropertyField(currentProperty, content);
            content.text = "Rotation";
            currentProperty.NextVisible(false);
            EditorGUILayout.PropertyField(currentProperty, content);
            GUI.enabled = wasEnabled;
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            base.DrawInspector(currentProperty);
        }
    }
}
