using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if WEAVR_VR
using Valve.VR;
using Valve.VR.InteractionSystem;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TXT.WEAVR.Interaction
{
    [Serializable]
    public class VR_ControllerAction
    {

        public enum ActionType
        {
            GripPressed,
            Trigger,
            TriggerPressed,
            TrackPad,
            TrackPadPressed,
            TrackPadTouched,
            TrackPadClicked,
            Menu,
            //HeadSetOn,
            //Pose,
            //SkeletonLeftHand,
            //SkeletonRightHand
        }

        public enum ActionState
        {
            OnPress,
            OnPressDown,
            OnPressUp
        }

        public VR_ControllerManager.HandType handType = VR_ControllerManager.HandType.AnyHand;
        public ActionType mAction = ActionType.Trigger;
        public ActionState trigger = ActionState.OnPressDown;


        #region [  EVENTS  ]

        private event Action m_onTriggered;
        private event Action m_onPress;
        private event Action m_onPressDown;
        private event Action m_onPressUp;

        public event Action OnTriggered {
            add {
                Initialize();
                m_requiresUpdate = true;
                m_onTriggered += value;
            }
            remove {
                m_onTriggered -= value;
                m_requiresUpdate = m_onTriggered != null
                            || m_onPress != null
                            || m_onPressDown != null
                            || m_onPressUp != null;
            }
        }

        public event Action OnPress {
            add {
                Initialize();
                m_requiresUpdate = true;
                m_onPress += value;
            }
            remove {
                m_onPress -= value;
                m_requiresUpdate = m_onTriggered != null
                            || m_onPress != null
                            || m_onPressDown != null
                            || m_onPressUp != null;
            }
        }

        public event Action OnPressDown {
            add {
                Initialize();
                m_requiresUpdate = true;
                m_onPressDown += value;
            }
            remove {
                m_onPressDown -= value;
                m_requiresUpdate = m_onTriggered != null
                            || m_onPress != null
                            || m_onPressDown != null
                            || m_onPressUp != null;
            }
        }

        public event Action OnPressUp {
            add {
                Initialize();
                m_requiresUpdate = true;
                m_onPressUp += value;
            }
            remove {
                m_onPressUp -= value;
                m_requiresUpdate = m_onTriggered != null
                            || m_onPress != null
                            || m_onPressDown != null
                            || m_onPressUp != null;
            }
        }

        #endregion

        private bool m_requiresUpdate;

        private bool m_isInitialized;



#if WEAVR_VR
        private string m_actionName;
        private Func<string, SteamVR_Input_Sources, bool, bool> m_checkFunctor;
        private Func<string, SteamVR_Input_Sources, bool, bool> m_pressFunctor;
        private Func<string, SteamVR_Input_Sources, bool, bool> m_pressUpFunctor;
        private Func<string, SteamVR_Input_Sources, bool, bool> m_pressDownFunctor;
        private Func<Vector2> m_getAxisFunctor;
        private Hand m_hand;

        public Hand Hand
        {
            get { return m_hand; }
            set
            {
                if (m_hand != value)
                {
                    m_hand = value;
                    if (m_hand != null)
                    {
                        Setup(m_hand);
                    }
                    else
                    {
                        m_checkFunctor = m_pressFunctor = m_pressUpFunctor = m_pressDownFunctor = DefaultAction;
                        m_getAxisFunctor = DefaultAxisAction;
                    }
                }
            }
        }

        public void Validate()
        {
            if (handType == VR_ControllerManager.HandType.ThisHand && !Application.isPlaying)
            {
                handType = VR_ControllerManager.HandType.AnyHand;
            }
        }

        public bool RequiresUpdate => m_requiresUpdate;

        public void Initialize(object hand)
        {
            var vrHand = hand as Hand;
            if(vrHand != null)
            {
                handType = VR_ControllerManager.HandType.ThisHand;
                Hand = vrHand;
                m_isInitialized = true;
            }
        }

        private void Initialize()
        {
            if (m_isInitialized) { return; }
            m_isInitialized = true;
            VR_ControllerManager.Instance.Register(this);
        }
        
        public void UpdateState()
        {
            if(m_onTriggered != null && m_checkFunctor(m_actionName, Valve.VR.SteamVR_Input_Sources.Any, false))
            {
                m_onTriggered();
            }
            if (m_onPress != null && m_pressFunctor(m_actionName, Valve.VR.SteamVR_Input_Sources.Any, false))
            {
                m_onPress();
            }
            if (m_onPressUp != null && m_pressUpFunctor(m_actionName, Valve.VR.SteamVR_Input_Sources.Any, false))
            {
                m_onPressUp();
            }
            if (m_onPressDown != null && m_pressDownFunctor(m_actionName, Valve.VR.SteamVR_Input_Sources.Any, false))
            {
                m_onPressDown();
            }
        }

        public void Destroy()
        {
            VR_ControllerManager.Instance?.Unregister(this);
        }

        public bool IsTriggered()
        {
            if (!m_isInitialized)
            {
                Initialize();
            }

            return m_checkFunctor(m_actionName, m_hand.handType, false);
        }

        public bool IsTriggered(Hand hand)
        {
            if (!m_isInitialized)
            {
                Initialize();
            }

            return m_checkFunctor(m_actionName, hand.handType, false);
        }

        public bool IsPress()
        {
            if (!m_isInitialized)
            {
                Initialize();
            }
            return m_pressFunctor(m_actionName, Valve.VR.SteamVR_Input_Sources.Any, false);
        }

        public bool IsPressDown()
        {
            if (!m_isInitialized)
            {
                Initialize();
            }
            return m_pressDownFunctor(m_actionName, Valve.VR.SteamVR_Input_Sources.Any, false);
        }

        public bool IsPressUp()
        {
            if (!m_isInitialized)
            {
                Initialize();
            }
            return m_pressUpFunctor(m_actionName, Valve.VR.SteamVR_Input_Sources.Any, false);
        }

        public Vector2 GetButtonAxis()
        {
            if (!m_isInitialized)
            {
                Initialize();
            }
            return m_getAxisFunctor();
        }
        
        private bool DefaultAction(string iActionName, SteamVR_Input_Sources iFilter, bool iCaseSensitive)
        {
            if (m_hand != null)
            {
                Setup(m_hand);
                return m_checkFunctor(m_actionName, Valve.VR.SteamVR_Input_Sources.Any, false);
            }

            return false;
        }

        private Vector2 DefaultAxisAction()
        {
            if (m_hand != null)
            {
                Setup(m_hand);
                return m_getAxisFunctor();
            }

            return Vector2.zero;
        }

        private void Setup(Hand hand)
        {
            m_getAxisFunctor = () => Valve.VR.SteamVR_Input.GetVector2(m_actionName, Valve.VR.SteamVR_Input_Sources.Any, false);
            m_actionName = mAction.ToString();
            var otherHand = handType == VR_ControllerManager.HandType.AnyHand ? m_hand.otherHand : null;
            if(otherHand != null)
            {
                m_pressFunctor = (id, type, caseSensitive) => Valve.VR.SteamVR_Input.GetState(id, otherHand.handType);
                m_pressDownFunctor = (id, type, caseSensitive) => Valve.VR.SteamVR_Input.GetStateDown(id, otherHand.handType);
                m_pressUpFunctor = (id, type, caseSensitive) => Valve.VR.SteamVR_Input.GetStateUp(id, otherHand.handType);
            }
            else
            {
                m_pressFunctor = Valve.VR.SteamVR_Input.GetState;
                m_pressDownFunctor = Valve.VR.SteamVR_Input.GetStateDown;
                m_pressUpFunctor = Valve.VR.SteamVR_Input.GetStateUp;
            }
            switch (trigger)
            {
                case ActionState.OnPress:
                    m_checkFunctor = m_pressFunctor;
                    break;
                case ActionState.OnPressDown:
                    m_checkFunctor = m_pressDownFunctor;
                    break;
                case ActionState.OnPressUp:
                    m_checkFunctor = m_pressUpFunctor;
                    break;
            }
        }


#else
        public void Initialize(object hand)
        {

        }

        private void Initialize()
        {
            if (m_isInitialized) { return; }
            m_isInitialized = true;
        }

        public void Destroy()
        {

        }
        public void Validate()
        {

        }
#endif

    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(VR_ControllerAction))]
    public class VR_ControllerButtonDrawer : PropertyDrawer
    {
        private VR_ControllerAction m_target;
        private bool m_validate;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (m_validate)
            {
                GetTarget(property)?.Validate();
                m_validate = false;
            }

            EditorGUI.BeginChangeCheck();
            position.width -= 170;
            property.NextVisible(true);
            EditorGUI.PropertyField(position, property, label);
            position.x += position.width;
            position.width = 70;
            property.NextVisible(true);
            EditorGUI.PropertyField(position, property, GUIContent.none);
            position.x += position.width;
            position.width = 100;
            property.NextVisible(true);
            EditorGUI.PropertyField(position, property, GUIContent.none);
            m_validate = EditorGUI.EndChangeCheck();

        }

        private VR_ControllerAction GetTarget(SerializedProperty property)
        {
            if (m_target == null)
            {
                m_target = fieldInfo.GetValue(property.serializedObject.targetObject) as VR_ControllerAction;
            }
            return m_target;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }

#endif

}
