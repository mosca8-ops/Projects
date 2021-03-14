using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;


namespace TXT.WEAVR.Procedure
{
    public class LockObjectAction : BaseReversibleAction
    {
        [SerializeField]
        private ValueProxyGameObject m_target;
        [SerializeField]
        private Space m_lockSpace;
        [SerializeField]
        private Axis m_lockPosition;
        [SerializeField]
        private Axis m_lockRotation;

        private TransformLock m_locker;
        private Space m_prevSpace;
        private Axis m_prevPosition;
        private Axis m_prevRotation;

        public override bool Execute(float dt)
        {
            var target = m_target.Value;
            if (target)
            {
                m_locker = target.GetOrCreateComponent<TransformLock>();
                m_prevSpace = m_locker.LockSpace;
                m_prevPosition = m_locker.PositionLock;
                m_prevRotation = m_locker.RotationLock;
                m_locker.Lock(m_lockPosition, m_lockRotation, m_lockSpace);
            }
            return true;
        }

        public override void OnContextExit(ExecutionFlow flow)
        {
            if (m_locker)
            {
                m_locker.Lock(m_prevPosition, m_prevRotation, m_prevSpace);
            }
        }

        public override void FastForward()
        {
            base.FastForward();
            Execute(0);
        }

        public override string GetDescription()
        {
            if (m_lockPosition != Axis.None && m_lockRotation != Axis.None)
            {
                return $"Lock {m_target}: Position{m_lockPosition.GetString()} and Rotation{m_lockRotation.GetString()}";
            }
            if (m_lockPosition != Axis.None)
            {
                return $"Lock {m_target}: Position{m_lockPosition.GetString()}";
            }
            else if (m_lockRotation != Axis.None)
            {
                return $"Lock {m_target}: Rotation{m_lockRotation.GetString()}";
            }
            return $"Unlock {m_target}";

        }
    }

}