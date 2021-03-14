using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;


namespace TXT.WEAVR.Utility.Serialization
{
    
    public enum SerializationMode
    {
        SteamVR,
        Generic,
    }

    public interface ITargetSerializer
    {
        string[] SerializedInstances
        {
            get;
            set;
        }

        BuildTarget LastBuildTarget
        {
            get;
            set;
        }

        bool UseEditorJson
        {
            get;
        }

        bool AwakeDone
        {
            get;
            set;
        }

        SerializationMode SerializationMode
        {
            get;
        }

        string[] FilteredFields
        {
            get;
        }
    }


    public static class TargetSerializerExtensions
    {
        private static int GetMaxTargets(SerializationMode iSerializationMode)
        {
            switch (iSerializationMode)
            {
                case SerializationMode.SteamVR:
                    return 2;
                default:
                    return Enum.GetNames(typeof(BuildTarget)).Length;
            }
        }



        private static int GetGenericTargetIndex(BuildTarget iBuildTarget)
        {
            switch (iBuildTarget)
            {
                case BuildTarget.NoTarget: return 0;
                case BuildTarget.StandaloneOSX: return 3;
                case BuildTarget.StandaloneWindows: return 6;
                case BuildTarget.iOS: return 9;
                case BuildTarget.Android: return 12;
                case BuildTarget.StandaloneLinux: return 13;
                case BuildTarget.StandaloneWindows64: return 14;
                case BuildTarget.WebGL: return 15;
                case BuildTarget.WSAPlayer: return 16;
                case BuildTarget.StandaloneLinux64: return 17;
                case BuildTarget.StandaloneLinuxUniversal: return 18;
                case BuildTarget.PS4: return 24;
                case BuildTarget.XboxOne: return 26;
                case BuildTarget.tvOS: return 30;
                case BuildTarget.Switch: return 31;
                case BuildTarget.Lumin: return 32;
                //case BuildTarget.BJM: return 33;
            }
            return 0;
        }

        private static int GetSteamVRTargetIndex(BuildTarget iBuildTarget)
        {
            switch (iBuildTarget)
            {
                case BuildTarget.StandaloneLinux: 
                case BuildTarget.StandaloneWindows64: 
                case BuildTarget.StandaloneLinux64: 
                case BuildTarget.StandaloneLinuxUniversal:
                case BuildTarget.StandaloneOSX:
                case BuildTarget.StandaloneWindows:
                    return 0;
                default:
                    return 1;
            }
        }

