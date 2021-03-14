using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Core
{

    public class EditorCoroutine
    {
        private IEnumerator m_currentCoroutine;
        private Stack<IEnumerator> m_coroutineStack;
        private Action m_onFinishCallback;

        private EditorCoroutine(IEnumerator coroutine, Action onFinishedCallback) {
            m_coroutineStack = new Stack<IEnumerator>();
            m_currentCoroutine = coroutine;
            m_onFinishCallback = onFinishedCallback;

            EditorApplication.update += Tick;
        }

        void StopCoroutine() {
            EditorApplication.update -= Tick;
            m_currentCoroutine = null;
            m_coroutineStack.Clear();
        }

        void Tick() {
            if(m_coroutineStack.Count == 0 && m_currentCoroutine == null) {
                EditorApplication.update -= Tick;
                m_onFinishCallback?.Invoke();
                return;
            }
            if(m_currentCoroutine == null) {
                m_currentCoroutine = m_coroutineStack.Pop();
                m_currentCoroutine.MoveNext();
            }
            var currentReturn = m_currentCoroutine.Current;
            if (currentReturn is IEnumerator) {
                if (m_currentCoroutine != currentReturn && (currentReturn as IEnumerator).MoveNext()) {
                    m_coroutineStack.Push(m_currentCoroutine);
                }
                m_currentCoroutine = currentReturn as IEnumerator;
            }
            else if (!m_currentCoroutine.MoveNext()) { 
                if (m_coroutineStack.Count > 0) {
                    m_currentCoroutine = m_coroutineStack.Pop();
                    m_currentCoroutine.MoveNext();
                }
                else {
                    m_currentCoroutine = null;
                }
            }
        }

        //void Tick() {
        //    if (m_coroutineStack.Count == 0 && m_currentCoroutine == null) {
        //        EditorApplication.update -= Tick;
        //        if (m_onFinishCallback != null) {
        //            m_onFinishCallback();
        //        }
        //        return;
        //    }
        //    if (m_currentCoroutine == null) {
        //        m_currentCoroutine = m_coroutineStack.Pop();
        //        m_currentCoroutine.MoveNext();
        //    }
        //    var currentReturn = m_currentCoroutine.Current;
        //    if (currentReturn is IEnumerator) {
        //        m_coroutineStack.Push(m_currentCoroutine);
        //        m_currentCoroutine = currentReturn as IEnumerator;
        //    }
        //    else if (!m_currentCoroutine.MoveNext()) {
        //        if (m_coroutineStack.Count > 0) {
        //            m_currentCoroutine = m_coroutineStack.Pop();
        //            m_currentCoroutine.MoveNext();
        //        }
        //        else {
        //            m_currentCoroutine = null;
        //        }
        //    }
        //}

        private static IEnumerator DelayCoroutine(Action toRun, float time) {
            yield return new WaitForSecondsRealtime(time);
            toRun();
        }

        private static IEnumerator NextFrameCoroutine(Action toRun) {
            yield return new WaitForEndOfFrame();
            toRun();
        }

        public static EditorCoroutine StartCoroutine(IEnumerator coroutine) {
            return new EditorCoroutine(coroutine, null);
        }

        public static EditorCoroutine StartCoroutine(IEnumerator coroutine, Action onFinishCallback) {
            return new EditorCoroutine(coroutine, onFinishCallback);
        }

        public static void RunDelayed(Action toRun, float delay) {
            new EditorCoroutine(DelayCoroutine(toRun, delay), null);
        }

        public static void RunOnNextFrame(Action toRun) {
            new EditorCoroutine(NextFrameCoroutine(toRun), null);
        }

        public static void StopCoroutine(EditorCoroutine coroutine) {
            if(coroutine != null) {
                coroutine.StopCoroutine();
            }
        }
    }
}
