using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TXT.WEAVR.Editor;
using TXT.WEAVR.License;
using TXT.WEAVR.Localization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using TempSettings = System.Collections.Generic.Dictionary<string, object>;

namespace TXT.WEAVR.Core
{


    public class SettingsWindow : EditorWindow
    {
        private Dictionary<SettingsHandler, SettingsWrapper> m_handlers;

        private VisualElement m_settingsContainer;
        private VisualElement m_settingsTogglesContainer;
        private Button m_confirmButton;

        private List<IWeavrSettingsClient> m_settingsClients;
        
        public VisualElement Root => rootVisualElement;

        [MenuItem("WEAVR/Setup/Settings", priority = 100)]
        public static void ShowWindow()
        {
            ShowWizard();
        }

        private static void ShowWizard()
        {
            GetWindow<SettingsWindow>(true, "Settings window");
        }

        private void OnEnable()
        {
            if (!WeavrLE.IsValid())
            {
                DestroyImmediate(this);
                return;
            }

            maxSize = minSize = new Vector2(600, 720);

            var playerStreammingHandler = new SettingsHandler(Path.Combine(Application.streamingAssetsPath, "WEAVR.settings"), true);

            m_handlers = new Dictionary<SettingsHandler, SettingsWrapper>
            {
                { WeavrEditor.Settings, new SettingsWrapper("Editor", WeavrEditor.Settings, SettingsFlags.Editor) },
                { playerStreammingHandler, new SettingsWrapper("Player", playerStreammingHandler, SettingsFlags.Runtime) },
            };

            VisualElement thisWindow = WeavrStyles.CreateFromTemplate("Windows/SettingsWindow");
            Root.Add(thisWindow);
            Root.StretchToParentSize();

            m_settingsClients = new List<IWeavrSettingsClient>();
            var clientsTypes = EditorTools.GetAllImplementations(typeof(IWeavrSettingsClient));
            foreach (var type in clientsTypes)
            {
                try
                {
                    var client = Activator.CreateInstance(type) as IWeavrSettingsClient;
                    m_settingsClients.Add(client);
                    if (client is Object obj)
                    {
                        DestroyImmediate(obj);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Unable to store Settings Client of type '{type.Name}' due to {e.Message}");
                }
            }

            thisWindow.StretchToParentSize();
            thisWindow.Q("window").StretchToParentSize();
            thisWindow.AddStyleSheetPath(EditorGUIUtility.isProSkin ? "Styles/SettingsWindow" : "Styles/SettingsWindow_lite");

            m_confirmButton = thisWindow.Q<Button>("apply-button");
            m_confirmButton.clickable.clicked -= ApplyButton_Clicked;
            m_confirmButton.clickable.clicked += ApplyButton_Clicked;

            var cancelButton = thisWindow.Q<Button>("cancel-button");
            cancelButton.clickable.clicked -= Close;
            cancelButton.clickable.clicked += Close;

            var resetButton = thisWindow.Q<Button>("reset-button");
            resetButton.clickable.clicked -= ResetChanges;
            resetButton.clickable.clicked += ResetChanges;

            m_settingsContainer = thisWindow.Q("settings-container");
            m_settingsTogglesContainer = thisWindow.Q("settings-toggles-container");

            foreach (var handlerPair in m_handlers)
            {
                BuildSettings(handlerPair.Key, handlerPair.Value, m_settingsClients);
            }

            m_settingsTogglesContainer.Q<Toggle>().value = true;
            m_settingsContainer.Add(m_handlers[WeavrEditor.Settings].RootView);
        }

        private void OnDisable()
        {
            if (m_handlers != null)
            {
                bool flushAll = false;
                foreach (var pair in m_handlers)
                {
                    m_settingsContainer.Add(pair.Value.RootView);
                    if (pair.Value.CurrentValues.Count > 0 
                        && EditorUtility.DisplayDialog($"Changes Detected to {pair.Value.Name}", 
                        "There are unsaved changes in settings, do you want to save them?", 
                        "Save Changes", "Cancel"))
                    {
                        flushAll = true;
                        break;
                    }
                    pair.Value.RootView.RemoveFromHierarchy();
                }
                if (flushAll)
                {
                    foreach (var pair in m_handlers)
                    {
                        FlushToSettingsHandler(pair.Key);
                    }
                }
            }
        }

        private void ResetChanges()
        {
            foreach(var pair in m_handlers)
            {
                foreach(var resetCallbackPair in pair.Value.ResetCallbacks)
                {
                    resetCallbackPair.Value?.Invoke();
                }
            }
        }

        private void BuildSettings(SettingsHandler settings, SettingsWrapper wrapper, List<IWeavrSettingsClient> clients)
        {
            var toggle = new Toggle();
            toggle.Q("unity-checkmark")?.RemoveFromHierarchy();
            toggle.text = wrapper.Name;
            toggle.AddToClassList("settings-toggle");
            toggle.RegisterValueChangedCallback(e => EnableSettingsGroup(toggle, e.newValue, wrapper));
            m_settingsTogglesContainer.Add(toggle);

            wrapper.RootView = new VisualElement();
            wrapper.RootView.AddToClassList("settings-main-group");
            foreach(var client in clients)
            {
                wrapper.Merge(client);
            }

            foreach(var group in wrapper.Groups)
            {
                var view = CreateSettingGroup(wrapper, group, wrapper.Flags);
                if(view != null)
                {
                    wrapper.RootView.Add(view);
                }
            }

            settings.MergeFrom(wrapper.Groups, false);
        }

        private void EnableSettingsGroup(Toggle toggle, bool newValue, SettingsWrapper wrapper)
        {
            if (!newValue) { return; }

            m_settingsTogglesContainer.Query<Toggle>().ForEach(t => t.SetValueWithoutNotify(false));
            toggle.SetValueWithoutNotify(true);

            m_settingsContainer.Clear();
            m_settingsContainer.Add(wrapper.RootView);
        }

        private void ApplyButton_Clicked()
        {
            foreach (var handler in m_handlers)
            {
                FlushToSettingsHandler(handler.Key);
            }
            //Close();
        }

        private void FlushToSettingsHandler(SettingsHandler settings)
        {
            try
            {
                bool wasAutosave = settings.AutoSave;
                settings.AutoSave = false;
                foreach (var pair in m_handlers[settings].CurrentValues)
                {
                    try
                    {
                        switch (pair.Value)
                        {
                            case int i:
                                settings.SetValue(pair.Key, i);
                                break;
                            case float i:
                                settings.SetValue(pair.Key, i);
                                break;
                            case bool i:
                                settings.SetValue(pair.Key, i);
                                break;
                            case string i:
                                settings.SetValue(pair.Key, i);
                                break;
                            case Color i:
                                settings.SetValue(pair.Key, i);
                                break;
                            default:
                                settings.SetValue(pair.Key, pair.Value);
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Log($"[SettingsWindow]: Unable to apply value '{pair.Value}' to setting '{pair.Key}' due to {e.Message}");
                    }
                }
                settings.AutoSave = false;
                settings.AutoSave = wasAutosave;

                m_handlers[settings].RootView.Query(className: "setting-line").ForEach(e => e.RemoveFromClassList("changed"));
                m_handlers[settings].CurrentValues.Clear();
            }
            catch (Exception e)
            {
                Debug.Log($"[SettingsWindow]: Unable to apply settings to {settings.SettingsFilePath} due to {e.Message}");
            }
        }

        private VisualElement CreateSettingGroup(SettingsWrapper wrapper, SettingsGroup group, SettingsFlags inheritFlags)
        {
            inheritFlags = group.flags == SettingsFlags.Inherit ? inheritFlags : group.flags;

            if (group.flags != SettingsFlags.Inherit && !group.flags.HasFlag(wrapper.Flags))
            {
                return null;
            }

            var groupView = new VisualElement();
            groupView.AddToClassList("settings-group");
            groupView.Add(new Label(group.group));

            foreach (var setting in group.settings)
            {
                var line = CreateSettingsLine(wrapper, setting, inheritFlags);
                if (line != null)
                {
                    groupView.Add(line);
                }
            }

            foreach(var subgroup in group.subgroups)
            {
                var view = CreateSettingGroup(wrapper, subgroup, inheritFlags);
                if (view != null)
                {
                    groupView.Add(view);
                }
            }

            if (groupView.childCount > 0)
            {
                wrapper.RootView.Add(groupView);
            }

            return groupView;
        }

        private VisualElement CreateSettingsLine(SettingsWrapper wrapper, Setting setting, SettingsFlags inheritFlags)
        {
            if(!setting.flags.HasFlag(wrapper.Flags) || (setting.flags == SettingsFlags.Inherit && !inheritFlags.HasFlag(wrapper.Flags)))
            {
                return null;
            }

            string key = setting.name;
            string description = setting.description;
            object value = setting.Value;

            if(wrapper.VisualLines.TryGetValue(key, out VisualElement line))
            {
                line.RemoveFromHierarchy();
            }
            wrapper.ResetCallbacks.Remove(key);

            line = new VisualElement();
            wrapper.VisualLines[key] = line;

            line.AddToClassList("setting-line");
            var label = new Label(EditorTools.NicifyName(key));
            label.AddToClassList("setting-label");
            if (!string.IsNullOrEmpty(description))
            {
                label.tooltip = description;
            }

            Action resetCallback = null;
            line.Add(label);
            switch (value)
            {
                case int intValue:
                    var control = new IntegerField();
                    control.value = intValue;
                    resetCallback = CreateResetCallback(v => control.SetValueWithoutNotify(v), intValue, line, wrapper, key);
                    control.AddToClassList("setting-control");
                    control.RegisterValueChangedCallback(e => SetValue(line, wrapper, key, e.previousValue, e.newValue));
                    line.Add(control);
                    break;
                case float floatValue:
                    var floatControl = new FloatField();
                    floatControl.value = floatValue;
                    resetCallback = CreateResetCallback(v => floatControl.SetValueWithoutNotify(v), floatValue, line, wrapper, key);
                    floatControl.AddToClassList("setting-control");
                    floatControl.RegisterValueChangedCallback(e => SetValue(line, wrapper, key, e.previousValue, e.newValue));
                    line.Add(floatControl);
                    break;
                case bool boolValue:
                    var boolControl = new Toggle();
                    boolControl.value = boolValue;
                    resetCallback = CreateResetCallback(v => boolControl.SetValueWithoutNotify(v), boolValue, line, wrapper, key);
                    boolControl.AddToClassList("setting-control");
                    boolControl.RegisterValueChangedCallback(e => SetValue(line, wrapper, key, e.previousValue, e.newValue));
                    line.Add(boolControl);
                    break;
                case string stringValue:
                    var textControl = new TextField();
                    textControl.value = stringValue;
                    resetCallback = CreateResetCallback(v => textControl.SetValueWithoutNotify(v), stringValue, line, wrapper, key);
                    textControl.AddToClassList("setting-control");
                    textControl.RegisterValueChangedCallback(e => SetValue(line, wrapper, key, e.previousValue, e.newValue));
                    line.Add(textControl);
                    break;
                case Color colorValue:

                    break;
                default:
                    line.Add(new Label("Undefined"));
                    break;
            }

            if (!wrapper.ResetCallbacks.ContainsKey(key))
            {
                wrapper.ResetCallbacks[key] = resetCallback;
                line.AddManipulator(new ContextualMenuManipulator(e => e.menu.AppendAction("Reset Change",
                                                                  a => resetCallback?.Invoke(),
                                                                  a => line.ClassListContains("changed") ?
                                                                        DropdownMenuAction.Status.Normal :
                                                                        DropdownMenuAction.Status.Disabled)));
            }
            return line;
        }

        private Action CreateResetCallback<T>(Action<T> setter, T defaultValue, VisualElement line, SettingsWrapper wrapper, string key)
        {
            return () =>
            {
                T value = wrapper.GetValue(key, defaultValue);
                wrapper.CurrentValues.Remove(key);
                setter(value);
                line.RemoveFromClassList("changed");
            };
        }

        private void SetValue<T>(VisualElement line, SettingsWrapper wrapper, string key, T oldValue, T newValue)
        {
            wrapper.CurrentValues[key] = newValue;
            line.AddToClassList("changed");
        }

        private class SettingsWrapper
        {
            public TempSettings CurrentValues { get; } = new TempSettings();
            public Dictionary<string, VisualElement> VisualLines { get; } = new Dictionary<string, VisualElement>();
            public SettingsHandler Handler { get; private set; }
            public string Name { get; private set; }
            public Dictionary<string, Action> ResetCallbacks { get; } = new Dictionary<string, Action>();
            public List<SettingsGroup> Groups { get; private set; }
            public SettingsFlags Flags { get; private set; }
            public VisualElement RootView { get; set; }

            public SettingsWrapper(string name, SettingsHandler handler, SettingsFlags flags)
            {
                Name = name;
                Handler = handler;
                Groups = new List<SettingsGroup>(Handler.GetAllGroups());
                Flags = flags;
            }

            public T GetValue<T>(string key, T fallbackValue)
            {
                foreach(var group in Groups)
                {
                    if(group.TryGetValue<T>(key, out T value))
                    {
                        return value;
                    }
                }
                return fallbackValue;
            }

            public void ResetValue(string key)
            {
                if(ResetCallbacks.TryGetValue(key, out Action action))
                {
                    action?.Invoke();
                }
                CurrentValues.Remove(key);
            }

            internal void Merge(IWeavrSettingsClient client)
            {
                var settings = client.Settings;
                var clientGroup = Groups.FirstOrDefault(g => g.group.ToLower() == client.SettingsSection.ToLower())
                                ?? new SettingsGroup()
                                {
                                    flags = settings.Aggregate(SettingsFlags.Inherit, (f, g) => f | g.Flags),
                                    group = client.SettingsSection,
                                };

                if (!Groups.Contains(clientGroup) && clientGroup.flags.HasFlag(Flags))
                {
                    Groups.Add(clientGroup);
                }

                foreach(var item in settings)
                {
                    if(item is SettingsGroup group)
                    {
                        var existingGroup = Groups.FirstOrDefault(g => g.group == group.group);
                        if(existingGroup != null)
                        {
                            existingGroup.Merge(group, false);
                        }
                        else
                        {
                            Groups.Add(group);
                        }
                    }
                    else if(item is Setting setting)
                    {
                        var existing = clientGroup.settings.FirstOrDefault(s => s.name == setting.name);
                        if(existing != null) 
                        {
                            existing.flags |= setting.flags;
                        }
                        else
                        {
                            clientGroup.settings.Add(setting);
                        }
                    }
                }
            }
        }
    }
}
