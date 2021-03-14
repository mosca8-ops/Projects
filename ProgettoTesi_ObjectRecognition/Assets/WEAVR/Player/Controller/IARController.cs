using System;
using System.Threading.Tasks;
using TXT.WEAVR.Core;

namespace TXT.WEAVR.Player.Controller
{
    public interface IARController : IController
    {
        Task Start();
        Task Stop();
        event OnValueChanged<bool> OnAREnabled;
    }
}