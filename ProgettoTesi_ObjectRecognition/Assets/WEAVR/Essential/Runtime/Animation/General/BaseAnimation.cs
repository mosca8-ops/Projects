using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Animation
{
    [System.Serializable]
    public abstract class BaseAnimation : ScriptableObject, IPooledAnimation
    {
        protected static Transform s_instantTargetTransform;

        private IAnimationPool m_pool;

        protected AnimationState m_state;
        protected GameObject m_gameObject;

        protected bool m_restoreOnFinish = false;
        protected ObjectState m_initialState;

        protected bool m_loop = false;
        protected int m_loopCount = 0;

        public virtual AnimationState CurrentState {
            get { return m_state; }
            set {
                if (m_state != value) {
                    m_state = value;
                    StateChanged();
                    if(m_state == AnimationState.Finished || m_state == AnimationState.Stopped) {
                        if(m_loop && --m_loopCount > 0) {
                            m_state = AnimationState.Playing;
                            m_initialState.Restore();
                            OnStart();
                        }
                        else if (m_restoreOnFinish) {
                            m_initialState.Restore();
                        }
                    }
                }
            }
        }

        public virtual GameObject GameObject {
            get { return m_gameObject; }
            set {
                if(m_gameObject != value) {
                    m_gameObject = value;
                    m_restoreOnFinish = false;
                    if(value != null) {
                        m_initialState.Save(value.transform);
                        GameObjectChanged();
                    }
                }
            }
        }
        
        public int Id { get; set; }
        public virtual bool RestoreOnFinish {
            get { return m_restoreOnFinish; }
            set { m_restoreOnFinish = value; }
        }
        public virtual OnAnimationEnded2 AnimationEndCallback { get; set; }
        public virtual void OnStart() {
            if (m_gameObject != null) {
                m_initialState.Save(m_gameObject.transform);
            }
        }
        public abstract void Animate(float dt);
        protected abstract bool DeserializeDataInternal(object[] data);
        protected abstract object[] SerializeDataInternal();
        protected virtual void StateChanged() { }
        protected virtual void GameObjectChanged() { }

        public virtual void Reset() {
            m_initialState.Restore();
        }

        public bool DeserializeData(object[] data) {
            if(data.Length < 3) { return false; }
            return bool.TryParse(data[0].ToString(), out m_restoreOnFinish)
                && bool.TryParse(data[1].ToString(), out m_loop)
                && int.TryParse(data[2].ToString(), out m_loopCount)
                && DeserializeDataInternal(GetRemainingData(3, data));
        }

        public object[] SerializeData() {
            List<object> data = new List<object>() { m_restoreOnFinish, m_loop, m_loopCount };
            data.AddRange(SerializeDataInternal());
            return data.ToArray();
        }

        private object[] GetRemainingData(int startIndex, object[] data) {
            object[] newData = new object[data.Length - startIndex];
            for (int i = 0; i < newData.Length; i++) {
                newData[i] = data[i + startIndex];
            }
            return newData;
        }

        protected virtual void OnEnable() {
            if (s_instantTargetTransform == null) {
                var go = new GameObject("BridgeTransform_Temporary");
                go.hideFlags = HideFlags.HideAndDontSave;
                s_instantTargetTransform = go.transform;
            }
            CurrentState = AnimationState.NotStarted;
        }

        protected static Transform CreateTemporaryTransform(Transform parent, bool local, Vector3 position, Quaternion rotation) {
            var newGO = new GameObject("Animation_TempTransform");
            newGO.hideFlags = HideFlags.HideAndDontSave;
            newGO.transform.SetParent(parent, false);
            if (local) {
                newGO.transform.localPosition = position;
                newGO.transform.localRotation = rotation;
            }
            else {
                newGO.transform.position = position;
                newGO.transform.rotation = rotation;
            }

            return newGO.transform;
        }

        protected struct ObjectState
        {
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;

            public Vector3 position;
            public Quaternion rotation;

            public Transform target;
            public Transform parent;

            public void Save(Transform target) {
                this.target = target;
                parent = target.parent;

                localPosition = target.localPosition;
                localRotation = target.localRotation;
                localScale = target.localScale;

                position = target.position;
                rotation = target.rotation;
            }

            public void Restore() {
                if(target == null) { return; }
                if(target.parent != parent) {
                    target.SetPositionAndRotation(position, rotation);
                }
                else {
                    target.localPosition = localPosition;
                    target.localRotation = localRotation;
                }
                target.localScale = localScale;
            }
        }

        #region [  POOL  ]

        public void SetPool(IAnimationPool pool) {
            m_pool = pool;
        }

        public virtual void OnDiscard() {
            if (m_pool != null) {
                m_pool.Reclaim(this);
            }
        }

        #endregion
    }
}