using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Components/Pose Store")]
    public class PoseStore : MonoBehaviour
    {
        private Pose m_savedPose;

        public void HandlePose(bool restore) {
            if (restore) { RestorePose(); }
            else { SavePose(); }
        }

        public void SavePose() {
            m_savedPose.UpdateFrom(transform);
        }

        public void RestorePose() {
            m_savedPose.TryRestore(transform);
        }

        public void RestoreLocalPose() {
            m_savedPose.TryRestoreLocal(transform);
        }

        protected struct Pose
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 lossyScale;

            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;

            public System.DateTime Timestamp { get; private set; }
            private bool m_canRestore;

            public void UpdateFrom(Transform transform) {
                position = transform.position;
                rotation = transform.rotation;

                localPosition = transform.localPosition;
                localRotation = transform.localRotation;
                localScale = transform.localScale;

                lossyScale = transform.lossyScale;

                Timestamp = System.DateTime.Now;
                m_canRestore = true;
            }

            public void UpdateFrom(GameObject gameObject) {
                if (gameObject != null) {
                    UpdateFrom(gameObject.transform);
                }
            }

            public void TryRestore(Transform transformToRestore, bool forceRestore = false) {
                if (m_canRestore || forceRestore) {
                    transformToRestore.SetPositionAndRotation(position, rotation);
                    transformToRestore.localScale = localScale;
                }
                m_canRestore = false;
            }

            public void TryRestoreLocal(Transform transformToRestore, bool forceRestore = false) {
                if (m_canRestore || forceRestore) {
                    transformToRestore.localPosition = localPosition;
                    transformToRestore.localRotation = localRotation;
                    transformToRestore.localScale = localScale;
                }
                m_canRestore = false;
            }
        }
    }
}
