namespace TXT.WEAVR.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using UnityEditor;
    using UnityEngine;

    public static class ModulesInitializer
    {
        [UnityEditor.Callbacks.DidReloadScripts]
        [InitializeOnLoadMethod]
        public static void RefreshModulesList()
        {
            HashSet<string> reflectedModules = new HashSet<string>();
            HashSet<string> dependecyModules = new HashSet<string>();
            Dictionary<string, string> modules = new Dictionary<string, string>();

            List<Type> weavrModules = new List<Type>();

            // Load reflected modules//
            GlobalModuleAttribute attr;
            foreach (var type in GetAllTypesInNamespace("TXT.WEAVR"))
            {
                attr = type.GetCustomAttribute<GlobalModuleAttribute>();
                if (attr != null)
                {
                    reflectedModules.Add(attr.ModuleName);
                    foreach (var dep in attr.Dependencies)
                    {
                        dependecyModules.Add(dep);
                    }
                }
                if (type.IsSubclassOf(typeof(WeavrModule)))
                {
                    weavrModules.Add(type);
                }
            }

            // Load field modules
            foreach (var field in typeof(WeavrModules).GetFields())
            {
                if (field.FieldType == typeof(string))
                {
                    modules.Add(field.Name, (field.GetValue(null) ?? "").ToString());
                }
            }

            // Now check if there are new modules
            bool needsRegenerating = false;
            string value = null;
            foreach (var refModule in reflectedModules)
            {
                needsRegenerating |= !modules.TryGetValue(refModule, out value) || value == "";
                modules[refModule] = refModule;
            }

            foreach (var depModule in dependecyModules)
            {
                if (!modules.ContainsKey(depModule))
                {
                    needsRegenerating = true;
                    modules.Add(depModule, "");
                }
            }

            // TODO Fix
            // Regenerate the file 
            //if (needsRegenerating)
            //{
            //    StringBuilder sb = new StringBuilder();
            //    foreach (var keyPair in modules)
            //    {
            //        sb.AppendFormat(_lineFormat, keyPair.Key, keyPair.Value).AppendLine();
            //    }

            //    // Get the file
            //    string filePath = Application.dataPath + "/WEAVR/WeavrModules.cs";
            //    if (File.Exists(filePath))
            //    {
            //        File.Delete(filePath);
            //    }
            //    File.WriteAllText(filePath, string.Format(_modulesFormat, sb.ToString()));
            //    AssetDatabase.Refresh();

            //    Debug.Log("WEAVR: Regenerated WeavrModules");
            //}

            // Check and initialize the modules
            foreach (var type in weavrModules)
            {
                string resourceFolder = Application.dataPath.Replace("/Assets", "/") + EditorTools.GetModulePath(type) + "Resources";
                if (!Directory.Exists(resourceFolder))
                {
                    Directory.CreateDirectory(resourceFolder);
                }
                string modulePath = EditorTools.GetModulePath(type) + "Resources/" + type.Name + "_InitData.asset";
                WeavrModule module = AssetDatabase.LoadAssetAtPath(modulePath, type) as WeavrModule;
                if (module == null && !File.Exists(Application.dataPath.Replace("/Assets", "/") + modulePath))
                {
                    AssetDatabase.CreateAsset(ScriptableObject.CreateInstance(type), modulePath);
                    AssetDatabase.Refresh();
                }
            }
        }

        private static IEnumerable<Type> GetAllTypesInNamespace(string nspace)
        {
            return EditorTools.GetAllAssemblyTypes().Where(t => t.IsClass && t.Namespace == nspace);
        }

        private static readonly string _lineFormat = @"        public static readonly string {0} = ""{1}"";";
        private static readonly string _modulesFormat = @"/* -----[  AUTO-GENERATED  ]-----
* Any changes applied manually to this file will most probably be overwritten.
* 
* Copyright © TXTGroup
*/

namespace TXT.WEAVR
{{
    using UnityEngine;

    public abstract class WeavrModules : ScriptableObject
    {{
{0}
    }}
}}";
    }
}