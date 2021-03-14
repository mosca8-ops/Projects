namespace TXT.WEAVR.Simulation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("WEAVR/Simulation/Shared Memory Output")]
    public class SharedMemoryStreamOutput : MonoBehaviour
    {
        public string memoryId = "CameraOutput";
        public RenderTexture renderTexture;

        private IntPtr _fileHandle;
        private IntPtr _viewHandle;

        // Use this for initialization
        void Awake() {
            //_camera = GetComponent<Camera>();

        }

        // Update is called once per frame
        void Update() {

        }
    }
}