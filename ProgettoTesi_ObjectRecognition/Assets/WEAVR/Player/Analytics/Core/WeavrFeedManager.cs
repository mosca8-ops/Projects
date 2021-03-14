using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.Interaction;
using TXT.WEAVR.Communication.Entities.Xapi;
using TXT.WEAVR.Maintenance;
using TXT.WEAVR.Player.Model;
using TXT.WEAVR.Procedure;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using TXT.WEAVR.Communication;

using ProcedureAsset = TXT.WEAVR.Procedure.Procedure;
using ProcedureEntity = TXT.WEAVR.Communication.Entities.Procedure;
using TXT.WEAVR.Communication.Entities;
using TXT.WEAVR.Core;

namespace TXT.WEAVR.Player.Analytics
{
    [AddComponentMenu("WEAVR/Setup/WEAVR Feed Manager")]
    public class WeavrFeedManager : MonoBehaviour, IAnalyticsUnit, IWeavrSettingsClient
    {

        private class StateFeedManager
        {
            public Guid? IdProcedure { get; set; }
            public Guid? IdProcedureVersion { get; set; }
            public Guid? IdProcedureVersionPlatform { get; set; }

            public Guid? IdProcedureStep { get; set; }
            public Guid? IdProcedureVersionStep { get; set; }

            public Guid? IdExecutionProcedureVersion { get; set; }
            public Guid? IdExecutionProcedureVersionStep { get; set; }
        }

        private void EnqueueRequest(Request request)
        {
            try
            {
                List<XApiStatement> statements = null;
                if (typeof(IEnumerable).IsAssignableFrom(request.Body.GetType()))
                {
                    statements = request.Body as List<XApiStatement>;
                }
                else
                {
                    statements = new List<XApiStatement>() { request.Body as XApiStatement };
                }
                List<Request> requests = new List<Request>();
                foreach (var statement in statements)
                {
                    requests.Add(new Request()
                    {
                        Url = request.Url,
                        QueryUrl = request.QueryUrl,
                        Headers = request.Headers,
                        Body = statement
                    });
                }

                AddToQueue(requests);
            }
            catch (Exception e)
            {
                WeavrDebug.LogError(this, "Exception during Enqueue Request " + e.Message);
            }
        }

        private const string FEED_REQUESTS = "FEED_REQUESTS";

        private StateFeedManager m_stateFeedManager;
        private List<Request> m_feedsShadow;
        private ProcedureEntity m_currentProcedure;
        private ProcedureVersion m_currentProcedureVersion;
        private IEnumerable<IXApiProvider> m_providers;

        public bool Active => isActiveAndEnabled;

        public string SettingsSection => "WEAVR Server";

        public IEnumerable<ISettingElement> Settings => new Setting[]{
            new Setting()
                {
                    name = "AnalyticsURL",
                    description = "URL for analytics web server",
                    flags = SettingsFlags.Runtime,
                    Value = string.Empty,
                }
        };

        public void AddFeed(string url, Dictionary<string, string> queryUrl, Dictionary<string, string> headers, XApiStatement body)
        {

            var request = new Request()
            {
                Url = url,
                QueryUrl = queryUrl,
                Headers = headers,
                Body = body
            };

            TryToSyncFeed(new List<Request>() { request });
        }

