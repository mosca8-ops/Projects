using Newtonsoft.Json;
using System;
using System.Collections;
using System.Threading.Tasks;
using TXT.WEAVR.Communication;
using TXT.WEAVR.Player.Communication;
using TXT.WEAVR.Player.Communication.Auth;
using TXT.WEAVR.Player.Communication.DTO;
using TXT.WEAVR.Player.Model;
using TXT.WEAVR.Player.Views;

namespace TXT.WEAVR.Player.Controller
{

    class AuthenticationController : BaseController, IAuthenticationController
    {
        public IUserModel Model { get; private set; }
        public ILoginView LoginView { get; private set; }
        public Guid LastUserId { get; private set; }

        public AuthenticationController(IUserModel model, ILoginView view, IDataProvider provider) : base(provider)
        {
            Model = model ?? provider.GetModel<IUserModel>();
            LoginView = view ?? provider.GetView<ILoginView>();
        }

        public async Task Login()
        {
            if (!WeavrPlayer.Options.Login)
            {
                Model.AuthUser = WeavrPlayer.Authentication.AuthUser = WeavrPlayer.Authentication.CreateFakeUser("Student");
                return;
            }
            if (ValidateUser(Model))
            {
                // Save the last used guid
                LastUserId = Model.AuthUser?.Id ?? Guid.Empty;
                AddInterceptors();
                return;
            }

            // Show the login view
            ShowLoginView();

            while (Model.AuthUser == null)
            {
                await Task.Yield();
            }

            // Check if last guid has changed
            if(LastUserId != Guid.Empty && LastUserId != Model.AuthUser.Id)
            {
                // Restart everything except login
                LastUserId = Model.AuthUser.Id;
                DataProvider.GetController<ICoreController>().Restart();
            }
        }

        private bool ValidateUser(IUserModel model)
        {
            if(WeavrPlayer.Authentication.AccessTokenId != null && WeavrPlayer.Authentication.AuthUser != null)
            {
                model.AuthUser = WeavrPlayer.Authentication.AuthUser;
                //model.CurrentUser = model.AuthUser.User;
                return true;
            }
            // TODO: Check if setting the AuthUser from the model makes sense (added here mostly for testing purposes)
            else if(model.AuthUser?.Token?.Access_Token != null)
            {
                WeavrPlayer.Authentication.AuthUser = model.AuthUser;
                return true;
            }

            // Get the last saved authorized user, if any.
            var lastAuthUser = DataProvider.GetPersistentData<AuthUser>(WeavrPlayer.Constants.AUTH_LAST_USER);
            if (lastAuthUser != null)
            {
                var now = DateTime.Now;
                
                // Get the last token update
                var lastUpdateDate = DataProvider.GetPersistentData<DateTime>(WeavrPlayer.Constants.AUTH_LAST_TOKEN_UPDATE);
                if(now < lastUpdateDate)
                {
                    // Most probably the time of the machine has been changed -> need to re-authorize
                    return false;
                }
                
                // Get the next expire date of the token
                var nextExpireDate = DataProvider.GetPersistentData<DateTime>(WeavrPlayer.Constants.AUTH_NEXT_TOKEN_EXPIRE_DATE);
                
                // Check if token has expired, if yes -> update with refresh one, otherwise -> return valid user
                if(now < nextExpireDate)
                {
                    // Set the values and return valid user
                    WeavrPlayer.Authentication.AuthUser = lastAuthUser;
                    model.AuthUser = lastAuthUser;
                    return true;
                }

                // TODO: Decide whether to send the refresh token, or invalidate the user
                // For now we will invalidate the user
                return false;
            }

            return false;
        }

