using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.UI
{
    [RequireComponent(typeof(ContentSizeFitter))]
    [AddComponentMenu("WEAVR/UI/Fit Content Fixer")]
    public class ContentSizeFitterUpdater : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        private int m_updateCount;

        private void OnValidate()
        {
            m_updateCount = GetComponentsInChildren<ContentSizeFitter>(true).Length;
        }

        void OnEnable()
        {
            for (int i = 0; i < m_updateCount; i++)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
            }
        }       
    }
}
