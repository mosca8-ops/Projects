#if UNITY_EDITOR
using UnityEditor;
using TXT.WEAVR.Utility.Serialization;
#endif
using UnityEngine;
using System;

#if WEAVR_VR
using BaseClass = Valve.VR.SteamVR_Behaviour_Pose;
#else
using BaseClass = UnityEngine.MonoBehaviour;
#endif

namespace TXT.WEAVR.Interaction
{
    [AddComponentMenu("")]
    public partial class VR_Behaviour_Pose : BaseClass
    {

    }

#if UNITY_EDITOR && WEAVR_SERIALIZATION

    public partial class VR_Behaviour_Pose
    {
        [HideInInspector]
        [SerializeField]
        private string[] m_serializedInstances;
    }

    public partial class VR_Behaviour_Pose : ITargetSerializer
    {
        [NonSerialized]
        private static BuildTarget m_lastBuildTarget;
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

        private static readonly string[] wFilteredFields = new string[0];
        public string[] FilteredFields
        {
            get
            {
                return wFilteredFields;
            }
        }

        public void Awake()
        {
            if (!Application.isPlaying)
            {
                this.InitializeInstances(true);
            }
        }
    }
#endif
}
