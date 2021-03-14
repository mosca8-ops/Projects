using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.Utility;
using UnityEngine;

namespace TXT.WEAVR
{

    public static class GameObjectExtensions
    {
        private static List<object> s_transferList = new List<object>();

        public static T GetOrCreateComponent<T>(this GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (!c)
            {
                c = go.AddComponent<T>();
            }
            return c;
        }

        public static string GetHierarchyPath(this GameObject go)
        {
            return SceneTools.GetGameObjectPath(go);
        }

        public static string GetHierarchyPath(this Component component)
        {
            return SceneTools.GetGameObjectPath(component.gameObject);
        }

        public static GameObject FindInScene(string path)
        {
            return GameObject.Find(path) ?? SceneTools.GetGameObjectAtScenePath(path);
        }

        public static float GetSimilarityScore(this GameObject go, GameObject other)
        {
            if(go && other) { return GetSimilarityScore(go.transform, other.transform); }
            return 0;
        }

        public static float GetSimilarityScore(this Transform t, Transform other)
        {
            return Similarity.GetSimilarity(t, other);
        }

        public static bool RPC(this GameObject go, string methodName, params object[] parameters)
        {
            return RPC(go, methodName, false, parameters);
        }

        public static bool RPC(this GameObject go, string methodName, bool includeLocal, params object[] parameters)
        {
            // TODO
            //#if WEAVR_NETWORK
            //            var photonView = go.GetComponent<PhotonView>();
            //            if (photonView != null)
            //            {
            //                s_transferList.Clear();
            //                s_transferList.Add(photonView.viewID);
            //                s_transferList.AddRange(parameters);
            //                photonView.RPC(methodName, includeLocal ? PhotonTargets.All : PhotonTargets.Others, s_transferList.ToArray());
            //                return true;
            //            }
            //#endif
            return false;
        }

        public static bool RPC(this Component component, string methodName, bool includeLocal, params object[] parameters)
        {
            return component.gameObject.RPC(methodName, includeLocal, parameters);
        }

        public static bool RPC(this Component component, string methodName, params object[] parameters)
        {
            return component.gameObject.RPC(methodName, parameters);
        }

        public static T OnReceivedRPC<T>(this T component, int photonViewId) where T : MonoBehaviour
        {
            // TODO
            //#if WEAVR_NETWORK
            //            return PhotonView.Find(photonViewId)?.GetComponent<T>();
            //#else
            //            return null;
            //#endif

            return null;
        }

        public static void RepositionWithOffset(this GameObject gameObject, Transform offset, Transform destination)
        {
            var lastOffsetParent = offset.parent;
            var lastParent = gameObject.transform.parent;
            gameObject.transform.SetParent(offset, true);
            offset.SetParent(destination, true);
            offset.SetPositionAndRotation(destination.position, destination.rotation);
            gameObject.transform.SetParent(lastParent, true);
            offset.SetParent(lastOffsetParent, true);
        }

        public static Bounds GetBounds(this GameObject gameObject, float fallbackSize = 0.1f, bool includeChildren = true, bool worldScale = true)
        {
            // Rotate object to identity, to get the correct bounds
            var prevRotation = gameObject.transform.rotation;
            gameObject.transform.rotation = Quaternion.identity;

            // Get the bounds
            Bounds bounds;
            var collider = gameObject.GetComponent<Collider>();
            if (collider == null && includeChildren)
            {
                collider = gameObject.GetComponentInChildren<Collider>(true);
            }
            if (collider != null)
            {
                bool wasEnabled = collider.enabled;
                collider.enabled = true;
                bounds = collider.bounds;
                collider.enabled = wasEnabled;

                if (worldScale)
                {
                    bounds.extents = Vector3.Scale(collider.transform.lossyScale, bounds.extents);
                }
                //bounds.extents = new Vector3(bounds.extents.x / gameObject.transform.lossyScale.x,
                //                        bounds.extents.y / gameObject.transform.lossyScale.y,
                //                        bounds.extents.z / gameObject.transform.lossyScale.z);
            }
            else
            {
                var renderer = gameObject.GetComponent<Renderer>();
                if (renderer == null && includeChildren)
                {
                    renderer = gameObject.GetComponentInChildren<Renderer>(true);
                }
                if (renderer != null)
                {
                    bounds = renderer.bounds;
                    //bounds.extents = new Vector3(bounds.extents.x / gameObject.transform.lossyScale.x,
                    //                    bounds.extents.y / gameObject.transform.lossyScale.y,
                    //                    bounds.extents.z / gameObject.transform.lossyScale.z);
                }
                else
                {
                    bounds = new Bounds(gameObject.transform.position, Vector3.one * fallbackSize);
                }
            }

            // Rotate to its previous rotation
            gameObject.transform.rotation = prevRotation;
            return bounds;
        }

