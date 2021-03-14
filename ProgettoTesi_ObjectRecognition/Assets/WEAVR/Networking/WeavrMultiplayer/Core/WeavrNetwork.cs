using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using System.Linq;
using UnityEngine.SceneManagement;
using System.IO;

using Photon.Pun;
using Photon.Realtime;
using TXT.WEAVR.Communication.Entities;

namespace TXT.WEAVR.Networking
{
    [AddComponentMenu("WEAVR/Setup/WEAVR Network")]
    public class WeavrNetwork : MonoBehaviourPunCallbacks, IWeavrSettingsClient, IWeavrSingleton
    {

        #region [  STATIC PART  ]

        private static readonly string k_ConfigFilePath = Application.streamingAssetsPath + "/connection.config";
        private const string k_ProcedureRoom = "[ProcedureRoom]:";

        private static WeavrNetwork s_instance;
        public static WeavrNetwork Instance {
            get {
                if (!s_instance)
                {
                    s_instance = Weavr.TryGetInCurrentScene<WeavrNetwork>();
                }
                return s_instance;
            }
        }

        private static bool s_autoConnect;
        private static NetworkUser s_currentUser;

        public static NetworkUser CurrentUser {
            get => s_currentUser;
            set {
                if (s_currentUser != value)
                {
                    if (NetworkUser.AreSame(s_currentUser, value))
                    {
                        s_currentUser = value;
                        return;
                    }
                    if (s_currentUser != null)
                    {
                        if (s_instance) { s_instance.Disconnect(); }
                        else { PhotonNetwork.Disconnect(); }
                    }
                    s_currentUser = value;
                    if (s_currentUser != null && s_autoConnect && Instance)
                    {
                        s_instance.TryConnect();
                    }
                }
            }
        }

        public static void SetUser(User user)
        {
            if (user != null)
            {
                CurrentUser = new NetworkUser(user);
            }
            else
            {
                CurrentUser = null;
            }
        }

        #endregion

        [SerializeField]
        private bool m_autoConnect = true;
        [SerializeField]
        private bool m_reconnectOnDisconnect = true;
        [SerializeField]
        private bool m_overwriteSettings = true;
        [NonSerialized]
        private string m_lastJoinedRoom;

        [Space]
        public int networkUpdateRate = 30;
        public bool closeRoomOnExit = true;

        [Space]
        [SerializeField]
        private bool m_preferLocalUser = false;
        [SerializeField]
        private bool m_autoJoinRooms = false;

        [Space]
        [SerializeField]
        private Events m_events;

        public UnityEvent OnPhotonConnected => m_events.onPhotonConnected;
        public UnityEventString OnPhotonDisconnected => m_events.onPhotonDisconnected;
        public UnityEventString OnRoomCreated => m_events.onRoomCreated;
        public UnityEventString OnRoomAvailable => m_events.onRoomAvailable;
        public UnityEvent OnNoRoomAvailable => m_events.onNoRoomAvailable;
        public UnityEvent OnRoomListUpdated => m_events.onRoomListUpdated;
        public UnityEventString OnRoomJoined => m_events.onRoomJoined;
        public UnityEventString OnRoomLeft => m_events.onRoomLeft;
        public UnityEventString OnPlayerEntered => m_events.onPlayerEnteredRoom;
        public UnityEventString OnPlayerLeft => m_events.onPlayerLeftRoom;
        public UnityEventString OnPlayerAlreadyInRoom => m_events.onPlayerAlreadyInRoom;

        public delegate void PlayerDelegate(string playerId, string nickName);

        public event PlayerDelegate PlayerEnteredRoom;
        public event PlayerDelegate PlayerLeftRoom;

        public event Action<string> PlayerNameChanged;

        public NetworkUser ThisUser => CurrentUser;

        public bool IsInRoom => CurrentUser?.Room != null;

        public string PlayerFirstName
        {
            get => CurrentUser?.FirstName;
            set
            {
                if(CurrentUser != null && CurrentUser.FirstName != value)
                {
                    CurrentUser.FirstName = value;
                    PlayerNameChanged?.Invoke(CurrentUser.FirstName + (string.IsNullOrEmpty(CurrentUser.LastName) ? string.Empty : " " + CurrentUser.LastName));
                }
            }
        }

