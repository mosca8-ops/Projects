using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Amazon;
using Amazon.Polly;
using Amazon.Polly.Model;
using System.Globalization;
using System.Linq;
using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using UnityEditor;

using Object = UnityEngine.Object;
using TXT.WEAVR.Core;
using System.Threading;

namespace TXT.WEAVR.Speech
{
    class AmazonTTSSettings : IWeavrSettingsClient
    {
        public string SettingsHandlerID => "Editor";

        public string SettingsGroup => "Amazon TextToSpeech";

        public IEnumerable<(string key, object value, string description)> SettingsDefaultPairs => new (string, object, string)[]{
            ("AccessKeyId", "AKIAREHAKO2I4V2ELZPD", "First part of the user id"),
            ("SecretAccessKey", "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", "Second part of the user id"),
            ("AWSRegion", RegionEndpoint.EUWest1.SystemName, "The endpoint region"),
        };

        public string SettingsSection => "Amazon TextToSpeech";

        public IEnumerable<ISettingElement> Settings => new Setting[]
        {
            ("AccessKeyId", "AKIAREHAKO2I4V2ELZPD", "First part of the user id", SettingsFlags.EditableInEditor),
            ("SecretAccessKey", "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", "Second part of the user id", SettingsFlags.EditableInEditor),
            ("AWSRegion", RegionEndpoint.EUWest1.SystemName, "The endpoint region", SettingsFlags.EditableInEditor),
        };
    }

    public class AmazonTTS : IDisposable, ITextToSpeechHandler, IWeavrSettingsListener
    {

        #region [  STATIC PART  ]

        private static AmazonTTS s_instance;
        public static AmazonTTS Current
        {
            get
            {
                if(s_instance == null)
                {
                    s_instance = new AmazonTTS();
                }
                return s_instance;
            }
        }

        private static readonly string[] s_fileExtensions = { ".mp3" };

        private static readonly Dictionary<LanguageCode, string> s_languagesCorrespondencies = new Dictionary<LanguageCode, string> {
            { LanguageCode.Arb, "ar" },
            { LanguageCode.TrTR, LanguageCode.TrTR.Value },
            { LanguageCode.SvSE, LanguageCode.SvSE.Value },
            { LanguageCode.RuRU, LanguageCode.RuRU.Value },
            { LanguageCode.RoRO, LanguageCode.RoRO.Value },
            { LanguageCode.PtPT, LanguageCode.PtPT.Value },
            { LanguageCode.PtBR, LanguageCode.PtBR.Value },
            { LanguageCode.PlPL, LanguageCode.PlPL.Value },
            { LanguageCode.NlNL, LanguageCode.NlNL.Value },
            { LanguageCode.NbNO, LanguageCode.NbNO.Value },
            { LanguageCode.KoKR, LanguageCode.KoKR.Value },
            { LanguageCode.JaJP, LanguageCode.JaJP.Value },
            { LanguageCode.ItIT, LanguageCode.ItIT.Value },
            { LanguageCode.IsIS, LanguageCode.IsIS.Value },
            { LanguageCode.HiIN, LanguageCode.HiIN.Value },
            { LanguageCode.EnGB, LanguageCode.EnGB.Value },
            { LanguageCode.FrCA, LanguageCode.FrCA.Value },
            { LanguageCode.EsUS, LanguageCode.EsUS.Value },
            { LanguageCode.EsMX, LanguageCode.EsMX.Value },
            { LanguageCode.EsES, LanguageCode.EsES.Value },
            { LanguageCode.EnUS, LanguageCode.EnUS.Value },
            { LanguageCode.EnIN, LanguageCode.EnIN.Value },
            { LanguageCode.EnGBWLS, LanguageCode.EnGB.Value },
            { LanguageCode.FrFR, LanguageCode.FrFR.Value },
            { LanguageCode.EnAU, LanguageCode.EnAU.Value },
            { LanguageCode.DeDE, LanguageCode.DeDE.Value },
            { LanguageCode.DaDK, LanguageCode.DaDK.Value },
            { LanguageCode.CyGB, LanguageCode.CyGB.Value },
            { LanguageCode.CmnCN, "zh" },
        };

        private static Dictionary<LanguageCode, CultureInfo> s_langConversionTable;
        public static Dictionary<LanguageCode, CultureInfo> LangConversionTable
        {
            get
            {
                if (s_langConversionTable == null)
                {
                    s_langConversionTable = new Dictionary<LanguageCode, CultureInfo>();
                    foreach (var pair in s_languagesCorrespondencies)
                    {
                        s_langConversionTable[pair.Key] = CultureInfo.GetCultureInfo(pair.Value);
                    }
                }
                return s_langConversionTable;
            }
        }

