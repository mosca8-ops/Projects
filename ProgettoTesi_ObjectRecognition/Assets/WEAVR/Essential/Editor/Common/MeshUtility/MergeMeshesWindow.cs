using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using TXT.WEAVR;
using TXT.WEAVR.Editor;
using System.IO;
using System;
using UnityEngine.Rendering;
using TXT.WEAVR.Common;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

public class MergeMeshesWindow : EditorWindow
{
    public const float k_SimilarityEpsilon = 0.01f;

#if !WEAVR_DLL
    [MenuItem("WEAVR/Utilities/Merge Models", priority = 5)]
#endif
    public static void ShowExample()
    {
        MergeMeshesWindow wnd = GetWindow<MergeMeshesWindow>();
        wnd.titleContent = new GUIContent("Merge Models");
    }

    private string m_thisPath;
    public string WindowAssetPath {
        get {
            if (m_thisPath == null)
            {
                var monoScript = MonoScript.FromScriptableObject(this);
                m_thisPath = AssetDatabase.GetAssetPath(monoScript).Replace(monoScript.name + ".cs", "");
            }
            return m_thisPath;
        }
    }

    MeshesMerger m_meshMerger;
    MaterialMergeCriteria m_materialMergeCriteria;

    [NonSerialized]
    GameObject m_source;

    Button m_mergeHierarchyButton;
    Button m_mergeLeavesButton;
    ObjectField m_sourceField;
    TextField m_modelDestination;
    TextField m_modelName;
    Toggle m_removeChildren;
    Toggle m_useSelected;
    Toggle m_makePrefab;
    Toggle m_mergeMaterials;
    Toggle m_swapOriginal;

    VisualElement m_mergeMaterialsOptions;
    Toggle m_sameColor;
    Toggle m_sameName;
    Toggle m_sameProperties;
    Toggle m_sameTexture;
    Toggle m_sameShader;
    Toggle m_copyMaterials;
    EnumField m_extractMaterials;

    Label m_childrenCount;
    Label m_meshesCount;
    Label m_materialsCount;
    Label m_verticesCount;
    Label m_polyCount;

    Label m_messageLabel;

    VisualElement m_pivotModeField;

    int m_sameNameDistance = 5;
    float m_sameColorEpsilon = k_SimilarityEpsilon;

    public enum PivotMode
    {
        Root, Center, Adjustable
    }

    enum ExtractMaterialsMode
    {
        DoNotExtract, ExtractAndCopy, ExtractAndMerge
    }

    [NonSerialized]
    PivotMode m_pivotMode = PivotMode.Root;

    public PivotMode Pivot {
        get => m_pivotMode;
        set {
            if (m_pivotMode != value)
            {
                m_pivotMode = value;
                UpdatePivotToggles(value);
                Tools.hidden = false;
                SceneView.duringSceneGui -= SceneView_DuringSceneGui;
                if (value == PivotMode.Adjustable)
                {
                    Tools.hidden = true;
                    SceneView.duringSceneGui += SceneView_DuringSceneGui;
                }
                SceneView.RepaintAll();
            }
        }
    }

    private void OnDisable()
    {
        Tools.hidden = false;
    }

    private void SceneView_DuringSceneGui(SceneView obj)
    {
        Tools.hidden = true;
        PivotCenter.position = Handles.PositionHandle(PivotCenter.position, Tools.pivotRotation == PivotRotation.Global ? Quaternion.identity : PivotCenter.rotation);
        PivotCenter.rotation = Handles.RotationHandle(PivotCenter.rotation, PivotCenter.position);
    }

    private HashSet<VisualElement> m_valuesToSync = new HashSet<VisualElement>();

    private Transform m_pivotCenter;
    private bool m_selectionIsChanging;

