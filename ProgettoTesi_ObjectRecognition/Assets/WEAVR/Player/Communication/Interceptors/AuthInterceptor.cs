using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using TXT.WEAVR.Communication;
using TXT.WEAVR.Player;
using TXT.WEAVR.Player.Communication.Auth;
using TXT.WEAVR.Player.Delegates;
using UnityEngine;
using UnityEngine.Networking;

namespace TXT.WEAVR.Player.Communication
{
    public class AuthInterceptor : IInterceptor
    {
        public event AsyncAction OnFailedTokenAuth;

        public async Task<InterceptOutcome> Intercept(UnityWebRequest webRequest, Request request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(request.TokenId) && webRequest.responseCode == (int)HttpStatusCode.Unauthorized)
            {
                // Send the refresh token
                WWWForm form = new WWWForm();
                form.AddField("client_id", WeavrPlayer.Authentication.ClientID);
                form.AddField("client_secret", WeavrPlayer.Authentication.ClientSecret);
                form.AddField("token_type", WeavrPlayer.Authentication.TokenType);
                form.AddField("grant_type", "refresh_token");
                form.AddField("scope", WeavrPlayer.Authentication.Scope);
                form.AddField("refresh_token", WeavrPlayer.Authentication.RefreshTokenId);

                using (UnityWebRequest refreshWWW = UnityWebRequest.Post(WeavrPlayer.API.IdentityApp.RECONNECT_TOKEN, form))
                {
                    try
                    {
                        await refreshWWW.SendWebRequest();

                        if (!refreshWWW.isNetworkError && !refreshWWW.isHttpError)
                        {
                            // Everything looks fine, get new token
                            var token = JsonConvert.DeserializeObject<AuthToken>(refreshWWW.downloadHandler.text);
                            WeavrPlayer.Authentication.Token = token;

                            // Need to resend the request
                            //await ResendRequest(webRequest, request);
                            request.TokenId = WeavrPlayer.Authentication.AccessTokenId;
                            return InterceptOutcome.RequiresResend;
                        }
                    }
                    catch (Exception e)
                    {
                        WeavrDebug.LogException(this, e);
                    }
                }

                // Then if that fails, need to notify it
                await OnFailedTokenAuth?.Invoke();

                // If something changed and the auth is ok, then redo the same request
                //await ResendRequest(webRequest, request);
                request.TokenId = WeavrPlayer.Authentication.AccessTokenId;
                return InterceptOutcome.RequiresResend;
            }

            return InterceptOutcome.Ok;
        }

        private static async Task ResendRequest(UnityWebRequest webRequest, Request request)
        {
            if (!string.IsNullOrEmpty(WeavrPlayer.Authentication.AccessTokenId))
            {
                request.TokenId = WeavrPlayer.Authentication.AccessTokenId;
                webRequest.SetRequestHeader(RequestHeader.AUTHORIZATION, request.Headers[RequestHeader.AUTHORIZATION]);
                await webRequest.SendWebRequest();
            }
        }
    }

}