using System;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using TXT.WEAVR.License;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.Editor
{
    [Serializable]
    public class CameraManagerWindow : EditorWindow
    {
        private const float c_colorAlpha = 1f;
        private const int k_maxColors = 12;

        [MenuItem("WEAVR/Utilities/Camera Manager", priority = 5)]
        public static void ShowWindow()
        {
            s_instance = GetWindow<CameraManagerWindow>();
            s_instance.minSize = new Vector2(350, 300);
            s_instance.titleContent = new GUIContent("Camera Manager", WeavrStyles.Icons["camera"]);
            s_instance.Show();

        }

        #region [  STYLES  ]
        private class Styles : BaseStyles
        {
            public GUIStyle liveViewStyle;
            public GUIStyle titleStyle;
            public GUIStyle tableHeaderStyle;
            public GUIStyle rowButtonStyle;
            public GUIStyle previewCamStyle;
            public GUIStyle sceneLabelStyle;

            protected override void InitializeStyles(bool isProSkin)
            {
                liveViewStyle = WeavrStyles.EditorSkin2.FindStyle("camManager_LiveView");
                titleStyle = WeavrStyles.EditorSkin2.FindStyle("camManager_Title");
                tableHeaderStyle = WeavrStyles.EditorSkin2.FindStyle("camManager_CamerasTableHeader") ?? new GUIStyle("Box");
                rowButtonStyle = WeavrStyles.EditorSkin2.FindStyle("camManager_RowButton");
                previewCamStyle = WeavrStyles.EditorSkin2.FindStyle("camManager_PreviewCamera");
                sceneLabelStyle = WeavrStyles.EditorSkin2.FindStyle("camManager_SceneLabel");
            }
        }
        #endregion

        #region [  CONTENTS  ]
        private class Contents
        {
            public readonly GUIContent viewButtonContent;
            public readonly GUIContent recNormalButtonContent;
            public readonly GUIContent recActiveButtonContent;
            public readonly GUIContent removeButtonContent;
            public readonly GUIContent createButtonContent;
            public readonly GUIContent liveViewActiveContent;
            public readonly GUIContent liveViewNormalContent;
            public readonly GUIContent refreshListContent;
            public readonly GUIContent resetButtonContent;

            public Contents()
            {
                liveViewNormalContent = new GUIContent("Live View", WeavrStyles.Icons.Rec);
                liveViewActiveContent = new GUIContent("Live View", WeavrStyles.Icons.RecActive);
                createButtonContent = new GUIContent(" Create", WeavrStyles.Icons.Plus, "Creates new Virtual Camera");
                refreshListContent = new GUIContent(" Refresh", WeavrStyles.Icons.Refresh, "Refresh Virtual Cameras List");
                resetButtonContent = new GUIContent(" Reset", WeavrStyles.Icons.Reset, "Resets camera to defaults");

                viewButtonContent = new GUIContent(WeavrStyles.Icons.Visibility, "View");
                recNormalButtonContent = new GUIContent(WeavrStyles.Icons.Rec, "Save");
                recActiveButtonContent = new GUIContent(WeavrStyles.Icons.RecActive, "Save");
                removeButtonContent = new GUIContent(WeavrStyles.Icons.Delete, "Remove");
            }
        }
        #endregion

        private static CameraManagerWindow s_instance;

        [SerializeField]
        private List<VirtualCamera> m_cameras;
        [SerializeField]
        private Dictionary<VirtualCamera, Color> m_colors;

        private Dictionary<Camera, bool> m_allCameras = new Dictionary<Camera, bool>();
        private Camera m_sampleCamera;
        private Camera m_shadowCamera;
        private Transform m_camerasContainer;
        private Rect m_scenePreviewRect;
        private EditorWindow m_gameWindow;
        private bool m_liveView;

        private bool m_showPoints = false;
        private bool m_showLabels = true;
        private bool m_showFrustums = true;

        private Styles m_styles = new Styles();
        private Contents m_contents;

        private Camera SampleCamera {
            get { return m_sampleCamera; }
            set {
                if (m_sampleCamera != value)
                {
                    m_sampleCamera = value;
                    ResetShadowCamera();
                }
            }
        }

        private Vector2 m_scrollPosition;

        public void OnEnable()
        {
            if (!WeavrLE.IsValid())
            {
                DestroyImmediate(this);
                return;
            }

            if (m_styles == null)
            {
                m_styles = new Styles();
            }

            if (m_contents == null)
            {
                m_contents = new Contents();
            }

            //if (s_defaultColors == null)
            //{
            //}
            s_defaultColors = GenerateColors(0.4f);

            if (m_colors == null)
            {
                m_colors = new Dictionary<VirtualCamera, Color>();
            }

            if (m_cameras == null)
            {
                m_cameras = new List<VirtualCamera>();
            }

            if (!m_camerasContainer)
            {
                m_camerasContainer = GameObject.Find("CamerasPositions")?.transform;
            }

            if (m_cameras.Count == 0)
            {
                GetAllCameras(m_camerasContainer);
            }
            else
            {
                UpdateColors();
            }

            if (SampleCamera == null)
            {
                var allWeavrCameras = SceneTools.GetComponentsInScene<WeavrCamera>();
                SampleCamera = allWeavrCameras.FirstOrDefault(c => c.Type == WeavrCamera.WeavrCameraType.Free)?.GetComponent<Camera>()
                            ?? allWeavrCameras.FirstOrDefault()?.GetComponent<Camera>()
                            ?? Camera.allCameras[0];
            }

            m_liveView = false;

            if (s_instance == null)
            {
                s_instance = this;
            }
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void SetLiveViewEnabled(bool enable)
        {
            if (enable)
            {
                if (m_shadowCamera == null)
                {
                    var shadowGO = Instantiate(SampleCamera.gameObject);
                    shadowGO.SetActive(true);
                    m_shadowCamera = shadowGO.GetComponent<Camera>();
                    shadowGO.hideFlags = HideFlags.HideAndDontSave;
                    m_shadowCamera.ResetAspect();
                }

                m_scenePreviewRect = new Rect(20, 40, 160, 160);
                if (m_shadowCamera.aspect != 0)
                {
                    m_scenePreviewRect.width = m_scenePreviewRect.height * m_shadowCamera.aspect;
                }
                if (SceneView.lastActiveSceneView != null)
                {
                    m_scenePreviewRect.y = SceneView.lastActiveSceneView.position.height - m_scenePreviewRect.height - m_scenePreviewRect.y;
                    m_scenePreviewRect.x = SceneView.lastActiveSceneView.position.width - m_scenePreviewRect.width - m_scenePreviewRect.x;
                }
                m_allCameras.Clear();
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                    {
                        m_allCameras[camera] = camera.enabled;
                    }
                }
                m_shadowCamera.enabled = true;
                m_gameWindow = GetMainGameView();
                if (m_gameWindow) { m_gameWindow.Focus(); }
                SetOnSceneGUIEnabled(true);
            }
            else
            {
                if (m_shadowCamera)
                {
                    DestroyImmediate(m_shadowCamera.gameObject);
                }
                foreach (var pair in m_allCameras)
                {
                    pair.Key.enabled = pair.Value;
                }
                SetOnSceneGUIEnabled(false);
            }
        }

        //private Rect GetCameraPreviewRect() {
        //    Vector2 previewSize = c.targetTexture ? new Vector2(c.targetTexture.width, c.targetTexture.height) : GameView.GetMainGameViewTargetSize();

        //    if (previewSize.x < 0f) {
        //        // Fallback to Scene View of not a valid game view size
        //        previewSize.x = sceneView.position.width;
        //        previewSize.y = sceneView.position.height;
        //    }

        //    // Apply normalizedviewport rect of camera
        //    Rect normalizedViewPortRect = c.rect;
        //    previewSize.x *= Mathf.Max(normalizedViewPortRect.width, 0f);
        //    previewSize.y *= Mathf.Max(normalizedViewPortRect.height, 0f);

        //    // Prevent using invalid previewSize
        //    if (previewSize.x < 1f || previewSize.y < 1f)
        //        return;

        //    float aspect = previewSize.x / previewSize.y;

        //    // Scale down (fit to scene view)
        //    previewSize.y = kPreviewNormalizedSize * sceneView.position.height;
        //    previewSize.x = previewSize.y * aspect;
        //    if (previewSize.y > sceneView.position.height * 0.5f) {
        //        previewSize.y = sceneView.position.height * 0.5f;
        //        previewSize.x = previewSize.y * aspect;
        //    }
        //    if (previewSize.x > sceneView.position.width * 0.5f) {
        //        previewSize.x = sceneView.position.width * 0.5f;
        //        previewSize.y = previewSize.x / aspect;
        //    }

        //    // Get and reserve rect
        //    Rect cameraRect = GUILayoutUtility.GetRect(previewSize.x, previewSize.y);
        //}

        private void SetOnSceneGUIEnabled(bool enable)
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            if (enable)
            {
                SceneView.duringSceneGui += OnSceneGUI;
            }
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.Repaint();
            }
        }

        private void OnDisable()
        {
            if (m_shadowCamera != null)
            {
                DestroyImmediate(m_shadowCamera.gameObject);
            }
            SetOnSceneGUIEnabled(false);
        }

        void OnDestroy()
        {
            SetOnSceneGUIEnabled(false);
        }

        public static EditorWindow GetMainGameView()
        {
            System.Reflection.Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
            Type type = assembly.GetType("UnityEditor.GameView");
            EditorWindow gameview = EditorWindow.GetWindow(type);
            return gameview;
        }

        void OnSceneGUI(SceneView sceneView)
        {
            if (!m_liveView) { return; }

            if (m_showLabels)
            {
                foreach (var camera in m_cameras)
                {
                    if (camera.gameObject.activeInHierarchy)
                    {
                        m_styles.sceneLabelStyle.normal.textColor = m_colors[camera];
                        Handles.Label(camera.transform.position, camera.gameObject.name, m_styles.sceneLabelStyle);
                    }
                }
            }

            SyncCameraWithScene();
            if (m_gameWindow)
            {
                m_gameWindow.Repaint();
            }
            //Handles.BeginGUI();
            //DrawFrame(m_scenePreviewRect, "Preview by WEAVR Camera Manager", 4);
            //DrawFrame(m_scenePreviewRect, Color.black, 1);
            ////EditorGUI.DrawRect(_scenePreviewRect, Color.black);
            ////CameraPreviewWindow.ShowPreview(_shadowCamera, _scenePreviewRect);
            //Handles.EndGUI();
            //var cameraRect = m_scenePreviewRect;
            //cameraRect.x += 2;
            //cameraRect.y -= 4;
            //cameraRect.height += 4;
            ////cameraRect.height -= 4;
            //Handles.DrawCamera(cameraRect, m_shadowCamera, DrawCameraMode.Normal);
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Active)]
        static void DrawGizmos(VirtualCamera virtualCamera, GizmoType gizmoType)
        {
            if (s_instance == null || !s_instance.m_liveView || !virtualCamera.gameObject.activeInHierarchy) { return; }
            Color cameraColor = Color.white;
            if (!s_instance.m_colors.TryGetValue(virtualCamera, out cameraColor))
            {
                return;
            }
            if (s_instance.m_showPoints)
            {
                Gizmos.color = cameraColor;
                Gizmos.DrawSphere(virtualCamera.transform.position, 0.02f);
            }
            if (s_instance.m_showFrustums)
            {
                cameraColor.a = 0.25f;
                Gizmos.color = cameraColor;
                var oldMatrix = Gizmos.matrix;
                Gizmos.matrix *= Matrix4x4.TRS(virtualCamera.transform.position, virtualCamera.transform.rotation, virtualCamera.transform.lossyScale);
                Gizmos.DrawFrustum(Vector3.zero,
                                   virtualCamera.fieldOfView,
                                   virtualCamera.farClipPlane,
                                   virtualCamera.nearClipPlane,
                                   s_instance.m_shadowCamera.aspect);
                Gizmos.matrix = oldMatrix;
            }
        }


        private void DrawFrame(Rect rect, Color color, float borderWidth)
        {
            rect.width += borderWidth * 2;
            rect.height += borderWidth * 2;
            rect.y -= borderWidth;
            rect.x -= borderWidth;
            EditorGUI.DrawRect(rect, color);
        }

        private void DrawFrame(Rect rect, string title, float borderWidth)
        {
            rect.width += borderWidth * 2;
            rect.height += borderWidth * 2 + EditorGUIUtility.singleLineHeight;
            rect.y -= borderWidth + EditorGUIUtility.singleLineHeight;
            rect.x -= borderWidth;
            GUI.Box(rect, "Preview by WEAVR Camera Manager", m_styles.previewCamStyle);
        }

        private void OnGUI()
        {
            m_styles?.Refresh();
            //_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Camera", m_styles.titleStyle);
            SampleCamera = EditorGUILayout.ObjectField(SampleCamera, typeof(Camera), true) as Camera;
            GUI.enabled = SampleCamera != null;
            EditorGUILayout.LabelField("Container", m_styles.titleStyle);
            EditorGUILayout.BeginHorizontal();
            var camerasContainer = EditorGUILayout.ObjectField(m_camerasContainer, typeof(Transform), true) as Transform;
            if (camerasContainer != m_camerasContainer)
            {
                m_camerasContainer = camerasContainer;
                RefreshCameras(camerasContainer);
            }
            if (m_camerasContainer == null && GUILayout.Button("Show All"))
            {
                GetAllCameras(null);
            }
            EditorGUILayout.EndHorizontal();
            DrawVirtualCameras();
            EditorGUILayout.EndVertical();
            // Commands
            EditorGUILayout.BeginVertical();
            var liveView = GUILayout.Toggle(m_liveView, m_liveView ? m_contents.liveViewActiveContent : m_contents.liveViewNormalContent, m_styles.liveViewStyle, GUILayout.ExpandWidth(true));
            if (liveView != m_liveView)
            {
                m_liveView = liveView;
                SetLiveViewEnabled(liveView);
            }
            bool wasEnabled = GUI.enabled;
            GUI.enabled = liveView;
            if (GUILayout.Button(m_contents.createButtonContent))
            {
                CreateNewCamera();
            }
            GUI.enabled = wasEnabled;
            if (GUILayout.Button(m_contents.refreshListContent))
            {
                RefreshCameras();
            }
            if (GUILayout.Button(m_contents.resetButtonContent))
            {
                ResetShadowCamera();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Visualize", m_styles.titleStyle);
            EditorGUI.BeginChangeCheck();
            m_showPoints = GUILayout.Toggle(m_showPoints, "Points", EditorStyles.miniButton);
            m_showLabels = GUILayout.Toggle(m_showLabels, "Labels", EditorStyles.miniButton);
            m_showFrustums = GUILayout.Toggle(m_showFrustums, "Frustums", EditorStyles.miniButton);
            if (EditorGUI.EndChangeCheck() && SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.Repaint();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            //EditorGUILayout.EndScrollView();

            if (m_liveView && m_shadowCamera != null)
            {

                DrawCameraControls();

            }
        }

        private void DrawCameraControls()
        {
            EditorGUILayout.BeginVertical(m_styles.tableHeaderStyle);
            EditorGUILayout.LabelField("Controls", m_styles.titleStyle);
            bool changeOccurred = false;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            m_shadowCamera.orthographic = EditorGUILayout.Toggle("Is Orthographic", m_shadowCamera.orthographic);
            if (GUILayout.Button(m_contents.resetButtonContent, GUILayout.Width(80)))
            {
                changeOccurred = true;
                m_shadowCamera.orthographic = m_sampleCamera.orthographic;
            }
            EditorGUILayout.EndHorizontal();

            if (m_shadowCamera.orthographic)
            {
                EditorGUILayout.BeginHorizontal();
                m_shadowCamera.orthographicSize = EditorGUILayout.FloatField("Size", m_shadowCamera.orthographicSize);
                if (GUILayout.Button(m_contents.resetButtonContent))
                {
                    changeOccurred = true;
                    m_shadowCamera.orthographicSize = m_sampleCamera.orthographicSize;
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                m_shadowCamera.fieldOfView = (int)EditorGUILayout.Slider("Field Of View", m_shadowCamera.fieldOfView, 1, 179);
                if (GUILayout.Button(m_contents.resetButtonContent, GUILayout.Width(80)))
                {
                    changeOccurred = true;
                    m_shadowCamera.fieldOfView = m_sampleCamera.fieldOfView;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            float lastLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 80;
            EditorGUILayout.LabelField("Clipping Plane");
            EditorGUIUtility.labelWidth = 50;
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            m_shadowCamera.nearClipPlane = EditorGUILayout.FloatField("Near", m_shadowCamera.nearClipPlane);
            if (GUILayout.Button(m_contents.resetButtonContent, GUILayout.Width(80)))
            {
                changeOccurred = true;
                m_shadowCamera.nearClipPlane = m_sampleCamera.nearClipPlane;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            m_shadowCamera.farClipPlane = EditorGUILayout.FloatField("Far", m_shadowCamera.farClipPlane);
            if (GUILayout.Button(m_contents.resetButtonContent, GUILayout.Width(80)))
            {
                changeOccurred = true;
                m_shadowCamera.farClipPlane = m_sampleCamera.farClipPlane;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUIUtility.labelWidth = lastLabelWidth;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            if ((EditorGUI.EndChangeCheck() || changeOccurred) && SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.Repaint();
            }
        }

        private void DrawVirtualCameras()
        {
            EditorGUILayout.LabelField("Virtual Cameras", m_styles.titleStyle);

            if (m_cameras.Count == 0)
            {
                if (m_camerasContainer == null)
                {
                    //EditorGUILayout.HelpBox("No virtual cameras detected in the scene", MessageType.Info);
                    RefreshCameras();
                }
                else
                {
                    //EditorGUILayout.HelpBox("No virtual cameras detected under " + _camerasContainer.gameObject.name, MessageType.Info);
                    RefreshCameras(m_camerasContainer);
                }
                //return;
            }

            float editorLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 60;

            EditorGUILayout.BeginHorizontal(m_styles.tableHeaderStyle);

            var allActive = GetMixedValue();
            EditorGUI.showMixedValue = !allActive.HasValue;
            bool oldAllActive = allActive ?? false;
            bool newAllActive = EditorGUILayout.Toggle(oldAllActive, GUILayout.Width(16));
            if (newAllActive != oldAllActive)
            {
                newAllActive |= !allActive.HasValue;
                for (int i = 0; i < m_cameras.Count; i++)
                {
                    if (m_cameras[i] == null)
                    {
                        m_cameras.RemoveAt(i--);
                        continue;
                    }
                    m_cameras[i].gameObject.SetActive(newAllActive);
                }
            }

            EditorGUI.showMixedValue = false;
            EditorGUILayout.LabelField("Sample Camera");
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Actions", GUILayout.Width(50));

            EditorGUILayout.EndHorizontal();

            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
            for (int i = 0; i < m_cameras.Count; i++)
            {
                if (m_cameras[i] == null)
                {
                    m_cameras.RemoveAt(i--);
                    continue;
                }
                var camera = m_cameras[i];
                EditorGUILayout.BeginHorizontal();
                bool isActive = EditorGUILayout.Toggle(camera.gameObject.activeInHierarchy, GUILayout.Width(16));
                if (isActive != camera.gameObject.activeInHierarchy) { camera.gameObject.SetActive(isActive); }
                var colorRect = EditorGUILayout.GetControlRect(GUILayout.Width(4));
                EditorGUI.DrawRect(colorRect, m_colors[camera]);
                EditorGUILayout.ObjectField(camera, typeof(VirtualCamera), true);
                bool wasEnabled = GUI.enabled;
                GUI.enabled = m_liveView && isActive;
                if (GUILayout.Button(m_contents.viewButtonContent, m_styles.rowButtonStyle))
                {
                    camera.ApplyTo(m_shadowCamera);
                    if (SceneView.lastActiveSceneView != null)
                    {
                        AlignSceneCameraWith(SceneView.lastActiveSceneView, camera);
                    }
                }
                if (GUILayout.Button(m_liveView ? m_contents.recActiveButtonContent : m_contents.recNormalButtonContent, m_styles.rowButtonStyle))
                {
                    camera.UpdateFrom(m_shadowCamera, true);
                }
                GUI.enabled = wasEnabled;
                if (GUILayout.Button(m_contents.removeButtonContent, m_styles.rowButtonStyle))
                {
                    Undo.RecordObject(this, "Removed virtual camera");
                    m_cameras.RemoveAt(i--);
                    Undo.DestroyObjectImmediate(camera.gameObject);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUIUtility.labelWidth = editorLabelWidth;
        }

        private static void AlignSceneCameraWith(SceneView sceneView, VirtualCamera camera)
        {
            var offset = sceneView.pivot - sceneView.camera.transform.position;
            var cameraDistance = offset.magnitude;

            sceneView.pivot = camera.transform.position + camera.transform.forward * cameraDistance;
            sceneView.rotation = camera.transform.rotation;

            //sceneView.camera.transform.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);
        }

        private void SyncCameraWithScene()
        {
            if (SceneView.lastActiveSceneView == null || m_shadowCamera == null)
            {
                return;
            }
            var sceneCamera = SceneView.lastActiveSceneView.camera;
            m_shadowCamera.transform.SetPositionAndRotation(sceneCamera.transform.position, sceneCamera.transform.rotation);
        }

        private void ResetShadowCamera()
        {
            if (m_shadowCamera != null)
            {
                var undoCam = m_shadowCamera;
                Undo.RecordObject(this, "Reset shadow cam");
                m_shadowCamera = null;
                Undo.DestroyObjectImmediate(undoCam.gameObject);
                var shadowGO = Instantiate(m_sampleCamera.gameObject);
                m_shadowCamera = shadowGO.GetComponent<Camera>();
                shadowGO.hideFlags = HideFlags.HideAndDontSave;

                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.Repaint();
                }
            }
        }

        private bool? GetMixedValue()
        {
            bool? active = null;
            for (int i = 0; i < m_cameras.Count; i++)
            {
                if (m_cameras[i] == null)
                {
                    m_cameras.RemoveAt(i--);
                    continue;
                }
                var camera = m_cameras[i];
                if (!active.HasValue) { active = camera.gameObject.activeInHierarchy; }
                else if (active.Value != camera.gameObject.activeInHierarchy)
                {
                    return null;
                }
            }
            return active;
        }

        private void GetAllCameras(Transform container)
        {
            RefreshCameras(container);
            if (container == null)
            {
                foreach (var cam in m_cameras)
                {
                    if (cam.transform.parent != null)
                    {
                        m_camerasContainer = cam.transform.parent;
                        break;
                    }
                }
            }
        }

        private void CreateNewCamera()
        {
            GameObject newCamGO = new GameObject("CameraPose_" + (m_cameras.Count + 1));
            Undo.RegisterCreatedObjectUndo(newCamGO, "Virtual Camera");
            var virtualCam = Undo.AddComponent<VirtualCamera>(newCamGO);
            virtualCam.UpdateFrom(m_shadowCamera, true);
            newCamGO.transform.SetParent(m_camerasContainer, true);
            Undo.RecordObject(this, "Added virtual camera");
            m_colors[virtualCam] = GetColor(m_cameras.Count, c_colorAlpha);
            m_cameras.Add(virtualCam);
        }

        private void RefreshCameras()
        {
            m_cameras.Clear();
            m_colors.Clear();
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                m_cameras.AddRange(root.GetComponentsInChildren<VirtualCamera>(true));
            }

            UpdateColors();
        }

        private void RefreshCameras(Transform root)
        {
            if (root == null)
            {
                var cameraPositions = GameObject.Find("CameraPositions");
                if (cameraPositions != null) { root = cameraPositions.transform; }
            }
            if (root != null)
            {
                m_cameras.Clear();
                m_colors.Clear();
                m_cameras.AddRange(root.GetComponentsInChildren<VirtualCamera>(true));

                UpdateColors();
            }
            else
            {
                RefreshCameras();
            }
        }

        private void UpdateColors()
        {
            int index = 0;
            if (m_cameras != null && m_cameras.Count > 0)
            {
                foreach (var cam in m_cameras)
                {
                    if (cam != null)
                    {
                        m_colors[cam] = GetColor(index++, c_colorAlpha);
                    }
                }
            }
        }

        private static Color GetRandomColor(float alpha)
        {
            return new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, alpha);
        }

        private static Color GetColor(int index, float alpha)
        {
            return index < s_defaultColors.Length ? s_defaultColors[index] : GetRandomColor(alpha);
        }

        private class CameraRow
        {
            public VirtualCamera camera;
            public bool active;
        }

        private static Color[] s_defaultColors;
        private static Color[] GenerateColors(float stepSize)
        {
            List<Color> colors = new List<Color>();
            var group = ColorPalette.Global.GetReadonlyGroup("VirtualCameras");
            for (int i = 0; i < k_maxColors; i++)
            {
                colors.Add(group[$"Camera {i}"]);
            }

            if (colors.Count == 0 || colors[0] == Color.clear)
            {
                //colors.Add(Color.blue);
                colors.Add(Color.cyan);
                colors.Add(Color.green);
                colors.Add(Color.magenta);
                colors.Add(Color.red);
                colors.Add(Color.white);
                colors.Add(Color.yellow);
            }
            float r_progress = 0.9f, g_progress = 0.8f, b_progress = 0.7f;
            while (r_progress > 0)
            {
                while (g_progress > 0)
                {
                    while (b_progress > 0)
                    {
                        colors.Add(new Color(r_progress, g_progress, b_progress));
                        b_progress -= stepSize;
                    }
                    g_progress -= stepSize;
                }
                r_progress -= stepSize;
            }
            return colors.ToArray();
        }
    }
}
