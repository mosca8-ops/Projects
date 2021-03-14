namespace TXT.WEAVR.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using TXT.WEAVR.License;
    using System.IO;
    using System;
    using TXT.WEAVR.Packaging;
    using TXT.WEAVR.Procedure;
    using TXT.WEAVR.Localization;
    using TXT.WEAVR.Core;
    using UnityEngine.Events;
    using Newtonsoft.Json;
    using UnityEditor.Callbacks;
    using TXT.WEAVR.Common;
    using System.Linq;
    using System.Security.Cryptography;
    using TXT.WEAVR.Communication.Entities;

    using Object = UnityEngine.Object;
    using SceneEntity = Communication.Entities.Scene;
    using ProcedureAsset = Procedure.Procedure;
    using ProcedureEntity = Communication.Entities.Procedure;
    using TXT.WEAVR.Communication.DTO;
    using System.Text;

    [System.Serializable]
    [InitializeOnLoad]
    public class UploadProcedureWindow : EditorWindow, ISerializationCallbackReceiver
    {

        public class WeavrUploadSettings : IWeavrSettingsClient
        {
            public string SettingsSection => "Upload";

            public IEnumerable<ISettingElement> Settings =>

                new Setting[] { ("WeavrServerURL", "https://weavrmanageruidevelop.azurewebsites.net/procedures" as object, "The address for weavr server upload APIs", SettingsFlags.EditableInEditor) };
        }

        static UploadProcedureWindow()
        {
            ProcedureEditor.RegisterCommand(new GUIContent("Upload", "Upload the procedure to WEAVR Manager"), ShowWindow);
        }

        public class Styles : BaseStyles
        {
            public GUIStyle centeredLabel;
            public GUIStyle boldLabel;
            public GUIStyle titleLabel;
            public GUIStyle normalLabel;
            public GUIStyle groupBox;
            public GUIStyle box;
            public GUIStyle errorLabel;

            protected override void InitializeStyles(bool isProSkin)
            {
                centeredLabel = new GUIStyle("Label")
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                boldLabel = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = true,
                };
                errorLabel = new GUIStyle(boldLabel)
                {
                    normal = new GUIStyleState()
                    {
                        textColor = Color.red,
                    }
                };
                titleLabel = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13,
                    padding = new RectOffset(0, 0, 0, 6),
                };
                normalLabel = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                };
                groupBox = new GUIStyle("GroupBox");
                box = new GUIStyle("Box");
            }
        }

        public static string ServerURL { get; set; }
        private readonly static MD5 s_MD5 = MD5.Create();

        private bool m_restoreProcedureView = false;
        public delegate void OnAssetBundleEvent();
        public static event OnAssetBundleEvent OnBeforeAssetBundleBuildingStart;
        public static event OnAssetBundleEvent OnAssetBundleBuildingEnd;
        public static UnityEvent OnRefreshNeeded = new UnityEvent();

        private string m_currentPath;
        private string m_filename;
        private Vector2 m_scrollPosition;
        [NonSerialized]
        private bool m_needsRefresh;

        private static Styles s_styles = new Styles();
        private string m_jsonContent;

        public string sceneAssetPath = "";
        public string bundleName = "";
        public string procedurePath = "";
        public int stepsCounter = 0;
        public ProcedureEntity procedureEntity;
        public string[] additiveScenesPathList;
        public List<SceneEntity> additiveScenes;

        private List<SceneLabels> m_sceneLabels;
        private ProcedureAsset m_procedureAsset;

        private Dictionary<Object, string> m_assetBundlesNames = new Dictionary<Object, string>();

        public ProcedureAsset Procedure
        {
            get => m_procedureAsset;
            set
            {
                if(m_procedureAsset != value)
                {
                    if (m_procedureAsset)
                    {
                        m_procedureAsset.OnModified -= ProcedureAsset_OnModified;
                    }
                    m_procedureAsset = value;
                    if (m_procedureAsset)
                    {
                        m_procedureAsset.OnModified -= ProcedureAsset_OnModified;
                        m_procedureAsset.OnModified += ProcedureAsset_OnModified;
                        RefreshUploadData();
                    }
                    else
                    {
                        ResetUploadData();
                    }
                }
            }
        }
        
        [SerializeField]
        private string CopySceneGuid = "";
        [SerializeField]
        private string CopySceneName = "";
        [SerializeField]
        private string CopyAssetPath = "";
        [SerializeField]
        private string CopyBundleName = "";
        [SerializeField]
        private string CopyProcedureName = "";
        [SerializeField]
        private string CopyProcedureGuid = "";
        [SerializeField]
        private string CopyProcedurePath = "";
        [SerializeField]
        private string CopyUpdateDate = "";
        [SerializeField]
        private string CopyCreateDate = "";
        [SerializeField]
        private int CopyStepsCounter = 0;
        [SerializeField]
        private string CopyServerURL = "";
        [SerializeField]
        private string CopyJsonContent = "";
        [SerializeField]
        private string[] CopyAddittiveScenes;

        public static void ShowWindow()
        {
            if (!WeavrLE.IsValid())
            {
                return;
            }

            //Show existing window instance. If one doesn't exist, make one.
            var mainWindow = GetWindow<UploadProcedureWindow>();
            mainWindow.Init();
        }

        private void Init()
        {
            wantsMouseEnterLeaveWindow = true;
            titleContent = new GUIContent("Upload", WeavrStyles.Icons["uploadIcon"]);
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(CopyJsonContent))
            {
                var uploadedProcedure = JsonConvert.DeserializeObject<UploadedProcedure>(CopyJsonContent);
                procedureEntity = uploadedProcedure?.Procedure;
                additiveScenes = uploadedProcedure?.AdditiveScenes?.ToList();
            }

            EditorApplication.wantsToQuit -= CloseWindow;
            EditorApplication.wantsToQuit += CloseWindow;

            m_needsRefresh = true;
            if (m_procedureAsset)
            {
                m_procedureAsset.OnModified -= ProcedureAsset_OnModified;
                m_procedureAsset.OnModified += ProcedureAsset_OnModified;
            }
        }

        private void Update()
        {
            Procedure = ProcedureEditor.Instance.LastProcedure;
        }

        private bool CloseWindow()
        {
            CopyJsonContent = "";
            return true;
        }

        private void OnDisable()
        {
            EditorApplication.wantsToQuit -= CloseWindow;
            if (m_procedureAsset)
            {
                m_procedureAsset.OnModified -= ProcedureAsset_OnModified;
            }
        }

        private void OnFocus()
        {
            if (m_procedureAsset)
            {
                RefreshUploadData();
            }
        }

        private void OnDestroy()
        {
            sceneAssetPath = "";
            bundleName = "";
            procedurePath = "";
            additiveScenesPathList = null;
            stepsCounter = 0;
        }


        public void OnBeforeSerialize()
        {
            CopyAssetPath = sceneAssetPath;
            CopyBundleName = bundleName;
            CopyProcedurePath = procedurePath;
            CopyStepsCounter = stepsCounter;
            CopyServerURL = ServerURL;

            CopyAddittiveScenes = additiveScenesPathList;
            CopyJsonContent = m_jsonContent;
        }

        public void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(CopyServerURL))
            {
                ServerURL = CopyServerURL;
            }
            else
            {
                ServerURL = "-Insert URL-";
            }

            if (!string.IsNullOrEmpty(CopyJsonContent))
            {
                sceneAssetPath = CopyAssetPath;
                bundleName = CopyBundleName;
                procedurePath = CopyProcedurePath;
                stepsCounter = CopyStepsCounter;
                additiveScenesPathList = CopyAddittiveScenes;

                //Debug.Log(CopyJsonContent);
                var uploadedProcedure = JsonConvert.DeserializeObject<UploadedProcedure>(CopyJsonContent);
                procedureEntity = uploadedProcedure?.Procedure;
                additiveScenes = uploadedProcedure?.AdditiveScenes?.ToList();
                m_jsonContent = CopyJsonContent;
            }
        }

        private void ResetUploadData()
        {
            
        }

        private void RefreshUploadData()
        {
            if (m_needsRefresh)
            {
                m_needsRefresh = false;
            }

            string[] providers = new string[] { "Standard" };

            sceneAssetPath = m_procedureAsset.ScenePath;

            List<IProcedureStep> steps = m_procedureAsset.Graph.Nodes.Where(n => n is IProcedureStep).Select(n => n as IProcedureStep).Distinct(new IProcedureStepComparer()).ToList();
            Guid sceneGuid = Guid.Empty;
            try
            {
                if (m_procedureAsset && m_procedureAsset.Graph && m_procedureAsset.Graph.ReferencesTable)
                {
                    m_procedureAsset.Graph.ReferencesTable.SceneData?.ValidateSceneData();
                }

                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(AssetDatabase.LoadAssetAtPath<SceneAsset>(m_procedureAsset.ScenePath), out string sceneGuidString, out long fileId))
                {
                    sceneGuid = new Guid(sceneGuidString);
                }
            }
            catch (Exception ex)
            {
                WeavrDebug.LogException(nameof(UploadProcedureWindow), ex);
            }

            // --- Procedure
            procedureEntity = new ProcedureEntity
            {
                Id = Guid.Parse(m_procedureAsset.Guid),
                UnityId = Guid.Parse(m_procedureAsset.Guid),
                Name = m_procedureAsset.name,
                SceneId = sceneGuid,
                Configuration = m_procedureAsset.Configuration.ShortName,
                ProcedureSteps = steps.Select(p => new ProcedureStep()
                {
                    UnityId = Guid.Parse(p.StepGUID),
                }),
                ProcedureVersions = new ProcedureVersion[]
                {
                    new ProcedureVersion()
                    {
                        AvailableLanguages = m_procedureAsset.LocalizationTable.Languages.Select(l => l.TwoLettersISOName).ToList(),
                        DefaultLanguage = m_procedureAsset.LocalizationTable.DefaultLanguage.TwoLettersISOName,
                        EditorVersion = Weavr.VERSION,
                        //ImagePreview = ConvertTextureToString(m_procedureAsset.Media.previewImage),
                        Description = m_procedureAsset.Description,
                        ExecutionModes = m_procedureAsset.ExecutionModes.Select(e => e.ModeName),
                        ProcedureVersionPlatforms = new ProcedureVersionPlatform[]
                        {
                            new ProcedureVersionPlatform()
                            {
                                BuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
                                Providers = providers,
                                ProcedureMedias = new ProcedureMedia[0],
                                ProcedureVersionPlatformFile = new ProcedureVersionPlatformFile()
                                {
                                    Src = m_procedureAsset.Guid,
                                }
                            }
                        },
                        ProcedureVersionSteps = steps.Select(s => new ProcedureVersionStep()
                        {
                            ProcedureStepId = new Guid(s.StepGUID),
                            Index = int.TryParse(s.Number, out int result) ? result : 0,
                            Name = s.Title,
                            Description = s.Description
                        }).ToList(),
                    }
                },
            };

            procedurePath = AssetDatabase.GetAssetPath(m_procedureAsset);
            if (File.Exists(procedurePath))
            {
                procedureEntity.CreatedAt = File.GetCreationTimeUtc(procedurePath);
                procedureEntity.UpdatedAt = File.GetLastWriteTimeUtc(procedurePath);
                procedureEntity.GetLastVersion().UpdatedAt = procedureEntity.UpdatedAt;
                procedureEntity.GetLastVersion().CreatedAt = procedureEntity.CreatedAt;
                procedureEntity.GetLastVersion().ProcedureVersionPlatforms.First().UpdatedAt = procedureEntity.UpdatedAt;
                procedureEntity.GetLastVersion().ProcedureVersionPlatforms.First().CreatedAt = procedureEntity.CreatedAt;
            }

            // --- Scenes

            procedureEntity.Scene = CreateSceneEntity(m_procedureAsset.ScenePath, providers);
            additiveScenes = m_procedureAsset.Environment.additiveScenes
                                             .Where(s => !string.IsNullOrEmpty(s))
                                             .Select(s => CreateSceneEntity(s, providers)).ToList();
            additiveScenesPathList = m_procedureAsset.Environment.additiveScenes.ToArray();
            m_sceneLabels = new SceneLabels[] { new SceneLabels(m_procedureAsset.ScenePath) }
                                    .Concat(m_procedureAsset.Environment.additiveScenes.Select(s => new SceneLabels(s))).ToList();
            stepsCounter = m_procedureAsset.Graph.Nodes.Count;

            m_jsonContent = JsonConvert.SerializeObject(new UploadedProcedure() { Procedure = procedureEntity, AdditiveScenes = additiveScenes });

            Repaint();
        }

        private void ProcedureAsset_OnModified(ProcedureObject procedure)
        {
            RefreshUploadData();
        }

        private static string ConvertTextureToString(Texture2D previewImage)
        {
            if(previewImage)
            {
                if ((previewImage.width * previewImage.height) < (640 * 480))
                {
                    return Convert.ToBase64String(previewImage.EncodeToPNG());
                }
                return "exception";
            }
            return null;
        }

        private static SceneEntity CreateSceneEntity(string scenePath, params string[] providers)
        {
            if (string.IsNullOrEmpty(scenePath)) { return null; }

            var guid = AssetDatabase.AssetPathToGUID(scenePath);
            var sceneGuid = Guid.Parse(guid);
            SceneEntity scene = new SceneEntity
            {
                Id = sceneGuid,
                UnityId = sceneGuid,
                Name = scenePath,
                SceneVersions = new SceneVersion[]
                {
                    new SceneVersion()
                    {
                        SceneVersionPlatforms = new SceneVersionPlatform[]
                        {
                            new SceneVersionPlatform()
                            {
                                BuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
                                Providers = providers,
                                SceneVersionPlatformFile = new SceneVersionPlatformFile()
                                {
                                    Src = sceneGuid.ToString(),
                                }
                            }
                        }
                    }
                },
            };

            if (File.Exists(scenePath))
            {
                scene.CreatedAt = File.GetCreationTimeUtc(scenePath);
                scene.UpdatedAt = File.GetLastWriteTimeUtc(scenePath);
            }
            //scene.SceneVersionPlatforms = scene.SceneVersions.SelectMany(s => s.SceneVersionPlatforms).ToArray();
            return scene;
        }

        void OnGUI()
        {
            s_styles.Refresh();
            if (BuildPipeline.isBuildingPlayer)
            {
                m_restoreProcedureView = true;
                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Build in progress...", s_styles.centeredLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                return;
            }

            if (!m_procedureAsset)
            {
                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("NO PROCEDURE SELECTED", s_styles.centeredLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Label("Only procedure which are being open in Procedure Editor can be uploaded");
                if (!ProcedureEditor.Instance)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if(GUILayout.Button("Opne Procedure Editor"))
                    {
                        ProcedureEditor.ShowWindow();
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                return;
            }

            if (m_restoreProcedureView)
            {
                m_restoreProcedureView = false;
                OnAssetBundleBuildingEnd?.Invoke();
            }

            if (m_needsRefresh)
            {
                RefreshUploadData();
            }

            if (!string.IsNullOrEmpty(m_jsonContent))
            {
                m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

                EditorGUILayout.Space();

                GUILayout.BeginVertical(s_styles.groupBox);
                GUILayout.Label("Procedure", s_styles.titleLabel);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Name", s_styles.normalLabel, GUILayout.MinWidth(100));
                GUILayout.Label(procedureEntity.Name, s_styles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Configuration", s_styles.normalLabel, GUILayout.MinWidth(100));
                GUILayout.Label(procedureEntity.Configuration, s_styles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Path", s_styles.normalLabel, GUILayout.MinWidth(100));
                GUILayout.Label(procedurePath, s_styles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Guid", s_styles.normalLabel, GUILayout.MinWidth(100));
                GUILayout.Label(procedureEntity.UnityId.ToString(), s_styles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Step counter:", s_styles.normalLabel, GUILayout.MinWidth(100));
                GUILayout.Label(stepsCounter.ToString(), s_styles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Update date:", s_styles.normalLabel, GUILayout.MinWidth(100));
                GUILayout.Label(procedureEntity.UpdatedAt.ToLocalTime().ToString("G"), s_styles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();


                GUILayout.BeginVertical(s_styles.groupBox);
                GUILayout.Label("Procedure Scene", s_styles.titleLabel);

                DrawSceneLabels(m_sceneLabels[0], false);

                bool assetBundleExists = true;
                if (!File.Exists(Path.Combine(AssetBundlesBuilder.GetCurrentPlatformAssetBundlePath(), $"{procedureEntity.Name}_{procedureEntity.UnityId}.wvr")))
                {
                    assetBundleExists = false;
                }

                GUILayout.Space(15);

                GUILayout.Label("Additive Scenes", s_styles.titleLabel);
                for (int i = 1; i < m_sceneLabels.Count; i++)
                {
                    GUILayout.BeginHorizontal();
                    DrawSceneLabels(m_sceneLabels[i], true);
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();

                GUILayout.BeginVertical(s_styles.groupBox);
                GUILayout.Label("Upload", s_styles.titleLabel);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Platform", s_styles.normalLabel, GUILayout.MinWidth(100));
                GUILayout.Label(EditorUserBuildSettings.activeBuildTarget.ToString(), s_styles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Archive", s_styles.normalLabel, GUILayout.MinWidth(100));
                if (assetBundleExists)
                {
                    GUILayout.Label(Path.Combine(AssetBundlesBuilder.GetCurrentPlatformAssetBundlePath(), $"{procedureEntity.Name}_{procedureEntity.UnityId}.wvr"), s_styles.boldLabel);
                }
                else
                {
                    GUILayout.Label("NOT FOUND", s_styles.boldLabel);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                EditorGUILayout.EndScrollView();

                GUILayout.BeginHorizontal();

                if (assetBundleExists)
                {
                    if (GUILayout.Button(new GUIContent(" Rebuild Asset Bundle ", WeavrStyles.Icons["buildPackageIcon"]), GUILayout.MinHeight(30)))
                    {
                        CreateAssetsBundlesForUpload();
                    }
                }
                else
                {
                    if (GUILayout.Button(new GUIContent(" Build Asset Bundle ", WeavrStyles.Icons["buildPackageIcon"]), GUILayout.MinHeight(30)))
                    {
                        CreateAssetsBundlesForUpload();
                    }
                }
                GUI.enabled = assetBundleExists;
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent(" Open Folder ", WeavrStyles.Icons["openFolderIcon"]), GUILayout.MinHeight(30)))
                {
                    EditorUtility.RevealInFinder(Path.Combine(AssetBundlesBuilder.GetCurrentPlatformAssetBundlePath(), $"{procedureEntity.Name}_{procedureEntity.UnityId}.wvr"));
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent(" Open Browser ", WeavrStyles.Icons["openBrowserIcon"]), GUILayout.MinHeight(30)))
                {
                    OpenUploadBrowserPage();
                }
                GUILayout.EndHorizontal();

            }
        }

        private void DrawSceneLabels(SceneLabels scene, bool drawBox)
        {
            if (drawBox)
            {
                GUILayout.BeginVertical("Box");
            }

            if (scene.isNull)
            {
                GUILayout.Label("NULL SCENE", s_styles.errorLabel);
            }
            else if (scene.isInvalid)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Name", s_styles.normalLabel, GUILayout.MinWidth(100));
                GUILayout.Label(scene.name, s_styles.errorLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Path", s_styles.normalLabel, GUILayout.MinWidth(100));
                GUILayout.Label(scene.path, s_styles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Name", s_styles.normalLabel, GUILayout.MinWidth(100));
                GUILayout.Label(scene.name, s_styles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Path", s_styles.normalLabel, GUILayout.MinWidth(100));
                GUILayout.Label(scene.path, s_styles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Guid", s_styles.normalLabel, GUILayout.MinWidth(100));
                GUILayout.Label(scene.guid, s_styles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            if (drawBox)
            {
                GUILayout.EndVertical();
            }
        }

        private void CreateAssetsBundlesForUpload()
        {
            OnBeforeAssetBundleBuildingStart?.Invoke();
            var folderPath = Path.Combine(AssetBundlesBuilder.GetCurrentPlatformAssetBundlePath(), 
                                          procedureEntity.Name + "_" + procedureEntity.UnityId.ToString());

            LocalizationManager.Current.Table = null;
            ProcedureRunner.Current.CurrentProcedure = null;
            m_assetBundlesNames.Clear();

            try
            {
                DetachProcedureFromScene(ProcedureEditor.Instance.LastProcedure);
                var media = m_procedureAsset.Media.Clone();
                m_procedureAsset.Media.CopyFrom(null);
                var builds = CreateAssetBundles(procedureEntity.Name, procedureEntity.Id, procedurePath, sceneAssetPath, additiveScenesPathList);
                AssetBundlesBuilder.SetCurrentPlatformCorrectBuildSettings();
                AssetBundlesBuilder.BuildCurrentPlatformAssetBundles(builds,
                                                                     true,
                                                                     procedureEntity.Name + "_" + procedureEntity.UnityId.ToString());

                UpdateFilesData(procedureEntity, additiveScenes.Concat(new SceneEntity[] { procedureEntity.Scene }), folderPath);

                UpdateAndRestoreMediaData(media, folderPath);

                AssetBundlesBuilder.WriteJsonMetadataFile(procedureEntity.UnityId.ToString(),
                                                          new UploadedProcedure() { Procedure = procedureEntity, AdditiveScenes = additiveScenes },
                                                          procedureEntity.Name + "_" + procedureEntity.UnityId.ToString());

                AssetBundlesBuilder.ConvertBundleToWEAVRFile($"{procedureEntity.Name}_{procedureEntity.UnityId}",
                                                             $"{procedureEntity.Name}_{procedureEntity.UnityId}.wvr",
                                                             deleteFolder: true);
            }
            catch (Exception ex)
            {
                WeavrDebug.LogException(this, ex);
            }
            finally
            {
                RestoreAssetBundlesNames();
            }
        }

        private void RestoreAssetBundlesNames()
        {
            foreach(var pair in m_assetBundlesNames)
            {
                var path = AssetDatabase.GetAssetPath(pair.Key);
                var importer = !string.IsNullOrEmpty(path) ? AssetImporter.GetAtPath(path) : null;
                if (importer)
                {
                    importer.assetBundleName = pair.Value;
                }
            }
        }

        private void UpdateAndRestoreMediaData(ProcedureAsset.MediaItems media, string folderPath)
        {
            var platformVersion = procedureEntity.ProcedureVersions.FirstOrDefault()?.ProcedureVersionPlatforms.FirstOrDefault();
            List<ProcedureMedia> mediaEntities = new List<ProcedureMedia>(platformVersion.ProcedureMedias);
            if (media.previewImage)
            {
                var previewImagePath = AssetDatabase.GetAssetPath(media.previewImage);
                var previewImageFileName = Path.GetFileName(previewImagePath);
                File.Copy(previewImagePath, Path.Combine(folderPath, previewImageFileName));

                var previewMedia = CreateMediaEntity(previewImagePath,
                    previewImageFileName,
                    media.previewImage.width,
                    media.previewImage.height,
                    "Main preview image of the procedure");
                mediaEntities.Add(previewMedia);

                procedureEntity.ProcedurePreview = previewMedia;
            }

            platformVersion.ProcedureMedias = mediaEntities;
            m_procedureAsset.Media.CopyFrom(media);
        }

        private static ProcedureMedia CreateMediaEntity(
                string sourceFile, 
                string relativeDestFile, 
                int width, 
                int height, 
                string description = null)
        {
            var media = new ProcedureMedia()
            {
                FileExtension = Path.GetExtension(sourceFile),
                Description = description,
                Height = height,
                Width = width,
                Src = relativeDestFile,
                Size = new FileInfo(sourceFile).Length,
                CreatedAt = File.GetCreationTimeUtc(sourceFile),
                UpdatedAt = File.GetLastWriteTimeUtc(sourceFile),
            };

            using (Stream fileStream = new FileStream(sourceFile, FileMode.Open))
            {
                media.MD5 = s_MD5.ComputeHash(fileStream);
            }

            return media;
        }

        private void UpdateFilesData(ProcedureEntity procedure, IEnumerable<SceneEntity> scenes, string folder)
        {
            var procedureFileEntity = procedure.ProcedureVersions?.FirstOrDefault()?.ProcedureVersionPlatforms?.FirstOrDefault()?.ProcedureVersionPlatformFile;
            if(procedureFileEntity != null)
            {
                var procedureFile = Path.Combine(folder, procedure.UnityId.ToString());
                procedureFileEntity.Size = new FileInfo(procedureFile).Length;
                using (Stream fileStream = new FileStream(procedureFile, FileMode.Open))
                {
                    procedureFileEntity.MD5 = s_MD5.ComputeHash(fileStream);
                }
            }

            foreach(var scene in scenes)
            {
                var sceneFileEntity = scene.SceneVersions?.FirstOrDefault()?.SceneVersionPlatforms?.FirstOrDefault()?.SceneVersionPlatformFile;
                if(sceneFileEntity != null)
                {
                    var sceneFile = Path.Combine(folder, scene.UnityId.ToString());
                    sceneFileEntity.Size = new FileInfo(sceneFile).Length;
                    using (Stream fileStream = new FileStream(sceneFile, FileMode.Open))
                    {
                        sceneFileEntity.MD5 = s_MD5.ComputeHash(fileStream);
                    }
                }
            }
        }

        private void DetachProcedureFromScene(ProcedureAsset procedure)
        {
            ProcedureObjectInspector.ResetSelection();

            var visited = new HashSet<Object>();
            // Clear up the procedure
            DetachFromScene(procedure, visited);

            // Clear up the whole file
            var path = AssetDatabase.GetAssetPath(procedure);
            var allObjects = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach(var obj in allObjects)
            {
                DetachFromScene(obj, visited);
            }

            // Clear up the procedure dependencies
            var dependencies = AssetDatabase.GetDependencies(path);
            foreach(var dependencyPath in dependencies)
            {
                var depObjects = AssetDatabase.LoadAllAssetsAtPath(dependencyPath);
                foreach (var obj in depObjects)
                {
                    DetachFromScene(obj, visited);
                }
            }
        }

        private void DetachFromScene(Object node, HashSet<Object> visitedNodes)
        {
            if (!node || visitedNodes.Contains(node)) { return; }

            var path = AssetDatabase.GetAssetPath(node);
            if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(AssetImporter.GetAtPath(path).assetBundleName))
            {
                var importer = AssetImporter.GetAtPath(path);
                if (importer && !string.IsNullOrEmpty(importer.assetBundleName))
                {
                    var dependencies = AssetDatabase.GetAssetBundleDependencies(importer.assetBundleName, true);
                    if(dependencies?.Length > 0)
                    {
                        StringBuilder sb = new StringBuilder($"{path} depends on: ");

                        foreach(var dep in dependencies)
                        {
                            sb.Append('\'').Append(dep).Append('\'').Append(',').Append(' ');
                        }

                        sb.Length -= 2;
                        WeavrDebug.Log(this, sb.ToString());
                    }

                    m_assetBundlesNames[node] = importer.assetBundleName;
                    importer.assetBundleName = null;

                }
            }

            visitedNodes.Add(node);
            var serNode = new SerializedObject(node);
            serNode.Update();
            var iterator = serNode.FindProperty("m_Script");
            if (iterator == null) { return; }
            while (iterator.Next(iterator.propertyType == SerializedPropertyType.Generic))
            {
                if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (iterator.objectReferenceValue is GameObject go)
                    {
                        if (!PrefabUtility.IsPartOfPrefabAsset(go) || go.scene.IsValid() || !EditorUtility.IsPersistent(go))
                        {
                            iterator.objectReferenceValue = null;
                        }
                    }
                    else if (iterator.objectReferenceValue is Component c)
                    {
                        if (!PrefabUtility.IsPartOfPrefabAsset(c.gameObject) || c.gameObject.scene.IsValid() || !EditorUtility.IsPersistent(c.gameObject))
                        {
                            iterator.objectReferenceValue = null;
                        }
                    }
                    else
                    {
                        DetachFromScene(iterator.objectReferenceValue, visitedNodes);
                    }
                }
            }
            serNode.ApplyModifiedProperties();
        }

        private AssetBundleBuild[] CreateAssetBundles(string procedureName, Guid procedureGuid, string procedurePath, string sceneAssetPath, string[] additiveScenesPathList)
        {
            // Needed in order to have the latest dependencies
            AssetDatabase.Refresh();

            var procedureDependencies = FilterDependencies(AssetDatabase.GetDependencies(procedurePath));
            Dictionary<string, AssetBundleBuild> scenesDependencies = new Dictionary<string, AssetBundleBuild>();
            scenesDependencies[sceneAssetPath] = CreateSceneAssetBundle(sceneAssetPath, procedureDependencies);
            foreach (var scenePath in additiveScenesPathList)
            {
                scenesDependencies[scenePath] = CreateSceneAssetBundle(scenePath, procedureDependencies);
            }

            List<AssetBundleBuild> builds = new List<AssetBundleBuild>();
            builds.Add(new AssetBundleBuild()
            {
                assetBundleName = procedureGuid.ToString(),
                assetNames = procedureDependencies,
            });

            builds.AddRange(scenesDependencies.Values);

            return builds.ToArray();
        }

        private AssetBundleBuild CreateSceneAssetBundle(string sceneAssetPath, string[] excludeAssets)
        {
            return new AssetBundleBuild
            {
                assetBundleName = Guid.Parse(AssetDatabase.AssetPathToGUID(sceneAssetPath)).ToString(),
                assetNames = new string[] { sceneAssetPath },// FilterDependencies(AssetDatabase.GetDependencies(sceneAssetPath)).Except(excludeAssets).ToArray(),
            };
        }

        private string[] FilterDependencies(string[] paths)
        {
            return paths.Where(p => IsValidAsset(p)).ToArray();
        }

        private static bool IsValidAsset(string asset)
        {
            return !asset.EndsWith(".cs") && !asset.StartsWith("Package") && !asset.Contains("/Resources/");
        }

        private void OpenUploadBrowserPage()
        {
            UriBuilder baseUri = new UriBuilder(WeavrEditor.Settings.GetValue<string>("WeavrServerURL"));
            string queryToAppend = "path=" + Path.GetFullPath(Path.Combine(AssetBundlesBuilder.GetCurrentPlatformAssetBundlePath(), procedureEntity.Name + "_" + procedureEntity.UnityId.ToString()));
            AddQueryToUrl(queryToAppend, baseUri);
            queryToAppend = "procedureGuid=" + procedureEntity.UnityId.ToString();
            AddQueryToUrl(queryToAppend, baseUri);
            queryToAppend = "sceneGuid=" + procedureEntity.Scene.UnityId.ToString();
            AddQueryToUrl(queryToAppend, baseUri);

            Application.OpenURL(baseUri.Uri.AbsoluteUri);
        }

        private UriBuilder AddQueryToUrl(string query, UriBuilder baseUri)
        {
            if (baseUri.Query != null && baseUri.Query.Length > 1)
                baseUri.Query = baseUri.Query.Substring(1) + "&" + query;
            else
                baseUri.Query = query;

            return baseUri;
        }

        private class IProcedureStepComparer : IEqualityComparer<IProcedureStep>
        {
            public bool Equals(IProcedureStep x, IProcedureStep y)
            {
                if (string.Equals(x.StepGUID, y.StepGUID, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                return false;
            }

            public int GetHashCode(IProcedureStep obj)
            {
                return obj.StepGUID.GetHashCode();
            }
        }

        [Serializable]
        private struct SceneLabels
        {
            public string name;
            public string path;
            public string guid;
            public bool isNull;
            public bool isInvalid;

            public SceneLabels(string scenePath)
            {
                isInvalid = false;
                isNull = false;
                if (string.IsNullOrEmpty(scenePath))
                {
                    isNull = true;
                    name = null;
                    path = null;
                    guid = null;
                }
                else
                {
                    path = scenePath;
                    var obj = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                    if (obj)
                    {
                        name = obj.name;
                        guid = Guid.Parse(AssetDatabase.AssetPathToGUID(path)).ToString();
                    }
                    else
                    {
                        name = "NOT FOUND";
                        guid = null;
                    }
                }
            }
        }

        private static RuntimePlatform ConvertToPlatform(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneOSX: return RuntimePlatform.OSXPlayer;
                case BuildTarget.StandaloneWindows: return RuntimePlatform.WindowsPlayer;
                case BuildTarget.iOS: return RuntimePlatform.IPhonePlayer;
                case BuildTarget.Android: return RuntimePlatform.Android;
                case BuildTarget.StandaloneWindows64: return RuntimePlatform.WindowsPlayer;
                case BuildTarget.WebGL: return RuntimePlatform.WebGLPlayer;
                case BuildTarget.WSAPlayer: return RuntimePlatform.WSAPlayerX86;
                case BuildTarget.StandaloneLinux64: return RuntimePlatform.LinuxPlayer;
                case BuildTarget.PS4: return RuntimePlatform.PS4;
                case BuildTarget.XboxOne: return RuntimePlatform.XboxOne;
                case BuildTarget.tvOS: return RuntimePlatform.tvOS;
                case BuildTarget.Switch: return RuntimePlatform.Switch;
                case BuildTarget.Lumin: return RuntimePlatform.Lumin;
                case BuildTarget.Stadia: return RuntimePlatform.Stadia;
            }

            throw new Exception($"Cannot convert BuildTarget {target} to any known RuntimePlatform. Is this a new type of BuildTarget?");
        }
    }
}