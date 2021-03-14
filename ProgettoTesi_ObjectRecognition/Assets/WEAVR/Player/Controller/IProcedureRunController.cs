
using System;
using System.Threading.Tasks;
using TXT.WEAVR.Player.DataSources;

namespace TXT.WEAVR.Player.Controller
{
    public interface IProcedureRunController : IController
    {
        IProcedureProxy CurrentProcedure { get; }
        Task StartProcedure(Guid procedureId, string executionMode, string language);
        Task StopProcedure();
        Task RestartProcedure();
        Task Close();
    }
}
