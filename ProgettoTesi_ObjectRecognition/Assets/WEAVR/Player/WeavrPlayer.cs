using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TXT.WEAVR.Player.Communication.Auth;
using UnityEngine;

namespace TXT.WEAVR.Player
{
    [Serializable]
    public partial class WeavrPlayerOptions
    {
        public bool Login = true;
        public bool BuiltinProcedures = true;
        public bool Offline = false;
    }

    public partial class WeavrPlayer
    {
        public static string VERSION => "1.3.0";
        // Standard
        // OpenVR
        // Oculus
        // Pico
        // Hololens
        public static string INPUT_PROVIDER { get; internal set; } = InputProviders.Standard;
        public static string PLATFORM => GetPlatform();
        public static string UID => SystemInfo.deviceUniqueIdentifier;
        public static async Task<LocationInfo?> GetLocationAsync()
        {
            if (!Input.location.isEnabledByUser)
            {
                return null;
            }

            try
            {
                if (Input.location.status != LocationServiceStatus.Running
                    && Input.location.status != LocationServiceStatus.Initializing)
                {
                    Input.location.Start();
                }

                await Task.Delay(500);
                Input.location.Stop();
            }
            catch(Exception ex)
            {
                WeavrDebug.LogException(nameof(WeavrDebug), ex);
            }
            return Input.location.lastData;
        }

        private static string GetPlatform()
        {
            switch (Application.platform)
            {
#if UNITY_EDITOR
                case RuntimePlatform.WindowsEditor:
                    switch (UnityEditor.EditorUserBuildSettings.activeBuildTarget)
                        {
                            case UnityEditor.BuildTarget.StandaloneOSX: return RuntimePlatform.OSXPlayer.ToString();
                            case UnityEditor.BuildTarget.StandaloneWindows: return RuntimePlatform.WindowsPlayer.ToString();
                            case UnityEditor.BuildTarget.iOS: return RuntimePlatform.IPhonePlayer.ToString();
                            case UnityEditor.BuildTarget.Android: return RuntimePlatform.Android.ToString();
                            case UnityEditor.BuildTarget.StandaloneWindows64: return RuntimePlatform.WindowsPlayer.ToString();
                            case UnityEditor.BuildTarget.WebGL: return RuntimePlatform.WebGLPlayer.ToString();
                            case UnityEditor.BuildTarget.WSAPlayer: return RuntimePlatform.WSAPlayerX86.ToString();
                            case UnityEditor.BuildTarget.StandaloneLinux64: return RuntimePlatform.LinuxPlayer.ToString();
                            case UnityEditor.BuildTarget.PS4: return RuntimePlatform.PS4.ToString();
                            case UnityEditor.BuildTarget.XboxOne: return RuntimePlatform.XboxOne.ToString();
                            case UnityEditor.BuildTarget.tvOS: return RuntimePlatform.tvOS.ToString();
                            case UnityEditor.BuildTarget.Switch: return RuntimePlatform.Switch.ToString();
                            case UnityEditor.BuildTarget.Lumin: return RuntimePlatform.Lumin.ToString();
                            case UnityEditor.BuildTarget.Stadia: return RuntimePlatform.Stadia.ToString();
                        }
                    return RuntimePlatform.WindowsPlayer.ToString();
#endif
                default: return Application.platform.ToString();
            }
        }

        public static WeavrPlayerOptions Options { get; internal set; } = new WeavrPlayerOptions();
        
        public partial class InputProviders
        {
            public const string Standard = "Standard";
            public const string OpenVR = "OpenVR";
            public const string Oculus = "Oculus";
            public const string Pico = "Pico";
            public const string Hololens = "Hololens";
        }

        public partial class API
        {
            public static string BASE_URL { get; set; }

            // Identity-App
            public partial class IdentityApp
            {
                private const string BASE_API = "api/v1/";
                public static string DEBUG_NAME => "WeavrPlayer:Identity";
                public static string BASE_APP_URL { get; set; } = "/identity-app/";

                // Login
                public static string LOGIN => string.Concat(BASE_URL, BASE_APP_URL, BASE_API, "Account/Login");
                public static string LOGOUT => string.Concat(BASE_URL, BASE_APP_URL, BASE_API, "Account/Logout");
                public static string RESET_PASSWORD => string.Concat(BASE_URL, BASE_APP_URL, BASE_API, "Account/ResetPassword");
                public static string SEND_RESET_PASSWORD => string.Concat(BASE_URL, BASE_APP_URL, BASE_API, "Account/SendResetPassword");

                // Tokens
                public static string RECONNECT_TOKEN => string.Concat(BASE_URL, BASE_APP_URL, "connect/token");

                // Groups
                public static string USERS_GROUPS(Guid userId) => string.Concat(BASE_URL, BASE_APP_URL, BASE_API, "Users/", userId.ToString(), "/Groups");
                public static string GROUPS(Guid groupId) => string.Concat(BASE_URL, BASE_APP_URL, BASE_API, "Groups/", groupId.ToString());
            }