        public string PlayerLastName
        {
            get => CurrentUser?.LastName;
            set
            {
                if (CurrentUser != null && CurrentUser.LastName != value)
                {
                    CurrentUser.LastName = value;
                    PlayerNameChanged?.Invoke(CurrentUser.LastName + (string.IsNullOrEmpty(CurrentUser.LastName) ? string.Empty : " " + CurrentUser.LastName));
                }
            }
        }

        public string PlayerName
        {
            get => CurrentUser?.FirstName + (string.IsNullOrEmpty(CurrentUser?.LastName) ? string.Empty : " " + CurrentUser.LastName);
            set
            {
                if (PlayerName != value)
                {
                    CurrentUser.FirstName = value;
                    CurrentUser.LastName = null;
                    PlayerNameChanged?.Invoke(CurrentUser.FirstName);
                }
            }
        }

        [Serializable]
        public class LocalUser
        {
            public string id;
            public string firstName;
            public string lastName;
        }

        [Serializable]
        public class RoomDefinition
        {
            public string roomNamePrefix;
            public int minPlayers;
            public int maxPlayers;
            [Tooltip("Player time to live. Will remove the avatar if player is disconnected for the specified amount of time")]
            public int playerTTL;
            public bool closeOnOwnerLeave;
        }

        public event Action<GameObject> AvatarChanged;

        private GameObject m_avatar;
        public GameObject Avatar
        {
            get => m_avatar;
            set
            {
                if(m_avatar != value)
                {
                    m_avatar = value;
                    AvatarChanged?.Invoke(m_avatar);
                }
            }
        }


        private List<RoomInfo> m_roomsList = null;

        private Dictionary<string, (int minPlayers, int maxPlayers)> m_fixedRoomsDictionary = new Dictionary<string, (int minPlayers, int maxPlayers)>();
        private Dictionary<string, Coroutine> m_validationCoroutines = new Dictionary<string, Coroutine>();

        public bool IsRemoteConnected => PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode;
        public bool Offline {
            get => PhotonNetwork.OfflineMode;
            set {
                PhotonNetwork.OfflineMode = value;
                if (value)
                {
                    TryConnect();
                }
                else if (!value && IsRemoteConnected)
                {
                    Disconnect();
                }
            }
        }
        public IEnumerable<string> AvailableRooms => m_roomsList?.Where(r => r.IsOpen && r.IsVisible).Select(r => r.Name);
        public IEnumerable<RoomData> AllRooms => m_roomsList?.Select(r => new RoomData(r));
        public string ConnectedRoomName => PhotonNetwork.CurrentRoom?.Name;

        private bool m_shouldBeConnected;


        #region [  SETTINGS PART  ]

        public string SettingsSection => "WEAVR Multiplayer";

        public IEnumerable<ISettingElement> Settings => new Setting[]
        {
            ("AppId", PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime, "", SettingsFlags.EditableInPlayer),
            ("AppVersion", PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion, "", SettingsFlags.Runtime),
            ("UseNameServer", PhotonNetwork.PhotonServerSettings.AppSettings.UseNameServer, "", SettingsFlags.Runtime),
            ("FixedRegion", PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion, "", SettingsFlags.EditableInPlayer),
            ("Server", PhotonNetwork.PhotonServerSettings.AppSettings.Server, "", SettingsFlags.Runtime),
            ("Port", PhotonNetwork.PhotonServerSettings.AppSettings.Port, "", SettingsFlags.Runtime),
        };

        #endregion

