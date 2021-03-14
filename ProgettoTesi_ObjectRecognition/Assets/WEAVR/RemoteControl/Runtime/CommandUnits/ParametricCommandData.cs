using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TXT.WEAVR.RemoteControl
{
    [Serializable]
    public class ParametricCommandData : ICommandDataPrototype, ICommandData
    {
        private static readonly char[] s_arraySeparators = { ',', ' ' };
        private static readonly char[] s_spaceSeparators = { ' ' };

        public int RequesterId { get; private set; }

        private string m_data;
        public IParameter[] Parameters { get; private set; }

        private ParametricCommandData(int requesterId, string data)
        {
            RequesterId = requesterId;
            m_data = data;
            DispatchData(data);
        }

        public ParametricCommandData() { }

        private void DispatchData(string data)
        {
            // Trim for any inconvenience
            data = data.Trim();
            if (data[0] == '{' && data[data.Length - 1] == '}' && DispatchJsonData(data))
            {
                return;
            }
            if (data[0] == '[' && data[data.Length - 1] == ']' && DispatchArray(data))
            {
                return;
            }
            if (data[0] == '(' && data[data.Length - 1] == ')' && DispatchPositinalParameters(data))
            {
                return;
            }

            // otherwise it is just a plain list of parameters
            var plainValues = SmartSplit(data, ' ');
            Parameters = new IDataParameter[plainValues.Length];
            for (int i = 0; i < plainValues.Length; i++)
            {
                Parameters[i] = new CommandDataParameter(i, i.ToString(), plainValues[i]);
            }
        }

        private bool DispatchArray(string data)
        {
            //// First check if it is a JSON array
            //if (DispatchJsonData(@"{""" + nameof(SerializedParameterArray.parameters) + @""": " + data + "}"))
            //{
            //    return true;
            //}

            // Otherwise it should be an array of key value pairs
            data = data.Trim('[', ']');
            var splits = SmartSplit(data, ',');

            // Since it is a key value pairs array, so every split should be splitted again in key value pairs
            Parameters = new IDataParameter[splits.Length];
            for (int i = 0; i < splits.Length; i++)
            {
                // key: value
                int colonIndex = splits[i].IndexOf(':');
                if(colonIndex < 0)
                {
                    // Here we don't have a key value pair, add the parameter in any case
                    Parameters[i] = new CommandDataParameter(i, i.ToString(), splits[i]);
                }
                else
                {
                    Parameters[i] = new CommandDataParameter(i, splits[i].Substring(0, colonIndex).Trim(), splits[i].Substring(colonIndex + 1, splits[i].Length - colonIndex - 1).Trim());
                }
            }
            return true;
        }

        private bool DispatchPositinalParameters(string data)
        {
            // Otherwise it should be an array of values
            data = data.Trim('(', ')');
            var splits = SmartSplit(data, ',');
            Parameters = new IDataParameter[splits.Length];
            for (int i = 0; i < splits.Length; i++)
            {
                Parameters[i] = new CommandDataParameter(i, i.ToString(), splits[i]);
            }
            return true;
        }

        private static string[] SmartSplit(string data, params char[] splitter)
        {
            if (data.IndexOf('"') < data.LastIndexOf('"'))
            {
                // Here we have a potential string
                var majorSplits = data.Split('"');
                List<string> splits = new List<string>();
                string prevString = string.Empty;
                for (int i = 0; i < majorSplits.Length; i++)
                {
                    if (i % 2 == 1)
                    {
                        // Is is a string enclosed in ""
                        prevString = majorSplits[i];
                    }
                    else
                    {
                        var nextSplits = SmartSplit(majorSplits[i], splitter);
                        if (nextSplits.Length > 0)
                        {
                            nextSplits[0] = prevString + nextSplits[0];
                            splits.AddRange(SmartSplit(majorSplits[i], splitter));
                        }
                    }
                }
                return splits.ToArray();
            }
            else if(data.IndexOf('[') < data.IndexOf(']'))
            {
                // Here we have a potential array
                var majorSplits = data.Split('[');
                // Here the assumption is we don't have nested arrays
                List<string> splits = new List<string>();
                for (int i = 0; i < majorSplits.Length; i++)
                {
                    if(i % 2 == 1)
                    {
                        int indexClosingBracket = majorSplits[i].IndexOf(']');
                        if(indexClosingBracket < 0)
                        {
                            splits.AddRange(SmartSplit(majorSplits[i], splitter));
                        }
                        else
                        {
                            splits.Add(majorSplits[i].Substring(0, indexClosingBracket).Trim());
                            if(indexClosingBracket < majorSplits[i].Length - 2)
                            {
                                splits.AddRange(SmartSplit(majorSplits[i].Substring(indexClosingBracket + 1, majorSplits[i].Length - indexClosingBracket - 1), splitter));
                            }
                        }
                    }
                    else
                    {
                        splits.AddRange(SmartSplit(majorSplits[i], splitter));
                    }
                }

                return splits.ToArray();
            }
            else return data.Trim().Split(splitter, StringSplitOptions.RemoveEmptyEntries);
        }

        private bool DispatchJsonData(string data)
        {
            var serializedArray = JsonUtility.FromJson<SerializedParameterArray>(data);
            if(serializedArray == null)
            {
                return false;
            }

            Parameters = new IDataParameter[serializedArray.parameters.Length];
            for (int i = 0; i < serializedArray.parameters.Length; i++)
            {
                Parameters[i] = new CommandDataParameter(i, serializedArray.parameters[i].name, serializedArray.parameters[i].data);
            }

            return true;
        }

        public object GetRawData() => Parameters;

        public ICommandData Create(int requestId, byte[] serializedData)
        {
            return new ParametricCommandData(requestId, Encoding.ASCII.GetString(serializedData));
        }

        public bool TryGet<T>(string key, out T data)
        {
            for (int i = 0; i < Parameters.Length; i++)
            {
                if (Parameters[i].Name == key && Parameters[i] is T tdata)
                {
                    data = tdata;
                    return true;
                }
            }

            data = default;
            return false;
        }

        public bool TryGet<T>(out T data)
        {
            for (int i = 0; i < Parameters.Length; i++)
            {
                if (Parameters[i] is T tdata)
                {
                    data = tdata;
                    return true;
                }
            }

            data = default;
            return false;
        }

        public ICommandData Create(int requestId, IParameter[] parameters)
        {
            return new ParametricCommandData()
            {
                RequesterId = requestId,
                Parameters = parameters,
            };
        }

        [Serializable]
        private class SerializedParameter
        {
            public string name;
            public string data;
        }

        [Serializable]
        private class SerializedParameterArray
        {
            public SerializedParameter[] parameters;
        }

        public struct CommandDataParameter : IDataParameter
        {
            public int Index { get; }
            public string Name { get; set; }
            public string Data { get; private set; }
            public object Value { get => Data; set => Data = value is string s ? s : value?.ToString(); }

            public Type ValueType => Value?.GetType();

            public CommandDataParameter(int index, string name, string data)
            {
                Index = index;
                Name = name;
                Data = data;
            }
        }
    }
}
