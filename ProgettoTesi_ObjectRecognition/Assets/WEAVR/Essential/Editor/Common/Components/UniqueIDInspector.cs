namespace TXT.WEAVR.Editor
{
    using TXT.WEAVR.Core;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(UniqueID))]
    public class UniqueIDInspector : Editor
    {
        [MenuItem("CONTEXT/UniqueID/Copy UniqueID")]
        static void CopyUniqueID(MenuCommand command)
        {
            var uniqueID = (UniqueID)command.context;
            GUIUtility.systemCopyBuffer = uniqueID.ID;
        }

        public override void OnInspectorGUI() {
            var uniqueID = target as UniqueID;
            if (!string.IsNullOrEmpty(uniqueID.ID)) {
                EditorGUILayout.LabelField(uniqueID.ID, EditorStyles.centeredGreyMiniLabel);
            } else {
                EditorGUILayout.LabelField("Prefabs are not allowed to have IDs", EditorStyles.centeredGreyMiniLabel);
            }
        }
        
    }
}