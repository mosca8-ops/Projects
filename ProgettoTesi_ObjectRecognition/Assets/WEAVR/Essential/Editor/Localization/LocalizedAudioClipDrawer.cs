using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TXT.WEAVR.Common;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Localization
{
    [CustomPropertyDrawer(typeof(LocalizedAudioClip), useForChildren: true)]
    public class LocalizedAudioClipDrawer : LocalizedItemDrawer
    {
        private static GUIContent s_playContent = new GUIContent(@" ▶");
        private static GUIContent s_stopContent = new GUIContent(@"■");

        private static AudioSource s_audioSource;
        public static AudioSource AudioSource
        {
            get
            {
                if (s_audioSource == null)
                {
                    GameObject gameObject = GameObject.Find("LOCALIZED_AUDIO_TEST");
                    if (gameObject == null)
                    {
                        gameObject = new GameObject("LOCALIZED_AUDIO_TEST");
                        gameObject.hideFlags = HideFlags.HideAndDontSave;
                        s_audioSource = gameObject.AddComponent<AudioSource>();
                    }
                    else
                    {
                        s_audioSource = gameObject.GetComponent<AudioSource>();
                    }
                }
                return s_audioSource;
            }
        }

        protected override void TargetPropertyField(Rect r, SerializedProperty key, SerializedProperty value, GUIContent label, bool isExpanded)
        {
            if(isExpanded)
            {
                r.width -= 50;
                base.TargetPropertyField(r, key, value, label, isExpanded);

                r.x += r.width;
                r.width = 25;

                var clip = value.objectReferenceValue as AudioClip;
                EditorGUI.BeginDisabledGroup(!clip);
                if (GUI.Button(r, s_playContent, EditorStyles.miniButtonLeft))
                {
                    if (IsPlaying())
                    {
                        Stop();
                    }
                    AudioSource.clip = clip;
                    s_audioSource.Play();
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
                base.TargetPropertyField(r, key, value, label, isExpanded);
            }
        }

        protected virtual void Stop()
        {
            AudioSource.Stop();
        }

        protected virtual bool IsPlaying()
        {
            return AudioSource.isPlaying;
        }
    }
}
