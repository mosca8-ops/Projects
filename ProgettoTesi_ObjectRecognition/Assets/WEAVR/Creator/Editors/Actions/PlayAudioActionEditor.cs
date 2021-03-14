using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(PlayAudioClipAction), true)]
    class PlayAudioActionEditor : ActionEditor
    {
        private PlayAudioClipAction m_action;
        private GUIContent m_playContent = new GUIContent(@" ▶");
        private GUIContent m_stopContent = new GUIContent(@" ■");
        
        private AudioSource m_audioSource;
        public AudioSource AudioSource
        {
            get
            {
                if (m_audioSource == null)
                {
                    string key = $"AUDIO_TEST_{GetHashCode()}";
                    GameObject gameObject = GameObject.Find(key);
                    if (gameObject == null)
                    {
                        gameObject = new GameObject(key);
                        gameObject.hideFlags = HideFlags.HideAndDontSave;
                        m_audioSource = gameObject.AddComponent<AudioSource>();
                    }
                    else
                    {
                        m_audioSource = gameObject.GetComponent<AudioSource>();
                    }
                }
                return m_audioSource;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_action = target as PlayAudioClipAction;
        }

        protected override bool HasMiniPreview => m_action.CanPreview();

        protected override float MiniPreviewHeight => EditorGUIUtility.singleLineHeight;

        protected virtual string GetPreviewLabel()
        {
            return m_action.Clip ? m_action.Clip.name : "No Audio";
        }

        protected override void DrawMiniPreview(Rect r)
        {
            if (IsPlaying() && Event.current.type == EventType.Repaint)
            {
                s_baseStyles.miniPreviewProgressBar.Draw(new Rect(r.x + s_baseStyles.miniPreviewProgressBar.margin.left, 
                                                r.y + s_baseStyles.miniPreviewProgressBar.margin.top, 
                                                (r.width - s_baseStyles.miniPreviewProgressBar.margin.horizontal) * (m_audioSource.time / m_action.Clip.length), r.height),
                    false, false, false, false);
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
            if (GUI.Button(r, m_stopContent))
            {
                Stop();
            }
            EditorGUI.EndDisabledGroup();

            if (IsPlaying())
            {
                ProcedureObjectInspector.RepaintFull();
            }
        }

        protected virtual void Play()
        {
            m_audioSource.clip = m_action.Clip;
            m_audioSource.volume = m_action.Volume;
            m_audioSource.Play();
        }

        protected virtual void Stop()
        {
            m_audioSource.Stop();
        }

        protected virtual bool IsPlaying()
        {
            return AudioSource.isPlaying && m_action.Clip;
        }
    }
}
