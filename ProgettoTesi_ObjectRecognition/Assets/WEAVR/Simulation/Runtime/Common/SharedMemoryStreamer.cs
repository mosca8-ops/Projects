namespace TXT.WEAVR.Simulation
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [RequireComponent(typeof(Renderer))]
    [AddComponentMenu("WEAVR/Simulation/Shared Memory Streamer")]
    public class SharedMemoryStreamer : MonoBehaviour
    {
        [Tooltip("The id of the shared memory to access the image bytes from")]
        public string memoryId;

        [Header("Texture Properties")]
        [Tooltip("Material of the screen")]
        public Material screenMaterial;
        [Tooltip("The size of the texture to stream")]
        public Vector2Int textureSize = new Vector2Int(640, 480);
        [Tooltip("The format of the texture pixel")]
        public TextureStreamFormat textureFormat = TextureStreamFormat.RGB24;
        [Tooltip("The texture to show in case of error")]
        public Texture2D errorTexture;

        [Header("Other Properties")]
        [Tooltip("Whether to update texture in fixed update loop, or in normal one")]
        public bool fixedUpdate = true;
        [Tooltip("Whether to start streaming immediately on start or triggered manually later")]
        public bool playOnStart = true;
        public bool IsPlaying { get; set; }

        private SharedMemoryManager.DisplayHandler _displayHandler;
        private Renderer _renderer;
        private bool _initialized = false;
        private bool _currentlyInError = false;

        // Use this for initialization
        void Start() {
            if (string.IsNullOrEmpty(memoryId)) {
                Debug.LogErrorFormat("[{0}].SharedMemoryStreamer: Memory Id is not set", gameObject.name);
                IsPlaying = false;
                return;
            }

            _renderer = GetComponent<Renderer>();
            _currentlyInError = true;
            _renderer.material.mainTexture = errorTexture;

            InitializeDisplayHandler();
            IsPlaying = playOnStart && _displayHandler != null;
        }

        private void OnValidate() {
            if(screenMaterial == null) {
                screenMaterial = GetComponent<Renderer>().sharedMaterial;
            }
        }

        private void InitializeDisplayHandler() {
            if (_displayHandler == null) {
                _displayHandler = SharedMemoryManager.Instance.GetDisplayHandler(memoryId, textureSize, textureFormat);
            }
            if (_displayHandler != null) {
                if (screenMaterial != _renderer.material) {
                    _renderer.material = screenMaterial;
                }
                _renderer.material.mainTexture = _displayHandler.Texture;
                _initialized = true;
                _currentlyInError = false;
            }
        }

        // Update is called once per frame
        void Update() {
            if (!_initialized) { InitializeDisplayHandler(); }
            if (!fixedUpdate && IsPlaying) {
                UpdateTexture();
            }
        }

        private void FixedUpdate() {
            if (!_initialized) { InitializeDisplayHandler(); }
            if (fixedUpdate && IsPlaying) {
                UpdateTexture();
            }
        }

        private void UpdateTexture() {
            if (!_displayHandler.UpdateTexture()) {
                _currentlyInError = true;
                _renderer.material.mainTexture = errorTexture;
            }
            else if (_currentlyInError) {
                _currentlyInError = false;
                _renderer.material.mainTexture = _displayHandler.Texture;
            }
        }
    }
}