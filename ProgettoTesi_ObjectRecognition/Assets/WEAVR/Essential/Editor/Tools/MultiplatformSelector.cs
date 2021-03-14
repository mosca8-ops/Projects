namespace TXT.WEAVR.Tools
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(MonoScript))]
    [CanEditMultipleObjects]
    public class MultiplatformSelector : Editor
    {

        private UnityEngine.Object[] _lastTargets;
        private DirectivesContainer _currentContainer;
        private string _scriptText;
        private int _maxScriptTextLength = 4000;

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Script Modifier", EditorStyles.boldLabel);

            if (_currentContainer == null || _lastTargets != targets)
            {
                _currentContainer = new DirectivesContainer(targets);
            }

            RenderDirectives(_currentContainer);

            if (_scriptText != (target as MonoScript).text)
            {
                _scriptText = (target as MonoScript).text;
                if (_scriptText.Length > _maxScriptTextLength)
                {
                    _scriptText = _scriptText.Substring(0, _maxScriptTextLength);
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.LabelField(_scriptText, EditorStyles.textArea);
            EditorGUI.indentLevel++;

            _lastTargets = targets;
        }

        public static void RenderDirectives(DirectivesContainer container)
        {
            EditorGUI.indentLevel++;
            #region Platform Specific Group
            foreach (var platformArray in container.PlatformDirectives)
            {
                platformArray.Visible = EditorGUILayout.Foldout(platformArray.Visible, platformArray.Label);
                if (platformArray.Visible)
                {
                    EditorGUI.indentLevel++;
                    foreach (var directive in platformArray)
                    {
                        bool lastMixedValue = EditorGUI.showMixedValue;
                        EditorGUI.showMixedValue = directive.IsMixedValue;
                        directive.Value = EditorGUILayout.ToggleLeft(directive.Label, directive.Value);
                        EditorGUI.showMixedValue = lastMixedValue;
                    }
                    EditorGUI.indentLevel--;
                }
            }
            #endregion

            EditorGUILayout.Space();

            #region Common Directives
            var commonDirectives = container.CommonDirectives;
            commonDirectives.Visible = EditorGUILayout.Foldout(commonDirectives.Visible, commonDirectives.Label);
            if (commonDirectives.Visible)
            {
                EditorGUI.indentLevel++;
                foreach (var directive in commonDirectives)
                {
                    bool lastMixedValue = EditorGUI.showMixedValue;
                    EditorGUI.showMixedValue = directive.IsMixedValue;
                    directive.Value = EditorGUILayout.ToggleLeft(directive.Label, directive.Value);
                    EditorGUI.showMixedValue = lastMixedValue;
                }
                EditorGUI.indentLevel--;
            }
            #endregion

            #region Player Directives
            var playerDirectives = container.PlayerDirectives;
            playerDirectives.Visible = EditorGUILayout.Foldout(playerDirectives.Visible, playerDirectives.Label);
            if (playerDirectives.Visible)
            {
                EditorGUI.indentLevel++;
                foreach (var directive in playerDirectives)
                {
                    bool lastMixedValue = EditorGUI.showMixedValue;
                    EditorGUI.showMixedValue = directive.IsMixedValue;
                    directive.Value = EditorGUILayout.ToggleLeft(directive.Label, directive.Value);
                    EditorGUI.showMixedValue = lastMixedValue;
                }
                EditorGUI.indentLevel--;
            }
            #endregion

            EditorGUILayout.Space();

            #region Editor Version
            var versionDirective = container.UnityVersion;
            EditorGUILayout.BeginHorizontal();
            bool tempMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = versionDirective.IsMixedValue;
            versionDirective.Value = EditorGUILayout.ToggleLeft(versionDirective.Label, versionDirective.Value);
            bool versionIsEditable = versionDirective.Value && !versionDirective.IsMixedValue;
            var versionGroup = versionDirective.MainVersionGroup;
            if (versionIsEditable)
            {
                if (!versionGroup.IsRange && versionDirective.Value)
                {
                    versionGroup.UnityPreciseVersion.version = EditorGUILayout.TextField(versionGroup.UnityPreciseVersion.version ?? "");
                    versionGroup.UnityPreciseVersion.major = EditorGUILayout.TextField(versionGroup.UnityPreciseVersion.major ?? "");
                    versionGroup.UnityPreciseVersion.minor = EditorGUILayout.TextField(versionGroup.UnityPreciseVersion.minor ?? "");
                }
            }
            EditorGUILayout.EndHorizontal();
            if (versionIsEditable)
            {
                EditorGUI.indentLevel++;
                versionGroup.IsRange = EditorGUILayout.ToggleLeft("Range", versionGroup.IsRange);
                if (versionGroup.IsRange)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Min Version");
                    versionGroup.UnityMinVersion.version = EditorGUILayout.TextField(versionGroup.UnityMinVersion.version ?? "");
                    versionGroup.UnityMinVersion.major = EditorGUILayout.TextField(versionGroup.UnityMinVersion.major ?? "");
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Max Version (Excluded)");
                    versionGroup.UnityMaxVersion.version = EditorGUILayout.TextField(versionGroup.UnityMaxVersion.version ?? "");
                    versionGroup.UnityMaxVersion.major = EditorGUILayout.TextField(versionGroup.UnityMaxVersion.major ?? "");
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.showMixedValue = tempMixedValue;
            #endregion

            EditorGUILayout.Space();

            EditorGUI.indentLevel--;

            string buttonLabel = container.TextAssets.Count > 1 ? "Apply Changes for " + container.TextAssets.Count + " Files" : "Apply Changes";

            if (GUILayout.Button(buttonLabel))
            {
                container.ApplyChanges();
            }
        }

    }
}