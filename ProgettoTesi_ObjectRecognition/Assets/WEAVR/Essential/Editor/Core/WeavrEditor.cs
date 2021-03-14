using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TXT.WEAVR.Core;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR
{
    public static class WeavrEditor
    {
        private const string k_tempFolderName = "Temp_Weavr";
        private const string k_resourcesFolderPath = "Essential/Editor/Resources/";

        public const string PATH = "Assets/WEAVR/";
        public const string EDITOR_RESOURCES_PATH = PATH + k_resourcesFolderPath;
        public static string EDITOR_RESOURCES_FULLPATH => Path.Combine(Application.dataPath.Replace("Assets", ""), PATH, k_resourcesFolderPath);

        private static Commands _commands;
        public static Commands Commands {
            get {
                if(_commands == null) {
                    _commands = new Commands();
                }
                return _commands;
            }
        }

        private static StateKeeper s_stateKeeper;
        public static StateKeeper ObjectStates
        {
            get
            {
                if(s_stateKeeper == null)
                {
                    s_stateKeeper = ScriptableObject.CreateInstance<StateKeeper>();
                }
                return s_stateKeeper;
            }
        }

        private static SettingsHandler s_settings;
        public static SettingsHandler Settings
        {
            get
            {
                if(s_settings == null)
                {
                    s_settings = new SettingsHandler(EDITOR_RESOURCES_FULLPATH + "EditorSettings.settings", true);
                }
                return s_settings;
            }
        }

        public static string GetTempFolder() {
            string folderPath = Path.Combine(Application.dataPath, k_tempFolderName);
            if (!Directory.Exists(folderPath)) {
                Directory.CreateDirectory(folderPath);
            }
            return folderPath;
        }

        public static void ClearTempFolder() {
            Directory.Delete(GetTempFolder(), true);
        }

        public static class Clipboard {
            private static HashSet<object> m_objects = new HashSet<object>();

            //internal Clipboard() {
            //    _objects = new HashSet<object>();
            //}

            public static void Copy(params object[] objects) {
                m_objects.Clear();
                GUIUtility.systemCopyBuffer = null;
                Append(objects);
            }

            public static void Copy(IEnumerable objects)
            {
                GUIUtility.systemCopyBuffer = null;
                m_objects.Clear();
                foreach(var item in objects)
                {
                    m_objects.Add(item);
                }
            }

            public static void Append(params object[] objects) {
                GUIUtility.systemCopyBuffer = null;
                for (int i = 0; i < objects.Length; i++) {
                    if (objects[i] != null && !m_objects.Contains(objects[i])) {
                        m_objects.Add(objects[i]);
                    }
                }
            }

            public static void Append(IEnumerable objects)
            {
                GUIUtility.systemCopyBuffer = null;
                foreach (var item in objects)
                {
                    m_objects.Add(item);
                }
            }

            public static bool Contains<T>() {
                if (!string.IsNullOrEmpty(GUIUtility.systemCopyBuffer)) { return false; }
                foreach (var obj in m_objects) {
                    if(obj is T) { return true; }
                }
                return false;
            }

            public static bool Contains(Type type) {
                if (!string.IsNullOrEmpty(GUIUtility.systemCopyBuffer)) { return false; }
                foreach (var obj in m_objects) {
                    if(obj.GetType() == type || obj.GetType().IsSubclassOf(type)) {
                        return true;
                    }
                }
                return false;
            }

            public static object Contains(object obj) {
                if (!string.IsNullOrEmpty(GUIUtility.systemCopyBuffer)) { return false; }
                foreach (var o in m_objects) {
                    if (o.Equals(obj)) {
                        return true;
                    }
                }
                return false;
            }

            public static IEnumerable<T> Paste<T>() {
                var objects = Get<T>();
                Clear();
                if (!string.IsNullOrEmpty(GUIUtility.systemCopyBuffer)) { return new T[0]; }
                return objects;
            }

            public static IEnumerable<object> Paste() {
                var objects = new List<object>(GetAll());
                Clear();
                if (!string.IsNullOrEmpty(GUIUtility.systemCopyBuffer)) { return new object[0]; }
                return objects;
            }

            public static IEnumerable<T> Get<T>() {
                List<T> objects = new List<T>();
                foreach(var obj in m_objects) {
                    if(obj is T) {
                        objects.Add((T)obj);
                    }
                }
                return objects;
            }

            public static IEnumerable<object> GetAll() {
                return m_objects;
            }

            public static void Clear() {
                m_objects.Clear();
            }
        }
    }
}