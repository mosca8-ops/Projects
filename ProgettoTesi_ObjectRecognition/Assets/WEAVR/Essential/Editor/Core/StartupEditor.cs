namespace TXT.WEAVR.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using TXT.WEAVR.Editor;
    using UnityEditor;
    using UnityEditor.Callbacks;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.Video;

    [InitializeOnLoad]
    public class StartupEditor
    {
        private static bool _initialized = false;
        private const string k_LastTimeKey = "WEAVR_LAST_TIME_SINCE_START_REGISTERED";

        static StartupEditor()
        {
            Initialize();
        }

        [DidReloadScripts]
        private static void Initialize()
        {
            if (!_initialized)
            {
                _initialized = true;
                SetupFirstStart();
                SetupStepActions();
                SetupTypeAliases();
                SetupTypeFilters();

                ModulesInitializer.RefreshModulesList();

                LayerManager.CreateLayer(Weavr.InteractionLayer);
                LayerManager.CreateLayer("VR UI");
                LayerManager.CreateLayer(Weavr.Overlay3DLayer);

                if (!Directory.Exists(Weavr.ProceduresFullPath))
                {
                    Directory.CreateDirectory(Weavr.ProceduresFullPath);
                }
                if (!Directory.Exists(Weavr.ProceduresDataFullPath))
                {
                    Directory.CreateDirectory(Weavr.ProceduresDataFullPath);
                }

                WeavrPackageManager.CheckAndInstallRequiredPackages();
            }
        }

        private static void SetupFirstStart()
        {
            if (EditorApplication.timeSinceStartup < PlayerPrefs.GetFloat(k_LastTimeKey, float.MaxValue))
            {
                FindAndInvokeFirstStartClients();
            }
            PlayerPrefs.SetFloat(k_LastTimeKey, (float)EditorApplication.timeSinceStartup);
            EditorApplication.quitting -= EditorApplication_Quitting;
            EditorApplication.quitting += EditorApplication_Quitting;
        }

        private static void EditorApplication_Quitting()
        {
            PlayerPrefs.DeleteKey(k_LastTimeKey);
        }

        private static void FindAndInvokeFirstStartClients()
        {
            foreach(var pair in EditorTools.GetAttributesWithTypes<InitializeOnEditorStartAttribute>())
            {
                if (string.IsNullOrEmpty(pair.Key.MethodName))
                {
                    // Initialize static constructor
                    try
                    {
                        pair.Value.TypeInitializer?.Invoke(null, null);
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        pair.Value.GetMethod(pair.Key.MethodName, 
                              System.Reflection.BindingFlags.Static 
                            | System.Reflection.BindingFlags.Public 
                            | System.Reflection.BindingFlags.NonPublic)?.Invoke(null, null);
                    }
                    catch { }
                }
            }
        }

        private static void SetupStepActions()
        {

            #region [  LEGACY CODE  ]
            #region [  Normal Actions  ]
            //StepActionsDictionary.Register(typeof(WaitAction),                             typeof(WaitTimeAction),                         Color.black);
            //StepActionsDictionary.Register(typeof(WEAVR.Editor.CameraMoveAction),          typeof(Procedure.CameraMoveAction),             Color.blue);
            //StepActionsDictionary.Register(typeof(WEAVR.Editor.CameraFollowAction),        typeof(Procedure.CameraFollowAction),           Color.blue);
            //StepActionsDictionary.Register(typeof(SoundPlayAction),                        typeof(PlayAudioAction),                        Color.magenta);
            //StepActionsDictionary.Register(typeof(TTSAction),                              typeof(TextToSpeechAction),                     Color.magenta);
            //StepActionsDictionary.Register(typeof(ObjectShowAction),                       typeof(HideShowObject),                         WEAVRStyles.Colors.darkGreen);
            //StepActionsDictionary.Register(typeof(WEAVR.Editor.ObjectMoveAction),          typeof(Procedure.ObjectMoveAction),             WEAVRStyles.Colors.darkGreen);
            //StepActionsDictionary.Register(typeof(InteractionEnablerAction),               typeof(EnableInteractionAction),                WEAVRStyles.Colors.darkGreen);
            //StepActionsDictionary.Register(typeof(WEAVR.Editor.SetValueAction),            typeof(Procedure.SetValueAction),               Color.black);
            //StepActionsDictionary.Register(typeof(WEAVR.Editor.AnimatorParameterAction),   typeof(Procedure.AnimatorParameterAction),      Color.red);
            //StepActionsDictionary.Register(typeof(WEAVR.Editor.ReachTargetAction),         typeof(Procedure.ReachTargetAction),            WEAVRStyles.Colors.darkGreen);
            //StepActionsDictionary.Register(typeof(WEAVR.Editor.FocalPointAction),          typeof(Procedure.FocalPointAction),             WEAVRStyles.Colors.orange);
            //StepActionsDictionary.Register(typeof(WEAVR.Editor.ObjectOutlineAction),       typeof(Procedure.OutlineAction),                WEAVRStyles.Colors.orange);
            //StepActionsDictionary.Register(typeof(WEAVR.Editor.HideFocalPointAction),      typeof(Procedure.HideFocalPointAction),         WEAVRStyles.Colors.orange);

            //StepActionsDictionary.Register(typeof(WEAVR.Editor.CallMethodAction), typeof(Procedure.HideFocalPointAction), WEAVRStyles.Colors.orange);
            //StepActionsDictionary.Register( typeof(ShowPictureAction),                  typeof(Object));
            //StepActionsDictionary.Register( typeof(ShowTextAction),                     typeof(Object));

            #endregion

            #region [  Control Actions  ]
            //StepActionsDictionary.Register(typeof(WEAVR.Editor.StopAllAsyncActions), typeof(Procedure.StopAllAsyncAction), WEAVRStyles.Colors.faintDarkGray, true);
            //StepActionsDictionary.Register(typeof(WEAVR.Editor.WaitAllAsyncActions), typeof(Procedure.WaitAllAsyncAction), WEAVRStyles.Colors.faintDarkGray, true);

            #endregion
            #endregion
        }

        private static void SetupTypeFilters()
        {
            // Get Unity Types engine, and remove those which you want to be added
            var types = new List<Type>(typeof(UnityEngine.Object).Assembly.GetTypes());
            // Physics Module
            types.AddRange(typeof(Collider).Assembly.GetTypes());
            // UI Module
            types.AddRange(typeof(CanvasScaler).Assembly.GetTypes());
            types.AddRange(typeof(Canvas).Assembly.GetTypes());
            // Other Modules
            types.AddRange(typeof(TextGenerator).Assembly.GetTypes());

            // Exclude from filters
            types.Remove(typeof(Transform));
            types.Remove(typeof(Vector3));
            types.Remove(typeof(Vector2));
            types.Remove(typeof(Vector4));
            types.Remove(typeof(Rect));
            types.Remove(typeof(Toggle));
            types.Remove(typeof(Selectable));
            types.Remove(typeof(Button));
            types.Remove(typeof(RectTransform));
            types.Remove(typeof(Image));
            types.Remove(typeof(Text));
            types.Remove(typeof(TextMesh));
            types.Remove(typeof(Color));
            types.Remove(typeof(Graphic));
            types.Remove(typeof(Rigidbody));
            types.Remove(typeof(Renderer));
            types.Remove(typeof(Sprite));
            types.Remove(typeof(Texture2D));
            types.Remove(typeof(Texture));
            types.Remove(typeof(AudioClip));
            types.Remove(typeof(VideoClip));
            types.Remove(typeof(Gradient));
            types.Remove(typeof(Behaviour));
            types.Remove(typeof(GameObject));
            //types.Remove(typeof(MeshRenderer));

            TypesFilters.Add(TypesFilters.FilterType.PropertyHiddenTypes, types.ToArray());
            TypesFilters.Add(TypesFilters.FilterType.PropertyHiddenTypes,
                                     typeof(UniqueID),
                                     typeof(Type), typeof(MeshRenderer));

            TypesFilters.Add(TypesFilters.FilterType.PropertyNotInspectableTypes,
                                     typeof(string),
                                     typeof(Enum));

            // Setup properties to hide
            TypesFilters.HideProperty(typeof(Vector2), "Item");
            TypesFilters.HideProperty(typeof(Vector3), "Item");
            TypesFilters.HideProperty(typeof(Vector4), "Item");
            TypesFilters.HideProperty(typeof(Color), "Item");
            TypesFilters.HideProperty(typeof(PlainList<>), "Item");
            TypesFilters.HideProperty(typeof(Transform), "root");
            TypesFilters.HideProperty(typeof(GameObject), "gameObject");
            //TypesFilters.HideProperty(typeof(Transform), "parent");

            // Add Custom not inspectable and hidden types

        }

        private static void SetupTypeAliases()
        {
            TypesFilters.RegisterTypeAlias(typeof(byte), "byte");
            TypesFilters.RegisterTypeAlias(typeof(sbyte), "sbyte");
            TypesFilters.RegisterTypeAlias(typeof(short), "short");
            TypesFilters.RegisterTypeAlias(typeof(ushort), "ushort");
            TypesFilters.RegisterTypeAlias(typeof(int), "int");
            TypesFilters.RegisterTypeAlias(typeof(uint), "uint");
            TypesFilters.RegisterTypeAlias(typeof(long), "long");
            TypesFilters.RegisterTypeAlias(typeof(ulong), "ulong");
            TypesFilters.RegisterTypeAlias(typeof(float), "float");
            TypesFilters.RegisterTypeAlias(typeof(double), "double");
            TypesFilters.RegisterTypeAlias(typeof(decimal), "decimal");
            TypesFilters.RegisterTypeAlias(typeof(object), "object");
            TypesFilters.RegisterTypeAlias(typeof(bool), "bool");
            TypesFilters.RegisterTypeAlias(typeof(char), "char");
            TypesFilters.RegisterTypeAlias(typeof(string), "string");
            TypesFilters.RegisterTypeAlias(typeof(void), "void");
        }

    }
}