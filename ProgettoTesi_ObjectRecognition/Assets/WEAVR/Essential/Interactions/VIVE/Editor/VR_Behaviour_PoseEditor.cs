#if WEAVR_SERIALIZATION
using TXT.WEAVR.Utility.Serialization;
using UnityEditor;

namespace TXT.WEAVR.Interaction
{
    [CustomEditor(typeof(VR_Behaviour_Pose))]
    public class VR_Behaviour_PoseEditor : UnityEditor.Editor
    {
        private VR_Behaviour_Pose m_self = null;

        public void Awake()
        {
            m_self = target as VR_Behaviour_Pose;
            m_self.InitializeInstances(true);
        }

        public override void OnInspectorGUI()
        {
            m_self = target as VR_Behaviour_Pose;
            base.OnInspectorGUI();
            m_self.UpdateInstance();
        }
    }
}
#endif