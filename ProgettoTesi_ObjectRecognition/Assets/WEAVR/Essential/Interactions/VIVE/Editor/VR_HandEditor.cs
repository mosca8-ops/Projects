#if WEAVR_SERIALIZATION
using TXT.WEAVR.Utility.Serialization;
using UnityEditor;

namespace TXT.WEAVR.Interaction
{
    [CustomEditor(typeof(VR_Hand))]
    public class VR_HandEditor : UnityEditor.Editor
    {
        private VR_Hand m_self = null;

        public void Awake()
        {
            m_self = target as VR_Hand;
            m_self.InitializeInstances(true);
        }

        public override void OnInspectorGUI()
        {
            m_self = target as VR_Hand;
            base.OnInspectorGUI();
            m_self.UpdateInstance();
        }
    }
}
#endif