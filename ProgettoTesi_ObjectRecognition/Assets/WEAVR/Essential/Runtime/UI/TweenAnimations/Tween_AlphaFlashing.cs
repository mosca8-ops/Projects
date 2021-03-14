using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Tweening
{

    [AddComponentMenu("WEAVR/UI/Animations/Alpha Flashing")]
    public class Tween_AlphaFlashing : MonoBehaviour
    {
        [Range(0, 1)]
        public float minAlpha = 0;
        [Range(0, 1)]
        public float maxAlpha = 1;
        [Range(0.1f, 10)]
        public float duration = 1;

        private CanvasGroup m_group;
        private Graphic m_graphic;

        private float m_deltaMove;
        private float m_target;

        public float Alpha
        {
            get => m_group ? m_group.alpha : m_graphic ? m_graphic.color.a : 0;
            set
            {
                if (m_group)
                {
                    m_group.alpha = value;
                }
                if (m_graphic)
                {
                    var color = m_graphic.color;
                    color.a = value;
                    m_graphic.color = color;
                }
            }
        }

        void Awake()
        {
            m_group = GetComponent<CanvasGroup>();
            if (!m_group)
            {
                m_graphic = GetComponent<Graphic>();
            }
            m_deltaMove = 1 / duration;
        }

        private void OnEnable()
        {
            Alpha = maxAlpha;
            m_target = minAlpha;
        }

        // Update is called once per frame
        void Update()
        {
            if(Alpha >= maxAlpha)
            {
                m_target = minAlpha;
            }
            else if(Alpha <= minAlpha)
            {
                m_target = maxAlpha;
            }
            Alpha = Mathf.MoveTowards(Alpha, m_target, Time.deltaTime * m_deltaMove);
        }
    }
}
