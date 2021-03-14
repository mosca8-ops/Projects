using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Interaction
{
    [CustomEditor(typeof(VR_RotatorSimple))]
    public class VR_RotatorSimpleEditor : UnityEditor.Editor
    {
        private Quaternion m_defaultRotation;
        private Vector3 m_defaultForward;
        private VR_RotatorSimple m_rotator;

        private void OnEnable()
        {
            m_rotator = target as VR_RotatorSimple;
            m_defaultRotation = m_rotator.transform.rotation;
            m_defaultForward = m_rotator.transform.forward;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.BeginHorizontal("Box");
            EditorGUILayout.BeginHorizontal("Box");
            if(GUILayout.Button("Save", EditorStyles.miniButtonLeft))
            {
                m_defaultRotation = m_rotator.transform.rotation;
                m_defaultForward = m_rotator.transform.forward;
            }
            GUILayout.Label("DEFAULT STATE", EditorStyles.centeredGreyMiniLabel);
            if (GUILayout.Button("Restore", EditorStyles.miniButtonRight))
            {
                m_rotator.transform.rotation = m_defaultRotation;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal("Box");
            if (GUILayout.Button("Set Min", EditorStyles.miniButtonLeft))
            {
                serializedObject.Update();
                EnableLimits();
                SetMin();
                serializedObject.ApplyModifiedProperties();
            }
            GUILayout.Label("LIMITS", EditorStyles.centeredGreyMiniLabel);
            if (GUILayout.Button("Set Max", EditorStyles.miniButtonRight))
            {
                serializedObject.Update();
                EnableLimits();
                SetMax();
                serializedObject.ApplyModifiedProperties();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();
        }

        private void SetMax()
        {
            float angle = Quaternion.Angle(m_defaultRotation, m_rotator.transform.rotation);
            serializedObject.FindProperty($"{nameof(VR_RotatorSimple.angleLimits)}.value.{nameof(Span.max)}").floatValue = angle;
            if(Mathf.Abs(angle) > 5)
            {
                serializedObject.FindProperty(nameof(VR_RotatorSimple.axisOfRotation)).vector3Value = m_rotator.transform.InverseTransformDirection(Vector3.Cross(m_defaultForward, m_rotator.transform.forward).normalized);
            }
        }

        private void SetMin()
        {
            float angle = Quaternion.Angle(m_rotator.transform.rotation, m_defaultRotation);
            serializedObject.FindProperty($"{nameof(VR_RotatorSimple.angleLimits)}.value.{nameof(Span.min)}").floatValue = -angle;
            if (Mathf.Abs(angle) > 5)
            {
                serializedObject.FindProperty(nameof(VR_RotatorSimple.axisOfRotation)).vector3Value = -m_rotator.transform.InverseTransformDirection(Vector3.Cross(m_defaultForward, m_rotator.transform.forward).normalized);
            }
        }

        private void EnableLimits()
        {
            serializedObject.FindProperty($"{nameof(VR_RotatorSimple.angleLimits)}.{nameof(Optional.enabled)}").boolValue = true;
        }
    }
}
