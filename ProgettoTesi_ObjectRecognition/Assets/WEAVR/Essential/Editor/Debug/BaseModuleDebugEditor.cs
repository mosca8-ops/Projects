using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Debugging
{
    [CustomEditor(typeof(BaseModuleDebug), true)]
    public class BaseModuleDebugEditor : UnityEditor.Editor
    {
        private bool m_firstPassDone;

        private void OnEnable()
        {
            m_firstPassDone = false;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            if (!serializedObject.FindProperty("m_initialized").boolValue)
            {
                m_firstPassDone = true;
                serializedObject.FindProperty("m_initialized").boolValue = true;
                CreateTexts();
                serializedObject.ApplyModifiedProperties();
            }
            else if (m_firstPassDone)
            {
                m_firstPassDone = false;
                SetTexts();
            }
        }

        private void CreateTexts()
        {
            BaseModuleDebug module = target as BaseModuleDebug;
            var existingDebugLines = new List<DebugLine>(module.GetComponentsInChildren<DebugLine>());
            if(existingDebugLines.Count == 0)
            {
                return;
            }
            List<FieldInfo> textProperties = new List<FieldInfo>();
            var fields = module.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach(var fieldInfo in fields)
            {
                if(fieldInfo.FieldType == typeof(Text) && fieldInfo.Name != "m_moduleTitle")
                {
                    textProperties.Add(fieldInfo);
                }
            }

            while(existingDebugLines.Count < textProperties.Count)
            {
                var newLine = Instantiate(existingDebugLines[0].gameObject) as GameObject;
                newLine.transform.SetParent(module.transform, false);
                existingDebugLines.Add(newLine.GetComponent<DebugLine>());
            }
        }

        private void SetTexts()
        {
            BaseModuleDebug module = target as BaseModuleDebug;
            var existingDebugLines = new List<DebugLine>(module.GetComponentsInChildren<DebugLine>());
            if (existingDebugLines.Count == 0)
            {
                return;
            }
            List<FieldInfo> textProperties = new List<FieldInfo>();
            var fields = module.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var fieldInfo in fields)
            {
                if (fieldInfo.FieldType == typeof(Text) && fieldInfo.Name != "m_moduleTitle")
                {
                    textProperties.Add(fieldInfo);
                }
            }

            for (int i = 0; i < textProperties.Count; i++)
            {
                existingDebugLines[i].gameObject.name = $"Line_{textProperties[i].Name}";
                existingDebugLines[i].SetLabelName(textProperties[i].Name + ":");
                textProperties[i].SetValue(module, existingDebugLines[i].value);
            }
        }
    }
}