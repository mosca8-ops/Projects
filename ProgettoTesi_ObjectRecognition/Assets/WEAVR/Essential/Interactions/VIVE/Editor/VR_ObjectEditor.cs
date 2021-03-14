#if UNITY_EDITOR
using TXT.WEAVR.Utility.Serialization;
using UnityEditor;
#endif

namespace TXT.WEAVR.Interaction
{
    [CustomEditor(typeof(VR_Object))]
    public class VR_ObjectEditor : UnityEditor.Editor
    {
        private VR_Object m_self = null;

        public void Awake()
        {
            m_self = target as VR_Object;
            m_self.InitializeInstances(true);
        }

        public override void OnInspectorGUI()
        {
            m_self = target as VR_Object;
            m_self.m_interactionMode = (VR_Object.InteractionMode) EditorGUILayout.EnumPopup("Interaction Mode", m_self.m_interactionMode);
#if WEAVR_VR
            //m_self.highlightOnHover = EditorGUILayout.Toggle("HighLight on Hover", m_self.highlightOnHover);
            switch (m_self.GetInteractionMode())
            {
                case VR_Object.InteractionMode.GlueToObject:
                    m_self.m_GlueHandDistance = EditorGUILayout.FloatField("Glue Distance", m_self.m_GlueHandDistance);
                    m_self.m_GlueHandTime = EditorGUILayout.FloatField("Glue Time", m_self.m_GlueHandTime);
                    break;
                case VR_Object.InteractionMode.BlendToFinalPose:
                    break;
            }
#endif
            m_self.UpdateInstance();
        }
    }
}