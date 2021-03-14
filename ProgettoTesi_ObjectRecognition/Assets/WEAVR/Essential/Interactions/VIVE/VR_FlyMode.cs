using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if WEAVR_VR
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Interaction
{

    [AddComponentMenu("WEAVR/VR/Advanced/Fly Mode")]
    public class VR_FlyMode : MonoBehaviour
    {
        #region [  STATIC PART  ]

        private static VR_FlyMode _instance;

        public static VR_FlyMode Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<VR_FlyMode>();
                    if (_instance == null)
                    {
                        // If no object is active, then create a new one
                        GameObject go = new GameObject("VR_FlyMode");
                        _instance = go.AddComponent<VR_FlyMode>();
                    }
                }
                return _instance;
            }
        }

        #endregion

        public enum NavigationMode { Head, Controller }

        public NavigationMode navigateMode;
        public GameObject navigatingHand;
        public GameObject navigationIndicator;
        public GameObject upDownHand;
        public float moveSpeed = 1;
        public bool resetToLastPosition = true;
        
        [Header("Buttons")]
        [SerializeField]
        protected VR_ControllerAction m_exitModeButton;
        [SerializeField]
        protected VR_ControllerAction m_navigateButton;
        [SerializeField]
        protected VR_ControllerAction m_upDownButton;

        private Vector3? m_lastPosition;

        public void Toggle()
        {
            enabled = !enabled;
        }

#if WEAVR_VR

        private Vector3 m_targetPosition;

        private void Reset()
        {
            navigatingHand = GetComponentInChildren<Hand>()?.gameObject;
        }

        private void Awake()
        {
            _instance = this;
        }

        private void Start()
        {
            enabled = false;
        }

        private void OnValidate()
        {
            if (navigatingHand != null && navigatingHand.GetComponent<Hand>() == null)
            {
                navigatingHand = null;
            }
            if (upDownHand != null && upDownHand.GetComponent<Hand>() == null)
            {
                upDownHand = null;
            }
            if(navigatingHand == upDownHand)
            {
                upDownHand = null;
            }
        }

        public void ResetToLastPosition()
        {
            if (m_lastPosition.HasValue)
            {
                Valve.VR.InteractionSystem.Player.instance.trackingOriginTransform.position = m_lastPosition.Value;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (m_exitModeButton.IsTriggered())
            {
                enabled = false;
                return;
            }

            var player = Valve.VR.InteractionSystem.Player.instance.trackingOriginTransform;
            var arrow = navigateMode == NavigationMode.Head ?
                        Valve.VR.InteractionSystem.Player.instance.hmdTransform : navigatingHand.transform;
            if (m_navigateButton.IsTriggered())
            {
                var axis = m_navigateButton.GetButtonAxis() * 0.1f;
                m_targetPosition += arrow.forward * axis.y 
                                  + arrow.right * axis.x;
            }

            if(upDownHand != null && m_upDownButton.IsTriggered())
            {
                var axis = m_upDownButton.GetButtonAxis() * 0.1f;
                m_targetPosition += arrow.up * axis.y;
            }

            player.position = Vector3.Lerp(player.position, m_targetPosition, moveSpeed * Time.deltaTime);
        }

        private void OnEnable()
        {
            //m_exitModeButton.Initialize(navigatingHand.GetComponent<Hand>());
            //m_navigateButton.Initialize(navigatingHand.GetComponent<Hand>());

            TogglePointers(navigatingHand.GetComponentsInChildren<WorldPointer>(), false);

            if(upDownHand != null)
            {
                //m_upDownButton.Initialize(upDownHand.GetComponent<Hand>());
                TogglePointers(upDownHand.GetComponentsInChildren<WorldPointer>(), false);
            }

            m_lastPosition = m_targetPosition = Valve.VR.InteractionSystem.Player.instance.trackingOriginTransform.position;
            Teleport.instance.enabled = false;
        }

        private void TogglePointers(IEnumerable<WorldPointer> pointers, bool toEnable)
        {
            foreach(var pointer in pointers)
            {
                pointer.enabled = pointer.gameObject.activeInHierarchy && toEnable;
            }
        }

        private void OnDisable()
        {
            if (resetToLastPosition)
            {
                ResetToLastPosition();
            }

            Teleport.instance.enabled = true;

            TogglePointers(navigatingHand.GetComponentsInChildren<WorldPointer>(), true);
            if (upDownHand != null)
            {
                TogglePointers(upDownHand.GetComponentsInChildren<WorldPointer>(), true);
            }
        }
#endif
    }
}
