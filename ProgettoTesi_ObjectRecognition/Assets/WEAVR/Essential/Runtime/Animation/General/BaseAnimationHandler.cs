using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Animation
{
    public abstract class BaseAnimationHandler : ScriptableObject, IAnimationHandler
    {
        protected static Transform s_instantTargetTransform;

        protected AnimationState m_state;
        protected IAnimationData m_data;

        public virtual AnimationState CurrentState {
            get { return m_state; }
            set {
                if (m_state != value) {
                    m_state = value;
                    StateChanged();
                }
            }
        }

        public virtual IAnimationData CurrentData {
            get { return m_data; }
            set {
                if(m_data != value) {
                    m_data = value;
                    DataChanged();
                }
            }
        }

        public abstract Type[] HandledTypes { get; }

        public virtual GameObject GameObject { get; set; }
        protected virtual void DataChanged() { }
        protected virtual void StateChanged() { }
        public abstract void Animate(float dt);

        protected virtual void OnEnable() {
            if (s_instantTargetTransform == null) {
                var go = new GameObject("BridgeTransform_Temporary");
                go.hideFlags = HideFlags.HideAndDontSave;
                s_instantTargetTransform = go.transform;
            }
            CurrentState = AnimationState.NotStarted;
        }

        protected static Transform CreateTemporaryTransform(Transform parent, Vector3 position, Quaternion rotation) {
            var newGO = new GameObject("AnimationHandler_TempTransform");
            newGO.hideFlags = HideFlags.HideAndDontSave;
            newGO.transform.SetParent(parent, false);
            newGO.transform.position = position;
            newGO.transform.rotation = rotation;

            return newGO.transform;
        }

    }
}