        public async void TryToSyncFeed(List<Request> feeds)
        {
            try
            {
                var www = new WeavrWebRequest();
                // When we add a feed
                if (feeds?.Count > 0)
                {
                    // When there is no queue
                    if (m_feedsShadow.Count == 0
                        && Application.internetReachability != NetworkReachability.NotReachable)
                    {
                        var req = new Request()
                        {
                            Url = feeds[0].Url,
                            QueryUrl = feeds[0].QueryUrl,
                            Headers = feeds[0].Headers,
                            Body = feeds.Select(f => f.Body as XApiStatement).ToList()
                        };

                        Debug.Log("TryToSyncFeed: No Queue Found, trying to Send Xapi [" + req + "]");

                        // In case of Success => No action
                        // In case of HttpError => No action
                        // In case of NetworkError => Add the request to the Queue
                        var response = await www.POST(req);
                        if (response.HasError)
                        {
                            EnqueueRequest(req);
                        }
                    }
                    // When there is queue
                    else
                    {
                        Debug.Log("TryToSyncFeed: Queue Found, trying to Add Xapi in queue [" + m_feedsShadow + ", " + feeds + "]");

                        AddToQueue(feeds);
                    }
                }

                if (m_feedsShadow.Count > 0
                    && Application.internetReachability != NetworkReachability.NotReachable)
                {
                    Debug.Log("FeedsShadow has elements");
                    var req = new Request()
                    {
                        Url = m_feedsShadow[0].Url,
                        QueryUrl = m_feedsShadow[0].QueryUrl,
                        Headers = m_feedsShadow[0].Headers,
                        Body = m_feedsShadow.Select(f => f.Body as XApiStatement).ToList()
                    };

                    Debug.Log("TryToSyncFeed: Queue Found, trying to send All [" + req + "]");

                    // In case of Success => Clear the queue
                    // In case of HttpError => Clear the queue
                    // In case of NetworkError => No action
                    var response = await www.POST(req);
                    if (!response.HasError)
                    {
                        try
                        {
                            ClearQueue();
                        }
                        catch(Exception ex)
                        {
                            WeavrDebug.LogError(this, "Exception during ClearQueue " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception during TryToSyncFeed: {e.Message} ");
            }
        }

        protected virtual void Start()
        {

        }

        private void Awake()
        {
            try
            {
                var feeds = PlayerPrefs.GetString(FEED_REQUESTS, null);

                // PlayerPrefs is empty
                if (string.IsNullOrEmpty(feeds))
                {
                    m_feedsShadow = new List<Request>();
                }
                // PlayerPrefs is not empty
                else
                {
                    m_feedsShadow = JsonConvert.DeserializeObject<List<Request>>(feeds);
                }
            }
            catch (Exception e)
            {
                Debug.Log("Error " + e.Message);
            }
            finally
            {
                if (m_feedsShadow == null)
                {
                    m_feedsShadow = new List<Request>();
                }
            }

            if (m_stateFeedManager == null)
            {
                m_stateFeedManager = new StateFeedManager();
            }

            m_providers = GetComponentsInChildren<IXApiProvider>(true);
        }

        public void Start(ProcedureEntity entity, ProcedureAsset asset, ExecutionMode executionMode)
        {
            m_currentProcedure = entity;
            m_currentProcedureVersion = entity.GetLastVersionForCurrentPlatform();
            AddProcedureEventsListener(asset, executionMode);

            if (IsToSend(VerbType.START, ActionType.PROCEDURE))
            {
                var requests = new List<Request>();

                m_stateFeedManager.IdProcedure = entity.Id;
                m_stateFeedManager.IdProcedureVersion = m_currentProcedureVersion.Id;
                m_stateFeedManager.IdProcedureVersionPlatform = entity.GetLastVersionPlatform().Id;
                m_stateFeedManager.IdProcedureStep = null;
                m_stateFeedManager.IdProcedureVersionStep = null;
                m_stateFeedManager.IdExecutionProcedureVersion = Guid.NewGuid();
                m_stateFeedManager.IdExecutionProcedureVersionStep = null;

                var req = CreateRequest(CreateXApiBody(m_currentProcedureVersion.Id, null, VerbType.START, ActionType.PROCEDURE));

                requests.Add(req);

                TryToSyncFeed(requests);
            }
        }

        public void Stop()
        {
            if (IsToSend(VerbType.END, ActionType.PROCEDURE))
            {

                var requests = new List<Request>();

                var req = CreateRequest(CreateXApiBody(m_currentProcedureVersion.Id, null, VerbType.END, ActionType.PROCEDURE_STEP));

                m_stateFeedManager.IdProcedureStep = null;
                m_stateFeedManager.IdProcedureVersionStep = null;
                m_stateFeedManager.IdExecutionProcedureVersionStep = null;

                var req2 = CreateRequest(CreateXApiBody(m_currentProcedureVersion.Id, null, VerbType.END, ActionType.PROCEDURE));

                m_stateFeedManager.IdProcedure = null;
                m_stateFeedManager.IdProcedureVersion = null;
                m_stateFeedManager.IdProcedureVersionPlatform = null;
                m_stateFeedManager.IdProcedureStep = null;
                m_stateFeedManager.IdProcedureVersionStep = null;
                m_stateFeedManager.IdExecutionProcedureVersion = null;
                m_stateFeedManager.IdExecutionProcedureVersionStep = null;

                requests.Add(req);
                requests.Add(req2);

                TryToSyncFeed(requests);
            }

            foreach(var provider in m_providers)
            {
                if(provider?.Active == true)
                {
                    provider.Cleanup();
                }
            }
        }

        protected virtual void AddProcedureEventsListener(ProcedureAsset procedure, ExecutionMode mode)
        {
            ProcedureRunner.Current.StepStarted -= OnStepStarted;
            ProcedureRunner.Current.StepStarted += OnStepStarted;

            ProcedureRunner.Current.StepFinished -= OnStepFinished;
            ProcedureRunner.Current.StepFinished += OnStepFinished;

            var interactables = SceneTools.GetComponentsInScene<AbstractInteractiveBehaviour>();

            foreach(var provider in m_providers)
            {
                if(provider?.Active == true)
                {
                    provider.Prepare(procedure, mode, interactables, OnAnalyticEventReceived);
                }
            }
        }

        protected Request CreateRequest(object body)
        {
            return new Request()
            {
                Url = GetURL(),
                QueryUrl = null,
                ContentType = MIME.JSON,
                Body = body,
            };
        }

        private string GetURL()
        {
            return Weavr.Settings.GetValue("AnalyticsURL", WeavrPlayer.API.AnalyticsApp.XAPI_FEED);
        }

        protected void OnAnalyticEventReceived(string verb, string @object, Guid guid)
        {
            //Debug.Log($"OnAnalyticEventReceived: {verb} {@object}");

            if (IsToSend(verb, @object))
            {
                var requests = new List<Request>();

                var req = CreateRequest(CreateXApiBody(guid, null, verb, @object));

                requests.Add(req);

                TryToSyncFeed(requests);
            }
        }
        
        private void OnStepStarted(IProcedureStep step)
        {
            //Debug.Log("OnStepStarted: " + step);

            if (IsToSend(VerbType.START, ActionType.PROCEDURE_STEP))
            {
                var requests = new List<Request>();

                m_stateFeedManager.IdProcedureStep = m_currentProcedure.ProcedureSteps.FirstOrDefault(ps => ps.UnityId == new Guid(step.StepGUID)).Id;
                m_stateFeedManager.IdProcedureVersionStep = m_currentProcedureVersion.ProcedureVersionSteps.FirstOrDefault(pvp => pvp.ProcedureStepId == m_stateFeedManager.IdProcedureStep).Id;
                m_stateFeedManager.IdExecutionProcedureVersionStep = Guid.NewGuid();

                var req = CreateRequest(CreateXApiBody(new Guid(step.StepGUID), null, VerbType.START, ActionType.PROCEDURE_STEP));

                requests.Add(req);

                TryToSyncFeed(requests);
            }
        }

        private void OnStepFinished(IProcedureStep step)
        {
            //Debug.Log("OnStepFinished: " + step);

            if (IsToSend(VerbType.END, ActionType.PROCEDURE_STEP))
            {

                var requests = new List<Request>();

                var req = CreateRequest(CreateXApiBody(new Guid(step.StepGUID), null, VerbType.END, ActionType.PROCEDURE_STEP));

                m_stateFeedManager.IdProcedureStep = null;
                m_stateFeedManager.IdProcedureVersionStep = null;
                m_stateFeedManager.IdExecutionProcedureVersionStep = null;

                requests.Add(req);

                TryToSyncFeed(requests);
            }
        }

        private XApiStatement CreateXApiBody(Guid guid, Dictionary<string, object> extensions, string verb, string actionType)
        {
            return new XApiStatement()
            {
                Actor = new XApiActor()
                {
                    Id = WeavrPlayer.Authentication.AuthUser?.Id ?? Guid.Empty,
                    ObjectType = ActorType.AGENT
                },
                Verb = verb,
                Object = new XApiObject()
                {
                    Id = guid,
                    ObjectType = ObjectType.ACTIVITY,
                    ActionType = actionType,
                    Extensions = CreateExtensions(extensions)
                },
            };
        }

        private XApiExtensions CreateExtensions(Dictionary<string, object> extensions)
        {
            return new XApiExtensions()
            {
                IdProcedure = m_stateFeedManager.IdProcedure,
                IdProcedureVersion = m_stateFeedManager.IdProcedureVersion,
                IdProcedureVersionPlatform = m_stateFeedManager.IdProcedureVersionPlatform,
                IdProcedureStep = m_stateFeedManager.IdProcedureStep,
                IdProcedureVersionStep = m_stateFeedManager.IdProcedureVersionStep,
                IdExecutionProcedureVersion = m_stateFeedManager.IdExecutionProcedureVersion,
                IdExecutionProcedureVersionStep = m_stateFeedManager.IdExecutionProcedureVersionStep,

                TimeStamp = DateTime.UtcNow,

                Extensions = extensions
            };
        }

        private void ClearQueue()
        {
            try
            {
                m_feedsShadow.Clear();
                PlayerPrefs.SetString(FEED_REQUESTS, string.Empty);
            }
            catch (Exception e)
            {
                WeavrDebug.LogError(this, "ClearQueue Error " + e.Message);
            }
        }

        private void AddToQueue(List<Request> feeds)
        {
            int oldLength = m_feedsShadow.Count;
            try
            {
                m_feedsShadow.AddRange(feeds);
                PlayerPrefs.SetString(FEED_REQUESTS, JsonConvert.SerializeObject(m_feedsShadow));
            }
            catch (Exception e)
            {
                WeavrDebug.LogError(this, "AddToQueue Error " + e.Message);

                if (m_feedsShadow.Count != oldLength)
                {
                    m_feedsShadow.RemoveRange(oldLength - 1, feeds.Count);
                }
            }
        }

        protected bool IsToSend(string verb, string @object)
        {
            return true;
            // TODO: Will be further implemented
            ////Debug.Log(JsonConvert.SerializeObject(WeavrServerManager.Instance.CurrentUser));
            //if (WeavrServerManager.Instance.CurrentUser != null)
            //{
            //    foreach (var analyticEvent in WeavrServerManager.Instance.CurrentUser.Account.AnalyticEvents)
            //    {
            //        if (analyticEvent.Verb == verb && analyticEvent.Object == @object
            //            || analyticEvent.Verb == verb && analyticEvent.Object == null)
            //        {
            //            return true;
            //        }
            //    }
            //}
            //return false;
        }
    }
}