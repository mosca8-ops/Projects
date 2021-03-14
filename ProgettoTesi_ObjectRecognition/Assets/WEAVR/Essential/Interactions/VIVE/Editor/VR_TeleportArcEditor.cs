#if WEAVR_SERIALIZATION
using TXT.WEAVR.Utility.Serialization;
using UnityEditor;

namespace TXT.WEAVR.Interaction
{
    [CustomEditor(typeof(VR_TeleportArc))]
    public class VR_TeleportArcEditor : UnityEditor.Editor
    {
        private VR_TeleportArc m_self = null;

        public void Awake()
        {
            m_self = target as VR_TeleportArc;
            m_self.InitializeInstances(true);
        }

        public override void OnInspectorGUI()
        {
            m_self = target as VR_TeleportArc;
            base.OnInspectorGUI();
            m_self.UpdateInstance();
        }
    }
}
#endif