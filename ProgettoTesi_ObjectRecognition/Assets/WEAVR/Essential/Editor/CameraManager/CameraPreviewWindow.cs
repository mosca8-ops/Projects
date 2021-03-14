using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Editor
{
    public class CameraPreviewWindow : ScriptableObject
    {
        private RenderTexture _renderTexture;
        private static CameraPreviewWindow _instance;
        private static CameraPreviewWindow Instance {
            get {
                if (!_instance) {
                    _instance = CreateInstance<CameraPreviewWindow>();
                }
                return _instance;
            }
        }

        private void OnEnable() {
            // Instantiate
        }

        private void OnDestroy() {
            if (_renderTexture) {
                _renderTexture.Release();
                DestroyImmediate(_renderTexture);
            }
        }

        public static void ShowPreview(Camera camera, Rect position) {
            ShowPreview(camera, position, 0, 0, 0);
        }

        public static void ShowPreview(Camera camera, Rect position, float aspectRatio) {
            ShowPreview(camera, position, aspectRatio, 0, 0);
        }

        public static void ShowPreview(Camera camera, Rect position, float aspectRatio, float borderWidth) {
            ShowPreview(camera, position, aspectRatio, borderWidth, 0);
        }

        public static void ShowPreview(Camera camera, Rect position, float aspectRatio, float borderWidth, float cornerRadius) {
            var window = Instance;
            if(window._renderTexture == null) {
                window._renderTexture = new RenderTexture((int)position.width, (int)position.height, 0, RenderTextureFormat.ARGB32);
            }
            else if (window._renderTexture.width != (int)position.width || window._renderTexture.height != (int)position.height) {
                window._renderTexture.Release();
                DestroyImmediate(window._renderTexture);
                window._renderTexture = new RenderTexture((int)position.width, (int)position.height, 0, RenderTextureFormat.ARGB32);
            }
            if (camera != null) {
                camera.targetTexture = window._renderTexture;
                camera.Render();
                camera.targetTexture = null;
            }
            GUI.DrawTexture(position, window._renderTexture, ScaleMode.ScaleToFit, true, aspectRatio, Color.white, borderWidth, cornerRadius);
        }
    }
}