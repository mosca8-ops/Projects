using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TXT.WEAVR.Common
{ 
    [Obsolete("Use Executable instead")]
    [AddComponentMenu("")]
    public class ClickableObject : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        //[DynamicEnum(() => "")]
        public bool IsClicked { get; private set; }

        private bool _wasClicked;

        public void OnPointerDown(PointerEventData eventData) {


        }

        public void Click()
        {
            _wasClicked = true;
        }

        public void OnPointerUp(PointerEventData eventData) {
            _wasClicked = true;
        }

        private void OnEnable()
        {
            _wasClicked = false;
        }

        private void OnDisable()
        {
            _wasClicked = false;
        }

        // Use this for initialization
        void Start() {
            IsClicked = false;
            _wasClicked = false;
        }

        // Update is called once per frame
        void Update() {
            IsClicked = _wasClicked;
            _wasClicked = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _wasClicked = true;
        }
    }
}