            // Content-App
            public partial class ContentApp
            {
                private const string BASE_API = "api/v1/";
                public static string DEBUG_NAME => "WeavrPlayer:Content";
                public static string BASE_APP_URL { get; set; } = "/content-app/";
                public static string BASE_CONTENT_APP_URL { get; set; } = "/contentfileprovider-app/";

                // PROCEDURES
                public static string PROCEDURE(Guid procedureId) => string.Concat(BASE_URL, BASE_APP_URL, BASE_API, "Procedures/", procedureId.ToString());
                public static string SCENE(Guid sceneId) => string.Concat(BASE_URL, BASE_APP_URL, "api/", "Scenes/Get/", sceneId.ToString());
                public static string HIERARCHY(Guid userGuid) => string.Concat(BASE_URL, BASE_APP_URL, BASE_API, "Procedures/Hierarchy/", userGuid.ToString());
                public static string GET_PROCEDURE_FILE => string.Concat(BASE_URL, BASE_CONTENT_APP_URL, BASE_API, "FileProvider/ProcedureVersionPlatformFile");
                public static string GET_SCENE_FILE => string.Concat(BASE_URL, BASE_CONTENT_APP_URL, BASE_API, "FileProvider/SceneVersionPlatformFile");
            }

            public partial class AnalyticsApp
            {
                private const string BASE_API = "api/v1/";
                public static string BASE_ANALYTICS_APP_URL { get; set; } = "/analytic-app/";
                public static string XAPI_FEED => string.Concat(BASE_URL, BASE_ANALYTICS_APP_URL, BASE_API, "XApi");
            }
        }

        public partial class Constants
        {
            private const string PLAYER = "PLAYER:";
            private const string LOGIN = "LOGIN:";
            private const string AUTH = "AUTH:";


            // Login part
            internal const string LOGIN_USERNAME = PLAYER + LOGIN + "SAVED_USERNAME";
            internal const string LOGIN_PASSWORD = PLAYER + LOGIN + "SAVED_PASSWORD";
            internal const string LOGIN_REMEMBER_ME = PLAYER + LOGIN + "SAVED_REMEMBER_ME";

            // Auth part
            internal const string AUTH_LAST_ACCESS_TOKEN = PLAYER + AUTH + "SAVED_LAST_ACCESS_TOKEN";
            internal const string AUTH_LAST_REFRESH_TOKEN = PLAYER + AUTH + "SAVED_LAST_REFRESH_TOKEN";
            internal const string AUTH_LAST_USER = PLAYER + AUTH + "SAVED_LAST_USER";
            internal const string AUTH_NEXT_TOKEN_EXPIRE_DATE = PLAYER + AUTH + "NEXT_TOKEN_EXPIRE_DATE";
            internal const string AUTH_LAST_TOKEN_UPDATE = PLAYER + AUTH + "LAST_TOKEN_UPDATE";
        }

        public partial class Labels
        {
            public static string LoginError => "Login Error";
            public static string ChangePasswordError => "Change Password Error";
            public static string CannotChangePasswordError => "Cannot change password";
            public static string Info => "Info";
        }

        public partial class Authentication
        {
            // TODO: Move these values into a configuration file maybe
            internal const string ClientID = "ro.weavrPlayer";
            internal const string ClientSecret = "secret";
            internal const string TokenType = "Bearer";
            internal const string Scope = "offline_access openid profile identity analytic collaboration content manageruiagg server";

            public static AuthUser AuthUser { get; internal set; }
            public static string AccessTokenId => Token?.Access_Token;
            public static string RefreshTokenId => Token?.Refresh_Token;

            public static AuthToken Token
            {
                get => AuthUser?.Token;
                internal set
                {
                    if (AuthUser != null && AuthUser.Token != value)
                    {
                        AuthUser.Token = value;
                    }
                }
            }

            internal static AuthUser CreateFakeUser(string name) => new AuthUser()
            {
                Id = Guid.Empty,
                Roles = new string[] { "Student" },
                Token = new AuthToken()
                {
                    Access_Token = string.Empty,
                    Expires_In = 100000,
                    Id_Token = string.Empty,
                    Scope = Scope,
                    Token_Type = "Fake",
                    Refresh_Token = string.Empty
                },
                User = new WEAVR.Communication.Entities.User()
                {
                    AuthUserId = Guid.Empty,
                    FirstName = name,
                    LastName = string.Empty,
                }
            };

            public static ConnectToken CreateConnectToken() => new ConnectToken()
            {
                Client_Id = ClientID,
                Client_Secret = ClientSecret,
                Grant_Type = "refresh_token",
                Refresh_Token = RefreshTokenId,
                Scope = Scope,
                Token_Type = TokenType
            };
        }
    }
}
