using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using TXT.WEAVR.Editor;
using TXT.WEAVR.Localization;

namespace TXT.WEAVR.Speech
{
    public class WindowsVoice : IDisposable, ITextToSpeechHandler
    {
        private CultureInfo Language => LocalizationManager.Current.CurrentLanguage.CultureInfo;
        private readonly List<string> _keys = new List<string>() { @"SOFTWARE\Microsoft\Speech\Voices", @"SOFTWARE\Microsoft\Speech_OneCore\Voices" };
        private readonly string[] s_fileExtensions = { ".wav" };

        private int m_volume = 100;
        private int m_rate = 0;

        private static WindowsVoice s_instance;
        public static WindowsVoice Global
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = new WindowsVoice();
                }
                return s_instance;
            }
        }

        private Dictionary<string, string> m_lastSetVoices = new Dictionary<string, string>();
        public Dictionary<string, string> LastSetVoices => m_lastSetVoices;

        private ReadOnlyCollection<Voice> _allVoices;
        /// <summary>
        /// Gets the available voices on the system
        /// </summary>
        public IEnumerable<ITextToSpeechVoice> AllVoices
        {

            get
            {
                if (_allVoices == null)
                {
                    List<Voice> voicesList = new List<Voice>();
                    foreach (var key in _keys)
                    {
                        using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(key + @"\Tokens"))
                        {
                            if (regKey != null)
                            {
                                var names = regKey.GetSubKeyNames();
                                foreach (var name in names)
                                {
                                    using (RegistryKey voiceRegKey = regKey.OpenSubKey(name))
                                    {
                                        if (voiceRegKey != null)
                                        {
                                            using (RegistryKey attributesRegKey = voiceRegKey.OpenSubKey("Attributes"))
                                            {
                                                if (attributesRegKey != null)
                                                {
                                                    var voice = new Voice()
                                                    {
                                                        Age = attributesRegKey.GetValue("Age").ToString(),
                                                        Gender = attributesRegKey.GetValue("Gender").ToString(),
                                                        LanguageId = int.Parse(attributesRegKey.GetValue("Language").ToString(), NumberStyles.HexNumber),
                                                        Name = attributesRegKey.GetValue("Name").ToString(),
                                                        RegKey = Path.GetDirectoryName(attributesRegKey.Name),
                                                    };

                                                    voicesList.Add(voice);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    _allVoices = voicesList.AsReadOnly();
                }

                return _allVoices;
            }
        }

        /// <summary>
        /// Gets the available voices for that language on the system
        /// </summary>
        public ReadOnlyCollection<Voice> Voices => AllVoices.Select(v => v as Voice).Where(v => v.LanguageId == Language.LCID).ToList().AsReadOnly();
        //public ReadOnlyDictionary<string, Voice> VoicesByName => new ReadOnlyDictionary<string, Voice>(AllVoicesByName.Where(v => v.Value.Language == Language.LCID).ToDictionary(v => v.Key, v => v.Value));

        public ReadOnlyCollection<string> VoicesName => Voices.Select(v => v.Name).ToList().AsReadOnly();


        private Voice _currentVoice;
        /// <summary>
        /// Gets the current voice
        /// </summary>
        public ITextToSpeechVoice CurrentVoice
        {
            get
            {
                if (_currentVoice == null && Voices.Count > 0)
                {
                    _currentVoice = Voices[0];
                }
                return _currentVoice;
            }
            set
            {
                if (value is Voice voice && voice != _currentVoice)
                {
                    _currentVoice = voice;
                }
            }
        }

        /// <summary>
        /// Gets the current voice Name
        /// </summary>
        public string CurrentVoiceName
        {
            get
            {
                return CurrentVoice.Name;
            }
            set
            {
                if (value == null)
                {
                    return;
                }
                CurrentVoice = AllVoices.Where(v => v.Name == value).FirstOrDefault();
            }
        }

        public bool IsPlaying => State == SpeechState.Speaking;

        /// <summary>
        /// Saves the the text spoken to the specified filepath as WAVe file
        /// </summary>
        /// <param name="text">THe text to speak</param>
        /// <param name="filepath">The filepath where to save the speech</param>
        public string SaveAsWaveFile(string voice, string text, string filepath)
        {
            if (!filepath.ToLower().EndsWith(".wav"))
            {
                filepath += ".wav";
            }
            if (Path.GetExtension(filepath).ToLower() != ".wav")
            {
                filepath = Path.ChangeExtension(filepath, ".wav");
            }
            SetSynthesizerValues(voice);
            Synthesizer.SpeakToFile(text, filepath);
            return filepath;
        }

        private void SetSynthesizerValues(string voice = null)
        {
            Synthesizer.SetRate(m_rate);
            Synthesizer.SetVolume(m_volume);
            Synthesizer.SetVoice(!string.IsNullOrEmpty(voice) ? voice : CurrentVoiceName);
        }

        /// <summary>
        /// Stops all speech queue
        /// </summary>
        public void Stop()
        {
            Synthesizer.Stop();
        }

        /// <summary>
        /// Speaks the specified text in a synchronous way
        /// </summary>
        /// <param name="text">The text to speak</param>
        public void Speak(string text)
        {
            SetSynthesizerValues();
            Synthesizer.Speak(text);
        }

        /// <summary>
        /// Queues the specified text to be spoken
        /// </summary>
        /// <param name="text">The text to speak</param>
        public void QueueSpeech(string text)
        {
            SetSynthesizerValues();
            Synthesizer.SpeakAsync(text);
        }

        /// <summary>
        /// Speaks the specified text in an asynchronous way
        /// </summary>
        /// <param name="text">The text to speak</param>
        public void SpeakAsync(string text)
        {
            //Synthesizer.Stop();
            SetSynthesizerValues();
            Synthesizer.SpeakAsync(text);
        }

        /// <summary>
        /// Gets the state of the speech
        /// </summary>
        public SpeechState State
        {
            get
            {
                switch (Synthesizer.GetState())
                {
                    case 2:
                        return SpeechState.Speaking;
                    default:
                        return SpeechState.Ready;
                }
            }
        }

        /// <summary>
        /// Gets or sets the rate (velocity) at which this speech is progressing
        /// </summary>
        public int Rate
        {
            get
            {
                return m_rate;
            }
            set
            {
                if (value >= -10 && value <= 10)
                {
                    m_rate = value;
                }
                else if (value < -10)
                {
                    m_rate = -10;
                }
                else
                {
                    m_rate = 10;
                }
            }
        }

        /// <summary>
        /// Gets or sets the volume of the speech
        /// </summary>
        public int Volume
        {
            get
            {
                return m_volume;
            }
            set
            {
                if (value >= 0 && value <= 100)
                {
                    m_volume = value;
                }
                else if (value < 0)
                {
                    m_volume = 0;
                }
                else
                {
                    m_volume = 100;
                }
            }
        }

        public bool IsAvailable => true;

        public IEnumerable<CultureInfo> AvailableLanguages => throw new NotImplementedException();

        public string[] FileExtensions => s_fileExtensions;

        public void Dispose()
        {
            Synthesizer.DestroySpeech();
        }

        public IEnumerable<ITextToSpeechVoice> GetVoices(CultureInfo language) => AllVoices.Where(v => v.Language.TwoLetterISOLanguageName == language.TwoLetterISOLanguageName);

        public bool CanHandleLanguage(CultureInfo language) => AllVoices.Any(v => v.Language.TwoLetterISOLanguageName == language.TwoLetterISOLanguageName);

        public string SaveToFile(string text, string filepath)
        {
            return SaveAsWaveFile(null, text, filepath);
        }

        public string SaveToFile(string voice, string text, string filepath)
        {
            return SaveAsWaveFile(voice, text, filepath);
        }

        ~WindowsVoice()
        {
            Synthesizer.DestroySpeech();
        }

        private static class Synthesizer
        {
            [DllImport("WindowsVoice")]
            public static extern void DestroySpeech();
            [DllImport("WindowsVoice")]
            public static extern void AddToSpeechQueue(string s);
            [DllImport("WindowsVoice")]
            public static extern void Speak(string s);
            [DllImport("WindowsVoice")]
            public static extern void SpeakAsync(string s);
            [DllImport("WindowsVoice")]
            public static extern void Stop();
            [DllImport("WindowsVoice")]
            public static extern int GetState();
            [DllImport("WindowsVoice")]
            public static extern void SetVolume(int volume);
            [DllImport("WindowsVoice")]
            public static extern int GetVolume();
            [DllImport("WindowsVoice")]
            public static extern void SetRate(int volume);
            [DllImport("WindowsVoice")]
            public static extern int GetRate();
            [DllImport("WindowsVoice")]
            public static extern void SpeakToFile(string text, string filepath);
            [DllImport("WindowsVoice")]
            public static extern void SetVoice(string name);
            //[DllImport("WindowsVoice")]
            //public static extern void GetVoice(int index, StringBuilder stringBuilder);
            //[DllImport("WindowsVoice")]
            //public static extern int GetVoicesCount();
            //[DllImport("WindowsVoice")]
            //public static extern int GetVoicesCountByLanguage(int language);
            //[DllImport("WindowsVoice")]
            //public static extern void SetVoiceByIndex(int voiceIndex);
        }
    }

    public class Voice : ITextToSpeechVoice
    {
        private CultureInfo m_cultureInfo;
        private int m_languageId;

        public string Age { get; set; }
        public string Gender { get; set; }
        public string Name { get; set; }
        public string RegKey { get; set; }

        public int LanguageId
        {
            get => m_languageId;
            set
            {
                if (m_languageId != value)
                {
                    m_languageId = value;
                    m_cultureInfo = null;
                }
            }
        }

        public CultureInfo Language
        {
            get
            {
                if (m_cultureInfo == null)
                {
                    m_cultureInfo = CultureInfo.GetCultureInfo(LanguageId);
                }
                return m_cultureInfo;
            }
        }
    }
}