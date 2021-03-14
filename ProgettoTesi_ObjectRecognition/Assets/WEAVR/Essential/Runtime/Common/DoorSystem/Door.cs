//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Events;

//namespace TXT.WEAVR.Common
//{
//    [System.Serializable]
//    public class DoorOpenEvent : UnityEvent<float> { }

//    public interface IDoorOpeningData
//    {
//        void Setup(AbstractDoor door);
//        bool UpdatePosition(AbstractDoor door);
//        void Cleanup(AbstractDoor door);

//        void OpenTo(AbstractDoor door, float openRatio);

//        void SnapshotClosed(AbstractDoor door);
//        void SnapshotFullyOpen(AbstractDoor door);
//        bool IsClosed(AbstractDoor door);
//        bool IsFullyOpen(AbstractDoor door);
//        bool IsValid(AbstractDoor door);
//    }

//    //public abstract class AbstractDoorOpening : IDoorOpeningData
//    //{

//    //}

//    public class Door : MonoBehaviour
//    {
//        public enum OpeningMode {
//            Free = 1,
//            Slide = 2,
//            Hinge = 4
//        }

//        [Header("Configuration")]
//        [SerializeField]
//        [InvokeOnChange(nameof(UpdateOpeningMode))]
//        protected OpeningMode m_openingMode = OpeningMode.Free;

//        [SerializeField]
//        [ShowOnEnum(nameof(m_openingMode), (int)OpeningMode.Free)]
//        protected FreeData m_freeData = new FreeData();
//        [SerializeField]
//        [ShowOnEnum(nameof(m_openingMode), (int)OpeningMode.Slide)]
//        protected SlideData m_slideData = new SlideData();
//        [SerializeField]
//        [ShowOnEnum(nameof(m_openingMode), (int)OpeningMode.Hinge)]
//        protected HingeData m_hingeData = new HingeData();
        
//        [Space]
//        [SerializeField]
//        protected bool m_canBeLocked = true;
//        [SerializeField]
//        protected bool m_preferPhysics = true;
//        [Space]
//        [SerializeField]
//        protected bool m_blockOnFullyOpened = true;
//        [SerializeField]
//        protected bool m_blockOnClosed = true;
//        [SerializeField]
//        protected bool m_snapOnClosed = true;
//        [SerializeField]
//        [DisabledBy(nameof(m_canBeLocked))]
//        [RangeFrom(0, nameof(m_locks))]
//        protected int m_locksThreshold;

//        [Header("Components")]
//        [SerializeField]
//        protected DoorHandle m_doorHandle;
//        [SerializeField]
//        [DisabledBy(nameof(m_canBeLocked))]
//        protected List<DoorLock> m_locks;

//        [Header("Events")]
//        [SerializeField]
//        protected EventsContainer m_events;
//        [SerializeField]
//        protected AdvancedEventsContainer m_advancedEvents;

//        [SerializeField]
//        [HideInInspector]
//        protected Vector3 m_closedLocalPosition;
//        [SerializeField]
//        [HideInInspector]
//        protected Quaternion m_closedLocalRotation;

//        protected bool m_isClosed;
//        protected bool m_isFullyOpened;
//        protected bool m_isLocked;

//        protected Rigidbody m_rigidBody;

//        private IDoorOpeningData m_currentData;
//        private OpeningMode m_prevOpeningMode = OpeningMode.Free;

//        protected float m_currentOpening;
//        protected float? m_targetOpening;

//        public DoorHandle Handle => m_doorHandle;

//        public float CurrentOpenProgress
//        {
//            get
//            {
//                return m_currentOpening;
//            }
//            set
//            {
//                if(m_currentOpening != value)
//                {
//                    if(value <= 0)
//                    {
//                        m_currentOpening = 0;
//                        IsClosed = true;
//                        IsFullyOpened = false;
//                    }
//                    else if(value >= 1)
//                    {
//                        m_currentOpening = 1;
//                        IsFullyOpened = true;
//                        IsClosed = false;
//                    }
//                    else
//                    {
//                        m_currentOpening = value;
//                    }
//                    m_advancedEvents.OnDoorOpenProgress.Invoke(m_currentOpening);
//                }
//            }
//        }

