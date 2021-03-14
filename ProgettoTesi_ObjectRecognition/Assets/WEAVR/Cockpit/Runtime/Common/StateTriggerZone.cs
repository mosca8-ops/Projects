namespace TXT.WEAVR.Cockpit
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;

    [AddComponentMenu("")]
    public class StateTriggerZone : MonoBehaviour, IPointerDownHandler, IPointerUpHandler/*, IPointerEnterHandler, IPointerExitHandler*/
    {
        [HideInInspector]
        public BaseDiscreteState state;
        [SerializeField]
        [HideInInspector]
        private Collider _collider;
        public Collider Collider {
            get {
                if(_collider == null) {
                    _collider = GetComponent<Collider>();
                }
                return _collider;
            }
            set {
                _collider = value;
            }
        }

        public void OnPointerDown(PointerEventData eventData) {
            if (!eventData.used && state.OnPointerDown(eventData)) { eventData.Use(); }
        }

        //public void OnPointerEnter(PointerEventData eventData) {
        //    if (!eventData.used && state.OnPointerEnter(eventData)) { eventData.Use(); }
        //}

        //public void OnPointerExit(PointerEventData eventData) {
        //    if (!eventData.used && state.OnPointerExit(eventData)) { eventData.Use(); }
        //}

        public void OnPointerUp(PointerEventData eventData) {
            if (!eventData.used && state.OnPointerUp(eventData)) { eventData.Use(); }
        }

        private void OnValidate() {
            if(state != null && state.triggerZone != this) {
                Delete();
            }
        }

        public void Delete() {
            // Destroy this component, or gameobject if empty
            if (transform.childCount == 0 && GetComponent<MeshRenderer>() == null) {
                DestroyImmediate(gameObject);
            }
            else {
                DestroyImmediate(this);
            }
        }
    }
}