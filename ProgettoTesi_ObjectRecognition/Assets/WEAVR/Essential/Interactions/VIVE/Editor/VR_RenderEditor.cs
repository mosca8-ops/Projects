#if WEAVR_SERIALIZATION
using TXT.WEAVR.Utility.Serialization;
using UnityEditor;

namespace TXT.WEAVR.Interaction
{
    [CustomEditor(typeof(VR_Render))]
    public class VR_RenderEditor : UnityEditor.Editor
    {
        private VR_Render m_self = null;

        public void Awake()
        {
            m_self = target as VR_Render;
            m_self.InitializeInstances(true);
        }

        public override void OnInspectorGUI()
        {
            m_self = target as VR_Render;
            base.OnInspectorGUI();
            m_self.UpdateInstance();
        }
    }
}
#endif