using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Utilities/Transform Lock")]
    [DefaultExecutionOrder(32000)]
    public class TransformLock : MonoBehaviour
    {
        [SerializeField]
        private Space m_lockSpace = Space.World;

        private Vector3 m_fixedPosition;
        private Vector3 m_fixedRotation;

        private Axis m_positionLock;
        private Axis m_rotationLock;

        public Axis PositionLock
        {
            get => m_positionLock;
            set
            {
                if(m_positionLock != value)
                {
                    m_positionLock = value;
                    m_fixedPosition = (m_lockSpace == Space.Self ? transform.localPosition : transform.position).Filter(m_positionLock);
                    CheckIfShouldLock();
                }
            }
        }

        public Axis RotationLock
        {
            get => m_rotationLock;
            set
            {
                if (m_rotationLock != value)
                {
                    m_rotationLock = value;
                    m_fixedRotation = (m_lockSpace == Space.Self ? transform.localEulerAngles : transform.eulerAngles).Filter(m_rotationLock);
                    CheckIfShouldLock();
                }
            }
        }

        public Space LockSpace
        {
            get => m_lockSpace;
            set
            {
                if(m_lockSpace != value)
                {
                    m_lockSpace = value;
                    m_fixedPosition = (m_lockSpace == Space.Self ? transform.localPosition : transform.position).Filter(m_positionLock);
                    m_fixedRotation = (m_lockSpace == Space.Self ? transform.localEulerAngles : transform.eulerAngles).Filter(m_rotationLock);
                }
            }
        }

        private void CheckIfShouldLock() => enabled = m_positionLock != Axis.None || m_rotationLock != Axis.None;

        public void Lock(Axis position, Axis rotation, Space space)
        {
            LockSpace = space;
            PositionLock = position;
            RotationLock = rotation;
        }

        public void Unlock()
        {
            PositionLock = Axis.None;
            RotationLock = Axis.None;
        }

        public void UnlockPosition()
        {
            PositionLock = Axis.None;
        }

        public void UnlockRotation()
        {
            RotationLock = Axis.None;
        }

        private void Update()
        {
            KeepLocked();
        }

        private void LateUpdate()
        {
            KeepLocked();
        }

        private void KeepLocked()
        {
            if (LockSpace == Space.World)
            {
                transform.position = m_fixedPosition.Filter(m_positionLock, transform.position);
                transform.eulerAngles = m_fixedPosition.Filter(m_rotationLock, transform.eulerAngles);
            }
            else
            {
                transform.localPosition = m_fixedPosition.Filter(m_positionLock, transform.localPosition);
                transform.localEulerAngles = m_fixedPosition.Filter(m_rotationLock, transform.localEulerAngles);
            }
        }
    }
}