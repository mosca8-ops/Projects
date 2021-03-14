using System.Threading;
using System.Threading.Tasks;
using TXT.WEAVR.Communication;
using TXT.WEAVR.Player;
using UnityEngine.Networking;

namespace TXT.WEAVR.Player.Communication
{
    public class TokenInjectInterceptor : IInterceptor
    {
        public bool ForceInject { get; set; }

        public Task<InterceptOutcome> Intercept(UnityWebRequest webRequest, Request request, CancellationToken cancellationToken)
        {
            if((ForceInject || string.IsNullOrEmpty(request.TokenId)) && request.Url.StartsWith(WeavrPlayer.API.BASE_URL))
            {
                request.TokenId = WeavrPlayer.Authentication.AccessTokenId;
            }
            return Task.FromResult(InterceptOutcome.Ok);
        }
    }

}