    private Transform PivotCenter {
        get {
            if (!m_pivotCenter)
            {
                m_pivotCenter = new GameObject("PivotCenter").transform;
                m_pivotCenter.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
            return m_pivotCenter;
        }
    }

    public void OnEnable()
    {
        if (m_materialMergeCriteria == null)
            m_materialMergeCriteria = new MaterialMergeCriteria(false, m_sameNameDistance, m_sameColorEpsilon);

        if (m_meshMerger == null)
            m_meshMerger = new MeshesMerger(m_materialMergeCriteria);

        minSize = new Vector2(400, 300);

        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // Import UXML
        VisualElement labelFromUXML = WeavrStyles.CreateFromTemplate("Windows/MergeMeshesWindow");
        root.Add(labelFromUXML);
        root.AddStyleSheetPath("Styles/MergeMeshesWindow");

        InitializeData(root);
    }

    private void InitializeData(VisualElement root)
    {
        root.Query<Toggle>(className: "button-toggle").ForEach(t => t.RegisterCallback<MouseUpEvent>(e => t.value = !t.value));

        var optionsSection = root.Q("options-section");
        var statsSection = root.Q("stats-section");

        m_valuesToSync.Clear();

        m_sourceField = optionsSection.Q<ObjectField>("source-field");
        m_sourceField.objectType = typeof(GameObject);
        m_sourceField.RegisterValueChangedCallback(UpdateSource);

        m_modelDestination = optionsSection.Q<TextField>("model-path");
        m_modelDestination.value = m_modelDestination.text;
        var showFolderButton = m_modelDestination.Q<Button>("show-folder");
        showFolderButton.clicked -= FocusDestinationFolder;
        showFolderButton.clicked += FocusDestinationFolder;
        showFolderButton.SetEnabled(!string.IsNullOrEmpty(m_modelDestination.value));
        m_modelDestination.RegisterValueChangedCallback(e => showFolderButton.SetEnabled(!string.IsNullOrEmpty(e.newValue)));

        m_modelName = optionsSection.Q<TextField>("model-name");
        m_pivotModeField = optionsSection.Q("pivot-mode");

        m_pivotModeField?.Query<Toggle>().AtIndex(0).RegisterCallback<MouseUpEvent>(e => Pivot = 0);
        m_pivotModeField?.Query<Toggle>().AtIndex(1).RegisterCallback<MouseUpEvent>(e => Pivot = (PivotMode)1);
        m_pivotModeField?.Query<Toggle>().AtIndex(2).RegisterCallback<MouseUpEvent>(e => Pivot = (PivotMode)2);

        m_removeChildren = optionsSection.Q<Toggle>("remove-children");
        m_makePrefab = AddToSync(optionsSection.Q<Toggle>("make-prefab"));
        m_useSelected = AddToSync(optionsSection.Q<Toggle>("use-selected"));
        m_useSelected.RegisterValueChangedCallback(e => SetUpdateSelected());
        m_mergeMaterials = AddToSync(optionsSection.Q<Toggle>("merge-materials"));
        m_mergeMaterialsOptions = optionsSection.Q("materials-options");
        m_sameColor = AddToSync(m_mergeMaterialsOptions.Q<Toggle>("merge-same-color"));
        m_sameColor.RegisterValueChangedCallback(e => UpdateStats());
        var similarityColorText = AddToSync(m_sameColor.Q<TextField>());
        if (similarityColorText != null)
        {
            m_sameColorEpsilon = float.TryParse(similarityColorText.value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : m_sameColorEpsilon;
            similarityColorText.RegisterValueChangedCallback(e =>
            {
                if (float.TryParse(e.newValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float similarity) && similarity != m_sameColorEpsilon)
                {
                    m_sameColorEpsilon = similarity;
                    UpdateStats();
                }

            });
        }

        m_sameName = AddToSync(m_mergeMaterialsOptions.Q<Toggle>("merge-same-name"));
        m_sameName.RegisterValueChangedCallback(e => UpdateStats());
        var similarityNameText = AddToSync(m_sameName.Q<TextField>());
        if (similarityNameText != null)
        {
            m_sameNameDistance = int.TryParse(similarityColorText.value, out int result) ? result : m_sameNameDistance;
            similarityNameText.RegisterValueChangedCallback(e =>
            {
                if (int.TryParse(e.newValue, out int similarity) && similarity != m_sameNameDistance)
                {
                    m_sameNameDistance = similarity;
                    UpdateStats();
                }
            });
        }

        m_sameProperties = AddToSync(m_mergeMaterialsOptions.Q<Toggle>("merge-same-properties"));
        m_sameProperties.RegisterValueChangedCallback(e => UpdateStats());
        m_sameTexture = AddToSync(m_mergeMaterialsOptions.Q<Toggle>("merge-same-texture"));
        m_sameTexture.RegisterValueChangedCallback(e => UpdateStats());
        m_sameShader = AddToSync(m_mergeMaterialsOptions.Q<Toggle>("merge-same-shader"));
        m_sameShader.RegisterValueChangedCallback(e => UpdateStats());
        m_copyMaterials = AddToSync(m_mergeMaterialsOptions.Q<Toggle>("copy-materials"));
        m_copyMaterials.RegisterValueChangedCallback(e => m_extractMaterials.style.display = e.newValue ? DisplayStyle.Flex : DisplayStyle.None);
        m_extractMaterials = AddToSync(m_mergeMaterialsOptions.Q<EnumField>("extract-materials"));
        m_extractMaterials.Init(ExtractMaterialsMode.DoNotExtract);

        m_mergeMaterials.RegisterValueChangedCallback(e =>
        {
            if (e.newValue)
            {
                m_mergeMaterials.parent.Insert(m_mergeMaterials.parent.IndexOf(m_mergeMaterials) + 1, m_mergeMaterialsOptions);
            }
            else
            {
                m_mergeMaterialsOptions.RemoveFromHierarchy();
            }
            m_copyMaterials.value = e.newValue;
            UpdateStats();
        });

        m_swapOriginal = AddToSync(optionsSection.Q<Toggle>("swap-original"));

        m_mergeHierarchyButton = root.Q<Button>("merge-hierarchy");
        m_mergeHierarchyButton.clicked -= MergeButton_Clicked;
        m_mergeHierarchyButton.clicked += MergeButton_Clicked;

        m_mergeLeavesButton = root.Q<Button>("merge-leaves");
        m_mergeLeavesButton.clicked -= MergeLeavesButton_Clicked;
        m_mergeLeavesButton.clicked += MergeLeavesButton_Clicked;

        m_mergeHierarchyButton.parent.SetEnabled(m_sourceField.value);

        m_childrenCount = statsSection.Q("children-count").Q<Label>("value");
        m_meshesCount = statsSection.Q("meshes-count").Q<Label>("value");
        m_materialsCount = statsSection.Q("materials-count").Q<Label>("value");
        m_verticesCount = statsSection.Q("vertices-count").Q<Label>("value");
        m_polyCount = statsSection.Q("polygons-count").Q<Label>("value");

        m_messageLabel = root.Q<Label>("message-label");

        UpdatePivotToggles(Pivot);
        SyncFromSavedValues();
        SetUpdateSelected();
        SetMessage(null);
    }

    private void FocusDestinationFolder()
    {
        EditorUtility.FocusProjectWindow();
        if (!string.IsNullOrEmpty(m_modelDestination.value))
        {
            var path = Path.Combine(Application.dataPath.Replace("Assets", ""), !string.IsNullOrEmpty(m_modelDestination.value) ? m_modelDestination.value : m_modelDestination.text);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            var folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(m_modelDestination.value.TrimEnd('/').TrimEnd('\\'));
            EditorGUIUtility.PingObject(folder);
        }
    }

    private void SelectionChanged()
    {
        if (!m_selectionIsChanging)
        {
            m_selectionIsChanging = true;
            if (Selection.gameObjects.Length == 1)
            {
                UpdateSource(Selection.activeGameObject, false);
                m_mergeHierarchyButton.text = $"Merge Selected";
                PivotCenter.position = Selection.activeGameObject.transform.position;
                PivotCenter.forward = Selection.activeGameObject.transform.forward;
            }
            else if (Selection.gameObjects.Length > 1)
            {
                UpdateSource(Selection.gameObjects[0], false);
                m_mergeHierarchyButton.text = $"Merge Selected [{Selection.gameObjects.Length}]";

                if (Selection.gameObjects.Any(g => g.GetComponentsInChildren<MeshFilter>(true).Any(f => f.sharedMesh && !f.sharedMesh.isReadable)))
                {
                    m_mergeHierarchyButton.parent.SetEnabled(false);
                    SetMessage("Warning: there are non-readable meshes in selection. Please enable 'Read/Write' flag in the mesh inspector.");
                }
                else
                {
                    m_mergeHierarchyButton.parent.SetEnabled(true);
                }

                switch (Pivot)
                {
                    case PivotMode.Center:
                        PivotCenter.position = Selection.gameObjects.Select(g => g.transform.position).Aggregate((a, v) => a + v) / Selection.gameObjects.Length;
                        PivotCenter.forward = Selection.gameObjects[0].transform.forward;
                        SceneView.lastActiveSceneView.Frame(GetBounds(Selection.gameObjects), false);
                        break;
                    case PivotMode.Adjustable:
                        SceneView.lastActiveSceneView.Frame(GetBounds(Selection.gameObjects), false);
                        break;
                }
            }
            m_selectionIsChanging = false;
        }
        UpdateStats();
    }

    #region [ Sync ]
    private T AddToSync<T>(T elem) where T : VisualElement
    {
        m_valuesToSync.Add(elem);
        return elem;
    }

    private void SyncFromSavedValues()
    {
        Pivot = (PivotMode)EditorPrefs.GetInt("MergeMeshesTool::PivotMode", (int)Pivot);
        foreach (var elem in m_valuesToSync)
        {
            switch (elem)
            {
                case Toggle field: SyncFromValue(field); break;
                case TextField field: SyncFromValue(field); break;
                case EnumField field: SyncFromValue(field); break;
            }
        }
    }

    private void SyncToSavedValues()
    {
        EditorPrefs.SetInt("MergeMeshesTool::PivotMode", (int)Pivot);
        foreach (var elem in m_valuesToSync)
        {
            switch (elem)
            {
                case Toggle field: SyncToValue(field); break;
                case TextField field: SyncToValue(field); break;
                case EnumField field: SyncToValue(field); break;
            }
        }
    }

    private void SyncFromValue(Toggle toggle)
    {
        toggle.value = EditorPrefs.GetBool("MergeMeshesTool::" + toggle.name, toggle.value);
    }

    private void SyncToValue(Toggle toggle) => EditorPrefs.SetBool("MergeMeshesTool::" + toggle.name, toggle.value);

    private void SyncFromValue(TextField textField)
    {
        textField.value = EditorPrefs.GetString("MergeMeshesTool::" + textField.name, textField.value);
    }

    private void SyncToValue(TextField textField) => EditorPrefs.SetString("MergeMeshesTool::" + textField.name, textField.value);

    private void SyncFromValue(EnumField enumField)
    {
        enumField.value = (Enum)Enum.Parse(enumField.value.GetType(), EditorPrefs.GetString("MergeMeshesTool::" + enumField.name, enumField.value.ToString()));
    }

    private void SyncToValue(EnumField enumField) => EditorPrefs.SetString("MergeMeshesTool::" + enumField.name, enumField.value.ToString());
    #endregion

    #region [ Window Updates ]
    private void SetUpdateSelected()
    {
        Selection.selectionChanged -= SelectionChanged;
        m_sourceField.SetEnabled(!m_useSelected.value);
        if (m_useSelected.value)
        {
            m_mergeHierarchyButton.text = "Merge Selected";
            Selection.selectionChanged += SelectionChanged;

            SelectionChanged();
        }
        else
        {
            m_mergeHierarchyButton.text = "Merge Hierarchy";
        }
    }

    private void UpdateSource(ChangeEvent<UnityEngine.Object> evt)
    {
        if (evt.newValue != m_source)
        {
            UpdateSource(evt.newValue as GameObject);
        }
    }

    private void UpdateSource(GameObject source, bool updateStats = true)
    {
        m_source = source;
        m_sourceField.value = source;
        m_mergeHierarchyButton.parent.SetEnabled(source);
        if (m_source && PrefabUtility.IsPartOfAnyPrefab(m_source))
        {
            SetMessage("Warning: the selected object is part of a prefab. It will be completely unpacked before merging.");
        }
        else if (m_source.GetComponentsInChildren<MeshFilter>(true).Any(f => f.sharedMesh && !f.sharedMesh.isReadable))
        {
            m_mergeHierarchyButton.parent.SetEnabled(false);
            SetMessage("Warning: there are non-readable meshes in selection. Please enable 'Read/Write' flag in the mesh inspector.");
        }
        else
        {
            SetMessage(null);
        }

        m_modelName.value = m_source ? m_source.name.Replace('\\', ' ').Replace('/', ' ') : "-";

        if (updateStats)
        {
            UpdateStats();
        }
    }

    private void UpdateStats()
    {
        if (!m_source)
        {
            m_modelName.value = "-";

            m_childrenCount.text = 0.ToString();
            m_meshesCount.text = 0.ToString();
            m_materialsCount.text = 0.ToString();
            m_verticesCount.text = 0.ToString();
            m_polyCount.text = 0.ToString();

            return;
        }

        UpdateMaterialComparison();

        IEnumerable<GameObject> sources = m_useSelected.value ? Selection.gameObjects : new GameObject[] { m_source };

        var meshes = sources.SelectMany(s => s.GetComponentsInChildren<MeshFilter>()).Where(m => m && m.sharedMesh);
        var children = sources.SelectMany(s => s.GetComponentsInChildren<Transform>(true));

        m_childrenCount.text = $"{children.Count() - 1} ({children.Count(t => t.gameObject.activeInHierarchy) - 1} Active)";
        m_meshesCount.text = meshes.Count().ToString();
        var sharedMaterials = m_mergeMaterials.value ?
                                    m_meshMerger.MergeMaterials(meshes.Select(m => m.GetComponent<Renderer>()).Where(r => r).SelectMany(r => r.sharedMaterials).Where(m => m).ToArray()) :
                                    meshes.Select(m => m.GetComponent<Renderer>()).Where(r => r).SelectMany(r => r.sharedMaterials).Where(m => m).ToArray();
        m_materialsCount.text = $"{sharedMaterials.Length} ({sharedMaterials.Distinct().Count()} Distinct)";
        m_verticesCount.text = meshes.Sum(m => m.sharedMesh.vertexCount).ToString();
        m_polyCount.text = meshes.Sum(m => m.sharedMesh.triangles.Length / 3).ToString();
    }

    private void UpdateMaterialComparison()
    {
        m_meshMerger.MaterialMergeCriteria.SameColor = m_sameColor.value;
        m_meshMerger.MaterialMergeCriteria.SameName = m_sameName.value;
        m_meshMerger.MaterialMergeCriteria.SameProperties = m_sameProperties.value;
        m_meshMerger.MaterialMergeCriteria.SameTexture = m_sameTexture.value;
        m_meshMerger.MaterialMergeCriteria.SameShader = m_sameShader.value;
        m_meshMerger.MaterialMergeCriteria.SameColorEpsilon = m_sameColorEpsilon;
        m_meshMerger.MaterialMergeCriteria.SameNameDistance = m_sameNameDistance;
    }

    private void UpdatePivotToggles(PivotMode value)
    {
        int index = 0;
        foreach (var toggle in m_pivotModeField?.Query<Toggle>().ToList())
        {
            if (toggle != null)
            {
                toggle.value = (int)value == index++;
            }
        }
    }
    #endregion

    private Bounds GetBounds(GameObject[] gameObjects)
    {
        Bounds bounds = gameObjects[0].GetComponent<Renderer>() ? gameObjects[0].GetComponent<Renderer>().bounds : new Bounds(gameObjects[0].transform.position, Vector3.one);
        foreach (var renderer in gameObjects.SelectMany(g => g.GetComponentsInChildren<Renderer>()))
        {
            bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }

    public void SetMessage(string message)
    {
        if (m_messageLabel == null) { return; }
        if (string.IsNullOrEmpty(message))
        {
            m_messageLabel.text = message;
            m_messageLabel.parent.style.display = DisplayStyle.None;
        }
        else
        {
            m_messageLabel.text = message;
            m_messageLabel.parent.style.display = DisplayStyle.Flex;
        }
    }

    private void MergeLeavesButton_Clicked()
    {
        var sources = m_meshMerger.GetOutermostLeaves(m_source.transform).Select(t => t.parent).ToArray();
        var mergedRoots = sources.Where(s => s).Select(s => Merge(s.gameObject, s.name)).ToArray();
        Selection.objects = mergedRoots;
    }

    private void MergeButton_Clicked()
    {
        if (m_useSelected.value)
        {
            Selection.activeObject = Merge(m_source, m_modelName.value, Selection.gameObjects);
        }
        else
        {
            Selection.activeObject = Merge(m_source, m_modelName.value);
        }
    }

    private GameObject Merge(GameObject source, string meshName, IEnumerable<GameObject> objects = null)
    {
        SyncToSavedValues();

        var path = Path.Combine(Application.dataPath.Replace("Assets", ""), !string.IsNullOrEmpty(m_modelDestination.value) ? m_modelDestination.value : m_modelDestination.text);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        var (mesh, materials) = objects != null ? m_meshMerger.CombineList(source, objects, m_mergeMaterials.value) : m_meshMerger.CombineChildren(source, m_mergeMaterials.value);

        GameObject go = source;

        if (mesh)
        {
            mesh.name = meshName;

            if (PrefabUtility.IsPartOfAnyPrefab(source))
            {
                var rootPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(source);
                PrefabUtility.UnpackPrefabInstance(rootPrefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            switch (Pivot)
            {
                case PivotMode.Center:
                case PivotMode.Adjustable:
                    AdjustMeshPivot(mesh, source.transform, PivotCenter);
                    break;
            }

            go = new GameObject(meshName);
            var renderer = go.AddComponent<MeshRenderer>();
            var meshFilter = go.AddComponent<MeshFilter>();

            //if (m_mergeMaterials.value)
            //{
            //    materials = MergeMaterials(materials);
            //}

            meshFilter.sharedMesh = mesh;
            Material[] originalMaterials = materials;
            if (m_copyMaterials.value)
            {
                materials = m_meshMerger.SmartCloneMaterials(materials);
            }

            GameObject prefab = null;
            if (m_makePrefab.value)
            {
                prefab = PrefabUtility.SaveAsPrefabAsset(go, FileUtil.GetProjectRelativePath(Path.Combine(path, meshName + ".prefab")));
                AssetDatabase.AddObjectToAsset(mesh, prefab);
                if (m_copyMaterials.value)
                {
                    switch ((ExtractMaterialsMode)m_extractMaterials.value)
                    {
                        case ExtractMaterialsMode.ExtractAndCopy:
                            var materialsPath = Path.Combine(path, "Materials");
                            if (!Directory.Exists(materialsPath))
                            {
                                Directory.CreateDirectory(materialsPath);
                            }
                            foreach (var mat in materials)
                            {
                                AssetDatabase.CreateAsset(mat, FileUtil.GetProjectRelativePath(Path.Combine(materialsPath, mat.name + ".mat")));
                            }
                            break;
                        case ExtractMaterialsMode.ExtractAndMerge:
                            materialsPath = Path.Combine(path, "Materials");
                            if (!Directory.Exists(materialsPath))
                            {
                                Directory.CreateDirectory(materialsPath);
                            }
                            var alreadyExtractedMaterials = originalMaterials.Where(m => AssetDatabase.IsMainAsset(m)).ToArray();
                            var existingMaterials = Directory.EnumerateFiles(materialsPath).Select(p => AssetDatabase.LoadAssetAtPath<Material>(FileUtil.GetProjectRelativePath(p))).Where(m => m).ToArray();
                            Dictionary<Material, List<int>> materialIndices = new Dictionary<Material, List<int>>();
                            for (int i = 0; i < materials.Length; i++)
                            {
                                if (!materialIndices.TryGetValue(materials[i], out List<int> indices))
                                {
                                    indices = new List<int>();
                                    materialIndices[materials[i]] = indices;
                                }
                                indices.Add(i);
                            }

                            Dictionary<Material, Material> similarMaterials = new Dictionary<Material, Material>();

                            foreach (var mat in materialIndices.Keys)
                            {
                                similarMaterials[mat] = m_meshMerger.FindMostSimilar(mat, alreadyExtractedMaterials, existingMaterials);
                            }

                            foreach (var indices in materialIndices)
                            {
                                var similarMaterial = similarMaterials[indices.Key];
                                foreach (var index in indices.Value)
                                {
                                    materials[index] = similarMaterial;
                                }
                            }
                            foreach (var pair in similarMaterials)
                            {
                                if (pair.Key == pair.Value && !AssetDatabase.Contains(pair.Value))
                                {
                                    AssetDatabase.CreateAsset(pair.Value, FileUtil.GetProjectRelativePath(Path.Combine(materialsPath, pair.Value.name + ".mat")));
                                }
                                else
                                {
                                    AssetDatabase.AddObjectToAsset(pair.Key as Material, prefab);
                                }
                            }
                            break;
                        default:
                            foreach (var mat in materials)
                            {
                                if (!AssetDatabase.Contains(mat))
                                {
                                    AssetDatabase.AddObjectToAsset(mat, prefab);
                                }
                            }
                            break;
                    }
                }
                var prefabMeshFilter = prefab.GetComponent<MeshFilter>();
                prefabMeshFilter.sharedMesh = mesh;

                prefab.GetComponent<Renderer>().sharedMaterials = materials;
            }

            meshFilter.sharedMesh = mesh;
            renderer.sharedMaterials = materials;

            if (m_swapOriginal.value)
            {
                if (m_makePrefab.value)
                {
                    DestroyImmediate(go);
                    go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                }
                var parent = source.transform.parent;
                go.name = source.name;
                go.hideFlags = source.hideFlags;
                go.isStatic = source.isStatic;

                if (parent)
                {
                    go.transform.SetParent(parent, true);
                }

                go.transform.SetSiblingIndex(source.transform.GetSiblingIndex());
                switch (Pivot)
                {
                    case PivotMode.Center:
                        go.transform.position = PivotCenter.position;
                        go.transform.rotation = PivotCenter.rotation;
                        go.transform.localScale = Vector3.one;
                        break;
                    case PivotMode.Adjustable:
                        go.transform.position = PivotCenter.position;
                        go.transform.rotation = PivotCenter.rotation;
                        go.transform.localScale = Vector3.one;
                        break;
                    default:
                        go.transform.localPosition = source.transform.localPosition;
                        go.transform.localRotation = source.transform.localRotation;
                        go.transform.localScale = source.transform.localScale;
                        break;
                }

                DestroyImmediate(source);
                source = null;

                foreach (var g in objects)
                {
                    if (g)
                    {
                        DestroyImmediate(g);
                    }
                }
            }
            else
            {
                DestroyImmediate(go);
                go = source;
            }
        }

        return go;
    }

    private void AdjustMeshPivot(Mesh mesh, Transform from, Transform to)
    {
        var delta = to.position - from.position;
        mesh.SetVertices(mesh.vertices.Select(v => to.InverseTransformPoint(from.TransformPoint(v))).ToArray());
        mesh.SetNormals(mesh.normals.Select(v => to.InverseTransformDirection(from.TransformDirection(v))).ToArray());
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        mesh.Optimize();
        mesh.MarkModified();
    }

    private Vector3 Divide(Vector3 a, Vector3 b) => new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
}