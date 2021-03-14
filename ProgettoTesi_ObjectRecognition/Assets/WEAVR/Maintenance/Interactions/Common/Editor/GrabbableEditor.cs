#if UNITY_EDITOR
using TXT.WEAVR.Maintenance;
using UnityEditor;
using UnityEngine;
#endif

#if WEAVR_VR
using Valve.VR;

namespace TXT.WEAVR.Interaction
{
    [CustomEditor(typeof(Grabbable))]
    public class GrabbableEditor : UnityEditor.Editor
    {
        private Grabbable m_self;

        private GameObject CreateControllerPreview(Transform iParent)
        {
            GameObject wRet = null;
            string[] defaultLeftPaths = AssetDatabase.FindAssets(string.Format("t:Prefab {0}", "HTC_Controller"));
            if (defaultLeftPaths != null && defaultLeftPaths.Length > 0)
            {
                string defaultLeftGUID = defaultLeftPaths[0];
                string defaultLeftPath = AssetDatabase.GUIDToAssetPath(defaultLeftGUID);
                wRet = GameObject.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(defaultLeftPath));
                wRet.transform.parent = iParent.transform;
                wRet.transform.position = iParent.position;
                wRet.transform.rotation = iParent.rotation;
            }
            return wRet;
        }

        public void Awake()
        {
            m_self = target as Grabbable;
            Selection.selectionChanged += m_self.HandleSelectionChanged;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();


            VR_Skeleton_Poser wPoser = m_self.GetComponent<VR_Skeleton_Poser>();
            if (wPoser)
            {
                m_self.m_handIsFree = EditorGUILayout.Toggle("Hand is Free", m_self.m_handIsFree);
                if (m_self.m_handIsFree)
                {
                    m_self.m_rotationAxis = EditorGUILayout.Vector3Field("Rotation Axis Transform", m_self.m_rotationAxis);
                }
                if (m_self.m_ControllerAttachmentPoint != null)
                {
                    m_self.HideControllerPreview();
                    m_self.m_ControllerAttachmentPoint.hideFlags = HideFlags.HideInHierarchy;
                }
            }
            else
            {
                m_self.m_showControllerPreview = EditorGUILayout.Toggle("Show Controller Preview", m_self.m_showControllerPreview);
                m_self.m_ControllerAttachmentPoint = (Transform) EditorGUILayout.ObjectField("Controller Attachment Point", m_self.m_ControllerAttachmentPoint, typeof(Transform), true);
                if (m_self.m_ControllerAttachmentPoint == null)
                {
                    GameObject wGameObject = CreateControllerPreview(m_self.transform);
                    if (wGameObject != null)
                    {
                        wGameObject.transform.parent = m_self.transform;
                        wGameObject.transform.localPosition = new Vector3();
                        wGameObject.transform.localRotation = new Quaternion();
                        wGameObject.name = "ControllerAttachmentPoint";
                        m_self.m_ControllerAttachmentPoint = wGameObject.transform;
                    }              
                }
                if (m_self.m_showControllerPreview)
                {
                    m_self.ShowControllerPreview();
                }
                else
                {
                    m_self.HideControllerPreview();
                }
            }


        }

    }
}
#endif