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
    [CustomEditor(typeof(ConditionsCatalogue))]
    public class ConditionsCatalogueEditor : CatalogueEditor<ConditionsCatalogue, ConditionDescriptor, BaseCondition>
    {
        protected override string AddDescriptorLabel()
        {
            return "Add Condition";
        }

        protected override List<Type> GetDescriptorTypes()
        {
            var flowType = typeof(FlowCondition);
            return typeof(BaseCondition).GetAllSubclassesOf().Where(t => !t.IsAbstract && !flowType.IsAssignableFrom(t)).ToList();
        }
        
        protected override void DrawDescriptor(DescriptorGroup parent, ConditionDescriptor conditionDescriptor)
        {
            EditorGUILayout.BeginVertical("Box");

            EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
            GUILayout.Label("Condition", s_styles.descriptorType);
            EditorGUILayout.Space();
            GUILayout.Label(conditionDescriptor.FullPath, s_styles.path);
            GUILayout.FlexibleSpace();
            var color = GUI.color;
            DrawUpDownArrows(parent, conditionDescriptor);
            GUI.color = Color.red;
            if (GUILayout.Button("X"))
            {
                m_preRenderAction = () =>
                {
                    parent.Remove(conditionDescriptor);
                    RefreshCatalogue();
                };
            }
            GUI.color = color;
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            float width = EditorGUIUtility.singleLineHeight * 3f;
            float height = EditorGUIUtility.singleLineHeight * 3;
            conditionDescriptor.Icon = EditorGUILayout.ObjectField(conditionDescriptor.Icon, typeof(Texture2D), false, GUILayout.Width(width), GUILayout.Height(height)) as Texture2D;
            EditorGUILayout.BeginVertical();

            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth -= width + EditorGUIUtility.standardVerticalSpacing * 2;
            var serObj = Get(conditionDescriptor);
            serObj.Update();

            string sampleType = conditionDescriptor.Sample ? conditionDescriptor.Sample.GetType().Name : "NULL";
            if (conditionDescriptor.Variant != 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Type", sampleType, EditorStyles.boldLabel);
                GUILayout.Label($"v. {conditionDescriptor.Variant}", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(30));
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("Type", sampleType, EditorStyles.boldLabel);
            }
            serObj.FindProperty("m_name").stringValue = conditionDescriptor.Name = EditorGUILayout.TextField("Name", conditionDescriptor.Name);
            serObj.FindProperty("m_color").colorValue = conditionDescriptor.Color = EditorGUILayout.ColorField("Color", conditionDescriptor.Color);
            //actionDescriptor.RelativePath = EditorGUILayout.TextField("Path Name", actionDescriptor.RelativePath);
            //EditorGUILayout.LabelField("Full Path", actionDescriptor.FullPath);
            
            serObj.FindProperty("m_fullPath").stringValue = conditionDescriptor.FullPath;

            serObj.ApplyModifiedProperties();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            serObj.FindProperty("m_description").stringValue = conditionDescriptor.Description = EditorGUILayout.TextArea(conditionDescriptor.Description);
            if (string.IsNullOrEmpty(conditionDescriptor.Description))
            {
                GUI.Label(GUILayoutUtility.GetLastRect(), "Description", s_styles.placeholder);
            }

            GUILayout.Label("DEFAULT VALUES", EditorStyles.centeredGreyMiniLabel);

            EditorGUIUtility.labelWidth = labelWidth;

            (ProcedureObjectEditor.Get(conditionDescriptor.Sample) as ConditionEditor).DrawLayoutSelective(conditionDescriptor.HiddenProperties);

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

        protected override ConditionDescriptor InstantiateDescriptor(Type type)
        {
            return ConditionDescriptor.CreateDescriptor(type);
        }
    }
}
