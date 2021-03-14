using System;
using System.Reflection;
using TXT.WEAVR.Editor;
using TXT.WEAVR.Utility.Serialization;
using UnityEditor;
using UnityEngine;


#if WEAVR_VR
using Valve.VR;
using BaseClass = Valve.VR.SteamVR_Skeleton_PoserEditor;
#else
using BaseClass = UnityEditor.Editor;
#endif

namespace TXT.WEAVR.Interaction
{
    [CustomEditor(typeof(VR_Skeleton_Poser))]
    public class VR_Skeleton_PoserEditor : BaseClass
    {

        private VR_Skeleton_Poser m_self = null;
        private bool mAwakeDone = false;
        
        public void Awake()
        {
            Debug.Log("Awake Editor");
            m_self = target as VR_Skeleton_Poser;
            m_self.InitializeInstances(true);
#if WEAVR_VR
            Selection.selectionChanged += m_self.HandleSelectionChanged;
#endif
        }

        public void OnDisable()
        {
            if (!Application.isPlaying && m_self != null)
            {
                m_self.UpdateInstance();
            }
        }


#if WEAVR_VR

        private static Transform CreateTransformIfNotExists(Transform iParent, string iName)
        {
            Transform wRet = iParent.Find(iName);
            if (wRet == null)
            {
                GameObject wGameObject = new GameObject();
                wGameObject.transform.parent = iParent;
                wGameObject.transform.localPosition = new Vector3();
                wGameObject.transform.localRotation = new Quaternion();
                wGameObject.name = iName;
                wRet = wGameObject.transform;
            }
            else if (wRet.hideFlags == HideFlags.HideInHierarchy)
            {
                wRet.hideFlags = HideFlags.None;
            }
            return wRet;
        }
        private void UpdateLeftAttachmentTransform(Transform iUpdatedTransform)
        {
            Transform wLeftRotationAxis = CreateTransformIfNotExists(m_self.transform, VR_Skeleton_Poser.c_LeftRotationAxisName);
            Transform wLeftHandAttachmentPoint = m_self.transform.Find(VR_Skeleton_Poser.c_LeftHandAttachmentPointName);
            if (wLeftHandAttachmentPoint != null)
            {
                wLeftHandAttachmentPoint.transform.parent = wLeftRotationAxis;
            }
            else
            {
                wLeftHandAttachmentPoint = CreateTransformIfNotExists(wLeftRotationAxis, VR_Skeleton_Poser.c_LeftHandAttachmentPointName);
            }

            VR_Skeleton_PoserUpdater wTransformUpdater = wLeftHandAttachmentPoint.gameObject.GetComponent<VR_Skeleton_PoserUpdater>();
            if (wTransformUpdater == null)
            {
                wTransformUpdater = wLeftHandAttachmentPoint.gameObject.AddComponent<VR_Skeleton_PoserUpdater>();
            }

            Transform wInverseLeftHandAttachmentPoint = CreateTransformIfNotExists(m_self.transform, VR_Skeleton_Poser.c_InverseLeftHandAttachmentPointName);
            VR_Skeleton_PoserUpdater wInverseTransformUpdater = wInverseLeftHandAttachmentPoint.gameObject.GetComponent<VR_Skeleton_PoserUpdater>();
            if (wInverseTransformUpdater == null)
            {
                wInverseTransformUpdater = wInverseLeftHandAttachmentPoint.gameObject.AddComponent<VR_Skeleton_PoserUpdater>();
            }
            if (wInverseLeftHandAttachmentPoint.hideFlags != HideFlags.HideInHierarchy)
            {
                wInverseLeftHandAttachmentPoint.hideFlags = HideFlags.HideInHierarchy;
                EditorApplication.RepaintHierarchyWindow();
                EditorApplication.DirtyHierarchyWindowSorting();
            }

            Transform wLocalAttachmentPointTransform = CreateTransformIfNotExists(iUpdatedTransform, VR_Skeleton_Poser.c_InverseLeftHandAttachmentPointName);

            wTransformUpdater.setReferenceTransform(iUpdatedTransform, false);
            wInverseTransformUpdater.setReferenceTransform(wLocalAttachmentPointTransform, true);
            wTransformUpdater.enabled = true;
            wInverseTransformUpdater.enabled = true;
            wTransformUpdater.UpdateTransform();
            wInverseTransformUpdater.UpdateTransform();
        }

