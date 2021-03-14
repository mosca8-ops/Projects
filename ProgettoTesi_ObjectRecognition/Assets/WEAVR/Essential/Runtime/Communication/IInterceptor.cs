using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace TXT.WEAVR.Communication
{
    public enum InterceptOutcome
    {
        Ok,
        RequiresResend,
        Error,
    }

    public interface IInterceptor
    {
        Task<InterceptOutcome> Intercept(UnityWebRequest webRequest, Request request, CancellationToken token);
    }
}