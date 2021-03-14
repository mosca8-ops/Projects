using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections.Generic;

namespace TXT.WEAVR.Procedure
{
    public class ProcedureStateInjectionResolver : MonoBehaviour
    {
        private static bool s_resolved;
        private static bool hasToSave;

        [DidReloadScripts]
        [InitializeOnLoadMethod]
        private static void Resolve()
        {
            if (s_resolved)
                return;
            s_resolved = true;

            EditorApplication.playModeStateChanged -= HandleOnPlayModeChanged;
            EditorApplication.playModeStateChanged += HandleOnPlayModeChanged;

            ProcedureStateManager.CheckFolders -= CheckFolders;
            ProcedureStateManager.CheckFolders += CheckFolders;

            ProcedureStateManager.CreateAsset -= CreateAsset;
            ProcedureStateManager.CreateAsset += CreateAsset;

            ProcedureStateManager.SuccessfulSaveState -= SuccessfulSaveState;
            ProcedureStateManager.SuccessfulSaveState += SuccessfulSaveState;
        }

        private static void HandleOnPlayModeChanged(PlayModeStateChange _playmodeState)
        {
            if (_playmodeState == PlayModeStateChange.EnteredEditMode && hasToSave)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                hasToSave = false;
            }
        }

        private static void CheckFolders(string _newFolder)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            if (!AssetDatabase.IsValidFolder("Assets/Resources/" + _newFolder))
                AssetDatabase.CreateFolder("Assets/Resources", _newFolder);
        }

        private static void CreateAsset(ProcedureMaterialData _object, string _path)
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(_path);
            if (asset != null)
                AssetDatabase.DeleteAsset(_path);

            AssetDatabase.CreateAsset(_object, _path);
            AddAssets(_object.MaterialDatas, _path);

            hasToSave = true;
        }

        private static void AddAssets(List<ProcedureMaterialData.MaterialData> _materialDatas, string _path)
        {
            foreach (var matData in _materialDatas)
            {
                if (!AssetDatabase.Contains(matData.Material))
                    AssetDatabase.AddObjectToAsset(matData.Material, _path);
            }
        }

        private static void SuccessfulSaveState()
        {
            EditorUtility.DisplayDialog("Successful Save State",
                "The state of the procedure has been saved succesfully.",
                "Dismiss");
        }
    }
}