//        protected IDoorOpeningData CurrentOpeningData {
//            get {
//                return m_currentData;
//            }
//            set {
//                if(m_currentData != value)
//                {
//                    m_currentData?.Cleanup(this);
//                    m_currentData = value;
//                    m_currentData?.Setup(this);
//                }
//            }
//        }

//        public OpeningMode Mode => m_openingMode;

//        public bool IsClosed {
//            get {
//                return m_isClosed;
//            }
//            set {
//                if(m_isClosed != value)
//                {
//                    m_isClosed = value;
//                    if (value)
//                    {
//                        m_events.OnDoorClosed.Invoke();
//                        if (m_blockOnClosed)
//                        {
//                            if (m_preferPhysics)
//                            {
//                                m_rigidBody.isKinematic = true;
//                            }
//                        }
//                        if (m_snapOnClosed)
//                        {
//                            bool hadGravity = m_rigidBody.useGravity;
//                            m_rigidBody.useGravity = false;
//                            StartCoroutine(DelayedAction(0.08f, () =>
//                            {
//                                m_rigidBody.isKinematic = true;
//                                transform.localPosition = m_closedLocalPosition;
//                                transform.localRotation = m_closedLocalRotation;
//                                m_rigidBody.useGravity = hadGravity;
//                            }));
//                        }
//                    }
//                    else if(!IsLocked)
//                    {
//                        if (m_blockOnClosed)
//                        {
//                            if(m_preferPhysics && m_rigidBody.isKinematic)
//                            {
//                                m_rigidBody.isKinematic = false;
//                            }
//                        }
//                        m_events.OnDoorOpened.Invoke();
//                    }
//                    else
//                    {
//                        m_isClosed = true;
//                    }
//                }
//            }
//        }

//        public bool IsFullyOpened {
//            get {
//                return m_isFullyOpened;
//            }
//            set {
//                if(m_isFullyOpened != value)
//                {
//                    m_isFullyOpened = value;
//                    if (value)
//                    {
//                        m_advancedEvents.OnDoorFullyOpened.Invoke();
//                        if (m_blockOnFullyOpened)
//                        {
//                            if (m_preferPhysics)
//                            {
//                                m_rigidBody.isKinematic = true;
//                            }
//                        }
//                    }
//                    else if (m_blockOnFullyOpened)
//                    {
//                        if (m_preferPhysics)
//                        {
//                            m_rigidBody.isKinematic = false;
//                        }
//                    }
//                }
//            }
//        }

//        public bool IsLocked {
//            get {
//                return m_isLocked;
//            }
//            set {
//                if(m_isLocked != value)
//                {
//                    m_isLocked = value && m_canBeLocked;
//                    if (value)
//                    {
//                        m_events.OnDoorLocked.Invoke();
//                    }
//                    else
//                    {
//                        m_events.OnDoorUnlocked.Invoke();
//                    }
//                }
//            }
//        }

//        public bool IsActive {
//            get { return enabled; }
//            set {
//                if(enabled != value)
//                {
//                    enabled = value;
//                }
//            }
//        }

//        public void SetIsActive(bool active)
//        {
//            IsActive = active && CurrentOpeningData.IsValid(this);
//        }

//        private void OnValidate()
//        {
//            if(m_currentData == null)
//            {
//                UpdateOpeningMode();
//            }
//        }

//        private void Awake()
//        {
//            m_rigidBody = GetComponent<Rigidbody>();
//            if(m_preferPhysics && m_rigidBody == null)
//            {
//                m_rigidBody = gameObject.AddComponent<Rigidbody>();
//                m_rigidBody.isKinematic = true;
//            }
//            UpdateOpeningMode();
//        }

//        private IEnumerator DelayedAction(float delay, Action action)
//        {
//            yield return new WaitForSeconds(delay);
//            action();
//        }