        private void Awake()
        {
            if (s_instance && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            s_autoConnect = m_autoConnect;


            //WeavrServerManager.Instance.UserChanged -= WeavrServerManager_UserChanged;
            //WeavrServerManager.Instance.UserChanged += WeavrServerManager_UserChanged;

            if(s_autoConnect && m_preferLocalUser)
            {
                LocalUser localUser = null;
                if (TryGetComponent(out NetworkFallbackUser fallbackUser) && Application.isEditor && fallbackUser.useAsTestUser)
                {
                    localUser = fallbackUser.user;
                    if (fallbackUser.writeToJson)
                    {
                        Weavr.WriteToConfigFile("local_player.json", JsonUtility.ToJson(localUser, true));
                    }
                }
                else if (Weavr.TryGetConfigFilePath("local_player.json", out string localPlayerFile))
                {
                    localUser = JsonUtility.FromJson<LocalUser>(File.ReadAllText(localPlayerFile));
                }
                else if(fallbackUser != null)
                {
                    localUser = fallbackUser.user;
                    Weavr.WriteToConfigFile("local_player.json", JsonUtility.ToJson(localUser, true));
                }
                if (localUser != null)
                {
                    CurrentUser = new NetworkUser(localUser.id, localUser.firstName, localUser.lastName);
                }
            }
            
            //// TODO
            //{
            //    SetUser(new Communication.Entities.User()
            //    {
            //        Id = new Guid("1"),
            //        FirstName = "Player ",
            //        LastName = "1",
            //    });
            //}

            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.GameVersion = Weavr.VERSION;
        }

        private void WeavrServerManager_UserChanged(object sender, User newUser)
        {
            SetUser(newUser);
        }

        public bool CanCreateRoomIn(UnityEngine.SceneManagement.Scene scene)
        {
            return IsRemoteConnected && scene.IsValid() && scene.isLoaded && Weavr.TryGetInScene<SceneNetwork>(scene);
        }

        public bool CanCreateRoomInCurrentScene()
        {
            return CanCreateRoomIn(SceneManager.GetActiveScene());
        }

        public void ForceRejoin()
        {
            Disconnect();
            TryConnect();
            TryAutoJoinRooms();
        }

        // Use this for initialization
        public void TryConnect()
        {
            if (IsRemoteConnected) { return; }

            if (CurrentUser != null)
            {
                if (!string.IsNullOrEmpty(CurrentUser.UserId))
                {
                    if (PhotonNetwork.AuthValues == null)
                    {
                        PhotonNetwork.AuthValues = new AuthenticationValues();
                    }
                    PhotonNetwork.AuthValues.UserId = CurrentUser.UserId;
                }
                PhotonNetwork.NickName = $"{CurrentUser.FirstName} {CurrentUser.LastName}";
            }

            ConfigureFromWeavrSettings();
            if (!string.IsNullOrEmpty(PhotonNetwork.PhotonServerSettings.AppSettings.Server)
                && (PhotonNetwork.AuthValues == null || string.IsNullOrEmpty(PhotonNetwork.AuthValues.UserId)))
            {
                PhotonNetwork.AuthValues = new AuthenticationValues(new Guid().ToString());
            }

            //PhotonNetwork.GameVersion = VERSION;
            try
            {
                PhotonNetwork.ConnectUsingSettings();
            }
            catch
            {
                NotificationManager.NotificationError("Connection Error");
            }

            if (IsRemoteConnected)
            {
                m_shouldBeConnected = true;
                if (!string.IsNullOrEmpty(CurrentUser?.UserId))
                {
                    if (PhotonNetwork.IsConnectedAndReady)
                    {
                        OnConnectedToMaster();
                    }
                    else
                    {
                        OnPhotonConnected.Invoke();
                    }
                }
            }
            else
            {
                m_shouldBeConnected = false;
                NotificationManager.NotificationError("Connection Error");
                OnPhotonDisconnected.Invoke("Unavailable");
            }
            //PreviousOnGUI();
        }

        private void TryAutoJoinRooms()
        {
            if (m_autoJoinRooms && !PhotonNetwork.InRoom)
            {
                RoomDefinition fixedRoom = null;
                if (Weavr.TryGetConfigFilePath("fixed_room.json", out string fixedRoomPath))
                {
                    fixedRoom = JsonUtility.FromJson<RoomDefinition>(File.ReadAllText(fixedRoomPath));
                }
                else if (TryGetComponent(out NetworkFixedRoom fixedRoomComponent))
                {
                    fixedRoom = fixedRoomComponent.roomDefinition;
                    Weavr.WriteToConfigFile("fixed_room.json", JsonUtility.ToJson(fixedRoom, true));
                }

                if (fixedRoom != null)
                {
                    AutoJoinTo(fixedRoom.roomNamePrefix, fixedRoom.minPlayers, fixedRoom.maxPlayers, fixedRoom.playerTTL, fixedRoom.closeOnOwnerLeave);
                }
            }
        }

        public void AutoJoinTo(string roomNamePrefix, int minPlayers, int maxPlayers, int playerTTL, bool closeOnOwnerLeave)
        {
            if (!string.IsNullOrEmpty(ConnectedRoomName))
            {
                PhotonNetwork.LeaveRoom();
            }

            var validRooms = m_roomsList?.Where(r => r.Name.Contains(roomNamePrefix));
            var selectedRoom = validRooms?.OrderBy(r => r.Name).Reverse().FirstOrDefault(r => r.IsOpen);
            if(selectedRoom != null)
            {
                JoinRoom(selectedRoom.Name);
            }
            else
            {
                var lastRoomName = validRooms?.OrderBy(r => r.Name).LastOrDefault()?.Name;
                if(lastRoomName != null && int.TryParse(lastRoomName, out int count))
                {
                    lastRoomName = roomNamePrefix + count.ToString("000");
                }
                else
                {
                    lastRoomName = roomNamePrefix + ((validRooms?.Count() ?? 0) + 1).ToString("000");
                }
                CreateRoom(lastRoomName, playerTTL, maxPlayers);
                m_fixedRoomsDictionary[lastRoomName] = (minPlayers, maxPlayers);
            }
        }

        private void OnDestroy()
        {
            //if (WeavrServerManager.WeakInstance)
            //{
            //    WeavrServerManager.WeakInstance.UserChanged -= WeavrServerManager_UserChanged;
            //}
            if (CurrentUser?.Room != null)
            {
                if (closeRoomOnExit && CurrentUser.IsRoomMaster)
                {
                    CurrentUser.Room.EmptyRoomTtl = 2;
                }
                LeaveRoom();
                CurrentUser.Room = null;
            }
            Disconnect();
            if (s_instance == this)
            {
                s_instance = null;
                CurrentUser = null;
            }
        }

        private void LeaveRoom()
        {
            if (IsRemoteConnected)
            {
                if (CurrentUser?.Room != null)
                {
                    PhotonNetwork.DestroyPlayerObjects(PhotonNetwork.NetworkingClient?.LocalPlayer);
                }
                PhotonNetwork.LeaveRoom();
            }
        }

        public void Disconnect()
        {
            NotificationManager.NotificationInfo("Disconnecting...");
            if (IsRemoteConnected)
            {
                m_shouldBeConnected = false;
                if (CurrentUser?.Room != null)
                {
                    PhotonNetwork.DestroyPlayerObjects(PhotonNetwork.NetworkingClient?.LocalPlayer);
                }
                PhotonNetwork.Disconnect();
            }
        }

        private void ConfigureFromWeavrSettings()
        {
            Debug.Log($"[WEAVR Networking]: Writing configuration from {Weavr.Settings.SettingsFilePath}");
            var appSettings = PhotonNetwork.PhotonServerSettings.AppSettings;

            if (Weavr.Settings.TryGetValue("AppId", out string appid) && !string.IsNullOrEmpty(appid))
            {
                appSettings.AppIdRealtime = appid;
                appSettings.AppIdChat = appid;
                appSettings.AppIdVoice = appid;
                Debug.Log($"[WEAVR Networking]:[Connection.config]: AppId = {appid}");
            }
            else
            {
                Weavr.Settings.SetValue("AppId", appSettings.AppIdRealtime);
            }
            if (Weavr.Settings.TryGetValue("AppIdRealtime", out string appIdRealtime) && !string.IsNullOrEmpty(appIdRealtime))
            {
                appSettings.AppIdRealtime = appIdRealtime;
            }
            if (Weavr.Settings.TryGetValue("AppIdChat", out string appIdChat) && !string.IsNullOrEmpty(appIdChat))
            {
                appSettings.AppIdChat = appIdChat;
            }
            if (Weavr.Settings.TryGetValue("AppIdVoice", out string appIdVoice) && !string.IsNullOrEmpty(appIdVoice))
            {
                appSettings.AppIdVoice = appIdVoice;
            }

            appSettings.AppVersion = Weavr.Settings.GetValue("AppVersion", appSettings.AppVersion);
            appSettings.UseNameServer = Weavr.Settings.GetValue("UseNameServer", appSettings.UseNameServer);
            appSettings.FixedRegion = Weavr.Settings.GetValue("FixedRegion", appSettings.FixedRegion);
            appSettings.Server = Weavr.Settings.GetValue("Server", appSettings.Server);
            appSettings.Port = Weavr.Settings.GetValue("Port", appSettings.Port);

            if (m_overwriteSettings)
            {
                PhotonNetwork.PhotonServerSettings.AppSettings = appSettings;
            }
        }

        public void UpdateRoomsList()
        {
            if (!PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
            }
        }

        public void JoinRoom(string roomName)
        {
            PhotonNetwork.JoinRoom(roomName);
        }

        public IEnumerable<RoomData> GetRoomsFor(Communication.Entities.Procedure procedure)
        {
            if (Offline || !IsRemoteConnected)
            {
                return null;
            }
            return m_roomsList?.Where(r => r.Name.Contains(procedure.Id.ToString())).Select(r => new RoomData(r));
        }

        public IEnumerable<RoomData> GetRoomsFor(Procedure.Procedure procedure)
        {
            if (Offline || !IsRemoteConnected)
            {
                return null;
            }
            return m_roomsList?.Where(r => r.Name.Contains(procedure.Guid.ToString())).Select(r => new RoomData(r));
        }

        public void CreateRoom(Procedure.Procedure procedure)
        {
            CreateRoom(FormatRoomName(procedure.name, procedure.Guid));
        }

        public void CreateRoom(Communication.Entities.Procedure procedure)
        {
            CreateRoom(FormatRoomName(procedure.Name, procedure.Id.ToString()));
        }

        private static string FormatRoomName(string procedureName, string procedureId)
        {
            return $"{k_ProcedureRoom} {CurrentUser?.FirstName} {CurrentUser?.LastName} | {CurrentUser?.UserId} | {procedureId} | {procedureName}";
        }

        public void CreateRoom(string roomName, int? playerTTL = null, int? maxPlayers = null)
        {
            if (IsRemoteConnected && PhotonNetwork.IsConnectedAndReady)
            {
                RoomOptions options = new RoomOptions
                {
                    PlayerTtl = playerTTL ?? 30000, // 30 sec
                    EmptyRoomTtl = 5000, // 5 sec
                    CleanupCacheOnLeave = true,
                    MaxPlayers = (byte)(maxPlayers ?? Weavr.Settings.GetValue("MaxPlayers", 12)),
                };

                if (CurrentUser != null)
                {
                    CurrentUser.IsRoomMaster = true;
                }

                PhotonNetwork.CreateRoom(roomName, options);
            }
            else
            {
                PhotonNetwork.GameVersion = Weavr.VERSION;
                PhotonNetwork.ConnectUsingSettings();
            }
            //m_shouldBeConnected = true;
        }

        public string FilterRoomName(string roomName)
        {
            if (roomName != null && roomName.StartsWith(k_ProcedureRoom))
            {
                return roomName.Replace(k_ProcedureRoom, "").Split('|').LastOrDefault().Trim();
            }
            return roomName;
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            if (CurrentUser != null)
            {
                CurrentUser.IsRoomMaster = false;
            }
            base.OnCreateRoomFailed(returnCode, message);
            NotificationManager.NotificationError("Room creation failed");
        }

        public override void OnConnectedToMaster()
        {
            base.OnConnected();
            if (PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode)
            {
                m_shouldBeConnected = true;
                UpdateRoomsList();

                OnPhotonConnected.Invoke();
                NotificationManager.NotificationInfo("Connection Ready");

                if (PhotonNetwork.CurrentRoom != null)
                {
                    OnRoomAvailable.Invoke(PhotonNetwork.CurrentRoom.Name);
                }
            }
        }

        public override void OnConnected()
        {
            base.OnConnected();
            OnPhotonConnected.Invoke();
        }

        public override void OnCreatedRoom()
        {
            base.OnCreatedRoom();
            OnRoomCreated.Invoke(PhotonNetwork.CurrentRoom?.Name);
            NotificationManager.NotificationInfo($"Created Room '{FilterRoomName(PhotonNetwork.CurrentRoom?.Name)}'");
            if (CurrentUser != null)
            {
                CurrentUser.Room = PhotonNetwork.CurrentRoom;
                CurrentUser.IsRoomMaster = true;
            }
            m_shouldBeConnected = true;
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            base.OnDisconnected(cause);
            if (CurrentUser != null)
            {
                CurrentUser.Room = null;
            }
            OnPhotonDisconnected.Invoke(cause.ToString());
            if (cause == DisconnectCause.DisconnectByClientLogic)
            {
                NotificationManager.NotificationInfo($"Disconnected");
            }
            else
            {
                NotificationManager.NotificationError($"Connection Lost");
            }
            if (m_reconnectOnDisconnect && m_shouldBeConnected)
            {
                StartCoroutine(OnDisconnectedFromPhotonInternal());
            }
            m_shouldBeConnected = false;
        }

        public override void OnLeftRoom()
        {
            base.OnLeftRoom();
            OnRoomLeft.Invoke(CurrentUser?.Room?.Name);
            Avatar = null;
            NotificationManager.NotificationInfo($"Left Room '{FilterRoomName(CurrentUser?.Room?.Name)}'");
        }

        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            base.OnPlayerLeftRoom(otherPlayer);
            NotificationManager.NotificationInfo($"{otherPlayer.NickName} left this session");
            if(!string.IsNullOrEmpty(ConnectedRoomName) 
                && CurrentUser != null && CurrentUser.IsRoomMaster 
                && m_fixedRoomsDictionary.TryGetValue(ConnectedRoomName, out (int minPlayers, int maxPlayers) value))
            {
                if(PhotonNetwork.CurrentRoom.PlayerCount < value.minPlayers || PhotonNetwork.CurrentRoom.PlayerCount > value.maxPlayers)
                {
                    // This room has some issues
                    if(m_validationCoroutines.TryGetValue(ConnectedRoomName, out Coroutine coroutine))
                    {
                        StopCoroutine(coroutine);
                    }

                    m_validationCoroutines[ConnectedRoomName] = StartCoroutine(CheckRoomValidity(PhotonNetwork.CurrentRoom, value.minPlayers, value.maxPlayers));
                }
            }

            OnPlayerLeft.Invoke(otherPlayer.NickName);
            PlayerLeftRoom?.Invoke(otherPlayer.UserId, otherPlayer.NickName);
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            base.OnPlayerEnteredRoom(newPlayer);
            OnPlayerEntered.Invoke(newPlayer.NickName);
            PlayerEnteredRoom?.Invoke(newPlayer.UserId, newPlayer.NickName);
            NotificationManager.NotificationInfo($"{newPlayer.NickName} joined this session");
        }

