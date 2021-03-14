using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("")]
    public class OccupiedSpace : MonoBehaviour
    {
        [SerializeField]
        [ShowAsReadOnly]
        private Collider m_trigger;

        [SerializeField]
        private bool m_autoPushOut = true;

        public Collider Trigger { get { return m_trigger; } }
        public bool AutoPushOut { get { return m_autoPushOut; } set { m_autoPushOut = value; } }

        private void OnTriggerStay(Collider other)
        {
            if (m_autoPushOut)
            {

            }
        }

        private void OnCollisionStay(Collision collision)
        {

        }

        #region [  STATIC PART  ]

        public static OccupiedSpace Create(Transform parent, GameObject go, float minVoxelSize = 0.1f)
        {
            OccupiedSpace space = GetComponentInFirstChildren<OccupiedSpace>(go);
            if (space == null)
            {
                bool wasActive = go.activeInHierarchy;
                go.SetActive(true);
                var lastPosition = go.transform.position;
                var lastRotation = go.transform.rotation;
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);


                Collider existingCollider = go.GetComponent<Collider>();
                Bounds bounds = new Bounds();
                if (existingCollider != null)
                {
                    bounds = existingCollider.bounds;
                }
                else
                {
                    var renderer = go.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        bounds = renderer.bounds;
                    }
                }

                GameObject child = new GameObject("Occupied Space");
                child.transform.SetParent(go.transform, true);
                child.layer = WorldBounds.GetOccupancySpacePassiveLayer();
                space = AddComponent<OccupiedSpace>(child);

                CreateBoxCollider(child, space, bounds.center, bounds.size);

                go.transform.SetPositionAndRotation(lastPosition, lastRotation);
                go.SetActive(wasActive);

                //var meshFilter = go.GetComponent<MeshFilter>();
                //if(meshFilter != null) {
                //    GetOccupancyGrid(meshFilter.sharedMesh, minVoxelSize);
                //}
            }
            return space;
        }

        public static void OptimizeSpace(OccupiedSpace occupiedSpace, params Transform[] freeSpaceSamples)
        {
            if (occupiedSpace == null) { return; }
            var childrenSpaces = occupiedSpace.transform.parent.GetComponentsInChildren<OccupiedSpace>(true).ToList();
            var rootBounds = occupiedSpace.Trigger.bounds;
            List<OccupiedSpace> spacesToRemove = new List<OccupiedSpace>();
            Dictionary<OccupiedSpace, Bounds> bounds = childrenSpaces.ToDictionary(s => s, s => s.Trigger.bounds);
            bounds.Remove(occupiedSpace);

            for (int i = 0; i < childrenSpaces.Count; i++)
            {
                var space = childrenSpaces[i];
                if (space == occupiedSpace || !space.gameObject.isStatic) { continue; }
                bool needsRemoval = false;
                var min = space.Trigger.bounds.min;
                var max = space.Trigger.bounds.max;
                if (rootBounds.Contains(min) && rootBounds.Contains(max))
                {
                    needsRemoval = true;
                }
                else if (freeSpaceSamples.Any(s => space.transform.parent.gameObject != s.gameObject
                      && space.Trigger.bounds.Contains(s.position)))
                {
                    needsRemoval = true;
                }
                else
                {
                    foreach (var keyPair in bounds)
                    {
                        if (keyPair.Key != space && keyPair.Value.Contains(min) && keyPair.Value.Contains(max))
                        {
                            needsRemoval = true;
                            break;
                        }
                    }
                }
                if (needsRemoval)
                {
                    bounds.Remove(space);
                    childrenSpaces.RemoveAt(i--);
                    DestroySpace(space);
                }
            }

            if (freeSpaceSamples.Any(s => s.gameObject != occupiedSpace.transform.parent.gameObject && rootBounds.Contains(s.position)))
            {
                DestroySpace(occupiedSpace);
            }
        }

        private static void CreateBoxCollider(GameObject go, OccupiedSpace space, Vector3 center, Vector3 size)
        {
            var boxCollider = AddComponent<BoxCollider>(go);
            boxCollider.center = center;
            boxCollider.size = size;
            //boxCollider.isTrigger = true;
            space.m_trigger = boxCollider;
        }

        public static void DestroySpace(GameObject go)
        {
            OccupiedSpace space = go.GetComponent<OccupiedSpace>();
            if (space != null)
            {
                //if (space.m_trigger != null) {
                //    RemoveObject(space.m_trigger);
                //}
                RemoveObject(space.gameObject);
            }
        }

        public static void DestroySpace(OccupiedSpace space)
        {
            if (space != null)
            {
                //if (space.m_trigger != null) {
                //    RemoveObject(space.m_trigger);
                //}
                RemoveObject(space.gameObject);
            }
        }

        public static T GetComponentInFirstChildren<T>(GameObject go)
        {
            foreach (Transform child in go.transform)
            {
                T component = child.GetComponent<T>();
                if (component != null) { return component; }
            }
            return default(T);
        }

        public static Bounds[] GetOccupancyGrid(Mesh mesh, float minVoxelSize)
        {
            List<Bounds> bounds = new List<Bounds>();
            int voxelsOnX = Mathf.CeilToInt(mesh.bounds.size.x / minVoxelSize) + 1;
            int voxelsOnY = Mathf.CeilToInt(mesh.bounds.size.y / minVoxelSize) + 1;
            int voxelsOnZ = Mathf.CeilToInt(mesh.bounds.size.z / minVoxelSize) + 1;

            Vector3 min = mesh.bounds.min;
            float minX = min.x;
            float minY = min.y;
            float minZ = min.z;

            Bounds[,,] voxels = new Bounds[voxelsOnX, voxelsOnY, voxelsOnZ];
            bool[,,] occupiedCells = new bool[voxelsOnX, voxelsOnY, voxelsOnZ];
            HashSet<Segment> computedSegments = new HashSet<Segment>();

            Bounds sampleVoxel = new Bounds(Vector3.zero, Vector3.one * minVoxelSize);
            Segment[] segments = new Segment[3];
            List<Vector3> vertices = new List<Vector3>();
            mesh.GetVertices(vertices);
            var trianglesIndices = mesh.triangles;
            for (int i = 0; i < trianglesIndices.Length; i += 3)
            {
                var line1 = new Segment(vertices[trianglesIndices[i]], vertices[trianglesIndices[i + 1]]);
                var line2 = new Segment(vertices[trianglesIndices[i + 1]], vertices[trianglesIndices[i + 2]]);
                var line3 = new Segment(vertices[trianglesIndices[i + 2]], vertices[trianglesIndices[i]]);

                if (!computedSegments.Contains(line1))
                {
                    line1.Split(minVoxelSize);
                    foreach (var point in line1)
                    {
                        Vector3 relPoint = point - min;
                        occupiedCells[Mathf.Max(0, Mathf.FloorToInt(relPoint.x / minVoxelSize)),
                                      Mathf.Max(0, Mathf.FloorToInt(relPoint.y / minVoxelSize)),
                                      Mathf.Max(0, Mathf.FloorToInt(relPoint.z / minVoxelSize))] = true;
                    }

                    computedSegments.Add(line1);
                }

                if (!computedSegments.Contains(line2))
                {
                    line2.Split(minVoxelSize);
                    foreach (var point in line2)
                    {
                        Vector3 relPoint = point - min;
                        occupiedCells[Mathf.Max(0, Mathf.FloorToInt(relPoint.x / minVoxelSize)),
                                      Mathf.Max(0, Mathf.FloorToInt(relPoint.y / minVoxelSize)),
                                      Mathf.Max(0, Mathf.FloorToInt(relPoint.z / minVoxelSize))] = true;
                    }

                    computedSegments.Add(line2);
                }

                if (!computedSegments.Contains(line3))
                {
                    line3.Split(minVoxelSize);
                    foreach (var point in line3)
                    {
                        Vector3 relPoint = point - min;
                        occupiedCells[Mathf.Max(0, Mathf.FloorToInt(relPoint.x / minVoxelSize)),
                                      Mathf.Max(0, Mathf.FloorToInt(relPoint.y / minVoxelSize)),
                                      Mathf.Max(0, Mathf.FloorToInt(relPoint.z / minVoxelSize))] = true;
                    }

                    computedSegments.Add(line3);
                }
            }

            // Compute the bounds
            for (int x = 0; x < occupiedCells.GetLength(0); x++)
            {
                for (int y = 0; y < occupiedCells.GetLength(1); y++)
                {
                    for (int z = 0; z < occupiedCells.GetLength(2); z++)
                    {
                        // Move it in an octree and from there create the bounding box
                    }
                }
            }
            return null;
        }

        private static T AddComponent<T>(GameObject go) where T : Component
        {
            return go.AddComponent<T>();
        }

        private static void RemoveObject(Object obj)
        {
            if (Application.isEditor) {
                DestroyImmediate(obj);
            }
            else {
                Destroy(obj);
            }
        }

        private struct Segment
        {
            public Vector3 pointA;
            public Vector3 pointB;
            public Vector3 splitDelta;
            public int splits;
            public int hashCode;
            public float stepSize;
            public float length;

            public Segment(Vector3 pointA, Vector3 pointB)
            {
                this.pointA = pointA;
                this.pointB = pointB;
                splitDelta = Vector3.one;
                splits = 0;
                stepSize = 0;
                length = (pointB - pointA).magnitude;
                hashCode = pointA.GetHashCode() << 16 + pointB.GetHashCode() >> 16;
            }

            public void Split(float distanceSize)
            {
                stepSize = distanceSize;
            }

            public IEnumerator<Vector3> GetEnumerator()
            {
                Vector3 nextPoint = pointA;
                float currentProgress = 0;
                while (currentProgress < length)
                {
                    yield return nextPoint;
                    nextPoint = Vector3.MoveTowards(nextPoint, pointB, stepSize);
                    currentProgress += stepSize;
                }
                yield return nextPoint;
            }

            public override bool Equals(object obj)
            {
                return obj.GetHashCode() == obj.GetHashCode();
            }

            public override int GetHashCode()
            {
                return hashCode;
            }
        }

        #endregion
    }
}