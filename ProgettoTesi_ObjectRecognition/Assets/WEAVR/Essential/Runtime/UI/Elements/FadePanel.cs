using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.UI
{

    public class FadePanel : MonoBehaviour, IFadeObject
    {
        public Graphic graphic;

        [AbsoluteValue]
        public float fadeInDuration = 0.5f;
        [Tooltip("Effect when fading into main view")]
        public AnimationCurve easeIn;

        [AbsoluteValue]
        public float fadeOutDuration = 0.5f;
        [Tooltip("Effect when fading out of main view and into this panel")]
        public AnimationCurve easeOut;

        private Coroutine m_fadeCoroutine;

        private void Reset()
        {
            graphic = GetComponentInChildren<Graphic>(true);
            easeIn = AnimationCurve.EaseInOut(0, 1, 1, 0);
            easeOut = AnimationCurve.EaseInOut(0, 0, 1, 1);
        }

        public void FadeIn()
        {
            gameObject.SetActive(true);
            StopFadeCoroutine();
            m_fadeCoroutine = StartCoroutine(FadeTo(easeIn, fadeInDuration, () => gameObject.SetActive(false)));
        }

        public void FadeOut()
        {
            gameObject.SetActive(true);
            StopFadeCoroutine();
            m_fadeCoroutine = StartCoroutine(FadeTo(easeOut, fadeOutDuration, null));
        }

        private void StopFadeCoroutine()
        {
            if(m_fadeCoroutine != null && gameObject.activeInHierarchy)
            {
                StopCoroutine(m_fadeCoroutine);
                m_fadeCoroutine = null;
            }
        }
        
        IEnumerator FadeTo(AnimationCurve curve, float duration, Action onEndCallback)
        {
            float fadeTime = 0;
            var color = graphic.color;
            color.a = curve.Evaluate(0);
            graphic.color = color;
            while (fadeTime < duration)
            {
                color.a = curve.Evaluate(Mathf.Clamp01(fadeTime / duration));
                graphic.color = color;
                fadeTime += Time.deltaTime;
                yield return null;
            }
            color.a = curve.Evaluate(1);
            graphic.color = color;

            m_fadeCoroutine = null;
            onEndCallback?.Invoke();
        }
    }
}
