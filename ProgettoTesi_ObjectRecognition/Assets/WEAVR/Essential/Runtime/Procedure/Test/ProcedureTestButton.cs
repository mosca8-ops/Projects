using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Procedure
{
    [AddComponentMenu("")]
    [RequireComponent(typeof(Button))]
    public class ProcedureTestButton : MonoBehaviour
    {
        public Button button;
        public Text text;
        public Image image;

        private Action<bool> m_whenToggle;

        private Sprite m_spriteOn;
        private Sprite m_spriteOff;

        public string Text
        {
            get => text ? text.text : null;
            set { if (text) text.text = value; }
        }

        public void SetupAsButton(string label, Texture2D texture, Action onClick)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
            CreateSprites(texture, null);
            SetSprite(m_spriteOn);
            Text = label;
        }

        public void SetupAsToggle(bool value, string labelOn, Texture2D textureOn, string labelOff, Texture2D textureOff, Action<bool> onChange)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ToggleValue = !ToggleValue);
            m_boolValue = value;
            CreateSprites(textureOn, textureOff);
            m_whenToggle = v =>
            {
                if (v)
                {
                    Text = labelOn;
                    SetSprite(m_spriteOn);
                }
                else
                {
                    Text = labelOff;
                    SetSprite(m_spriteOff);
                }
                onChange?.Invoke(v);
            };
        }

        private bool m_boolValue;
        public bool ToggleValue
        {
            get => m_boolValue;
            set
            {
                if(m_boolValue != value)
                {
                    m_boolValue = value;
                    m_whenToggle?.Invoke(value);
                }
            }
        }

        private void CreateSprites(Texture2D on, Texture2D off)
        {
            if (m_spriteOn){ SmartDestroy(m_spriteOn); }
            if (m_spriteOff) { SmartDestroy(m_spriteOff); }

            if(on) m_spriteOn = Sprite.Create(on, new Rect(0, 0, on.width, on.height), new Vector2(0.5f, 0.5f));
            if(off) m_spriteOff = Sprite.Create(off, new Rect(0, 0, off.width, off.height), new Vector2(0.5f, 0.5f));
        }

        private void SmartDestroy(UnityEngine.Object obj)
        {
            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }

        private void SetSprite(Sprite sprite)
        {
            if (image)
            {
                image.gameObject.SetActive(sprite);
                image.sprite = sprite;
            }
        }
    }
}