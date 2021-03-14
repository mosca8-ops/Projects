using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TXT.WEAVR.Editor;
using TXT.WEAVR.Speech;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Localization
{
    [CustomPropertyDrawer(typeof(LocalizedTTS), useForChildren: true)]
    public class LocalizedTTSDrawer : LocalizedItemDrawer
    {
        private static GUIContent s_playContent = new GUIContent(@" ▶");
        private static GUIContent s_stopContent = new GUIContent(@"■");
        
        private static TextToSpeechManager TTSManager => TextToSpeechManager.Current;

        private class Styles : BaseStyles
        {
            public GUIStyle text;
            public GUIStyle box;

            protected override void InitializeStyles(bool isProSkin)
            {
                text = new GUIStyle(EditorStyles.textField);
                text.wordWrap = true;

                box = new GUIStyle("Box");
            }
        }

        private static readonly Styles s_style = new Styles();
        private static readonly GUIContent s_content = new GUIContent();
        private static readonly GUIContent s_voiceLabel = new GUIContent("Voice");

        private ITextToSpeechHandler m_lastHandler;
        private Dictionary<string, float> m_heights = new Dictionary<string, float>();

        private Func<SerializedProperty, SerializedProperty, bool> m_updateValueCallback;
        private float m_width = EditorGUIUtility.currentViewWidth;

        protected override void TargetPropertyField(Rect r, SerializedProperty key, SerializedProperty value, GUIContent label, bool isExpanded)
        {
            if(isExpanded)
            {
                DrawVoicePair(r, key, value, label, isExpanded);

                r.height = EditorGUIUtility.singleLineHeight;
                r.x += r.width - 50;
                r.width = 25;

                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(GetText(value)));
                if (GUI.Button(r, s_playContent, EditorStyles.miniButtonLeft))
                {
                    if (IsPlaying())
                    {
                        Stop();
                    }
                    Play(value);
                }
                EditorGUI.EndDisabledGroup();

                r.x += r.width;
                EditorGUI.BeginDisabledGroup(!IsPlaying());
                if (GUI.Button(r, s_stopContent, EditorStyles.miniButtonRight))
                {
                    Stop();
                }
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                DrawVoicePair(r, key, value, label, isExpanded);
            }
        }

        protected override void DrawMainValueWithExpand(ref Rect rect, SerializedProperty property, SerializedProperty key, SerializedProperty value, GUIContent label, float targetHeight)
        {
            float rectWidth = rect.width;
            rect.width = 70;
            property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label);
            rect.x += rect.width;
            rect.width = rectWidth - rect.width;
            rect.height = targetHeight;
            
            DrawVoicePair(rect, key, value, GUIContent.none, false);

            rect.x += rect.width - 50;
            rect.width = 48;
            rect.height = EditorGUIUtility.singleLineHeight;
            GUI.Label(rect, LocalizationManager.Current.CurrentLanguage.Name, EditorStyles.centeredGreyMiniLabel);
            rect.height = targetHeight;
        }

        protected override float GetTargetPropertyHeight(SerializedProperty value)
        {
            s_style.Refresh();
            s_content.text = GetText(value);
            float textHeight = s_style.text.CalcHeight(s_content, m_width);
            m_heights[value.propertyPath] = textHeight;
            return textHeight + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + 2;
        }


        private static string GetVoiceName(SerializedProperty value) => value?.FindPropertyRelative(nameof(TextToSpeechPair.voiceName)).stringValue;
        private static string GetText(SerializedProperty value) => value?.FindPropertyRelative(nameof(TextToSpeechPair.text)).stringValue;
        private static string GetHandler(SerializedProperty value) => value?.FindPropertyRelative(nameof(TextToSpeechPair.handler)).stringValue;
        private static void SetHandler(SerializedProperty value, string handlerType) => value.FindPropertyRelative(nameof(TextToSpeechPair.handler)).stringValue = handlerType;

        private void DrawVoicePair(Rect r, SerializedProperty key, SerializedProperty value, GUIContent label, bool expanded, float voiceWidthOffset = 50)
        {
            if(expanded)
            {
                r.x += 1;
                r.y += 1;
                r.width -= 2;
                r.width -= 2;
                if (Event.current.type == EventType.Repaint)
                {
                    s_style.box.Draw(r, false, false, false, false);
                }
            }

            if (r.width > 1)
            {
                if (label != GUIContent.none)
                {
                    m_width = r.width - EditorGUIUtility.labelWidth + 2;
                }
                else
                {
                    m_width = r.width;
                }
            }

            //base.TargetPropertyField(r, value, label, isExpanded);
            if (!m_heights.TryGetValue(value.propertyPath, out float textHeight))
            {
                s_content.text = GetText(value);
                textHeight = s_style.text.CalcHeight(s_content, m_width);
                m_heights[value.propertyPath] = textHeight;
            }

            var voice = value.FindPropertyRelative(nameof(TextToSpeechPair.voiceName));
            var text = value.FindPropertyRelative(nameof(TextToSpeechPair.text));
            var handler = value.FindPropertyRelative(nameof(TextToSpeechPair.handler));

            if(m_updateValueCallback != null && m_updateValueCallback(handler, voice))
            {
                m_updateValueCallback = null;
            }

            if (string.IsNullOrEmpty(voice.stringValue))
            {
                var lang = CultureInfo.GetCultureInfo(key.stringValue);
                voice.stringValue = TTSManager.Handlers.FirstOrDefault(h => h.CanHandleLanguage(lang))?.GetVoices(lang).FirstOrDefault()?.Name;
            }

            Rect voiceRect = new Rect(r.x, r.y, r.width, EditorGUIUtility.singleLineHeight);
            voiceRect.width -= voiceWidthOffset;
            if (expanded)
            {
                voiceRect = EditorGUI.PrefixLabel(voiceRect, s_voiceLabel);
            }
            if(GUI.Button(voiceRect, voice.stringValue, EditorStyles.popup))
            {
                string voicePropertyPath = voice.propertyPath;
                string handlerPropertyPath = handler.propertyPath;
                var currentKeyLang = CultureInfo.GetCultureInfo(key.stringValue);
                GenericMenu menu = new GenericMenu();

                foreach (var ttsHandler in TTSManager.Handlers)
                {
                    var handlerType = ttsHandler?.GetType().Name;
                    var handlerNiceName = EditorTools.NicifyName(handlerType);

                    if (!ttsHandler.IsAvailable) {
                        menu.AddDisabledItem(new GUIContent($"{handlerNiceName}  Unavailable"));
                        continue; 
                    }

                    var langVoices = ttsHandler.GetVoices(currentKeyLang);
                    if (langVoices.Count() > 0)
                    {
                        menu.AddDisabledItem(new GUIContent($"{handlerNiceName}/Voices in {currentKeyLang?.DisplayName ?? key.stringValue}"));
                        foreach (var langVoice in langVoices)
                        {
                            menu.AddItem(new GUIContent($"{handlerNiceName}/{langVoice.Name} [{langVoice.Gender}, {langVoice.Age}]"),
                                langVoice.Name == voice.stringValue,
                                () => m_updateValueCallback = (h, v) =>
                                {
                                    if (v.propertyPath == voicePropertyPath && h.propertyPath == handlerPropertyPath)
                                    {
                                        h.stringValue = handlerType;
                                        v.stringValue = langVoice.Name;
                                        TTSManager.LastVoices[langVoice.Language.Name] = (handlerType, langVoice.Name);
                                        return true;
                                    }
                                    return false;
                                });
                        }
                        menu.AddSeparator($"{handlerNiceName}/");
                        menu.AddDisabledItem(new GUIContent($"{handlerNiceName}/Other languages"));
                    }

                    foreach (var voiceGroup in ttsHandler.AllVoices.Except(langVoices).GroupBy(v => v.Language))
                    {
                        foreach (var langVoice in voiceGroup)
                        {
                            menu.AddItem(new GUIContent($"{handlerNiceName}/{voiceGroup.Key.DisplayName}/{langVoice.Name} [{langVoice.Language.TwoLetterISOLanguageName.ToUpper()}, {langVoice.Gender}, {langVoice.Age}]"),
                                    langVoice.Name == voice.stringValue,
                                    () => m_updateValueCallback = (h, v) =>
                                    {
                                        if (v.propertyPath == voicePropertyPath)
                                        {
                                            h.stringValue = handlerType;
                                            v.stringValue = langVoice.Name;
                                            TTSManager.LastVoices[langVoice.Language.Name] = (handlerType, langVoice.Name);
                                            return true;
                                        }
                                        return false;
                                    });
                        }
                    }
                }
                menu.DropDown(voiceRect);
            }

            r.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            r.height = textHeight;
            r = EditorGUI.PrefixLabel(r, label);

            if (expanded)
            {
                // Remove the indent part
                r.x -= EditorGUI.indentLevel * 15;
                r.width += EditorGUI.indentLevel * 15;
            }
            text.stringValue = EditorGUI.TextArea(r, text.stringValue, s_style.text);
        }

        protected virtual void Play(SerializedProperty value)
        {
            Stop();
            var handlerType = GetHandler(value);
            var voiceName = GetVoiceName(value);
            var text = GetText(value);
            if(!TTSManager.HandlersTable.TryGetValue(handlerType, out m_lastHandler))
            {
                WeavrDebug.LogError(value.serializedObject.targetObject, $"Unable to find handler of type {handlerType}, playing with default one");
                m_lastHandler = TTSManager.GetCompatibleOrDefaultHandler(voiceName);
            }

            if (m_lastHandler != null)
            {
                m_lastHandler.CurrentVoiceName = voiceName;
                m_lastHandler.SpeakAsync(text);
            }
        }

        protected virtual void Stop()
        {
            if (m_lastHandler != null)
            {
                m_lastHandler.Stop();
                m_lastHandler = null;
            }
        }

        protected virtual bool IsPlaying()
        {
            return m_lastHandler != null && m_lastHandler.State == SpeechState.Speaking;
        }
    }
}