        public async Task Logout()
        {
            try
            {
                var www = new WeavrWebRequest();
                await www.POST(new Request()
                {
                    Url = WeavrPlayer.API.IdentityApp.LOGOUT,
                });
            }
            catch (Exception ex)
            {
                WeavrDebug.LogException(this, ex);
            }
            finally
            {
                // Remove interceptors
                WeavrPlayer.Authentication.AuthUser = null;
                Model.AuthUser = null;
                WeavrWebRequest.RequestInterceptors.RemoveAll<TokenInjectInterceptor>();
                WeavrWebRequest.ResponseInterceptors.RemoveAll<AuthInterceptor>();
                DataProvider.ClearData(WeavrPlayer.Constants.AUTH_LAST_USER);
                DataProvider.ClearData(WeavrPlayer.Constants.AUTH_LAST_TOKEN_UPDATE);
                DataProvider.ClearData(WeavrPlayer.Constants.AUTH_NEXT_TOKEN_EXPIRE_DATE);
            }
        }

        public async Task<bool> ChangePassword(string currentPassword, string newPassword)
        {
            try
            {
                var response = await new WeavrWebRequest().POST(new Request()
                {
                    Url = WeavrPlayer.API.IdentityApp.RESET_PASSWORD,
                    ContentType = MIME.JSON,
                    Body = new ChangePasswordModel()
                    {
                        Email = WeavrPlayer.Authentication.AuthUser.User.Email,
                        Password = currentPassword,
                        ConfirmPassword = newPassword,
                        Token = WeavrPlayer.Authentication.AccessTokenId,
                    },
                });

                return response.Code == (int)HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                WeavrDebug.LogException(this, ex);
            }
            return false;
        }

        public void ShowLoginView()
        {
            PrepareLoginView();
            LoginView.Show();
        }

        public void ClearSavedData()
        {
            DataProvider.ClearData(WeavrPlayer.Constants.LOGIN_USERNAME);
            DataProvider.ClearData(WeavrPlayer.Constants.LOGIN_PASSWORD);
            DataProvider.ClearData(WeavrPlayer.Constants.LOGIN_REMEMBER_ME);
        }

        private void PrepareLoginView()
        {
            LoginView.OnHide -= View_OnHide;
            LoginView.OnHide += View_OnHide;
            ClearView(LoginView);

            // Fill up with data
            LoginView.Username = DataProvider.GetPersistentData<string>(WeavrPlayer.Constants.LOGIN_USERNAME);
            LoginView.Password = DataProvider.GetPersistentData<string>(WeavrPlayer.Constants.LOGIN_PASSWORD);
            LoginView.RememberMe = DataProvider.GetPersistentData<bool>(WeavrPlayer.Constants.LOGIN_REMEMBER_ME);

            // Assign events
            LoginView.OnSubmitLogin -= View_OnSubmitLogin;
            LoginView.OnSubmitLogin += View_OnSubmitLogin;

            LoginView.OnForgotPassword -= View_OnForgotPassword;
            LoginView.OnForgotPassword += View_OnForgotPassword;
        }

        private void View_OnForgotPassword()
        {

        }

