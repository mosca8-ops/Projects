using Unity.Barracuda;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

public class NNHandler : System.IDisposable
{
    public Model model;
    public List<float> anchors;
    public IWorker worker;
    public Dictionary<string, Dictionary<string, object>> cfg;

    public NNHandler(NNModel nnmodel, string configText)
    {
        model = ModelLoader.Load(nnmodel);
        if (WebCamDetector.android)
        {
            worker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, model);
        } 
        else
        {
            worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
        }
        cfg = GetPropertyDictionary(configText);
        anchors = GetAnchors(cfg);
    }
    //new for take all cfg
    private Dictionary<string, Dictionary<string, object>> GetPropertyDictionary(string text)
    {
        Dictionary<string, Dictionary<string, object>> cfg = new Dictionary<string, Dictionary<string, object>>();
       
        string key = "";
        string[] pars = text.Split('[');
        foreach (string par in pars)
        {
            //Debug.Log(par);
            string[] lines = par.Split('\n');
            key = lines[0].Replace(']', ' ');
            key = key.Trim(' ');
            //Debug.Log(key);
            if (!key.Equals(""))
            {
                cfg = GetProperty(key, lines, cfg);
            }
        }
        return cfg;
    }

    private Dictionary<string, Dictionary<string, object>> GetProperty(string key, string[] lines, Dictionary<string, Dictionary<string, object>> cfg)
    {
        Dictionary<string, object> data = new Dictionary<string, object>();
        foreach(string line in lines)
        { 
            if (!line.Contains('#') &&
                !line.Contains(']') &&
                !line.Equals("") &&
                !line.Equals("\r"))
            {
                //Debug.Log(line);
                line.Trim(' ');
                object[] entries = line.Split('=');
                string keys = (string) entries[0];
                string value = (string)entries[1];
                data.Add(keys.Trim(' '), value.Replace('\r', ' ').Trim(' '));
            }
        }
        int count = 0;
        //se esiste già una chiave uguale inizio a numerarle
        key = key.Replace('\r', ' ').Trim(' ');
        while (cfg.ContainsKey(key))
        {
            count++;
            if (key.Any(char.IsDigit))
            {
                key = Regex.Replace(key, @"[\d-]", (string) ""+count);  
            }
            else
            {
                key = key + count;
            }
        }
        key = key.Replace('\r', ' ').Trim(' ');
        cfg.Add(key, data);
        return cfg;
    }

    private List<float> GetAnchors(Dictionary<string, Dictionary<string, object>> cfg)
    {
        Dictionary<string, object> data = new Dictionary<string, object>();
        List<float> anchors = new List<float>();
        object outs;
        
        cfg.TryGetValue("region", out data);
        data.TryGetValue("anchors", out outs);
        string result = (string) outs;
        string[] results = result.Split(',');
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i].Contains("\r"))
            {
                string newResult = results[i].Replace("\r", "");
                anchors.Add(float.Parse(newResult, CultureInfo.InvariantCulture.NumberFormat));
            }
            else
            {
                anchors.Add(float.Parse(results[i], CultureInfo.InvariantCulture.NumberFormat));
            }
        }
        return anchors;
    }
    public void Dispose()
    {
        worker.Dispose();
    }

    //only anchors
    /*
    private List<float> GetAnchorsProperty(string text)
    {
        string[] lines = text.Split('\n');
        List<float> result = new List<float>();
        foreach (string line in lines)
        {
            if (line.Contains("anchors"))
            {
                string[] entries = line.Split('=');
                string[] anchors = entries[1].Split(',');
                for(int i=0;i<anchors.Length;i++)
                {
                    result.Add(float.Parse(anchors[i], CultureInfo.InvariantCulture.NumberFormat));
                }
            }
        }
        return result;
    }*/
}
