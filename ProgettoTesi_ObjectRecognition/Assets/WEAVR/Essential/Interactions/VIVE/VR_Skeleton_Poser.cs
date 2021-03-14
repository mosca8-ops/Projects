using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEditor;
using UnityEngine;
#if WEAVR_VR
using Valve.VR;
#endif

#if WEAVR_VR
using BaseClass = Valve.VR.SteamVR_Skeleton_Poser;

#else
using BaseClass = UnityEngine.MonoBehaviour;
#endif

#if UNITY_EDITOR
using TXT.WEAVR.Utility.Serialization;
#endif

namespace TXT.WEAVR.Interaction
{
    #region SerializationRegion
    public partial class VR_Skeleton_Poser
    {
        [HideInInspector]
        [SerializeField]
        private string[] m_serializedInstances;
    }

#if UNITY_EDITOR
    public partial class VR_Skeleton_Poser : ITargetSerializer
    {
        [NonSerialized]
        private BuildTarget m_lastBuildTarget = BuildTarget.NoTarget;
        [NonSerialized]
        private bool m_awakeDone = false;

        public string[] SerializedInstances
        {
            get
            {
                return m_serializedInstances;
            }
            set
            {
                m_serializedInstances = value;
            }
        }

        public BuildTarget LastBuildTarget
        {
            get
            {
                return m_lastBuildTarget;
            }
            set
            {
                m_lastBuildTarget = value;
            }
        }

        public bool UseEditorJson
        {
            get
            {
                return true;
            }
        }

        public bool AwakeDone
        {
            get
            {
                return m_awakeDone;
            }
            set
            {
                m_awakeDone = value;
            }
        }

        public TXT.WEAVR.Utility.Serialization.SerializationMode SerializationMode
        {
            get
            {
                return TXT.WEAVR.Utility.Serialization.SerializationMode.SteamVR;
            }
        }

        private static readonly string[] wFilteredFields = new string[] { "previewLeftInstance", "previewRightInstance" };
        public string[] FilteredFields
        {
            get
            {
                return wFilteredFields;
            }
        }

        public new void Awake()
        {
            if (!Application.isPlaying)
            {
                this.InitializeInstances(true);
            }
#if WEAVR_VR
            else
            {
                var wBaseAwake = TargetSerializerExtensions.GetNonVirtualMethod<BaseClass, VR_Skeleton_Poser>(this, "Awake");
                wBaseAwake();
            }
#endif            
        }
    }
#endif
    #endregion

    [ExecuteInEditMode]
    [Core.Stateless]
    [Core.DoNotExpose]
    [AddComponentMenu("WEAVR/VR/Interactions/Skeleton Poser")]
    public partial class VR_Skeleton_Poser : BaseClass
    {

      
#if WEAVR_VR
        private Transform m_LeftRotationAxis = null;
        private Transform m_LeftHandAttachmentPoint = null;
        private Transform m_InverseLeftHandAttachmentPoint = null;
        public const string c_LeftRotationAxisName = "LeftRotationAxis";
        public const string c_LeftHandAttachmentPointName = "LeftHandAttachmentPoint";
        public const string c_InverseLeftHandAttachmentPointName = "InverseLeftHandAttachmentPoint";
        private Transform m_RigthRotationAxis = null;
        private Transform m_RigthHandAttachmentPoint = null;
        private Transform m_InverseRigthHandAttachmentPoint = null;
        public const string c_RigthRotationAxisName = "RigthRotationAxis";
        public const string c_RigthHandAttachmentPointName = "RigthHandAttachmentPoint";
        public const string c_InverseRigthHandAttachmentPointName = "InverseRigthHandAttachmentPoint";


        public virtual Transform GetAttachmentPoint(SteamVR_Input_Sources iSource, bool iInverse)
        {
            Transform wRet = null;
            switch (iSource)
            {
                case SteamVR_Input_Sources.LeftHand:
                    wRet = iInverse ? m_InverseLeftHandAttachmentPoint : m_LeftHandAttachmentPoint;
                    break;
                case SteamVR_Input_Sources.RightHand:
                    wRet = iInverse ? m_InverseRigthHandAttachmentPoint : m_RigthHandAttachmentPoint;
                    break;
                default:
                    break;
            }
            return wRet;
        }

        private void SetUpAttachmentPoint(ref Transform iAttachmentPoint, string iName)
        {
            iAttachmentPoint = transform.Find(iName);
            if (iAttachmentPoint != null)
            {
                VR_Skeleton_PoserUpdater wLeftPoser = iAttachmentPoint.GetComponent<VR_Skeleton_PoserUpdater>();
                if (wLeftPoser != null)
                {
                    Destroy(wLeftPoser);
                }
            }
        }

   
        public virtual void Start()
        {
            if (Application.isPlaying)
            {
                SetUpAttachmentPoint(ref m_LeftHandAttachmentPoint, c_LeftRotationAxisName + "/" + c_LeftHandAttachmentPointName);
                SetUpAttachmentPoint(ref m_RigthHandAttachmentPoint, c_RigthRotationAxisName + "/" + c_RigthHandAttachmentPointName);
                if (m_RigthHandAttachmentPoint == null)
                {
                    SetUpAttachmentPoint(ref m_RigthHandAttachmentPoint, c_RigthHandAttachmentPointName);
                }
                if (m_LeftHandAttachmentPoint == null)
                {
                    SetUpAttachmentPoint(ref m_LeftHandAttachmentPoint, c_LeftHandAttachmentPointName);
                }
                SetUpAttachmentPoint(ref m_InverseLeftHandAttachmentPoint, c_InverseLeftHandAttachmentPointName);
                SetUpAttachmentPoint(ref m_InverseRigthHandAttachmentPoint, c_InverseRigthHandAttachmentPointName);
            }
        }

#if UNITY_EDITOR

        private void HideAttachmentHelpers()
        {
            Transform wRet = transform.Find(VR_Skeleton_Poser.c_LeftRotationAxisName);
            if (wRet)
            {
                wRet.gameObject.hideFlags = HideFlags.HideInHierarchy;
            }
            wRet = transform.Find(VR_Skeleton_Poser.c_RigthRotationAxisName);
            if (wRet)
            {
                wRet.gameObject.hideFlags = HideFlags.HideInHierarchy;
            }
            List<GameObject> wToDestroy = new List<GameObject>();
            foreach (Transform wChild in transform)
            {
                if (wChild.GetComponent<SteamVR_Behaviour_Skeleton>() != null)
                {
                    wToDestroy.Add(wChild.gameObject);
                }
            }
            foreach (GameObject wToBeDestroyed in wToDestroy)
            {
                DestroyImmediate(wToBeDestroyed);
            }
        }
        public void OnDestroy() 
        {
            if (this)
            {
                Selection.selectionChanged -= this.HandleSelectionChanged;
            }
            HideAttachmentHelpers();   
        }
         
        public void HandleSelectionChanged()
        {
            if(!this || !transform) { return; }

            if (!Selection.activeGameObject ||
                (!System.Object.ReferenceEquals(Selection.activeGameObject.transform, transform) &&
                 !Selection.activeGameObject.transform.IsChildOf(transform)))
            {
                GetType().GetField("showLeftPreview", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this, false);
                GetType().GetField("showRightPreview", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this, false);
                HideAttachmentHelpers();
                this.UpdateInstance(/*wFilteredFields*/);
                Selection.selectionChanged -= this.HandleSelectionChanged;
            }
        }
#endif //UNITY_EDITOR
#endif //WEAVR_VR
    };
}

