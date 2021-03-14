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
    [CustomEditor(typeof(AnimationsCatalogue))]
    public class AnimationsCatalogueEditor : CatalogueEditor<AnimationsCatalogue, AnimationDescriptor, BaseAnimationBlock>
    {
        protected override string AddDescriptorLabel()
        {
            return "Add Block";
        }

        protected override AnimationDescriptor InstantiateDescriptor(Type type)
        {
            return AnimationDescriptor.CreateDescriptor(type);
        }

        protected override void DrawDescriptor(DescriptorGroup parent, AnimationDescriptor animationDescriptor)
        {
            EditorGUILayout.BeginVertical("Box");

            EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
            GUILayout.Label("Animation Block", s_styles.descriptorType);
            EditorGUILayout.Space();
            GUILayout.Label(animationDescriptor.FullPath, s_styles.path);
            GUILayout.FlexibleSpace();
            var color = GUI.color;
            DrawUpDownArrows(parent, animationDescriptor);
            GUI.color = Color.red;
            if (GUILayout.Button("X"))
            {
                m_preRenderAction = () =>
                {
                    parent.Remove(animationDescriptor);
                    RefreshCatalogue();
                };
            }
            GUI.color = color;
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            float width = EditorGUIUtility.singleLineHeight * 3f;
            float height = EditorGUIUtility.singleLineHeight * 3;
            animationDescriptor.Icon = EditorGUILayout.ObjectField(animationDescriptor.Icon, typeof(Texture2D), false, GUILayout.Width(width), GUILayout.Height(height)) as Texture2D;

            EditorGUILayout.BeginVertical();

            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth -= width + EditorGUIUtility.standardVerticalSpacing * 2;
            var serObj = Get(animationDescriptor);
            serObj.Update();

            string sampleType = animationDescriptor.Sample ? animationDescriptor.Sample.GetType().Name : "NULL";
            if (animationDescriptor.Variant != 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Type", sampleType, EditorStyles.boldLabel);
                GUILayout.Label($"v. {animationDescriptor.Variant}", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(30));
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("Type", sampleType, EditorStyles.boldLabel);
            }
            serObj.FindProperty("m_name").stringValue = animationDescriptor.Name = EditorGUILayout.TextField("Name", animationDescriptor.Name);
            serObj.FindProperty("m_color").colorValue = animationDescriptor.Color = EditorGUILayout.ColorField("Color", animationDescriptor.Color);
            //actionDescriptor.RelativePath = EditorGUILayout.TextField("Path Name", actionDescriptor.RelativePath);
            //EditorGUILayout.LabelField("Full Path", actionDescriptor.FullPath);
            
            serObj.FindProperty("m_fullPath").stringValue = animationDescriptor.FullPath;


            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            serObj.FindProperty("m_description").stringValue = animationDescriptor.Description = EditorGUILayout.TextArea(animationDescriptor.Description);
            if (string.IsNullOrEmpty(animationDescriptor.Description))
            {
                GUI.Label(GUILayoutUtility.GetLastRect(), "Description", s_styles.placeholder);
            }
            serObj.ApplyModifiedProperties();

            GUILayout.Label("DEFAULT VALUES", EditorStyles.centeredGreyMiniLabel);

            EditorGUIUtility.labelWidth = labelWidth;

            (ProcedureObjectEditor.Get(animationDescriptor.Sample) as AnimationBlockEditor).DrawLayoutSelective(animationDescriptor.HiddenProperties);

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
