using UnityEditor;

namespace TXT.WEAVR.Procedure
{
    public interface ICustomActionEditor
    {
        void DrawLayout(SerializedObject serializedObject, SerializedProperty firstProperty);
    }
}