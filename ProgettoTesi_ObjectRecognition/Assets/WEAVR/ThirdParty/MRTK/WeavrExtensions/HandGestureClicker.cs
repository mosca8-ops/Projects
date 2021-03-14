#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
namespace TXT.WEAVR.InteractionUI
{
    using Common;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.XR.WSA.Input;

    public class HandGestureClicker : MonoBehaviour, IPointerHandler
    {
        public PointerManager pointerManager;

        private GestureRecognizer _gestureRecognizer;
        private InteractionSource? _firstHandSource;
        private InteractionSource? _secondHandSource;

        private IInteractiveObject _currentClickable;

        private void OnValidate() {
            //if(pointerManager == null) {
            //    Debug.LogError("Pointer manager is not set");
            //}
        }

        public void PointerAction(InteractionType type, Vector3 pointerPosition) {
            if(_currentClickable != null && (_firstHandSource.HasValue || _secondHandSource.HasValue)) {
                _currentClickable.Interact(InteractionType.PointerDown);
            }
        }

        // Use this for initialization
        void Start() {
            InteractionManager.InteractionSourceDetected += InteractionManager_InteractionSourceDetected;
            InteractionManager.InteractionSourceLost += InteractionManager_InteractionSourceLost;
            InteractionManager.InteractionSourcePressed += InteractionManager_InteractionSourcePressed;
            InteractionManager.InteractionSourceReleased += InteractionManager_InteractionSourceReleased;

            _gestureRecognizer = new GestureRecognizer();
            _gestureRecognizer.SetRecognizableGestures(GestureSettings.Tap);
            _gestureRecognizer.Tapped += GestureRecognizer_Tapped;

            if(pointerManager != null) {
                pointerManager.ClickableChanged += PointerManager_ClickableChanged;
            }
        }

        private void PointerManager_ClickableChanged(IInteractiveObject previous, IInteractiveObject current) {
            _currentClickable = current;
        }

        private void GestureRecognizer_Tapped(TappedEventArgs obj) {
            // TODO: TO BE IMPLEMENTED...
        }

        private void InteractionManager_InteractionSourceReleased(InteractionSourceReleasedEventArgs obj) {
            // TODO: TO BE IMPLEMENTED...
        }

        private void InteractionManager_InteractionSourcePressed(InteractionSourcePressedEventArgs obj) {
            if(_currentClickable != null && obj.state.source.kind == InteractionSourceKind.Hand) {
                if ((_firstHandSource.HasValue && _firstHandSource.Value.id == obj.state.source.id) ||
                    (_secondHandSource.HasValue && _secondHandSource.Value.id == obj.state.source.id)) {
                    _currentClickable.Interact(InteractionType.PointerDown);
                }
            }
        }

        private void InteractionManager_InteractionSourceLost(InteractionSourceLostEventArgs obj) {
            if (obj.state.source.kind == InteractionSourceKind.Hand) {
                if (_firstHandSource.HasValue && _firstHandSource.Value.id == obj.state.source.id) {
                    _firstHandSource = null;
                } else if (_secondHandSource.HasValue && _secondHandSource.Value.id == obj.state.source.id) {
                    _secondHandSource = null;
                }
            }
        }

        private void InteractionManager_InteractionSourceDetected(InteractionSourceDetectedEventArgs obj) {
            if(obj.state.source.kind == InteractionSourceKind.Hand) {
                if (!_firstHandSource.HasValue) {
                    _firstHandSource = obj.state.source;
                } else if (!_secondHandSource.HasValue) {
                    _secondHandSource = obj.state.source;
                }
            }
        }

        private void OnDestroy() {
            InteractionManager.InteractionSourceDetected -= InteractionManager_InteractionSourceDetected;
            InteractionManager.InteractionSourceLost -= InteractionManager_InteractionSourceLost;
            InteractionManager.InteractionSourcePressed -= InteractionManager_InteractionSourcePressed;
            InteractionManager.InteractionSourceReleased -= InteractionManager_InteractionSourceReleased;
        }
    }
}
#endif