//        // Use this for initialization
//        void Start()
//        {
//            enabled &= CurrentOpeningData.IsValid(this);
//            if(m_preferPhysics && m_rigidBody != null)
//            {
//                m_rigidBody.isKinematic = true;
//            }
//            CheckIfLocked();
//            for (int i = 0; i < m_locks.Count; i++)
//            {
//                m_locks[i].OnLock.RemoveListener(CheckIfLocked);
//                m_locks[i].OnLock.AddListener(CheckIfLocked);
//                m_locks[i].OnUnlock.RemoveListener(CheckIfLocked);
//                m_locks[i].OnUnlock.AddListener(CheckIfLocked);
//            }
//            if (m_doorHandle != null && m_doorHandle.GetComponent<Rigidbody>() != null)
//            {
//                var handleRigidBody = m_doorHandle.GetComponent<Rigidbody>();
//                var fixedJoint = gameObject.AddComponent<FixedJoint>();
//                fixedJoint.connectedBody = handleRigidBody;
//                handleRigidBody.isKinematic = true;
//            }

//            UpdateValues();

//            //if (IsClosed)
//            //{
//            //    m_closedLocalPosition = transform.localPosition;
//            //    m_closedLocalRotation = transform.localRotation;
//            //}
//        }

//        // Update is called once per frame
//        void FixedUpdate()
//        {
//            if (m_targetOpening.HasValue)
//            {
//                CurrentOpeningData?.OpenTo(this, m_targetOpening.Value);
//            }
//            if (!m_preferPhysics)
//            {
//                UpdatePosition();
//            }
//            if (!IsLocked)
//            {
//                UpdateValues();
//            }
//            else if (m_preferPhysics)
//            {
//                m_rigidBody.isKinematic = true;
//            }
//        }

//        public void Open()
//        {
//            if (IsClosed)
//            {
//                m_targetOpening = 1;
//            }
//        }

//        public void Close()
//        {
//            if (!IsClosed)
//            {
//                m_targetOpening = 0;
//            }
//        }

//        public void Unlock()
//        {
//            if (!IsLocked) { return; }

//            for (int i = 0; i < m_locks.Count; i++)
//            {
//                m_locks[i].UnLock();
//            }
//        }

//        public void Lock()
//        {
//            if (!IsClosed || IsLocked) { return; }

//            for (int i = 0; i < m_locks.Count && i <= m_locksThreshold; i++)
//            {
//                m_locks[i].Lock();
//            }
//        }

//        public void SnapshotClosed()
//        {
//            CurrentOpeningData.SnapshotClosed(this);
//            m_closedLocalPosition = transform.localPosition;
//            m_closedLocalRotation = transform.localRotation;
//        }

//        public void SnapshotFullyOpen()
//        {
//            CurrentOpeningData.SnapshotFullyOpen(this);
//        }

//        protected virtual void UpdatePosition()
//        {
//            CurrentOpeningData.UpdatePosition(this);
//        }

//        private void UpdateValues()
//        {
//            bool doorHandleIsActive = m_doorHandle == null || !m_doorHandle.isActive;
//            IsClosed = CurrentOpeningData.IsClosed(this) && doorHandleIsActive;
//            IsFullyOpened = CurrentOpeningData.IsFullyOpen(this) && doorHandleIsActive;
//        }

//        private void UpdateOpeningMode()
//        {
//            OnUpdateOpeningMode(m_prevOpeningMode, m_openingMode);
//            m_prevOpeningMode = m_openingMode;
//        }

//        protected virtual void OnUpdateOpeningMode(OpeningMode prevMode, OpeningMode currentMode)
//        {
//            switch (currentMode)
//            {
//                case OpeningMode.Free:
//                    CurrentOpeningData = m_freeData;
//                    break;
//                case OpeningMode.Hinge:
//                    CurrentOpeningData = m_hingeData;
//                    break;
//                case OpeningMode.Slide:
//                    CurrentOpeningData = m_slideData;
//                    break;
//            }
//        }

//        internal void RegisterLock(DoorLock doorLock)
//        {
//            if(m_locks == null)
//            {
//                m_locks = new List<DoorLock>();
//            }
//            if (!m_locks.Contains(doorLock))
//            {
//                m_locks.Add(doorLock);
//                doorLock.OnLock.RemoveListener(CheckIfLocked);
//                doorLock.OnLock.AddListener(CheckIfLocked);
//                doorLock.OnUnlock.RemoveListener(CheckIfLocked);
//                doorLock.OnUnlock.AddListener(CheckIfLocked);
//            }
//        }

