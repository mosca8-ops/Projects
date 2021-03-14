using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Components/Selectable")]
    public class SelectableObject : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private static SelectableObject _currentlySelected;
        
        public Color borderColor = Color.cyan;

        private bool _isSelected;
        public bool IsSelected {
            get {
                return _isSelected;
            }
            set {
                if(_isSelected != value) {
                    _isSelected = value;
                    if (value) {
                        if(_currentlySelected != null && _currentlySelected != this) {
                            _currentlySelected.IsSelected = false;
                        }
                        _currentlySelected = this;
                        Outliner.Outline(gameObject, borderColor);
                    }
                    else {
                        Outliner.RemoveOutline(gameObject);
                    }
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData) {

        }

        public void OnPointerUp(PointerEventData eventData) {
            IsSelected = !IsSelected;
        }

        // Use this for initialization
        void Start() {
            IsSelected = false;
        }
    }
}