        private IEnumerator CheckRoomValidity(Room room, int minPlayers, int maxPlayers)
        {
            yield return new WaitForSeconds(room.PlayerTtl);
            if(PhotonNetwork.CurrentRoom.Name == room.Name && PhotonNetwork.CurrentRoom.IsOpen && (room.PlayerCount < minPlayers || room.PlayerCount > maxPlayers))
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.DestroyAll();
                }
                LeaveRoom();
            }
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            m_roomsList = roomList;
            if (m_roomsList != null && m_roomsList.Count > 0)
            {
                NotificationManager.NotificationInfo("New Rooms Available");
                OnRoomAvailable.Invoke(m_roomsList[0].Name);
            }
            else
            {
                OnNoRoomAvailable.Invoke();
            }
            OnRoomListUpdated.Invoke();

            TryAutoJoinRooms();
        }

        public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
        {
            base.OnMasterClientSwitched(newMasterClient);
            CurrentUser.IsRoomMaster = newMasterClient.UserId == CurrentUser.UserId;
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            base.OnJoinRoomFailed(returnCode, message);
            if (CurrentUser != null)
            {
                CurrentUser.Room = null;
            }
            NotificationManager.NotificationError("Joined Room Failed: Retry later");

            TryAutoJoinRooms();
        }

        private void FixedUpdate()
        {
            if (!IsRemoteConnected && m_shouldBeConnected)
            {
                m_shouldBeConnected = false;
                NotificationManager.NotificationError("Connection Error");
                OnPhotonDisconnected.Invoke("Connection Error");
            }
            else
            {
                m_shouldBeConnected = IsRemoteConnected;
            }
        }

        public override void OnJoinedRoom()
        {
            m_shouldBeConnected = true;

            if (CurrentUser != null)
            {
                CurrentUser.Room = PhotonNetwork.CurrentRoom;
            }

            string playerName = string.IsNullOrEmpty(PhotonNetwork.NetworkingClient?.NickName) ?
                                CurrentUser?.FirstName + " " + CurrentUser?.LastName :
                                PhotonNetwork.NetworkingClient?.NickName;

            if (Guid.TryParse(playerName, out Guid dummy))
            {
                playerName = string.IsNullOrEmpty(PhotonNetwork.NickName) ? PhotonNetwork.NickName : PhotonNetwork.LocalPlayer.ActorNumber.ToString();
            }

            if (m_lastJoinedRoom != PhotonNetwork.CurrentRoom.Name)
            {
                Debug.Log($"Connected to '{FilterRoomName(CurrentUser.Room.Name)}' as {CurrentUser?.FirstName} {CurrentUser?.LastName}");

                PhotonNetwork.SendRate = networkUpdateRate;
                PhotonNetwork.SerializationRate = networkUpdateRate;

                m_lastJoinedRoom = PhotonNetwork.CurrentRoom.Name;
            }
            
            Avatar = Weavr.TryGetInCurrentScene<SceneNetwork>().InstantiateAvatar(playerName);

            OnRoomJoined.Invoke(m_lastJoinedRoom);
            foreach(var player in PhotonNetwork.CurrentRoom.Players)
            {
                if(player.Value.UserId != PhotonNetwork.LocalPlayer.UserId)
                {
                    OnPlayerAlreadyInRoom?.Invoke(player.Value.NickName);
                }
            }
            NotificationManager.NotificationInfo($"Joined Room '{FilterRoomName(m_lastJoinedRoom)}'");
        }

        public override void OnJoinedLobby()
        {
            base.OnJoinedLobby();
            //if (PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode)
            //{
            //    TryAutoJoinRooms();
            //}
        }

        public void StartCurrentProcedure()
        {
            if (enabled/* && m_role.currentRole != NetworkRole.RoleType.Instructor*/)
            {
                Procedure.ProcedureRunner.Current.StartCurrentProcedure();
            }
        }

        IEnumerator OnDisconnectedFromPhotonInternal()
        {
            bool isNowConnected = !m_shouldBeConnected;
            while (!isNowConnected)
            {
                yield return new WaitForSeconds(1);
                PhotonNetwork.ReconnectAndRejoin();
                isNowConnected = PhotonNetwork.IsConnected;
                Debug.Log("Reconnect And Rejoin");
            }
        }

        [Serializable]
        public class UnityEventString : UnityEvent<string> { }

        [Serializable]
        private struct Events
        {
            public UnityEvent onPhotonConnected;
            public UnityEventString onPhotonDisconnected;
            public UnityEventString onRoomCreated;
            public UnityEventString onRoomAvailable;
            public UnityEvent onNoRoomAvailable;
            public UnityEvent onRoomListUpdated;
            public UnityEventString onRoomJoined;
            public UnityEventString onRoomLeft;
            public UnityEventString onPlayerEnteredRoom;
            public UnityEventString onPlayerLeftRoom;
            public UnityEventString onPlayerAlreadyInRoom;
        }

        public class RoomData
        {
            public int PlayersCount { get; private set; }
            public int MaxPlayers { get; private set; }
            public bool IsOpen { get; private set; }
            public bool IsVisible { get; private set; }
            public bool IsRemoved { get; private set; }
            public string Name { get; private set; }

            public bool IsValid => !IsRemoved && !string.IsNullOrEmpty(Name);

            public (string username, string userid, string procedureName) Split()
            {
                if (Name.StartsWith(k_ProcedureRoom))
                {
                    string name = Name.Replace(k_ProcedureRoom, "").Trim();
                    var splits = Name.Replace(k_ProcedureRoom, "").Trim().Split('|');
                    return (splits.Length > 3 ? splits[0].Trim() : null, splits.Length > 2 ? splits[1].Trim() : null, splits[splits.Length - 1]);
                }
                return (null, null, Name);
            }

            public RoomData(RoomInfo room)
            {
                PlayersCount = room.PlayerCount;
                MaxPlayers = room.MaxPlayers;
                IsOpen = room.IsOpen;
                IsVisible = room.IsVisible;
                IsRemoved = room.RemovedFromList;
                Name = room.Name;
            }
        }
    }
}