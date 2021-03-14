
using System.Threading.Tasks;
using TXT.WEAVR.Player.DataSources;

namespace TXT.WEAVR.Player.Controller
{
    public interface ILibraryController : IController
    {
        Task ShowLibrary();
        Task RefreshLibrary();
        void AddSource(IProcedureDataSource source);
        void RemoveSource(IProcedureDataSource source);
        void CleanUp();
    }
}