//        protected void CheckIfLocked()
//        {
//            int unlockedLocks = 0;
//            for (int i = 0; i < m_locks.Count; i++)
//            {
//                if(m_locks[i] == null || m_locks[i].door != this)
//                {
//                    m_locks.RemoveAt(i--);
//                    continue;
//                }
//                if (!m_locks[i].IsLocked)
//                {
//                    unlockedLocks++;
//                }
//            }
//            IsLocked = unlockedLocks < m_locksThreshold;
//        }

//        [Serializable]
//        protected class SlideData : IDoorOpeningData
//        {
//            public Vector3 fullyOpenPosition;
//            public Vector3 closedPosition;
//            public float closingDistance;
            
//            private Vector3 m_direction;
//            private Transform m_closedTransform;
//            private Transform m_openedTransform;

//            public void Setup(AbstractDoor door)
//            {
//                if (Mathf.Approximately(closingDistance, 0))
//                {
//                    closingDistance = 0.05f;
//                }
//                InstantiateTransform(ref m_closedTransform, door, "temp_Closed", closedPosition);
//                InstantiateTransform(ref m_openedTransform, door, "temp_Opened", fullyOpenPosition);
//            }

//            private void InstantiateTransform(ref Transform transform, AbstractDoor door, string name, Vector3 position)
//            {
//                if(transform == null)
//                {
//                    transform = new GameObject(name).transform;
//                    transform.gameObject.hideFlags = HideFlags.HideAndDontSave;
//                }

//                transform.SetParent(door.transform.parent, false);
//                transform.localPosition = position;
//            }

//            public void Cleanup(AbstractDoor door)
//            {
//                if(m_closedTransform != null)
//                {
//                    Destroy(m_closedTransform.gameObject);
//                }
//                if(m_openedTransform != null)
//                {
//                    Destroy(m_openedTransform.gameObject);
//                }
//            }

//            public bool UpdatePosition(AbstractDoor door)
//            {
//                return false;
//            }

//            public bool IsClosed(AbstractDoor door)
//            {
//                return Vector3.Distance(closedPosition, door.transform.localPosition) < closingDistance;
//            }

//            public bool IsFullyOpen(AbstractDoor door)
//            {
//                return Vector3.Distance(fullyOpenPosition, door.transform.localPosition) < closingDistance;
//            }

//            public void SnapshotClosed(AbstractDoor door)
//            {
//                closedPosition = door.transform.localPosition;
//            }

//            public void SnapshotFullyOpen(AbstractDoor door)
//            {
//                fullyOpenPosition = door.transform.localPosition;
//            }

//            public bool IsValid(AbstractDoor door)
//            {
//                return true;
//            }

//            public void OpenTo(AbstractDoor door, float openRatio)
//            {
//                throw new NotImplementedException();
//            }
//        }

//        [Serializable]
//        protected class HingeData : IDoorOpeningData
//        {
//            [Tooltip("Closing distance threshold in degrees")]
//            public float closingThreshold;
//            [InvokeOnChange(nameof(DataUpdated))]
//            public Transform rotationPoint;
//            [InvokeOnChange(nameof(DataUpdated))]
//            public Vector3 rotationAxis;
//            public HingeJoint hinge;
//            public Span limits;

//            [ShowAsReadOnly]
//            public float closedRotation;
//            [ShowAsReadOnly]
//            public float fullyOpenedRotation;

//            private AbstractDoor m_door;

//            public HingeData()
//            {
//                closingThreshold = 0.5f;
//                m_door = null;
//                hinge = null;
//                rotationPoint = null;
//                rotationAxis = new Vector3(0, 1, 0);
//                limits = new Span(0, 120);

//                closedRotation = 0;
//                fullyOpenedRotation = 0;
//            }

//            public void Setup(AbstractDoor door)
//            {
//                m_door = door;
//                hinge = door.GetComponent<HingeJoint>();
//                if(hinge == null)
//                {
//                    hinge = door.gameObject.AddComponent<HingeJoint>();
//                    hinge.useLimits = true;
//                    UpdateHingeLimits();

