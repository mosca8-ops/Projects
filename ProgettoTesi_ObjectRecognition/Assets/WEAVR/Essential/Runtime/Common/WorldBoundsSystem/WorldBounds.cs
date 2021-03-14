using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.EditorBridge;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.Common
{
    [ExecuteInEditMode]
    [RequireLayers(ACTIVE_SPACE_LAYER, PASSIVE_SPACE_LAYER)]
    [AddComponentMenu("")]
    public class WorldBounds : MonoBehaviour
    {
        public const string ACTIVE_SPACE_LAYER = "ActiveSpaceOccupier";
        public const string PASSIVE_SPACE_LAYER = "PassiveSpaceOccupier";

        [SerializeField]
        [ShowAsReadOnly]
        private int m_bounds;

        [SerializeField]
        private float m_minVoxelSize = 0.1f;

        [SerializeField]
        private Transform[] m_freeSpaceSamples;

        [SerializeField]
        private bool m_useVolumeLimits = false;
        [SerializeField]
        [HiddenBy(nameof(m_useVolumeLimits), hiddenWhenTrue: true)]
        private BoxCollider m_computationVolume;
        [SerializeField]
        [HiddenBy(nameof(m_useVolumeLimits), hiddenWhenTrue: false)]
        private Vector3 m_volumeLimits = new Vector3(6, 3, 6);
        
        private List<OccupiedSpace> m_spaces = new List<OccupiedSpace>();
        public List<OccupiedSpace> OccupiedSpaces { get { return m_spaces; } }

        public static int GetOccupancySpaceActiveLayer() {
            return LayerMask.NameToLayer(ACTIVE_SPACE_LAYER);
        }

        public static int GetOccupancySpacePassiveLayer() {
            return LayerMask.NameToLayer(PASSIVE_SPACE_LAYER);
        }
        
        /***************************************************************************************************
         *                          TODO to fix ghost collision bugs
         * Create a shadow object with colliders for all renderers:
         *      - Assign PassiveBounds to static game objects (maybe by using voxelisation)
         *      - Assign ActiveBounds to non-static game objects and on each frame check sync transforms of
         *      shadow object with ActiveBounds game object
         * 
         * By separating the actual game object from shadow ones, the rigid body and interactive objects 
         * won't receive events from collision between shadow colliders
         * *************************************************************************************************/

        private Coroutine m_buildCoroutine;
        private bool m_isRunning;
        private float m_operationProgress;
        private string m_operationText;
        private float m_progressQuant = 0.01f;
        [SerializeField]
        [HideInInspector]
        private bool m_visibleInHierarchy = false;
        [SerializeField]
        [HideInInspector]
        private bool m_visibleInScene = true;

        public float OperationProgress { get { return m_operationProgress; } }
        public string OperationText { get { return m_operationText; } }
        public bool IsRunning { get { return m_isRunning; } }

        public bool VisibileInHierarchy {
            get { return m_visibleInHierarchy; }
            set {
                if(m_visibleInHierarchy != value) {
                    m_visibleInHierarchy = value;
                    if (value) {
                        foreach(var space in GetAllComponents<OccupiedSpace>()) {
                            space.gameObject.hideFlags = HideFlags.None;
                        }
                    }
                    else {
                        foreach (var space in GetAllComponents<OccupiedSpace>()) {
                            space.gameObject.hideFlags = HideFlags.HideInHierarchy;
                        }
                    }
                    EditorApplication.DirtyHierarchyWindowSorting();
                }
            }
        }

        public bool VisibleInScene { get { return m_visibleInScene; } set { m_visibleInScene = value; } }

        public void BuildBounds() {
            m_buildCoroutine = StartCoroutine(BuildBoundsCoroutine());
        }

        public void CancelBuild() {
            if(m_isRunning && m_buildCoroutine != null) {
                StopCoroutine(m_buildCoroutine);
                m_buildCoroutine = null;
            }
            m_isRunning = false;
            m_operationProgress = 0;
        }

        public IEnumerator RefreshBounds() {
            yield return new WaitForEndOfFrame();
            if (m_spaces.Count != m_bounds) {
                m_spaces = GetAllComponents<OccupiedSpace>();
                m_bounds = m_spaces.Count;
            }
        }

        protected Bounds? GetBounds(Transform point) {
            Collider collider = point.GetComponent<Collider>() ?? point.GetComponentInChildren<Collider>();
            if (collider != null) {
                return collider.bounds;
            }
            else {
                Renderer renderer = point.GetComponent<Renderer>() ?? point.GetComponentInChildren<Renderer>();
                if (renderer != null) {
                    return renderer.bounds;
                }
            }

            return null;
        }

        public IEnumerator BuildBoundsCoroutine(Action<int, int> buildProgressCallback = null, Action<int, int> optimizeProgressCallback = null) {
            m_operationProgress = 0;
            m_isRunning = true;

            List<MeshRenderer> renderers = GetAllComponents<MeshRenderer>();

            if(m_useVolumeLimits || m_computationVolume)
            {
                BoxCollider boxCollider = m_useVolumeLimits ? gameObject.AddComponent<BoxCollider>() : m_computationVolume;
                if (m_useVolumeLimits)
                {
                    boxCollider.size = m_volumeLimits;
                }

                var bounds = boxCollider.bounds;
                renderers = renderers.Where(r => bounds.Contains(r.transform.position)).ToList();

                if (m_useVolumeLimits)
                {
                    DestroyImmediate(boxCollider);
                }
            }

            Action<int, int> buildProgress = (i, c) => m_bounds = i;
            if(buildProgressCallback != null) {
                buildProgressCallback = (i, c) => {
                    m_bounds = i;
                    buildProgressCallback(i, c);
                };
            }

            yield return Execute(renderers, 
                                r => OccupiedSpace.Create(transform, r.gameObject).gameObject.hideFlags = VisibileInHierarchy ? HideFlags.None : HideFlags.HideInHierarchy, 
                                "Building...",  buildProgress);

            yield return Execute(GetFirstComponents<OccupiedSpace>(),
                                r => OccupiedSpace.OptimizeSpace(r, m_freeSpaceSamples),
                                "Optimizing...", optimizeProgressCallback);

            m_spaces.Clear();
            m_spaces.AddRange(GetAllComponents<OccupiedSpace>());

            m_bounds = m_spaces.Count;

            if (!VisibileInHierarchy) {
                EditorApplication.DirtyHierarchyWindowSorting();
            }

            m_operationProgress = 0;
            m_isRunning = false;
        }

        public IEnumerator ClearBounds() {
            m_operationProgress = 0;
            m_isRunning = true;
            yield return Execute(GetAllComponents<OccupiedSpace>(), s => OccupiedSpace.DestroySpace(s.gameObject), "Clearing...", (i, c) => m_bounds = c - i);
            m_spaces.Clear();

            m_operationProgress = 0;
            m_isRunning = false;
        }

        private static List<T> GetAllComponents<T>() where T : Component {
            List<T> elements = new List<T>();
            if (SceneManager.GetActiveScene().isLoaded) {
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) {
                    elements.AddRange(root.GetComponentsInChildren<T>(true));
                }
            }
            return elements;
        }

        private static List<T> GetFirstComponents<T>() where T : Component {
            List<T> elements = new List<T>();
            if (SceneManager.GetActiveScene().isLoaded) {
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) {
                    GetFirstComponents(root.transform, elements);
                }
            }
            return elements;
        }

        private static void GetFirstComponents<T>(Transform current, List<T> elements) where T : Component {
            var component = current.GetComponent<T>();
            if(component != null) {
                elements.Add(component);
                return;
            }
            foreach(Transform child in current) {
                GetFirstComponents(child, elements);
            }
        }

        private IEnumerator Execute<T>(List<T> elements, Action<T> operation, string operationName, Action<int, int> eachEndBatchAction) {
            int batchSize = (int)(m_progressQuant * elements.Count) + 1;
            int batchCount = 0;

            yield return new WaitForEndOfFrame();
            int i = 0;
            while (i < elements.Count) {
                int nextCount = Mathf.Min(batchSize * (batchCount + 1), elements.Count);
                for (i = batchCount * batchSize; i < nextCount && i < elements.Count; i++) {
                    operation(elements[i]);
                }
                m_operationProgress = (i + 1) / (float)elements.Count;
                m_operationText = string.Format("{0} {1:0}%", operationName, m_operationProgress * 100f);
                if(eachEndBatchAction != null) {
                    eachEndBatchAction(i, elements.Count);
                }
                batchCount++;
                yield return new WaitForEndOfFrame();
            }
            yield return null;
        }

        /// <summary>
        /// Updates the layers physics collision matrix
        /// </summary>
        private static void UpdatePhysicsLayerMatrix() {
            int activeLayer = GetOccupancySpaceActiveLayer();
            int passiveLayer = GetOccupancySpacePassiveLayer();
            if (activeLayer <= 0 || passiveLayer <= 0) {
                throw new System.InvalidOperationException("Needed layers were not created");
            }
            int totalLayers = Mathf.Max(activeLayer, passiveLayer) + 1;
            for (int i = 0; i < totalLayers; i++) {
                Physics.IgnoreLayerCollision(passiveLayer, i, true);
                Physics.IgnoreLayerCollision(activeLayer, i, i != passiveLayer && i != activeLayer);
            }
        }

        public static void RemoveFromRaycasts(params int[] layers) {
            foreach(var raycaster in GetAllComponents<PhysicsRaycaster>()) {
                if(raycaster == null) continue;
                foreach(var layer in layers) {
                    raycaster.eventMask &= ~(1 << layer);
                }
            }

            foreach (var raycaster in GetAllComponents<Physics2DRaycaster>()) {
                if (raycaster == null) continue;
                foreach (var layer in layers) {
                    raycaster.eventMask &= ~(1 << layer);
                }
            }
        }

        public static void RemoveBoundsLayersFromRaycasts() {
            RemoveFromRaycasts(GetOccupancySpaceActiveLayer(), GetOccupancySpacePassiveLayer());
        }

        private void OnEnable() {
            if (Application.isEditor) {
                if (m_freeSpaceSamples == null || m_freeSpaceSamples.Length == 0) {
                    m_freeSpaceSamples = new Transform[] { transform };
                }

                UpdatePhysicsLayerMatrix();
                RemoveBoundsLayersFromRaycasts();
            }
        }

    }
}
