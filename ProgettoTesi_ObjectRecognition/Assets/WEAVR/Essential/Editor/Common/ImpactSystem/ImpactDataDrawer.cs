using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.ImpactSystem
{
    [CustomPropertyDrawer(typeof(ImpactData))]
    public class ImpactDataDrawer : PropertyDrawer
    {

        private static GUIStyle s_boxStyle;
        private static GUIContent s_followGo = new GUIContent("F", "Follow collider");
        private static GUIContent s_revImpulse = new GUIContent("R", "Rotate to impulse");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.x += EditorGUI.indentLevel * 15f;
            position.width -= EditorGUI.indentLevel * 15f;
            position.height -= 2;

            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            if(Event.current.type == EventType.Repaint)
            {
                if(s_boxStyle == null)
                {
                    s_boxStyle = "Box";
                }
                s_boxStyle.Draw(position, false, false, false, false);
            }

            position.x += 2;
            position.width -= 4;
            position.y += 2;
            position.height -= 4;

            var rect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            var labelWidth = EditorGUIUtility.labelWidth;

            // Draw first line
            GUI.Label(rect, label, EditorStyles.boldLabel);
            rect.x += rect.width;
            rect.width = position.width - rect.width;

            EditorGUIUtility.labelWidth = 60;
            var forceProperty = property.FindPropertyRelative(nameof(ImpactData.force));
            forceProperty.floatValue = Mathf.Max(0, EditorGUI.DelayedFloatField(rect, "Min Force", forceProperty.floatValue));

            // Draw Sounds
            var soundsProperty = property.FindPropertyRelative(nameof(ImpactData.sounds));
            rect.x = position.x;
            rect.width = 20;
            rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
            if(GUI.Button(rect, "+"))
            {
                soundsProperty.InsertArrayElementAtIndex(soundsProperty.arraySize);
                var minProperty = soundsProperty.GetArrayElementAtIndex(soundsProperty.arraySize - 1).FindPropertyRelative(nameof(ImpactData.Sound.pitch)).FindPropertyRelative(nameof(Span.min));
                var maxProperty = soundsProperty.GetArrayElementAtIndex(soundsProperty.arraySize - 1).FindPropertyRelative(nameof(ImpactData.Sound.pitch)).FindPropertyRelative(nameof(Span.max));

                if(minProperty.floatValue == 0 && maxProperty.floatValue == 0)
                {
                    minProperty.floatValue = ImpactData.Sound.k_defaultMinPitch;
                    maxProperty.floatValue = ImpactData.Sound.k_defaultMaxPitch;
                }
                else if(minProperty.floatValue > maxProperty.floatValue)
                {
                    minProperty.floatValue = Mathf.Max(0, maxProperty.floatValue);
                    maxProperty.floatValue = Mathf.Min(2, minProperty.floatValue);
                }
                else
                {
                    minProperty.floatValue = Mathf.Max(0, minProperty.floatValue);
                    maxProperty.floatValue = Mathf.Min(2, maxProperty.floatValue);
                }
            }
            rect.x += rect.width + 2;
            rect.width = labelWidth;
            GUI.Label(rect, "Sounds");

            rect.width = position.width - 24;
            rect.x = position.x;
            rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;

            var remRect = new Rect(position.xMax - 20, rect.y, 20, rect.height);

            EditorGUIUtility.labelWidth = 60;
            EditorGUI.indentLevel++;

            rect.width -= 180;
            var pitchRect = new Rect(rect.xMax - 10, rect.y, position.width - rect.width - remRect.width + 10, rect.height);

            for (int i = 0; i < soundsProperty.arraySize; i++)
            {
                // Clip part
                var soundProperty = soundsProperty.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(ImpactData.Sound.clip));
                EditorGUI.PropertyField(rect, soundProperty, GUIContent.none);

                // Pitch part
                bool guiEnabled = GUI.enabled;
                GUI.enabled = soundProperty.objectReferenceValue;
                soundProperty.Next(false);
                EditorGUI.PropertyField(pitchRect, soundProperty);
                GUI.enabled = guiEnabled;

                if(GUI.Button(remRect, "-", EditorStyles.miniButton))
                {
                    soundsProperty.DeleteArrayElementAtIndex(i--);
                }
                rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                pitchRect.y = remRect.y = rect.y;
            }

            EditorGUI.indentLevel--;

            // Draw effects
            EditorGUIUtility.labelWidth = 60;
            var effectsProperty = property.FindPropertyRelative(nameof(ImpactData.effects));

            remRect.y = rect.y = rect.y + 8;
            rect.width = 20;
            remRect.x = position.xMax - 20;
            remRect.width = 20;

            if(GUI.Button(rect, "+"))
            {
                effectsProperty.InsertArrayElementAtIndex(effectsProperty.arraySize);
            }

            var lifetimeRect = new Rect(remRect.x - 100, remRect.y, 60, remRect.height);

            EditorGUIUtility.labelWidth = 100;

            rect.x += rect.width + 2;
            rect.width = labelWidth;
            GUI.Label(rect, "Effects");
            GUI.Label(lifetimeRect, "Lifetime");

            var followRect = new Rect(lifetimeRect.xMax + 2, lifetimeRect.y, 15, lifetimeRect.height);
            var rotateRect = new Rect(lifetimeRect.xMax + 19, lifetimeRect.y, 15, lifetimeRect.height);
            GUI.Label(followRect, s_followGo);
            GUI.Label(rotateRect, s_revImpulse);

            rect.x = position.x + 15;
            rect.width = position.width - 124 - 15;
            rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
            followRect.y = rotateRect.y = lifetimeRect.y = remRect.y = rect.y;

            //EditorGUI.indentLevel++;
            for (int i = 0; i < effectsProperty.arraySize; i++)
            {
                var effectProp = effectsProperty.GetArrayElementAtIndex(i);
                effectProp.Next(true);

                EditorGUI.PropertyField(rect, effectProp);
                effectProp.Next(false);

                effectProp.floatValue = EditorGUI.FloatField(lifetimeRect, effectProp.floatValue);

                effectProp.Next(false);
                effectProp.boolValue = EditorGUI.Toggle(followRect, effectProp.boolValue);

                effectProp.Next(false);
                effectProp.boolValue = EditorGUI.Toggle(rotateRect, effectProp.boolValue);

                if (GUI.Button(remRect, "-", EditorStyles.miniButton))
                {
                    effectsProperty.DeleteArrayElementAtIndex(i--);
                }
                rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                followRect.y = rotateRect.y = lifetimeRect.y = remRect.y = rect.y;
            }
            //EditorGUI.indentLevel--;

            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel = indentLevel;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing)
                * (3 + property.FindPropertyRelative(nameof(ImpactData.sounds)).arraySize + property.FindPropertyRelative(nameof(ImpactData.effects)).arraySize) + 14;
        }
    }
}
