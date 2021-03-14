#if WEAVR_SERIALIZATION
using TXT.WEAVR.Utility.Serialization;
using UnityEditor;

namespace TXT.WEAVR.Interaction
{
    [CustomEditor(typeof(VR_Teleport))]
    public class VR_TeleportEditor : UnityEditor.Editor
    {
        private VR_Teleport m_self = null;

        public void Awake()
        {
            m_self = target as VR_Teleport;
            m_self.InitializeInstances(true);
        }

        public override void OnInspectorGUI()
        {
            m_self = target as VR_Teleport;
            base.OnInspectorGUI();
            m_self.UpdateInstance();
        }
    }
}
#endif