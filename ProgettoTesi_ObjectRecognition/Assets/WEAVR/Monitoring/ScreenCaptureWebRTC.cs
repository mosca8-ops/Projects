using Byn.Net;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Legacy.Communication.WEAVRConnector
{
    [RequireComponent(typeof(MonitoringManager))]
    //[RequireComponent(typeof(MonitoringWWW))]
    [AddComponentMenu("WEAVR/Setup/Screen Capture RTC")]
    public class ScreenCaptureWebRTC : MonoBehaviour
    {
        //#region Server Fields
        //private string _signalingUrl;
        //private string _turnUrl = "turn:10.205.8.84:3478";
        //private string _turnUrlUser = "webrtc";
        //private string _turnUrlPassword = "turnpassword";
        //private string _stunUrl = "stun:10.205.8.84:3478";
        //#endregion

        //#region Network Fields
        //private IBasicNetwork mNetwork = null;
        //private WebRtcNetworkFactory factory = null;
        //private bool mIsServer = false;
        //private List<ConnectionId> mConnections = new List<ConnectionId>();
        //#endregion

        //#region Screen Capture Fields
        //private Texture2D tex;
        //private Texture2D tex2D;
        //private int width;
        //private int height;
        //private byte[] bytes;
        //private int step = 2;
        //private RenderTexture renderTexture;
        //readonly WaitForEndOfFrame waitFrameEnd = new WaitForEndOfFrame();
        //private Rect rectangle;
        //private MonitoringManager monitoringManager;
        //#endregion

        //public Vector2 resolution = new Vector2(850, 400);
        //[Range(10f, 90f)]
        //public int qualityOfImage = 70;

        //void Start()
        //{
        //    monitoringManager = MonitoringManager.Instance;

        //    _signalingUrl = WeavrSettings.Current.GetValue<string>("SignalingUrl");
        //    _turnUrl = WeavrSettings.Current.GetValue<string>("TurnUrl");
        //    _turnUrlUser = WeavrSettings.Current.GetValue<string>("TurnUser");
        //    _turnUrlPassword = WeavrSettings.Current.GetValue<string>("TurnPassword");
        //    _stunUrl = WeavrSettings.Current.GetValue<string>("StunUrl");

        //    width = (int)resolution[0];
        //    height = (int)resolution[1];
        //    tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        //    rectangle = new Rect(0, 0, width, height);


        //    factory = WebRtcNetworkFactory.Instance;
        //}

        //public void Update()
        //{
        //    if (step == 2)
        //    {
        //        StartCoroutine(SendFrame());
        //        step = 0;
        //    }
        //    step++;
        //}

        //private void FixedUpdate()
        //{
        //    HandleNetwork();
        //}

        //private void HandleNetwork()
        //{
        //    if (mNetwork != null)
        //    {
        //        mNetwork.Update();
        //        NetworkEvent evt;
        //        //check for new status messages
        //        while (mNetwork != null && mNetwork.Dequeue(out evt))
        //        {
        //            //check every message
        //            switch (evt.Type)
        //            {
        //                case NetEventType.ServerInitialized:
        //                    {
        //                        mIsServer = true;
        //                        string address = evt.Info;
        //                        Debug.Log("Server started. Address: " + address);
        //                    }
        //                    break;
        //                case NetEventType.ServerInitFailed:
        //                    {
        //                        mIsServer = false;
        //                        Debug.Log("Server start failed.");
        //                        Reset();
        //                    }
        //                    break;
        //                case NetEventType.ServerClosed:
        //                    {
        //                        mIsServer = false;
        //                        Debug.Log("Server closed. No incoming connections possible until restart.");
        //                    }
        //                    break;
        //                case NetEventType.NewConnection:
        //                    {
        //                        mConnections.Add(evt.ConnectionId);
        //                        monitoringManager.askForActivation = false;
        //                        Debug.Log("New local connection! ID: " + evt.ConnectionId);

        //                        if (mIsServer)
        //                        {
        //                            string msg = "New user " + evt.ConnectionId + " joined the room.";
        //                            Debug.Log(msg);
        //                        }
        //                    }
        //                    break;
        //                case NetEventType.ConnectionFailed:
        //                    {
        //                        Debug.Log("Connection failed");
        //                        Reset();
        //                        monitoringManager.askForActivation = true;
        //                    }
        //                    break;
        //                case NetEventType.Disconnected:
        //                    {
        //                        mConnections.Remove(evt.ConnectionId);
        //                        monitoringManager.askForActivation = true;
        //                        Debug.Log("Local Connection ID " + evt.ConnectionId + " disconnected");
        //                        if (mIsServer == false)
        //                        {
        //                            Reset();
        //                        }
        //                        else
        //                        {
        //                            string userLeftMsg = "User " + evt.ConnectionId + " left the room.";
        //                            Debug.Log(userLeftMsg);

        //                            if (mConnections.Count > 0)
        //                            {
        //                                Debug.Log(userLeftMsg);
        //                            }
        //                        }
        //                    }
        //                    break;
        //                case NetEventType.ReliableMessageReceived:
        //                case NetEventType.UnreliableMessageReceived:
        //                    {
        //                        HandleIncommingMessage(ref evt);
        //                    }
        //                    break;
        //            }
        //        }
        //        //finish this update by flushing the messages out if the network wasn't destroyed during update
        //        if (mNetwork != null)
        //            mNetwork.Flush();
        //    }
        //}

        //private IEnumerator SendFrame()
        //{
        //    if (mConnections.Count > 0)
        //    {
        //        yield return waitFrameEnd;

        //        Destroy(tex);

        //        tex = ScreenCapture.CaptureScreenshotAsTexture();
        //        renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        //        Graphics.Blit(tex, renderTexture);

        //        yield return null;

        //        Destroy(tex2D);
        //        RenderTexture.active = renderTexture;
        //        tex2D = new Texture2D(width, height, TextureFormat.RGB24, true);
        //        tex2D.filterMode = FilterMode.Point;

        //        tex2D.ReadPixels(rectangle, 0, 0, false);

        //        RenderTexture.active = null;
        //        RenderTexture.ReleaseTemporary(renderTexture);

        //        bytes = ImageConversion.EncodeToJPG(tex2D, qualityOfImage);

        //        foreach (ConnectionId id in mConnections)
        //        {
        //            Debug.Log($"Sent frame, size: {bytes.Length}");
        //            mNetwork.SendData(id, bytes, 0, bytes.Length, false);
        //        }
        //    }
        //}

        //public void OpenRoomButtonPressed(string roomName)
        //{
        //    Setup();
        //    mNetwork.StartServer(roomName);
        //    Debug.Log("StartServer " + roomName);
        //}

        //public void JoinRoomButtonPressed(string roomName)
        //{
        //    Setup();
        //    mNetwork.Connect(roomName);
        //    monitoringManager.askForActivation = false;
        //    Debug.Log("Connecting to " + roomName + " ...");
        //}

        //private void HandleIncommingMessage(ref NetworkEvent evt)
        //{
        //    MessageDataBuffer buffer = (MessageDataBuffer)evt.MessageData;
        //    Texture2D tex = new Texture2D(width, height);
        //    tex.LoadImage(buffer.Buffer);

        //    //return the buffer so the network can reuse it
        //    buffer.Dispose();
        //}

        //public void Setup()
        //{
        //    Debug.Log("Initializing webrtc network");
        //    mNetwork = WebRtcNetworkFactory.Instance.CreateDefault(_signalingUrl, new IceServer[] { new IceServer(_turnUrl, _turnUrlUser, _turnUrlPassword), new IceServer(_stunUrl) });
        //    if (mNetwork != null)
        //    {
        //        Debug.Log("WebRTCNetwork created");
        //    }
        //    else
        //    {
        //        Debug.Log("Failed to access webrtc ");
        //    }
        //}

        //public void Reset()
        //{
        //    Debug.Log("Cleanup!");
        //    mIsServer = false;
        //    mConnections = new List<ConnectionId>();
        //    Cleanup();
        //}

        ///// <summary>
        ///// called during reset and destroy
        ///// </summary>
        //private void Cleanup()
        //{
        //    mNetwork.Dispose();
        //    mNetwork = null;
        //    Debug.Log("Disconnected");
        //}
    }
}
