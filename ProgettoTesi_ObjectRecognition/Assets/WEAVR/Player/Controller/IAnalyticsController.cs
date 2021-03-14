using TXT.WEAVR.Player.DataSources;

namespace TXT.WEAVR.Player.Controller
{
    public interface IAnalyticsController : IController
    {
        void Begin(IProcedureProxy proxy);
        void End();
    }
}
