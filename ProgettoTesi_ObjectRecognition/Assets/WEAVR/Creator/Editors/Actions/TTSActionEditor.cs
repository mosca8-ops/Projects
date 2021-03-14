using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TXT.WEAVR.Localization;
using TXT.WEAVR.Speech;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(TextToSpeechAction), true)]
    class TTSActionEditor : ActionEditor, IAssetImporter, ISmartCreatedCallback
    {
        private readonly static MD5 md5 = MD5.Create();

        private const float k_ProgressBarWidth = 60;
        private const float k_ProgressBarAdv = 2f;

        private static TextToSpeechManager TTSManager => TextToSpeechManager.Current;

        private ITextToSpeechHandler m_lastHandler;
        private TextToSpeechAction m_ttsAction;
        private GUIContent m_playContent = new GUIContent(@" ▶");
        private GUIContent m_stopContent = new GUIContent(@"■");

        private float m_xOffset = 0;
        protected override bool HasMiniPreview => !string.IsNullOrEmpty(m_ttsAction.Text);

        protected override float MiniPreviewHeight => EditorGUIUtility.singleLineHeight;
        
        private LocalizedTTS m_checkpointTTS;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_ttsAction = target as TextToSpeechAction;
            if (m_ttsAction)
            {
                serializedObject.Update();
                m_checkpointTTS = m_ttsAction.Speech.Clone();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            var callbacks = new List<Action>();
            if (TryImport(callbacks))
            {
                foreach(var callback in callbacks)
                {
                    callback?.Invoke();
                }
            }
        }

        protected string GetAudioClipsFolder()
        {
            string folderPath = Path.Combine(m_ttsAction.GetProcedureDataPath(), "TTS AudioClips");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            return folderPath;
        }

        public bool TryImport(List<Action> postImportCallback)
        {
            if (!m_ttsAction || m_checkpointTTS == null) { return false; }

            bool wasChanged = false;
            var currentTTS = m_ttsAction.Speech;
            currentTTS.UpdateLanguages();
            var clip = m_ttsAction.LocalizedClip;
            string folderPath = GetAudioClipsFolder();
            foreach (var pair in m_checkpointTTS.Values)
            {
                //var tts = currentTTS.Values[pair.Key];
                if(pair.Key == null || !currentTTS.Values.TryGetValue(pair.Key, out TextToSpeechPair tts)) { continue; }

                if(pair.Value == null)
                {
                    if (tts == null)
                    {
                        var lang = new CultureInfo(pair.Key);
                        if (lang == null || !TTSManager.Handlers.Any(h => h.IsAvailable && h.CanHandleLanguage(lang)))
                        {
                            WeavrDebug.LogError(m_ttsAction, $"Text-to-Speech currently cannot handle {lang.DisplayName} language.");
                            currentTTS.Values[pair.Key] = new TextToSpeechPair() { handler = "none", voiceName = "none", text = string.Empty };
                        }
                        else
                        {
                            WeavrDebug.LogError(m_ttsAction, $"Corrupted Text-to-Speech for {lang.DisplayName} language. Default values will be used");
                            var handler = TTSManager.Handlers.FirstOrDefault(h => h.IsAvailable && h.CanHandleLanguage(lang));
                            currentTTS.Values[pair.Key] = new TextToSpeechPair()
                            {
                                handler = handler?.GetType().Name,
                                voiceName = handler?.AllVoices.FirstOrDefault(v => v.Language.TwoLetterISOLanguageName == lang.TwoLetterISOLanguageName)?.Name,
                                text = string.Empty
                            };
                        }
                    }
                    continue;
                }

                if (pair.Value.voiceName != tts.voiceName 
                    || pair.Value.text != tts.text 
                    || (!string.IsNullOrEmpty(tts.text) 
                            && (!m_ttsAction.LocalizedClip.Values.TryGetValue(pair.Key, out AudioClip foundClip) || !foundClip)))
                {
                    wasChanged = true;
                    if (!string.IsNullOrEmpty(tts.voiceName) && !string.IsNullOrEmpty(tts.text))
                    {
                        string filename = $"{m_ttsAction.Guid} [{tts.text.GetHashCode()} - {pair.Key} - {tts.voiceName}]";
                        string filePath = Path.Combine(folderPath, filename);

                        if (string.IsNullOrEmpty(tts.handler))
                        {
                            var newHandler = TTSManager.SafeGetHandler(tts.handler, tts.voiceName)?.GetType().Name;
                            WeavrDebug.LogError(this, $"Missing voice handler for TTS action. Default one will be used: {newHandler}");
                            tts.handler = newHandler;
                        }

                        clip.Values[pair.Key] = SaveClip(tts.handler, tts.voiceName, tts.text, filePath);
                        if (clip.Values.TryGetValue(pair.Key, out AudioClip langClip) && langClip && langClip.name != filename)
                        {
                            clip.Values[pair.Key] = null;
                            File.Delete(langClip.GetFullAssetPath());
                            //DestroyImmediate(langClip, true);
                        }
                        else if (!langClip)
                        {
                            postImportCallback.Add(() => clip.Values[pair.Key] = GetAudioClip(filePath));
                        }
                    }
                    else if (clip.Values.TryGetValue(pair.Key, out AudioClip langClip) && langClip)
                    {
                        //DestroyImmediate(langClip, true);
                        clip.Values[pair.Key] = null;
                        File.Delete(langClip.GetFullAssetPath());
                    }
                }
            }

            if (wasChanged)
            {
                m_checkpointTTS = currentTTS.Clone();
            }
            return wasChanged;
        }

        protected override void DrawMiniPreview(Rect r)
        {
            if (IsPlaying() && Event.current.type == EventType.Repaint)
            {
                float realOffset = m_xOffset % (r.width + k_ProgressBarWidth);
                float width = Mathf.Min(k_ProgressBarWidth, realOffset, r.width - realOffset - s_baseStyles.miniPreviewProgressBar.margin.right + k_ProgressBarWidth);
                float x = realOffset > k_ProgressBarWidth ? r.x + s_baseStyles.miniPreviewProgressBar.margin.left + realOffset - k_ProgressBarWidth : r.x;
                s_baseStyles.miniPreviewProgressBar.Draw(new Rect(x,
                                                r.y + s_baseStyles.miniPreviewProgressBar.margin.top,
                                                width, 
                                                r.height),
                    false, false, false, false);
                m_xOffset += k_ProgressBarAdv;
            }
            else
            {
                m_xOffset = 0;
            }

            GUI.Label(r, GetPreviewLabel(), s_baseStyles.miniPreviewLabel);
            r.x += r.width - 50;
            r.width = 25;

            if (GUI.Button(r, m_playContent, EditorStyles.miniButtonLeft))
            {
                if (IsPlaying())
                {
                    Stop();
                }
                Play();
            }

            r.x += r.width;
            EditorGUI.BeginDisabledGroup(!IsPlaying());
            if (GUI.Button(r, m_stopContent, EditorStyles.miniButtonRight))
            {
                Stop();
            }
            EditorGUI.EndDisabledGroup();

            if (IsPlaying())
            {
                ProcedureObjectInspector.RepaintFull();
            }
        }

        protected virtual string GetPreviewLabel()
        {
            return m_ttsAction.Text.Length > 50 ? m_ttsAction.Text.Substring(0, 49) + "..." : m_ttsAction.Text;
        }

        //protected virtual void Play()
        //{
        //    TTSManager.CurrentVoiceName = m_ttsAction.Speech?.CurrentValue.voiceName;
        //    //TTS.Volume = (int)(m_ttsAction.Volume * 100f);
        //    TTSManager.SpeakAsync(m_ttsAction.Text);
        //}

        //protected virtual void Stop()
        //{
        //    TTSManager.Stop();
        //}

        //protected virtual bool IsPlaying()
        //{
        //    return TTSManager.State == SpeechState.Speaking;
        //}

        protected virtual void Play()
        {
            if(m_ttsAction.Speech == null) { return; }

            Stop();

            var tts = m_ttsAction.Speech.CurrentValue;

            if (tts != null && string.IsNullOrEmpty(tts.handler))
            {
                var newHandler = TTSManager.SafeGetHandler(tts.handler, tts.voiceName)?.GetType().Name;
                WeavrDebug.LogError(this, $"Missing voice handler for TTS action. Default one will be used: {newHandler}");
                m_ttsAction.Speech.CurrentValue.handler = newHandler;
            }

            var handlerType = tts?.handler;
            var voiceName = tts?.voiceName;
            var text = tts?.text;

            if (!TTSManager.HandlersTable.TryGetValue(handlerType, out m_lastHandler))
            {
                WeavrDebug.LogError(m_ttsAction, $"Unable to find handler of type {handlerType}, playing with default one");
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

        private AudioClip SaveClip(string handlerType, string voice, string text, string audioPath)
        {
            try
            {
                var handler = TTSManager.HandlersTable[handlerType];
                var createNewOne = true;
                foreach (var extension in handler.FileExtensions)
                {
                    if (File.Exists(audioPath + extension))
                    {
                        createNewOne = false;
                        audioPath += extension;
                        break;
                    }
                }
                if (createNewOne)
                {
                    handler.SaveToFile(voice, text, audioPath);
                    AssetDatabase.ImportAsset(audioPath, ImportAssetOptions.ForceSynchronousImport);
                    //AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }
            }
            catch (Exception e)
            {
                WeavrDebug.LogException(this, e);
            }

            return GetAudioClip(audioPath);
        }

        private static AudioClip GetAudioClip(string audioPath)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath.Replace("\\", "/").Replace(Application.dataPath, "Assets"))
                ?? AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath.Replace("\\", "/").Replace(Application.dataPath, "Assets") + ".wav")
                ?? AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath.Replace("\\", "/").Replace(Application.dataPath, "Assets") + ".mp3")
                ?? AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath.Replace("\\", "/").Replace(Application.dataPath, "Assets") + ".ogg");
        }

        private static string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input

            byte[] hash = md5.ComputeHash(Encoding.ASCII.GetBytes(input));

            // step 2, convert byte array to hex string

            StringBuilder sb = new StringBuilder();
            for (var i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            return sb.ToString();
        }

        public void OnSmartCreated(bool byCloning)
        {
            if (m_ttsAction)
            {
                if (byCloning)
                {
                    foreach (var pair in m_ttsAction.Speech.Values)
                    {
                        pair.Value.text = "";
                    }
                }
                else
                {
                    m_ttsAction.Speech.UpdateLanguages();
                    var otherDictionary = new Dictionary<string, TextToSpeechPair>(m_ttsAction.Speech.Values);
                    foreach (var pair in otherDictionary)
                    {
                        var tts = pair.Value ?? new TextToSpeechPair();
                        if (!byCloning && TTSManager.LastVoices.TryGetValue(pair.Key, out (string handler, string voice) item))
                        {
                            tts.handler = item.handler;
                            tts.voiceName = item.voice;
                        }
                        tts.text = "";
                        if(pair.Value == null)
                        {
                            m_ttsAction.Speech.Values[pair.Key] = tts;
                        }
                    }
                }
            }
        }
    }
}
