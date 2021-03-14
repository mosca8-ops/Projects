using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TXT.WEAVR.Speech
{
    public enum SpeechState
    {
        Ready,
        Speaking
    }

    public interface ITextToSpeechHandler
    {
        string[] FileExtensions { get; }
        bool IsAvailable { get; }
        SpeechState State { get; }
        ITextToSpeechVoice CurrentVoice { get; set; }
        string CurrentVoiceName { get; set; }
        IEnumerable<CultureInfo> AvailableLanguages { get; }
        IEnumerable<ITextToSpeechVoice> AllVoices { get; }
        IEnumerable<ITextToSpeechVoice> GetVoices(CultureInfo language);

        bool CanHandleLanguage(CultureInfo language);

        void Speak(string text);
        void SpeakAsync(string text);
        void Stop();

        string SaveToFile(string text, string filepath);
        string SaveToFile(string voice, string text, string filepath);
    }

    public interface ITextToSpeechVoice
    {
        string Age { get; }
        string Gender { get; }
        string Name { get; }
        CultureInfo Language { get; }
    }
}
