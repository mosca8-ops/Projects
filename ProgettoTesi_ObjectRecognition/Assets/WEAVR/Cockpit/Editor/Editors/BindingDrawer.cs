namespace TXT.WEAVR.Cockpit
{
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Editor;
    using UnityEditor;
    using UnityEngine;

    public class BindingDrawer
    {
        private const float _separatorHeight = 1;

        public static void DrawBinding(Binding binding, bool nameIsFixed, PropertyPathField propertyPathField = null) {
            float leftSideWidth = 100;

            float lastLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = leftSideWidth;

            EditorGUILayout.BeginHorizontal();
            if (nameIsFixed) {
                EditorGUILayout.LabelField(binding.id, GUILayout.Width(leftSideWidth));
            }
            else {
                binding.id = EditorGUILayout.TextField(binding.id, GUILayout.Width(leftSideWidth));
            }

            binding.mode = (BindingMode)EditorGUILayout.EnumPopup(binding.mode);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Data Source", GUILayout.Width(leftSideWidth));
            binding.dataSource = EditorGUILayout.ObjectField(binding.dataSource, typeof(GameObject), true) as GameObject;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Path", GUILayout.MaxWidth(leftSideWidth));
            bool showFullPath = binding.propertyPath != null && binding.propertyPath.Length * 4 < (EditorGUIUtility.currentViewWidth - leftSideWidth);
            if (propertyPathField != null) {
                binding.propertyPath = propertyPathField
                                      .DrawPropertyPathField(binding.dataSource,
                                                             binding.propertyPath,
                                                             binding.mode == BindingMode.Write || binding.mode == BindingMode.Both,
                                                             showFullPath);
                var propertyInfo = propertyPathField.SelectedProperty;
                if (propertyInfo != null) {
                    binding.type = propertyInfo.type;
                }
            }
            else {
                binding.propertyPath = WeavrGUILayout
                                     .PropertyPathPopup(binding, binding.dataSource,
                                                            binding.propertyPath,
                                                            binding.mode == BindingMode.Write || binding.mode == BindingMode.Both,
                                                            showFullPath);
                var propertyInfo = WeavrGUI.GetPropertyInfo(binding);
                if(propertyInfo != null) {
                    binding.type = propertyInfo.type;
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = lastLabelWidth;
        }

        public static void DrawBinding(Rect rect, Binding binding, bool nameIsFixed, PropertyPathField propertyPathField = null) {
            float lastLabelWidth = EditorGUIUtility.labelWidth;
            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUIUtility.labelWidth = 80;

            Rect leftRect = rect;
            leftRect.width = EditorGUIUtility.labelWidth - 10;

            Rect rightRect = leftRect;
            rightRect.x += leftRect.width + 5;
            rightRect.width = rect.width - leftRect.width - 5;

            if (nameIsFixed) {
                EditorGUI.SelectableLabel(leftRect, binding.id);
            }
            else {
                binding.id = EditorGUI.TextField(leftRect, binding.id);
            }

            binding.mode = (BindingMode)EditorGUI.EnumPopup(rightRect, binding.mode);
            rect.y += EditorGUIUtility.singleLineHeight + _separatorHeight;

            binding.dataSource = EditorGUI.ObjectField(rect, "Data Source", binding.dataSource, typeof(GameObject), true) as GameObject;
            rect.y += EditorGUIUtility.singleLineHeight + _separatorHeight;

            if(binding.dataSource == null) {
                EditorGUIUtility.labelWidth = lastLabelWidth;
                return;
            }

            leftRect.y = rightRect.y = rect.y;
            EditorGUI.LabelField(leftRect, "Path");
            bool showFullPath = binding.propertyPath != null && binding.propertyPath.Length * 4.5f < rightRect.width;
            if (propertyPathField != null) {
                binding.propertyPath = propertyPathField
                                      .DrawPropertyPathField(rightRect, binding.dataSource,
                                                             binding.propertyPath,
                                                             binding.mode == BindingMode.Write || binding.mode == BindingMode.Both,
                                                             showFullPath);
                var propertyInfo = propertyPathField.SelectedProperty;
                if (propertyInfo != null) {
                    binding.type = propertyInfo.type;
                }
            }
            else {
                binding.propertyPath = WeavrGUI
                                     .PropertyPathPopup(binding, rightRect, binding.dataSource,
                                                            binding.propertyPath,
                                                            binding.mode == BindingMode.Write || binding.mode == BindingMode.Both,
                                                            showFullPath);
                var propertyInfo = WeavrGUI.GetPropertyInfo(binding);
                if (propertyInfo != null) {
                    binding.type = propertyInfo.type;
                }
            }

            EditorGUIUtility.labelWidth = lastLabelWidth;
        }

        public static float GetHeight(Binding binding) {
            float lineHeight = EditorGUIUtility.singleLineHeight + _separatorHeight;
            return binding.dataSource != null ? lineHeight * 3 : lineHeight * 2;
        }
    }
}