//                    hinge = door.GetComponent<HingeJoint>();
//                }
//                if (rotationPoint == null)
//                {
//                    rotationPoint = door.transform;
//                }
//                DataUpdated();
//            }

//            private void UpdateHingeLimits()
//            {
//                var hingeLimits = hinge.limits;
//                hingeLimits.min = limits.min;
//                hingeLimits.max = limits.max;
//                hinge.limits = hingeLimits;
//            }

//            public void Cleanup(AbstractDoor door)
//            {
//                if(hinge != null)
//                {
//                    if (Application.isPlaying)
//                    {
//                        Destroy(hinge);
//                    }
//                    else
//                    {
//                        DestroyImmediate(hinge);
//                    }
//                    hinge = null;
//                }
//            }

//            private void DataUpdated()
//            {
//                if(m_door == null) { return; }
//                hinge.anchor = m_door.transform.InverseTransformPoint(rotationPoint.position);
//                hinge.axis = rotationAxis;
//                UpdateHingeLimits();
//            }

//            public bool UpdatePosition(AbstractDoor door)
//            {
//                return false;
//            }

//            public void SnapshotClosed(AbstractDoor door)
//            {
//                closedRotation = Vector3.Dot(door.transform.localEulerAngles, rotationAxis);
//                limits.max = fullyOpenedRotation - closedRotation;
//                UpdateHingeLimits();
//            }

//            public bool IsClosed(AbstractDoor door)
//            {
//                return Mathf.Abs(Vector3.Dot(door.transform.localEulerAngles, rotationAxis) - closedRotation) < closingThreshold;
//            }

//            public bool IsFullyOpen(AbstractDoor door)
//            {
//                return Mathf.Abs(Vector3.Dot(door.transform.localEulerAngles, rotationAxis) - fullyOpenedRotation) < closingThreshold;
//            }

//            public void SnapshotFullyOpen(AbstractDoor door)
//            {
//                fullyOpenedRotation = Vector3.Dot(door.transform.localEulerAngles, rotationAxis);
//                limits.max = fullyOpenedRotation - closedRotation;
//                UpdateHingeLimits();
//            }

//            public bool IsValid(AbstractDoor door)
//            {
//                return rotationPoint != null;
//            }

//            public void OpenTo(AbstractDoor door, float openRatio)
//            {
//                throw new NotImplementedException();
//            }
//        }

//        [Serializable]
//        protected class FreeData : IDoorOpeningData
//        {
//            [HideInInspector]
//            public Vector3 closedPosition;
//            public float closingDistance;
//            public Vector3 defaultOpenDirection = Vector3.forward;

//            public void Setup(AbstractDoor door)
//            {
//                if (Mathf.Approximately(closingDistance, 0))
//                {
//                    closingDistance = 0.05f;
//                }
//            }

//            public void Cleanup(AbstractDoor door)
//            {
                
//            }

//            public bool UpdatePosition(AbstractDoor door)
//            {
//                return door.Handle != null ? door.Handle.transform.hasChanged : door.transform.hasChanged;
//            }

//            public void SnapshotClosed(AbstractDoor door)
//            {
//                closedPosition = door.transform.localPosition;
//            }

//            public bool IsClosed(AbstractDoor door)
//            {
//                return Vector3.Distance(closedPosition, door.transform.localPosition) < closingDistance;
//            }

//            public bool IsFullyOpen(AbstractDoor door)
//            {
//                return !door.IsClosed;
//            }

//            public void SnapshotFullyOpen(AbstractDoor door)
//            {
                
//            }

//            public bool IsValid(AbstractDoor door)
//            {
//                return true;
//            }

//            public void OpenTo(AbstractDoor door, float openRatio)
//            {
//                throw new NotImplementedException();
//            }
//        }

//        [Serializable]
//        protected struct EventsContainer
//        {
//            public UnityEvent OnDoorClosed;
//            public UnityEvent OnDoorOpened;
//            public UnityEvent OnDoorUnlocked;
//            public UnityEvent OnDoorLocked;
//        }

//        [Serializable]
//        protected struct AdvancedEventsContainer
//        {
//            public UnityEvent OnDoorFullyOpened;
//            public DoorOpenEvent OnDoorOpenProgress;
//        }
//    }
//}
