namespace TXT.WEAVR.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public struct Pose
    {
        public Vector3 position;
        public Quaternion rotation;

        [SerializeField]
        private Vector3 euler;

        public Pose(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
            euler = rotation.eulerAngles;
        }

        public Pose(Vector3 position)
        {
            this.position = position;
            this.rotation = Quaternion.identity;

            euler = rotation.eulerAngles;
        }

        public Pose(Quaternion rotation)
        {
            this.position = Vector3.zero;
            this.rotation = rotation;

            euler = rotation.eulerAngles;
        }

        public Pose(Transform transform)
        {
            this.position = transform.position;
            this.rotation = transform.localRotation;

            euler = rotation.eulerAngles;
        }

        public void Set(Transform transform)
        {
            this.position = transform.position;
            this.rotation = transform.localRotation;

            euler = rotation.eulerAngles;
        }
        
        public void UpdateRotation()
        {

        }

        public static Pose operator +(Pose pose, Vector3 position)
        {
            pose.position += position;
            return pose;
        }

        public static Pose operator -(Pose pose, Vector3 position)
        {
            pose.position -= position;
            return pose;
        }

        public static Pose operator +(Pose pose, Transform transform)
        {
            pose.position += transform.position;
            return pose;
        }

        public static Pose operator -(Pose pose, Transform transform)
        {
            pose.position -= transform.position;
            return pose;
        }
    }
}