using System;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Core
{

    public class CameraChangedEvent : UnityEvent<Camera> { }

    [Stateless]
    [AddComponentMenu("WEAVR/Setup/WEAVR Manager")]
    public class WeavrManager : MonoBehaviour
    {
        #region [  CONST PART  ]

        public const string kSymbols_VR = "WEAVR_VR";
        public const string kSymbols_NETWORK = "WEAVR_NETWORK";
        public const string kSymbols_VUFORIA = "WEAVR_VUFORIA";

        #endregion

        #region [  STATIC PART  ]
        private static WeavrManager m_main;

        /// <summary>
        /// Gets the instantiated Weavr Manager
        /// </summary>
        public static WeavrManager Main {
            get {
                if (!m_main)
                {
                    m_main = FindObjectOfType<WeavrManager>();
                    //if (m_main == null && Application.isPlaying) {
                    //    // Create a default one
                    //    var go = new GameObject("WeavrManager");
                    //    m_main = go.AddComponent<WeavrManager>();
                    //}
                    m_main?.Awake();
                }
                return m_main;
            }
        }

        #endregion

        public enum Platform { VR, Touch, PC, Hololens }

        [Header("Global Objects")]
        [SerializeField]
        [Tooltip("The currently playing camera")]
        [Draggable]
        protected Camera m_mainCamera;

        [Header("Flags")]
        [SerializeField]
        [Tooltip("Whether to use world pointer or alternative input methods")]
        [KeyBinding(nameof(m_useWorldPointerKey))]
        protected bool m_useWorldPointer = true;

        [SerializeField]
        [Tooltip("Whether to use teleport")]
        [KeyBinding(nameof(m_useTeleportKey))]
        protected bool m_useTeleport = true;

        [SerializeField]
        [Tooltip("Whether to show highlights when interacting with objects")]
        [KeyBinding(nameof(m_interactioneHighlightsKey))]
        protected bool m_interactionHighlights = true;

        [SerializeField]
        [Tooltip("Whether interaction hints should be visible or not")]
        [KeyBinding(nameof(m_showHintsKey))]
        protected bool m_showHints = true;

        [SerializeField]
        [Tooltip("Whether billboards should be visible or not")]
        [KeyBinding(nameof(m_showBillboardKey))]
        protected bool m_showBillboards = true;

        [SerializeField]
        [Tooltip("Whether navigation to where the hints are should be visible or not")]
        [KeyBinding(nameof(m_showNavigationKey))]
        protected bool m_showNavigationHints = true;

        [SerializeField]
        protected Platform m_currentPlatform;
        public Platform CurrentPlatform { get { return m_currentPlatform; } }

        [Header("Events")]
        public CameraChangedEvent onCameraChange;

        public static Camera DefaultCamera {
            get { return Main ? Main.Camera : null; }
            set {
                if (Main)
                {
                    Main.Camera = value;
                }
            }
        }

        private Camera Camera {
            get { return m_mainCamera; }
            set
            {
                if (m_mainCamera != value)
                {
                    m_mainCamera = value;
                }
            }
        }

        public static bool UseWorldPointer { get { return Main ? Main.m_useWorldPointer : false; } }

        public static bool UseTeleport { get { return Main ? Main.m_useTeleport : false; } }


        // Key bindings....
        #region [  KEY BINDINGS  ]

        [SerializeField]
        [HideInInspector]
        protected KeyBinding m_useWorldPointerKey = new KeyBinding(KeyCode.P);
        [SerializeField]
        [HideInInspector]
        protected KeyBinding m_useTeleportKey = new KeyBinding(KeyCode.T);
        [SerializeField]
        [HideInInspector]
        protected KeyBinding m_interactioneHighlightsKey = new KeyBinding(KeyCode.I);
        [SerializeField]
        [HideInInspector]
        protected KeyBinding m_showHintsKey = new KeyBinding(KeyCode.H);
        [SerializeField]
        [HideInInspector]
        protected KeyBinding m_showBillboardKey = new KeyBinding(KeyCode.B);
        [SerializeField]
        [HideInInspector]
        protected KeyBinding m_showNavigationKey = new KeyBinding(KeyCode.N);

        #endregion

        public static bool ShowHints {
            get { return Main.m_showHints; }
            set { Main.m_showHints = value; }
        }

        public static bool ShowInteractionHighlights {
            get { return Main.m_interactionHighlights; }
            set { Main.m_interactionHighlights = value; }
        }

        public static bool ShowBillboards {
            get { return Main.m_showBillboards; }
            set { Main.m_showBillboards = value; }
        }

        public static bool ShowNavigationHints {
            get { return Main.m_showNavigationHints; }
            set { Main.m_showNavigationHints = value; }
        }

        private void Awake()
        {
            WeavrCamera.CameraChanged -= CameraHasChanged;
            WeavrCamera.CameraChanged += CameraHasChanged;
            m_main = this;
        }

        private void CameraHasChanged(Camera cam)
        {
            onCameraChange?.Invoke(cam);
        }

        private void Start()
        {
            // KeyBindings
            KeyBinding.BindKeyUp(m_useWorldPointerKey, () => m_useWorldPointer = !m_useWorldPointer);
            KeyBinding.BindKeyUp(m_useTeleportKey, () => m_useTeleport = !m_useTeleport);
            KeyBinding.BindKeyUp(m_interactioneHighlightsKey, () => m_interactionHighlights = !m_interactionHighlights);
            KeyBinding.BindKeyUp(m_showHintsKey, () => m_showHints = !m_showHints);
            KeyBinding.BindKeyUp(m_showNavigationKey, () => m_showNavigationHints = !m_showNavigationHints);
            KeyBinding.BindKeyUp(m_showBillboardKey, () => m_showBillboards = !m_showBillboards);
        }

        private void Update()
        {
            m_useWorldPointerKey.Update();
            m_useTeleportKey.Update();
            m_interactioneHighlightsKey.Update();
            m_showHintsKey.Update();
            m_showBillboardKey.Update();
            m_showNavigationKey.Update();

            //Camera = Camera.current;
        }

        private void OnDisable()
        {
            WeavrCamera.CameraChanged -= CameraHasChanged;
        }
    }
}
