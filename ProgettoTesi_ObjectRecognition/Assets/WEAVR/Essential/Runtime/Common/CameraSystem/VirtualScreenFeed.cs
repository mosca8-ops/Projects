using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("")]
    public class VirtualScreenFeed : Button
    {

        [Space]
        [SerializeField]
        [Draggable]
        protected ScrollRect m_scroller;

        [SerializeField]
        protected UnityEvent m_onSelected;

        public UnityEvent OnSelected => m_onSelected;

        protected RectTransform m_trasform;

        //protected override void Reset()
        //{
        //    base.Reset();
        //    m_scroller = GetComponentInParent<ScrollRect>();
        //}

        protected override void Start()
        {
            base.Start();
            m_trasform = transform as RectTransform;
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            float viewportWidth = m_scroller.viewport.rect.width;
            float viewportHeight = m_scroller.viewport.rect.height;
            if (viewportWidth != 0)
            {
                m_scroller.normalizedPosition = new Vector2(m_trasform.localPosition.x / viewportWidth, m_scroller.normalizedPosition.y);
            }
            m_onSelected.Invoke();
        }

        //   // Update is called once per frame
        //   void Update () {

        //}
    }
}