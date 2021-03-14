namespace TXT.WEAVR.Simulation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using TXT.WEAVR.License;
    using UnityEditor;
    using UnityEngine;

    public class SimulationModelGenerator : ScriptableWizard
    {
        private string[] _namespaces;
        private int _namespaceIndex;
        private Dictionary<string, NamespaceMeta> _allTypes;
        private string _simulationPlayerFolder;
        private string _simulationEditorFolder;
        private string _simulationScriptName;
        private List<NamespaceMeta> _selectedMetas;
        private Vector2 _scrollPosition;

#if !WEAVR_DLL
        [MenuItem("WEAVR/Simulation/Generate Model", priority = 40)]
#endif
        static void ShowWizard()
        {
            var generator = DisplayWizard<SimulationModelGenerator>("Simulation Model Generator", "Generate");
            generator.Initialize();
        }

        private void OnEnable()
        {
            if (!WeavrLE.IsValid())
            {
                DestroyImmediate(this);
                return;
            }
        }

        private void Initialize()
        {
            minSize = new Vector2(700, 400);
            _allTypes = new Dictionary<string, NamespaceMeta>();
            _simulationPlayerFolder = "/Generated/Simulation/Player/";
            _simulationEditorFolder = "/Generated/Simulation/Editor/";
            _simulationScriptName = "SimulationModel";
            NamespaceMeta nspaceMeta = null;
            foreach (var type in GetAllAssemblyTypes())
            {
                string nspace = string.IsNullOrEmpty(type.Namespace) ? "none" : type.Namespace;
                string key = nspace.Replace('.', '/');
                if (!_allTypes.TryGetValue(key, out nspaceMeta))
                {
                    nspaceMeta = new NamespaceMeta()
                    {
                        name = nspace,
                        foldout = false,
                        scrollPosition = Vector2.zero,
                        types = new List<TypeMeta>()
                    };
                    _allTypes.Add(key, nspaceMeta);
                }
                nspaceMeta.types.Add(new TypeMeta()
                {
                    type = type,
                    access = SharedMemoryAccess.Read,
                    selected = false
                });
            }

            _namespaces = new string[_allTypes.Keys.Count + 2];
            _namespaces[0] = "All";
            _namespaces[1] = "SharedMemory Compatible";
            int keyIndex = 2;
            foreach (var keyPair in _allTypes)
            {
                _namespaces[keyIndex++] = keyPair.Key;
                keyPair.Value.scrollHeight = (keyPair.Value.types.Count + 1) * EditorGUIUtility.singleLineHeight;
            }
            _namespaceIndex = -1;

            _selectedMetas = new List<NamespaceMeta>();
        }

        protected override bool DrawWizardGUI()
        {
            helpString = "This wizard will generate a script with a property per each selected type. " +
                         "It is useful when a simulation data structure has been changed and the WEAVR needs to get access to these new data.";
            int newAssemblyIndex = EditorGUILayout.Popup("Selected Namespace", _namespaceIndex, _namespaces);
            if (newAssemblyIndex != _namespaceIndex && 0 <= newAssemblyIndex && newAssemblyIndex < _namespaces.Length)
            {
                _namespaceIndex = newAssemblyIndex;
                _scrollPosition = Vector2.zero;
                _selectedMetas.Clear();
                if (_namespaceIndex == 0)
                {
                    _selectedMetas.AddRange(_allTypes.Values.OrderBy(m => m.name));
                }
                else if (_namespaceIndex == 1)
                {
                    // Show only shared memory compatible
                    _selectedMetas.AddRange(_allTypes.Values.Where(m => m.types.Count(t => t.type.StructLayoutAttribute.Value == LayoutKind.Sequential) > 0).OrderBy(m => m.name));
                }
                else
                {
                    _selectedMetas.Add(_allTypes[_namespaces[_namespaceIndex]]);
                }
            }

            if (_selectedMetas == null || _selectedMetas.Count == 0) { return false; }

            if (_selectedMetas.Count > 1)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Collapse All"))
                {
                    foreach (var nspaceMeta in _selectedMetas)
                    {
                        nspaceMeta.foldout = false;
                    }
                }
                if (GUILayout.Button("Expand All"))
                {
                    foreach (var nspaceMeta in _selectedMetas)
                    {
                        nspaceMeta.foldout = true;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                _selectedMetas[0].foldout = true;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var nspaceMeta in _selectedMetas)
            {
                nspaceMeta.foldout = EditorGUILayout.Foldout(nspaceMeta.foldout, nspaceMeta.name);

                if (nspaceMeta.foldout)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Select All"))
                    {
                        foreach (var typePair in nspaceMeta.types)
                        {
                            typePair.selected = true;
                        }
                    }
                    if (GUILayout.Button("Deselect All"))
                    {
                        foreach (var typePair in nspaceMeta.types)
                        {
                            typePair.selected = false;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    nspaceMeta.scrollPosition = EditorGUILayout.BeginScrollView(nspaceMeta.scrollPosition,
                                                                                GUILayout.Height(nspaceMeta.scrollHeight),
                                                                                GUILayout.MaxHeight(250));
                    EditorGUI.indentLevel++;

                    if (_namespaceIndex != 1)
                    {
                        foreach (var typeMeta in nspaceMeta.types)
                        {
                            if (typeMeta.type.StructLayoutAttribute.Value == LayoutKind.Sequential)
                            {
                                DrawAsSimulationVariable(typeMeta);
                            }
                            else
                            {
                                typeMeta.selected = EditorGUILayout.ToggleLeft(typeMeta.type.Name, typeMeta.selected);
                            }
                        }
                    }
                    else
                    {
                        foreach (var typeMeta in nspaceMeta.types)
                        {
                            if (typeMeta.type.StructLayoutAttribute.Value == LayoutKind.Sequential)
                            {
                                DrawAsSimulationVariable(typeMeta);
                            }
                            else
                            {
                                typeMeta.selected = false;
                            }
                        }
                    }

                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndScrollView();
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            _simulationScriptName = EditorGUILayout.TextField("Script name: ", _simulationScriptName);

            EditorGUIUtility.labelWidth = 40;

            // Player part
            EditorGUILayout.LabelField(string.Concat("Player: ", _simulationPlayerFolder, _simulationScriptName, ".cs"));

            // Editor part
            EditorGUILayout.LabelField(string.Concat("Editor: ", _simulationEditorFolder, _simulationScriptName, "Editor.cs"));
            return true;
        }

        private static void DrawAsSimulationVariable(TypeMeta typeMeta)
        {
            EditorGUILayout.BeginHorizontal();
            typeMeta.selected = EditorGUILayout.ToggleLeft(typeMeta.type.Name, typeMeta.selected, GUILayout.Width(200));
            bool wasGUIEnabled = GUI.enabled;
            GUI.enabled = typeMeta.selected;
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100;
            typeMeta.access = (SharedMemoryAccess)EditorGUILayout.EnumPopup(typeMeta.access, GUILayout.Width(100));
            typeMeta.memoryId = EditorGUILayout.TextField("Memory ID", typeMeta.memoryId ?? typeMeta.type.Name);
            EditorGUIUtility.labelWidth = labelWidth;
            GUI.enabled = wasGUIEnabled;
            EditorGUILayout.EndHorizontal();
        }

        private IEnumerable<Type> GetAllAssemblyTypes()
        {
            var thisAssembly = this.GetType().Assembly;
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(t =>
                {
                    var name = t.GetName().Name;
                    return !name.StartsWith("Unity")
                        && !name.StartsWith("mscorlib")
                        && !name.StartsWith("System")
                        && !name.StartsWith("Boo.")
                        && !name.StartsWith("Microsoft")
                        && !name.StartsWith("Json")
                        && !name.StartsWith("JSON")
                        && t != thisAssembly;
                })
                .SelectMany(t =>
                {
                    // Ugly hack to handle mis-versioned dlls
                    var innerTypes = new Type[0];
                    try
                    {
                        innerTypes = t.GetTypes();
                    }
                    catch { }
                    return innerTypes;
                })
                .Where(t => !(t.IsAbstract || t.IsGenericType || t.IsNotPublic || t.Name.StartsWith("<") || t.IsNested) && (t.IsValueType || t.GetConstructor(Type.EmptyTypes) != null));
        }

        void OnWizardCreate()
        {
            if (_selectedMetas == null) { return; }
            List<NamespaceMeta> selectedMetas = new List<NamespaceMeta>();
            foreach (var nspaceMeta in _selectedMetas)
            {
                if (nspaceMeta.types.Count(t => t.selected) > 0)
                {
                    selectedMetas.Add(nspaceMeta);
                }
            }
            if (selectedMetas.Count == 0) { return; }

            // Create both editor and player scripts
            // First create folders if not present
            string playerFullPath = Application.dataPath + _simulationPlayerFolder;
            string editorFullPath = Application.dataPath + _simulationEditorFolder;

            if (!Directory.Exists(playerFullPath))
            {
                Directory.CreateDirectory(playerFullPath);
            }
            if (!Directory.Exists(editorFullPath))
            {
                Directory.CreateDirectory(editorFullPath);
            }

            // Now create the scripts
            // The player one first
            StringBuilder usingBuilder = new StringBuilder();
            StringBuilder declarations = new StringBuilder();
            StringBuilder instantiations = new StringBuilder();
            string simVarAttributeName = typeof(SimulationVariableAttribute).Name.Replace("Attribute", "");
            string accessTypeName = typeof(SharedMemoryAccess).Name;
            foreach (var nspaceMeta in selectedMetas)
            {
                if (!_namespaceGenerated.Contains(nspaceMeta.name)
                    && "TXT.WEAVR.Core" != nspaceMeta.name
                    && "TXT.WEAVR.Simulation" != nspaceMeta.name)
                {
                    usingBuilder.AppendFormat(_playerTypeUsingFormat, nspaceMeta.name).AppendLine();
                }
                string escapedName = nspaceMeta.name.Replace(".", "");
                declarations.AppendLine().AppendFormat(_playerTypeSepFormat, nspaceMeta.name).AppendLine()
                            .AppendLine(_playerInstantiateBoolAttr)
                            .AppendFormat(_playerInstTooltipFormat, nspaceMeta.name).AppendLine()
                            .AppendFormat(_playerInstantiateBoolFormat, escapedName).AppendLine();
                instantiations.AppendFormat(_playerIntantiateOpenFormat, escapedName).AppendLine();
                foreach (var type in nspaceMeta.types)
                {
                    if (type.selected)
                    {
                        if (!string.IsNullOrEmpty(type.memoryId))
                        {
                            declarations.AppendLine(_playerShowPrimitivesFormat)
                                        .AppendFormat(_playerSimVarAttributeFormat,
                                                      simVarAttributeName,
                                                      accessTypeName,
                                                      type.access,
                                                      type.memoryId)
                                        .AppendLine();
                        }
                        if (type.type.IsSubclassOf(typeof(UnityEngine.Object)))
                        {
                            string name = type.type.Name.Substring(0, 1).ToLower() + type.type.Name.Substring(1);
                            declarations.AppendFormat(_playerTypeFormat, type.type.Name, name).AppendLine(";");
                        }
                        else
                        {
                            declarations.AppendFormat(_playerTypeFormat, type.type.Name, type.type.Name).AppendLine(" { get; set; }");
                            if (!type.type.IsValueType)
                            {
                                instantiations.AppendFormat(_playerTypeAwakeFormat, type.type.Name).AppendLine();
                            }
                        }
                    }
                }
                instantiations.AppendLine(_playerIntantiateClose);
            }

            string playerFilePath = playerFullPath + _simulationScriptName + ".cs";
            if (File.Exists(playerFilePath))
            {
                File.Delete(playerFilePath);
            }
            File.WriteAllText(playerFilePath, string.Format(_playerCodeWithPlaceHolders,
                                                            GetType().Name,
                                                            usingBuilder.ToString(),
                                                            _namespaceGenerated,
                                                            _simulationScriptName,
                                                            declarations.ToString(),
                                                            instantiations.ToString()));
            AssetDatabase.Refresh();
        }

        private class TypeMeta
        {
            public Type type;
            public SharedMemoryAccess access;
            public string memoryId;
            public bool selected;
        }

        private class NamespaceMeta
        {
            public string name;
            public bool foldout;
            public float scrollHeight;
            public Vector2 scrollPosition;
            public List<TypeMeta> types;
        }

        private static string _playerCodeWithPlaceHolders =
            @"/* AUTO-GENERATED
* This file was generated by {0}.
* Any changes applied manually to this file will most probably be overwritten.
* 
* Copyright © TXTGroup
*/

{1}
using UnityEngine;
using TXT.WEAVR.Core;
using TXT.WEAVR.Simulation;

namespace {2}
{{

    public class {3} : MonoBehaviour
    {{
        public enum MapOn {{ None, Awake, Start }}
        [DoNotExpose]
        public MapOn memoryMapOn = MapOn.Awake;
{4}
        void Awake(){{
{5}
            if (memoryMapOn == MapOn.Awake) {{
                SimulationEvalEngine.Instance.MapToSharedMemory(this);
            }}
        }}

        void Start() {{
            if(memoryMapOn == MapOn.Start) {{
                SimulationEvalEngine.Instance.MapToSharedMemory(this);
            }}
        }}
    }}
}}";

        private static string _namespaceGenerated = "TXT.Generated";
        private static string _playerTypeUsingFormat = "using {0};";
        private static string _playerTypeSepFormat = "        // -- {0}";
        private static string _playerInstantiateBoolAttr = "        [DoNotExpose]";
        private static string _playerInstTooltipFormat = "        [Tooltip(\"Create all selected {0} namespace instances on awake\")]";
        private static string _playerInstantiateBoolFormat = "        public bool new{0} = true;";
        private static string _playerShowPrimitivesFormat = "        [ShowPrimitivesOnly]";
        private static string _playerSimVarAttributeFormat = "        [{0}({1}.{2}, @\"{3}\")]";
        private static string _playerTypeFormat = "        public {0} {1}";
        private static string _playerIntantiateOpenFormat = "            if(new{0}){{";
        private static string _playerTypeAwakeFormat = "               {0} = new {0}();";
        private static string _playerIntantiateClose = "            }";
    }
}