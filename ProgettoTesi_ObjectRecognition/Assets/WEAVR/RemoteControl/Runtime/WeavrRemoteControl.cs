using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.RemoteControl
{

    public class WeavrRemoteControl : IDisposable
    {
        private static HashSet<ICommandChannel> s_channelsToRegister = new HashSet<ICommandChannel>();
        private static HashSet<ICommandUnit> s_commandsToRegister = new HashSet<ICommandUnit>();
        private static HashSet<object> s_eventsProvidersToRegister = new HashSet<object>();
        private static Dictionary<string, Delegate> s_events = new Dictionary<string, Delegate>();
        private static HashSet<IQueryUnit> s_queriesToRegister = new HashSet<IQueryUnit>();

        private static WeavrRemoteControl s_instance;

        public static event Action<string> CommandReceived;

        #region [  STATIC INITIALIZATION PART  ]

        public static void Initialize()
        {
            if (s_instance == null)
            {
                s_instance = new WeavrRemoteControl();
            }

            SetupConverters();
            // InitializeCommandUnits();   // <--- UNCOMMENT IF ALL COMMAND UNITS IN THE PROJECT SHOULD BE REGISTERED
            // InitializeQueryUnits();   // <--- UNCOMMENT IF ALL QUERY UNITS IN THE PROJECT SHOULD BE REGISTERED
        }

        private static void InitializeCommandUnits()
        {
            // Register Command Units
            foreach(var commandUnitType in TypeRetriever.GetTypesWhichImplement<ICommandUnit>())
            {
                // The MonoBehaviours should register themselves...
                if(!typeof(MonoBehaviour).IsAssignableFrom(commandUnitType) && commandUnitType.GetConstructor(Type.EmptyTypes) != null)
                {
                    s_commandsToRegister.Add(Activator.CreateInstance(commandUnitType) as ICommandUnit);
                }
            }
        }

        private static void InitializeQueryUnits()
        {
            // Register Command Units
            foreach (var commandUnitType in TypeRetriever.GetTypesWhichImplement<IQueryUnit>())
            {
                // The MonoBehaviours should register themselves...
                if (!typeof(MonoBehaviour).IsAssignableFrom(commandUnitType) && commandUnitType.GetConstructor(Type.EmptyTypes) != null)
                {
                    s_queriesToRegister.Add(Activator.CreateInstance(commandUnitType) as IQueryUnit);
                }
            }
        }

        public static void SetupConverters()
        {
            Serializer.RegisterConverterValueType<Parameters.Color32, Color>(c => new Color(c.red / 255f, c.green / 255f, c.blue / 255f, c.alpha / 255f));
            Serializer.RegisterConverterValueType<Color, Parameters.Color32>(c => new Parameters.Color32(c.r, c.g, c.b, c.a));
            Serializer.RegisterConverterValueType<Vector3, float[]>(v => new float[] { v.x, v.y, v.z });
            Serializer.RegisterConverter<float[], Vector3>(f => f.Length == 3, f => new Vector3(f[0], f[1], f[2]));
            Serializer.RegisterConverter<GameObject, Guid>(go => go.GetComponent<GuidComponent>(), go => go.GetComponent<GuidComponent>().Guid);
            Serializer.RegisterConverter<GameObject, string>(go => go.GetHierarchyPath());
        }

        public static void SetupConverters(Serializer serializer)
        {
            serializer.RegisterLocalConverterValueType<Parameters.Color32, Color>(c => new Color(c.red / 255f, c.green / 255f, c.blue / 255f, c.alpha / 255f));
            serializer.RegisterLocalConverterValueType<Color, Parameters.Color32>(c => new Parameters.Color32(c.r, c.g, c.b, c.a));
            serializer.RegisterLocalConverterValueType<Vector3, float[]>(v => new float[] { v.x, v.y, v.z });
            serializer.RegisterLocalConverter<float[], Vector3>(f => f.Length == 3, f => new Vector3(f[0], f[1], f[2]));
            serializer.RegisterLocalConverter<GameObject, Guid>(go => go.GetComponent<GuidComponent>(), go => go.GetComponent<GuidComponent>().Guid);
            serializer.RegisterLocalConverter<GameObject, string>(go => go.GetHierarchyPath());
        }

        #endregion

        #region [  STATIC REGISTRATION PART  ]

        public static void Register(ICommandChannel channel)
        {
            if(s_instance != null)
            {
                s_instance.RegisterChannelInternal(channel);
            }
            s_channelsToRegister.Add(channel);
        }

        
        public static void Unregister(ICommandChannel channel)
        {
            s_instance?.UnregisterChannelInternal(channel);
            s_channelsToRegister.Remove(channel);
        }

        public static void Register(ICommandUnit commandUnit)
        {
            if (s_instance != null)
            {
                s_instance.RegisterCommandInternal(commandUnit);
            }
            s_commandsToRegister.Add(commandUnit);
        }
        
        public static void Unregister(ICommandUnit commandUnit)
        {
            s_instance?.UnregisterCommandInternal(commandUnit);
            s_commandsToRegister.Remove(commandUnit);
        }

        public static void RegisterEvents(object eventProvider)
        {
            if (s_instance != null)
            {
                s_instance.RegisterEventsInternal(eventProvider);
            }
            s_eventsProvidersToRegister.Add(eventProvider);
        }
        
        public static void UnregisterEvents(object eventProvider)
        {
            s_instance?.UnregisterEventsInternal(eventProvider);
            s_eventsProvidersToRegister.Remove(eventProvider);
        }

        public static void Register(IQueryUnit queryUnit)
        {
            if (s_instance != null)
            {
                s_instance.RegisterQueryInternal(queryUnit);
            }
            s_queriesToRegister.Add(queryUnit);
        }

        
        public static void Unregister(IQueryUnit queryUnit)
        {
            s_instance?.UnregisterQueryInternal(queryUnit);
            s_queriesToRegister.Remove(queryUnit);
        }

        public static WeavrQuery Query
        {
            get => s_instance?.m_query;
        }

        public static WeavrRC WeavrRC
        {
            get => s_instance?.m_rc;
        }
        #endregion

        Serializer m_serializer;
        WeavrRC m_rc;
        WeavrQuery m_query;

        Dictionary<int, (IRequest request, long rid, bool binary)> m_pendingRequests;

        private WeavrRemoteControl(bool caseSensitiveCommands = false)
        {
            Serializer.TextSerializeFunctor = JsonUtility.ToJson;
            Serializer.TextDeserializeFunctor = JsonUtility.FromJson;

            m_query = new WeavrQuery();
            m_serializer = new Serializer();
            SetupConverters(m_serializer);
            m_rc = new WeavrRC(new DataInterface(m_serializer, caseSensitiveCommands));

            // This is only for debug purposes
            // TODO: Remove it in production
            //m_rc.Interface.SaveCommands = true;

            m_pendingRequests = new Dictionary<int, (IRequest request, long rid, bool binary)>();

            // Apply all registrations
            foreach(var channel in s_channelsToRegister)
            {
                RegisterChannelInternal(channel);
            }
            foreach(var command in s_commandsToRegister)
            {
                RegisterCommandInternal(command);
            }
            foreach(var eventProvider in s_eventsProvidersToRegister)
            {
                RegisterEventsInternal(eventProvider);
            }
            foreach(var queryUnit in s_queriesToRegister)
            {
                RegisterQueryInternal(queryUnit);
            }
        }

        public void Dispose()
        {
            foreach (var channel in s_channelsToRegister)
            {
                Unregister(channel);
            }
            foreach (var command in s_commandsToRegister)
            {
                Unregister(command);
            }
            foreach (var eventProvider in s_eventsProvidersToRegister)
            {
                UnregisterEvents(eventProvider);
            }
            foreach (var queryUnit in s_queriesToRegister)
            {
                Unregister(queryUnit);
            }
        }

        #region [  CHANNELS PART  ]

        private void Channel_OnNewRequest(IRequest request)
        {
            // Since we need to handle the data exchange without relying entirely on WeavrRC,
            // we will make use of some WeavrRC functionality but on a lower level
            
            var requestData = request.GetRequest();
            if(requestData.Bytes.Length < 3)
            {
                request.SendResponse(m_serializer.SerializeBinary($"[Error]: Too short message received {requestData.Bytes.Length}"));
                return;
            }

            var dispatch = m_rc.Interface.DispatchAndExecute(request.GetRequest());

            switch (dispatch.DispatchType)
            {
                case DataInterface.DispatchType.RegisterToEvent:
                    // Register the client for Interface_EventReadyToBeSent
                    m_rc.RegisterEventListener(request.Source, dispatch);
                    break;
                case DataInterface.DispatchType.UnregisterFromEvent:
                    // Unregister the client from Interface_EventReadyToBeSent
                    m_rc.UnregisterEventListener(request.Source, dispatch);
                    break;
                case DataInterface.DispatchType.ErrorReceived:
                    WeavrDebug.LogError(this, $"Received Error: {dispatch.Message}");
                    break;
                case DataInterface.DispatchType.DispatchingError:
                    WeavrDebug.LogError(this, $"Dispatch Error: {dispatch.Message}");
                    break;
                case DataInterface.DispatchType.ReturnedValue:
                    if (dispatch.ResponseBytes != null)
                    {
                        request.SendResponse(dispatch.ResponseBytes);
                    }
                    break;
                case DataInterface.DispatchType.InvokeMethod:
                    // Print the message if there is one
                    CommandReceived?.Invoke(dispatch.Message);
                    break;
                case DataInterface.DispatchType.InvokeMethodWithReturn:
                    // Print the message if there is one
                    CommandReceived?.Invoke(dispatch.Message);
                    // Send back the message
                    if (dispatch.ResponseBytes?.Length > 0)
                    {
                        request.SendResponse(dispatch.ResponseBytes);
                    }
                    break;
                case DataInterface.DispatchType.InvokeMethodWithDelayedReturn:
                    // Print the message if there is one
                    CommandReceived?.Invoke(dispatch.Message);
                    // Once the result is ready, send it back to the client
                    dispatch.ResponseBytesReady += r => request.SendResponse(r.ResponseBytes);
                    break;
                default:
                    // In theory everything else should be handled by the interface
                    break;
            }
        }

        #endregion

        #region [  COMMANDS PART  ]

        private void RequestSucceeded(ICommandUnit unit, int requestId, object result)
        {
            var (request, rid, binary) = m_pendingRequests[requestId];
            //request.SendResponse(m_composer.Compose(rid, binary, null, result));
            m_pendingRequests.Remove(requestId);
        }

        private void RequestFailed(ICommandUnit unit, int requestId, object result)
        {
            var (request, rid, binary) = m_pendingRequests[requestId];
            //request.SendResponse(m_composer.Compose(rid, binary, null, "[Error]: Failed to execute command. Result = ", result));
            m_pendingRequests.Remove(requestId);
        }


        #endregion

        #region [  QUERY PART  ]


        #endregion

        #region [  REGISTRATION PART  ]

        private void RegisterChannelInternal(ICommandChannel channel)
        {
            channel.OnNewRequest -= Channel_OnNewRequest;
            channel.OnNewRequest += Channel_OnNewRequest;
        }
        
        private void UnregisterChannelInternal(ICommandChannel channel)
        {
            channel.OnNewRequest -= Channel_OnNewRequest;
        }

        private void RegisterCommandInternal(ICommandUnit commandUnit)
        {
            m_rc.Interface.BindAllRemoteMethods(commandUnit);
            commandUnit.RegisterCommands(m_rc.Interface);
            RegisterEvents(commandUnit);
        }

        private void UnregisterCommandInternal(ICommandUnit commandUnit)
        {
            m_rc.Interface.UnbindAllRemoteMethods(commandUnit);
            commandUnit.UnregisterCommands(m_rc.Interface);
            UnregisterEvents(commandUnit);
        }

        private void RegisterEventsInternal(object eventProvider)
        {
            m_rc.Interface.RegisterRemoteEvents(eventProvider);
        }

        private void UnregisterEventsInternal(object eventProvider)
        {
            m_rc.Interface.UnregisterRemoteEvents(eventProvider);
        }

        private void RegisterQueryInternal(IQueryUnit queryUnit)
        {
            m_query.Register(queryUnit);
        }

        private void UnregisterQueryInternal(IQueryUnit queryUnit)
        {
            m_query.Unregister(queryUnit);
        }

        #endregion
    }
}
