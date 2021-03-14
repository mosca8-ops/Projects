
using System;
using TXT.WEAVR.Player.DataSources;
using TXT.WEAVR.Player.Views;

namespace TXT.WEAVR.Player.Controller
{
    public interface IProcedureController : IController
    {
        void ShowView(Guid procedureId);
        void ShowView(IProcedureViewModel viewModel);
    }
}
