using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Maintenance
{
    [AddComponentMenu("WEAVR/Interactions/Controls/Multi Button")]
    public class MultiPurposePushButton : PushButton
    {
        [Header("Multi Purpose Button")]
        [SerializeField]
        [Tooltip("If true then the events will be called")]
        protected bool m_canRaise = true;
        [SerializeField]
        [RangeFrom(0, nameof(m_eventGroups))]
        protected int m_initialGroup = 0;
        [SerializeField]
        [ShowAsReadOnly]
        private int m_currentGroup;
        [SerializeField]
        protected EventGroup[] m_eventGroups;

        public bool CanRaise
        {
            get { return m_canRaise; }
            set { m_canRaise = value; }
        }

        public int CurrentGroup
        {
            get { return m_currentGroup; }
            set
            {
                if (m_currentGroup != value)
                {
                    if (m_currentGroup < 0 || m_currentGroup >= m_eventGroups.Length)
                    {
                        throw new ArgumentOutOfRangeException(nameof(CurrentGroup));
                    }
                    m_currentGroup = value;
                    m_canRaise = true;
                }
            }
        }

        public void SetCurrentGroup(int group)
        {
            CurrentGroup = group;
        }

        protected override void Reset()
        {
            base.Reset();
            m_hideMainEvents = true;
        }

        protected override void Start()
        {
            if (m_eventGroups.Length > 0)
            {
                OnUp.AddListener(CallOnUp);
                OnDown.AddListener(CallOnDown);
                OnStateChanged.AddListener(CallOnStateChanged);
                OnContinuouslyDown.AddListener(CallOnDownContinuously);

                CurrentGroup = m_initialGroup;
            }
            base.Start();
        }

        private void CallOnUp()
        {
            if (m_canRaise)
            {
                m_eventGroups[m_currentGroup].OnUp.Invoke();
            }
        }

        private void CallOnDown()
        {
            if (m_canRaise)
            {
                m_eventGroups[m_currentGroup].OnDown.Invoke();
            }
        }

        private void CallOnStateChanged()
        {
            if (m_canRaise)
            {
                m_eventGroups[m_currentGroup].OnStateChanged.Invoke();
            }
        }

        private void CallOnDownContinuously(float time)
        {
            if (m_canRaise)
            {
                m_eventGroups[m_currentGroup].OnContinuouslyDown.Invoke(time);
            }
        }
    }
}
