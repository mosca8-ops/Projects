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
    public class VR_ControllerAxis
    {
        public enum TriggerType { Swipe, Scroll }

        [Flags]
        public enum SwipeDirection {
            None = 0,
            Left = 1 << 0,
            Up = 1 << 1,
            Right = 1 << 2,
            Down = 1 << 3,
        }

        public VR_ControllerManager.HandType handType = VR_ControllerManager.HandType.AnyHand;
        public TriggerType type = TriggerType.Scroll;

        [Tooltip("How fast to allow element change when swiping")]
        [Range(0, 1)]
        public float scrollUpdateRate = 0.1f;

        [Tooltip("The normalized step size of the swiping")]
        [Range(0, 1)]
        public float scrollStepSize = 0.1f;


#if WEAVR_VR

        private bool m_isInitialized = false;
        private bool m_wasTouching;
        private bool m_hasSwiped;
        private int m_currentFrame = -1;
        private float m_nextTimeUpdate;
        private float m_sqrStepSize;
        private Vector2 m_lastAxisValue;
        private Vector2 m_scrollDelta;
        private SwipeDirection m_currentDirection;
        private Hand m_hand;

        public Hand Hand
        {
            get { return m_hand; }
            set
            {
                if (m_hand != value)
                {
                    m_hand = value;
                }
            }
        }

        public void Initialize(object hand)
        {
            Hand = hand as Hand;
            if(Hand != null)
            {
                m_isInitialized = true;
            }
        }

        public void Initialize()
        {
            VR_ControllerManager.Instance.Register(this);
            m_isInitialized = true;
        }

        public bool IsTriggered()
        {
            return type == TriggerType.Scroll ? IsScrolling() : HasSwiped();
        }

        public bool IsScrolling()
        {
            UpdateState();
            return m_scrollDelta.sqrMagnitude > 0;
        }

        public bool HasSwiped()
        {
            UpdateState();
            return m_hasSwiped;
        }

        public Vector2 GetAxis()
        {
            UpdateState();
            return m_lastAxisValue;
        }

        public Vector2 GetScrollDelta()
        {
            UpdateState();
            return m_scrollDelta;
        }

        public SwipeDirection GetDirection()
        {
            UpdateState();
            return m_currentDirection;
        }

        public void UpdateState()
        {
            if (!m_isInitialized)
            {
                Initialize();
            }
            if (m_currentFrame == Time.frameCount)
            {
                return;
            }

            m_currentFrame = Time.frameCount;
            m_hasSwiped = false;
            m_currentDirection = SwipeDirection.None;
            m_scrollDelta.Set(0, 0);

            if (m_nextTimeUpdate > Time.time)
            {
                return;
            }

            var hand = m_hand;
            bool isTouching = TXT.WEAVR.Interaction.VR_ControllerManager.isTrackpadTouched(m_hand);
            if(!isTouching && handType == VR_ControllerManager.HandType.AnyHand 
                && m_hand.otherHand != null)
            {
                isTouching = TXT.WEAVR.Interaction.VR_ControllerManager.isTrackpadTouched(m_hand.otherHand);
                if (isTouching)
                {
                    hand = m_hand.otherHand;
                }
            }
            if (!isTouching && !m_wasTouching)
            {
                //m_lastAxisValue.Set(0, 0);
                return;
            }

            var axisValue = TXT.WEAVR.Interaction.VR_ControllerManager.getTrackpadAxis(hand);

            if (isTouching && !m_wasTouching)
            {
                m_wasTouching = isTouching;
                m_lastAxisValue = axisValue;
                m_nextTimeUpdate = Time.time + scrollUpdateRate;
                return;
            }


            if (isTouching && m_wasTouching)
            {
                m_scrollDelta = axisValue - m_lastAxisValue;

                //m_lastAxisValue = axisValue;
                if (m_scrollDelta.sqrMagnitude < scrollStepSize * scrollStepSize)
                {
                    m_scrollDelta.Set(0, 0);
                    m_wasTouching = isTouching;
                    return;
                }
            }

            ComputeDirection();

            m_lastAxisValue = axisValue;
            m_hasSwiped = m_currentDirection != SwipeDirection.None && !isTouching && m_wasTouching;
            m_wasTouching = isTouching;
            m_nextTimeUpdate = Time.time + scrollUpdateRate;
        }

        private void ComputeDirection()
        {
            if (m_scrollDelta.x > scrollStepSize)
            {
                m_currentDirection |= SwipeDirection.Right;
            }
            else if (m_scrollDelta.x < -scrollStepSize)
            {
                m_currentDirection |= SwipeDirection.Left;
            }

            if (m_scrollDelta.y > scrollStepSize)
            {
                m_currentDirection |= SwipeDirection.Up;
            }
            else if (m_scrollDelta.y < -scrollStepSize)
            {
                m_currentDirection |= SwipeDirection.Down;
            }
        }

#else
        public void Initialize(object hand)
        {

        }
#endif
    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(VR_ControllerAxis))]
    public class VR_ControllerAxisDrawer : PropertyDrawer
    {
        private static readonly GUIContent s_updateRateContent 
                            = new GUIContent("Update Rate", "How fast to allow element change when swiping");
        private static readonly GUIContent s_stepSizeContent
                            = new GUIContent("Step Size", "The normalized step size of the swiping");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float width = position.width;
            float startX = position.x;
            position.height = EditorGUIUtility.singleLineHeight;
            position.width = EditorGUIUtility.labelWidth;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);
            bool isExpanded = property.isExpanded;
            property.NextVisible(true);
            position.x += position.width;
            position.width = 100;
            EditorGUI.PropertyField(position, property, GUIContent.none);
            property.NextVisible(true);
            position.x += position.width;
            position.width = width - EditorGUIUtility.labelWidth - 100;
            EditorGUI.PropertyField(position, property, GUIContent.none);

            if (isExpanded)
            {
                EditorGUI.indentLevel++;

                position.x = startX;
                position.width = width;
                position.y += position.height + EditorGUIUtility.standardVerticalSpacing;

                property.NextVisible(true);
                EditorGUI.PropertyField(position, property, s_updateRateContent);
                position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                property.NextVisible(true);
                EditorGUI.PropertyField(position, property, s_stepSizeContent);

                EditorGUI.indentLevel--;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.isExpanded ? EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing * 2
                                       : EditorGUIUtility.singleLineHeight;
        }
    }

#endif
}
