using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [AddComponentMenu("WEAVR/Procedures/Visual Inspection/Visual Inspection Pool")]
    public class VisualInspectionPool : AbstractVisualMarkerPool<IVisualInspectionMarker>, IWeavrSingleton
    {

        #region [  STATIC PART  ]

        private static VisualInspectionPool s_currentSceneInstance;
        public static VisualInspectionPool Current
        {
            get
            {
                if (!s_currentSceneInstance)
                {
                    s_currentSceneInstance = Weavr.TryGetInCurrentScene<VisualInspectionPool>();
                }
                return s_currentSceneInstance;
            }
        }

        #endregion

        [SerializeField]
        [Draggable]
        private AbstractInspectionLogic m_defaultInspectionLogic;
        [SerializeField]
        [Draggable]
        private AbstractInspectionMarker m_markerSample;

        private List<AbstractInspectionLogic> m_targetsLists = new List<AbstractInspectionLogic>();

        public override IVisualInspectionMarker DefaultSample => m_markerSample;

        protected override IVisualInspectionMarker InstantiateNewMarker(IVisualInspectionMarker sample) => sample is Component c ? Instantiate(c) as IVisualInspectionMarker : m_markerSample ? Instantiate(m_markerSample) : null;
        



        private void OnDestroy()
        {
            if(s_currentSceneInstance == this)
            {
                s_currentSceneInstance = null;
            }
        }

        public IVisualInspectionLogic GetDefaultInspection(IVisualInspectionLogic inspection)
        {
            return inspection == null ? Instantiate(m_defaultInspectionLogic) : inspection is Component c ? Instantiate(c) as IVisualInspectionLogic : GetDefaultInspection();
        }

        private IVisualInspectionLogic GetDefaultInspection()
        {
            IVisualInspectionLogic inspection = null;
            if (m_targetsLists.Count > 0)
            {
                inspection = m_targetsLists[0];
                m_targetsLists.RemoveAt(0);
            }
            else
            {
                inspection = Instantiate(m_defaultInspectionLogic);
            }
            (inspection as Component)?.gameObject.SetActive(true);
            return inspection;
        }

        public void Reclaim(IVisualInspectionLogic inspection)
        {
            if(inspection.GetType() == m_defaultInspectionLogic.GetType() && inspection is Component c)
            {
                c.transform.SetParent(transform, true);
                c.gameObject.SetActive(false);
                m_targetsLists.Add(inspection as AbstractInspectionLogic);
            }
            else if(inspection is Component cd && cd.gameObject.scene.isLoaded)
            {
                if (Application.isPlaying)
                {
                    Destroy(cd.gameObject);
                }
                else
                {
                    DestroyImmediate(cd.gameObject);
                }
            }
        }
    }
}
