using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine.UIElements;
using System;
using TXT.WEAVR.Editor;
using System.Linq;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(ActionsCatalogue))]
    public class ActionsCatalogueEditor : CatalogueEditor<ActionsCatalogue, ActionDescriptor, BaseAction>
    {
        protected override string AddDescriptorLabel()
        {
            return "Add Action";
        }

        protected override ActionDescriptor InstantiateDescriptor(Type type)
        {
            return ActionDescriptor.CreateDescriptor(type);
        }

        protected override void DrawDescriptor(DescriptorGroup parent, ActionDescriptor actionDescriptor)
        {
            EditorGUILayout.BeginVertical("Box");

            EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
            GUILayout.Label("Action", s_styles.descriptorType);
            EditorGUILayout.Space();
            GUILayout.Label(actionDescriptor.FullPath, s_styles.path);
            GUILayout.FlexibleSpace();
            var color = GUI.color;
            DrawUpDownArrows(parent, actionDescriptor);
            GUI.color = Color.red;
            if (GUILayout.Button("X"))
            {
                m_preRenderAction = () =>
                {
                    parent.Remove(actionDescriptor);
                    RefreshCatalogue();
                };
            }
            GUI.color = color;
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            float width = EditorGUIUtility.singleLineHeight * 3f;
            float height = EditorGUIUtility.singleLineHeight * 3;
            actionDescriptor.Icon = EditorGUILayout.ObjectField(actionDescriptor.Icon, typeof(Texture2D), false, GUILayout.Width(width), GUILayout.Height(height)) as Texture2D;

            EditorGUILayout.BeginVertical();

            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth -= width + EditorGUIUtility.standardVerticalSpacing * 2;
            var serObj = Get(actionDescriptor);
            serObj.Update();

            string sampleType = actionDescriptor.Sample ? actionDescriptor.Sample.GetType().Name : "NULL";
            if (actionDescriptor.Variant != 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Type", sampleType, EditorStyles.boldLabel);
                GUILayout.Label($"v. {actionDescriptor.Variant}", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(30));
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("Type", sampleType, EditorStyles.boldLabel);
            }
            serObj.FindProperty("m_name").stringValue = actionDescriptor.Name = EditorGUILayout.TextField("Name", actionDescriptor.Name);
            serObj.FindProperty("m_color").colorValue = actionDescriptor.Color = EditorGUILayout.ColorField("Color", actionDescriptor.Color);
            //actionDescriptor.RelativePath = EditorGUILayout.TextField("Path Name", actionDescriptor.RelativePath);
            //EditorGUILayout.LabelField("Full Path", actionDescriptor.FullPath);
            
            serObj.FindProperty("m_fullPath").stringValue = actionDescriptor.FullPath;


            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            serObj.FindProperty("m_description").stringValue = actionDescriptor.Description = EditorGUILayout.TextArea(actionDescriptor.Description);
            if (string.IsNullOrEmpty(actionDescriptor.Description))
            {
                GUI.Label(GUILayoutUtility.GetLastRect(), "Description", s_styles.placeholder);
            }
            serObj.ApplyModifiedProperties();

            GUILayout.Label("DEFAULT VALUES", EditorStyles.centeredGreyMiniLabel);

            EditorGUIUtility.labelWidth = labelWidth;

            (ProcedureObjectEditor.Get(actionDescriptor.Sample) as ActionEditor).DrawLayoutSelective(actionDescriptor.HiddenProperties);

            //var serObj = new SerializedObject(actionDescriptor.Sample);
            //var property = serObj.FindProperty(nameof(BaseAction.separator));
            //while (property.NextVisible(false))
            //{
            //    EditorGUILayout.PropertyField(property);
            //}

            if (EditorGUI.EndChangeCheck())
            {
                //actionDescriptor.UpdateFullPaths();
            }


            EditorGUILayout.EndVertical();

            GUILayout.Space(4);
        }
    }
}
