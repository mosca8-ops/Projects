using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TXT.WEAVR.InputControls
{
    [ExecuteInEditMode]
    [DefaultExecutionOrder(-100)]
    [AddComponentMenu("")]
    public class MobileControlRig : MonoBehaviour, IWeavrSingleton
    {

        #region [  STATIC PART  ]

        private static MobileControlRig s_instance;
        public static MobileControlRig Current
        {
            get
            {
                if (!s_instance)
                {
                    s_instance = Weavr.TryGetInCurrentScene<MobileControlRig>();
                }
                return s_instance;
            }
        }


        public static bool IsActive => Current && Current.isActiveAndEnabled && Current.m_isActive;

        #endregion

        private AbstractTouchControl[] m_handlers;

        private CrossPlatformInputManager.ActiveInputMethod? m_prevInput;

        private bool m_isActive;

        private void Awake()
        {
            if (CheckEnableControlRig())
            {
                Input.multiTouchEnabled = true;
                m_handlers = GetComponentsInChildren<AbstractTouchControl>(true);
            }
        }

        void OnEnable()
        {
            if (!CheckEnableControlRig())
            {
                return;
            }
            if (!s_instance)
            {
                s_instance = this;
            }
            m_isActive = true;
            m_prevInput = CrossPlatformInputManager.ActiveInputMode;
            CrossPlatformInputManager.SwitchActiveInputMethod(CrossPlatformInputManager.ActiveInputMethod.Touch);
        }

        private void OnDisable()
        {
            if(s_instance == this)
            {
                s_instance = null;
            }
            if(m_prevInput.HasValue)
            {
                CrossPlatformInputManager.SwitchActiveInputMethod(m_prevInput.Value);
                m_prevInput = null;
            }
        }

        public IEventSystemHandler GetHandler(PointerEventData data)
        {
            foreach(var handler in m_handlers)
            {
                if (handler.CanHandlePointerDown(data))
                {
                    return handler;
                }
            }
            return null;
        }

        private bool CheckEnableControlRig()
        {
            EnableControlRig(Application.isMobilePlatform);
            return Application.isMobilePlatform;
        }


        private void EnableControlRig(bool enabled)
        {
            foreach (Transform t in transform)
            {
                t.gameObject.SetActive(enabled);
            }
            //gameObject.SetActive(enabled);
            m_isActive = enabled;
        }
    }
}