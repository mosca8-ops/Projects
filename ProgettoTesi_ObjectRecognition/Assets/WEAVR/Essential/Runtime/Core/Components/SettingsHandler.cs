using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Core
{
    [Flags]
    public enum SettingsFlags
    {
        Inherit = 0,
        Editor = 1 << 0,
        Runtime = 1 << 1,
        Visible = 1 << 2,
        Editable = 1 << 3,

        EditableInPlayer = Runtime | Visible | Editable,
        EditableInEditor = Editor | Visible | Editable,
    }

    public class SettingsHandler
    {
        private bool m_autoSave;
        private string m_settingsFilepath;

        private Dictionary<string, SettingsGroup> m_settings = new Dictionary<string, SettingsGroup>();
        private Dictionary<string, List<IWeavrSettingsListener>> m_settingsListeners = new Dictionary<string, List<IWeavrSettingsListener>>();

        public bool AutoSave
        {
            get => m_autoSave;
            set
            {
                if (m_autoSave != value)
                {
                    m_autoSave = value;
                    if (m_autoSave)
                    {
                        WriteSettingsFile();
                    }
                }
            }
        }

        public string SettingsFilePath
        {
            get => m_settingsFilepath;
            set
            {
                if (m_settingsFilepath != value)
                {
                    m_settingsFilepath = value;
                    if (!string.IsNullOrEmpty(m_settingsFilepath))
                    {
                        if (m_autoSave)
                        {
                            WriteSettingsFile();
                        }
                    }
                }
            }
        }
        
        public bool HasKey(string key) => m_settings.ContainsKey(key);

        public SettingsGroup GetGroup(string key)
        {
            if(m_settings.TryGetValue(key, out SettingsGroup group))
            {
                return group;
            }
            return null;
        }

        public IEnumerable<SettingsGroup> GetAllGroups() => m_settings.Values;

        public Setting GetSetting(string key)
        {
            if(TryGetSetting(key, out Setting setting))
            {
                return setting;
            }
            return null;
        }

        public T GetValue<T>(string key, T fallbackValue = default)
        {
            if(TryGetSetting(key, out Setting setting) && setting.Value is T tValue)
            {
                return tValue;
            }
            return fallbackValue;
        }

        public bool SettingExists(string key) => TryGetSetting(key, out _);

        public void SetValue<T>(string key, T value)
        {
            if(TryGetSetting(key, out Setting setting))
            {
                setting.Value = value;
            }
        }

        public SettingsHandler(string settingsPath, bool autoSave)
        {
            m_autoSave = autoSave;
            m_settingsFilepath = settingsPath;
            ReadSettingsFile();
        }

        public void RegisterListener(IWeavrSettingsListener listener, string settingKey)
        {
            string key = settingKey.ToLower();
            if (!m_settingsListeners.TryGetValue(key, out List<IWeavrSettingsListener> listeners))
            {
                listeners = new List<IWeavrSettingsListener>();
                m_settingsListeners[key] = listeners;
            }
            if (!listeners.Contains(listener))
            {
                listeners.Add(listener);
            }
        }

        public void UnregisterListener(IWeavrSettingsListener listener, string settingKey)
        {
            string key = settingKey.ToLower();
            if (m_settingsListeners.TryGetValue(key, out List<IWeavrSettingsListener> listeners))
            {
                listeners.Remove(listener);
            }
        }

        public void UnregisterListenerFromAllKeys(IWeavrSettingsListener listener)
        {
            foreach (var list in m_settingsListeners.Values)
            {
                list.Remove(listener);
            }
        }

        public void RegisterCallback(Action<Setting> callback, string settingKey)
        {
            if(TryGetSetting(settingKey, out Setting setting))
            {
                setting.SettingChanged -= callback;
                setting.SettingChanged += callback;
            }
        }

        public void UnregisterCallback(Action<Setting> callback, string setting)
        {
            if (TryGetSetting(setting, out Setting s))
            {
                s.SettingChanged -= callback;
            }
        }

        public bool TryGetSetting(string name, out Setting setting)
        {
            foreach(var group in m_settings.Values)
            {
                if(group.TryGetSetting(name, out setting))
                {
                    return true;
                }
            }
            setting = null;
            return false;
        }

        public bool TryGetValue<T>(string name, out T value)
        {
            if (TryGetSetting(name, out Setting setting) && setting.Value is T tValue)
            {
                value = tValue;
                return true;
            }
            value = default;
            return false;
        }

        private IEnumerable<Setting> GetAllSettings(string setting)
        {
            return m_settings.Values.SelectMany(g => g.GetAllSettings()).Where(s => s.name == setting);
        }

        public void MergeFrom(SettingsHandler other, bool overwriteExisting)
        {
            MergeFrom(other.m_settings.Values, overwriteExisting);
            if (m_autoSave)
            {
                WriteSettingsFile();
            }
            
        }

        private void ClearHooks()
        {
            foreach (var setting in m_settings.Values.SelectMany(g => g.GetAllSettings()))
            {
                setting.SettingChanged -= NotifySettingChanged;
            }
        }

        private void RefreshHooks()
        {
            foreach(var setting in m_settings.Values.SelectMany(g => g.GetAllSettings()))
            {
                setting.SettingChanged -= NotifySettingChanged;
                setting.SettingChanged += NotifySettingChanged;
            }
        }

        private void NotifySettingChanged(Setting setting)
        {
            if (m_settingsListeners.TryGetValue(setting.name, out List<IWeavrSettingsListener> listeners))
            {
                foreach (var listener in listeners)
                {
                    try
                    {
                        listener.OnSettingChanged(setting.name);
                    }
                    catch (Exception ex)
                    {
                        WeavrDebug.LogException(this, ex);
                    }
                }
            }
        }

        public void MergeFrom(IEnumerable<SettingsGroup> groups, bool overwriteExisting)
        {
            ClearHooks();
            foreach(var g in groups)
            {
                if(m_settings.TryGetValue(g.group, out SettingsGroup existing))
                {
                    existing.Merge(g, overwriteExisting);
                }
                else
                {
                    m_settings[g.group] = g;
                }
            }
            RefreshHooks();
        }

        public void WriteSettingsFile()
        {
            File.WriteAllText(SettingsFilePath, JsonConvert.SerializeObject(m_settings.Values.ToArray(), Formatting.Indented));
        }

        private void ReadSettingsFile(bool clearOldSettings = false)
        {
            if (File.Exists(SettingsFilePath))
            {
                if (clearOldSettings)
                {
                    m_settings.Clear();
                }
                var settingsText = File.ReadAllText(SettingsFilePath);
                if (settingsText.Length > 2)
                {
                    try
                    {
                        var settings = JsonConvert.DeserializeObject<SettingsGroup[]>(settingsText);
                        MergeFrom(settings, true);
                    }
                    catch(Exception ex)
                    {
                        WeavrDebug.LogException($"Settings[{SettingsFilePath}]", ex);
                        WriteSettingsFile();
                    }
                }
            }
            else
            {
                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                WriteSettingsFile();
            }

        }
    }

    public interface ISettingElement
    {
        SettingsFlags Flags { get; }
    }

    [Serializable]
    public class SettingsGroup : ISettingElement
    {
        public string group;
        public SettingsFlags flags;
        public List<Setting> settings = new List<Setting>();
        public List<SettingsGroup> subgroups = new List<SettingsGroup>();

        [JsonIgnore]
        SettingsFlags ISettingElement.Flags => flags;

        public Setting this[string name] => TryGetSetting(name, out Setting value, true) ? value : null;

        public IEnumerable<Setting> GetAllSettings()
        {
            List<Setting> allSettings = new List<Setting>();
            AddSettings(allSettings);
            return allSettings;
        }

        private void AddSettings(List<Setting> allSettings)
        {
            allSettings.AddRange(settings);
            foreach(var g in subgroups)
            {
                g.AddSettings(allSettings);
            }
        }

        public bool TryGetValue(string name, out object value, bool recursive = true)
        {
            if(TryGetSetting(name, out Setting setting, recursive))
            {
                value = setting.Value;
                return true;
            }
            value = null;
            return false;
        }

        public bool TryGetValue<T>(string name, out T value, bool recursive = true)
        {
            if(TryGetValue(name, out object v, recursive) && v is T tvalue)
            {
                value = tvalue;
                return true;
            }
            value = default;
            return false;
        }

        public void SetValue<T>(string name, in T value)
        {
            if(TryGetSetting(name, out Setting setting, true))
            {
                setting.Value = value;
            }
        }

        public bool TryGetSetting(string name, out Setting setting, bool recursive = true)
        {
            foreach (var s in settings)
            {
                if (s.name == name)
                {
                    setting = s;
                    return true;
                }
            }
            if (recursive)
            {
                foreach (var g in subgroups)
                {
                    if (g.TryGetValue(name, out setting, recursive))
                    {
                        return true;
                    }
                }
            }
            setting = null;
            return false;
        }

        public void Merge(SettingsGroup other, bool overwrite)
        {
            flags |= other.flags;
            foreach (var item in other.settings)
            {
                var existing = settings.FirstOrDefault(s => s.name == item.name);
                if(existing == null)
                {
                    settings.Add(item);
                }
                else if(overwrite)
                {
                    existing.Value = item.Value;
                    existing.flags |= item.flags;
                }
            }

            foreach(var subGroup in other.subgroups)
            {
                var existing = subgroups.FirstOrDefault(g => g.group == subGroup.group);
                if(existing == null)
                {
                    subgroups.Add(subGroup);
                }
                else
                {
                    existing.Merge(subGroup, overwrite);
                }
            }
        }
    }

    [Serializable]
    public class Setting : ISettingElement
    {
        public string name;
        public string description;
        public SettingsFlags flags;
        [JsonProperty]
        private object value;

        [JsonIgnore]
        SettingsFlags ISettingElement.Flags => flags;

        [JsonIgnore]
        private Color? color;
        [JsonIgnore]
        private bool m_steady;

        public event Action<Setting> SettingChanged;

        [JsonIgnore]
        public object Value
        {
            get {
                if (m_steady)
                {
                    return color ?? value;
                }
                else
                {
                    m_steady = true;
                    if(value is string s && ColorUtility.TryParseHtmlString(s, out Color c))
                    {
                        color = c;
                        return color;
                    }
                    return value;
                }
            }
            set
            {
                if(!Equals(this.value, value))
                {
                    if (value is Color c)
                    {
                        this.value = ColorUtility.ToHtmlStringRGBA(c).ToLower();
                        color = c;
                    }
                    else
                    {
                        color = null;
                        this.value = value;
                    }
                    SettingChanged?.Invoke(this);
                }
            }
        }

        public static implicit operator Setting((string name, object value, string description, SettingsFlags flags) tuple)
        {
            return new Setting()
            {
                name = tuple.name,
                description = tuple.description,
                flags = tuple.flags,
                Value = tuple.value,
            };
        }
    }
}