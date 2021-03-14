using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Components/Transform Changer")]
    public class TransformChanger : MonoBehaviour
    {
        public Vector3[] predefinedPositions;

        private Transform m_originalParent;
        private Vector3 m_originalPosition;
        private Quaternion m_originalRotation;
        private Vector3 m_originalScale;

        public float X { get => transform.position.x; set => transform.position = new Vector3(value, transform.position.y, transform.position.z); }
        public float Y { get => transform.position.y; set => transform.position = new Vector3(transform.position.x, value, transform.position.z); }
        public float Z { get => transform.position.z; set => transform.position = new Vector3(transform.position.x, transform.position.y, value); }

        public float LocalX { get => transform.localPosition.x; set => transform.localPosition = new Vector3(value, transform.localPosition.y, transform.localPosition.z); }
        public float LocalY { get => transform.localPosition.y; set => transform.localPosition = new Vector3(transform.localPosition.x, value, transform.localPosition.z); }
        public float LocalZ { get => transform.localPosition.z; set => transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, value); }

        public float MoveByX { get => transform.position.x; set => transform.position = new Vector3(transform.position.x + value, transform.position.y, transform.position.z); }
        public float MoveByY { get => transform.position.y; set => transform.position = new Vector3(transform.position.x, transform.position.y + value, transform.position.z); }
        public float MoveByZ { get => transform.position.z; set => transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z + value); }

        public float MoveLocalByX { get => transform.localPosition.x; set => transform.localPosition = new Vector3(transform.localPosition.x + value, transform.localPosition.y, transform.localPosition.z); }
        public float MoveLocalByY { get => transform.localPosition.y; set => transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y + value, transform.localPosition.z); }
        public float MoveLocalByZ { get => transform.localPosition.z; set => transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, transform.localPosition.z + value); }

        public float ScaleX { get => transform.localScale.x; set => transform.localScale = new Vector3(value, transform.localScale.y, transform.localScale.z); }
        public float ScaleY { get => transform.localScale.y; set => transform.localScale = new Vector3(transform.localScale.x, value, transform.localScale.z); }
        public float ScaleZ { get => transform.localScale.z; set => transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, value); }

        public float RescaleX { get => transform.localScale.x; set => transform.localScale = new Vector3(transform.localScale.x + value, transform.localScale.y, transform.localScale.z); }
        public float RescaleY { get => transform.localScale.y; set => transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y + value, transform.localScale.z); }
        public float RescaleZ { get => transform.localScale.z; set => transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z + value); }

        public float RotationX { get => transform.localEulerAngles.x; set => transform.localEulerAngles = new Vector3(value, transform.localEulerAngles.y, transform.localEulerAngles.z); }
        public float RotationY { get => transform.localEulerAngles.y; set => transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, value, transform.localEulerAngles.z); }
        public float RotationZ { get => transform.localEulerAngles.z; set => transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, value); }

        public float RotateByX { get => transform.localEulerAngles.x; set => transform.localEulerAngles = new Vector3(transform.localEulerAngles.x + value, transform.localEulerAngles.y, transform.localEulerAngles.z); }
        public float RotateByY { get => transform.localEulerAngles.y; set => transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y + value, transform.localEulerAngles.z); }
        public float RotateByZ { get => transform.localEulerAngles.z; set => transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, transform.localEulerAngles.z + value); }

        private void Awake()
        {
            m_originalParent = transform.parent;
            m_originalPosition = transform.localPosition;
            m_originalRotation = transform.localRotation;
            m_originalScale = transform.localScale;
        }

        public void SetPositionAndRotationLike(Transform other)
        {
            if(!other) { return; }
            transform.SetPositionAndRotation(other.position, other.rotation);
        }

        public void SetPositionLike(Transform other)
        {
            if (!other) { return; }
            transform.position = other.position;
        }

        public void SetRotationLike(Transform other)
        {
            if (!other) { return; }
            transform.rotation = other.rotation;
        }

        public void SetPredefinedPosition(int index)
        {
            if(0 <= index && index < predefinedPositions.Length)
            {
                transform.position = predefinedPositions[index];
            }
        }

        public void ResetTransformGlobally()
        {
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            transform.localScale = Vector3.one;
        }

        public void ResetTransformLocally()
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        public void SetParentInOrigin(Transform parent)
        {
            transform.SetParent(parent, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        public void RestoreOriginalParent() => SetParentInOrigin(m_originalParent);

        public void RestoreCompletely()
        {
            RestoreOriginalParent();
            transform.localPosition = m_originalPosition;
            transform.localRotation = m_originalRotation;
            transform.localScale = m_originalScale;
        }
    }
}
