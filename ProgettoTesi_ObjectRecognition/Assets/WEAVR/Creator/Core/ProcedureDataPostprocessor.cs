using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{
    public class ProcedureDataPostprocessor : AssetPostprocessor
    {
        //private ProcedureDataPostprocessor s_instance;
        
        //public ProcedureDataPostprocessor Current
        //{
        //    get
        //    {
        //        if(s_instance == null)
        //        {
        //            s_instance = new ProcedureDataPostprocessor();
        //        }
        //        return s_instance;
        //    }
        //}

        //private Dictionary<string, List<Action<Object>>> m_callbacks = new Dictionary<string, List<Action<Object>>>();

        //public void RegisterCallback<T>(string assetPath, Action<T> callback) where T : Object
        //{
        //    if(!m_callbacks.TryGetValue(assetPath, out List<Action<Object>> actions))
        //    {
        //        actions = new List<Action<Object>>();
        //        m_callbacks[assetPath] = actions;
        //    }
        //    actions.Add(o => callback?.Invoke(o as T));
        //}

        //public void OnPostprocessAudio(AudioClip clip)
        //{
        //    string path = AssetDatabase.GetAssetPath(clip);
        //    Debug.Log($"Imported Audio: {clip.name}");
        //}
    }
}