        private async void View_OnSubmitLogin()
        {
            LoginView.StartLoading("Logging in...");
            try
            {
                var www = new WeavrWebRequest();

                var locationData = await WeavrPlayer.GetLocationAsync();
                var response = await www.POST(new Request()
                {
                    Url = WeavrPlayer.API.IdentityApp.LOGIN,
                    ContentType = MIME.JSON,
                    Body = new LoginDTO()
                    {
                        Email = LoginView.Username,
                        Password = LoginView.Password,
                        RememberMe = LoginView.RememberMe,
                        ClientId = WeavrPlayer.Authentication.ClientID,
                        Secret = WeavrPlayer.Authentication.ClientSecret,
                        Scope = WeavrPlayer.Authentication.Scope,
                        AdditionalInfo = new AdditionalInfo()
                        {
                            DeviceIdentifier = WeavrPlayer.UID,
                            DevicePlatform = WeavrPlayer.PLATFORM,
                            PlayerVersion = WeavrPlayer.VERSION,
                            Location = !locationData.HasValue ? null : new Location()
                            {
                                Longitude = locationData.Value.longitude,
                                Latitude = locationData.Value.latitude,
                                Altitude = locationData.Value.altitude,
                                HorizontalAccuracy = locationData.Value.horizontalAccuracy,
                                VerticalAccuracy = locationData.Value.verticalAccuracy,
                                Timestamp = locationData.Value.timestamp,
                            }
                        }
                    },
                });

                if (response.IsNetworkError)
                {
                    WeavrDebug.LogError(WeavrPlayer.API.IdentityApp.DEBUG_NAME + ":Login", response.FullError);
                }
                else if (response.IsHttpError)
                {
                    PopupManager.ShowError(Translate(WeavrPlayer.Labels.LoginError), Translate(response.FullError));
                }
                else if (!response.WasCancelled)
                {
                    // Get the token here
                    // Model.CurrentUser = JsonConvert.DeserializeObject<User>(response.Text);
                    var authUser = JsonConvert.DeserializeObject<AuthUser>(response.Text);

                    WeavrPlayer.Authentication.AuthUser = authUser;
                    Model.AuthUser = authUser;

                    if (LoginView.RememberMe)
                    {
                        // Save the values
                        DataProvider.SavePersistentData(WeavrPlayer.Constants.LOGIN_USERNAME, LoginView.Username);
                        DataProvider.SavePersistentData(WeavrPlayer.Constants.LOGIN_PASSWORD, LoginView.Password);
                        DataProvider.SavePersistentData(WeavrPlayer.Constants.LOGIN_REMEMBER_ME, LoginView.RememberMe);

                        // And Token Values
                        DataProvider.SavePersistentData(WeavrPlayer.Constants.AUTH_LAST_USER, authUser);
                        DataProvider.SavePersistentData(WeavrPlayer.Constants.AUTH_LAST_TOKEN_UPDATE, DateTime.Now);
                        DataProvider.SavePersistentData(WeavrPlayer.Constants.AUTH_NEXT_TOKEN_EXPIRE_DATE, DateTime.Now.AddSeconds(authUser.Token.Expires_In));
                    }
                    else
                    {
                        // Clear persistent data
                        ClearSavedData();
                    }

                    // Add the token and auth interceptors
                    AddInterceptors();

                    LoginView.Hide();
                }
            }
            catch (Exception e)
            {
                WeavrDebug.LogError("WeavrPlayer:Login", e.Message);
            }
            finally
            {
                LoginView.StopLoading();
            }
        }

        private void AddInterceptors()
        {
            WeavrWebRequest.RequestInterceptors.AddUnique(new TokenInjectInterceptor() { ForceInject = false });
            AuthInterceptor authInterceptor = WeavrWebRequest.ResponseInterceptors.Get<AuthInterceptor>(createIfNotPresent: true);
            authInterceptor.OnFailedTokenAuth -= AuthInterceptor_OnFailedTokenAuth;
            authInterceptor.OnFailedTokenAuth += AuthInterceptor_OnFailedTokenAuth;
        }

        private async Task AuthInterceptor_OnFailedTokenAuth()
        {
            // Clear some data first and then redo the login
            WeavrPlayer.Authentication.AuthUser = null;
            Model.AuthUser = null;
            DataProvider.ClearData(WeavrPlayer.Constants.AUTH_LAST_USER);
            DataProvider.ClearData(WeavrPlayer.Constants.AUTH_LAST_TOKEN_UPDATE);
            DataProvider.ClearData(WeavrPlayer.Constants.AUTH_NEXT_TOKEN_EXPIRE_DATE);
            await Login();
        }

        private void View_OnHide(IView view)
        {
            //if (view is ILoginView loginView)
            //{
            //    ClearView(loginView);
            //}
        }

        private void ClearView(ILoginView loginView)
        {
            loginView.Username = string.Empty;
            loginView.Password = string.Empty;
        }
    }
}
