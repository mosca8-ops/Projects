using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.AR
{
    [AddComponentMenu("")]
    public class ARObject : MonoBehaviour
    {
        public delegate void ARObjectDelegate(GameObject target);

        #region [  STATIC PART  ]

        private static List<ARObject> s_allObjects = new List<ARObject>();
        public static IReadOnlyList<ARObject> AllObjects => s_allObjects;

        private static ARObject s_global;
        public static ARObject Global
        {
            get
            {
                if (!s_global)
                {
                    s_global = new GameObject("Global_AR_Object").AddComponent<ARObject>();
                    s_global.gameObject.hideFlags = HideFlags.DontSave;
                }
                return s_global;
            }
        }

        public static bool IsGlobalAlive => s_global;

        private static void CleanupObjectsList()
        {
            for (int i = 0; i < s_allObjects.Count; i++)
            {
                if (!s_allObjects[i])
                {
                    s_allObjects.RemoveAt(i--);
                }
            }
        }

        #endregion

        [SerializeField]
        private GameObject m_target;
        [SerializeField]
        private AROptions m_options;

        public GameObject Target
        {
            get => m_target;
            set
            {
                if(m_target != value)
                {
                    m_target = value;
                    TargetChanged?.Invoke(m_target);
                }
            }
        }

        public AROptions Options { get => m_options; set => m_options = value; }

        public event ARObjectDelegate TargetChanged;

        private void Awake()
        {
            CleanupObjectsList();
            if (!s_allObjects.Contains(this))
            {
                s_allObjects.Add(this);
            }
        }

        private void OnDestroy()
        {
            s_allObjects.Remove(this);
        }

        public void SetTarget(GameObject target, AROptions options)
        {
            Options = options;
            Target = target;
        }

        [Serializable]
        public struct AROptions
        {
            public bool useLineToSurface;
            public Gradient lineGradient;
            public bool use3DAxes;
        }
    } 
}
