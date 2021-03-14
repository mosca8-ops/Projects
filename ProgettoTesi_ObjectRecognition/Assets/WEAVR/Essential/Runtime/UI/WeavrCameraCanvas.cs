using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.UI
{
    [RequireComponent(typeof(Canvas))]
    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/UI/WEAVR Camera Canvas")]
    public class WeavrCameraCanvas : MonoBehaviour
    {
        [Tooltip("Whether to allow to reset the camera to null or not")]
        public bool allowNullCamera = true;

        private void OnEnable()
        {
            if (WeavrCamera.CurrentCamera)
            {
                SetCanvasWorldCamera(WeavrCamera.CurrentCamera);
            }
            WeavrCamera.CameraChanged -= SetCanvasWorldCamera;
            WeavrCamera.CameraChanged += SetCanvasWorldCamera;
        }

        private void Start()
        {
            // This is a failsafe approach when setting the camera
            OnEnable();
        }

        private void OnDisable()
        {
            WeavrCamera.CameraChanged -= SetCanvasWorldCamera;
        }

        private void SetCanvasWorldCamera(Camera camera)
        {
            var canvas = GetComponent<Canvas>();
            if (canvas && (allowNullCamera || camera))
            {
                canvas.worldCamera = camera;
            }
        }
    }
}