        private void UpdateRigthAttachmentTransform(Transform iUpdatedTransform)
        {
            Transform wRigthRotationAxis = CreateTransformIfNotExists(m_self.transform, VR_Skeleton_Poser.c_RigthRotationAxisName);
            Transform wRigthHandAttachmentPoint = m_self.transform.Find(VR_Skeleton_Poser.c_RigthHandAttachmentPointName);
            if (wRigthHandAttachmentPoint != null)
            {
                wRigthHandAttachmentPoint.transform.parent = wRigthRotationAxis;
            }
            else
            {
                wRigthHandAttachmentPoint = CreateTransformIfNotExists(wRigthRotationAxis, VR_Skeleton_Poser.c_RigthHandAttachmentPointName);
            }

            VR_Skeleton_PoserUpdater wTransformUpdater = wRigthHandAttachmentPoint.gameObject.GetComponent<VR_Skeleton_PoserUpdater>();
            if (wTransformUpdater == null)
            {
                wTransformUpdater = wRigthHandAttachmentPoint.gameObject.AddComponent<VR_Skeleton_PoserUpdater>();
            }

            Transform wInverseRigthHandAttachmentPoint = CreateTransformIfNotExists(m_self.transform, VR_Skeleton_Poser.c_InverseRigthHandAttachmentPointName);
            VR_Skeleton_PoserUpdater wInverseTransformUpdater = wInverseRigthHandAttachmentPoint.gameObject.GetComponent<VR_Skeleton_PoserUpdater>();
            if (wInverseTransformUpdater == null)
            {
                wInverseTransformUpdater = wInverseRigthHandAttachmentPoint.gameObject.AddComponent<VR_Skeleton_PoserUpdater>();
            }
            if (wInverseRigthHandAttachmentPoint.hideFlags != HideFlags.HideInHierarchy)
            {
                wInverseRigthHandAttachmentPoint.hideFlags = HideFlags.HideInHierarchy;
                EditorApplication.RepaintHierarchyWindow();
                EditorApplication.DirtyHierarchyWindowSorting();
            }
            Transform wLocalAttachmentPointTransform = CreateTransformIfNotExists(iUpdatedTransform, VR_Skeleton_Poser.c_InverseRigthHandAttachmentPointName);

            wTransformUpdater.setReferenceTransform(iUpdatedTransform, false);
            wInverseTransformUpdater.setReferenceTransform(wLocalAttachmentPointTransform, true);
            wTransformUpdater.enabled = true;
            wInverseTransformUpdater.enabled = true;

            wTransformUpdater.UpdateTransform();
            wInverseTransformUpdater.UpdateTransform();
        }


        private void LogHierarchy(Transform iGameObject, int iLevel)
        {
            foreach (Transform wTransform in iGameObject)
            {
                Debug.Log(new String('\t', iLevel) + wTransform.name);
                LogHierarchy(wTransform, iLevel + 1);
            }
        }

        private void FbxToPose()
        {
            string selected = EditorUtility.OpenFilePanel("Open Skeleton Pose ScriptableObject", Application.dataPath, "fbx");
            selected = selected.Replace(Application.dataPath, "Assets");

            if (selected == null) return;

            var wAsset = AssetDatabase.LoadMainAssetAtPath(selected);
            if (wAsset != null && wAsset is GameObject wGO)
            {
                SteamVR_Skeleton_Pose activePose = (SteamVR_Skeleton_Pose) PrivateValueAccessor.GetPrivateFieldValue(typeof(SteamVR_Skeleton_PoserEditor), "activePose", this);
                FieldInfo forceUpdateHandsFieldInfo = PrivateValueAccessor.GetPrivateFieldInfo(typeof(SteamVR_Skeleton_PoserEditor), "forceUpdateHands");
                SteamVR_Skeleton_Pose wPose = new SteamVR_Skeleton_Pose();
                activePose.BonesFromFbx(wGO.transform);
                forceUpdateHandsFieldInfo.SetValue(this, true);
            }
            else
            {
                EditorUtility.DisplayDialog("WARNING", "Asset could not be loaded. Is it not a FBX?", "ok");
                return;
            }    
        }
#endif

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
#if WEAVR_VR
            if (GUILayout.Button("Import fbx"))
            {
                FbxToPose();
            }
            for (int i = 0; i < m_self.transform.childCount; i++)
            {
                Transform wChild = m_self.transform.GetChild(i);
                SteamVR_Behaviour_Skeleton wSkeleton = wChild.GetComponent<SteamVR_Behaviour_Skeleton>();
                if (wSkeleton != null)
                {
                    switch (wSkeleton.inputSource)
                    {
                        case SteamVR_Input_Sources.LeftHand:
                            UpdateLeftAttachmentTransform(wChild.transform);
                            break;
                        case SteamVR_Input_Sources.RightHand:
                            UpdateRigthAttachmentTransform(wChild.transform);
                            break;
                        default:
                            break;
                    }
                }
            }
#endif
            m_self.UpdateInstance();
        }
    }
}
