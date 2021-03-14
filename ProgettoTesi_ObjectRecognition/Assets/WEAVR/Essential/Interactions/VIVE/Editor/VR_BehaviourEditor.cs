#if WEAVR_SERIALIZATION
using TXT.WEAVR.Utility.Serialization;
using UnityEditor;

namespace TXT.WEAVR.Interaction
{
    [CustomEditor(typeof(VR_Behaviour))]
    public class VR_BehaviourEditor : UnityEditor.Editor
    {
        private VR_Behaviour m_self = null;

        public void Awake()
        {
            m_self = target as VR_Behaviour;
            m_self.InitializeInstances(true);
        }

        public override void OnInspectorGUI()
        {
            m_self = target as VR_Behaviour;
            base.OnInspectorGUI();
            m_self.UpdateInstance();
        }
    }
}
#endif