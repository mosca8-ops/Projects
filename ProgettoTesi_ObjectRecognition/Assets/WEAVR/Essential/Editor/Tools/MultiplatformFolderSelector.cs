namespace TXT.WEAVR.Tools
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    [CustomEditor(typeof(DefaultAsset), true)]
    [CanEditMultipleObjects]
    public class MultiplatformFolderSelector : Editor
    {

        private bool _includeChildren;
        private bool _isValidFolder;
        private Object[] _lastTargets;
        private DirectivesContainer _currentContainer;
        private List<Object> _allAssets;

        public override void OnInspectorGUI() {
            //base.OnInspectorGUI();
            if (target == null) {
                return;
            }
            if (_lastTargets != targets) {
                if (_allAssets == null) {
                    _allAssets = new List<Object>();
                }
                _allAssets.Clear();
                _isValidFolder = false;
                foreach (var folderAsset in targets) {
                    _isValidFolder |= AssetsUtility.IsAssetFolder(folderAsset);
                }
            }
            if (!_isValidFolder) {
                return;
            }
            GUI.enabled = true;
            EditorGUILayout.LabelField("Folder Scripts Modifier", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            bool shouldUseNewContainer = _currentContainer == null || _lastTargets != targets;
            if (_currentContainer == null || _lastTargets != targets) {
                _currentContainer = new DirectivesContainer(targets);
            }

            string recursiveButtonLabel = "Recursive Search" + (targets.Length > 1 ? " (in " + targets.Length + " folders)" : "");
            bool recursive = EditorGUILayout.ToggleLeft(recursiveButtonLabel, _includeChildren);
            shouldUseNewContainer |= recursive != _includeChildren;
            _includeChildren = recursive;

            if (shouldUseNewContainer) {
                _allAssets.Clear();
                foreach (var folderAsset in targets) {
                    if (AssetsUtility.IsAssetFolder(folderAsset)) {
                        _allAssets.AddRange(AssetsUtility.LoadAllAssetsAt(folderAsset, recursive));
                    }
                }

                _currentContainer = new DirectivesContainer(_allAssets.ToArray());
            }

            EditorGUILayout.Space();
            if (_allAssets.Count > 0) {
                MultiplatformSelector.RenderDirectives(_currentContainer);
            }

            _lastTargets = targets;
        }

        private void FindAssets(List<Object> assets, Object targetAsset, bool recurse) {
            var children = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(targetAsset));
            assets.AddRange(children);
            if (children.Length > 0 && recurse) {
                foreach (var child in children) {
                    FindAssets(assets, child, recurse);
                }
            }
        }
    }
}