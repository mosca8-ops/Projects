using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Procedure
{
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    [AddComponentMenu("WEAVR/Components/Billboard Progress Bar")]
    public class BillboardProgressBar : MonoBehaviour, IActiveProgressElement
    {
        [SerializeField]
        [HideInInspector]
        private Image m_imageComponent;

        public float Progress
        {
            get => m_imageComponent?.fillAmount ?? 0;
            set
            {
                if(m_imageComponent && m_imageComponent.fillAmount != value)
                {
                    m_imageComponent.fillAmount = value;
                }
            }
        }

        public void ResetProgress()
        {
            Progress = 0;
        }

        private void Awake()
        {
            if (!m_imageComponent)
            {
                m_imageComponent = GetComponent<Image>();
                m_imageComponent.type = Image.Type.Filled;
            }
        }
    }
}