        private static int GetTargetIndex(SerializationMode iSerializationMode, BuildTarget iBuildTarget)
        {
            switch (iSerializationMode)
            {
                case SerializationMode.SteamVR:
                    return GetSteamVRTargetIndex(iBuildTarget);
                default:
                    return GetGenericTargetIndex(iBuildTarget);
            }
        }
        //TODO move method
        public static Func<T> GetNonVirtualMethod<T, U>(this U iObject, string iMethodName)
        {
            var method = typeof(T).GetMethod(iMethodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            var ftn = method.MethodHandle.GetFunctionPointer();
            return (Func<T>)Activator.CreateInstance(typeof(Func<T>), iObject, ftn);
        }


        private static bool IsInitialized(this ITargetSerializer iSerializer)
        {
            return iSerializer.SerializedInstances != null && iSerializer.SerializedInstances.Length == GetMaxTargets(iSerializer.SerializationMode);
        }

        public static void InitializeInstances(this ITargetSerializer iSerializer, bool iLog = false)
        {
            if (Application.isPlaying) return;
            if (!iSerializer.AwakeDone)
            {
                int wMaxTargets = GetMaxTargets(iSerializer.SerializationMode);
                if (iSerializer.SerializedInstances == null)
                {
                    iSerializer.SerializedInstances = new string[wMaxTargets];
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
                else if (iSerializer.SerializedInstances.Length < wMaxTargets)
                {
                    string[] wOriginalValues = (string[])iSerializer.SerializedInstances.Clone();
                    iSerializer.SerializedInstances = new string[wMaxTargets];
                    for (int wIdx = 0; wIdx < wOriginalValues.Length; ++wIdx)
                    {
                        iSerializer.SerializedInstances[wIdx] = wOriginalValues[wIdx];
                    }
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
                else if (iSerializer.SerializedInstances.Length > wMaxTargets)
                {
                    iSerializer.SerializedInstances = new string[wMaxTargets];
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
                //if (iLog) Debug.Log("[InitializeInstances] Current build target is " + EditorUserBuildSettings.activeBuildTarget);
                iSerializer.LastBuildTarget = EditorUserBuildSettings.activeBuildTarget;
                iSerializer.RestoreInstance(iSerializer.LastBuildTarget);
                iSerializer.UpdateInstance();
                iSerializer.AwakeDone = true;
            }
        }

        private static void RestoreInstance(this ITargetSerializer iSerializer, BuildTarget iTarget, bool iLog = true)
        {
            int wIdx = GetTargetIndex(iSerializer.SerializationMode, iTarget);
            if (iSerializer.SerializedInstances != null && wIdx < iSerializer.SerializedInstances.Length &&
                !string.IsNullOrEmpty(iSerializer.SerializedInstances[wIdx]))
            {
                if (iSerializer.UseEditorJson)
                {
                    var wEditorObject = new JObject
                    {
                        { "MonoBehaviour", JObject.Parse(iSerializer.SerializedInstances[wIdx]) }
                    };
                    //if (iLog) Debug.Log("Restoring json instance for target " + iTarget + "\r\nContent = " + wEditorObject.ToString());
                    EditorJsonUtility.FromJsonOverwrite(wEditorObject.ToString(), iSerializer);
                }
                else
                {
                    JsonUtility.FromJsonOverwrite(iSerializer.SerializedInstances[wIdx], iSerializer);
                }
            }
        }

        public static void UpdateInstance(this ITargetSerializer iSerializer,  bool iLog = true)
        {
            if (Application.isPlaying) return;
            if (iSerializer.IsInitialized())
            {
                if (iSerializer.LastBuildTarget != EditorUserBuildSettings.activeBuildTarget)
                {
                    //if (iLog) Debug.Log("Last Target " + iSerializer.LastBuildTarget + " not matching current target " + EditorUserBuildSettings.activeBuildTarget);
                    iSerializer.LastBuildTarget = EditorUserBuildSettings.activeBuildTarget;
                    iSerializer.RestoreInstance(iSerializer.LastBuildTarget);
                }
                else
                {
                    
                    int wIdx = GetTargetIndex(iSerializer.SerializationMode, iSerializer.LastBuildTarget);
                    TypeInfo wTypeInfo = iSerializer.GetType().GetTypeInfo();
                    var wDerivedClassFieldDeclarations = wTypeInfo.DeclaredFields;
                    var wDerivedClassPropertiesDeclarations = wTypeInfo.DeclaredProperties;
                    string wPrevInstance = iSerializer.SerializedInstances[wIdx];
                    iSerializer.SerializedInstances[wIdx] = "";
                    JObject wJson;
                    if (iSerializer.UseEditorJson)
                    {
                        wJson = JObject.Parse(EditorJsonUtility.ToJson(iSerializer))["MonoBehaviour"] as JObject;
                    }
                    else
                    {
                        wJson = JObject.Parse(JsonUtility.ToJson(iSerializer));
                    }
                    foreach (var wField in wDerivedClassFieldDeclarations)
                    {
                        wJson.Remove(wField.Name);
                    }
                    foreach (var wProperty in wDerivedClassPropertiesDeclarations)
                    {
                        wJson.Remove(wProperty.Name);
                    }
                    if (iSerializer.FilteredFields != null)
                    {
                        foreach (var wField in iSerializer.FilteredFields)
                        {
                            wJson.Remove(wField);
                        }
                    } 
                    string wNewInstance = wJson.ToString(); 
                    if (wNewInstance != wPrevInstance)
                    {
                        //if (iLog) Debug.Log("Valid BuildTarget updating serialized representation " + wNewInstance);
                        iSerializer.SerializedInstances[wIdx] = wNewInstance;
                        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                    }
                    else
                    {
                        iSerializer.SerializedInstances[wIdx] = wPrevInstance;
                    }
                }
            }

        }
    }

}