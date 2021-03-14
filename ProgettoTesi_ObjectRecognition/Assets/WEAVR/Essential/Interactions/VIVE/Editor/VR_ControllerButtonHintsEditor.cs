#if WEAVR_SERIALIZATION
using TXT.WEAVR.Utility.Serialization;
using UnityEditor;

namespace TXT.WEAVR.Interaction
{
    [CustomEditor(typeof(VR_ControllerButtonHints))]
    public class VR_ControllerButtonHintsEditor : UnityEditor.Editor
    {
        private VR_ControllerButtonHints m_self = null;

        public void Awake()
        {
            m_self = target as VR_ControllerButtonHints;
            m_self.InitializeInstances(true);
        }

        public override void OnInspectorGUI()
        {
            m_self = target as VR_ControllerButtonHints;
            base.OnInspectorGUI();
            m_self.UpdateInstance();
        }
    }
}
#endif