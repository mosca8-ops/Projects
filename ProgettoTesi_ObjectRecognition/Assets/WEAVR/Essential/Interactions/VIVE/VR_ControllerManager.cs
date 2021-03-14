using System;
using System.Collections.Generic;
using UnityEngine;

#if WEAVR_VR
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Interaction
{

    [AddComponentMenu("WEAVR/VR/Interactions/Controller Manager")]
    public class VR_ControllerManager : MonoBehaviour
    {
        public enum HandType { AnyHand, LeftHand, RightHand, ThisHand }

        #region [  STATIC PART  ]

        private static VR_ControllerManager s_instance;

        public static VR_ControllerManager Instance {
            get {
                if (s_instance == null)
                {
                    s_instance = FindObjectOfType<VR_ControllerManager>();
                    if (s_instance == null)
                    {
                        // If no object is active, then create a new one
                        GameObject go = new GameObject("VR_ControllerManager");
                        s_instance = go.AddComponent<VR_ControllerManager>();
                    }
#if WEAVR_VR
                    s_instance.Initialize();
#endif
                }
                return s_instance;
            }
        }

        #endregion

#if WEAVR_VR
        [SerializeField]
        private Hand[] m_hands;

        [SerializeField]
        [HideInInspector]
        private HandControls m_leftHand;
        [SerializeField]
        [HideInInspector]
        private HandControls m_rightHand;
        [SerializeField]
        [HideInInspector]
        private HandControls m_anyHand;

        private bool m_initialized;

        private void Initialize()
        {
            m_hands = GetComponentsInChildren<Hand>();

            if (m_anyHand == null)
            {
                m_anyHand = new HandControls(Valve.VR.SteamVR_Input_Sources.Any);
            }
            if (m_leftHand == null)
            {
                m_leftHand = new HandControls(Valve.VR.SteamVR_Input_Sources.LeftHand);
            }
            if (m_rightHand == null)
            {
                m_rightHand = new HandControls(Valve.VR.SteamVR_Input_Sources.RightHand);
            }

            SetupHands();

            m_initialized = true;
        }

        private void SetupHands()
        {
            for (int i = 0; i < m_hands.Length; i++)
            {
                if (m_hands[i].handType == Valve.VR.SteamVR_Input_Sources.LeftHand)
                {
                    m_leftHand.hand = m_hands[i];
                }
                else if (m_hands[i].handType == Valve.VR.SteamVR_Input_Sources.RightHand)
                {
                    m_rightHand.hand = m_hands[i];
                }
                else
                {
                    m_anyHand.hand = m_hands[i];
                }
            }

            if (m_hands.Length > 0)
            {
                if (m_leftHand.hand == null)
                {
                    m_leftHand.hand = m_hands[0];
                }
                if (m_rightHand.hand == null)
                {
                    m_rightHand.hand = m_hands[0];
                }
                if (m_anyHand.hand == null)
                {
                    m_anyHand.hand = m_hands[0];
                }
            }
        }

        private void OnValidate()
        {
            if (!m_initialized)
            {
                Initialize();
            }
            if (m_initialized)
            {
                SetupHands();
            }
        }

        private void Awake()
        {
            if (s_instance != this)
            {
                Initialize();
            }
        }

        public void Register(VR_ControllerAction button)
        {
            switch (button.handType)
            {
                case HandType.AnyHand:
                    m_anyHand.Add(button);
                    break;
                case HandType.LeftHand:
                    m_leftHand.Add(button);
                    break;
                case HandType.RightHand:
                    m_rightHand.Add(button);
                    break;
            }
        }

        public bool Unregister(VR_ControllerAction button)
        {
            return m_anyHand.Remove(button) || m_leftHand.Remove(button) || m_rightHand.Remove(button);
        }

        public void Register(VR_ControllerAxis axis)
        {
            switch (axis.handType)
            {
                case HandType.AnyHand:
                    m_anyHand.Add(axis);
                    break;
                case HandType.LeftHand:
                    m_leftHand.Add(axis);
                    break;
                case HandType.RightHand:
                    m_rightHand.Add(axis);
                    break;
            }
        }

        public bool Unregister(VR_ControllerAxis axis)
        {
            return m_anyHand.Remove(axis) || m_leftHand.Remove(axis) || m_rightHand.Remove(axis);
        }

        // Update is called once per frame
        void Update()
        {
            if (!m_leftHand.SwapControlsIfNeeded(m_rightHand))
            {
                if (m_leftHand.TypeChanged())
                {
                    m_leftHand.PickFirstFreeHand(m_hands);
                }
                if (m_rightHand.TypeChanged())
                {
                    m_rightHand.PickFirstFreeHand(m_hands);
                }
            }

            if (m_anyHand.isValid)
            {
                m_anyHand.Update();
            }
            if (m_leftHand.isValid)
            {
                m_leftHand.Update();
            }
            if (m_rightHand.isValid)
            {
                m_rightHand.Update();
            }
        }

        public static Vector3 getControllerPosition(Hand iHand)
        {
            Valve.VR.SteamVR_Action_Pose[] poseActions = Valve.VR.SteamVR_Input.actionsPose;
            if (poseActions.Length > 0)
            {
                return poseActions[0].GetLocalPosition(iHand.handType);
            }
            return new Vector3(0, 0, 0);
        }

        public static Quaternion getControllerRotation(Hand iHand)
        {
            Valve.VR.SteamVR_Action_Pose[] poseActions = Valve.VR.SteamVR_Input.actionsPose;
            if (poseActions.Length > 0)
            {
                return poseActions[0].GetLocalRotation(iHand.handType);
            }
            return Quaternion.identity;
        }

        public static Vector3 getControllerAngularVelocity(Hand iHand)
        {
            Valve.VR.SteamVR_Action_Pose[] poseActions = Valve.VR.SteamVR_Input.actionsPose;
            if (poseActions.Length > 0)
            {
                return poseActions[0].GetAngularVelocity(iHand.handType);
            }
            return new Vector3(0, 0, 0);
        }

        public static Vector3 getControllerVelocity(Hand iHand)
        {
            Valve.VR.SteamVR_Action_Pose[] poseActions = Valve.VR.SteamVR_Input.actionsPose;
            if (poseActions.Length > 0)
            {
                return poseActions[0].GetVelocity(iHand.handType);
            }
            return new Vector3(0, 0, 0);
        }

        public static bool isTrackpadTouched(Hand iHand)
        {
            bool wRet = false;
            if (Valve.VR.SteamVR_Actions.wEAVR_TrackPadPressed.GetActive(iHand.handType))
            {
                wRet = Valve.VR.SteamVR_Actions.wEAVR_TrackPadPressed.GetState(iHand.handType);
            }
            return wRet;
        }

        public static Vector2 getTrackpadAxis(Hand iHand)
        {
            Vector2 wRet = new Vector2(0, 0);
            if (Valve.VR.SteamVR_Actions.wEAVR_TrackPad.GetActive(iHand.handType))
            {
                wRet = Valve.VR.SteamVR_Actions.wEAVR_TrackPad.GetAxis(iHand.handType);
            }
            return wRet;
        }

        public static bool getActionState(VR_ControllerAction.ActionType iActionType, Hand iHand)
        {
            bool wRet = false;
            string wActionName = iActionType.ToString();
            wRet = Valve.VR.SteamVR_Input.GetState(wActionName, iHand.handType);
            return wRet;
        }

        public static bool getActionStateDown(VR_ControllerAction.ActionType iActionType, Hand iHand)
        {
            bool wRet = false;
            string wActionName = iActionType.ToString();
            wRet = Valve.VR.SteamVR_Input.GetStateDown(wActionName, iHand.handType);
            return wRet;
        }

        public static bool getActionStateUp(VR_ControllerAction.ActionType iActionType, Hand iHand)
        {
            bool wRet = false;
            string wActionName = iActionType.ToString();
            wRet = Valve.VR.SteamVR_Input.GetStateUp(wActionName, iHand.handType);
            return wRet;
        }
        //-------------------------------------------------
        // Was the standard interaction button just pressed? In VR, this is a trigger press. In 2D fallback, this is a mouse left-click.
        //-------------------------------------------------
        public static bool GetStandardInteractionButtonDown(Hand iHand)
        {
            if (iHand == null)
            {
                return false;
            }
            if (iHand.noSteamVRFallbackCamera)
            {
                return Input.GetMouseButtonDown(0);
            }
            else
            {
                return getActionStateDown(VR_ControllerAction.ActionType.TriggerPressed, iHand);
            }
        }


        //-------------------------------------------------
        // Was the standard interaction button just released? In VR, this is a trigger press. In 2D fallback, this is a mouse left-click.
        //-------------------------------------------------
        public static bool GetStandardInteractionButtonUp(Hand iHand)
        {
            if (iHand == null)
            {
                return false;
            }
            if (iHand.noSteamVRFallbackCamera)
            {
                return Input.GetMouseButtonUp(0);
            }
            else
            {
                return getActionStateUp(VR_ControllerAction.ActionType.TriggerPressed, iHand);
            }
        }


        //-------------------------------------------------
        // Is the standard interaction button being pressed? In VR, this is a trigger press. In 2D fallback, this is a mouse left-click.
        //-------------------------------------------------
        public static bool GetStandardInteractionButton(Hand iHand)
        {
            if (iHand == null)
            {
                return false;
            }
            if (iHand.noSteamVRFallbackCamera)
            {
                return Input.GetMouseButton(0);
            }
            else
            {
                return getActionState(VR_ControllerAction.ActionType.TriggerPressed, iHand);
            }
        }

        //-------------------------------------------------
        public static Transform GetAttachmentTransform(Hand iHand, string attachmentPoint = "")
        {
            Transform attachmentTransform = null;
            if (iHand != null)
            {
                if (!string.IsNullOrEmpty(attachmentPoint))
                {
                    attachmentTransform = iHand.transform.Find(attachmentPoint);
                }

                if (!attachmentTransform)
                {
                    attachmentTransform = iHand.transform;
                }
            }
            return attachmentTransform;
        }

        [Serializable]
        private class HandControls
        {
            private Hand m_hand;
            public Valve.VR.SteamVR_Input_Sources handType { get; private set; }
            public Hand hand {
                get { return m_hand; }
                set {
                    if (m_hand != value)
                    {
                        m_hand = value;
                        if (buttons == null)
                        {
                            buttons = new List<VR_ControllerAction>();
                        }
                        if (axii == null)
                        {
                            axii = new List<VR_ControllerAxis>();

                        }
                        ApplyControls();
                    }
                }
            }
            public List<VR_ControllerAction> buttons;
            public List<VR_ControllerAxis> axii;

            public bool isValid => hand != null;

            public bool TypeChanged()
            {
                return handType != hand.handType;
            }

            public HandControls(Valve.VR.SteamVR_Input_Sources handType)
            {
                this.handType = handType;
                buttons = new List<VR_ControllerAction>();
                axii = new List<VR_ControllerAxis>();
            }

            public void Add(VR_ControllerAction button)
            {
                if (!buttons.Contains(button))
                {
                    buttons.Add(button);
                    button.Hand = hand;
                }
            }

            public bool Remove(VR_ControllerAction button)
            {
                return buttons.Remove(button);
            }

            public void Add(VR_ControllerAxis axis)
            {
                if (!axii.Contains(axis))
                {
                    axii.Add(axis);
                    axis.Hand = hand;
                }
            }

            public bool Remove(VR_ControllerAxis axis)
            {
                return axii.Remove(axis);
            }

            public void SwapControls(HandControls otherHand)
            {
                Debug.Log("Swapping controls");
                var tempButtons = otherHand.buttons;
                otherHand.buttons = buttons;
                buttons = tempButtons;

                var tempAxii = otherHand.axii;
                otherHand.axii = axii;
                axii = tempAxii;

                var tempHand = otherHand.hand;
                otherHand.hand = hand;
                hand = otherHand.hand;
            }

            //TODO Check if needed
            public bool SwapControlsIfNeeded(HandControls otherHand)
            {
                if (hand != otherHand.hand && hand.handType == otherHand.handType)
                {
                    SwapControls(otherHand);
                    return true;
                }
                return false;
            }

            public void PickFirstFreeHand(Hand[] hands)
            {
                for (int i = 0; i < hands.Length; i++)
                {
                    if (hands[i].handType == handType)
                    {
                        hand = hands[i];
                        return;
                    }
                }
                hand = hands[0];
            }

            public bool IsSameHand(HandControls otherHand)
            {
                return hand == otherHand.hand;
            }

            public void ApplyControls()
            {
                for (int i = 0; i < buttons.Count; i++)
                {
                    buttons[i].Hand = hand;
                }
                for (int i = 0; i < axii.Count; i++)
                {
                    axii[i].Hand = hand;
                }
            }

            public void Update()
            {
                for (int i = 0; i < buttons.Count; i++)
                {
                    if (buttons[i].RequiresUpdate)
                    {
                        buttons[i].UpdateState();
                    }
                }
            }

        }
#endif
    }
}
