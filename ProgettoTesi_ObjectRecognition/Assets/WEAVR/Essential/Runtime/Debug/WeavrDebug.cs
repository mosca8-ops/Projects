using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Profiling;

namespace TXT.WEAVR
{

    public class WeavrDebug
    {
        public static string s_logFormat = "[WEAVR Info]:[<b>{0}</b>]> {1}";
        public static string s_errorFormat = "[WEAVR ERROR]:[<b>{0}</b>]> {1}";
        public static string s_warningFormat = "[WEAVR Warning]:[<b>{0}</b>]> {1}";
        public static string s_exceptionFormat = "[WEAVR Exception]:[<b>{0}</b>]> {1}";

        private static Stack<DiagNode> s_diagNodes;
        private static DiagNode s_lastNode;

        private static bool s_trace;
        public static bool Trace
        {
            get => s_trace;
            set
            {
                if (s_trace != value)
                {
                    s_trace = value;
                    if (value && s_diagNodes == null)
                    {
                        Application.quitting -= Application_quitting;
                        Application.quitting += Application_quitting;
                        s_diagNodes = new Stack<DiagNode>();
                    }
                    else if (!value)
                    {
                        DiagNode node = s_lastNode;
                        while (s_diagNodes.Count > 0)
                        {
                            node = s_diagNodes.Pop();
                            StopNode(node);
                        }
                        if (node != null)
                        {
                            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, $"TRACE_{DateTime.Now:YYYYMMDD_hhmm}"),
                                              JsonUtility.ToJson(node, true));
                        }
                        s_diagNodes.Clear();
                    }
                }
            }
        }

        public static void Log(object source, string message)
        {
            Debug.LogFormat(s_logFormat, source is string s ? s : source?.GetType().Name, message);
        }

        public static void LogError(object source, string message)
        {
            Debug.LogErrorFormat(s_errorFormat, source is string s ? s : source?.GetType().Name, message);
        }

        public static void LogWarning(object source, string message)
        {
            Debug.LogWarningFormat(s_warningFormat, source is string s ? s : source?.GetType().Name, message);
        }

        public static void LogException(object source, System.Exception ex)
        {
            if (source is UnityEngine.Object)
            {
                Debug.LogException(ex, source as UnityEngine.Object);
            }
            else
            {
                Debug.LogErrorFormat(s_exceptionFormat, source is string s ? s : source, ex.GetType().Name);
                Debug.LogException(ex);
            }
        }

        public static void LogException(object source, string prependMessage, System.Exception ex)
        {
            if (source is UnityEngine.Object)
            {
                Debug.LogErrorFormat(s_exceptionFormat, source, $"{prependMessage}\n{ex.GetType().Name}: {ex.Message}");
                Debug.LogException(ex, source as UnityEngine.Object);
            }
            else
            {
                Debug.LogErrorFormat(s_exceptionFormat, source is string s ? s : source, $"{prependMessage}\n{ex.GetType().Name}: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BeginSample(in string name)
        {
            Profiler.BeginSample(name);
            if (Trace)
            {
                StartSampling(name);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EndSample()
        {
            Profiler.EndSample();
            if (Trace)
            {
                StopSampling();
            }
        }

        private static void StartSampling(string name)
        {
            var node = new DiagNode()
            {
                children = new List<DiagNode>(),
                name = name,
                stopwatch = new System.Diagnostics.Stopwatch(),

            };

            if (s_diagNodes.Count > 0)
            {
                var parent = s_diagNodes.Peek();
                node.parent = parent;
                parent.children.Add(node);
            }

            s_diagNodes.Push(node);
            node.stopwatch.Start();
        }

        private static void Application_quitting()
        {
            Application.quitting -= Application_quitting;
            Trace = false;
        }

        private static void StopSampling()
        {
            if (s_diagNodes.Count > 0)
            {
                StopNode(s_diagNodes.Pop());
            }
        }

        private static void StopNode(DiagNode node)
        {
            if (node != null)
            {
                s_lastNode = node;
                node.stopwatch.Stop();
                node.executionTime = node.stopwatch.ElapsedMilliseconds / 1000.0;
            }
        }

        [System.Serializable]
        private class DiagNode
        {
            public string name;
            public double executionTime;
            public List<DiagNode> children;
            [System.NonSerialized]
            public System.Diagnostics.Stopwatch stopwatch;
            [System.NonSerialized]
            public DiagNode parent;
        }
    }
}