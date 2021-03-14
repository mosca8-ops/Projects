#if WEAVR_SERIALIZATION
using TXT.WEAVR.Utility.Serialization;
using UnityEditor;

namespace TXT.WEAVR.Interaction
{
    [CustomEditor(typeof(VR_BodyCollider))]
    public class VR_BodyColliderEditor : UnityEditor.Editor
    {
        private VR_BodyCollider m_self = null;

        public void Awake()
        {
            m_self = target as VR_BodyCollider;
            m_self.InitializeInstances(true);
        }

        public override void OnInspectorGUI()
        {
            m_self = target as VR_BodyCollider;
            base.OnInspectorGUI();
            m_self.UpdateInstance();
        }
    }
}
#endif