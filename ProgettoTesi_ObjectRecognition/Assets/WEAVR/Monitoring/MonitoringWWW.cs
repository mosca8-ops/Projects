using System;
using UnityEngine;

namespace TXT.WEAVR.Legacy.Communication.WEAVRConnector
{
    //[AddComponentMenu("WEAVR/Setup/Monitoring WWW")]
    //public class MonitoringWWW : WeavrWWW
    //{
    //    public void CreateRoom(Guid procedureId, UWRCallback successCallback, UWRCallback errorCallback)
    //    {
    //        string procID = procedureId.ToString();
    //        var req = new Request()
    //        {
    //            Url = WeavrAPI.WEBRTC_ROOM,
    //            Headers = GetJsonHeader(true),
    //            Body = new Room()
    //            {
    //                ProcedureId = procedureId
    //            }
    //        };
    //        var enumerator = POSTCoroutine(req, successCallback, errorCallback);
    //        StartCoroutine(enumerator);
    //    }

    //    public void GetWebRtcActivator(Room room, UWRCallback successCallback, UWRCallback errorCallback)
    //    {
    //        var req = new Request()
    //        {
    //            Url = WeavrAPI.WEBRTC_ACTIVATION + "/" + room.Id
    //        };
    //        var enumerator = GETCoroutine(req, successCallback, errorCallback);
    //        StartCoroutine(enumerator);
    //    }

    //}
}
