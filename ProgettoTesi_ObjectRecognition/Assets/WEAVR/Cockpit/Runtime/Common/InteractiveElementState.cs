namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;

    [Serializable]
    [AddComponentMenu("")]
    public class InteractiveElementState : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
    {
        public delegate void SimplePointerEvent(object sender, PointerEventData data);

        public ElementState state;
        public Collider pointerCollider;
        public event SimplePointerEvent PointerUp;

        public void OnPointerUp(PointerEventData eventData) {
            if(PointerUp != null) {
                PointerUp(this, eventData);
            }
        }

        public void OnPointerDown(PointerEventData eventData) {
            
        }

        public Enum GetStateEnum(Enum sampleEnum) {
            if(sampleEnum != null) {
                foreach(Enum e in Enum.GetValues(sampleEnum.GetType())) {
                    if(e != null && e.ToString() == state.state) {
                        return e;
                    }
                }
            }
            return null;
        }
    }
}