        private static Dictionary<CultureInfo, LanguageCode> s_inverseLangConversionTable;
        public static Dictionary<CultureInfo, LanguageCode> InverseLangConversionTable
        {
            get
            {
                if (s_inverseLangConversionTable == null)
                {
                    s_inverseLangConversionTable = new Dictionary<CultureInfo, LanguageCode>();
                    foreach (var pair in LangConversionTable)
                    {
                        s_inverseLangConversionTable[pair.Value] = pair.Key;
                    }
                }
                return s_inverseLangConversionTable;
            }
        }

        #endregion

        private AudioSource m_audioSource;
        private AudioSource AudioSource
        {
            get
            {
                if (!m_audioSource)
                {
                    m_audioSource = new GameObject("AmazonAudioTest").AddComponent<AudioSource>();
                    m_audioSource.gameObject.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_audioSource;
            }
        }

        private AmazonPollyClient m_pollyClient;
        private AmazonPollyClient PollyClient
        {
            get
            {
                if (m_pollyClient == null)
                {
                    InitializePollyClient();
                }
                return m_pollyClient;
            }
        }

        private int m_settingUpdateId;
        private bool m_voiceRetrievalStarted;
        private List<AmazonVoice> m_voices = new List<AmazonVoice>();
        private Dictionary<CultureInfo, List<AmazonVoice>> m_loadedVoices = new Dictionary<CultureInfo, List<AmazonVoice>>();
        private AmazonVoice m_currentVoice;

        private List<AmazonVoice> Voices
        {
            get
            {
                if ((m_voices == null || m_voices.Count == 0) && IsAvailable)
                {
                    // Nothing, it will be generated automatically
                }
                return m_voices;
            }
        }

        public AmazonTTS()
        {
            InitializePollyClient();
            WeavrEditor.Settings.RegisterListener(this, "AccessKeyId");
            WeavrEditor.Settings.RegisterListener(this, "SecretAccessKey");
            WeavrEditor.Settings.RegisterListener(this, "AWSRegion");
        }

        private void InitializePollyClient()
        {
            string accessKeyId = WeavrEditor.Settings.GetValue("AccessKeyId", string.Empty);
            string secretKey = WeavrEditor.Settings.GetValue("SecretAccessKey", string.Empty);
            string awsRegion = WeavrEditor.Settings.GetValue<string>("AWSRegion");
            if (!string.IsNullOrEmpty(accessKeyId) && !string.IsNullOrEmpty(secretKey))
            {
                try
                {
                    m_pollyClient = new AmazonPollyClient(accessKeyId, secretKey, string.IsNullOrEmpty(awsRegion) ?
                        RegionEndpoint.EUWest1 : (RegionEndpoint.GetBySystemName(awsRegion) ?? RegionEndpoint.EUWest1));
                    RetrieveAllVoices();
                }
                catch(Exception e)
                {
                    WeavrDebug.LogError(this, $"Unable to connect to Amazon Polly: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        private async void RetrieveAllVoices()
        {
            if (m_voiceRetrievalStarted) { return; }
            m_voiceRetrievalStarted = true;

            m_voices.Clear();
            m_loadedVoices.Clear();
            DescribeVoicesResponse response;

            foreach (var key in s_languagesCorrespondencies.Keys)
            {
                response = null;
                if (LangConversionTable.TryGetValue(key, out CultureInfo lang) && !m_loadedVoices.ContainsKey(lang))
                {
                    try
                    {
                        response = await PollyClient.DescribeVoicesAsync(new DescribeVoicesRequest()
                        {
                            LanguageCode = key
                        });
                    }
                    catch(AmazonPollyException ae)
                    {
                        if(ae.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        {
                            WeavrDebug.LogError(this, $"Unable to get voices: AWS credentials might be wrong");
                            m_pollyClient = null;
                            m_currentVoice = null;
                            return;
                        }
                        else
                        {
                            WeavrDebug.LogError(this, $"Unable to get voices: {ae.Message} - {ae.ErrorCode}\n{ae.StatusCode}\n{ae.StackTrace}");
                        }
                    }
                    catch(Exception e)
                    {
                        m_voiceRetrievalStarted = false;
                        WeavrDebug.LogError(this, $"Unable to get voices: {e.Message}\n{e.StackTrace}");
                    }
                }

                if (!m_voiceRetrievalStarted) { return; }

                if (!m_loadedVoices.ContainsKey(lang) && (response != null && response.Voices != null && response.Voices.Count > 0))
                {
                    var newVoices = response.Voices.Select(v => new AmazonVoice(v));
                    m_loadedVoices[lang] = newVoices.ToList();
                    m_voices.AddRange(newVoices);
                }
            }

            if (m_voices.Count == 0)
            {
                m_pollyClient = null;
                m_currentVoice = null;
            }
            else if (m_currentVoice != null && !m_voices.Contains(m_currentVoice))
            {
                m_currentVoice = GetVoices(CultureInfo.CurrentCulture).FirstOrDefault() as AmazonVoice;
            }

            m_voiceRetrievalStarted = false;
        }


        #region [  INTERFACE IMPLEMENTATION  ]

        public bool IsAvailable => Application.internetReachability != NetworkReachability.NotReachable && PollyClient != null && m_voices.Count > 0;

        public SpeechState State { get; private set; }

        public ITextToSpeechVoice CurrentVoice {
            get => m_currentVoice ?? Voices.FirstOrDefault(v => v.Language.TwoLetterISOLanguageName == CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
            set
            {
                if (value is AmazonVoice voice && voice != m_currentVoice)
                {
                    m_currentVoice = voice;
                }
            }
        }

        public string CurrentVoiceName {
            get => m_currentVoice?.Name;
            set => CurrentVoice = Voices?.FirstOrDefault(v => v.Name.ToLowerInvariant() == value.ToLowerInvariant());
        }

        public IEnumerable<CultureInfo> AvailableLanguages => InverseLangConversionTable.Keys;

        public IEnumerable<ITextToSpeechVoice> AllVoices => Voices;
        
        public string[] FileExtensions => s_fileExtensions;

        public bool CanHandleLanguage(CultureInfo language) => InverseLangConversionTable.Keys.Any(k => k.TwoLetterISOLanguageName == language.TwoLetterISOLanguageName);

        public IEnumerable<ITextToSpeechVoice> GetVoices(CultureInfo language)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable || PollyClient == null) { return null; }
            List<AmazonVoice> newVoices = null;
            if (m_loadedVoices.TryGetValue(language, out List<AmazonVoice> founded) && founded.Count > 0)
            {
                return founded;
            }
            else if (TryLoadVoicesForLanguage(language, out newVoices)){
                return newVoices;
            }
            var available = Voices?.Where(v => v.Language.TwoLetterISOLanguageName == language.TwoLetterISOLanguageName);
            if (available.Count() > 0)
            {
                return available;
            }

            var similarCulture = LangConversionTable.Values.FirstOrDefault(v => v.TwoLetterISOLanguageName == language.TwoLetterISOLanguageName);
            return TryLoadVoicesForLanguage(similarCulture, out newVoices) ? newVoices : null;
        }

        private bool TryLoadVoicesForLanguage(CultureInfo language, out List<AmazonVoice> newVoices)
        {
            newVoices = null;
            if (InverseLangConversionTable.TryGetValue(language, out LanguageCode code))
            {
                var response = PollyClient.DescribeVoices(new DescribeVoicesRequest()
                {
                    LanguageCode = code
                });

                newVoices = response.Voices.Select(l => new AmazonVoice(l)).ToList();
                m_loadedVoices[language] = newVoices;
                m_voices.AddRange(newVoices);
            }
            return newVoices != null;
        }

        public string SaveToFile(string text, string filepath)
        {
            return SaveToFile(null, text, filepath);
        }

        public string SaveToFile(string voice, string text, string filepath)
        {
            if (!IsAvailable) { return null; }

            Stop();

            if (!Path.HasExtension(filepath))
            {
                filepath += ".mp3";
            }
            else if (Path.GetExtension(filepath).ToLower() != ".mp3")
            {
                filepath = Path.ChangeExtension(filepath, ".mp3");
            }

            var aVoice = string.IsNullOrEmpty(voice) ? 
                         CurrentVoice as AmazonVoice : 
                         Voices?.FirstOrDefault(v => v.Name == voice) ?? CurrentVoice as AmazonVoice;

            try
            {
                var response = PollyClient.SynthesizeSpeech(new SynthesizeSpeechRequest()
                {
                    LanguageCode = aVoice?.Voice.LanguageCode,
                    OutputFormat = OutputFormat.Mp3,
                    VoiceId = aVoice?.Voice.Id,
                    Text = $"<speak>{text}</speak>",
                    TextType = TextType.Ssml,
                });

                //SaveToAssets(filepath, response.AudioStream);
                using (FileStream fileStream = new FileStream(filepath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    //result.AudioStream.Seek(0, SeekOrigin.Begin);
                    response.AudioStream.CopyTo(fileStream);
                    fileStream.Flush(true);
                }
            }
            catch (AmazonPollyException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    WeavrDebug.LogError(this, $"Unable to synthetize speech: AWS credentials might be wrong");
                    m_pollyClient = null;
                    m_currentVoice = null;
                }
                else
                {
                    WeavrDebug.LogError(this, $"Unable to synthetize speech: {e.Message} - {e.ErrorCode}\n{e.StatusCode}\n{e.StackTrace}");
                }
            }
            catch(IOException e)
            {
                WeavrDebug.LogError(this, $"Unable to save speech: {e.Message}\n{e.StackTrace}");
            }
            catch (Exception e)
            {
                WeavrDebug.LogError(this, $"Unable to synthetize speech: {e.Message}\n{e.StackTrace}");
            }

            return filepath;
        }

        private static void SaveToAssets(string filepath, Stream audioStream)
        {
            filepath = filepath.Replace(Application.dataPath, "Assets/");
            if (!filepath.StartsWith("Assets"))
            {
                filepath = Path.Combine("Assets", filepath);
            }
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(filepath);
            if (clip)
            {
                Object.DestroyImmediate(clip, true);
            }

            using (FileStream fileStream = new FileStream(Path.Combine(Application.dataPath.Replace("Assets", ""), filepath), FileMode.OpenOrCreate, FileAccess.Write))
            {
                //result.AudioStream.Seek(0, SeekOrigin.Begin);
                audioStream.CopyTo(fileStream);
                fileStream.Flush(true);
            }

            AssetDatabase.ImportAsset(filepath);
        }

        private static async Task SaveToAssetsAsync(string filepath, Stream audioStream)
        {
            filepath = filepath.Replace(Application.dataPath, "Assets/");
            if (!filepath.StartsWith("Assets"))
            {
                filepath = Path.Combine("Assets", filepath);
            }
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(filepath);
            if (clip)
            {
                Object.DestroyImmediate(clip, true);
            }

            var dataPath = Application.dataPath.Replace("Assets", "");
            await Task.Run(() =>
            {
                using (FileStream fileStream = new FileStream(Path.Combine(dataPath, filepath), FileMode.Create, FileAccess.Write))
                {
                    //result.AudioStream.Seek(0, SeekOrigin.Begin);
                    audioStream.CopyTo(fileStream);
                    fileStream.Flush(true);
                }
            });

            AssetDatabase.ImportAsset(filepath);
        }

        public void Speak(string text)
        {
            if (!IsAvailable) { return; }

            Stop();

            State = SpeechState.Ready;

            try
            {
                var response = PollyClient.SynthesizeSpeech(new SynthesizeSpeechRequest()
                {
                    LanguageCode = (CurrentVoice as AmazonVoice)?.Voice.LanguageCode,
                    OutputFormat = OutputFormat.Mp3,
                    Text = $"<speak>{text}</speak>",
                    TextType = TextType.Ssml,
                    VoiceId = (CurrentVoice as AmazonVoice)?.Voice.Id
                });

                State = SpeechState.Speaking;

                string filepath = Path.Combine(WeavrEditor.GetTempFolder().Replace(Application.dataPath, "Assets/"), "amazon-temp.mp3");
                if (AudioSource.clip)
                {
                    Object.DestroyImmediate(AudioSource.clip);
                }

                SaveToAssets(filepath, response.AudioStream);
                AudioSource.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(filepath);
                AudioSource.Play();

                var clip = AudioSource.clip;

                Task.Delay((int)(clip.length * 1000f) + 500);
            }
            catch (AmazonPollyException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    WeavrDebug.LogError(this, $"Unable to synthetize speech: AWS credentials might be wrong");
                    m_pollyClient = null;
                    m_currentVoice = null;
                }
                else
                {
                    WeavrDebug.LogError(this, $"Unable to synthetize speech: {e.Message} - {e.ErrorCode}\n{e.StatusCode}\n{e.StackTrace}");
                }
            }
            catch (IOException e)
            {
                WeavrDebug.LogError(this, $"Unable to save speech: {e.Message}\n{e.StackTrace}");
            }
            catch (Exception e)
            {
                WeavrDebug.LogError(this, $"Unable to synthetize speech: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                State = SpeechState.Ready;
                KillAudioSource();
            }
            //EditorCoroutine.RunDelayed(() =>
            //{
            //    if (AudioSource.clip == clip)
            //    {
            //        KillAudioSource();
            //    }
            //    State = SpeechState.Ready;
            //}, clip.length + 1);
        }

        public async void SpeakAsync(string text)
        {
            if (!IsAvailable) { return; }

            Stop();

            State = SpeechState.Ready;

            Stream audioStream = null;

            try
            {
                var response = await PollyClient.SynthesizeSpeechAsync(new SynthesizeSpeechRequest()
                {
                    LanguageCode = (CurrentVoice as AmazonVoice)?.Voice.LanguageCode,
                    OutputFormat = OutputFormat.Mp3,
                    Text = $"<speak>{text}</speak>",
                    TextType = TextType.Ssml,
                    VoiceId = (CurrentVoice as AmazonVoice)?.Voice.Id
                });

                audioStream = response.AudioStream;

                State = SpeechState.Speaking;
            }
            catch (AmazonPollyException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    WeavrDebug.LogError(this, $"Unable to synthetize speech: AWS credentials might be wrong");
                    m_pollyClient = null;
                    m_currentVoice = null;
                }
                else
                {
                    WeavrDebug.LogError(this, $"Unable to synthetize speech: {e.Message} - {e.ErrorCode}\n{e.StatusCode}\n{e.StackTrace}");
                }
            }
            catch (IOException e)
            {
                WeavrDebug.LogError(this, $"Unable to save speech: {e.Message}\n{e.StackTrace}");
            }
            catch (Exception e)
            {
                WeavrDebug.LogError(this, $"Unable to synthetize speech: {e.Message}\n{e.StackTrace}");
            }

            string filepath = Path.Combine(WeavrEditor.GetTempFolder().Replace(Application.dataPath, "Assets/"), "amazon-temp.mp3");
            if (AudioSource.clip)
            {
                Object.DestroyImmediate(AudioSource.clip);
            }

            if(audioStream == null)
            {
                KillAudioSource();
                State = SpeechState.Ready;
                return;
            }

            await SaveToAssetsAsync(filepath, audioStream);
            AudioSource.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(filepath);
            AudioSource.Play();

            var clip = AudioSource.clip;

            await Task.Delay((int)(clip.length * 1000f) + 500);

            if (AudioSource.clip == clip)
            {
                KillAudioSource();
            }
            State = SpeechState.Ready;
        }

        private void KillAudioSource()
        {
            if (m_audioSource)
            {
                if (m_audioSource.clip)
                {
                    try
                    {
                        m_audioSource.Stop();
                        var clip = m_audioSource.clip;
                        m_audioSource.clip = null;
                        var filePath = AssetDatabase.GetAssetPath(clip);
                        Object.DestroyImmediate(clip, true);
                        //AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(clip));
                        File.Delete(Path.Combine(Application.dataPath.Replace("Assets", ""), filePath));
                    }
                    catch
                    {
                        // Empty, catch everything
                    }
                }

                try
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(m_audioSource);
                    }
                    else
                    {
                        Object.DestroyImmediate(m_audioSource);
                    }
                }
                catch
                {
                    // Empty, catch everything
                }
                finally
                {
                    m_audioSource = null;
                }
            }
        }

        public void Stop()
        {
            KillAudioSource();
            State = SpeechState.Ready;
        }

        public void Dispose()
        {
            KillAudioSource();
            WeavrEditor.Settings.UnregisterListenerFromAllKeys(this);
        }

        public async void OnSettingChanged(string settingKey)
        {
            m_voiceRetrievalStarted = false;
            m_pollyClient = null;
            m_currentVoice = null;

            m_settingUpdateId++;
            int currentSettingUpdateToken = m_settingUpdateId;

            await Task.Run(() =>
            {
                Task.Delay(500);
                if (currentSettingUpdateToken == m_settingUpdateId)
                {
                    InitializePollyClient();
                }
            });
        }

        #endregion


        private class AmazonVoice : ITextToSpeechVoice
        {
            public Amazon.Polly.Model.Voice Voice { get; private set; }

            public string Age => "Adult";

            public string Gender => Voice.Gender;

            public string Name => Voice.Name;

            public CultureInfo Language { get; private set; }
            
            public AmazonVoice(Amazon.Polly.Model.Voice voice)
            {
                Voice = voice;
                if(LangConversionTable.TryGetValue(voice.LanguageCode, out CultureInfo lang))
                {
                    Language = lang;
                }
            }
        }
    }
}
