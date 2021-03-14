#if UNITY_EDITOR
using TXT.WEAVR.Maintenance;
using UnityEditor;
#endif

#if WEAVR_VR
using Valve.VR;
#endif

namespace TXT.WEAVR.Interaction
{
    [CustomEditor(typeof(Executable))]
    public class ExecutableEditor : UnityEditor.Editor
    {

        public override void OnInspectorGUI()
        {
            var wSelf = target as Executable;
            base.OnInspectorGUI();
#if WEAVR_VR
            switch (wSelf.GetHoveringMode())
            {
                case VR_Object.HoveringMode.Finger:
                    wSelf.m_jointIndexForHovering = (SteamVR_Skeleton_JointIndexEnum) EditorGUILayout.EnumPopup("Hovering Finger", wSelf.m_jointIndexForHovering);
                    break;
            }
#endif
        }
    }
}