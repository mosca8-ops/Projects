using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Editor;
using TXT.WEAVR.Maintenance;
using TXT.WEAVR.Procedure;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.EditorBridge
{
    [InitializeOnLoad]
    public static class MaintenanceEditorInjection
    {
        static MaintenanceEditorInjection()
        {
            Resolve();
        }
        static void Resolve()
        {
            GrabPath.GetAnimationPositionFunction = GetAnimationPosition;
            GrabPath.AnimationAsARotationFunction = AnimationAsARotation;
        }

        private static Vector3[] GetAnimationPosition(AnimationClip animationClip)
        {
            AnimationCurve curve;
            var curveBindings = AnimationUtility.GetCurveBindings(animationClip);
            int index = 0;
            Vector3[] generatedPoints = new Vector3[100];

            foreach (var curveBinding in curveBindings)
            {
                if (curveBinding.propertyName == "m_LocalPosition.x")
                    index = 0;
                else if (curveBinding.propertyName == "m_LocalPosition.y")
                    index = 1;
                else if (curveBinding.propertyName == "m_LocalPosition.z")
                    index = 2;
                else
                    continue;

                curve = AnimationUtility.GetEditorCurve(animationClip, curveBinding);

                generatedPoints[0][index] = curve.Evaluate(0);

                for (float i = 2; i < 100 + 1; i++)
                    generatedPoints[(int)i - 1][index] = curve.Evaluate(animationClip.length * (i / 100));
            }

            return generatedPoints;
        }

        private static bool AnimationAsARotation(AnimationClip animationClip)
        {
            var curveBindings = AnimationUtility.GetCurveBindings(animationClip);

            foreach (var curveBinding in curveBindings)
            {
                if (curveBinding.propertyName == "localEulerAnglesRaw.x" || curveBinding.propertyName == "localEulerAnglesRaw.y" || curveBinding.propertyName == "localEulerAnglesRaw.z")
                {
                    return true;
                }
            }

            return false;
        }
    }
}
