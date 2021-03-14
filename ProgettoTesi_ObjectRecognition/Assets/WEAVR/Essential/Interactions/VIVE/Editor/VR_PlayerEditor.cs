#if WEAVR_SERIALIZATION
using TXT.WEAVR.Utility.Serialization;
using UnityEditor;

namespace TXT.WEAVR.Interaction
{
    [CustomEditor(typeof(VR_Player))]
    public class VR_PlayerEditor : UnityEditor.Editor
    {
        private VR_Player m_self = null;

        public void Awake()
        {
            m_self = target as VR_Player;
            m_self.InitializeInstances(true);
        }

        public override void OnInspectorGUI()
        {
            m_self = target as VR_Player;
            base.OnInspectorGUI();
            m_self.UpdateInstance();
        }
    }
}
#endif