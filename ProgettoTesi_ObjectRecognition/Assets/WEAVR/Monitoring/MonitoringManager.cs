using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Communication;
using TXT.WEAVR.Core;
using TXT.WEAVR.Procedure;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.Legacy.Communication.WEAVRConnector
{
    class MonitoringSettings : IWeavrSettingsClient
    {
        public string SettingsSection => "Monitoring";

        public IEnumerable<ISettingElement> Settings => new Setting[]{
            ("SignalingUrl", string.Empty, "Monitoring Signaling Url", SettingsFlags.Runtime),
            ("TurnUrl", string.Empty, "Monitoring Turn Url", SettingsFlags.Runtime),
            ("TurnUser", string.Empty, "Monitoring Turn User", SettingsFlags.Runtime),
            ("TurnPassword", string.Empty, "Monitoring Turn Password", SettingsFlags.Runtime),
            ("StunUrl", string.Empty, "Monitoring Stun Url", SettingsFlags.Runtime),
        };
    }

    [AddComponentMenu("WEAVR/Setup/Monitoring Manager")]
    public class MonitoringManager : MonoBehaviour
    {
        //#region Fields

        //private ScreenCaptureWebRTC screenCapture;
        //private MonitoringWWW _monitoringWWW;
        //private WeavrWWW _weavrWWW;
        //private Room room;

        //public bool askForActivation = true;


        //#endregion

        //#region Singleton

        //public static MonitoringManager _instance = null;

        //public static MonitoringManager Instance {
        //    get {
        //        if (_applicationIsQuitting)
        //        {
        //            return null;
        //        }
        //        if (_instance == null)
        //        {
        //            Debug.Log("Creation of MonitoringManager");

        //            _instance = FindObjectOfType<MonitoringManager>();
        //            if (_instance == null)
        //            {
        //                //If no object is active, then create a new one
        //                GameObject go = new GameObject("MonitoringManager");
        //                _instance = go.AddComponent<MonitoringManager>();
        //            }
        //        }

        //        return _instance;
        //    }
        //}

        //#endregion

        //private static bool _applicationIsQuitting = false;

        //public void OnDestroy()
        //{
        //    CancelInvoke();
        //    _applicationIsQuitting = true;
        //}

        //private void Awake()
        //{
        //    SceneManager.sceneLoaded -= OnSceneLoaded;
        //    SceneManager.sceneLoaded += OnSceneLoaded;

        //    _monitoringWWW = GetComponent<MonitoringWWW>();
        //    screenCapture = GetComponent<ScreenCaptureWebRTC>();

        //    if (_weavrWWW == null)
        //    {
        //        _weavrWWW = FindObjectOfType<WeavrWWW>();
        //    }

        //    AddProcedureEventsListener();
        //}

        //void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        //{
        //    AddProcedureEventsListener();
        //}

        //protected virtual void AddProcedureEventsListener()
        //{
        //    CancelInvoke();

        //    ProcedureRunner.Current.ProcedureStarted -= OnProcedureStarted;
        //    ProcedureRunner.Current.ProcedureStarted += OnProcedureStarted;
        //}

        //private void OnProcedureStarted(ProcedureRunner runner, WEAVR.Procedure.Procedure procedure, ExecutionMode mode)
        //{
        //    if (CheckMonitoringLicence())
        //    {
        //        CreateRoom(WeavrDataStorage.Instance.AssetBundleProcedureRequest.Procedure.Id);

        //        InvokeRepeating(nameof(GetActivator), 3f, 3f);
        //    }
        //}

        //private void Start()
        //{
        //}

        //public void CreateRoom(Guid currentProcedure)
        //{
        //    _monitoringWWW.CreateRoom(currentProcedure, SuccessfulCreateRoomPOST, FailedCreateRoomPOST);
        //}

        //public bool CheckMonitoringLicence()
        //{
        //    // HACK: Commented to avoid compilation errors
        //    //var services = WeavrServerManager.Instance.CurrentUser.Account.Services.ToList();
        //    //foreach (var service in services)
        //    //{
        //    //    if (service.Name == "MONITORING")
        //    //    {
        //    //        return true;
        //    //    }
        //    //}

        //    return false;
        //}

        //public void GetActivator()
        //{
        //    //Debug.Log ("Should ask for activation: " + (room != null && canUseMonitoringService));
        //    if (room != null)
        //    {
        //        Debug.Log("Sending the GET request");
        //        _monitoringWWW.GetWebRtcActivator(room, SuccessCallbackGET, ErrorCallbackGET);
        //    }
        //}

        //#region Successful Callbacks

        //public void SuccessfulCreateRoomPOST(UnityWebRequest www, Request request)
        //{
        //    Debug.Log(www.downloadHandler.text);
        //    room = JsonConvert.DeserializeObject<Room>(www.downloadHandler.text);
        //}

        //public void SuccessCallbackGET(UnityWebRequest www, Request request)
        //{
        //    if (www.downloadHandler.text == "true" && askForActivation)
        //    {
        //        screenCapture.JoinRoomButtonPressed(room.Id.ToString());
        //    }
        //    else
        //        Debug.Log("Activator is false");
        //}

        //#endregion

        //#region Failed Callbacks

        //public void FailedCreateRoomPOST(UnityWebRequest www, Request request)
        //{
        //    Debug.Log("Failed the POST request!: " + www.error);
        //}

        //public void ErrorCallbackGET(UnityWebRequest www, Request request)
        //{
        //    Debug.Log("Failed the GET request: " + www.error);
        //}

        //#endregion
    }
}