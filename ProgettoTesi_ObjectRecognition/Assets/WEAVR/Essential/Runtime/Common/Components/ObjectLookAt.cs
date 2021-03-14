using UnityEngine;

namespace TXT.WEAVR.Common
{
    /// <summary>
    /// The Billboard class implements the behaviors needed to keep a GameObject oriented towards the user.
    /// </summary>
    [AddComponentMenu("WEAVR/Components/Look At Object")]
    public class ObjectLookAt : MonoBehaviour
    {
        public enum PivotAxis
        {
            // Most common options, preserving current functionality with the same enum order.
            XY,
            Y,
            // Rotate about an individual axis.
            X,
            Z,
            // Rotate about a pair of axes.
            XZ,
            YZ,
            // Rotate about all axes.
            Free
        }

        /// <summary>
        /// The axis about which the object will rotate.
        /// </summary>
        [Tooltip("Specifies the axis about which the object will rotate.")]
        [SerializeField]
        private PivotAxis pivotAxis = PivotAxis.XY;
        public PivotAxis Axis
        {
            get { return pivotAxis; }
            set { pivotAxis = value; }
        }

        /// <summary>
        /// The target we will orient to. If no target is specified, the main camera will be used.
        /// </summary>
        [Tooltip("Specifies the target we will orient to. If no target is specified, the main camera will be used.")]
        [SerializeField]
        [Draggable]
        private Transform targetTransform;
        public Transform TargetTransform
        {
            get { return targetTransform; }
            set { targetTransform = value; }
        }

        private void OnEnable()
        {
            if (TargetTransform == null)
            {
                TargetTransform = Camera.main.transform;
            }
        }

        /// <summary>
        /// Keeps the object facing the camera.
        /// </summary>
        private void LateUpdate()
        {
            if (TargetTransform == null)
            {
                TargetTransform = Camera.main.transform;
            }

            // Get a Vector that points from the target to the main camera.
            Vector3 directionToTarget = TargetTransform.position - transform.position;

            bool useCameraAsUpVector = true;

            // Adjust for the pivot axis.
            switch (Axis)
            {
                case PivotAxis.X:
                    directionToTarget.x = 0.0f;
                    useCameraAsUpVector = false;
                    break;

                case PivotAxis.Y:
                    directionToTarget.y = 0.0f;
                    useCameraAsUpVector = false;
                    break;

                case PivotAxis.Z:
                    directionToTarget.x = 0.0f;
                    directionToTarget.y = 0.0f;
                    break;

                case PivotAxis.XY:
                    useCameraAsUpVector = false;
                    break;

                case PivotAxis.XZ:
                    directionToTarget.x = 0.0f;
                    break;

                case PivotAxis.YZ:
                    directionToTarget.y = 0.0f;
                    break;

                case PivotAxis.Free:
                default:
                    // No changes needed.
                    break;
            }

            // If we are right next to the camera the rotation is undefined. 
            if (directionToTarget.sqrMagnitude < 0.001f)
            {
                return;
            }

            // Calculate and apply the rotation required to reorient the object
            if (useCameraAsUpVector)
            {
                transform.rotation = Quaternion.LookRotation(-directionToTarget, Camera.main.transform.up);
            }
            else
            {
                transform.rotation = Quaternion.LookRotation(-directionToTarget);
            }
        }
    }
}