        public static void SmartDestroy(this GameObject go)
        {
            if (!go) { return; }

            if (Application.isPlaying)
            {
                var pooledObject = go.GetComponent<PooledObject>();
                if (pooledObject)
                {
                    pooledObject.Pool.Reclaim(go);
                }
                else
                {
                    UnityEngine.Object.Destroy(go);
                }
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }

    public static class Similarity
    {
        private static float s_nameSimilarity = 0.3f;
        private static float s_componentsSimilarity = 0.5f;
        private static float s_childrenSimilarity = 0.2f;
        private static int s_childrenMaxDepth = 2;

        public static float NameSimilarityWeight {
            get => s_nameSimilarity;
            set
            {
                s_nameSimilarity = value > 0 ? value : 0;
                NormalizeWeights();
            }
        }

        public static float ComponentsSimilarityWeight
        {
            get => s_componentsSimilarity;
            set
            {
                s_componentsSimilarity = value > 0 ? value : 0;
                NormalizeWeights();
            }
        }

        public static float ChildrenSimilarityWeight
        {
            get => s_childrenSimilarity;
            set
            {
                s_childrenSimilarity = value > 0 ? value : 0;
                NormalizeWeights();
            }
        }

        public static int ChildrenSimilarityMaxDepth
        {
            get => s_childrenMaxDepth;
            set
            {
                s_childrenMaxDepth = value > 1 ? value : 1;
            }
        }

        private static void NormalizeWeights()
        {
            float sum = s_nameSimilarity + s_childrenSimilarity + s_componentsSimilarity;
            s_nameSimilarity /= sum;
            s_childrenSimilarity /= sum;
            s_componentsSimilarity /= sum;
        }

        public static float GetSimilarity(Transform a, Transform b)
        {
            return ComputeSimilarity(a, b, NameSimilarityWeight, ComponentsSimilarityWeight, ChildrenSimilarityWeight);
        }

        public static float ComputeSimilarity(Transform a, Transform b,
                                        float nameWeight = 0.4f,
                                        float componentsWeight = 0.4f,
                                        float childrenWeight = 0.2f,
                                        int childrenMaxDepth = 2)
        {
            float nameSimilarity = nameWeight > 0 ? ComputeNameSimilarity(a, b) * nameWeight : 0;
            float componentsSimilarity = componentsWeight > 0 ? ComputeComponentsSimilarity(a, b) * componentsWeight : 0;
            float childrenSimilarity = childrenWeight > 0 ? ComputeChildrenSimilarity(a, b, childrenMaxDepth, nameWeight, componentsWeight) * childrenWeight : 0;

            return nameSimilarity + componentsSimilarity + childrenSimilarity;
        }

        public static float ComputeNameSimilarity(Transform a, Transform b)
        {
            //int minLength = a.name.Length < b.name.Length ? a.name.Length : b.name.Length;
            //int distance = a.name.SimilarityDistanceTo(b.name);
            //Debug.Log($"Name: [{a.name}] <-> [{b.name}]: Distance = {distance} | Score = {score}");
            //float score = minLength > 0 ? Mathf.Clamp01(1f - (a.name.SimilarityDistanceTo(b.name) / (float)minLength)) : 0;
            //return score;
            int minLength = a.name.Length < b.name.Length ? a.name.Length : b.name.Length;
            return minLength > 0 ? Mathf.Clamp01(1f - (a.name.SimilarityDistanceTo(b.name) / (float)minLength)) : 0;
        }

        public static float ComputeComponentsSimilarity(Transform a, Transform b)
        {
            var cA = a.GetComponents<Component>().Select(c => c.GetType());
            var cB = b.GetComponents<Component>().Select(c => c.GetType());
            int unionCount = cA.Union(cB).Count();
            return unionCount > 0 ? cA.Intersect(cB).Count() / (float)unionCount : 1;
        }

        public static float ComputeChildrenSimilarity(Transform a, Transform b, int maxDepth, float nameWeight = 0.5f, float componentsWeight = 0.3f, float childrenWeight = 0.2f)
        {
            if(maxDepth <= 0) { return 0; }

            float totalWeight = nameWeight + componentsWeight + childrenWeight;
            nameWeight /= totalWeight;
            componentsWeight /= totalWeight;
            childrenWeight /= totalWeight;
            totalWeight /= 3f;
            var cA = GetChildrenInLevels(a, 1);
            var cB = GetChildrenInLevels(b, 1);

            var min = cA.Count() > cB.Count() ? cB.ToList() : cA.ToList();
            var max = cA.Count() < cB.Count() ? cB : cA;

            int minCount = min.Count();
            int maxCount = max.Count();

            var remainingChildren = new List<Transform>(max);
            for (int i = 0; i < min.Count; i++)
            {
                var child = min[i];
                var candidate = remainingChildren.Select(c => (c, (ComputeNameSimilarity(c, child) * nameWeight 
                                                                 + ComputeComponentsSimilarity(c, child) * componentsWeight 
                                                                 + ComputeChildrenSimilarity(c, child, maxDepth - 1, nameWeight, componentsWeight, childrenWeight) * childrenWeight)))
                                                 .Where(c => c.Item2 > totalWeight).OrderByDescending(c => c.Item2)
                                                 .FirstOrDefault().c;
                if (candidate) { 
                    remainingChildren.Remove(candidate);
                    min.RemoveAt(i--);
                }
            }
            //foreach (var child in min.ToArray())
            //{
            //    var candidate = remainingChildren.FirstOrDefault(c => (ComputeNameSimilarity(c, child) * nameWeight + ComputeComponentsSimilarity(c, child) * componentsWeight) > totalWeight);
            //    if (candidate) { remainingChildren.Remove(candidate); }
            //}

            return maxCount > 0 ? (1 - (remainingChildren.Count / (float)maxCount)) * (1 - ((maxCount - minCount) / (float)maxCount)) : 1;
        }

        public static IEnumerable<Transform> GetChildrenInLevels(Transform t, int levels)
        {
            List<Transform> children = new List<Transform>();
            for (int i = 0; i < t.childCount; i++)
            {
                children.Add(t.GetChild(i));
            }
            if (levels > 1)
            {
                var prevChildren = children.ToArray();
                foreach (var child in prevChildren)
                {
                    children.AddRange(GetChildrenInLevels(child, levels - 1));
                }
            }
            return children;
        }
    }
}
